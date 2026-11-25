using Core.Maths;
using Core.Particulas;
using Core.Fields;

namespace Core.Fields.Electromagnetic
{
    public class UniformElectric:IField
    {
        public Vector3D campoElectrico { get; set; }

        public UniformElectric(Vector3D campoElectrico)
        {
            this.campoElectrico = campoElectrico;
        }

        public Vector3D Force(Particula particula1, double time)
        {
            // Assuming electric field acts in the direction of the field vector
            // Force = charge * electric field
            // For simplicity, let's assume the particle has a charge property (not shown in the Particula class)
            // Here we will just return the force as if the particle has a unit charge
            Vector3D fuerza=particula1.Carga * campoElectrico;
            Console.WriteLine($"Fuerza eléctrica por campo uniforme aplicada a {particula1.Nombre}: {fuerza}");
            return fuerza; // This is a simplification; normally you'd multiply by charge
        }

    }
}
