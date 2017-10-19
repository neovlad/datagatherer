using System.IO;

namespace datagatherer
{
    class Utils
    {
        public static bool isCommonPrinter(string printer)
        {
            if (printer.Trim().ToLower().Equals("fax") ||
                printer.Trim().ToLower().Equals("microsoft xps document writer") ||
                printer.Trim().ToLower().Equals("send to onenote 16") ||
                printer.Trim().ToLower().Equals("microsoft print to pdf"))
            {
                return true;
            }
            return false;
        }


        
        public static void GenerateStreamFromString(string s, Stream stream)
        {
            
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;

        }
    }
}
