using System;
using System.Collections.Generic;
using System.Text;

namespace Viewer.Opengl.Vertex
{
    public class Vertex
    {
        public const string VertexSrc = """
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

        public const string LineVertexSrc = """
        #version 330 core
        layout(location = 0) in vec3 aPos;
        layout(location = 1) in vec3 aColor;
        uniform mat4 uVP;
        out vec3 vColor;
        void main()
        {
            vColor = aColor;
            gl_Position = uVP * vec4(aPos, 1.0);
        }
        """;
    }
}
