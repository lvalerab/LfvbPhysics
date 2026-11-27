using Core.Fields.Electromagnetic;
using Core.Fields.Gravity;
using Core.Maths;
using Core.Particulas;
using LfvbPhysics;
using System;
using System.Collections.Generic;
using System.Text;

namespace Viewer.Scenes.Basicas
{
    class TiroParabolicoParticulaScene:GeneralScena
    {
        public readonly List<Particula> Particulas = new();
        public Core.Maths.Vector3D Gravity = new(0,-9.8,0);
        public Core.Maths.Vector3D E = new(0,0,0);   // campo eléctrico
        public Core.Maths.Vector3D B = new(0,0, 0);     // campo magnético
        
        public TiroParabolicoParticulaScene(World world)
        {
            this.world = world;
            this.world.Campos.Add(new UniformMagnetic(B));
            this.world.Campos.Add(new UniformElectric(E));
            this.world.Campos.Add(new UniformGravity(Gravity));
            Reset();
        }

        public TiroParabolicoParticulaScene() => Reset();

        public override void Reset()
        {
            Time = 0;
            Particulas.Clear();

            // Cargas positivas: giran en un sentido
            Particulas.Add(new Particula { Nombre="P1", Masa = 0.25, Carga = 0, Posicion = new Core.Maths.Vector3D(-1, 0 , 0), Velocidad = new Core.Maths.Vector3D(7, 7, 0) });

            Particulas.Add(new Particula { Nombre = "P2", Masa = 0.5, Carga = 0, Posicion = new Core.Maths.Vector3D(0, 0, 0), Velocidad = new Core.Maths.Vector3D(10, 7, 0) });

            Particulas.Add(new Particula { Nombre = "P3", Masa = 1, Carga = 0, Posicion = new Core.Maths.Vector3D(1, 0, 0), Velocidad = new Core.Maths.Vector3D(10, 10, 0) });


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
