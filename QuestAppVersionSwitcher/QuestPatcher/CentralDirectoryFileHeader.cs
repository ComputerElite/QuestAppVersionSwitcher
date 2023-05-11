using System;
using System.Collections.Generic;
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

        public async Task Populate(FileMemory memory)
        {
            int signature = await memory.ReadInt();
            if(signature != SIGNATURE)
                throw new Exception("Invalid CentralDirectoryFileHeader signature " + signature.ToString("X4"));
            VersionMadeBy = await memory.ReadShort();
            VersionNeeded = await memory.ReadShort();
            GeneralPurposeFlag = await memory.ReadShort();
            CompressionMethod = await memory.ReadShort();
            FileLastModificationTime = await memory.ReadShort();
            FileLastModificationDate = await memory.ReadShort();
            CRC32 = await memory.ReadInt();
            CompressedSize = await memory.ReadInt();
            UncompressedSize = await memory.ReadInt();
            var fileNameLength = await memory.ReadShort();
            var extraFieldLength = await memory.ReadShort();
            var fileCommentLength = await memory.ReadShort();
            DiskNumberFileStart = await memory.ReadShort();
            InternalFileAttributes = await memory.ReadShort();
            ExternalFileAttributes = await memory.ReadInt();
            Offset = await memory.ReadInt();
            FileName = await memory.ReadString(fileNameLength);
            ExtraField = await memory.ReadBytes(extraFieldLength);
            FileComment = await memory.ReadString(fileCommentLength);
        }

        public async Task Write(FileMemory memory)
        {
            await memory.WriteInt(SIGNATURE);
            await memory.WriteShort(VersionMadeBy);
            await memory.WriteShort(VersionNeeded);
            await memory.WriteShort(GeneralPurposeFlag);
            await memory.WriteShort(CompressionMethod);
            await memory.WriteShort(FileLastModificationTime);
            await memory.WriteShort(FileLastModificationDate);
            await memory.WriteInt(CRC32);
            await memory.WriteInt(CompressedSize);
            await memory.WriteInt(UncompressedSize);
            await memory.WriteShort((short)FileMemory.StringLength(FileName));
            await memory.WriteShort((short)ExtraField.Length);
            await memory.WriteShort((short) FileMemory.StringLength(FileComment));
            await memory.WriteShort(DiskNumberFileStart);
            await memory.WriteShort(InternalFileAttributes);
            await memory.WriteInt(ExternalFileAttributes);
            await memory.WriteInt(Offset);
            await memory.WriteString(FileName);
            await memory.WriteBytes(ExtraField);
            await memory.WriteString(FileComment);
        }
    }
}