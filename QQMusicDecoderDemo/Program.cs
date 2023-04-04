namespace QQMusicDecoderDemo
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var result = QQMusicDecoder.Helper.GetLyrics("229660624");
            //var result = QQMusicDecoder.Helper.GetLyricsByMid("003F1P942q4lEs");
            Console.WriteLine(result?.Lyrics);
            Console.ReadKey();
        }
    }
}