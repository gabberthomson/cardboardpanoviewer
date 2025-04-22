Shader "Custom/TopBottomStereo" // o "Custom/SingleTextureTopBottomStereo" se stai usando quello
{
    Properties
    {
        _MainTex ("Single Texture (Top-Bottom Stereo)", 2D) = "white" {} // Se usi SingleTextureTopBottomStereo
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Cull Front // Modifica: Imposta il culling a "Front"

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL; // Aggiungi le normali all'input
                float2 uv : TEXCOORD0;
            }; 

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex; // Se usi SingleTextureTopBottomStereo
            float _LeftEyeOnlyMode;

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                //v.normal = -v.normal; // Modifica: Inverti le normali qui**
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                fixed4 col = fixed4(0,0,0,1);

                int eyeIndex = unity_StereoEyeIndex;
                // Se la modalità "solo occhio sinistro" è attiva, forza sempre la parte superiore
                if (_LeftEyeOnlyMode > 0.5) {
                    eyeIndex = 0;
                }
                float2 uv = i.uv;
                uv.x = 1.0 - uv.x; // Corregge il mirroring
                if (eyeIndex == 0)
                {
                    uv.y = uv.y * 0.5 + 0.5;
                    col = tex2D(_MainTex, uv); 
                }
                else
                {
                    uv.y = uv.y * 0.5;
                    col = tex2D(_MainTex, uv);
                }



                return col;
            }
            ENDCG
        }
    }
}