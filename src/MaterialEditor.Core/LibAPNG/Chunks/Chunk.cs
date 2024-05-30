using System;
using System.IO;
using System.Text;

namespace LibAPNG
{
    public class Chunk
    {
        internal Chunk()
        {
            Length = 0;
            ChunkType = String.Empty;
            ChunkData = null;
            Crc = 0;
        }

        internal Chunk(byte[] bytes)
        {
            var ms = new MemoryStream(bytes);
            Length = Helper.ConvertEndian(ms.ReadUInt32());
            ChunkType = Encoding.ASCII.GetString(ms.ReadBytes(4));
            ChunkData = ms.ReadBytes((int)Length);
            Crc = Helper.ConvertEndian(ms.ReadUInt32());

            if (ms.Position != ms.Length)
                throw new Exception("Chunk length not correct.");
            if (Length != ChunkData.Length)
                throw new Exception("Chunk data length not correct.");

            ParseData(new MemoryStream(ChunkData));
        }

        internal Chunk(MemoryStream ms)
        {
            Length = Helper.ConvertEndian(ms.ReadUInt32());
            ChunkType = Encoding.ASCII.GetString(ms.ReadBytes(4));
            ChunkData = ms.ReadBytes((int)Length);
            Crc = Helper.ConvertEndian(ms.ReadUInt32());

            ParseData(new MemoryStream(ChunkData));
        }

        internal Chunk(Chunk chunk)
        {
            Length = chunk.Length;
            ChunkType = chunk.ChunkType;
            ChunkData = chunk.ChunkData;
            Crc = chunk.Crc;

            ParseData(new MemoryStream(ChunkData));
        }

        public uint Length { get; set; }

        public string ChunkType { get; set; }

        public byte[] ChunkData { get; set; }

        public uint Crc { get; set; }

        /// <summary>
        ///     Get raw data of the chunk
        /// </summary>
        public byte[] RawData
        {
            get
            {
                var ms = new MemoryStream();
                ms.WriteUInt32(Helper.ConvertEndian(Length));
                ms.WriteBytes(Encoding.ASCII.GetBytes(ChunkType));
                ms.WriteBytes(ChunkData);
                ms.WriteUInt32(Helper.ConvertEndian(Crc));

                return ms.ToArray();
            }
        }

        /// <summary>
        ///     Modify the ChunkData part.
        /// </summary>
        public void ModifyChunkData(int postion, byte[] newData)
        {
            Array.Copy(newData, 0, ChunkData, postion, newData.Length);

            using (var msCrc = new MemoryStream())
            {
                msCrc.WriteBytes(Encoding.ASCII.GetBytes(ChunkType));
                msCrc.WriteBytes(ChunkData);

                Crc = CrcHelper.Calculate(msCrc.ToArray());
            }
        }

        /// <summary>
        ///     Modify the ChunkData part.
        /// </summary>
        public void ModifyChunkData(int postion, uint newData)
        {
            ModifyChunkData(postion, BitConverter.GetBytes(newData));
        }

        protected virtual void ParseData(MemoryStream ms)
        {
        }
    }
}