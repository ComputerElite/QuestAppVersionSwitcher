using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuestPatcher.Core.Apk
{
    public class FileMemory : IDisposable, IAsyncDisposable
    {
        public Stream Stream { get; private set; }

        public long Position
        {
            get { return Stream.Position; }
            set { Stream.Position = value; }
        }

        public FileMemory(Stream stream)
        {
            this.Stream = stream;
        }

        public void Dispose()
        {
            Stream.Dispose();
        }

        public ValueTask DisposeAsync()
        {
            return Stream.DisposeAsync();
        }

        public long Length()
        {
            return Stream.Length;
        }

        public async Task WriteBytes(byte[] bytes)
        {
            await Stream.WriteAsync(bytes, 0, bytes.Length);
        }

        public async Task<byte[]> ReadBytes(int count)
        {
            byte[] bytes = new byte[count];
            int read = await Stream.ReadAsync(bytes, 0, count);
            return bytes;
        }

        public async Task<short> ReadShort()
        {
            return BitConverter.ToInt16(await ReadBytes(2), 0);
        }

        public async Task WriteShort(short value)
        {
            await WriteBytes(BitConverter.GetBytes(value));
        }

        public async Task<int> ReadInt()
        {
            return BitConverter.ToInt32(await ReadBytes(4), 0);
        }

        public async Task WriteInt(int value)
        {
            await WriteBytes(BitConverter.GetBytes(value));
        }

        public async Task<uint> ReadUInt()
        {
            return BitConverter.ToUInt32(await ReadBytes(4), 0);
        }

        public async Task WriteUInt(uint value)
        {
            await WriteBytes(BitConverter.GetBytes(value));
        }

        public async Task<long> ReadLong()
        {
            return BitConverter.ToInt64(await ReadBytes(8), 0);
        }

        public async Task WriteLong(long value)
        {
            await WriteBytes(BitConverter.GetBytes(value));
        }

        public async Task<ulong> ReadULong()
        {
            return BitConverter.ToUInt64(await ReadBytes(8), 0);
        }

        public async Task WriteULong(ulong value)
        {
            await WriteBytes(BitConverter.GetBytes(value));
        }

        public async Task<string> ReadString(int count)
        {
            return Encoding.UTF8.GetString(await ReadBytes(count));
        }

        public async Task WriteString(string value)
        {
            await WriteBytes(Encoding.UTF8.GetBytes(value));
        }

        public static int StringLength(string value)
        {
            return Encoding.UTF8.GetBytes(value).Length;
        }
    }
}