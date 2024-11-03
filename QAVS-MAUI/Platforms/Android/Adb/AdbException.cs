using System;

namespace DanTheMan827.OnDeviceADB
{
    internal class AdbException : Exception
    {
        public AdbException() { }
        public AdbException(string message) : base(message) { }
        public AdbException(string message, Exception innerException) : base(message, innerException) { }
    }
}
