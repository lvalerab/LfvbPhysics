using System;
using System.Collections.Generic;
using System.Text;

namespace Viewer.Cameras
{
    public abstract class Camera
    {
        public abstract System.Numerics.Vector3 Target { get; set; }
        public abstract System.Numerics.Matrix4x4 View();
    }
}
