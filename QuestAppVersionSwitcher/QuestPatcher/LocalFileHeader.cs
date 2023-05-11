using System;
using System.Collections.Generic;
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

        /// <summary>
        /// Reads the local file header from the given memory.
        /// </summary>
        /// <param name="memory"></param>
        /// <exception cref="Exception"></exception>
        public async Task ReadLocalFileHeader(FileMemory memory)
        {
            int signature = await memory.ReadInt();
            if(signature != SIGNATURE)
                throw new Exception("Invalid LocalFileHeader signature " + signature.ToString("X4"));
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
            FileName = await memory.ReadString(fileNameLength);
            ExtraField = await memory.ReadBytes(extraFieldLength);
        }

        public async Task Write(FileMemory memory)
        {
            await memory.WriteInt(SIGNATURE);
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
            await memory.WriteString(FileName);
            await memory.WriteBytes(ExtraField);
        }

    }
}