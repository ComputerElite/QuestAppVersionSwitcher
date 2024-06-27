using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ComputerUtils.Android;
using ComputerUtils.Android.Logging;

namespace QuestAppVersionSwitcher.Adb
{
     
    /// <summary>
    /// A class for managing the adb server.
    /// </summary>
    public class AdbServer : IDisposable
    {
        private static string? FilesDir => AndroidCore.context?.FilesDir?.Path;
        private static string? CacheDir => AndroidCore.context?.CacheDir?.Path;
        private static string? NativeLibsDir => AndroidCore.context.ApplicationInfo?.NativeLibraryDir;
        private CancellationTokenSource? CancelToken { get; set; }
        private Process? ServerProcess { get; set; }

        /// <summary>
        /// Path to the adb binary.
        /// </summary>
        public static string? AdbPath => NativeLibsDir != null ? Path.Combine(NativeLibsDir, "libadb.so") : null;

        /// <summary>
        /// If the server is running
        /// </summary>
        public bool IsRunning => ServerProcess != null && !ServerProcess.HasExited;

        public AdbServer()
        {
            Debug.Assert(FilesDir != null);
            Debug.Assert(CacheDir != null);
            Debug.Assert(NativeLibsDir != null);
            Debug.Assert(AdbPath != null);
        }
        private void StartServer()
        {
            Thread t = new Thread(() =>
            {
                // Asserts
                Debug.Assert(this.ServerProcess == null);
                Debug.Assert(this.CancelToken == null);

                // Create and configure the ProcessStartInfo.
                var adbInfo = new ProcessStartInfo(AdbPath, "pair localhost");
                adbInfo.WorkingDirectory = FilesDir;
                adbInfo.UseShellExecute = false;
                adbInfo.RedirectStandardOutput = true;
                adbInfo.RedirectStandardError = true;
                adbInfo.EnvironmentVariables["HOME"] = FilesDir;
                adbInfo.EnvironmentVariables["TMPDIR"] = CacheDir;

                // Start the process
                Logger.Log("Starting adb server process from " + adbInfo.FileName);
                ServerProcess = Process.Start(adbInfo);

                if (ServerProcess == null)
                {
                    Logger.Log("Adb server failed to start", LoggingType.Error);
                }

                // Dispose any token source that may exist (there shouldn't be any)
                CancelToken?.Dispose();

                // Wait for the server to exit
                while (!ServerProcess.StandardError.EndOfStream)
                {
                    Logger.Log(ServerProcess.StandardError.ReadLine());
                }
                while (!ServerProcess.StandardOutput.EndOfStream)
                {
                    Logger.Log(ServerProcess.StandardOutput.ReadLine());
                }
                ServerProcess.WaitForExit(); 
                // Log standard output and error
                Logger.Log("Adb server exited", LoggingType.Error);

                // Dispose our variables.
                DisposeVariables(false);
            });
            t.Start();
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
        public void Start()
        {
            if (!IsRunning)
            {
                StartServer();
            }
        }

        /// <summary>
        /// Stops the server if running.
        /// </summary>
        public void Stop() => DisposeVariables(true);

        public void Dispose() => Stop();
    }
}