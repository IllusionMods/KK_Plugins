using UnityEngine;

namespace KK_Plugins
{
    public class CircularBuffer
    {
        private readonly int _length;
        private readonly Vector3[] _buffer;
        private int _pointer;

        public CircularBuffer(int length)
        {
            _buffer = new Vector3[length];
            _length = length;
        }

        public void Add(Vector3 obj)
        {
            _buffer[_pointer] = obj;
            _pointer = (_pointer + 1) % _length;
        }

        public Vector3 Average()
        {
            var a = Vector3.zero;
            for (var i = 0; i < _length; i++) a += _buffer[i];
            return a / _length;
        }
    }
}
