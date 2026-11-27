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

        public World world;
        public double Time;

        public bool ShowVectors = false;

        public bool ShowEjes = true;


        public abstract void Reset();

        public abstract void Step(double dt);
    }
}
