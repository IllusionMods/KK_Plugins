using System;

namespace LibAPNG
{
    internal class Helper
    {
        /// <summary>
        ///     Convert big-endian to little-endian or reserve
        /// </summary>
        internal static byte[] ConvertEndian(byte[] i)
        {
            if (i.Length % 2 != 0)
                throw new Exception("byte array length must multiply of 2");

            Array.Reverse(i);

            return i;
        }

        /// <summary>
        ///     Convert big-endian to little-endian or reserve
        /// </summary>
        internal static int ConvertEndian(int i)
        {
            return BitConverter.ToInt32(ConvertEndian(BitConverter.GetBytes(i)), 0);
        }

        /// <summary>
        ///     Convert big-endian to little-endian or reserve
        /// </summary>
        internal static uint ConvertEndian(uint i)
        {
            return BitConverter.ToUInt32(ConvertEndian(BitConverter.GetBytes(i)), 0);
        }

        /// <summary>
        ///     Convert big-endian to little-endian or reserve
        /// </summary>
        internal static Int16 ConvertEndian(Int16 i)
        {
            return BitConverter.ToInt16(ConvertEndian(BitConverter.GetBytes(i)), 0);
        }

        /// <summary>
        ///     Convert big-endian to little-endian or reserve
        /// </summary>
        internal static UInt16 ConvertEndian(UInt16 i)
        {
            return BitConverter.ToUInt16(ConvertEndian(BitConverter.GetBytes(i)), 0);
        }

        /// <summary>
        ///     Compare two byte array
        /// </summary>
        public static bool IsBytesEqual(byte[] byte1, byte[] byte2)
        {
            if (byte1.Length != byte2.Length)
                return false;

            for (int i = 0; i < byte1.Length; i++)
            {
                if (byte1[i] != byte2[i])
                    return false;
            }
            return true;
        }
    }
}