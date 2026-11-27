using System;
using System.Collections.Generic;
using System.Text;

namespace Viewer.Opengl.Fragment
{
    public class Fragment
    {
        /*public static readonly string FragmentSrc = """
        #version 330 core
        in vec3 vNormal;
        in vec3 vColor;
        out vec4 FragColor;
        void main()
        {
            float ambientStrength = 0.1;
            vec3 ambient = ambientStrength * vColor;
            vec3 lightDir = normalize(vec3(1.0, 1.0, 1.0));
            float diff = max(dot(vNormal, lightDir), 0.0);
            vec3 diffuse = diff * vColor;
            FragColor = vec4(ambient + diffuse, 1.0);
        }
        """;*/
        public const string FragmentSrc = """
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


        public const string LineFragmentSrc = """
        #version 330 core
        in vec3 vColor;
        out vec4 FragColor;
        void main()
        {
            FragColor = vec4(vColor, 1.0);
        }
        """;
    }
}
