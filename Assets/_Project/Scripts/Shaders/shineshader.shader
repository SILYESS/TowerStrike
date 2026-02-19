Shader "UI/ShineDiagonal"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _ShineColor ("Shine Color", Color) = (0.9,1,0.9,0.2)
        _ShineWidth ("Shine Width", Range(0,1)) = 0.25
        _Speed ("Speed", Float) = 0.6
        _PauseTime ("Pause Time", Float) = 0.5
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _ShineColor;
            float _ShineWidth;
            float _Speed;
            float _PauseTime;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * i.color;

                float diag = (i.uv.x + i.uv.y) * 0.5;

                // Total sweep length in UV space
                float sweepLength = 1 + _ShineWidth * 2;

                // Total cycle time = sweep duration + pause
                float cycleTime = sweepLength / _Speed + _PauseTime;

                // Time in current cycle
                float tCycle = fmod(_Time.y, cycleTime);

                float t;

                if (tCycle < sweepLength / _Speed)
                {
                    // Moving shine
                    t = tCycle * _Speed - _ShineWidth;
                }
                else
                {
                    // Pause at the end
                    t = sweepLength - _ShineWidth;
                }

                float shine = smoothstep(t - _ShineWidth, t, diag) *
                              smoothstep(t + _ShineWidth, t, diag);

                col.rgb = lerp(col.rgb, col.rgb + _ShineColor.rgb, shine * _ShineColor.a);

                return col;
            }
            ENDCG
        }
    }
}
