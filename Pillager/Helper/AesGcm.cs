using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace Pillager.Helper
{
    //AES GCM from https://github.com/dvsekhvalnov/jose-jwt
    internal class AesGcm
    {
        public byte[] Decrypt(byte[] key, byte[] iv, byte[] aad, byte[] cipherText, byte[] authTag)
        {
            IntPtr hAlg = OpenAlgorithmProvider(Native.BCRYPT_AES_ALGORITHM, Native.MS_PRIMITIVE_PROVIDER, Native.BCRYPT_CHAIN_MODE_GCM);
            var keyDataBuffer = ImportKey(hAlg, key, out var hKey);

            byte[] plainText;

            Native.BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO authInfo = new Native.BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO(iv, aad, authTag);
            byte[] ivData = new byte[MaxAuthTagSize(hAlg)];

            int plainTextSize = 0;

            uint status = Native.BCryptDecrypt(hKey, cipherText, cipherText.Length, ref authInfo, ivData, ivData.Length, null, 0, ref plainTextSize, 0x0);

            if (status != Native.ERROR_SUCCESS)
                throw new CryptographicException(
                    $"Native.BCryptDecrypt() (get size) failed with status code: {status}");

            plainText = new byte[plainTextSize];

            status = Native.BCryptDecrypt(hKey, cipherText, cipherText.Length, ref authInfo, ivData, ivData.Length, plainText, plainText.Length, ref plainTextSize, 0x0);

            if (status == Native.STATUS_AUTH_TAG_MISMATCH)
                throw new CryptographicException("Native.BCryptDecrypt(): authentication tag mismatch");

            if (status != Native.ERROR_SUCCESS)
                throw new CryptographicException($"Native.BCryptDecrypt() failed with status code:{status}");

            authInfo.Dispose();

            Native.BCryptDestroyKey(hKey);
            Marshal.FreeHGlobal(keyDataBuffer);
            Native.BCryptCloseAlgorithmProvider(hAlg, 0x0);

            return plainText;
        }

        private int MaxAuthTagSize(IntPtr hAlg)
        {
            byte[] tagLengthsValue = GetProperty(hAlg, Native.BCRYPT_AUTH_TAG_LENGTH);

            return BitConverter.ToInt32(new[] { tagLengthsValue[4], tagLengthsValue[5], tagLengthsValue[6], tagLengthsValue[7] }, 0);
        }

        private IntPtr OpenAlgorithmProvider(string alg, string provider, string chainingMode)
        {
            uint status = Native.BCryptOpenAlgorithmProvider(out var hAlg, alg, provider, 0x0);

            if (status != Native.ERROR_SUCCESS)
                throw new CryptographicException(
                    $"Native.BCryptOpenAlgorithmProvider() failed with status code:{status}");

            byte[] chainMode = Encoding.Unicode.GetBytes(chainingMode);
            status = Native.BCryptSetAlgorithmProperty(hAlg, Native.BCRYPT_CHAINING_MODE, chainMode, chainMode.Length, 0x0);

            if (status != Native.ERROR_SUCCESS)
                throw new CryptographicException(
                    $"Native.BCryptSetAlgorithmProperty(Native.BCRYPT_CHAINING_MODE, Native.BCRYPT_CHAIN_MODE_GCM) failed with status code:{status}");

            return hAlg;
        }

        private IntPtr ImportKey(IntPtr hAlg, byte[] key, out IntPtr hKey)
        {
            byte[] objLength = GetProperty(hAlg, Native.BCRYPT_OBJECT_LENGTH);

            int keyDataSize = BitConverter.ToInt32(objLength, 0);

            IntPtr keyDataBuffer = Marshal.AllocHGlobal(keyDataSize);

            byte[] keyBlob = Concat(Native.BCRYPT_KEY_DATA_BLOB_MAGIC, BitConverter.GetBytes(0x1), BitConverter.GetBytes(key.Length), key);

            uint status = Native.BCryptImportKey(hAlg, IntPtr.Zero, Native.BCRYPT_KEY_DATA_BLOB, out hKey, keyDataBuffer, keyDataSize, keyBlob, keyBlob.Length, 0x0);

            if (status != Native.ERROR_SUCCESS)
                throw new CryptographicException($"Native.BCryptImportKey() failed with status code:{status}");

            return keyDataBuffer;
        }

        private byte[] GetProperty(IntPtr hAlg, string name)
        {
            int size = 0;

            uint status = Native.BCryptGetProperty(hAlg, name, null, 0, ref size, 0x0);

            if (status != Native.ERROR_SUCCESS)
                throw new CryptographicException(
                    $"Native.BCryptGetProperty() (get size) failed with status code:{status}");

            byte[] value = new byte[size];

            status = Native.BCryptGetProperty(hAlg, name, value, value.Length, ref size, 0x0);

            if (status != Native.ERROR_SUCCESS)
                throw new CryptographicException($"Native.BCryptGetProperty() failed with status code:{status}");

            return value;
        }

        public byte[] Concat(params byte[][] arrays)
        {
            int len = 0;

            foreach (byte[] array in arrays)
            {
                if (array == null)
                    continue;
                len += array.Length;
            }

            byte[] result = new byte[len - 1 + 1];
            int offset = 0;

            foreach (byte[] array in arrays)
            {
                if (array == null)
                    continue;
                Buffer.BlockCopy(array, 0, result, offset, array.Length);
                offset += array.Length;
            }

            return result;
        }
    }
}
