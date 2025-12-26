// Shader "Custom/WeaponMultiTexture"
Shader "Custom/WeaponMultiTexture"
{
    Properties
    {
        _MainTex ("Base Texture", 2D) = "white" {}
        _FrostTex ("Frost Texture", 2D) = "white" {}
        _DamageTex ("Damage Texture", 2D) = "white" {}
        _FrostAmount ("Frost Amount", Range(0, 1)) = 0.0
        _DamageAmount ("Damage Amount", Range(0, 1)) = 0.0
        _FrostColor ("Frost Color", Color) = (0.8, 0.9, 1.0, 1.0)
        _DamageColor ("Damage Color", Color) = (0.3, 0.2, 0.1, 1.0)
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
        
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
            
            sampler2D _MainTex;
            sampler2D _FrostTex;
            sampler2D _DamageTex;
            float4 _MainTex_ST;
            float _FrostAmount;
            float _DamageAmount;
            float4 _FrostColor;
            float4 _DamageColor;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // Основная текстура
                fixed4 base = tex2D(_MainTex, i.uv);
                
                // Текстура изморози
                fixed4 frost = tex2D(_FrostTex, i.uv);
                frost.rgb *= _FrostColor.rgb;
                
                // Текстура повреждений
                fixed4 damage = tex2D(_DamageTex, i.uv);
                damage.rgb *= _DamageColor.rgb;
                
                // Смешивание
                fixed4 result = base;
                
                // Наложение повреждений (используем multiply/overlay)
                if (_DamageAmount > 0)
                {
                    result.rgb = lerp(result.rgb, damage.rgb * result.rgb, _DamageAmount);
                }
                
                // Наложение изморози (добавляем поверх)
                if (_FrostAmount > 0)
                {
                    // Иморозь добавляет белый оттенок и собственную текстуру
                    fixed frostEffect = frost.r * _FrostAmount;
                    result.rgb = lerp(result.rgb, result.rgb + frost.rgb * frostEffect, _FrostAmount * 0.5);
                    result.rgb += frost.rgb * frostEffect * 0.3;
                }
                
                return result;
            }
            ENDCG
        }
    }
}
