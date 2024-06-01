using System;
using System.Collections.Generic;
using System.Text;

namespace KK_Plugins
{
    internal class Utility
    {

        /// <summary>
        /// Compares two byte arrays for equality in a high-performance manner using unsafe code.
        /// </summary>
        /// <param name="a">The first byte array to compare.</param>
        /// <param name="b">The second byte array to compare.</param>
        /// <returns>True if the byte arrays are equal, false otherwise.</returns>
        static public bool FastSequenceEqual(byte[] a, byte[] b)
        {
            // Check if both references are the same, if so, return true.
            if (System.Object.ReferenceEquals(a, b))
                return true;

            if (a == null || b == null)
                return false;

            int bytes = a.Length;

            if (bytes != b.Length)
                return false;

            if (bytes <= 0)
                return true;

            unsafe
            {
                // Fix the memory locations of the arrays to prevent the garbage collector from moving them.
                fixed (byte* pA = &a[0])
                fixed (byte* pB = &b[0])
                {
                    int offset = 0;

                    // If both pointers are 8-byte aligned, use 64-bit comparison.
                    if (((int)pA & 7) == 0 && ((int)pB & 7) == 0 && bytes >= 32)
                    {
                        offset = bytes & ~31;       // Round down to the nearest multiple of 32.

                        byte* pA_ = pA;
                        byte* pB_ = pB;
                        byte* pALast = pA + offset;

                        do
                        {
                            if (*(ulong*)pA_ != *(ulong*)pB_)
                                goto NotEquals;

                            pA_ += 8;
                            pB_ += 8;

                            if (*(ulong*)pA_ != *(ulong*)pB_)
                                goto NotEquals;

                            pA_ += 8;
                            pB_ += 8;

                            if (*(ulong*)pA_ != *(ulong*)pB_)
                                goto NotEquals;

                            pA_ += 8;
                            pB_ += 8;

                            if (*(ulong*)pA_ != *(ulong*)pB_)
                                goto NotEquals;

                            pA_ += 8;
                            pB_ += 8;
                        }
                        while (pA_ != pALast);
                    }
                    // If both pointers are 4-byte aligned, use 32-bit comparison.
                    else if (((int)pA & 3) == 0 && ((int)pB & 3) == 0 && bytes >= 16)
                    {
                        offset = bytes & ~15;       // Round down to the nearest multiple of 16.

                        byte* pA_ = pA;
                        byte* pB_ = pB;
                        byte* pALast = pA + offset;

                        do
                        {
                            if (*(uint*)pA_ != *(uint*)pB_)
                                goto NotEquals;

                            pA_ += 4;
                            pB_ += 4;

                            if (*(uint*)pA_ != *(uint*)pB_)
                                goto NotEquals;

                            pA_ += 4;
                            pB_ += 4;

                            if (*(uint*)pA_ != *(uint*)pB_)
                                goto NotEquals;

                            pA_ += 4;
                            pB_ += 4;

                            if (*(uint*)pA_ != *(uint*)pB_)
                                goto NotEquals;

                            pA_ += 4;
                            pB_ += 4;
                        }
                        while (pA_ != pALast);
                    }

                    // Compare remaining bytes one by one.
                    for (int i = offset; i < bytes; ++i)
                        if (pA[i] != pB[i])
                            goto NotEquals;
                }
            }

            return true;

NotEquals:
            // Return false indicating arrays are not equal.
            // Note: Using a return statement in the loop can potentially degrade performance due to the generated binary code, 
            return false;
        }
    }
}
