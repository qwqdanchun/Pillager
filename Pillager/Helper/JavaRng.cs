using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pillager.Helper
{
    public sealed class JavaRng
    {
        public JavaRng(long seed)
        {
            _seed = (seed ^ LARGE_PRIME) & ((1L << 48) - 1);
        }

        public long nextLong()
        {
            return ((long)next(32) << 32) + next(32);
        }

        public int nextInt(int bound)
        {
            if (bound <= 0)
                throw new ArgumentOutOfRangeException(nameof(bound), bound, "bound must be positive");

            int r = next(31);
            int m = bound - 1;
            if ((bound & m) == 0)  // i.e., bound is a power of 2
                r = (int)((bound * (long)r) >> 31);
            else
            {
                for (int u = r;
                     u - (r = u % bound) + m < 0;
                     u = next(31))
                    ;
            }
            return r;
        }

        private int next(int bits)
        {
            _seed = (_seed * LARGE_PRIME + SMALL_PRIME) & ((1L << 48) - 1);
            return (int)((_seed) >> (48 - bits));
        }

        private long _seed;

        private const long LARGE_PRIME = 0x5DEECE66DL;
        private const long SMALL_PRIME = 0xBL;
    }
}
