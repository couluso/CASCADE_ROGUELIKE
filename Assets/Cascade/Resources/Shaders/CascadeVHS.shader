// CASCADE — filtre VHS plein écran (Auteur de l'adaptation Unity : Coulu)
// Portage HLSL du « VHS Shader » de Vacation, publié sur godotshaders.com sous licence CC0
// (https://godotshaders.com/shader/vhs-shader/) : traînées de chrominance YIQ, netteté, bruit,
// saut vertical, lignes de balayage, vignette. Ajouts : bande de tracking, bruit de tête en bas d'écran,
// glitch piloté par le jeu. L'effet « verre brisé » de l'original n'est pas repris.
// S'applique sur une RawImage affichant la RenderTexture de la caméra : marche en Built-in comme en URP.
Shader "Cascade/VHS"
{
    Properties
    {
        _MainTex ("Image", 2D) = "black" {}
        _Smearing ("Traînée de couleur", Range(0, 1)) = 0.45
        _ColorOffset ("Décalage couleur", Range(-0.05, 0.05)) = 0.004
        _Sharpening ("Netteté", Range(0, 1)) = 0.35
        _StaticNoise ("Bruit", Range(0, 1)) = 0.05
        _VerticalJitter ("Saut vertical", Range(0, 0.1)) = 0.0012
        _ScanlineOpacity ("Lignes de balayage", Range(0, 1)) = 0.07
        _ScanlineCount ("Nombre de lignes", Float) = 300
        _Vignette ("Vignette", Range(0, 1)) = 0.32
        _Tracking ("Bande de tracking", Range(0, 1)) = 0.45
        _Glitch ("Glitch (piloté par script)", Range(0, 1)) = 0
        _VhsTime ("Temps (piloté par script)", Float) = 0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend One Zero

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _Smearing, _ColorOffset, _Sharpening, _StaticNoise, _VerticalJitter;
            float _ScanlineOpacity, _ScanlineCount, _Vignette, _Tracking, _Glitch, _VhsTime;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float noise (float2 p) { return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453123); }

            float3 rgb2yiq (float3 c)
            {
                return float3(dot(c, float3(0.299, 0.587, 0.114)),
                              dot(c, float3(0.596, -0.274, -0.322)),
                              dot(c, float3(0.211, -0.523, 0.312)));
            }

            float3 yiq2rgb (float3 c)
            {
                return float3(dot(c, float3(1.0, 0.956, 0.621)),
                              dot(c, float3(1.0, -0.272, -0.647)),
                              dot(c, float3(1.0, -1.106, 1.703)));
            }

            float3 Sample (float2 uv) { return tex2D(_MainTex, saturate(uv)).rgb; }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float t = _VhsTime;

                // Saut vertical analogique (plus fort pendant un glitch)
                uv.y += (noise(float2(floor(t * 24.0), 0.0)) - 0.5) * _VerticalJitter * (1.0 + _Glitch * 10.0);

                // Bande de tracking qui remonte lentement (ajout)
                float bandPos = frac(t * 0.06);
                float band = saturate(1.0 - abs(uv.y - bandPos) * 16.0);
                float lineNoise = noise(float2(floor(uv.y * _ScanlineCount), floor(t * 30.0)));
                uv.x += (lineNoise - 0.5) * 0.010 * band * _Tracking;
                uv.x += (lineNoise - 0.5) * 0.035 * _Glitch * step(0.65, noise(float2(floor(uv.y * 40.0), floor(t * 20.0))));

                // Bruit de tête de lecture en bas de l'image (ajout)
                float head = step(uv.y, 0.022);
                uv.x += head * (noise(float2(floor(uv.y * 220.0), floor(t * 30.0))) - 0.5) * 0.03;

                // Chrominance qui bave (traînée YIQ, cœur du shader original)
                float3 yiq = rgb2yiq(Sample(uv));
                float y = yiq.x;
                float iAcc = 0.0, qAcc = 0.0;
                [unroll]
                for (int n = 0; n < 12; n++)
                {
                    float xo = n * (_Smearing * 0.01) + _ColorOffset;
                    float3 s = rgb2yiq(Sample(uv - float2(xo, 0.0)));
                    iAcc += s.y;
                    qAcc += s.z;
                }
                float3 fy = float3(y, iAcc / 12.0, qAcc / 12.0);

                // Netteté des bords (luminance)
                float neighbor = rgb2yiq(Sample(uv + float2(0.001, 0.0))).x;
                fy.x += (y - neighbor) * _Sharpening;

                float3 col = yiq2rgb(fy);

                // Artefacts de bande
                col += (noise(uv + frac(t)) - 0.5) * _StaticNoise * (1.0 + _Glitch * 3.0);
                col += band * _Tracking * 0.05 * lineNoise;
                col -= sin(uv.y * _ScanlineCount * 3.14159) * _ScanlineOpacity * y;
                col *= smoothstep(0.8, 0.8 - _Vignette, distance(i.uv, float2(0.5, 0.5)));

                return fixed4(saturate(col), 1.0);
            }
            ENDCG
        }
    }
    Fallback Off
}
