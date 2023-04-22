using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ComputerUtils.Android.Logging;

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
            if (signature != SIGNATURE)
            {
                Logger.Log("Invalid DataDescriptor signature " + signature.ToString("X4") + ". While Aligning apk: It is likely that the game is pirated.");
                throw new Exception("Invalid DataDescriptor signature " + signature.ToString("X4") +
                                    ". This error may occur when your game is pirated.");
            }
            CRC32 = memory.ReadInt();
            CompressedSize = memory.ReadInt();
            UncompressedSize = memory.ReadInt();
        }

        public void Write(FileMemory memory)
        {
            memory.WriteInt(SIGNATURE);
            memory.WriteInt(CRC32);
            memory.WriteInt(CompressedSize);
            memory.WriteInt(UncompressedSize);
        }

    }
}