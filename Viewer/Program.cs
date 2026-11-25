// Physics.Viewer / Program.cs
// Requiere Physics.Core (Vec3, Particle) y los paquetes Silk.NET.Windowing, Silk.NET.OpenGL, Silk.NET.Input.
// En el .csproj: <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
// Si pusiste Physics.Core dentro de un namespace, añade: using Physics.Core;

using Core.Particulas;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using System.Numerics;
using System.Runtime.InteropServices;
using Viewer.Cameras;
using Viewer.Scenes;
using Viewer.Scenes.Gravity;

new App().Run();




// ------------------------------------------------------------------
// Aplicación: ventana, OpenGL, entrada y bucle
// ------------------------------------------------------------------
internal unsafe class App
{
    const string VertexSrc = """
        #version 330 core
        layout(location = 0) in vec3 aPos;      // vértice de la esfera unidad
        layout(location = 1) in vec3 iPos;      // posición de la instancia
        layout(location = 2) in vec3 iColor;    // color de la instancia
        layout(location = 3) in float iRadius;  // radio de la instancia
        uniform mat4 uVP;
        out vec3 vNormal;
        out vec3 vColor;
        void main()
        {
            vNormal = aPos;
            vColor = iColor;
            gl_Position = uVP * vec4(iPos + aPos * iRadius, 1.0);
        }
        """;

    const string FragmentSrc = """
        #version 330 core
        in vec3 vNormal;
        in vec3 vColor;
        out vec4 FragColor;
        void main()
        {
            vec3 n = normalize(vNormal);
            float d = max(dot(n, normalize(vec3(0.4, 0.8, 0.5))), 0.0);
            FragColor = vec4(vColor * (0.25 + 0.75 * d), 1.0);
        }
        """;

    const double Dt = 1.0 / 240.0;   // paso fijo de física
    const int FloatsPerInstance = 7; // pos(3) + color(3) + radio(1)

    readonly IWindow window;
    //readonly ParticulaScene scene = new(new LfvbPhysics.World(true,true,false));

    readonly GeneralScena scena = new OrbitalEscena(new LfvbPhysics.World(true, true, false));
    readonly OrbitalCamera camera = new OrbitalCamera();

    GL gl = null!;
    uint program, vao, sphereVbo, ebo, instanceVbo;
    int vpLocation, indexCount;
    float[] instanceData = Array.Empty<float>();

    double accumulator, timeScale = 1;
    bool paused;
    Vector2 lastMouse;
    bool hasLastMouse;

    public App()
    {
        var options = WindowOptions.Default with
        {
            Size = new Vector2D<int>(1280, 720),
            Title = "LFVB Physics Viewer",
            API = new GraphicsAPI(ContextAPI.OpenGL, ContextProfile.Core,
                                  ContextFlags.ForwardCompatible, new APIVersion(3, 3))
        };
        window = Window.Create(options);
        window.Load += OnLoad;
        window.Update += OnUpdate;
        window.Render += OnRender;
        window.FramebufferResize += s => gl.Viewport(0, 0, (uint)s.X, (uint)s.Y);
        window.Closing += OnClosing;
    }

    public void Run() => window.Run();

    void OnLoad()
    {
        gl = window.CreateOpenGL();
        gl.Enable(EnableCap.DepthTest);

        SetupInput();
        program = BuildProgram(VertexSrc, FragmentSrc);
        vpLocation = gl.GetUniformLocation(program, "uVP");
        SetupGeometry();
    }

    void SetupInput()
    {
        var input = window.CreateInput();

        foreach (var kb in input.Keyboards)
        {
            kb.KeyDown += (_, key, _) =>
            {
                switch (key)
                {
                    case Key.Space: paused = !paused; break;
                    case Key.R: scena.Reset(); accumulator = 0; break;
                    case Key.Up: timeScale = Math.Min(timeScale * 2, 16); break;
                    case Key.Down: timeScale = Math.Max(timeScale / 2, 1.0 / 16); break;
                    case Key.Escape: window.Close(); break;
                }
            };
        }

        foreach (var mouse in input.Mice)
        {
            mouse.MouseMove += (m, pos) =>
            {
                if (hasLastMouse && m.IsButtonPressed(MouseButton.Left))
                {
                    var d = pos - lastMouse;
                    camera.Yaw += d.X * 0.005f;
                    camera.Pitch = Math.Clamp(camera.Pitch + d.Y * 0.005f, -1.5f, 1.5f);
                }
                lastMouse = pos;
                hasLastMouse = true;
            };
            mouse.Scroll += (_, wheel) =>
                camera.Distance = Math.Clamp(camera.Distance * MathF.Pow(0.9f, wheel.Y), 2f, 150f);
        }
    }

