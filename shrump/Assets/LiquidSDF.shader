Shader "Custom/LiquidSDF"
{
    Properties
    {
        _ParticleRadius ("Particle Radius", Range(0.001, 0.2)) = 0.01
        _Threshold ("Threshold", Range(0.0, 0.1)) = 0.0
        _Smoothing ("Smoothing", Range(0.0, 0.2)) = 0.09
        _StreamStretch ("Stream Stretch", Range(1.0, 4.0)) = 1.01
        _TeardropIntensity ("Teardrop Intensity", Range(0.0, 2.0)) = 1.54
        [Toggle] _UseTexture ("Use Texture", Float) = 0
        _MainTex ("Rice Texture", 2D) = "white" {}
        _TextureScale ("Texture Scale", Range(0.1, 10.0)) = 1.0
        _LiquidColor ("Liquid Color", Color) = (0.3, 0.6, 0.9, 1.0)
        _OutlineColor ("Outline Color", Color) = (0.8, 0.9, 1.0, 1.0)
        _OutlineWidth ("Outline Width", Range(0.0, 0.05)) = 0.002
        [Toggle] _Pixelated ("Pixelated", Float) = 1
        _PixelSize ("Pixel Size", Range(2.0, 8.0)) = 8.0
    }
    
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            // Uniforms
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _UseTexture;
            float _TextureScale;
            int _ParticleCount;
            float _ParticlePositions[100];
            float _ParticleRadius;
            float _Threshold;
            float _Smoothing;
            float _StreamStretch;
            float _TeardropIntensity;
            float4 _LiquidColor;
            float4 _OutlineColor;
            float _OutlineWidth;
            float _Pixelated;
            float _PixelSize;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            // Smooth minimum for blending
            float smin(float a, float b, float k)
            {
                float h = saturate(0.5 + 0.5 * (b - a) / k);
                return lerp(b, a, h) - k * h * (1.0 - h);
            }

            // Teardrop SDF function
            float teardrop_sdf(float2 p, float radius, float stretch)
            {
                // Normalize position relative to teardrop
                p.y /= stretch;

                // Distance from origin
                float d = length(p);

                // Create teardrop shape by modulating radius based on angle
                float angle = atan2(p.x, -p.y); // Angle from top (pointing up)

                // Teardrop modulation - wider at bottom, pointy at top
                float angle_factor = cos(angle * 0.5); // Creates the teardrop bulge
                float teardrop_radius = radius * (0.7 + 0.3 * angle_factor);

                // Add pointy top effect
                float top_factor = smoothstep(-0.8, 0.2, p.y * stretch);
                teardrop_radius *= (0.3 + 0.7 * top_factor);

                return d - teardrop_radius;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;

                // Pixelation
                if (_Pixelated > 0.5)
                {
                    float2 screen_size = _ScreenParams.xy;
                    uv = floor(uv * screen_size / _PixelSize) * _PixelSize / screen_size;
                }

                float closest_distance = 1000.0;
                bool found_particle = false;

                // Calculate blended SDF
                for (int idx = 0; idx < _ParticleCount && idx < 50; idx++)
                {
                    int pos_idx = idx * 2;
                    if (pos_idx >= 100 || pos_idx + 1 >= 100) break;
                    if (_ParticlePositions[pos_idx] < 0.0 || _ParticlePositions[pos_idx + 1] < 0.0) continue;

                    float2 particle_pos = float2(_ParticlePositions[pos_idx], _ParticlePositions[pos_idx + 1]);
                    float2 to_particle = uv - particle_pos;

                    float dist;

                    if (_TeardropIntensity < 0.1)
                    {
                        // Original ellipse method
                        float2 ellipse_scale = float2(1.0, _StreamStretch);
                        float2 scaled_offset = to_particle / ellipse_scale;
                        dist = length(scaled_offset) - _ParticleRadius;
                    }
                    else
                    {
                        // Teardrop method
                        dist = teardrop_sdf(to_particle, _ParticleRadius, _StreamStretch * _TeardropIntensity);
                    }

                    if (!found_particle)
                    {
                        closest_distance = dist;
                        found_particle = true;
                    }
                    else
                    {
                        closest_distance = smin(closest_distance, dist, _Smoothing);
                    }
                }

                // Only render if we found particles and are close enough
                if (!found_particle || closest_distance > _Threshold + _OutlineWidth)
                {
                    return fixed4(0.0, 0.0, 0.0, 0.0);
                }
                else if (closest_distance <= _Threshold)
                {
                    // Inside the liquid
                    if (_UseTexture > 0.5)
                    {
                        // Sample texture with tiling based on UV
                        float2 texUV = uv * _TextureScale;
                        return tex2D(_MainTex, texUV);
                    }
                    else
                    {
                        return _LiquidColor;
                    }
                }
                else
                {
                    // Outline
                    return _OutlineColor;
                }
            }
            ENDCG
        }
    }
}