using System;
using Android.Content.PM;
using AndroidX.Core.App;
using ComputerUtils.Android;
using ComputerUtils.Android.Logging;

namespace DanTheMan827.OnDeviceADB
{
    public class QAVSAdbInteractor
    {
        public static AdbDevice device;
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
                Logger.Log("Found " + devices.Count + " adb devices.", "AdbServer");

                // If no devices are connected, enable ADB over WiFi.
                if (devices.Count == 0)
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
                        QAVSAdbInteractor.device = device;
                    }

                    // Restore the saved wireless debugging state.
                    AdbWrapper.AdbWifiState = adbWifiState;
                } else {
                    QAVSAdbInteractor.device = devices[0];
                }

                // Grant necessary permissions to all connected devices.
                AdbWrapper.GrantPermissions();
            }
        }

        public static bool TryConnect()
        {
            Logger.Log("Trying to enable wireless adb");
            bool hasPermission = ActivityCompat.CheckSelfPermission(AndroidCore.context, "android.permission.WRITE_SECURE_SETTINGS") == Permission.Granted;
            if (!hasPermission)
            {
                Logger.Log("Missing permission: android.permission.WRITE_SECURE_SETTINGS");
                return false;
            }
            
            try
            {
                AdbWrapper.KillServer();
                var port = AdbWrapper.EnableAdbWiFi(true);

                // If the port is above 0 we were successful.
                if (port > 0)
                {
                    Logger.Log("Found adb port in log, connecting: " + port, "ADB Wrapper");
                    // Connect to the loopback IP on the detected port.
                    var device = AdbWrapper.Connect("127.0.0.1", port);
                    return true;
                }
                else
                {
                    Logger.Log("Failed to find adb port in log", "ADB Wrapper");
                }
            } catch (Exception e)
            {
                Logger.Log("Failed to enable wireless adb: " + e.Message);
            }

            return false;
        }

        public static void Connect()
        {
            
        }
    }
}