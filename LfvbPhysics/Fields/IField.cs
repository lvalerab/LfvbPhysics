using Core.Maths;
using Core.Particulas;
using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Fields
{
    public interface IField
    {
        Vector3D Force(Particula particula1, double time);
    }
}
