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
            blowfishCipher = new Blowfish();
            blowfishCipher.InitializeKey(sha1.Hash);
        }

        public Navicat11Cipher(string CustomUserKey)
        {
            byte[] UserKey = Encoding.UTF8.GetBytes(CustomUserKey);
            var sha1 = new SHA1CryptoServiceProvider();
            byte[] UserKeyHash = sha1.TransformFinalBlock(UserKey, 0, 8);
            blowfishCipher = new Blowfish();
            blowfishCipher.InitializeKey(UserKeyHash);
        }

        public string DecryptString(string ciphertext)
        {
            int BlockSize = 8;
            byte[] ciphertext_bytes = StringToByteArray(ciphertext);

            byte[] CV = Enumerable.Repeat<byte>(0xFF, BlockSize).ToArray();
            blowfishCipher.BlockEncrypt(CV, 0, CV, 0);

            byte[] ret = new byte[0];
            int blocks_len = ciphertext_bytes.Length / BlockSize;
            int left_len = ciphertext_bytes.Length % BlockSize;
            byte[] temp = new byte[BlockSize];
            byte[] temp2 = new byte[BlockSize];
            for (int i = 0; i < blocks_len; ++i)
            {
                Array.Copy(ciphertext_bytes, BlockSize * i, temp, 0, BlockSize);
                Array.Copy(temp, temp2, BlockSize);
                blowfishCipher.BlockDecrypt(temp, 0, temp, 0);
                XorBytes(temp, CV, BlockSize);
                ret = ret.Concat(temp).ToArray();
                XorBytes(CV, temp2, BlockSize);
            }

            if (left_len != 0)
            {
                Array.Clear(temp, 0, temp.Length);
                Array.Copy(ciphertext_bytes, BlockSize * blocks_len, temp, 0, left_len);
                blowfishCipher.BlockEncrypt(CV, 0, CV, 0);
                XorBytes(temp, CV, BlockSize);
                ret = ret.Concat(temp.Take(left_len).ToArray()).ToArray();
            }

            return Encoding.UTF8.GetString(ret);
        }
    }
}
