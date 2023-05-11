using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuestPatcher.Core.Apk
{
    public class APKSigningBlock
    {
        public class IDValuePair
        {

            public uint ID { get; private set; }
            public int Value { get; private set; }
            public byte[]? Data { get; private set; }

            public IDValuePair(uint id, int value)
            {
                ID = id;
                Value = value;
                Data = null;
            }

            public IDValuePair(uint id, byte[] value)
            {
                ID = id;
                Value = value.Length;
                Data = value;
            }

            public int Length()
            {
                return 8 + 4 + Data?.Length ?? 4;
            }

            public async Task Write(FileMemory memory)
            {
                await memory.WriteULong((ulong) Length() - 8);
                await memory.WriteUInt(ID);
                if(Data == null)
                {
                    await memory.WriteInt(Value);
                } else
                {
                    await memory.WriteBytes(Data);
                }
            }

        }

        public static readonly string MAGIC = "APK Sig Block 42";

        public List<IDValuePair> Values { get; private set; }

        public APKSigningBlock()
        {
            Values = new List<IDValuePair>();
        }

        public async Task Write(FileMemory memory)
        {
            ulong size = (ulong) Values.Sum(values => values.Length()) + 8 + 16;
            await memory.WriteULong(size);
            foreach (IDValuePair value in Values)
            {
                await value.Write(memory);
            }
            await memory.WriteULong(size);
            await memory.WriteString(MAGIC);
        }

    }
}