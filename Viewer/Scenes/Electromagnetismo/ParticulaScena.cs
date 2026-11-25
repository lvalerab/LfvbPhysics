using Core.Fields.Electromagnetic;
using Core.Fields.Gravity;
using Core.Maths;
using Core.Particulas;
using LfvbPhysics;
using System;
using System.Collections.Generic;
using System.Text;

namespace Viewer.Scenes.Electromagnetismo
{
    class ParticulaScene:GeneralScena
    {
        public readonly List<Particula> Particulas = new();
        public Core.Maths.Vector3D Gravity = Vector3D.Zero;
        public Core.Maths.Vector3D E = new(0, 0, 0);   // campo eléctrico
        public Core.Maths.Vector3D B = new(0, 0, 0);     // campo magnético
        
        public ParticulaScene(World world)
        {
            this.world = world;
            Reset();
        }

        public ParticulaScene() => Reset();

        public override void Reset()
        {
            Time = 0;
            Particulas.Clear();

            // Cargas positivas: giran en un sentido
            Particulas.Add(new Particula {Nombre="P1POS", Masa = 100, Carga = 1e-5, Posicion = new Core.Maths.Vector3D(0, 1, 0), Velocidad = new Core.Maths.Vector3D(0, 0, 0) });
            //Particulas.Add(new Particula { Masa = 1, Carga = +1, Posicion = new Core.Maths.Vector3D(0, 0, 0), Velocidad = new Core.Maths.Vector3D(1, 0, 0) });
            //Particulas.Add(new Particula { Masa = 2, Carga = +1, Posicion = new Core.Maths.Vector3D(0, 0, 0), Velocidad = new Core.Maths.Vector3D(2, -0.3, 0) });

            // Cargas negativas: giran en sentido contrario
            Particulas.Add(new Particula { Nombre = "P2NEG", Masa = 100, Carga = -1e-5, Posicion = new Core.Maths.Vector3D(0, -1, 0), Velocidad = new Core.Maths.Vector3D(0, 0, 0) });
            //Particulas.Add(new Particula { Masa = 0.5, Carga = -1, Posicion = new Core.Maths.Vector3D(0, 0, 0), Velocidad = new Core.Maths.Vector3D(1.5, 0, 0) });

            // Neutra: no siente B ni E, va en línea recta
            //Particulas.Add(new Particula {Nombre="Neutra", Masa = 1, Carga = 0, Posicion = new Core.Maths.Vector3D(1, 1, 1), Velocidad = new Core.Maths.Vector3D(0, 0, 0) });

            this.world.Particulas = Particulas;
            //this.world.Campos.Add(new UniformMagnetic(B));
            //this.world.Campos.Add(new UniformElectric(E));
            //this.world.Campos.Add(new UniformGravity(Gravity));
        }

        public override void Step(double dt)
        {
            this.world.Update(dt);
            Time += dt;
        }
        
    }
}
