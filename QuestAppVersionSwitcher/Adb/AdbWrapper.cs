using System;
using System.Collections.Generic;
using Android.Provider;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ComputerUtils.Android.Logging;
using Application = Android.App.Application;

namespace DanTheMan827.OnDeviceADB
{
    /// <summary>
    /// Provides various ADB (Android Debug Bridge) functionalities.
    /// </summary>
    public static class AdbWrapper
    {
        /// <summary>
        /// Gets the path to the ADB executable.
        /// </summary>
        public static string? AdbPath => AdbServer.AdbPath;

        /// <summary>
        /// Checks if the ADB server is running.
        /// </summary>
        public static bool IsServerRunning => AdbServer.Instance.IsRunning;

        /// <summary>
        /// Gets or sets the state of ADB over WiFi.
        /// </summary>
        [DebuggerHidden]
        public static AdbWifiState AdbWifiState
        {
            get => Settings.Global.GetInt(Application.Context.ContentResolver, "adb_wifi_enabled") == 1 ? AdbWifiState.Enabled : AdbWifiState.Disabled;
            set => Settings.Global.PutInt(Application.Context.ContentResolver, "adb_wifi_enabled", (int)value);
        }

        /// <summary>
        /// Starts the ADB server asynchronously.
        /// </summary>
        public static void StartServer()
        {
            AdbServer.Instance.Start();
            Thread.Sleep(100);
        }

        /// <summary>
        /// Stops the ADB server asynchronously.
        /// </summary>
        public static async Task StopServer() => AdbServer.Instance.Stop();

        /// <summary>
        /// Kills the ADB server asynchronously.
        /// </summary>
        public static void KillServer()
        {
            RunAdbCommand("kill-server");
            StopServer();
        }
        
        public static List<AdbDevice> GetDevices() {
            List<AdbDevice> devices = new List<AdbDevice>();
            string[] d = adbS("devices -l", false).Split("\n");
            foreach (string l in d)
            {
                if (l.StartsWith("List of")) continue;
                string[] options = l.Split(' ');
                if (options[0].Trim() == "") continue;
                AdbDevice device = new AdbDevice();
                device.id = options[0];
                foreach(string o in options)
                {
                    string[] p = o.Split(":");
                    if (p[0] == "model")
                    {
                        device.name = p[1];
                        break;
                    }
                }
                devices.Add(device);
            }
            return devices;
        }

        /// <summary>
        /// Runs an ADB command asynchronously.
        /// </summary>
        /// <param name="arguments">The arguments to pass to adb.</param>
        /// <returns>An ExitInfo object containing the exit code, error message, and output of the command.</returns>
        public static ExitInfo RunAdbCommand(string arguments, AdbDevice? device)
        {
            StartServer();
            if(device != null) {
                arguments = "-s \"" + device.id + "\" " + arguments;
            }

            var procStartInfo = new ProcessStartInfo(AdbPath)
            {
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                Arguments = arguments
            };
            Logger.Log("Running adb " + arguments, "AdbWrapper");
            var proc = Process.Start(procStartInfo);

            if (proc == null)
            {
                throw new NullReferenceException(nameof(proc));
            }
        
            proc.WaitForExit();
            string error = "";
            try
            {
                error = proc.StandardError.ReadToEnd();
            }
            catch (Exception e)
            {
                error = "Internal QAVS error getting standard error: " + e;
            }
            string output = "";
            try
            {
                output = proc.StandardOutput.ReadToEnd();
            }
            catch (Exception e)
            {
                output = "Internal QAVS error getting standard output: " + e;
            }
            ExitInfo i = new ExitInfo()
            {
                ExitCode = proc.ExitCode,
                Error = error,
                Output = output
            };
            Logger.Log("Exit code: " + i.ExitCode + "\n Error: " + i.Error + "\n\n Output: " + i.Output, "AdbWrapper");

            return i;
        }

        /// <summary>
        /// Gets the ADB WiFi port asynchronously.
        /// </summary>
        /// <returns>The ADB WiFi port number.</returns>
        public static int GetAdbWiFiPort()
        {
            var logProc = Process.Start(new ProcessStartInfo()
            {
                FileName = "logcat",
                Arguments = "-d -s adbd -e adbwifi*",
                UseShellExecute = false,
                RedirectStandardOutput = true
            });

            if (logProc == null)
            {
                throw new NullReferenceException(nameof(logProc));
            }

            logProc.WaitForExit();

            var output = logProc.StandardOutput.ReadToEnd();
            var matches = Regex.Matches(output, "adbwifi started on port (\\d+)");

            if (matches.Count > 0)
            {
                return int.Parse(matches.Last().Groups[1].Value);
            }

            return 0;
        }

        /// <summary>
        /// Enables ADB over WiFi asynchronously.
        /// </summary>
        /// <param name="cycle">Whether to cycle the ADB WiFi state.  If you need to get the port number, you probably want this to be true as it will disable and enable wireless debugging causing the port to be advertised in the log.</param>
        /// <returns>The ADB WiFi port number.</returns>
        public static int EnableAdbWiFi(bool cycle = false)
        {
            if (cycle && AdbWifiState == AdbWifiState.Enabled)
            {
                AdbWifiState = AdbWifiState.Disabled;
                Thread.Sleep(100);
            }

            AdbWifiState = AdbWifiState.Enabled;
            Thread.Sleep(100);

            return GetAdbWiFiPort();
        }

