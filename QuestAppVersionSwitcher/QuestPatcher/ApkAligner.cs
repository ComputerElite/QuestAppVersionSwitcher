using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ComputerUtils.Android.Logging;
using QuestAppVersionSwitcher;
using QuestAppVersionSwitcher.Mods;
using QuestPatcher.Core.Apk;
using Xamarin.Forms;
using File = System.IO.File;

namespace QuestPatcher.Core
{

    public class ApkAligner
    {

        public static async Task<bool> AlignApk(string path)
        {
            await using FileStream fs = new FileStream(path, FileMode.Open);
            await using FileMemory memory = new FileMemory(fs);
            TempFile t = new TempFile();
            await using FileStream tmp = new FileStream(t.Path, FileMode.Create);
            await using FileMemory outMemory = new FileMemory(tmp);
            memory.Position = memory.Length() - 22;
            while(await memory.ReadInt() != EndOfCentralDirectory.SIGNATURE)
            {
                memory.Position -= 4 + 1;
            }
            memory.Position -= 4;
            List<CentralDirectoryFileHeader> cDs = new List<CentralDirectoryFileHeader>();
            EndOfCentralDirectory eocd;
            try
            {
                eocd = new EndOfCentralDirectory();
                await eocd.Populate(memory);
            }
            catch (Exception e)
            {
                QAVSWebserver.patchStatus.error = true;
                QAVSWebserver.patchStatus.errorText = "Error while aligning apk: " + e.Message;
                QAVSWebserver.BroadcastPatchingStatus();
                return false;
            }
            memory.Position = eocd.OffsetOfCD;
            for(int i = 0; i < eocd.NumberOfCDsOnDisk; i++)
            {
                CentralDirectoryFileHeader cd = new CentralDirectoryFileHeader();
                await cd.Populate(memory);
                var nextCD = memory.Position;
                memory.Position = cd.Offset;
                LocalFileHeader lfh = new LocalFileHeader();
                await lfh.ReadLocalFileHeader(memory);
                byte[] data = await memory.ReadBytes(cd.CompressedSize);
                DataDescriptor? dd = null;
                try
                {
                    if ((lfh.GeneralPurposeFlag & 0x08) != 0)
                    {
                        
                        dd = new DataDescriptor();
                        await dd.Populate(memory);
                    }
                }
                catch (Exception e)
                {
                    // Error reading DataDescriptor, abort aligning
                    QAVSWebserver.patchStatus.error = true;
                    QAVSWebserver.patchStatus.errorText = "Error while aligning apk: " + e.Message;
                    QAVSWebserver.BroadcastPatchingStatus();
                    return false;
                }
                if(lfh.CompressionMethod == 0) {
                    short padding = (short) ((outMemory.Position + 30 + FileMemory.StringLength(lfh.FileName) + lfh.ExtraField.Length) % 4);
                    if(padding > 0)
                    {
                        padding = (short) (4 - padding);
                        lfh.ExtraField = lfh.ExtraField.Concat(new byte[padding]).ToArray();
                    }
                }
                cd.Offset = (int) outMemory.Position;
                await lfh.Write(outMemory);
                await outMemory.WriteBytes(data);
                if(dd != null)
                    await dd.Write(outMemory);
                cDs.Add(cd);
                memory.Position = nextCD;
            }
            eocd.OffsetOfCD = (int) outMemory.Position;
            foreach(CentralDirectoryFileHeader cd in cDs)
            {
                await cd.Write(outMemory);
            }
            eocd.NumberOfCDs = (short) cDs.Count;
            eocd.NumberOfCDsOnDisk = (short) cDs.Count;
            eocd.SizeOfCD = (int) (outMemory.Position - eocd.OffsetOfCD);
            await eocd.Write(outMemory);
            fs.Close();
            tmp.Close();
            if (File.Exists(path)) File.Delete(path);
            File.Move(t.Path, path);
            return true;
        }

    }
}