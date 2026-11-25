using Core.Maths;
using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Particulas
{
    public class Particula
    {

        public string Nombre { get; set; } = "Particula";
        public double Masa { get; set; }
        public double Carga { get; set; }   

        public Vector3D Posicion { get; set; }

        public Vector3D Velocidad { get; set; } 

        public Vector3D Aceleracion { get; set; }


        //Para las ligaduras XPBD (Extended Position Based Dynamics)
        public bool IsFixed { get; set; } = false;

        public Vector3D Predicted { get; set; }

        public double InverseMasa
        {
            get => this.IsFixed ? 0 : 1 / Masa;
        }
    }
}
