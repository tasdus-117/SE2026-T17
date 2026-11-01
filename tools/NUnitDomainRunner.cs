// Standalone verification host. This file is outside Assets and never enters Unity assemblies.
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework.Api;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;

internal static class NUnitDomainRunner
{
    public static int Main(string[] args)
    {
        if (args.Length != 2)
        {
            Console.Error.WriteLine("Usage: NUnitDomainRunner <test-assembly> <result.xml>");
            return 2;
        }

        var testAssembly = Path.GetFullPath(args[0]);
        var domainPath = Path.Combine(Path.GetDirectoryName(testAssembly), "SmallArena.Domain.dll");
        var domain = Assembly.LoadFrom(domainPath);
        if (!string.Equals(domain.Location, domainPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Runner loaded a domain assembly from the wrong directory.");
        }
        var runner = new NUnitTestAssemblyRunner(new DefaultTestAssemblyBuilder());
        var settings = new Dictionary<string, object> { { "NumberOfTestWorkers", 0 } };
        if (runner.Load(Assembly.LoadFrom(testAssembly), settings) == null)
        {
            Console.Error.WriteLine("NUnit failed to load the test assembly.");
            return 2;
        }

        var result = runner.Run(TestListener.NULL, TestFilter.Empty);
        File.WriteAllText(args[1], result.ToXml(true).OuterXml);
        PrintCases(result);
        Console.WriteLine($"Passed: {result.PassCount}; failed: {result.FailCount}; skipped: {result.SkipCount}; inconclusive: {result.InconclusiveCount}");
        return result.PassCount > 0 && result.FailCount == 0 && result.SkipCount == 0 && result.InconclusiveCount == 0 ? 0 : 1;
    }

    private static void PrintCases(ITestResult result)
    {
        if (!result.HasChildren)
        {
            Console.WriteLine($"{result.ResultState.Status}: {result.FullName}");
            if (result.ResultState.Status == TestStatus.Failed)
            {
                Console.WriteLine(result.Message);
            }
            return;
        }

        foreach (var child in result.Children)
        {
            PrintCases(child);
        }
    }
}
