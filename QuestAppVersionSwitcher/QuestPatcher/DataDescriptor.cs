using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Java.IO;

namespace QuestPatcher.Core.Apk
{
    public class DataDescriptor
    {
        public static readonly int SIGNATURE = 0x08074b50;

        public int CRC32 { get; set; }
        public int CompressedSize { get; set; }
        public int UncompressedSize { get; set; }

        public DataDescriptor(FileMemory memory)
        {
            int signature = memory.ReadInt();
            if(signature != SIGNATURE)
                throw new Exception("Invalid DataDescriptor signature " + signature.ToString("X4"));
            CRC32 = memory.ReadInt();
            CompressedSize = memory.ReadInt();
            UncompressedSize = memory.ReadInt();
        }
        
        public DataDescriptor(Stream memory)
        {
            int signature = StreamReaderExtension.ReadInt(memory);
            if(signature != SIGNATURE)
                throw new Exception("Invalid DataDescriptor signature " + signature.ToString("X4"));
            CRC32 = StreamReaderExtension.ReadInt(memory);
            CompressedSize = StreamReaderExtension.ReadInt(memory);
            UncompressedSize = StreamReaderExtension.ReadInt(memory);
        }

        public void Write(FileMemory memory)
        {
            memory.WriteInt(SIGNATURE);
            memory.WriteInt(CRC32);
            memory.WriteInt(CompressedSize);
            memory.WriteInt(UncompressedSize);
        }
        
        public void Write(Stream memory)
        {
            StreamWriterExtension.WriteInt(memory, SIGNATURE);
            StreamWriterExtension.WriteInt(memory, CRC32);
            StreamWriterExtension.WriteInt(memory, CompressedSize);
            StreamWriterExtension.WriteInt(memory, UncompressedSize);
        }

    }
}