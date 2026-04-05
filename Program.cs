using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace CETGutter
{
    internal class Program
    {
        static void Log(string log)
            => Console.WriteLine($"[CETGutter] {log}");

        static byte[] DecryptXor(byte[] buffer)
        {
            for (int i = 2; i < buffer.Length; i++)
                buffer[i] = (byte)(buffer[i] ^ buffer[i - 2]);

            for (int i = buffer.Length - 2; i >= 0; i--)
                buffer[i] = (byte)(buffer[i] ^ buffer[i + 1]);

            byte keyC = 0xCE;
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = (byte)(buffer[i] ^ keyC);
                keyC = (byte)((keyC + 1) & 0xFF);
            }

            return buffer;
        }

        static byte[] Decompress(byte[] buffer)
        {
            var offset = 0;
            if (Encoding.UTF8.GetString(buffer, 0, 5) == "CHEAT")
                offset = 5;

            using var ms = new MemoryStream(buffer, offset, buffer.Length - offset);
            using var outMs = new MemoryStream();
            using var ds = new DeflateStream(ms, CompressionMode.Decompress);
            ds.CopyTo(outMs);

            return outMs.ToArray();
        }

        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Log("Provide a full path to the file or drag it on top of the executable");
                return;
            }

            var filePath = args[0];
            var xmlFilePath = $"{filePath}.xml";
            var intermediateFilePath = $"{xmlFilePath}.intermediate";

            if (!File.Exists(filePath))
            {
                Log("Specified file not found.");
                return;
            }

            var data = File.ReadAllBytes(filePath);

            if (data.Length >= 5 && Encoding.UTF8.GetString(data, 0, 5) == "<?xml")
            {
                Log("File is already unprotected XML.");
                File.WriteAllBytes(xmlFilePath, data);
                return;
            }

            try
            {
                var buffer = DecryptXor(data);
                File.WriteAllBytes(intermediateFilePath, buffer);

                var decompressedData = Decompress(buffer);
                var asText = Encoding.UTF8.GetString(decompressedData);

                // ignores everything before '<' (as in "<?xml") and processes it like that
                var @return = asText[(asText.IndexOf('<') - 1)..];

                File.WriteAllText(xmlFilePath, @return);

                // TODO decode the XML & CEForms if they exist.

                Log("Success!");
            }
            catch (Exception ex)
            {
                Log($"Error during decompression: {ex.Message}");
                return;
            }
        }
    }
}
