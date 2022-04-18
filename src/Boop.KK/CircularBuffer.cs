using System;
using UnityEngine;

namespace Boop
{
    public class CircularBuffer
    {
        public CircularBuffer(int length)
        {
            this._buffer = new Vector3[length];
            this._length = length;
        }

        public void Add(Vector3 obj)
        {
            this._buffer[this._pointer] = obj;
            this._pointer = (this._pointer + 1) % this._length;
        }

        public Vector3 Average()
        {
            Vector3 a = Vector3.zero;
            for (int i = 0; i < this._length; i++)
            {
                a += this._buffer[i];
            }
            return a / (float)this._length;
        }

        private Vector3[] _buffer;

        private readonly int _length;

        private int _pointer = 0;
    }
}
