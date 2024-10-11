Shader "PointCloud/PointCloud"
{
	Properties
	{
		_Color("Color", Color) = (1, 1, 1, 1)
		_PointSize("Point Size", Float) = 1
		_PointAlpha("Point Alpha", Float) = 1
		_Density("Density", Float) = 1
		_ScreenSize("Screen size", Vector) = (1, 1, 1, 1)
		_PointsTex("Points texture", 2D) = "white" {}
	}
	SubShader
	{
		Tags
		{
			"RenderType"      = "Transparent"
			"IgnoreProjector" = "True"
			"Queue" = "Transparent"
		}
		Pass
		{
			//Blend [_SrcMode][_DstMode]
			Blend SrcAlpha OneMinusSrcAlpha
			//Blend One Zero
			ZWrite On
			ZTest LEqual
			Cull Back

			CGPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag
			#pragma target 4.5

			#include "UnityCG.cginc"

			half _PointAlpha;
			float _PointSize;
			float _Density;
			float4x4 _MVP;
			float4 _ScreenSize;
			int _PointCount;

			struct Point
			{
				float3 pos;
				uint col;
            };
		
			StructuredBuffer<Point> _Points;
			sampler2D _PointsTex;

            struct Attributes
            {
				float3 vertex    : POSITION;
            };

			struct VertexOutput
			{
				float4 vertex    : SV_POSITION;
				half3 color     : COLOR;
			};

			inline half3 EncodeColor32(uint color)
			{
				half b = ((color & 0xff0000) >> 16);
				half g = ((color & 0xff00) >> 8);
				half r = ((color & 0xff));
				return half3(r / 256.0, g / 256.0, b / 256.0);
			}

			VertexOutput Vert(Attributes i, uint instanceID : SV_InstanceID)
			{
				uint pointIndex = floor((instanceID * 16384 + i.vertex.z) * _Density + 0.01);
				pointIndex = min(pointIndex, _PointCount - 1);

				//TO DO Bake positions into Texture for WebGL
				
				// uint x = floor(pointIndex / 2048);
				// uint y = pointIndex - x * 2048;
				//float texSize = 2048;
				//float offSetX = 1;
				//float offSetY = 0;
				//float _x = (texSize - pointIndex - 1)/texSize;
				//float _y = (texSize - 0 - 1)/texSize;
				//float4 data = tex2Dlod(_PointsTex, float4(_x, _y, 0, 0));
				
				Point p = _Points[pointIndex];

				VertexOutput o;
				o.color = EncodeColor32(p.col);
				o.vertex = UnityObjectToClipPos(p.pos);
				o.vertex.x += i.vertex.x * o.vertex.w * _ScreenSize.z;
				o.vertex.y += i.vertex.y * o.vertex.w * _ScreenSize.w;
				return o;
			}

			half4 Frag(VertexOutput i) : SV_TARGET
			{
				return half4(i.color.rgb, _PointAlpha);
			}
			ENDCG
		} // Pass
	} // SubShader
} // Shader