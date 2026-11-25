using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Ligaduras
{
    public interface ILigadura
    {
        void Reset();
        void Apply(double time);
    }
}
