using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Maths
{
    public class Vector3D(double x, double y, double z)
    {
        public double X=x, Y= y, Z = z;
        /******************************************
         * Operaciones de vectores
         * 
         *****************************************/

        public static Vector3D operator +(Vector3D a, Vector3D b)
        {
            return new Vector3D(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        }

        public static Vector3D operator -(Vector3D a, Vector3D b)
        {
            return new Vector3D(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        }

        public static Vector3D operator *(Vector3D a, double b)
        {
            return new Vector3D(a.X * b, a.Y * b, a.Z * b);
        }

        public static Vector3D operator *(double b, Vector3D a)
        {
            return new Vector3D(b * a.X, b * a.Y, b * a.Z);
        }

        public static Vector3D operator /(Vector3D a, double b)
        {
            return new Vector3D(a.X / b, a.Y / b, a.Z / b);
        }

        public static double Dot(Vector3D a, Vector3D b)
        {
            return a.X * b.X + a.Y * b.Y + a.Z * b.Z;
        }

        public static Vector3D Cross(Vector3D a, Vector3D b)
        {
            return new Vector3D(
                a.Y * b.Z - a.Z * b.Y,
                a.Z * b.X - a.X * b.Z,
                a.X * b.Y - a.Y * b.X
            );
        }

        public double Norma()
        {
            return Math.Sqrt(X * X + Y * Y + Z * Z);
        }

        public static Vector3D Normalize(Vector3D a)
        {
            double norm = a.Norma();
            if (norm == 0) throw new InvalidOperationException("Cannot normalize a zero vector.");
            return new Vector3D(a.X / norm, a.Y / norm, a.Z / norm);
        }

        public static Vector3D Zero => new Vector3D(0, 0, 0);

        public static double[][] ToMatrix(Vector3D v)
        {
            return new double[][]
            {
                new double[] { v.X },
                new double[] { v.Y },
                new double[] { v.Z }
            };
        }
     
        override public string ToString()
        {
            return $"(<{X}, {Y}, {Z}>)";
        }
    }
}
