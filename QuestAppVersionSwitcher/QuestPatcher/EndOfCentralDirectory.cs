using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuestPatcher.Core.Apk
{
    public class EndOfCentralDirectory
    {

        public static readonly int SIGNATURE = 0x06054b50;
        public short NumberOfDisk { get; set; }
        public short CDStartDisk { get; set; }
        public short NumberOfCDsOnDisk { get; set; }
        public short NumberOfCDs { get; set; }
        public int SizeOfCD { get; set; }
        public int OffsetOfCD { get; set; }
        public string Comment { get; set; }

        public EndOfCentralDirectory(FileMemory memory)
        {
            int signature = memory.ReadInt();
            if(signature != SIGNATURE)
                throw new Exception("Invalid EndOfCentralDirectory signature " + signature.ToString("X4"));
            NumberOfDisk = memory.ReadShort();
            CDStartDisk = memory.ReadShort();
            NumberOfCDsOnDisk = memory.ReadShort();
            NumberOfCDs = memory.ReadShort();
            SizeOfCD = memory.ReadInt();
            OffsetOfCD = memory.ReadInt();
            var commentLength = memory.ReadShort();
            Comment = memory.ReadString(commentLength);
        }
        
        public EndOfCentralDirectory(Stream memory)
        {
            int signature = StreamReaderExtension.ReadInt(memory);
            if(signature != SIGNATURE)
                throw new Exception("Invalid EndOfCentralDirectory signature " + signature.ToString("X4"));
            NumberOfDisk = StreamReaderExtension.ReadShort(memory);
            CDStartDisk = StreamReaderExtension.ReadShort(memory);
            NumberOfCDsOnDisk = StreamReaderExtension.ReadShort(memory);
            NumberOfCDs = StreamReaderExtension.ReadShort(memory);
            SizeOfCD = StreamReaderExtension.ReadInt(memory);
            OffsetOfCD = StreamReaderExtension.ReadInt(memory);
            var commentLength = StreamReaderExtension.ReadShort(memory);
            Comment = StreamReaderExtension.ReadString(memory, commentLength);
        }

        public void Write(FileMemory memory)
        {
            memory.WriteInt(SIGNATURE);
            memory.WriteShort(NumberOfDisk);
            memory.WriteShort(CDStartDisk);
            memory.WriteShort(NumberOfCDsOnDisk);
            memory.WriteShort(NumberOfCDs);
            memory.WriteInt(SizeOfCD);
            memory.WriteInt(OffsetOfCD);
            memory.WriteShort((short)FileMemory.StringLength(Comment));
            memory.WriteString(Comment);
        }
        
        public void Write(Stream memory)
        {
            StreamWriterExtension.WriteInt(memory, SIGNATURE);
            StreamWriterExtension.WriteShort(memory, NumberOfDisk);
            StreamWriterExtension.WriteShort(memory, CDStartDisk);
            StreamWriterExtension.WriteShort(memory, NumberOfCDsOnDisk);
            StreamWriterExtension.WriteShort(memory, NumberOfCDs);
            StreamWriterExtension.WriteInt(memory, SizeOfCD);
            StreamWriterExtension.WriteInt(memory, OffsetOfCD);
            StreamWriterExtension.WriteShort(memory, (short)FileMemory.StringLength(Comment));
            StreamWriterExtension.WriteString(memory, Comment);
        }

    }

    public class StreamReaderExtension
    {
        

        public static short ReadShort(Stream r)
        {
            byte[] buffer = new byte[2];
            r.Read(buffer, 0, 2);
            return BitConverter.ToInt16(buffer, 0);
        }

        public static string ReadString(Stream r, int length)
        {
            byte[] buffer = new byte[length];
            r.Read(buffer, 0, length);
            return Encoding.UTF8.GetString(buffer);
        }

        public static byte[] ReadBytes(Stream r, short length)
        {
            byte[] buffer = new byte[length];
            r.Read(buffer, 0, length);
            return buffer;
        }
        
        public static byte[] ReadBytes(Stream r, int length)
        {
            byte[] buffer = new byte[length];
            r.Read(buffer, 0, length);
            return buffer;
        }
        
        public static int ReadInt(Stream r)
        {
            return BitConverter.ToInt32(ReadBytes(r, 4), 0);
        }

        public static ulong ReadULong(Stream fs)
        {
            return BitConverter.ToUInt64(ReadBytes(fs, 8), 0);
        }
    }

    public class StreamWriterExtension
    {
        public static void WriteBytes(Stream w, byte[] bytes)
        {
            w.Write(bytes, 0, bytes.Length);
        }
        
        public static void WriteString(Stream w, string s) {
            WriteBytes(w, Encoding.UTF8.GetBytes(s));
        }
        
        public static void WriteShort(Stream w, short s) {
            WriteBytes(w, BitConverter.GetBytes(s));
        }
        
        public static void WriteInt(Stream w, int value)
        {
            WriteBytes(w, BitConverter.GetBytes(value));
        }

        public static void WriteULong(Stream w, ulong value)
        {
            WriteBytes(w, BitConverter.GetBytes(value));
        }
        
        public static void WriteUInt(Stream w, uint value)
        {
            WriteBytes(w, BitConverter.GetBytes(value));
        }
    }
}