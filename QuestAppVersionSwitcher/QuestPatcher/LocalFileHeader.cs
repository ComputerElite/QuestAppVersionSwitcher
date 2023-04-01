using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuestPatcher.Core.Apk
{
    public class LocalFileHeader
    {

        public static readonly int SIGNATURE = 0x04034b50;

        public short VersionNeeded { get; set; }
        public short GeneralPurposeFlag { get; set; }
        public short CompressionMethod { get; set; }
        public short FileLastModificationTime { get; set; }
        public short FileLastModificationDate { get; set; }
        public int CRC32 { get; set; }
        public int CompressedSize { get; set; }
        public int UncompressedSize { get; set; }
        public string FileName { get; set; }
        public byte[] ExtraField { get; set; }

        public LocalFileHeader(FileMemory memory)
        {
            int signature = memory.ReadInt();
            if(signature != SIGNATURE)
                throw new Exception("Invalid LocalFileHeader signature " + signature.ToString("X4"));
            VersionNeeded = memory.ReadShort();
            GeneralPurposeFlag = memory.ReadShort();
            CompressionMethod = memory.ReadShort();
            FileLastModificationTime = memory.ReadShort();
            FileLastModificationDate = memory.ReadShort();
            CRC32 = memory.ReadInt();
            CompressedSize = memory.ReadInt();
            UncompressedSize = memory.ReadInt();
            var fileNameLength = memory.ReadShort();
            var extraFieldLength = memory.ReadShort();
            FileName = memory.ReadString(fileNameLength);
            ExtraField = memory.ReadBytes(extraFieldLength);
        }
        
        public LocalFileHeader(Stream memory)
        {
            int signature = StreamReaderExtension.ReadInt(memory);
            if(signature != SIGNATURE)
                throw new Exception("Invalid LocalFileHeader signature " + signature.ToString("X4"));
            VersionNeeded = StreamReaderExtension.ReadShort(memory);
            GeneralPurposeFlag = StreamReaderExtension.ReadShort(memory);
            CompressionMethod = StreamReaderExtension.ReadShort(memory);
            FileLastModificationTime = StreamReaderExtension.ReadShort(memory);
            FileLastModificationDate = StreamReaderExtension.ReadShort(memory);
            CRC32 = StreamReaderExtension.ReadInt(memory);
            CompressedSize = StreamReaderExtension.ReadInt(memory);
            UncompressedSize = StreamReaderExtension.ReadInt(memory);
            var fileNameLength = StreamReaderExtension.ReadShort(memory);
            var extraFieldLength = StreamReaderExtension.ReadShort(memory);
            FileName =StreamReaderExtension.ReadString(memory, fileNameLength);
            ExtraField = StreamReaderExtension.ReadBytes(memory, extraFieldLength);
        }

        public void Write(FileMemory memory)
        {
            memory.WriteInt(SIGNATURE);
            memory.WriteShort(VersionNeeded);
            memory.WriteShort(GeneralPurposeFlag);
            memory.WriteShort(CompressionMethod);
            memory.WriteShort(FileLastModificationTime);
            memory.WriteShort(FileLastModificationDate);
            memory.WriteInt(CRC32);
            memory.WriteInt(CompressedSize);
            memory.WriteInt(UncompressedSize);
            memory.WriteShort((short)FileMemory.StringLength(FileName));
            memory.WriteShort((short)ExtraField.Length);
            memory.WriteString(FileName);
            memory.WriteBytes(ExtraField);
        }
        
        public void Write(Stream memory)
        {
            StreamWriterExtension.WriteInt(memory, SIGNATURE);
            StreamWriterExtension.WriteShort(memory, VersionNeeded);
            StreamWriterExtension.WriteShort(memory, GeneralPurposeFlag);
            StreamWriterExtension.WriteShort(memory, CompressionMethod);
            StreamWriterExtension.WriteShort(memory, FileLastModificationTime);
            StreamWriterExtension.WriteShort(memory, FileLastModificationDate);
            StreamWriterExtension.WriteShort(memory, VersionNeeded);
            StreamWriterExtension.WriteInt(memory, CRC32);
            StreamWriterExtension.WriteInt(memory, CompressedSize);
            StreamWriterExtension.WriteInt(memory, UncompressedSize);
            
            StreamWriterExtension.WriteShort(memory, (short)FileMemory.StringLength(FileName));
            StreamWriterExtension.WriteShort(memory, (short)ExtraField.Length);
            StreamWriterExtension.WriteString(memory, FileName);
            StreamWriterExtension.WriteBytes(memory, ExtraField);

        }

    }
}