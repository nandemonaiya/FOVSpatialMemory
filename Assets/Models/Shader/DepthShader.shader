// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/DepthShader" {
     Properties {

     }
     SubShader {
          Tags { "RenderType"="Opaque" }
         
          Pass
          {
               CGPROGRAM
               #pragma vertex v
               #pragma fragment p
              
               struct VertOut
               {
                    float4 pos : POSITION;
					float4 depth : TEXCOORD0;
               };
              
               VertOut v( float4 myPos : POSITION )
               {
                    VertOut OUT;
                   
                    OUT.pos = UnityObjectToClipPos( myPos);
					OUT.depth.z = OUT.pos.z;
					OUT.depth.w = OUT.pos.w;
                   
                    return OUT;
               }
              
               struct PixelOut
               {
                    float4 color : COLOR;
               };
              
               PixelOut p( VertOut input )
               {
                    PixelOut OUT;

					float d = input.depth.z/input.depth.w;

					OUT.color =  float4(d,d,d,1);
                   
                    return OUT;
               }
              
               ENDCG
          }
     }
     FallBack "Diffuse"
}