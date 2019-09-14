using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GmodItemExtractor
{
    // ReSharper disable once InconsistentNaming
    internal struct studiohdr_t
    {
        public int Id;
        public int Version;
        public char[] Name;
        public int Datalength;
        public int Flags;
    }

    internal class Mdl
    {
        private static void ReadTextures(BinaryReader reader, ICollection<string> output, int offset, int count)
        {
            reader.BaseStream.Seek(offset, SeekOrigin.Begin);

            int diroffset = reader.ReadInt32();

            reader.BaseStream.Seek(offset + diroffset, SeekOrigin.Begin);
            for (int i = 0; i < count; i++)
            {
                output.Add(ReadNullTerminatedString(reader));
            }
        }

        private static string ReadNullTerminatedString(BinaryReader reader)
        {
            StringBuilder sb = new StringBuilder();
            while (true)
            {
                char ch = reader.ReadChar();
                if (ch == char.MinValue)
                    break;

                sb.Append(ch);
            }

            return sb.ToString();
        }

        private static void ReadTextureDirs(BinaryReader reader, ICollection<string> output, int offset, int count)
        {
            reader.BaseStream.Seek(offset, SeekOrigin.Begin);

            List<int> offsets = new List<int>();
            for (int i = 0; i < count; i++)
            {
                offsets.Add(reader.ReadInt32());
            }

            foreach (int diroffset in offsets)
            {
                reader.BaseStream.Seek(diroffset, SeekOrigin.Begin);
                output.Add(ReadNullTerminatedString(reader));
            }
        }

        public static void Read(BinaryReader reader, ICollection<string> texoutput, ICollection<string> diroutput)
        {
            // ReSharper disable once UseObjectOrCollectionInitializer
            studiohdr_t mdl = new studiohdr_t(); // Create a new struct

            //Start of reading, we're at the first byte

            mdl.Id = reader.ReadInt32(); // Read ID (not used by us)
            mdl.Version = reader.ReadInt32(); // Read Version (not used by us)

            reader.ReadInt32(); // Unknown

            mdl.Name = reader.ReadChars(64); // Read Name (not used by us)
            mdl.Datalength = reader.ReadInt32(); // Read Datalength (not used by us)
            //Console.WriteLine("(Version: " + mdl.Version + ")");

            reader.ReadBytes(72); // Some vector shit (not used by us)

            mdl.Flags = reader.ReadInt32(); // Read Flags (not used by us)

            reader.ReadBytes(48); // Unknown

            int texcount = reader.ReadInt32(); // Reads the amount of textures
            int texoffset = reader.ReadInt32(); // Reads the offset where we can find the textures

            int texdircount = reader.ReadInt32(); // Reads the amount of texturedirectories
            int texdiroffset = reader.ReadInt32(); // Read the offset where we can find texturedirectories

            ReadTextures(reader, texoutput, texoffset, texcount);
            ReadTextureDirs(reader, diroutput, texdiroffset, texdircount);
        }
    }

    // ReSharper disable once InconsistentNaming
    public struct MDLFiles
    {
        public string[] Paths;
        public string[] FileNames;
    }

    // ReSharper disable once InconsistentNaming
    public class MDLInfo
    {
        public static MDLFiles GetInfo(string path)
        {
            List<string> texOutput = new List<string>();
            List<string> dirOutput = new List<string>();
            using (BinaryReader reader = new BinaryReader(File.Open(path, FileMode.Open)))
            {
                Mdl.Read(reader, texOutput, dirOutput);
            }

            return new MDLFiles
            {
                Paths = dirOutput.ToArray(),
                FileNames = texOutput.ToArray()
            };
        }

        public static MDLFiles GetInfo(byte[] mdlFile)
        {
            List<string> texOutput = new List<string>();
            List<string> dirOutput = new List<string>();
            using (MemoryStream stream = new MemoryStream(mdlFile))
            {
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    Mdl.Read(reader, texOutput, dirOutput);
                }
            }

            return new MDLFiles
            {
                Paths = dirOutput.ToArray(),
                FileNames = texOutput.ToArray()
            };
        }
    }
}