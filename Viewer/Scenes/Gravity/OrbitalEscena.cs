using Core.Fields.Electromagnetic;
using Core.Fields.Gravity;
using Core.Forces.Gravity;
using Core.Particulas;
using LfvbPhysics;

namespace Viewer.Scenes.Gravity
{
    public class OrbitalEscena:GeneralScena
    {

        public Core.Maths.Vector3D G = new(0, 0, 0);
        public Core.Maths.Vector3D E = new(0, 0, 0);   // campo eléctrico
        public Core.Maths.Vector3D B = new(0, 0, 0);     // campo magnético

        public OrbitalEscena(World world)
        {
            this.world = world;

            this.world.Campos.Add(new UniformMagnetic(B));  
            this.world.Campos.Add(new UniformElectric(E));  
            this.world.Campos.Add(new UniformGravity(G));

            this.Reset();
        }

        public override void Reset()
        {
            Time = 0;
            this.world.Particulas.Clear();

            
            this.world.Particulas.Add(new Particula { Nombre = "SOL", Masa = 1e11, Carga = 1, Posicion = new Core.Maths.Vector3D(0, 0, 0), Velocidad = new Core.Maths.Vector3D(0, 0, 0.02) });

            this.world.Particulas.Add(new Particula { Nombre = "TIERRA", Masa = 100, Carga = -1e-24, Posicion = new Core.Maths.Vector3D(0, 5, 0), Velocidad = new Core.Maths.Vector3D(1, 0, 0) });

            this.world.Particulas.Add(new Particula { Nombre = "MARTE", Masa = 60, Carga = -1e-24, Posicion = new Core.Maths.Vector3D(0, 7, 0), Velocidad = new Core.Maths.Vector3D(0.9, 0, 0) });

            this.world.Particulas.Add(new Particula { Nombre = "MARTE_TRANS", Masa = 60, Carga = -1e-24, Posicion = new Core.Maths.Vector3D(0, 0, 7), Velocidad = new Core.Maths.Vector3D(0, 0.4, 0.4) });

            this.world.Particulas.Add(new Particula { Nombre = "JUPITER", Masa = 40, Carga = -1e-24, Posicion = new Core.Maths.Vector3D(0, 12, 0), Velocidad = new Core.Maths.Vector3D(0.4, 0.1, -0.2) });

            //this.world.Particulas.Add(new Particula { Nombre = "SATURNO", Masa = 30, Carga = -1e-24, Posicion = new Core.Maths.Vector3D(0, 16, 0), Velocidad = new Core.Maths.Vector3D(0.5, 0.2, 0) });

        }

        public override void Step(double dt)
        {
            this.world.Update(dt);
            Time += dt;
        }
    }
}
