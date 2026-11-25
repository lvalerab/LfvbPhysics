using Core.Maths;
using Core.Particulas;
using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Ligaduras.XPBD
{
    public class LigaduraDistancia : ILigadura
    {
        double lambda = 0;
        Particula p1, p2;

        double distancia;
        double alpha;

        public LigaduraDistancia(Particula p1, Particula p2, double distancia, double alpha=0)
        {
            this.p1 = p1;
            this.p2 = p2;
            this.distancia = distancia;
            this.alpha = alpha;
        }

        public void Reset() => this.lambda = 0;

        public void Apply(double time)
        {
            var delta = p2.Predicted - p1.Predicted;
            var distanciaActual = delta.Norma();
            var distanciaDeseada = (p2.Posicion - p1.Posicion).Norma();            
            var w1 = p1.InverseMasa;
            var w2 = p2.InverseMasa;
            var wSum = w1 + w2;
            if (wSum == 0)
                return;            
            var deltaLambda = (-this.distancia - this.alpha * lambda) / wSum;
            lambda += deltaLambda;
            var correction = deltaLambda * Vector3D.Normalize(delta);
            p1.Predicted -= correction * w1;
            p2.Predicted += correction * w2;
        }

        
    }
}
