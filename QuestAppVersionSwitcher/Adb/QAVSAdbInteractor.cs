using System;
using Android.Content.PM;
using AndroidX.Core.App;
using ComputerUtils.Android;
using ComputerUtils.Android.Logging;

namespace DanTheMan827.OnDeviceADB
{
    public class QAVSAdbInteractor
    {
        public static string device;
        private static bool _hasInitialized = false;
        
        /// <summary>
        /// Checks if we have our needed permissions by trying to set the Wi-Fi debugging state
        /// </summary>
        /// <returns></returns>
        private bool HasPermission()
        {
            try
            {
                AdbWrapper.AdbWifiState = AdbWrapper.AdbWifiState;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        
        public void Initialize()
        {
            if (!_hasInitialized)
            {
                _hasInitialized = true;

                // Kill any existing ADB server instance.
                AdbWrapper.KillServerAsync().Wait();

                // Start a new ADB server instance.
                AdbWrapper.StartServerAsync().Wait();

                // Get the list of connected devices.
                var devices = AdbWrapper.GetDevicesAsync().Result;

                // If no devices are connected, enable ADB over WiFi.
                if (devices.Length == 0)
                {
                    // Store the current wireless debugging state.
                    var adbWifiState = AdbWrapper.AdbWifiState;

                    // Enable wireless debugging while cycling it to ensure we go from a disabled state to enabled.
                    var port = AdbWrapper.EnableAdbWiFiAsync(true).Result;

                    // If the port is above 0 we were successful.
                    if (port > 0)
                    {
                        // Disconnect all devices.
                        AdbWrapper.DisconnectAsync().Wait();

                        // Connect to the loopback IP on the detected port.
                        device = AdbWrapper.ConnectAsync("127.0.0.1", port).Result;
                    }

                    // Restore the saved wireless debugging state.
                    AdbWrapper.AdbWifiState = adbWifiState;
                }

                if (!HasPermission())
                {
                    // Grant necessary permissions to all connected devices (This may force quit the app!).
                    AdbWrapper.GrantPermissionsAsync().Wait();
                }
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
                AdbWrapper.KillServerAsync().Wait();
                var port = AdbWrapper.EnableAdbWiFiAsync(true).Result;

                // If the port is above 0 we were successful.
                if (port > 0)
                {
                    Logger.Log("Found adb port in log, connecting: " + port, "ADB Wrapper");
                    // Connect to the loopback IP on the detected port.
                    var device = AdbWrapper.ConnectAsync("127.0.0.1", port).Result;
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
    }
}