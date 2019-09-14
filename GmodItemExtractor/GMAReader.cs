using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GmodItemExtractor
{
    class GMAReader : BinaryReader
    {
        public GMAReader(Stream input) : base(input)
        {
        }

        public GMAReader(Stream input, Encoding encoding) : base(input, encoding)
        {
        }

        public GMAReader(Stream input, Encoding encoding, bool leaveOpen) : base(input, encoding, leaveOpen)
        {
        }

        private string ReadNullTerminatedString()
        {
            List<byte> bytes = new List<byte>();
            byte b;
            do
            {
                b = ReadByte();
                bytes.Add(b);
            } while (b != 0x0);

            bytes.RemoveAt(bytes.Count - 1);

            return Encoding.ASCII.GetString(bytes.ToArray());
        }

        public GMAHeader ReadHeader()
        {
            BaseStream.Seek(0, SeekOrigin.Begin);

            // Magic constant "GMAD"
            byte[] magicConstant = ReadBytes(4);
            if (Encoding.ASCII.GetString(magicConstant) != "GMAD")
                throw new Exception("Invalid file type");

            byte gmaVersion = ReadByte();
            if (gmaVersion > 3)
                throw new Exception("Unsupported GMA version " + gmaVersion);

            GMAHeader header = new GMAHeader
            {
                SteamID = ReadUInt64(),
                Timestamp = ReadInt64(),
                RequiredContent = ReadNullTerminatedString(),
                Name = ReadNullTerminatedString(),
                Description = ReadNullTerminatedString(),
                Author = ReadNullTerminatedString(),
                Version = ReadInt32()
            };

            List<GMAFileHeader> files = new List<GMAFileHeader>();
            long curOffset = 0;
            while (ReadUInt32() > 0)
            {
                GMAFileHeader curFile = new GMAFileHeader
                {
                    Path = ReadNullTerminatedString(),
                    Size = ReadInt64(),
                    CRC = ReadUInt32(),
                    FileNumber = files.Count,
                    Offset = curOffset
                };
                curOffset += curFile.Size;
                files.Add(curFile);
            }

            header.DataStart = BaseStream.Position;
            header.Files = files.ToArray();

            return header;
        }
    }

    struct GMAHeader
    {
        public ulong SteamID;
        public long Timestamp;
        public string RequiredContent;
        public string Name;
        public string Description;
        public string Author;
        public int Version;
        public GMAFileHeader[] Files;
        public long DataStart;
    }

    struct GMAFileHeader
    {
        public string Path;
        public long Size;
        public uint CRC;
        public long Offset;
        public int FileNumber;
    }

    class GMAFileHeaderComparer : EqualityComparer<GMAFileHeader>
    {
        public override bool Equals(GMAFileHeader x, GMAFileHeader y)
        {
            return GMAFile.PathEqual(x.Path, y.Path);
        }

        public override int GetHashCode(GMAFileHeader obj)
        {
            return obj.FileNumber;
        }
    }
}