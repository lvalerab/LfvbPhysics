// Physics.Viewer / Program.cs
// Requiere Physics.Core (Vec3, Particle) y los paquetes Silk.NET.Windowing, Silk.NET.OpenGL, Silk.NET.Input.
// En el .csproj: <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
// Si pusiste Physics.Core dentro de un namespace, añade: using Physics.Core;

using Core.Fields;
using Core.Maths;
using Core.Particulas;
using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;
using System.Numerics;
using System.Runtime.InteropServices;
using Viewer.Cameras;
using Viewer.Opengl.Draws;
using Viewer.Opengl.Fragment;
using Viewer.Opengl.Vertex;
using Viewer.Scenes;
using Viewer.Scenes.Basicas;
using Viewer.Scenes.Electromagnetismo;
using Viewer.Scenes.Gravity;

new App().Run();




// ------------------------------------------------------------------
// Aplicación: ventana, OpenGL, entrada y bucle
// ------------------------------------------------------------------
internal unsafe class App
{

    // Shaders

    const string VertexSrc = Vertex.VertexSrc;

    const string LineVertexSrc = Vertex.LineVertexSrc;

    const string FragmentSrc = Fragment.FragmentSrc;

    const string LineFragmentSrc = Fragment.LineFragmentSrc;


    const double Dt = 1.0 / 240.0;   // paso fijo de física
    const int FloatsPerInstance = 7; // pos(3) + color(3) + radio(1)

    readonly IWindow window;
    //readonly GeneralScena scena = new ParticulaScene(new LfvbPhysics.World(true,true,true));
    readonly GeneralScena scena = new OrbitalEscena(new LfvbPhysics.World(true, true, false));
    //readonly GeneralScena scena = new TiroParabolicoParticulaScene(new LfvbPhysics.World(true, true, false));


    readonly OrbitalCamera camera = new OrbitalCamera();

    GL gl = null!;
    
    uint program, vao, sphereVbo, ebo, instanceVbo;
    int vpLocation, indexCount;

    // Flechas de velocidad y aceleración
    uint lineProgram, lineVao, lineVbo;
    int lineVpLocation;
    readonly List<float> lineData = new();

    int stepCounter;

    const float VelocityScale = GeneralScena.VectorVelocityScale;       // longitud de flecha por unidad de velocidad
    const float AccelerationScale = GeneralScena.VectorAccelerationScale;   // longitud de flecha por unidad de aceleración

    readonly List<Trail> trails = new();

    //Para la rejillas de los planos    
    uint gridVao, gridVbo;
    int gridCount, pointSizeLocation;

    //Para el gdi
    ImGuiController imgui = null!;



    float[] instanceData = Array.Empty<float>();

    double accumulator, timeScale = 1;
    bool paused;
    Vector2 lastMouse;
    bool hasLastMouse;

    //Para poner la camara sobre una particula
    int indexCamara = -1;


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

        //Añado para dibujar flechas de velocidad y aceleración
        lineProgram = BuildProgram(LineVertexSrc, LineFragmentSrc);
        lineVpLocation = gl.GetUniformLocation(lineProgram, "uVP");

