//Copyright (c) 2012 Josip Medved <jmedved@jmedved.com>

//2012-04-12: Initial version.


using System;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;

namespace Pillager.Helper
{
    /// <summary>
    /// Generic PBKDF2 implementation.
    /// </summary>
    /// <example>This sample shows how to initialize class with SHA-256 HMAC.
    /// <code>
    /// using (var hmac = new HMACSHA256()) {
    ///     var df = new Pbkdf2(hmac, "password", "salt");
    ///     var bytes = df.GetBytes();
    /// }
    /// </code>
    /// </example>
    [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Pbkdf", Justification = "Spelling is correct.")]
    public class Pbkdf2 {

        /// <summary>
        /// Creates new instance.
        /// </summary>
        /// <param name="algorithm">HMAC algorithm to use.</param>
        /// <param name="password">The password used to derive the key.</param>
        /// <param name="salt">The key salt used to derive the key.</param>
        /// <param name="iterations">The number of iterations for the operation.</param>
        /// <exception cref="System.ArgumentNullException">Algorithm cannot be null - Password cannot be null. -or- Salt cannot be null.</exception>
        public Pbkdf2(HMAC algorithm, byte[] password, byte[] salt, int iterations) {
            Algorithm = algorithm ?? throw new ArgumentNullException(nameof(algorithm), "Algorithm cannot be null.");
            Algorithm.Key = password ?? throw new ArgumentNullException(nameof(password), "Password cannot be null.");
            Salt = salt ?? throw new ArgumentNullException(nameof(salt), "Salt cannot be null.");
            IterationCount = iterations;
            BlockSize = Algorithm.HashSize / 8;
            BufferBytes = new byte[BlockSize];
        }

        /// <summary>
        /// Creates new instance.
        /// </summary>
        /// <param name="algorithm">HMAC algorithm to use.</param>
        /// <param name="password">The password used to derive the key.</param>
        /// <param name="salt">The key salt used to derive the key.</param>
        /// <exception cref="System.ArgumentNullException">Algorithm cannot be null - Password cannot be null. -or- Salt cannot be null.</exception>
        public Pbkdf2(HMAC algorithm, byte[] password, byte[] salt)
            : this(algorithm, password, salt, 1000) {
        }

        /// <summary>
        /// Creates new instance.
        /// </summary>
        /// <param name="algorithm">HMAC algorithm to use.</param>
        /// <param name="password">The password used to derive the key.</param>
        /// <param name="salt">The key salt used to derive the key.</param>
        /// <param name="iterations">The number of iterations for the operation.</param>
        /// <exception cref="System.ArgumentNullException">Algorithm cannot be null - Password cannot be null. -or- Salt cannot be null.</exception>
        public Pbkdf2(HMAC algorithm, string password, string salt, int iterations) :
            this(algorithm, UTF8Encoding.UTF8.GetBytes(password), UTF8Encoding.UTF8.GetBytes(salt), iterations) {
        }

        /// <summary>
        /// Creates new instance.
        /// </summary>
        /// <param name="algorithm">HMAC algorithm to use.</param>
        /// <param name="password">The password used to derive the key.</param>
        /// <param name="salt">The key salt used to derive the key.</param>
        /// <exception cref="System.ArgumentNullException">Algorithm cannot be null - Password cannot be null. -or- Salt cannot be null.</exception>
        public Pbkdf2(HMAC algorithm, string password, string salt) :
            this(algorithm, password, salt, 1000) {
        }


        private readonly int BlockSize;
        private uint BlockIndex = 1;

        private byte[] BufferBytes;
        private int BufferStartIndex;
        private int BufferEndIndex;


        /// <summary>
        /// Gets algorithm used for generating key.
        /// </summary>
        public HMAC Algorithm { get; private set; }

        /// <summary>
        /// Gets salt bytes.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "Byte array is proper return value in this case.")]
        public byte[] Salt { get; private set; }

        /// <summary>
        /// Gets iteration count.
        /// </summary>
        public int IterationCount { get; private set; }


        /// <summary>
        /// Returns a pseudo-random key from a password, salt and iteration count.
        /// </summary>
        /// <param name="count">Number of bytes to return.</param>
        /// <returns>Byte array.</returns>
        public byte[] GetBytes(int count) {
            byte[] result = new byte[count];
            int resultOffset = 0;
            int bufferCount = BufferEndIndex - BufferStartIndex;

            if (bufferCount > 0) { //if there is some data in buffer
                if (count < bufferCount) { //if there is enough data in buffer
                    Buffer.BlockCopy(BufferBytes, BufferStartIndex, result, 0, count);
                    BufferStartIndex += count;
                    return result;
                }
                Buffer.BlockCopy(BufferBytes, BufferStartIndex, result, 0, bufferCount);
                BufferStartIndex = BufferEndIndex = 0;
                resultOffset += bufferCount;
            }

            while (resultOffset < count) {
                int needCount = count - resultOffset;
                BufferBytes = Func();
                if (needCount > BlockSize) { //we one (or more) additional passes
                    Buffer.BlockCopy(BufferBytes, 0, result, resultOffset, BlockSize);
                    resultOffset += BlockSize;
                } else {
                    Buffer.BlockCopy(BufferBytes, 0, result, resultOffset, needCount);
                    BufferStartIndex = needCount;
                    BufferEndIndex = BlockSize;
                    return result;
                }
            }
            return result;
        }


        private byte[] Func() {
            var hash1Input = new byte[Salt.Length + 4];
            Buffer.BlockCopy(Salt, 0, hash1Input, 0, Salt.Length);
            Buffer.BlockCopy(GetBytesFromInt(BlockIndex), 0, hash1Input, Salt.Length, 4);
            var hash1 = Algorithm.ComputeHash(hash1Input);

            byte[] finalHash = hash1;
            for (int i = 2; i <= IterationCount; i++) {
                hash1 = Algorithm.ComputeHash(hash1, 0, hash1.Length);
                for (int j = 0; j < BlockSize; j++) {
                    finalHash[j] = (byte)(finalHash[j] ^ hash1[j]);
                }
            }
            if (BlockIndex == uint.MaxValue) { throw new InvalidOperationException("Derived key too long."); }
            BlockIndex += 1;

            return finalHash;
        }

        private static byte[] GetBytesFromInt(uint i) {
            var bytes = BitConverter.GetBytes(i);
            if (BitConverter.IsLittleEndian) {
                return new[] { bytes[3], bytes[2], bytes[1], bytes[0] };
            }

            return bytes;
        }

    }
}
