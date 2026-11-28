using LfvbPhysics;
using System;
using System.Collections.Generic;
using System.Text;

namespace Viewer.Scenes
{
    public abstract class GeneralScena
    {

        public const float VectorVelocityScale = 0.5f;
        public const float VectorAccelerationScale = 0.5f;

        public int CapacidadEstela = 1500; // cantidad de puntos que se guardan para dibujar la estela de cada partícula

        public int DibujarEstelaCada = 4; // dibujar la estela cada N pasos de simulación

        public bool ShowNames = false;

        public bool ShowVectors = false;

        public bool ShowEjes = true;

        public bool ShowEstelas = true;

        public bool ShowGrids = true;

        public bool ShowCampos = true;

        public int PuntosDesdeElOrigenGrids = 10;    // puntos desde el origen hacia cada lado
        public float SeparacionPuntosGrids = 1f;     // separación entre puntos (unidades del mundo)

        public World world;
        public double Time;




        public abstract void Reset();

        public abstract void Step(double dt);
    }
}
