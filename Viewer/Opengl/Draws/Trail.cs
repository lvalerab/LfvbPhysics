using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Viewer.Opengl.Draws
{
    sealed class Trail
    {
        readonly Vector3[] buf;
        int head, count;
        public Trail(int capacity) => buf = new Vector3[capacity];
        public int Count => count;
        public void Add(Vector3 p)
        {
            buf[head] = p;
            head = (head + 1) % buf.Length;
            if (count < buf.Length) count++;
        }
        public Vector3 this[int i] => buf[(head - count + i + buf.Length) % buf.Length]; // 0 = el más antiguo
    }
}
