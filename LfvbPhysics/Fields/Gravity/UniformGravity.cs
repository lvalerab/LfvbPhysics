using Core.Maths;
using Core.Particulas;
using Core.Fields;

namespace Core.Fields.Gravity
{
    public class UniformGravity:IField
    {
        public Vector3D gravedad { get; set; }

        public UniformGravity(Vector3D gravedad)
        {
            this.gravedad = gravedad;
        }


        public Vector3D Force(Particula particula1, double time)
        {
            // Assuming gravity acts in the negative Y direction
             // Acceleration due to gravity in m/s^2
            return particula1.Masa * gravedad;
        }
    }
}
