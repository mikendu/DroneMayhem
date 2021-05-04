// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Tools/Color Volume"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
		_Alpha("Alpha", Range(0.0, 1.0)) = 0.0
    }

    SubShader
    {
		Tags{ "Queue" = "Transparent" "RenderType" = "Transparent" }
		Blend SrcAlpha OneMinusSrcAlpha
		ZWrite Off
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vertexShader
			#pragma fragment fragmentShader
			#include "UnityCG.cginc"
			#include "./Common.cginc"

			fixed4 _Color;
			float _Alpha;

			int _gradientMode;
			int _useGradient;
			sampler2D _gradient;

			/*
			float4 origin;
			float4 forward;
			float4 up;
			float4 right;
			*/

			float4 scale;

			struct vertexInput
			{
				float4 vertex : POSITION;
			};

			struct vertToFrag
			{
				float4 vertex : SV_POSITION;
				float4 worldSpacePosition: TEXCOORD1;
			};

			// Main Vertex Shader
			vertToFrag vertexShader(vertexInput input)
			{
				vertToFrag output;
				output.vertex = UnityObjectToClipPos(input.vertex);
				output.worldSpacePosition = mul(unity_ObjectToWorld, input.vertex);
				return output;
			}

			float3 evaluateColor(float3 modelPosition)
			{
				if (_useGradient)
				{
					float sampleIndex = 0.0;
					[forcecase] switch (_gradientMode)
					{
					case 0:
						sampleIndex = invLerp(-0.5, 0.5, modelPosition.x);
						break;

					case 1:
						float angle = atan2(modelPosition.z, modelPosition.x);
						float normalized = (angle + PI) / (2.0 * PI);
						sampleIndex = clamp(normalized, 0.0, 1.0);
						break;

					case 2:
						float radialDistance = length(modelPosition.xz);
						sampleIndex = pow(radialDistance / 0.525, 1.25);
						break;

					case 3:
						float sphericalDistance = length(modelPosition);
						sampleIndex = pow(sphericalDistance / 0.525, 1.25);
						break;
					}
					return tex2D(_gradient, float2(sampleIndex, 0)).rgb;
				}
				else
				{
					return _Color.rgb;
				}
			}

			float3 getModelPosition(float3 worldSpacePosition)
			{
				/*
				float3 offset = worldSpacePosition - origin;
				float x = dot(offset, right) / scale.x;
				float y = dot(offset, up) / scale.y;
				float z = dot(offset, forward) / scale.z;
				return float3(x, y, z);*/
				return mul(unity_WorldToObject, float4(worldSpacePosition, 1.0)).xyz;

			}

			// Main Fragment Shader
			fixed4 fragmentShader(vertToFrag input) : SV_Target
			{
				fixed4 result = 0;
				result.a = _Alpha;

				float3 cameraPos = _WorldSpaceCameraPos;
				float3 rayDirection = normalize(input.worldSpacePosition - cameraPos);
				float marchDistance = calculateMarchDistance(cameraPos, rayDirection);


				int numSteps = 30;
				float stepSize = marchDistance / numSteps;
				float3 position = input.worldSpacePosition;
				float3 accumulatedColor = 0;
				float multiplier = 1.5 * stepSize;
				float totalSize = length(scale);
				multiplier *= (1.75 / totalSize);
				//if (marchDistance > 0)
					//multiplier *= (.50 / marchDistance);
				
				for (int step = 0; step < numSteps; step++)
				{
					//float multiplier = 0.05;
					//float multiplier = 1.5 * stepSize * exp(-distanceTraveled);
					float3 currentColor = multiplier * evaluateColor(getModelPosition(position));
					accumulatedColor += (currentColor);
					position += (stepSize * rayDirection);
				}

				result.rgb += accumulatedColor;
				result.rgb += (0.25 * evaluateColor(getModelPosition(position)));
				result.rgb += (0.25 * evaluateColor(getModelPosition(input.worldSpacePosition)));

				return result;
			}
			ENDCG
		}
    }
}
