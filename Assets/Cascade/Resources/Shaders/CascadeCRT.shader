// CASCADE — filtre « moniteur d'arcade » plein écran (Auteur de l'adaptation Unity : Coulu)
// Base : portage HLSL du « VHS Shader » de Vacation (godotshaders.com, licence CC0,
// https://godotshaders.com/shader/vhs-shader/) — conversion YIQ, netteté, bruit, vignette.
// Réglé beaucoup plus léger (traînée de couleur quasi nulle pour garder les couleurs des pics lisibles)
// et complété par un rendu de borne : courbure d'écran, lueur des phosphores (bloom), masque RGB,
// lignes de balayage, saturation, glitch piloté par le jeu.
Shader "Cascade/ArcadeCRT"
{
    Properties
    {
        _MainTex ("Image", 2D) = "black" {}
        _Curvature ("Courbure d'écran", Range(0, 0.2)) = 0.035
        _Glow ("Lueur (bloom)", Range(0, 2)) = 0.55
        _GlowThreshold ("Seuil de lueur", Range(0, 1)) = 0.55
        _GlowRadius ("Rayon de lueur (px)", Range(0.5, 8)) = 3
        _Smearing ("Traînée de couleur VHS", Range(0, 1)) = 0.06
        _ColorOffset ("Décalage couleur", Range(-0.02, 0.02)) = 0.0012
        _Sharpening ("Netteté", Range(0, 1)) = 0.25
        _Saturation ("Saturation", Range(0, 2)) = 1.18
        _Contrast ("Contraste", Range(0.5, 1.5)) = 1.06
        _StaticNoise ("Bruit", Range(0, 0.3)) = 0.018
        _ScanlineOpacity ("Lignes de balayage", Range(0, 1)) = 0.16
        _ScanlineCount ("Nombre de lignes", Float) = 380
        _Mask ("Masque RGB", Range(0, 0.5)) = 0.1
        _Vignette ("Vignette", Range(0, 1)) = 0.22
        _VerticalJitter ("Saut vertical", Range(0, 0.02)) = 0.0003
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
            float4 _MainTex_TexelSize;
            float _Curvature, _Glow, _GlowThreshold, _GlowRadius, _Smearing, _ColorOffset, _Sharpening;
            float _Saturation, _Contrast, _StaticNoise, _ScanlineOpacity, _ScanlineCount, _Mask, _Vignette;
            float _VerticalJitter, _Glitch, _VhsTime;

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

            float3 Bright (float2 uv)
            {
                float3 c = Sample(uv);
                float l = max(c.r, max(c.g, c.b));
                return c * saturate((l - _GlowThreshold) / max(1e-3, 1.0 - _GlowThreshold));
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Courbure d'écran cathodique
                float2 cc = i.uv - 0.5;
                float2 uv = 0.5 + cc * (1.0 + _Curvature * dot(cc, cc) * 4.0);
                if (uv.x < 0.0 || uv.x > 1.0 || uv.y < 0.0 || uv.y > 1.0) return fixed4(0, 0, 0, 1);

                float t = _VhsTime;
                uv.y += (noise(float2(floor(t * 24.0), 0.0)) - 0.5) * (_VerticalJitter + _Glitch * 0.01);
                float lineNoise = noise(float2(floor(uv.y * _ScanlineCount), floor(t * 30.0)));
                uv.x += (lineNoise - 0.5) * 0.03 * _Glitch * step(0.6, noise(float2(floor(uv.y * 40.0), floor(t * 20.0))));

                // Couleur : légère traînée YIQ (héritée du shader VHS), très faible pour garder les teintes franches
                float3 yiq = rgb2yiq(Sample(uv));
                float y = yiq.x;
                float iAcc = 0.0, qAcc = 0.0;
                [unroll]
                for (int n = 0; n < 4; n++)
                {
                    float xo = n * (_Smearing * 0.01) + _ColorOffset;
                    float3 s = rgb2yiq(Sample(uv - float2(xo, 0.0)));
                    iAcc += s.y;
                    qAcc += s.z;
                }
                float3 fy = float3(y, iAcc * 0.25, qAcc * 0.25);
                float neighbor = rgb2yiq(Sample(uv + float2(_MainTex_TexelSize.x, 0.0))).x;
                fy.x += (y - neighbor) * _Sharpening;
                float3 col = yiq2rgb(fy);

                // Lueur des phosphores : les éléments lumineux (capuchons, ampoules, néons) bavent un peu de lumière
                float2 px = _MainTex_TexelSize.xy * _GlowRadius;
                float3 glow = Bright(uv + float2(px.x, 0)) + Bright(uv - float2(px.x, 0))
                            + Bright(uv + float2(0, px.y)) + Bright(uv - float2(0, px.y))
                            + Bright(uv + px * 1.5) + Bright(uv - px * 1.5)
                            + Bright(uv + float2(px.x, -px.y) * 1.5) + Bright(uv + float2(-px.x, px.y) * 1.5)
                            + Bright(uv + float2(px.x * 3.0, 0)) + Bright(uv - float2(px.x * 3.0, 0))
                            + Bright(uv + float2(0, px.y * 3.0)) + Bright(uv - float2(0, px.y * 3.0));
                col += glow / 12.0 * _Glow;

                // Saturation et contraste (couleurs d'arcade franches)
                float luma = dot(col, float3(0.299, 0.587, 0.114));
                col = lerp(luma.xxx, col, _Saturation);
                col = (col - 0.5) * _Contrast + 0.5;

                // Masque RGB et lignes de balayage (multiplicatifs : n'écrasent pas les couleurs)
                float2 screenPx = i.uv * _ScreenParams.xy;
                int m = (int)fmod(screenPx.x, 3.0);
                float3 mask = m == 0 ? float3(1.0, 1.0 - _Mask, 1.0 - _Mask) : m == 1 ? float3(1.0 - _Mask, 1.0, 1.0 - _Mask) : float3(1.0 - _Mask, 1.0 - _Mask, 1.0);
                col *= mask * (1.0 + _Mask * 0.5);
                float scan = 0.5 + 0.5 * sin(uv.y * _ScanlineCount * 6.28318);
                col *= 1.0 - _ScanlineOpacity * (1.0 - scan);

                // Bruit, vignette et coins arrondis de l'écran
                col += (noise(uv + frac(t)) - 0.5) * _StaticNoise * (1.0 + _Glitch * 4.0);
                col *= smoothstep(0.8, 0.8 - _Vignette, distance(i.uv, float2(0.5, 0.5)));
                float2 edge = smoothstep(0.0, 0.012, uv) * smoothstep(0.0, 0.012, 1.0 - uv);
                col *= edge.x * edge.y;

                return fixed4(saturate(col), 1.0);
            }
            ENDCG
        }
    }
    Fallback Off
}
