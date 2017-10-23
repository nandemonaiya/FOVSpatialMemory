// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader"Custom/DiffuseWithShadow"
{
	Properties
	{
		_MainTex ("Main Texture", 2D) = "white" {}
		_ShadowMap ("Shadow Map", 2D) = "white" {}
		_Angle ("Angle", Float) = 0.0
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 200

		Pass
		{
			CGPROGRAM
			// Upgrade NOTE: excluded shader from OpenGL ES 2.0 because it does not contain a surface program or both vertex and fragment programs.
			#pragma exclude_renderers gles

			#pragma vertex v
			#pragma fragment p

			uniform sampler2D _MainTex;
			uniform sampler2D _ShadowMap;
			uniform float4x4 _LightViewProj;
			uniform float _Angle;


			struct VertexOut
			{
				float4 position : POSITION;
				float2 uv : TEXCOORD0;
				float4 proj : TEXCOORD1;
			} ;

			VertexOut v( float4 position : POSITION, float2 uv : TEXCOORD0 )
			{
				VertexOut OUT;

				OUT.position =  UnityObjectToClipPos( position );
				OUT.uv = uv;
				OUT.proj = mul( mul( unity_ObjectToWorld, float4(position.xyz, 1)), _LightViewProj );

				return OUT;
			}

			struct PixelOut
			{
				float4	color : COLOR;
			} ;

			PixelOut p(VertexOut IN)
			{
				PixelOut OUT;

				float2 ndc = float2(IN.proj.x/IN.proj.w, IN.proj.y/IN.proj.w);
				float2 uv = (1 + float2( ndc.x, ndc.y)) * 0.5;

				float theta = _Angle*3.14159/ 180;
				float2x2 matRot = float2x2( cos(theta), sin(theta),
											-sin(theta), cos(theta) );
				uv = mul( uv, matRot);

				float4 c = tex2D( _ShadowMap, uv );

				if( uv.x < 0 || uv.y < 0 ||
					uv.x > 1  || uv.y > 1 || c.a <= 0.00f )
					{
						c = tex2D(_MainTex, IN.uv);
					}

				OUT.color = c;

				return OUT;
			}

			ENDCG
		}

	} 
	FallBack"Diffuse"
}


