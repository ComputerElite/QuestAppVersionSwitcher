using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ComputerUtils.Android.Logging;

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

        public async Task Populate(FileMemory memory)
        {
            int signature = await memory.ReadInt();
            if (signature != SIGNATURE)
            {
                Logger.Log("Invalid EndOfCentralDirectory signature " + signature.ToString("X4") + ". APK may be corrupted", LoggingType.Error);
                throw new Exception("Invalid EndOfCentralDirectory signature " + signature.ToString("X4") + ". APK may be corrupted");
            }
            NumberOfDisk = await memory.ReadShort();
            CDStartDisk = await memory.ReadShort();
            NumberOfCDsOnDisk = await memory.ReadShort();
            NumberOfCDs = await memory.ReadShort();
            SizeOfCD = await memory.ReadInt();
            OffsetOfCD = await memory.ReadInt();
            var commentLength = await memory.ReadShort();
            Comment = await memory.ReadString(commentLength);
        }

        public async Task Write(FileMemory memory)
        {
            await memory.WriteInt(SIGNATURE);
            await memory.WriteShort(NumberOfDisk);
            await memory.WriteShort(CDStartDisk);
            await memory.WriteShort(NumberOfCDsOnDisk);
            await memory.WriteShort(NumberOfCDs);
            await memory.WriteInt(SizeOfCD);
            await memory.WriteInt(OffsetOfCD);
            await memory.WriteShort((short)FileMemory.StringLength(Comment));
            await memory.WriteString(Comment);
        }

    }
}