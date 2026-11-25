using Core.Maths;
using Core.Particulas;

namespace Core.Forces.Electromagnetic
{
    public class Electrica: IForce
    {

        public static double k = 8.9875517923e9; // Constante de Coulomb

        public void ApplyForce(double time, ref Particula p1, ref Particula p2)
        {
            // Calcular la distancia entre las partículas
            Vector3D r = p2.Posicion - p1.Posicion;
            double distancia = r.Norma();
            // Evitar división por cero
            if (distancia == 0)
                return;
            // Calcular la magnitud de la fuerza eléctrica            
            double fuerzaMagnitud = k * (p1.Carga * p2.Carga) / (distancia * distancia);
            // Calcular la dirección de la fuerza
            Vector3D direccion = Vector3D.Normalize(r);
            // Aplicar la fuerza a las partículas
            Vector3D fuerza = fuerzaMagnitud * direccion ;
            p1.Aceleracion += (fuerza / p1.Masa);
            //p2.Aceleracion -= fuerza / p2.Masa;
            Console.WriteLine($"Fuerza electrica aplicada a {p1.Nombre} por {p2.Nombre}: {fuerza}, direccion: {direccion}, atraccion: {p1.Carga*p2.Carga<=0}");
        }
    }   
}
