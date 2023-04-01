using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using ComputerUtils.Android.Logging;
using QuestAppVersionSwitcher;
using QuestAppVersionSwitcher.ClientModels;
using QuestAppVersionSwitcher.Mods;
using QuestPatcher.Core.Apk;

namespace QuestPatcher.Core
{

    public class ApkAligner
    {

        public static void AlignApk(string path)
        {
            try
            {
                using FileStream fs = new FileStream(path, FileMode.Open);
                TempFile temp = new TempFile();
                QAVSWebserver.patchText = JsonSerializer.Serialize(new MessageAndValue<String>("Aligning apk", ""));
                using FileStream outFs = new FileStream(temp.Path, FileMode.Create);
                outFs.Position = 0;
                fs.Position = fs.Length - 22;
                while(StreamReaderExtension.ReadInt(fs) != EndOfCentralDirectory.SIGNATURE)
                {
                    fs.Position -= 4 + 1;
                }
                fs.Position -= 4;
                List<CentralDirectoryFileHeader> cDs = new List<CentralDirectoryFileHeader>();
                EndOfCentralDirectory eocd = new EndOfCentralDirectory(fs);
                if(eocd == null)
                    return;
                fs.Position = eocd.OffsetOfCD;
                for(int i = 0; i < eocd.NumberOfCDsOnDisk; i++)
                {
                    CentralDirectoryFileHeader cd = new CentralDirectoryFileHeader(fs);
                    var nextCD = fs.Position;
                    fs.Position = cd.Offset;
                    LocalFileHeader lfh = new LocalFileHeader(fs);
                    byte[] data = StreamReaderExtension.ReadBytes(fs, cd.CompressedSize);
                    DataDescriptor? dd = null;
                    if((lfh.GeneralPurposeFlag & 0x08) != 0) 
                        dd = new DataDescriptor(fs);
                    if(lfh.CompressionMethod == 0) {
                        short padding = (short) ((outFs.Position + 30 + FileMemory.StringLength(lfh.FileName) + lfh.ExtraField.Length) % 4);
                        if(padding > 0)
                        {
                            padding = (short) (4 - padding);
                            lfh.ExtraField = lfh.ExtraField.Concat(new byte[padding]).ToArray();
                        }
                    }
                    cd.Offset = (int) outFs.Position;
                    lfh.Write(outFs);
                    StreamWriterExtension.WriteBytes(outFs, data);
                    if(dd != null)
                        dd.Write(outFs);
                    cDs.Add(cd);
                    fs.Position = nextCD;
                }
                eocd.OffsetOfCD = (int) outFs.Position;
                foreach(CentralDirectoryFileHeader cd in cDs)
                {
                    cd.Write(outFs);
                }
                eocd.NumberOfCDs = (short) cDs.Count;
                eocd.NumberOfCDsOnDisk = (short) cDs.Count;
                eocd.SizeOfCD = (int) (outFs.Position - eocd.OffsetOfCD);
                eocd.Write(outFs);
                fs.Close();
                Logger.Log("Aligning done. Deleting " + path + " and replacing it with " + temp.Path);
                QAVSWebserver.patchText = JsonSerializer.Serialize(new MessageAndValue<String>("Aligning done, removing temporary apk", ""));
                if(File.Exists(path)) File.Delete(path);
                File.Move(temp.Path, path);
            }
            catch (Exception e)
            {
                Logger.Log(e.ToString(), LoggingType.Error);
                QAVSWebserver.patchCode = 500;
                QAVSWebserver.patchText = JsonSerializer.Serialize(new MessageAndValue<String>("Error while aligning apk: " + e, ""));
            }
            
        }

    }
}