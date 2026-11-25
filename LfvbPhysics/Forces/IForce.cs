using Core.Maths;
using Core.Particulas;

namespace Core.Forces
{
    public interface IForce
    {
        public void ApplyForce(double time, ref Particula p1, ref Particula p2);
    }
}