        SetupGeometry();
    }

    void SetupInput()
    {
        var input = window.CreateInput();
        
        //Para el GDI
        imgui = new ImGuiController(gl, window, input);

        foreach (var kb in input.Keyboards)
        {
            kb.KeyDown += (_, key, _) =>
            {
                if (ImGui.GetIO().WantCaptureKeyboard) return;   // escribiendo en el panel

                switch (key)
                {
                    case Key.Space: paused = !paused; break;
                    case Key.R: scena.Reset(); accumulator = 0; trails.Clear(); stepCounter = 0; break;
                    case Key.T: scena.ShowEstelas = !scena.ShowEstelas; break;
                    case Key.C: trails.Clear(); break;
                    case Key.Up: timeScale = Math.Min(timeScale * 2, 16); break;
                    case Key.Down: timeScale = Math.Max(timeScale / 2, 1.0 / 16); break;   
                    case Key.Left: indexCamara = (indexCamara - 1 + scena.world.Particulas.Count) % scena.world.Particulas.Count; break;
                    case Key.Right: indexCamara = (indexCamara + 1) % scena.world.Particulas.Count; break;
                    case Key.O: indexCamara = -1; break;
                    case Key.V: scena.ShowVectors = !scena.ShowVectors; break;
                    case Key.E: scena.ShowEjes = !scena.ShowEjes; break;
                    case Key.B: scena.ShowGrids = !scena.ShowGrids; break;
                    case Key.F: scena.ShowCampos = !scena.ShowCampos; break;
                    case Key.L: scena.ShowNames = !scena.ShowNames; break;
                    case Key.Escape: window.Close(); break;
                }
            };
        }

       
        foreach (var mouse in input.Mice)
        {
            mouse.MouseMove += (m, pos) =>
            {
                if (hasLastMouse && m.IsButtonPressed(MouseButton.Left) && !ImGui.GetIO().WantCaptureMouse)
                {
                    var d = pos - lastMouse;
                    camera.Yaw += d.X * 0.005f;
                    camera.Pitch = Math.Clamp(camera.Pitch + d.Y * 0.005f, -1.5f, 1.5f);
                }
                lastMouse = pos;
                hasLastMouse = true;
            };
            mouse.Scroll += (_, wheel) =>
            {
                if (ImGui.GetIO().WantCaptureMouse) return;
                camera.Distance = Math.Clamp(camera.Distance * MathF.Pow(0.9f, wheel.Y), 2f, 150f);
            };
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

        // Líneas de las flechas: posición(3) + color(3) por vértice
        lineVao = gl.GenVertexArray();
        gl.BindVertexArray(lineVao);
        lineVbo = gl.GenBuffer();
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, lineVbo);
        uint lineStride = 6 * sizeof(float);
        gl.EnableVertexAttribArray(0);
        gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, lineStride, (void*)0);
        gl.EnableVertexAttribArray(1);
        gl.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, lineStride, (void*)(3 * sizeof(float)));

        // Rejilla de puntos (se construye una sola vez)
        var grid = BuildGrid(scena);
        gridCount = grid.Length / 6;
        gridVao = gl.GenVertexArray();
        gl.BindVertexArray(gridVao);
        gridVbo = gl.GenBuffer();
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, gridVbo);
        gl.BufferData<float>(BufferTargetARB.ArrayBuffer, grid, BufferUsageARB.StaticDraw);
        gl.EnableVertexAttribArray(0);
        gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, lineStride, (void*)0);
        gl.EnableVertexAttribArray(1);
        gl.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, lineStride, (void*)(3 * sizeof(float)));

        gl.BindVertexArray(0);
    }

    void OnUpdate(double delta)
    {
        if (paused) return;
        accumulator += Math.Min(delta, 0.1) * timeScale;   // acotado para evitar la "espiral de la muerte"
        while (accumulator >= Dt)
        {
            scena.Step(Dt);
            if (++stepCounter % scena.DibujarEstelaCada == 0) RecordTrails();
            accumulator -= Dt;
        }
        //Evito overflow de stepCounter
        if (stepCounter.Equals(int.MaxValue))
        {
            stepCounter = 0;
        }
    }

    void OnRender(double delta)
    {
        gl.ClearColor(0.08f, 0.08f, 0.10f, 1f);
        gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        imgui.Update((float)delta);

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

        if(scena.ShowGrids)
        {
            DrawGrid(vp);
        }

        if(scena.ShowCampos)
        {
            DrawFieldsGizmo();
        }

        if (scena.ShowEstelas)
        {
            DrawTrails(vp, n);
        }

        if(scena.ShowNames)
        {
            DrawLabels(vp, n);
        }

        if (scena.ShowVectors)
        {
            DrawVectors(vp, n);
        }
        if(scena.ShowEjes)
        {
            DrawEjes();
        }

        DrawControlPanel();

        //Esto siempre el ultimo
        imgui.Render();
    }

    void OnClosing()
    {
       /* gl.DeleteBuffer(sphereVbo);
        gl.DeleteBuffer(ebo);
        gl.DeleteBuffer(instanceVbo);
        gl.DeleteVertexArray(vao);
        gl.DeleteProgram(program);*/

        imgui.Dispose();
        gl.DeleteBuffer(sphereVbo);
        gl.DeleteBuffer(ebo);
        gl.DeleteBuffer(instanceVbo);
        gl.DeleteVertexArray(vao);
        gl.DeleteProgram(program);
        gl.DeleteProgram(lineProgram);
        gl.DeleteBuffer(lineVbo);
        gl.DeleteBuffer(gridVbo);
        gl.DeleteVertexArray(gridVao);
        gl.DeleteVertexArray(lineVao);
    }


    //dibuja la rejilla de puntos en los planos XY, XZ, YZ
    void DrawGrid(Matrix4x4 vp)
    {
        float scale = (float)window.FramebufferSize.X / Math.Max(1, window.Size.X);   // HiDPI
        gl.UseProgram(lineProgram);
        gl.UniformMatrix4(lineVpLocation, false, MemoryMarshal.CreateReadOnlySpan(ref vp.M11, 16));
        gl.Uniform1(pointSizeLocation, 2f * scale);
        gl.BindVertexArray(gridVao);
        gl.DrawArrays(PrimitiveType.Points, 0, (uint)gridCount);
    }

    // Puntos tenues en los tres planos coordenados; cada plano con un matiz del color de su eje normal.
    // Los puntos de las líneas múltiplo de 5 son algo más brillantes para dar escala.
    static float[] BuildGrid(GeneralScena scena)
    {
        var d = new List<float>();
        int h = scena.PuntosDesdeElOrigenGrids;

        var tintX = new Vector3(1.0f, 0.6f, 0.6f);   // plano ZY (normal X)
        var tintY = new Vector3(0.6f, 1.0f, 0.6f);   // plano XZ (normal Y), el "suelo"
        var tintZ = new Vector3(0.6f, 0.7f, 1.0f);   // plano XY (normal Z)

        void P(float x, float y, float z, Vector3 tint, bool major)
        {
            float b = major ? 0.50f : 0.26f;
            d.Add(x * scena.SeparacionPuntosGrids); d.Add(y * scena.SeparacionPuntosGrids); d.Add(z * scena.SeparacionPuntosGrids);
            d.Add(tint.X * b); d.Add(tint.Y * b); d.Add(tint.Z * b);
        }

        for (int i = -h; i <= h; i++)
        {
            for (int j = -h; j <= h; j++)
            {
                bool major = i % 5 == 0 || j % 5 == 0;
                P(i, j, 0, tintZ, major);   // plano XY
                P(0, i, j, tintX, major);   // plano ZY
                P(i, 0, j, tintY, major);   // plano XZ
            }
        }
        return d.ToArray();
    }

    // Etiquetas: se proyecta la posición 3D a píxeles y se dibuja texto con ImGui junto a cada partícula
    void DrawLabels(Matrix4x4 vp, int n)
    {
        var draw = ImGui.GetForegroundDrawList();
        float w = window.Size.X, h = window.Size.Y;

        for (int i = 0; i < n; i++)
        {
            var p = scena.world.Particulas[i];
            var clip = Vector4.Transform(
                new Vector4((float)p.Posicion.X, (float)p.Posicion.Y, (float)p.Posicion.Z, 1f), vp);

            // Detrás de la cámara o fuera de los planos de recorte: sin etiqueta
            if (clip.W <= 0f || clip.Z < 0f || clip.Z > clip.W) continue;

            float sx = (clip.X / clip.W * 0.5f + 0.5f) * w;
            float sy = (1f - (clip.Y / clip.W * 0.5f + 0.5f)) * h;
            if (sx < -50 || sx > w + 50 || sy < -50 || sy > h + 50) continue;

            string name = $"{p.Nombre}";
            //string text = 1 == 1
            //    ? name
            //    : $"{name}  q={p.Charge:+0.##;-0.##;0}  m={p.Mass:0.##}  |v|={p.Velocity.Length:0.00}";
            string text = $"{name}  q={p.Carga:+0.##;-0.##;0} Q  m={p.Masa:0.##} Kg  |v|={p.Velocidad.Norma():0.00} m/s |a|={(p.Aceleracion!=null?p.Aceleracion.Norma():0):0.00}";

            var color = p.Carga > 0 ? new Vector4(1.00f, 0.55f, 0.50f, 1f)
                      : p.Carga < 0 ? new Vector4(0.55f, 0.70f, 1.00f, 1f)
                      : new Vector4(0.85f, 0.85f, 0.85f, 1f);

            draw.AddText(new Vector2(sx + 10f, sy - 18f), ImGui.GetColorU32(color), text);
        }
    }

    // Dibuja flechas de velocidad y aceleración

    void DrawVectors(Matrix4x4 vp, int n)
    {
        lineData.Clear();
        var velColor = new Vector3(0.30f, 1.00f, 0.40f);
        var accColor = new Vector3(1.00f, 0.65f, 0.15f);

        for (int i = 0; i < n; i++)
        {
            var p = scena.world.Particulas[i];
            var origin = new Vector3((float)p.Posicion.X, (float)p.Posicion.Y, (float)p.Posicion.Z);

            if (scena.ShowVectors)
            {
                //La velocidad
                var v = new Vector3((float)p.Velocidad.X, (float)p.Velocidad.Y, (float)p.Velocidad.Z);
                AddArrow(origin, v * VelocityScale, velColor);
                if(p.Aceleracion!=null) { 
                    //La aceleración
                    var a = new Vector3((float)p.Aceleracion.X, (float)p.Aceleracion.Y, (float)p.Aceleracion.Z);
                    AddArrow(origin, a * AccelerationScale, accColor);
                } else
                {
                    AddArrow(origin,Vector3.Zero* AccelerationScale, accColor);
                }
            }
        }
        if (lineData.Count == 0) return;

        gl.Disable(EnableCap.DepthTest);   // las flechas siempre visibles, incluso dentro de la esfera
        gl.UseProgram(lineProgram);
        gl.UniformMatrix4(lineVpLocation, false, MemoryMarshal.CreateReadOnlySpan(ref vp.M11, 16));
        gl.BindVertexArray(lineVao);
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, lineVbo);
        gl.BufferData<float>(BufferTargetARB.ArrayBuffer, CollectionsMarshal.AsSpan(lineData), BufferUsageARB.DynamicDraw);
        gl.DrawArrays(PrimitiveType.Lines, 0, (uint)(lineData.Count / 6));
        gl.Enable(EnableCap.DepthTest);
    }

    // Dibuja los ejes X, Y, Z con letras en la esquina inferior izquierda
    void DrawEjes()
    {
        (Vector3 dir, Vector3 color, int letter)[] axes =
        {
            (Vector3.UnitX, new Vector3(1.00f, 0.30f, 0.30f), 0),
            (Vector3.UnitY, new Vector3(0.40f, 1.00f, 0.40f), 1),
            (Vector3.UnitZ, new Vector3(0.35f, 0.55f, 1.00f), 2),
        };

        var view = camera.View();
        lineData.Clear();
        foreach (var (dir, color, letter) in axes)
        {
            var d = Vector3.TransformNormal(dir, view);          // eje en el espacio de la cámara
            AddSegment(Vector3.Zero, d, color);                   // semieje positivo
            AddSegment(Vector3.Zero, -0.4f * d, color * 0.35f);   // semieje negativo, más tenue
            AddGlyph(letter, 1.28f * d, color);                   // letra en el extremo
        }

        // Viewport cuadrado abajo a la izquierda (en píxeles del framebuffer, válido en pantallas HiDPI)
        var fb = window.FramebufferSize;
        float scale = (float)fb.X / Math.Max(1, window.Size.X);
        int px = (int)(130 * scale), margin = (int)(12 * scale);
        var proj = Matrix4x4.CreateOrthographicOffCenter(-1.6f, 1.6f, -1.6f, 1.6f, -5f, 5f);

        gl.Viewport(margin, margin, (uint)px, (uint)px);
        gl.Disable(EnableCap.DepthTest);
        gl.UseProgram(lineProgram);
        gl.UniformMatrix4(lineVpLocation, false, MemoryMarshal.CreateReadOnlySpan(ref proj.M11, 16));
        gl.BindVertexArray(lineVao);
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, lineVbo);
        gl.BufferData<float>(BufferTargetARB.ArrayBuffer, CollectionsMarshal.AsSpan(lineData), BufferUsageARB.DynamicDraw);
        gl.DrawArrays(PrimitiveType.Lines, 0, (uint)(lineData.Count / 6));
        gl.Enable(EnableCap.DepthTest);
        gl.Viewport(0, 0, (uint)fb.X, (uint)fb.Y);
    }

    // Letras X, Y, Z hechas con segmentos, siempre de cara a la cámara
    void AddGlyph(int letter, Vector3 c, Vector3 color)
    {
        const float s = 0.12f;
        void L(float x1, float y1, float x2, float y2) =>
            AddSegment(c + new Vector3(x1 * s, y1 * s, 0), c + new Vector3(x2 * s, y2 * s, 0), color);

        switch (letter)
        {
            case 0: L(-1, -1, 1, 1); L(-1, 1, 1, -1); break;               // X
            case 1: L(-1, 1, 0, 0); L(1, 1, 0, 0); L(0, 0, 0, -1); break; // Y
            default: L(-1, 1, 1, 1); L(1, 1, -1, -1); L(-1, -1, 1, -1); break; // Z
        }
    }

    // Indicador de campos fijos en la esquina superior derecha:
    // E (amarillo) y B (cian), con la orientación real respecto a la cámara actual.
    // La longitud de cada flecha crece con la intensidad del campo (saturando), no es proporcional.
    void DrawFieldsGizmo()
    {
        var view = camera.View();
        lineData.Clear();

        // Ejes de referencia muy tenues para leer la dirección
        (Vector3 dir, Vector3 color)[] axes =
        {
            (Vector3.UnitX, new Vector3(1.00f, 0.30f, 0.30f)),
            (Vector3.UnitY, new Vector3(0.40f, 1.00f, 0.40f)),
            (Vector3.UnitZ, new Vector3(0.35f, 0.55f, 1.00f)),
        };
        foreach (var (dir, color) in axes)
        {
            var d = Vector3.TransformNormal(dir, view);
            AddSegment(-0.4f * d, d, color * 0.22f);
        }
        foreach (IField v in scena.world.Campos)
        {
            if (v is Core.Fields.Electromagnetic.UniformElectric ue)
            {
                AddFieldArrow(ue.campoElectrico, new Vector3(1.00f, 0.85f, 0.25f), 3, view);   // E
            }
            else if (v is Core.Fields.Electromagnetic.UniformMagnetic um)
            {
                AddFieldArrow(um.CampoMagnetico, new Vector3(0.30f, 0.90f, 1.00f), 4, view);   // B
            }
            else if (v is Core.Fields.Gravity.UniformGravity ug)
            {
                AddFieldArrow(ug.gravedad, new Vector3(0.80f, 0.80f, 0.80f), 5, view);   // G)
            }
        }

        var fb = window.FramebufferSize;
        float scale = (float)fb.X / Math.Max(1, window.Size.X);
        int px = (int)(130 * scale), margin = (int)(12 * scale);
        var proj = Matrix4x4.CreateOrthographicOffCenter(-1.6f, 1.6f, -1.6f, 1.6f, -5f, 5f);

        // Esquina superior derecha (el origen del viewport de OpenGL es abajo a la izquierda)
        gl.Viewport(fb.X - margin - px, fb.Y - margin - px, (uint)px, (uint)px);
        gl.Disable(EnableCap.DepthTest);
        gl.UseProgram(lineProgram);
        gl.UniformMatrix4(lineVpLocation, false, MemoryMarshal.CreateReadOnlySpan(ref proj.M11, 16));
        gl.BindVertexArray(lineVao);
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, lineVbo);
        gl.BufferData<float>(BufferTargetARB.ArrayBuffer, CollectionsMarshal.AsSpan(lineData), BufferUsageARB.DynamicDraw);
        gl.DrawArrays(PrimitiveType.Lines, 0, (uint)(lineData.Count / 6));
        gl.Enable(EnableCap.DepthTest);
        gl.Viewport(0, 0, (uint)fb.X, (uint)fb.Y);
    }

    //Dibuja las estelas de las partículas, guardando los últimos N puntos de cada partícula
    void RecordTrails()
    {
        while (trails.Count < scena.world.Particulas.Count) trails.Add(new Trail(scena.CapacidadEstela));
        for (int i = 0; i < scena.world.Particulas.Count; i++)
        {
            var p = scena.world.Particulas[i].Posicion;
            trails[i].Add(new Vector3((float)p.X, (float)p.Y, (float)p.Z));
        }
    }

    // Estela: tramo reciente brillante, tramo antiguo cada vez más tenue (se funde con el fondo)
    void DrawTrails(Matrix4x4 vp, int n)
    {
        lineData.Clear();
        for (int i = 0; i < n && i < trails.Count; i++)
        {
            var t = trails[i];
            if (t.Count < 2) continue;

            var charge = scena.world.Particulas[i].Carga;
            var baseColor = charge > 0 ? new Vector3(1.00f, 0.35f, 0.30f)
                          : charge < 0 ? new Vector3(0.30f, 0.50f, 1.00f)
                          : new Vector3(0.70f, 0.70f, 0.70f);

            for (int k = 0; k < t.Count - 1; k++)
            {
                float f0 = 0.10f + 0.90f * k / (t.Count - 1);
                float f1 = 0.10f + 0.90f * (k + 1) / (t.Count - 1);
                AddVertex(t[k], baseColor * f0);
                AddVertex(t[k + 1], baseColor * f1);
            }
        }
        if (lineData.Count == 0) return;

        gl.UseProgram(lineProgram);
        gl.UniformMatrix4(lineVpLocation, false, MemoryMarshal.CreateReadOnlySpan(ref vp.M11, 16));
        gl.BindVertexArray(lineVao);
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, lineVbo);
        gl.BufferData<float>(BufferTargetARB.ArrayBuffer, CollectionsMarshal.AsSpan(lineData), BufferUsageARB.DynamicDraw);
        gl.DrawArrays(PrimitiveType.Lines, 0, (uint)(lineData.Count / 6));
    }


    void AddVertex(Vector3 pos, Vector3 color)
    {
        lineData.Add(pos.X); lineData.Add(pos.Y); lineData.Add(pos.Z);
        lineData.Add(color.X); lineData.Add(color.Y); lineData.Add(color.Z);
    }

    void AddSegment(Vector3 a, Vector3 b, Vector3 color)
    {
        lineData.AddRange(new[] { a.X, a.Y, a.Z, color.X, color.Y, color.Z });
        lineData.AddRange(new[] { b.X, b.Y, b.Z, color.X, color.Y, color.Z });
    }

    void AddFieldArrow(Core.Maths.Vector3D field, Vector3 color, int letter, Matrix4x4 view)
    {
        double m = field.Norma();
        if (m < 1e-9) return;   // campo nulo: sin flecha

        var dirWorld = new Vector3((float)(field.X / m), (float)(field.Y / m), (float)(field.Z / m));
        var d = Vector3.TransformNormal(dirWorld, view);   // dirección en el espacio de la cámara
        float len = 1.25f * (float)(m / (m + 0.5));        // 0 -> 0, campo grande -> ~1.25

        AddArrow(Vector3.Zero, d * len, color);
        AddGlyph(letter, d * (len + 0.25f), color);
    }

    // Flecha = cuerpo + cabeza de cuatro aletas
    void AddArrow(Vector3 origin, Vector3 vec, Vector3 color)
    {
        float len = vec.Length();
        if (len < 1e-4f) return;

        var dir = vec / len;
        var tip = origin + vec;

        var helper = MathF.Abs(dir.Y) < 0.99f ? Vector3.UnitY : Vector3.UnitX;
        var side = Vector3.Normalize(Vector3.Cross(dir, helper));
        var up = Vector3.Cross(dir, side);

        float head = MathF.Min(0.25f * len, 0.25f);
        var basePoint = tip - head * dir;
        float w = 0.4f * head;

        AddSegment(origin, tip, color);
        AddSegment(tip, basePoint + w * side, color);
        AddSegment(tip, basePoint - w * side, color);
        AddSegment(tip, basePoint + w * up, color);
        AddSegment(tip, basePoint - w * up, color);
    }



    /**************************************************************************************
     * Para el GDI de configuacion
     *************************************************************************************/
      static readonly string[] LabelItems = { "Ninguna", "Nombre", "Nombre + datos" };

    //static Vector3D ToV(Vec3 v) => new((float)v.X, (float)v.Y, (float)v.Z);
    //static Vec3 FromV(Vector3 v) => new(v.X, v.Y, v.Z);

    // Panel de control: campos uniformes, simulación y visualización
    void DrawControlPanel()
    {
        ImGui.SetNextWindowPos(new Vector2(10, 10), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSize(new Vector2(330, 0), ImGuiCond.FirstUseEver);
        if (!ImGui.Begin("Simulación")) { ImGui.End(); return; }

        ImGui.Text($"t = {scena.Time:0.00} s    partículas: {scena.world.Particulas.Count}");

        // --- Campos uniformes ---
        ImGui.Separator();
        ImGui.Text("Campos uniformes (arrastra o Ctrl+clic para escribir)");
        //var g = ToV(scene.world.Gra);
        //var e = ToV(scene.E);
        //var b = ToV(scene.B);
        /*bool changed = false;
        changed |= ImGui.DragFloat3("g (m/s²)", ref g, 0.05f);
        changed |= ImGui.DragFloat3("E (V/m)", ref e, 0.02f);
        changed |= ImGui.DragFloat3("B (T)", ref b, 0.02f);
        if (changed)
        {
            scene.Gravity = FromV(g);
            scene.E = FromV(e);
            scene.B = FromV(b);
            scene.RefreshAccelerations();
        }*/

        /*void Preset(Vec3 gg, Vec3 ee, Vec3 bb)
        {
            scene.Gravity = gg; scene.E = ee; scene.B = bb;
            scene.RefreshAccelerations();
        }*/
        //if (ImGui.Button("Sin campos")) Preset(Vec3.Zero, Vec3.Zero, Vec3.Zero);
        //ImGui.SameLine();
        //if (ImGui.Button("Gravedad")) Preset(new Vec3(0, -9.81, 0), Vec3.Zero, Vec3.Zero);
        //ImGui.SameLine();
        //if (ImGui.Button("Ciclotrón")) Preset(Vec3.Zero, new Vec3(0, 0, 0.4), new Vec3(0, 1, 0));

        // --- Simulación ---
        ImGui.Separator();
        ImGui.Checkbox("Pausa", ref paused);
        ImGui.SameLine();
        if (ImGui.Button("Reiniciar"))
        {
            scena.Reset(); accumulator = 0; trails.Clear(); stepCounter = 0;
        }
        ImGui.SameLine();
        if (ImGui.Button("Borrar estelas")) trails.Clear();

        float ts = (float)timeScale;
        if (ImGui.SliderFloat("Ritmo", ref ts, 0.0625f, 16f, "%.2fx", ImGuiSliderFlags.Logarithmic))
            timeScale = ts;

        // --- Visualización ---
        ImGui.Separator();
        ImGui.Text("Visualización");
        ImGui.Checkbox("Velocidad y aceleracion", ref scena.ShowVectors);        
        ImGui.Checkbox("Estelas", ref scena.ShowEstelas);
        ImGui.SameLine();
        ImGui.Checkbox("Rejilla", ref scena.ShowGrids);
        ImGui.Checkbox("Ejes", ref scena.ShowEjes);
        ImGui.SameLine();
        ImGui.Checkbox("Campos", ref scena.ShowCampos);
        ImGui.Checkbox("Etiquetas", ref scena.ShowNames);

        ImGui.End();
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
