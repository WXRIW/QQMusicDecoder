using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using System.Text;

namespace QQMusicDecoder
{
    public unsafe class Decrypter
    {

        private readonly static byte[] QQKey = Encoding.ASCII.GetBytes("!@#)(*$%123ZXC!@!@#)(NHL");


        /// <summary>
        /// 解密 QRC 歌词
        /// </summary>
        /// <param name="encryptedLyrics">加密的歌词</param>
        /// <returns>解密后的 QRC 歌词</returns>
        public static string? DecryptLyrics(string encryptedLyrics)
        {
            var res = string.Empty;
            var encryptedTextByte = Convert.FromHexString(encryptedLyrics); // parse text to bites array
            byte[] data = new byte[encryptedTextByte.Length];
            byte[][][] schedule = new byte[3][][];
            for (int i = 0; i < 3; i++)
            {
                schedule[i] = new byte[16][];
                for (int j = 0; j < 16; j++)
                {
                    schedule[i][j] = new byte[6];
                }
            }
            DESHelper.TripleDESKeySetup(QQKey, schedule, DESHelper.DECRYPT);
            for (int i = 0; i < encryptedTextByte.Length; i += 8)
            {
                var temp = new byte[8];
                DESHelper.TripleDESCrypt(encryptedTextByte[i..], temp, schedule);
                for (int j = 0; j < 8; j++)
                {
                    data[i + j] = temp[j];
                }
            }

            var unzip = SharpZipLibDecompress(data);
            var result = Encoding.UTF8.GetString(unzip);
            return result;
        }

        protected static byte[] SharpZipLibDecompress(byte[] data)
        {
            var compressed = new MemoryStream(data);
            var decompressed = new MemoryStream();
            var inputStream = new InflaterInputStream(compressed);

            inputStream.CopyTo(decompressed);

            return decompressed.ToArray();
        }
    }
}