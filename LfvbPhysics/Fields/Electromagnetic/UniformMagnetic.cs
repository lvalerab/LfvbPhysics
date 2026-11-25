using Core.Maths;
using Core.Particulas;
using Core.Fields;

namespace Core.Fields.Electromagnetic
{
    public class UniformMagnetic: IField
    {
        public Vector3D CampoMagnetico { get; set; }
        public UniformMagnetic(Vector3D campoMagnetico)
        {
            this.CampoMagnetico = campoMagnetico;
        }
        public Vector3D Force(Particula particula1, double time)
        {
            // Assuming the magnetic field is uniform and constant
            // The force on a charged particle in a magnetic field is given by F = q * (v x B)
            Vector3D fuerza = particula1.Carga * Vector3D.Cross(particula1.Velocidad, CampoMagnetico);
            Console.WriteLine($"Fuerza magnética por campo uniforme aplicada a {particula1.Nombre}: {fuerza}");
            return fuerza;
        }
    }   
}
