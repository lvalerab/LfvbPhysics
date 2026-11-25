using Core.Particulas;
using Core.Fields;
using Core.Maths;
using Core.Forces;
using Core.Forces.Gravity;
using Core.Forces.Electromagnetic;
using Core.Fields.Electromagnetic;
using Core.Ligaduras;
using System.Linq.Expressions;

namespace LfvbPhysics
{
    public class World
    {
        public List<Particula> Particulas { get; set; } = new List<Particula>();    
        public List<IField> Campos { get; set; } = new List<IField>();
        public List<IForce> Fuerzas { get; set; } = new List<IForce>();

        public List<ILigadura> Ligaduras { get; set; } = new List<ILigadura>();

        Boolean ApplyUniformFields = true; // Flag to control whether uniform fields are applied
        Boolean ApplyForces = true; // Flag to control whether uniform fields are applied
        Boolean ApplyLigaduras = true; // Flag to control whether uniform fields are applied

        public World(Boolean applyUniformFields = true, Boolean applyForces = true, Boolean applyLigaduras = true)
        {
            // Initialize the world with default values if needed
            //Añadimos las fuerzas basicas
            Fuerzas.Add(new Gravedad());
            Fuerzas.Add(new Electrica());
            Fuerzas.Add(new Magnetica());   
            this.ApplyUniformFields= applyUniformFields;
            this.ApplyForces = applyForces;
            this.ApplyLigaduras = applyLigaduras;
        }

        public void BorisStep(Particula p, Vector3D e, Vector3D b, double dt)
        {
            // Half-step velocity update due to electric field
            Vector3D v_minus = p.Velocidad + (e * (dt / 2.0));
            // Calculate the rotation vector
            Vector3D t = b * (dt / 2.0);
            double t_magnitude_squared = Vector3D.Dot(t, t);
            Vector3D s = t * (2.0 / (1.0 + t_magnitude_squared));
            // Rotate the velocity
            Vector3D v_prime = v_minus + Vector3D.Cross(v_minus, t);
            Vector3D v_plus = v_minus + Vector3D.Cross(v_minus,s);
            // Final half-step velocity update due to electric field
            p.Velocidad = v_plus + (e * (dt / 2.0));
            // Update position
            p.Posicion += p.Velocidad * dt;
        }

        public void LigadurasStep(double timeStep)
        {
           foreach(var p in Particulas)
            {
                if(p.IsFixed)
                {
                    p.Predicted = p.Posicion; // If the particle is fixed, its predicted position is its current position
                }
                else
                {
                    p.Predicted = p.Posicion + p.Velocidad * timeStep; // Predict the new position based on current velocity
                }
            }
            //Reseteamos las lambdas de las ligaduras antes de aplicar los constraints
            foreach (var ligadura in Ligaduras) ligadura.Reset();
            foreach (var ligadura in Ligaduras) ligadura.Apply(timeStep);
            foreach (var p in Particulas)
            {
                if (!p.IsFixed)
                {
                    p.Velocidad = (p.Predicted - p.Posicion) / timeStep; // Update velocity based on the change in position
                    p.Posicion = p.Predicted; // Update position to the predicted position
                }
            }
        }

        public void Update(double timeStep)
        {
            Console.Clear();            
            try
            {
                // Reset accelerations
                foreach (var particula in Particulas)
                {
                    particula.Aceleracion = new Vector3D(0, 0, 0);
                }            
                if(ApplyForces)
                {
                    // Apply pairwise forces
                    for (int i = 0; i < Particulas.Count; i++)
                    {
                        // Apply uniform fields to each particle
                        if (ApplyUniformFields)
                        {
                            foreach (var campo in Campos)
                            {
                                var fuerza = campo.Force(Particulas[i], timeStep);
                                Particulas[i].Aceleracion += fuerza / Particulas[i].Masa;
                            }
                        }
                        Particula a = Particulas[i];
                        for (int j = 0; j < Particulas.Count; j++)
                        {
                            if (i != j)
                            {
                                Particula b = Particulas[j];
                                foreach (var fuerza in Fuerzas)
                                {
                                    fuerza.ApplyForce(timeStep, ref a, ref b);
                                }
                            }
                        }
                    }
                } else
                {
                    // Apply pairwise forces
                    for (int i = 0; i < Particulas.Count; i++)
                    {
                        // Apply uniform fields to each particle
                        if (ApplyUniformFields)
                        {
                            foreach (var campo in Campos)
                            {
                                var fuerza = campo.Force(Particulas[i], timeStep);
                                Particulas[i].Aceleracion += fuerza / Particulas[i].Masa;
                            }
                        }
                    }
                 }
               
                // Update positions and velocities
                int pi = 0;
                foreach (var particula in Particulas)
                {
                    particula.Velocidad += particula.Aceleracion * timeStep;
                    particula.Posicion += particula.Velocidad * timeStep;
                    bool isInEMField = false;
                    foreach (var campo in Campos)
                    {
                        if (campo is UniformElectric)
                        {
                            isInEMField = true;
                            break;
                        }
                    }
                    if (isInEMField)
                    {
                        // Apply Boris method for electromagnetic fields
                        Vector3D e = new Vector3D(0, 0, 0);
                        Vector3D b = new Vector3D(0, 0, 0);
                        foreach (var campo in Campos)
                        {
                            if (campo is UniformElectric ue)
                            {
                                e += ue.campoElectrico;
                            }
                            else if (campo is UniformMagnetic um)
                            {
                                b += um.CampoMagnetico;
                            }
                        }
                        //this.BorisStep(particula, e, b, timeStep);
                    }
                    // Update position and velocity using simple Euler integration
                    particula.Velocidad += particula.Aceleracion * timeStep;
                    particula.Posicion += particula.Velocidad * timeStep;
                    if(ApplyLigaduras)
                    {
                        LigadurasStep(timeStep); // Apply constraints after updating positions
                    }
                    Console.WriteLine($"Particula {particula.Nombre} Posicion = {particula.Posicion}, Velocidad = {particula.Velocidad}, Aceleracion = {particula.Aceleracion}");
                    pi++;
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during world update: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }

    }
}
