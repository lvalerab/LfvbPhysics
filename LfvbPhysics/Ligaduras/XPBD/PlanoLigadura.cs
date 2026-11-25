using Core.Maths;
using Core.Particulas;
using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Ligaduras.XPBD
{
    public class PlanoLigadura: ILigadura
    {
        Particula p;
        Vector3D normal;
        double offset;
        double radius;

        public PlanoLigadura(Particula p, Vector3D normal, double offset, double radius)
        {
            this.p = p;
            this.normal = normal;
            this.offset = offset;
            this.radius = radius;
        }


        public void Apply(double time)
        {
            double C = Vector3D.Dot(normal, p.Predicted) - offset - radius;
            if(C < 0)
            {
                double w = p.InverseMasa;
                if (w == 0)
                    return;
                double deltaLambda = -C / w;
                p.Predicted += deltaLambda * normal * w;
            }
        }
        public void Reset()
        {
           
        }
    }    
}
