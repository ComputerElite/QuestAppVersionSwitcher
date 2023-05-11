using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace QuestPatcher.Core.Apk
{
    public class APKSignatureSchemeV2
    {

        public class Signer
        {
            public class BlockSignedData
            {
                public class Digest
                {

                    public uint SignatureAlgorithmID { get; private set; }
                    public byte[] Data { get; private set; }

                    public Digest(uint signatureAlgorithmID, byte[] data)
                    {
                        SignatureAlgorithmID = signatureAlgorithmID;
                        Data = data;
                    }

                    public int Length()
                    {
                        return 4 + 4 + 4 + Data.Length;
                    }

                    public async Task Write(FileMemory memory)
                    {
                        await memory.WriteUInt((uint) Length() - 4);
                        await memory.WriteUInt(SignatureAlgorithmID);
                        await memory.WriteUInt((uint) Data.Length);
                        await memory.WriteBytes(Data);
                    }
                }

                public class AdditionalAttribute
                {

                    public uint ID { get; private set; }
                    public int Value { get; private set; }
                    public byte[]? Data { get; private set; }

                    public AdditionalAttribute(uint id, int value)
                    {
                        ID = id;
                        Value = value;
                        Data = null;
                    }

                    public AdditionalAttribute(uint id, byte[] value)
                    {
                        ID = id;
                        Value = value.Length;
                        Data = value;
                    }

                    public int Length()
                    {
                        return 4 + 4 + (Data?.Length ?? 4);
                    }

                    public async Task Write(FileMemory memory)
                    {
                        await memory.WriteUInt((uint) Length() - 4);
                        await memory.WriteUInt(ID);
                        if(Data == null)
                        {
                            await memory.WriteInt(Value);
                        }
                        else
                        {
                            await  memory.WriteBytes(Data);
                        }
                    }
                }

                public List<Digest> Digests { get; private set; }
                public List<byte[]> Certificates { get; private set; }
                public List<AdditionalAttribute> AdditionalAttributes { get; private set; }

                public BlockSignedData()
                {
                    Digests = new List<Digest>();
                    Certificates = new List<byte[]>();
                    AdditionalAttributes = new List<AdditionalAttribute>();
                }

                public int Length()
                {
                    return 4 + Digests.Sum(value => value.Length()) + 4 + Certificates.Count * 4 + Certificates.Sum(value => value.Length) + 4 + AdditionalAttributes.Sum(value => value.Length());
                }

                public async Task Write(FileMemory memory)
                {
                    await memory.WriteUInt((uint) Digests.Sum(value => value.Length()));
                    Digests.ForEach(value => value.Write(memory));

                    await memory.WriteUInt((uint) (Certificates.Count * 4 + Certificates.Sum(value => value.Length)));

                    foreach (byte[] c in Certificates)
                    {
                        await memory.WriteUInt((uint) c.Length);
                        await memory.WriteBytes(c);
                    }

                    await memory.WriteUInt((uint) AdditionalAttributes.Sum(value => value.Length()));
                    AdditionalAttributes.ForEach(value => value.Write(memory));
                }
            }

            public class BlockSignature
            {
                public uint SignatureAlgorithmID { get; private set; }
                public byte[] Data { get; private set; }

                public BlockSignature(uint signatureAlgorithmID, byte[] data)
                {
                    SignatureAlgorithmID = signatureAlgorithmID;
                    Data = data;
                }

                public int Length()
                {
                    return 4 + 4 + 4 + Data.Length;
                }

                public async Task Write(FileMemory memory)
                {
                    await memory.WriteUInt((uint) Length() - 4);
                    await memory.WriteUInt(SignatureAlgorithmID);
                    await memory.WriteUInt((uint) Data.Length);
                    await memory.WriteBytes(Data);
                }
            }

            public byte[]? SignedData { get; set; }
            public List<BlockSignature> Signatures { get; private set; }
            public byte[]? PublicKey { get; set; }

            public Signer() {
                SignedData = null;
                Signatures = new List<BlockSignature>();
                PublicKey = null;
            }

            public int Length()
            {
                return 4 + 4 + (SignedData?.Length ?? 0) + 4 + Signatures.Sum(value => value.Length()) + 4 + (PublicKey?.Length ?? 0);
            }

            public async Task Write(FileMemory memory)
            {
                await memory.WriteUInt((uint) Length() - 4);
                if(SignedData == null)
                {
                    await memory.WriteUInt(0);
                }
                else
                {
                    await memory.WriteUInt((uint) SignedData.Length);
                    await memory.WriteBytes(SignedData);
                }

                await memory.WriteUInt((uint) (Signatures.Sum(value => value.Length())));
                foreach (BlockSignature s in Signatures)
                {
                    await s.Write(memory);
                }

                if(PublicKey == null)
                {
                    await memory.WriteUInt(0);
                }
                else
                {
                    await memory.WriteUInt((uint) PublicKey.Length);
                    await memory.WriteBytes(PublicKey);
                }
            }
        }

        public static readonly uint ID = 0x7109871a;

        public List<Signer> Signers { get; private set; }

        public APKSignatureSchemeV2()
        {
            Signers = new List<Signer>();
        }

        public async Task Write(FileMemory memory)
        {
            await memory.WriteUInt((uint)Signers.Sum(value => value.Length()));
            foreach (Signer s in Signers)
            {
                await s.Write(memory);
            }
        }

        public async Task<APKSigningBlock.IDValuePair> ToIDValuePair()
        {
            using MemoryStream ms = new MemoryStream();
            using FileMemory memory = new FileMemory(ms);
            await Write(memory);
            return new APKSigningBlock.IDValuePair(ID, ms.ToArray());
        }

    }
}