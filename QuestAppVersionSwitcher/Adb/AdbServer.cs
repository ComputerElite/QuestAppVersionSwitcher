using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Application = Android.App.Application;

namespace DanTheMan827.OnDeviceADB
{
    /// <summary>
    /// A class for managing the adb server.
    /// </summary>
    public class AdbServer : IDisposable
    {
        public static int AdbPort = 5037;
        public static AdbServer Instance { get; private set; } = new AdbServer();
        private static string FilesDir => Application.Context?.FilesDir?.Path ?? throw new Exception("Unable to determine application files path");
        private static string CacheDir => Application.Context?.CacheDir?.Path ?? throw new Exception("Unable to determine application cache path");
        private static string NativeLibsDir => Application.Context.ApplicationInfo?.NativeLibraryDir ?? throw new Exception("Unable to determine native libs path");
        private CancellationTokenSource? CancelToken { get; set; }
        private Process? ServerProcess { get; set; }

        /// <summary>
        /// Path to the adb binary.
        /// </summary>
        public static string AdbPath => NativeLibsDir != null ? Path.Combine(NativeLibsDir, "libadb.so") : throw new Exception("Unable to determine adb path");

        /// <summary>
        /// If the server is running
        /// </summary>
        public bool IsRunning => ServerProcess != null && !ServerProcess.HasExited;

        private AdbServer() { }
        private async Task StartServer()
        {
            // Asserts
            Debug.Assert(this.ServerProcess == null);
            Debug.Assert(this.CancelToken == null);

            // Create and configure the ProcessStartInfo.
            var adbInfo = new ProcessStartInfo(AdbPath, $"-P {AdbPort} server nodaemon");
            adbInfo.WorkingDirectory = FilesDir;
            adbInfo.RedirectStandardOutput = true;
            adbInfo.RedirectStandardError = true;
            adbInfo.EnvironmentVariables["HOME"] = FilesDir;
            adbInfo.EnvironmentVariables["TMPDIR"] = CacheDir;
            adbInfo.EnvironmentVariables["ADB_MDNS"] = "0";
            adbInfo.EnvironmentVariables["ADB_MDNS_AUTO_CONNECT"] = "";

            // Start the process
            ServerProcess = Process.Start(adbInfo);

            if (ServerProcess == null)
            {
                throw new Exception("adb server failed to start");
            }

            // Dispose any token source that may exist (there shouldn't be any)
            CancelToken?.Dispose();

            // Create our CancellationTokenSource and register the process kill action.
            CancelToken = new CancellationTokenSource();
            CancelToken.Token.Register(this.ServerProcess.Kill);

            // Wait for the server to exit
            ServerProcess.WaitForExit();

            // Dispose our variables.
            DisposeVariables(false);
        }

        private void DisposeVariables(bool attemptKill)
        {
            // Stop the server
            if (attemptKill && ServerProcess != null && !ServerProcess.HasExited)
            {
                ServerProcess.Kill();
            }

            // Cleanup the token and process
            ServerProcess?.Dispose();
            ServerProcess = null;

            CancelToken?.Dispose();
            CancelToken = null;
        }

        /// <summary>
        /// Starts the server if not already running.
        /// </summary>
        public async void Start()
        {
            if (!IsRunning)
            {
                await StartServer();
            }
        }

        /// <summary>
        /// Stops the server if running.
        /// </summary>
        public void Stop(bool force = true) => DisposeVariables(force);

        public void Dispose() => Stop();
    }
}