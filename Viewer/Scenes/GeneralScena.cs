using LfvbPhysics;
using System;
using System.Collections.Generic;
using System.Text;

namespace Viewer.Scenes
{
    public abstract class GeneralScena
    {
        public World world;
        public double Time;

        public abstract void Reset();

        public abstract void Step(double dt);
    }
}
