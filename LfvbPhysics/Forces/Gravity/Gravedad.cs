using Core.Maths;
using Core.Particulas;

namespace Core.Forces.Gravity
{
    public class Gravedad: IForce
    {
        public static double G = 6.67430e-11; // Constante de gravitación universal

        public void ApplyForce(double time, ref Particula p1, ref Particula p2)
        {
            // Calcular la distancia entre las partículas
            Vector3D r = p2.Posicion - p1.Posicion;
            double distancia = r.Norma();
            // Evitar división por cero
            if (distancia == 0)
                return;
            // Calcular la magnitud de la fuerza gravitatoria            
            double fuerzaMagnitud = G * (p1.Masa * p2.Masa) / (distancia * distancia);
            // Calcular la dirección de la fuerza
            Vector3D direccion = Vector3D.Normalize(r);
            // Calcular la fuerza vectorial
            Vector3D fuerza = direccion * fuerzaMagnitud;
            // Aplicar la fuerza a las partículas (Newton's Third Law)
            p1.Aceleracion += fuerza / p1.Masa;
            //p2.Aceleracion -= fuerza / p2.Masa;
            Console.WriteLine($"Fuerza gravedad entre {p1.Nombre} y {p2.Nombre}: {fuerza}");
        }
    }    
}
