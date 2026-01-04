Shader "Custom/CloudShadows"
{
    Properties
    {
        _CloudTex ("Cloud Texture", 2D) = "white" {}  // Текстура облаков
        _BaseTex ("Base Texture", 2D) = "white" {}    // Основная текстура спрайта (опционально)
        _ShadowColor ("Shadow Color", Color) = (0.5, 0.5, 0.5, 0.3)
        _Speed ("Scroll Speed", Vector) = (0.5, 0.2, 0, 0)
        _Tiling ("Tiling", Vector) = (3, 3, 0, 0)
        _CloudOpacity ("Cloud Opacity", Range(0, 1)) = 0.5
        _UseBaseTexture ("Use Base Texture", Float) = 0
    }
    
    SubShader
    {
        Tags 
        { 
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }
        
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
                float4 color : COLOR;
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float2 cloudUV : TEXCOORD1;
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
            };
            
            sampler2D _CloudTex;
            sampler2D _BaseTex;
            float4 _CloudTex_ST;
            float4 _BaseTex_ST;
            float4 _ShadowColor;
            float2 _Speed;
            float2 _Tiling;
            float _CloudOpacity;
            float _UseBaseTexture;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                
                // UV для основной текстуры
                o.uv = TRANSFORM_TEX(v.uv, _BaseTex);
                
                // UV для облаков с тайлингом и анимацией
                o.cloudUV = v.uv * _Tiling;
                o.cloudUV += _Speed * _Time.y;
                
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // Получаем цвет облаков
                fixed4 cloudColor = tex2D(_CloudTex, i.cloudUV);
                
                // Если используем базовую текстуру
                fixed4 finalColor;
                if (_UseBaseTexture > 0.5)
                {
                    fixed4 baseColor = tex2D(_BaseTex, i.uv);
                    // Смешиваем базовую текстуру с облаками
                    finalColor = lerp(baseColor, cloudColor * _ShadowColor, _CloudOpacity);
                    finalColor.a = baseColor.a * i.color.a;
                }
                else
                {
                    // Только облака
                    finalColor = cloudColor * _ShadowColor * i.color;
                    finalColor.a *= _CloudOpacity;
                }
                
                return finalColor;
            }
            ENDCG
        }
    }
}