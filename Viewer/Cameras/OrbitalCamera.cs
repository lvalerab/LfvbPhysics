using Silk.NET.Core.Native;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Viewer.Cameras
{
    public class OrbitalCamera :Camera
    {
        public float Yaw { get; set; } = 0.8f;
        public float Pitch { get; set; } = 0.5f;    
        public float Distance { get; set; } = 14.0f;
        
        public override Vector3 Target { get; set; } = Vector3.Zero;

        public override Matrix4x4 View()
        {
           var dir = new Vector3(
           MathF.Cos(Pitch) * MathF.Cos(Yaw),
           MathF.Sin(Pitch),
           MathF.Cos(Pitch) * MathF.Sin(Yaw));
           return Matrix4x4.CreateLookAt(Target + Distance * dir, Target, Vector3.UnitY);
        }
    }
}
