using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BWMP
{
    struct MapListEntry
    {
        string ID;
        string Name;
        string Landscape;
        string NumberOfPlayers;
        string Unknown1;
        string Unknown2;

        public MapListEntry(string id, string name, string landscape, string nplayers, string unk1, string unk2)
        {
            ID = id;
            Name = name;
            Landscape = landscape;
            NumberOfPlayers = nplayers;
            Unknown1 = unk1;
            Unknown2 = unk2;
        }
    }

    static class Lionhead
    {
        public static string ConstructDBTableString(string[,] table)
        {
            StringBuilder builder = new StringBuilder();

            int nRows = table.GetLength(0);
            int nCols = table.GetLength(1);

            for (int iRow = 0; iRow < nRows; iRow++)
                for (int iCol = 0; iCol < nCols; iCol++)
                    builder.AppendFormat("\x02{0}\x03", table[iRow, iCol]);

            builder.AppendFormat("[rows]:{0}[columns]:{1}[totalcolumns]:{2}", nRows, nCols, nRows * nCols);

            return builder.ToString();
        }

        #region Web Encoding
        public static string LHWebEncode(byte[] inArr)
        {
            int strLength = inArr.Length * 2;

            StringBuilder builder = new StringBuilder(strLength);

            for (int i = 0; i < inArr.Length; i++)
            {
                var c1 = 65 + (inArr[i] >> 4);
                var c2 = 65 + (inArr[i] & 0xF);

                builder.Append((char)c1);
                builder.Append((char)c2);
            }

            return builder.ToString();
        }

        public static byte[] LHWebDecode(string inStr) /* this function works, it's the other obsufication shit) */
        {
            int outLength = inStr.Length / 2;
            byte[] outBuffer = new byte[inStr.Length / 2];
            int strPtr = 0;
            int i = 0;

            do
            {
                byte c1 = (byte)((byte)inStr[strPtr] - 65);
                byte c2 = (byte)((byte)inStr[strPtr + 1] - 65);

                strPtr += 2;

                byte finalc = (byte)(c2 + (byte)(c1 << 4));
                outBuffer[i] = finalc;

                i++;

            }
            while (i < outLength);

            return outBuffer;
        }
        #endregion

        #region Obsufucation

        private static int g_MagicSecret_1 = 0x429C; // 0x0AA9C;
        private static int g_MagicSecret_2 = 0x1D83; // 0x0CCC3;
        private static int g_MagicSecret_3 = 0x7BCA; // 0x0FFCF;
        private static int g_MagicSecret_4 = 0x5D10; // 0x0BBBB;
        public static void ResetMagicSecret()
        {
            g_MagicSecret_1 = 0x429C;
            g_MagicSecret_2 = 0x1D83;
            g_MagicSecret_3 = 0x7BCA;
            g_MagicSecret_4 = 0x5D10;
        }

        private static Encoding cp437 = Encoding.GetEncoding(437);

        public static byte[] Obsufucate(byte[] input, bool deob, int length = 0)
        {
            var bufferSizeByte = Math.Round(input.Length / 8.0) * 8;
            var bufferSizeInt = bufferSizeByte / 4;
            var lengthLeft = input.Length;

            byte[] inBytes = new byte[(int)bufferSizeByte];

            for (int n = 0; n < input.Length; n++)
                inBytes[n] = input[n];

            uint[] inBuffer = new uint[(int)bufferSizeInt];

            int n2 = 0;
            for (int n = 0; n < inBytes.Length; n += 4)
            {
                inBuffer[n2] = BitConverter.ToUInt32(inBytes, n);
                n2++;
            }

            int offsetThing = 0;

            if (lengthLeft >= 8)
            {
                var blocksOf8 = input.Length >> 3;
                lengthLeft = input.Length + (-8 * blocksOf8);

                for (offsetThing = 0; offsetThing < blocksOf8; offsetThing++)
                {
                    if (!deob)
                        ObsufucateBlock(ref inBuffer, offsetThing * 2);
                    else
                        DeobsufucateBlock(ref inBuffer, offsetThing * 2);

                    g_MagicSecret_1 += 4;
                    g_MagicSecret_2 -= 74;
                    g_MagicSecret_3 += 172;
                    g_MagicSecret_4 -= 26;
                }
            }

            if (lengthLeft > 0)
                if (!deob)
                    ObsufucateBlock(ref inBuffer, offsetThing * 2);
                else
                    DeobsufucateBlock(ref inBuffer, offsetThing * 2);

            byte[] outBytes;

            if (length == 0)
                outBytes = new byte[(int)bufferSizeByte];
            else
                outBytes = new byte[length];

            Buffer.BlockCopy(inBuffer, 0, outBytes, 0, outBytes.Length);
            return outBytes;
        }

        private static void ObsufucateBlock(ref uint[] buffer, int offset)
        {
            int magicInt = 0;

            uint strHalf_1 = buffer[offset + 0];
            uint strHalf_2 = buffer[offset + 1];

            for (int i = 0; i < 32; i++)
            {
                magicInt -= 0x61C88647;

                strHalf_1 += (uint)((magicInt + strHalf_2) ^ (g_MagicSecret_1 + (uint)16 * strHalf_2) ^ (g_MagicSecret_2 + (strHalf_2 >> 5)));
                strHalf_2 += (uint)((magicInt + strHalf_1) ^ (g_MagicSecret_3 + (uint)16 * strHalf_1) ^ (g_MagicSecret_4 + (strHalf_1 >> 5)));
            }

            buffer[offset + 0] = strHalf_1;
            buffer[offset + 1] = strHalf_2;
        }

        private static void DeobsufucateBlock(ref uint[] buffer, int offset)
        {
            uint magicInt = 0xC6EF3720;

            uint strHalf_1 = buffer[offset + 0];
            uint strHalf_2 = buffer[offset + 1];

            for (int i = 0; i < 32; i++)
            {
                strHalf_2 -= (uint)((magicInt + strHalf_1) ^ (g_MagicSecret_3 + (uint)16 * strHalf_1) ^ (g_MagicSecret_4 + (strHalf_1 >> 5)));
                strHalf_1 -= (uint)((magicInt + strHalf_2) ^ (g_MagicSecret_1 + (uint)16 * strHalf_2) ^ (g_MagicSecret_2 + (strHalf_2 >> 5)));

                magicInt += (uint)0x61C88647;
            }

            buffer[offset + 0] = strHalf_1;
            buffer[offset + 1] = strHalf_2;
        }

        #region Obsufucation Accessors
        public static byte[] Obsufucate(string input, int length = 0) { return Obsufucate(input, false, length); }
        public static byte[] Deobsufucate(string input, int length = 0) { return Obsufucate(input, true, length); }
        public static byte[] Deobsufucate(byte[] input, int length = 0) { return Obsufucate(input, true, length); }
        public static string ObsufucateAsString(byte[] input, bool deob, int length = 0) { return cp437.GetString(Obsufucate(input, deob, length)); }
        public static string ObsufucateAsString(string input, bool deob, int length = 0) { return cp437.GetString(Obsufucate(input, deob, length)); }

        public static string ObsufucateAsString(string input, int length = 0) { return ObsufucateAsString(input, false, length); }
        public static string DeobsufucateAsString(string input, int length = 0) { return ObsufucateAsString(input, true, length); }
        public static string ObsufucateAsString(byte[] input, int length = 0) { return ObsufucateAsString(input, false, length); }
        public static string DeobsufucateAsString(byte[] input, int length = 0) { return ObsufucateAsString(input, true, length); }
        public static byte[] Obsufucate(string input, bool deob, int length = 0) { return Obsufucate(cp437.GetBytes(input), deob, length); }
        #endregion

        #endregion

    }
}
