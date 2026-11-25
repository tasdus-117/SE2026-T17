using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace SmallArena.Tools
{
    // A non-inherited job handle kills every assigned process if the runner itself dies.
    public sealed class FourPlayerProcessJob : IDisposable
    {
        [StructLayout(LayoutKind.Sequential)] private struct BasicLimits
        {
            public long ProcessTime, JobTime; public uint Flags;
            public UIntPtr MinimumWorkingSet, MaximumWorkingSet;
            public uint ActiveProcesses; public UIntPtr Affinity; public uint Priority, Scheduling;
        }
        [StructLayout(LayoutKind.Sequential)] private struct Counters { public ulong A, B, C, D, E, F; }
        [StructLayout(LayoutKind.Sequential)] private struct Limits
        {
            public BasicLimits Basic; public Counters IO;
            public UIntPtr ProcessMemory, JobMemory, PeakProcessMemory, PeakJobMemory;
        }
        [StructLayout(LayoutKind.Sequential)] private struct Startup
        {
            public int Size; public IntPtr Reserved, Desktop, Title;
            public uint X, Y, Width, Height, XCount, YCount, Fill, Flags;
            public ushort Show, ReservedSize; public IntPtr ReservedBytes, Input, Output, Error;
        }
        [StructLayout(LayoutKind.Sequential)] private struct ProcessInfo { public IntPtr Process, Thread; public uint Id, ThreadId; }
        [StructLayout(LayoutKind.Sequential)] private struct StartupEx { public Startup Startup; public IntPtr Attributes; }
        [StructLayout(LayoutKind.Sequential)] private struct Accounting
        { public long UserTime, KernelTime, PeriodUserTime, PeriodKernelTime; public uint PageFaults, TotalProcesses, ActiveProcesses, TerminatedProcesses; }
        [StructLayout(LayoutKind.Sequential)] private struct Security { public int Size; public IntPtr Descriptor; [MarshalAs(UnmanagedType.Bool)] public bool Inherit; }
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)] private static extern IntPtr CreateJobObject(IntPtr security, string name);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool SetInformationJobObject(IntPtr job, int info, ref Limits limits, uint size);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool InitializeProcThreadAttributeList(IntPtr list, int count, int flags, ref IntPtr size);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool UpdateProcThreadAttribute(IntPtr list, uint flags, IntPtr attribute, IntPtr value, IntPtr size, IntPtr previous, IntPtr returned);
        [DllImport("kernel32.dll")] private static extern void DeleteProcThreadAttributeList(IntPtr list);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool TerminateJobObject(IntPtr job, uint exitCode);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool QueryInformationJobObject(IntPtr job, int info, out Accounting accounting, uint length, IntPtr returnedLength);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool TerminateProcess(IntPtr process, uint exitCode);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern uint ResumeThread(IntPtr thread);
        [DllImport("kernel32.dll")] private static extern bool CloseHandle(IntPtr handle);
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)] private static extern IntPtr CreateFile(string path, uint access, uint share, ref Security security, uint disposition, uint flags, IntPtr template);
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)] private static extern bool CreateProcess(string app, StringBuilder command, IntPtr processSecurity, IntPtr threadSecurity, bool inherit, uint flags, IntPtr environment, string directory, ref StartupEx startup, out ProcessInfo process);
        private IntPtr handle;
        private Timer timer;
        private volatile bool expired;
        public bool TimedOut { get { return expired; } }
        public FourPlayerProcessJob(int timeoutSeconds)
        {
            if (timeoutSeconds < 1 || timeoutSeconds > 3600) throw new ArgumentOutOfRangeException("timeoutSeconds");
            handle = CreateJobObject(IntPtr.Zero, null);
            if (handle == IntPtr.Zero) throw new Win32Exception();
            var limits = new Limits(); limits.Basic.Flags = 0x2000; // KILL_ON_JOB_CLOSE
            if (!SetInformationJobObject(handle, 9, ref limits, (uint)Marshal.SizeOf(typeof(Limits))))
            { CloseHandle(handle); handle = IntPtr.Zero; throw new Win32Exception(); }
            timer = new Timer(_ => { expired = true; TerminateJobObject(handle, 124); }, null, timeoutSeconds * 1000, Timeout.Infinite);
        }
        private static IntPtr Open(string path, uint access, uint disposition)
        {
            var security = new Security { Size = Marshal.SizeOf(typeof(Security)), Inherit = true };
            var value = CreateFile(path, access, 3, ref security, disposition, 0x80, IntPtr.Zero);
            if (value == new IntPtr(-1)) throw new Win32Exception();
            return value;
        }
        public Process Start(string executable, string arguments, string directory, string stdout, string stderr)
        {
            if (handle == IntPtr.Zero || expired) throw new InvalidOperationException("Watchdog is closed/expired.");
            IntPtr output = IntPtr.Zero, error = IntPtr.Zero, input = IntPtr.Zero;
            IntPtr attributes = IntPtr.Zero, jobList = IntPtr.Zero;
            bool initialized = false;
            var info = new ProcessInfo();
            bool created = false, resumed = false;
            try
            {
                output = Open(stdout, 0x40000000, 2); error = Open(stderr, 0x40000000, 2); input = Open("NUL", 0x80000000, 3);
                var size = IntPtr.Zero;
                InitializeProcThreadAttributeList(IntPtr.Zero, 1, 0, ref size);
                attributes = Marshal.AllocHGlobal(size);
                initialized = InitializeProcThreadAttributeList(attributes, 1, 0, ref size);
                if (!initialized) throw new Win32Exception();
                jobList = Marshal.AllocHGlobal(IntPtr.Size);
                Marshal.WriteIntPtr(jobList, handle);
                // Windows 10+: assignment is atomic with creation, even if the runner dies here.
                if (!UpdateProcThreadAttribute(attributes, 0, new IntPtr(0x0002000D), jobList, new IntPtr(IntPtr.Size), IntPtr.Zero, IntPtr.Zero)) throw new Win32Exception();
                var startup = new StartupEx { Attributes = attributes, Startup = new Startup { Size = Marshal.SizeOf(typeof(StartupEx)), Flags = 0x101, Input = input, Output = output, Error = error } };
                // Suspend until we cache a process handle; the child is already inside the job.
                created = CreateProcess(executable, new StringBuilder("\"" + executable + "\" " + arguments), IntPtr.Zero, IntPtr.Zero,
                    true, 0x08080004, IntPtr.Zero, directory, ref startup, out info);
                if (!created) throw new Win32Exception();
                var process = Process.GetProcessById((int)info.Id);
                var processHandle = process.Handle;
                if (ResumeThread(info.Thread) == uint.MaxValue) { process.Dispose(); throw new Win32Exception(); }
                resumed = true;
                return process;
            }
            finally
            {
                if (created && !resumed) TerminateProcess(info.Process, 1);
                if (info.Thread != IntPtr.Zero) CloseHandle(info.Thread);
                if (info.Process != IntPtr.Zero) CloseHandle(info.Process);
                if (output != IntPtr.Zero) CloseHandle(output);
                if (error != IntPtr.Zero) CloseHandle(error);
                if (input != IntPtr.Zero) CloseHandle(input);
                if (initialized) DeleteProcThreadAttributeList(attributes);
                if (attributes != IntPtr.Zero) Marshal.FreeHGlobal(attributes);
                if (jobList != IntPtr.Zero) Marshal.FreeHGlobal(jobList);
            }
        }
        public void Dispose()
        {
            if (timer != null)
            {
                using (var completed = new ManualResetEvent(false)) { timer.Dispose(completed); completed.WaitOne(); }
                timer = null;
            }
            if (handle != IntPtr.Zero)
            {
                try
                {
                    if (!TerminateJobObject(handle, 125)) throw new Win32Exception();
                    var deadline = Stopwatch.StartNew();
                    while (true)
                    {
                        Accounting accounting;
                        if (!QueryInformationJobObject(handle, 1, out accounting, (uint)Marshal.SizeOf(typeof(Accounting)), IntPtr.Zero)) throw new Win32Exception();
                        if (accounting.ActiveProcesses == 0) break;
                        if (deadline.Elapsed.TotalSeconds > 5) throw new InvalidOperationException("Owned job still has live processes after termination.");
                        Thread.Sleep(25);
                    }
                }
                finally { CloseHandle(handle); handle = IntPtr.Zero; }
            }
        }
    }
}
