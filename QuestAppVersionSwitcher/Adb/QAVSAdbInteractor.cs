using System;
using ComputerUtils.Android.Logging;

namespace DanTheMan827.OnDeviceADB
{
    public class QAVSAdbInteractor
    {
        private static bool _hasInitialized = false;
        public static void Initialize()
        {
            if (!_hasInitialized)
            {
                _hasInitialized = true;
                AdbServer.Instance = new AdbServer();

                // Kill any existing ADB server instance.
                AdbWrapper.KillServer();

                // Start a new ADB server instance.
                AdbWrapper.StartServer();

                // Get the list of connected devices.
                var devices = AdbWrapper.GetDevices();
                Logger.Log("Found " + devices.Length + " adb devices.", "AdbServer");

                // If no devices are connected, enable ADB over WiFi.
                if (devices.Length == 0)
                {
                    // Store the current wireless debugging state.
                    var adbWifiState = AdbWrapper.AdbWifiState;

                    // Enable wireless debugging while cycling it to ensure we go from a disabled state to enabled.
                    var port = AdbWrapper.EnableAdbWiFi(true);

                    // If the port is above 0 we were successful.
                    if (port > 0)
                    {
                        // Connect to the loopback IP on the detected port.
                        var device = AdbWrapper.Connect("127.0.0.1", port);

                        // Switch to tcpip mode.
                        AdbWrapper.TcpIpMode(device);

                        // Kill the server to ensure it refreshes.
                        AdbWrapper.KillServer();

                        // Launch the server.
                        AdbWrapper.StartServer();
                    }

                    // Restore the saved wireless debugging state.
                    AdbWrapper.AdbWifiState = adbWifiState;
                }

                // Grant necessary permissions to all connected devices.
                AdbWrapper.GrantPermissions();
            }
        }

        public static void TryConnect()
        {
            Logger.Log("Trying to enable wireless adb");
            try
            {
                var port = AdbWrapper.EnableAdbWiFi(true);

                // If the port is above 0 we were successful.
                if (port > 0)
                {
                    Logger.Log("Found adb port in log, connecting: " + port, "ADB Wrapper");
                    // Connect to the loopback IP on the detected port.
                    var device = AdbWrapper.Connect("127.0.0.1", port);
                }
                else
                {
                    Logger.Log("Failed to find adb port in log", "ADB Wrapper");
                }
            } catch (Exception e)
            {
                Logger.Log("Failed to enable wireless adb: " + e.Message);
            }
           
        }
    }
}