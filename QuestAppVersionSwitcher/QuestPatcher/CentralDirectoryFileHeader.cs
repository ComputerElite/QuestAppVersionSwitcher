using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuestPatcher.Core.Apk
{
    public class CentralDirectoryFileHeader
    {
        public static readonly int SIGNATURE = 0x02014b50;
        public short VersionMadeBy { get; set; }
        public short VersionNeeded { get; set; }
        public short GeneralPurposeFlag { get; set; }
        public short CompressionMethod { get; set; }
        public short FileLastModificationTime { get; set; }
        public short FileLastModificationDate { get; set; }
        public int CRC32 { get; set; }
        public int CompressedSize { get; set; }
        public int UncompressedSize { get; set; }
        public short DiskNumberFileStart { get; set; }
        public short InternalFileAttributes { get; set; }
        public int ExternalFileAttributes { get; set; }
        public int Offset { get; set; }
        public string FileName { get; set; }
        public byte[] ExtraField { get; set; }
        public string FileComment { get; set; }

        public CentralDirectoryFileHeader(FileMemory memory)
        {
            int signature = memory.ReadInt();
            if(signature != SIGNATURE)
                throw new Exception("Invalid CentralDirectoryFileHeader signature " + signature.ToString("X4"));
            VersionMadeBy = memory.ReadShort();
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
            var fileCommentLength = memory.ReadShort();
            DiskNumberFileStart = memory.ReadShort();
            InternalFileAttributes = memory.ReadShort();
            ExternalFileAttributes = memory.ReadInt();
            Offset = memory.ReadInt();
            FileName = memory.ReadString(fileNameLength);
            ExtraField = memory.ReadBytes(extraFieldLength);
            FileComment = memory.ReadString(fileCommentLength);
        }
        
        public CentralDirectoryFileHeader(Stream memory)
        {
            int signature = StreamReaderExtension.ReadInt(memory);
            if(signature != SIGNATURE)
                throw new Exception("Invalid CentralDirectoryFileHeader signature " + signature.ToString("X4"));
            VersionMadeBy = StreamReaderExtension.ReadShort(memory);
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
            var fileCommentLength = StreamReaderExtension.ReadShort(memory);
            DiskNumberFileStart = StreamReaderExtension.ReadShort(memory);
            InternalFileAttributes = StreamReaderExtension.ReadShort(memory);
            ExternalFileAttributes = StreamReaderExtension.ReadInt(memory);
            Offset = StreamReaderExtension.ReadInt(memory);
            FileName = StreamReaderExtension.ReadString(memory, fileNameLength);
            ExtraField = StreamReaderExtension.ReadBytes(memory, extraFieldLength);
            FileComment = StreamReaderExtension.ReadString(memory, fileCommentLength);
        }

        public void Write(FileMemory memory)
        {
            memory.WriteInt(SIGNATURE);
            memory.WriteShort(VersionMadeBy);
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
            memory.WriteShort((short) FileMemory.StringLength(FileComment));
            memory.WriteShort(DiskNumberFileStart);
            memory.WriteShort(InternalFileAttributes);
            memory.WriteInt(ExternalFileAttributes);
            memory.WriteInt(Offset);
            memory.WriteString(FileName);
            memory.WriteBytes(ExtraField);
            memory.WriteString(FileComment);
        }
        
        public void Write(Stream memory)
        {
            StreamWriterExtension.WriteInt(memory, SIGNATURE);
            StreamWriterExtension.WriteShort(memory, VersionMadeBy);
            StreamWriterExtension.WriteShort(memory, VersionNeeded);
            StreamWriterExtension.WriteShort(memory, GeneralPurposeFlag);
            StreamWriterExtension.WriteShort(memory, CompressionMethod);
            StreamWriterExtension.WriteShort(memory, FileLastModificationTime);
            StreamWriterExtension.WriteShort(memory, FileLastModificationDate);
            StreamWriterExtension.WriteInt(memory, CRC32);
            StreamWriterExtension.WriteInt(memory, CompressedSize);
            StreamWriterExtension.WriteInt(memory, UncompressedSize);
            StreamWriterExtension.WriteShort(memory, (short)FileMemory.StringLength(FileName));
            StreamWriterExtension.WriteShort(memory, (short)ExtraField.Length);
            StreamWriterExtension.WriteShort(memory, (short) FileMemory.StringLength(FileComment));
            StreamWriterExtension.WriteShort(memory, DiskNumberFileStart);
            StreamWriterExtension.WriteShort(memory, InternalFileAttributes);
            StreamWriterExtension.WriteInt(memory, ExternalFileAttributes);
            StreamWriterExtension.WriteInt(memory, Offset);
            StreamWriterExtension.WriteString(memory, FileName);
            StreamWriterExtension.WriteBytes(memory, ExtraField);
            StreamWriterExtension.WriteString(memory, FileComment);
        }
    }
}