using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ComputerUtils.Encryption
{
    public class PasswordEncryption
    {
        // char is 16 bit 0xFFFF max
        public static string Decrypt(string ciphertext, string password)
        {
            string finished = "";
            for (int i = 0; i < ciphertext.Length; i++)
            {
                int c = ciphertext[i] - password[i % password.Length];
                if (c < 0) c = 0xFFFF + c;
                finished += (char)c;
            }
            return finished;
        }

        public static string Encrypt(string text, string password)
        {
            string finished = "";
            for (int i = 0; i < text.Length; i++)
            {
                int c = ((text[i] + password[i % password.Length]));
                if (c > 0xFFFF) c -= 0xFFFF;
                finished += (char)c;
            }
            return finished;
        }

        public static string ToEasyReadTM(string input)
        {
            string finished = "";
            foreach (char c in input)
            {
                finished += (int)c + " ";
            }
            return finished;
        }
    }

    public class Encrypter
    {
        public Tuple<byte[], byte[]> EncryptOTP(byte[] input)
        {
            byte[] output = new byte[input.Length];
            byte[] key = new byte[input.Length];
            BitArray i = new BitArray(input);
            Random rnd = new Random();
            rnd.NextBytes(key);
            BitArray k = new BitArray(key);
            i.Xor(k);
            i.CopyTo(output, 0);
            k.CopyTo(key, 0);
            return new Tuple<byte[], byte[]>(output, key);
        }

        public void EncryptFileOTP(String file, String outputDirectory, bool overrideSourceFile = false, bool useLowMem = false, bool outputToConsole = true, int batches = 1000000)
        {
            String exe = AppDomain.CurrentDomain.BaseDirectory;
            if (useLowMem)
            {
                FileStream ifile = new FileStream(file, FileMode.Open);

                File.Delete(outputDirectory + "\\" + Path.GetFileName(file) + ".key");
                FileStream kfile = new FileStream(outputDirectory + "\\" + Path.GetFileName(file) + ".key", FileMode.Append);
                FileStream ofile = new FileStream(exe + Path.GetFileName(file), FileMode.Append);
                if (outputToConsole) Console.Write("0/0 (100%)");
                for (int i = 1; (long)i * (long)batches < ifile.Length + (long)batches; i++)
                {
                    int adjusted = (long)i * (long)batches < ifile.Length ? batches : (int)(ifile.Length % batches);
                    byte[] tmp1 = new byte[adjusted];
                    for (int ii = 0; ii < adjusted; ii++)
                    {
                        tmp1[ii] = (byte)ifile.ReadByte();
                    }

                    //EncryptOTP
                    byte[] output = new byte[tmp1.Length];
                    byte[] key = new byte[tmp1.Length];
                    BitArray inp = new BitArray(tmp1);
                    Random rnd = new Random();
                    rnd.NextBytes(key);
                    BitArray k = new BitArray(key);
                    inp.Xor(k);
                    inp.CopyTo(output, 0);
                    k.CopyTo(key, 0);

                    ofile.Write(output, 0, output.Length);
                    kfile.Write(key, 0, key.Length);
                    ofile.Flush();
                    kfile.Flush();
                    if (outputToConsole) Console.Write("\r" + (i * (batches / 1000000)) + " MB /" + (ifile.Length / 1000000) + " MB (" + ((double)i * (double)batches / ifile.Length * 100) + " %)");
                }
                if (outputToConsole) Console.WriteLine();
                if (outputToConsole) Console.WriteLine("Closing files");
                ofile.Close();
                kfile.Close();
                ifile.Close();
            }
            else
            {
                Console.WriteLine("Started Encryption, please wait.");
                byte[] fileContents = File.ReadAllBytes(file);

                //EncryptOTP
                byte[] output = new byte[fileContents.Length];
                byte[] key = new byte[fileContents.Length];
                BitArray inp = new BitArray(fileContents);
                Random rnd = new Random();
                rnd.NextBytes(key);
                BitArray k = new BitArray(key);
                inp.Xor(k);
                inp.CopyTo(output, 0);
                k.CopyTo(key, 0);

                File.WriteAllBytes(outputDirectory + "\\" + Path.GetFileName(file) + ".key", key);
                File.WriteAllBytes(exe + Path.GetFileName(file), output);
            }
            if (overrideSourceFile)
            {
                File.Delete(file);
                File.Move(exe + Path.GetFileName(file), outputDirectory + "\\" + Path.GetFileName(file));
            }
            else
            {
                File.Delete(outputDirectory + "\\" + Path.GetFileName(file) + ".encr");
                File.Move(exe + Path.GetFileName(file), outputDirectory + "\\" + Path.GetFileName(file) + ".encr");
            }

        }
    }

    public class Decrypter
    {
        public byte[] DecryptOTP(Byte[] input, Byte[] key)
        {
            byte[] output = new byte[input.Length];
            BitArray i = new BitArray(input);
            BitArray k = new BitArray(key);
            i.Xor(k);
            i.CopyTo(output, 0);
            return output;
        }

        public void DecryptOTPFile(String file, String keyFile, String outputDirectory, bool useLowMem = false, bool outputToConsole = true, int batches = 1000000)
        {
            if (useLowMem)
            {
                File.Delete(outputDirectory + Path.GetFileNameWithoutExtension(keyFile));
                FileStream ifile = new FileStream(file, FileMode.Open);
                FileStream kfile = new FileStream(keyFile, FileMode.Open);
                FileStream ofile = new FileStream(outputDirectory, FileMode.Append);
                for (int i = 1; (long)i * (long)batches < ifile.Length + (long)batches; i++)
                {
                    int adjusted = (long)i * (long)batches < ifile.Length ? batches : (int)(ifile.Length % batches);
                    byte[] tmp11 = new byte[adjusted];
                    byte[] tmp12 = new byte[adjusted];
                    Console.WriteLine(tmp11.Length);
                    for (int ii = 0; ii < adjusted; ii++)
                    {
                        tmp11[ii] = (byte)ifile.ReadByte();
                        tmp12[ii] = (byte)kfile.ReadByte();
                    }

                    //DecryptOTP
                    byte[] output1 = new byte[tmp11.Length];
                    BitArray inp = new BitArray(tmp11);
                    BitArray k = new BitArray(tmp12);
                    inp.Xor(k);
                    inp.CopyTo(output1, 0);

                    ofile.Write(output1, 0, output1.Length);
                    ofile.Flush();
                    if (outputToConsole) Console.Write("\r" + (i * (batches / 1000000)) + " MB /" + (ifile.Length / 1000000) + " MB (" + ((double)i * (double)batches / ifile.Length * 100) + " %)");
                }
                ofile.Flush();
                ifile.Close();
                kfile.Close();
                ofile.Close();
            }
            else
            {
                if (outputToConsole) Console.WriteLine("Started Decryption, please wait.");
                byte[] fileContents = File.ReadAllBytes(file);
                byte[] keyFileContents = File.ReadAllBytes(keyFile);

                //DecryptOPT
                byte[] output = new byte[fileContents.Length];
                BitArray i = new BitArray(fileContents);
                BitArray k = new BitArray(keyFileContents);
                i.Xor(k);
                i.CopyTo(output, 0);

                File.WriteAllBytes(outputDirectory, output);
            }
        }
    }
}