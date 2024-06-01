using System.IO;

namespace LibAPNG
{
    public class IENDChunk : Chunk
    {
        public IENDChunk(byte[] bytes)
            : base(bytes)
        {
        }

        public IENDChunk(MemoryStream ms)
            : base(ms)
        {
        }

        public IENDChunk(Chunk chunk)
            : base(chunk)
        {
        }
    }
}