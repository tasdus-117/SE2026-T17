using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SmallArena.Networking
{
    public enum ArenaNetworkRole { None, Server, Client, Host }

    /// <summary>Explicit network entry point for standalone clients/servers; ordinary menu startup is unchanged.</summary>
    public sealed class ArenaLaunchOptions
    {
        public ArenaNetworkRole Role { get; private set; }
        public string Address { get; private set; } = "localhost";
        public ushort Port { get; private set; }

        public static ArenaLaunchOptions Read(string[] arguments)
        {
            var result = new ArenaLaunchOptions();
            for (var index = 0; index < arguments.Length; index++)
            {
                var argument = arguments[index];
                var role = argument == "--arena-server" ? ArenaNetworkRole.Server
                    : argument == "--arena-client" ? ArenaNetworkRole.Client
                    : argument == "--arena-host" ? ArenaNetworkRole.Host : ArenaNetworkRole.None;
                if (role != ArenaNetworkRole.None)
                {
                    if (result.Role != ArenaNetworkRole.None) throw new ArgumentException("Choose only one --arena-server, --arena-client or --arena-host role.");
                    result.Role = role;
                }
                else if (argument == "--arena-address")
                {
                    if (++index >= arguments.Length || string.IsNullOrWhiteSpace(arguments[index])) throw new ArgumentException("--arena-address requires a host address.");
                    result.Address = arguments[index];
                }
                else if (argument == "--arena-port")
                {
                    if (++index >= arguments.Length || !ushort.TryParse(arguments[index], out var port) || port == 0) throw new ArgumentException("--arena-port requires a port between 1 and 65535.");
                    result.Port = port;
                }
            }
            return result;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnterBootstrap()
        {
            var options = Read(Environment.GetCommandLineArgs());
            if (options.Role != ArenaNetworkRole.None && SceneManager.GetActiveScene().name != "Bootstrap")
                SceneManager.LoadSceneAsync("Bootstrap");
        }
    }
}
