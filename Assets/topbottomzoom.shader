Shader "Custom/TopBottomZoomStereo"
{
    Properties
    {
        _MainTex ("Single Texture (Top-Bottom Stereo)", 2D) = "white" {}
        _Zoom     ("Zoom", Range(1,8)) = 1          // <<< AGGIUNTA MINIMA
        _FocusDir ("Focus Dir (World)", Vector) = (0,0,1,0) // <<< AGGIUNTA MINIMA
        _YawOffsetDeg ("Yaw Offset (deg)", Range(-180,180)) = 180
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        // Manteniamo il tuo culling
        Cull Front

        Pass
        {
            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma target   3.0
            #pragma multi_compile_instancing
            #pragma multi_compile _ UNITY_SINGLE_PASS_STEREO
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;   // x=1/width, y=1/height, z=width, w=height

            float _LeftEyeOnlyMode;

            // === AGGIUNTE ===
            float  _Zoom;
            float3 _FocusDir;
            float _YawOffsetDeg;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;   // tieniamo le tue UV (le usi per mirroring/split)
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos      : SV_POSITION;
                float3 worldPos : TEXCOORD0; // ci serve per ricavare la direzione camera->punto
                float2 uv       : TEXCOORD1; // pass-through delle tue UV (per mirroring/split)
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.pos      = UnityObjectToClipPos(v.vertex);
                o.uv       = v.uv;
                return o;
            }

            // === helper per zoom sferico: slerp tra due direzioni ===
            float3 slerpDir(float3 a, float3 b, float t)
            {
                a = normalize(a); b = normalize(b);
                float d  = clamp(dot(a,b), -1.0, 1.0);
                float th = acos(d);
                if (th < 1e-5) return normalize(lerp(a,b,t));
                float s  = sin(th);
                return normalize((sin((1.0 - t)*th)/s)*a + (sin(t*th)/s)*b);
            }

            // direzione (world) -> UV equirett [0..1] (coerente con inside-out sphere)
            float2 dirToEquirectUV(float3 d)
            {
                d = normalize(d);
                float yaw   = atan2(d.x, d.z) + radians(_YawOffsetDeg); // << offset
                float pitch = asin(clamp(d.y, -1.0, 1.0));

                float u = yaw   * (1.0 / (2.0 * UNITY_PI)) + 0.5;
                float v = pitch * (1.0 /  UNITY_PI)        + 0.5;
                return float2(u, v);
            }


            fixed4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                // === TUA LOGICA: Left Eye Only ===
                int eyeIndex = unity_StereoEyeIndex;
                // Se la modalità "solo occhio sinistro" è attiva, forza sempre la parte superiore
                // (NB: qui assumo che tu stia già definendo _LeftEyeOnlyMode nel tuo progetto.
                // Se il tuo originale lo aveva tra le uniform, aggiungilo nelle Properties/variabili.)
                // Per restare "minimale", non tocco questo blocco: uso eyeIndex come fai tu.
                // Se non hai _LeftEyeOnlyMode, puoi rimuovere questo if.
                // -----
 
                if (_LeftEyeOnlyMode > 0.5) {
                     eyeIndex = 0;
                }
                // -----

                // === Nuovo: ricavo la direzione camera -> punto sulla sfera ===
                float3 camPos = _WorldSpaceCameraPos;
                float3 dir    = normalize(i.worldPos - camPos);

                // === Zoom sferico minimale: avvicino 'dir' alla direzione di fuoco comune ===
                float  z       = max(_Zoom, 1.0);
                float  t       = 1.0 / z;
                float3 focus   = normalize(_FocusDir);
                float3 dirZoom = slerpDir(focus, dir, t);

                // Converto la direzione "zoomata" in UV equirettangolari
                float2 uv = dirToEquirectUV(dirZoom);

                 // Seam fix: wrap + mezzo-texel in U
                uv.x = frac(uv.x);
                float padU = 0.5 * _MainTex_TexelSize.x;  // 0.5 / width
                uv.x = uv.x * (1.0 - 2.0 * padU) + padU;

                fixed4 col;
                if (eyeIndex == 0)
                {
                    uv.y = uv.y * 0.5 + 0.5; // parte superiore = LEFT
                    col = tex2D(_MainTex, uv);
                }
                else
                {
                    uv.y = uv.y * 0.5;       // parte inferiore = RIGHT
                    col = tex2D(_MainTex, uv);
                }

                return col;
            }
            ENDCG
        }
    }
}
