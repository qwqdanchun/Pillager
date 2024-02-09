using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Pillager.Helper
{
    class Navicat11Cipher
    {

        private Blowfish blowfishCipher;

        protected static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }

        protected static void XorBytes(byte[] a, byte[] b, int len)
        {
            for (int i = 0; i < len; ++i)
                a[i] ^= b[i];
        }

        public Navicat11Cipher()
        {
            byte[] UserKey = Encoding.UTF8.GetBytes("3DC5CA39");
            var sha1 = new SHA1CryptoServiceProvider();
            sha1.TransformFinalBlock(UserKey, 0, UserKey.Length);
            blowfishCipher = new Blowfish(sha1.Hash);
        }

        public Navicat11Cipher(string CustomUserKey)
        {
            byte[] UserKey = Encoding.UTF8.GetBytes(CustomUserKey);
            var sha1 = new SHA1CryptoServiceProvider();
            byte[] UserKeyHash = sha1.TransformFinalBlock(UserKey, 0, 8);
            blowfishCipher = new Blowfish(UserKeyHash);
        }

        public string DecryptString(string ciphertext)
        {
            byte[] ciphertext_bytes = StringToByteArray(ciphertext);

            byte[] CV = Enumerable.Repeat<byte>(0xFF, Blowfish.BlockSize).ToArray();
            blowfishCipher.Encrypt(CV, Blowfish.Endian.Big);

            byte[] ret = new byte[0];
            int blocks_len = ciphertext_bytes.Length / Blowfish.BlockSize;
            int left_len = ciphertext_bytes.Length % Blowfish.BlockSize;
            byte[] temp = new byte[Blowfish.BlockSize];
            byte[] temp2 = new byte[Blowfish.BlockSize];
            for (int i = 0; i < blocks_len; ++i)
            {
                Array.Copy(ciphertext_bytes, Blowfish.BlockSize * i, temp, 0, Blowfish.BlockSize);
                Array.Copy(temp, temp2, Blowfish.BlockSize);
                blowfishCipher.Decrypt(temp, Blowfish.Endian.Big);
                XorBytes(temp, CV, Blowfish.BlockSize);
                ret = ret.Concat(temp).ToArray();
                XorBytes(CV, temp2, Blowfish.BlockSize);
            }

            if (left_len != 0)
            {
                Array.Clear(temp, 0, temp.Length);
                Array.Copy(ciphertext_bytes, Blowfish.BlockSize * blocks_len, temp, 0, left_len);
                blowfishCipher.Encrypt(CV, Blowfish.Endian.Big);
                XorBytes(temp, CV, Blowfish.BlockSize);
                ret = ret.Concat(temp.Take(left_len).ToArray()).ToArray();
            }

            return Encoding.UTF8.GetString(ret);
        }
    }
}