        /// <summary>
        /// Disables ADB over WiFi asynchronously.
        /// </summary>
        public static void DisableAdbWiFi()
        {
            AdbWifiState = AdbWifiState.Disabled;
            Thread.Sleep(100);
        }

        /// <summary>
        /// Connects to an ADB device over WiFi asynchronously.
        /// </summary>
        /// <param name="host">The host of the ADB device.</param>
        /// <param name="port">The port to connect to (default is 5555).</param>
        /// <returns>The connected host.</returns>
        public static string Connect(string host, int port = 5555)
        {
            StartServer();

            var output = RunAdbCommand($"connect {host}:{port}");
            var match = Regex.Match(output.Output, "^connected to (.*)$", RegexOptions.Multiline);

            if (output.ExitCode != 0 || !match.Success)
            {
                throw new AdbException(output.Output);
            }

            return match.Groups[1].Value.Trim();
        }

        /// <summary>
        /// Disconnects ADB devices asynchronously. If no device identifier is specified, all devices will disconnect.
        /// </summary>
        /// <param name="device">The device identifier (optional).</param>
        /// <returns>True if all devices were disconnected; otherwise, false.</returns>
        public static bool Disconnect(string? device = null)
        {
            StartServer();

            if (device == null)
            {
                return (RunAdbCommand("disconnect")).Output.Contains("disconnected everything");
            }
            else
            {
                return (RunAdbCommand("disconnect " + device)).Output.Contains("disconnected ");
            }
        }

        /// <summary>
        /// Gets the list of connected ADB devices asynchronously.
        /// </summary>
        /// <returns>An array of device identifiers.</returns>
        public static string[] GetDevices()
        {
            StartServer();

            var output = RunAdbCommand("devices");

            if (output.ExitCode != 0 || !output.Output.Contains("List of devices attached"))
            {
                throw new AdbException(output.Output);
            }

            var matches = Regex.Matches(output.Output, "^(.*?)\\tdevice$", RegexOptions.Multiline);

            return matches.Select(match => match.Groups[1].Value.Trim()).ToArray();
        }

        /// <summary>
        /// Grants necessary permissions to an ADB device asynchronously.
        /// </summary>
        /// <param name="device">The device identifier.</param>
        public static void GrantPermissions(string device)
        {
            StartServer();

            RunAdbCommand("-s " + device + " shell pm grant " + Application.Context.PackageName + " android.permission.WRITE_SECURE_SETTINGS");
            RunAdbCommand("-s " + device + " shell pm grant " + Application.Context.PackageName + " android.permission.READ_LOGS");
        }

        /// <summary>
        /// Grants necessary permissions to all connected ADB devices asynchronously.
        /// </summary>
        public static void GrantPermissions()
        {
            StartServer();

            var devices = GetDevices();

            foreach (var device in devices)
            {
                GrantPermissions(device);
            }
        }

        /// <summary>
        /// Sets ADB to TCP/IP mode asynchronously.
        /// </summary>
        /// <param name="device">The device identifier (optional).</param>
        /// <param name="port">The port to use (default is 5555).</param>
        public static void TcpIpMode(string? device = null, int port = 5555)
        {
            StartServer();
                
            if (device == null)
            {
                RunAdbCommand("tcpip " + port.ToString());
            }
            else
            {
                RunAdbCommand("-s " + device + " tcpip" + port.ToString());
                Disconnect(device);
            }

            Thread.Sleep(100);
        }

        /// <summary>
        /// Sets ADB to TCP/IP mode asynchronously.
        /// </summary>
        /// <param name="port">The port to use (default is 5555).</param>
        public static void TcpIpMode(int port = 5555) => TcpIpMode(null, port);

        /// <summary>
        /// Sets ADB to USB mode asynchronously.
        /// </summary>
        /// <param name="device">The device identifier (optional).</param>
        public static void UsbMode(string? device = null)
        {
            StartServer();

            if (device == null)
            {
                RunAdbCommand($"usb");
            }
            else
            {
                RunAdbCommand("-s " + device + " usb");
                Disconnect(device);
            }
        }

        /// <summary>
        /// Executes an ADB shell command and returns the output.
        /// </summary>
        /// <param name="device">The device identifier (optional).</param>
        /// <param name="command">The command and arguments to run.</param>
        /// <returns></returns>
        public static ExitInfo RunShellCommand(string? device = null, params string[] command)
        {
            var arguments = new List<string>();

            if (device != null)
            {
                arguments.Add("-s");
                arguments.Add(device);
            }

            arguments.Add("shell");
            arguments.AddRange(command);

            return RunAdbCommand(String.Join(' ', arguments.ToArray()));
        }
    }
    
    
    public class AdbDevice
    {
        public string id { get; set; } = "";
        public string name { get; set; } = "";

        public AdbDevice(string id, string name)
        {
            this.id = id;
            this.name = name;
        }

        public AdbDevice() { }

        public override string ToString()
        {
            return id + ": " + name;
        }
    }
}
