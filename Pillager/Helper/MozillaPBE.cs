using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;

namespace Pillager.Helper
{
    public class MozillaPBE
    {
        private byte[] cipherText { get; set; }
        private byte[] GlobalSalt { get; set; }
        private byte[] MasterPassword { get; set; }
        private byte[] EntrySalt { get; set; }
        public byte[] partIV { get; private set; }

        public MozillaPBE(byte[] cipherText, byte[] GlobalSalt, byte[] MasterPassword, byte[] EntrySalt, byte[] partIV)
        {
            this.cipherText = cipherText;
            this.GlobalSalt = GlobalSalt;
            this.MasterPassword = MasterPassword;
            this.EntrySalt = EntrySalt;
            this.partIV = partIV;
        }

        public byte[] Compute()
        {
            byte[] GLMP; // GlobalSalt + MasterPassword
            byte[] HP; // SHA1(GLMP)
            byte[] IV; // ivPrefix + partIV
            byte[] key; // ivPrefix + partIV
            int iterations = 1;
            int keyLength = 32;

            // GLMP
            GLMP = new byte[this.GlobalSalt.Length + this.MasterPassword.Length];
            Buffer.BlockCopy(this.GlobalSalt, 0, GLMP, 0, this.GlobalSalt.Length);
            Buffer.BlockCopy(this.MasterPassword, 0, GLMP, this.GlobalSalt.Length, this.MasterPassword.Length);

            // HP
            HP = new SHA1Managed().ComputeHash(GLMP);

            // IV
            byte[] ivPrefix = new byte[2] { 0x04, 0x0e };
            IV = new byte[ivPrefix.Length + this.partIV.Length];
            Buffer.BlockCopy(ivPrefix, 0, IV, 0, ivPrefix.Length);
            Buffer.BlockCopy(this.partIV, 0, IV, ivPrefix.Length, this.partIV.Length);

            // .NET 4.6 doesn't have support for PBKDF2 with SHA256, it was introduced in .NET 4.7.2
            // I wanna stick with .NET 4.6 so I'll use this PBKDF2_SHA256 implementation by medo64(https://github.com/medo64)
            // (https://github.com/medo64/Medo/blob/master/Source/Medo/Security/Cryptography/Pbkdf2%20%5B001%5D.cs)
            var df = new Pbkdf2(new HMACSHA256(), HP, this.EntrySalt, iterations);
            key = df.GetBytes(keyLength);

            // AES-CBC-256 settings
            Aes aes = new AesManaged
            {
                Mode = CipherMode.CBC,
                BlockSize = 128,
                KeySize = 256,
                Padding = PaddingMode.Zeros,
            };

            // Decrypt AES cipher text
            ICryptoTransform AESDecrypt = aes.CreateDecryptor(key, IV);
            var clearText = AESDecrypt.TransformFinalBlock(this.cipherText, 0, this.cipherText.Length);

            return clearText;
        }
    }
}
