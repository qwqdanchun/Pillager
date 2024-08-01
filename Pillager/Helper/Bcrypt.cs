using System;
using System.Security.Cryptography;
using System.Text;

namespace Pillager.Helper
{
    internal class Bcrypt
    {

        private static readonly byte[] _bcryptCipherText = Encoding.ASCII.GetBytes("OxychromaticBlowfishSwatDynamite");

        /// <summary>
        /// bcrypt_hash
        /// </summary>
        /// <param name="blowfish">blowfish object to use</param>
        /// <param name="sha2pass">SHA512 of password</param>
        /// <param name="sha2salt">SHA512 of salt</param>
        /// <returns></returns>
        private byte[] BcryptHash(Blowfish blowfish, byte[] sha2pass, byte[] sha2salt)
        {
            // this code is based on OpenBSD's bcrypt_pbkdf.c
            const int BLOCKSIZE = 8;

            // key expansion
            blowfish.InitializeState();
            blowfish.ExpandState(sha2pass, sha2salt);
            for (int i = 0; i < 64; ++i)
            {
                blowfish.ExpandState(sha2salt);
                blowfish.ExpandState(sha2pass);
            }

            // encryption
            byte[] cdata = (byte[])_bcryptCipherText.Clone();
            for (int i = 0; i < 64; ++i)
            {
                for (int j = 0; j < 32; j += BLOCKSIZE)
                {
                    blowfish.BlockEncrypt(cdata, j, cdata, j);
                }
            }

            // copy out
            for (int i = 0; i < 32; i += 4)
            {
                byte b0 = cdata[i + 0];
                byte b1 = cdata[i + 1];
                byte b2 = cdata[i + 2];
                byte b3 = cdata[i + 3];
                cdata[i + 3] = b0;
                cdata[i + 2] = b1;
                cdata[i + 1] = b2;
                cdata[i + 0] = b3;
            }

            return cdata;
        }

        /// <summary>
        /// bcrypt_pbkdf (pkcs #5 pbkdf2 implementation using the "bcrypt" hash)
        /// </summary>
        /// <param name="pass">password</param>
        /// <param name="salt">salt</param>
        /// <param name="rounds">rounds</param>
        /// <param name="keylen">key length</param>
        /// <returns>key</returns>
        public byte[] BcryptPbkdf(string pass, byte[] salt, uint rounds, int keylen)
        {
            // this code is based on OpenBSD's bcrypt_pbkdf.c

            if (rounds < 1)
            {
                return null;
            }
            if (salt.Length == 0 || keylen <= 0 || keylen > 1024)
            {
                return null;
            }

            byte[] key = new byte[keylen];

            int stride = (keylen + 32 - 1) / 32;
            int amt = (keylen + stride - 1) / stride;

            var blowfish = new Blowfish();
            using (var sha512 = new SHA512CryptoServiceProvider())
            {

                // collapse password
                byte[] passData = Encoding.UTF8.GetBytes(pass);
                byte[] sha2pass = sha512.ComputeHash(passData);

                // generate key, sizeof(out) at a time
                byte[] countsalt = new byte[4];
                for (int count = 1; keylen > 0; ++count)
                {
                    countsalt[0] = (byte)(count >> 24);
                    countsalt[1] = (byte)(count >> 16);
                    countsalt[2] = (byte)(count >> 8);
                    countsalt[3] = (byte)(count);

                    // first round, salt is salt
                    sha512.Initialize();
                    sha512.TransformBlock(salt, 0, salt.Length, null, 0);
                    sha512.TransformFinalBlock(countsalt, 0, countsalt.Length);
                    byte[] sha2salt = sha512.Hash;
                    byte[] tmpout = BcryptHash(blowfish, sha2pass, sha2salt);
                    byte[] output = (byte[])tmpout.Clone();

                    for (uint r = rounds; r > 1; --r)
                    {
                        // subsequent rounds, salt is previous output
                        sha512.Initialize();
                        sha2salt = sha512.ComputeHash(tmpout);
                        tmpout = BcryptHash(blowfish, sha2pass, sha2salt);
                        for (int i = 0; i < output.Length; ++i)
                        {
                            output[i] ^= tmpout[i];
                        }
                    }

                    // pbkdf2 deviation: output the key material non-linearly.
                    amt = Math.Min(amt, keylen);
                    int k;
                    for (k = 0; k < amt; ++k)
                    {
                        int dest = k * stride + (count - 1);
                        if (dest >= key.Length)
                        {
                            break;
                        }
                        key[dest] = output[k];
                    }
                    keylen -= k;
                }
            }

            return key;
        }
    }
}
