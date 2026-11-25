using Core.Maths;
using Core.Particulas;

namespace Core.Forces.Electromagnetic
{
    public class Magnetica: IForce
    {


        public void ApplyForce(double time, ref Particula p1, ref Particula p2)
        {
            //La fuerza magnética es perpendicular a la velocidad de la partícula y al campo magnético, que genera 
            //la particula 2 relativa a la particula 1, por lo que se puede calcular usando el producto vectorial

            //Calcular la velocidad relativa de la particula 1 respecto a la particula 2
            Vector3D v_relativa = p1.Velocidad - p2.Velocidad;
            //Calcular el vector que representa la dirección del campo magnético generado por la particula 2
            Vector3D r = p1.Posicion - p2.Posicion;
            if(r.Norma() == 0)
            {
                return; // Evitar división por cero si las partículas están en la misma posición
            }
            Vector3D B = Vector3D.Normalize(r);
            //Calcular la magnitud de la fuerza magnética usando la ley de Lorentz
            double fuerzaMagnitud = p1.Carga * v_relativa.Norma() * B.Norma();
            //Calcular la dirección de la fuerza magnética usando el producto vectorial
            Vector3D direccion = Vector3D.Cross(v_relativa, B);
            if(direccion.Norma() == 0)
            {
                return; // Evitar aplicar fuerza si la dirección es cero
            }
            direccion = Vector3D.Normalize(direccion);
            //Calcular la fuerza vectorial
            Vector3D fuerza = direccion * fuerzaMagnitud;
            //Aplicar la fuerza a las partículas (Ley de Newton)
            p1.Aceleracion += fuerza / p1.Masa;
            //p2.Aceleracion -= fuerza / p2.Masa;
            Console.WriteLine($"Fuerza magnética aplicada entre {p1.Nombre} y {p2.Nombre}: {fuerza}");
        }
    }
}
