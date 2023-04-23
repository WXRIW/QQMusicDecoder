using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace QQMusicDecoder
{
    public unsafe class Decrypter
    {
        private const string HEADER = "[offset:0]\n";

        /// <summary>
        /// 解密 QRC 歌词
        /// </summary>
        /// <param name="encryptedLyrics">加密的歌词</param>
        /// <returns>解密后的 QRC 歌词，失败将返回 null</returns>
        public static string? DecryptLyrics(string encryptedLyrics)
        {
            // 从Hex String转bytes
            var bytes = Convert.FromHexString(encryptedLyrics);

            // 补一下[offset:0]头
            var source = new byte[bytes.Length + HEADER.Length];
            var len = Encoding.ASCII.GetBytes(HEADER, source);
            bytes.CopyTo(source, len);

            // 固定数组内存，获取指针
            var memory = new Memory<byte>(source);
            using var handle = memory.Pin();
            IntPtr result = default;
            string text = string.Empty;
            try
            {
                // 拿到结果
                switch (RuntimeInformation.ProcessArchitecture)
                {
                    case Architecture.X86:
                        result = ExternalDecrypter.QrcDecode(new IntPtr(handle.Pointer), memory.Length);
                        break;
                    case Architecture.X64:
                        result = ExternalDecrypter64.QrcDecode(new IntPtr(handle.Pointer), memory.Length);
                        break;
                    case Architecture.Arm64:
                        result = ExternalDecrypterArm64.QrcDecode(new IntPtr(handle.Pointer), memory.Length);
                        break;
                    default:
                        result = ExternalDecrypter.QrcDecode(new IntPtr(handle.Pointer), memory.Length);
                        break;
                }
                if (result != IntPtr.Zero)
                {
                    // 此时传入的内存已经被解密，裁剪掉[offset:0]头
                    var bytes2 = memory[HEADER.Length..];

                    // 解压缩
                    var unzip = SharpZipLibDecompress(bytes2);

                    text = Encoding.UTF8.GetString(unzip);
                }
            }
            finally
            {
                Marshal.FreeHGlobal(result);
            }

            if (text == string.Empty)
            {
                return null;
            }

            return text;
        }

        protected static byte[] SharpZipLibDecompress(Memory<byte> data)
        {
            using var compressed = new MemoryStream();
            using var decompressed = new MemoryStream();
            using var inputStream = new InflaterInputStream(compressed);
            compressed.Write(data.Span);
            compressed.Seek(0, SeekOrigin.Begin);

            inputStream.CopyTo(decompressed);
            return decompressed.ToArray();
        }
    }

    internal class ExternalDecrypter
    {
        [DllImport("LyricDecoder.dll", EntryPoint = "qrcdecode", PreserveSig = true, ExactSpelling = false)]
        public unsafe static extern IntPtr QrcDecode(IntPtr src, int src_len);
    }

    internal class ExternalDecrypter64
    {
        [DllImport("LyricDecoder64.dll", EntryPoint = "qrcdecode", PreserveSig = true, ExactSpelling = false)]
        public unsafe static extern IntPtr QrcDecode(IntPtr src, int src_len);
    }

    internal class ExternalDecrypterArm64
    {
        [DllImport("LyricDecoderArm64.dll", EntryPoint = "qrcdecode", PreserveSig = true, ExactSpelling = false)]
        public unsafe static extern IntPtr QrcDecode(IntPtr src, int src_len);
    }
}