    void SetupGeometry()
    {
        var (vertices, indices) = BuildSphere(16, 32);
        indexCount = indices.Length;

        vao = gl.GenVertexArray();
        gl.BindVertexArray(vao);

        // Malla de la esfera (atributo 0)
        sphereVbo = gl.GenBuffer();
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, sphereVbo);
        gl.BufferData<float>(BufferTargetARB.ArrayBuffer, vertices, BufferUsageARB.StaticDraw);
        gl.EnableVertexAttribArray(0);
        gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), (void*)0);

        ebo = gl.GenBuffer();
        gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, ebo);
        gl.BufferData<uint>(BufferTargetARB.ElementArrayBuffer, indices, BufferUsageARB.StaticDraw);

        // Datos por instancia (atributos 1, 2, 3)
        int maxInstances = 100_000;
        instanceData = new float[maxInstances * FloatsPerInstance];
        instanceVbo = gl.GenBuffer();
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, instanceVbo);
        gl.BufferData<float>(BufferTargetARB.ArrayBuffer, instanceData, BufferUsageARB.DynamicDraw);

        uint stride = (uint)(FloatsPerInstance * sizeof(float));
        gl.EnableVertexAttribArray(1);
        gl.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, stride, (void*)0);
        gl.VertexAttribDivisor(1, 1);
        gl.EnableVertexAttribArray(2);
        gl.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, stride, (void*)(3 * sizeof(float)));
        gl.VertexAttribDivisor(2, 1);
        gl.EnableVertexAttribArray(3);
        gl.VertexAttribPointer(3, 1, VertexAttribPointerType.Float, false, stride, (void*)(6 * sizeof(float)));
        gl.VertexAttribDivisor(3, 1);

        gl.BindVertexArray(0);
    }

    void OnUpdate(double delta)
    {
        if (paused) return;
        accumulator += Math.Min(delta, 0.1) * timeScale;   // acotado para evitar la "espiral de la muerte"
        while (accumulator >= Dt)
        {
            scena.Step(Dt);
            accumulator -= Dt;
        }
    }

    void OnRender(double delta)
    {
        gl.ClearColor(0.08f, 0.08f, 0.10f, 1f);
        gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        // 1) Física (double) -> buffer de instancias (float)
        int n = Math.Min(scena.world.Particulas.Count, instanceData.Length / FloatsPerInstance);
        for (int i = 0; i < n; i++)
        {
            var p = scena.world.Particulas[i];
            int o = i * FloatsPerInstance;
            instanceData[o + 0] = (float)p.Posicion.X;
            instanceData[o + 1] = (float)p.Posicion.Y;
            instanceData[o + 2] = (float)p.Posicion.Z;

            // Color según la carga: rojo (+), azul (-), gris (neutra)
            (float r, float g, float b) = p.Carga > 0 ? (1f, 0.35f, 0.30f)
                                        : p.Carga < 0 ? (0.30f, 0.50f, 1f)
                                        : (0.70f, 0.70f, 0.70f);
            instanceData[o + 3] = r;
            instanceData[o + 4] = g;
            instanceData[o + 5] = b;
            instanceData[o + 6] = 0.12f;   // radio visual
        }
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, instanceVbo);
        gl.BufferSubData<float>(BufferTargetARB.ArrayBuffer, 0, instanceData.AsSpan(0, n * FloatsPerInstance));

        // 2) Cámara
        float aspect = (float)window.Size.X / Math.Max(1, window.Size.Y);
        var vp = camera.View() * Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 4f, aspect, 0.1f, 300f);

        // 3) Dibujo
        gl.UseProgram(program);
        gl.UniformMatrix4(vpLocation, false, MemoryMarshal.CreateReadOnlySpan(ref vp.M11, 16));
        gl.BindVertexArray(vao);
        gl.DrawElementsInstanced(PrimitiveType.Triangles, (uint)indexCount,
                                 DrawElementsType.UnsignedInt, (void*)0, (uint)n);
    }

    void OnClosing()
    {
        gl.DeleteBuffer(sphereVbo);
        gl.DeleteBuffer(ebo);
        gl.DeleteBuffer(instanceVbo);
        gl.DeleteVertexArray(vao);
        gl.DeleteProgram(program);
    }

    // --- Utilidades para compilar los shaders y slices ---------------------------------------------------

    static (float[] vertices, uint[] indices) BuildSphere(int stacks, int slices)
    {
        var v = new List<float>();
        var idx = new List<uint>();

        for (int i = 0; i <= stacks; i++)
        {
            float phi = MathF.PI * i / stacks;
            for (int j = 0; j <= slices; j++)
            {
                float theta = 2f * MathF.PI * j / slices;
                v.Add(MathF.Sin(phi) * MathF.Cos(theta));
                v.Add(MathF.Cos(phi));
                v.Add(MathF.Sin(phi) * MathF.Sin(theta));
            }
        }

        for (int i = 0; i < stacks; i++)
        {
            for (int j = 0; j < slices; j++)
            {
                uint a = (uint)(i * (slices + 1) + j);
                uint b = a + (uint)(slices + 1);
                idx.AddRange(new[] { a, b, a + 1, b, b + 1, a + 1 });
            }
        }
        return (v.ToArray(), idx.ToArray());
    }

    uint BuildProgram(string vertexSrc, string fragmentSrc)
    {
        uint vs = Compile(ShaderType.VertexShader, vertexSrc);
        uint fs = Compile(ShaderType.FragmentShader, fragmentSrc);

        uint prog = gl.CreateProgram();
        gl.AttachShader(prog, vs);
        gl.AttachShader(prog, fs);
        gl.LinkProgram(prog);
        gl.GetProgram(prog, ProgramPropertyARB.LinkStatus, out int ok);
        if (ok == 0) throw new Exception("Error al enlazar el programa: " + gl.GetProgramInfoLog(prog));

        gl.DeleteShader(vs);
        gl.DeleteShader(fs);
        return prog;
    }

    uint Compile(ShaderType type, string src)
    {
        uint s = gl.CreateShader(type);
        gl.ShaderSource(s, src);
        gl.CompileShader(s);
        gl.GetShader(s, ShaderParameterName.CompileStatus, out int ok);
        if (ok == 0) throw new Exception($"Error al compilar {type}: " + gl.GetShaderInfoLog(s));
        return s;
    }
}
