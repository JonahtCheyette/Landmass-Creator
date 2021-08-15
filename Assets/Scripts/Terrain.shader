Shader "Custom/Terrain" {
    Properties{
        testTexture("Texture", 2D) = "white"{}
        testScale("Scale", float) = 1
    }
    SubShader{
        Tags { "RenderType" = "Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        const static int maxLayerCount = 8;
        const static float epsilon = 1E-4;

        //data for coloration
        int layerCount;
        float3 baseColors[maxLayerCount];
        float baseStartHeights[maxLayerCount];
        float baseBlends[maxLayerCount];
        float baseColorStrengths[maxLayerCount];
        float baseTextureScales[maxLayerCount];

        //max and min heights, for gradients
        float minHeight;
        float maxHeight;

        sampler2D testTexture;
        float testScale;

        //creating a texture array
        UNITY_DECLARE_TEX2DARRAY(baseTextures);

        struct Input {
            //gets the world position of every pixel
            float3 worldPos;
            //gets the normal of the mesh there
            float3 worldNormal;
        };

        float inverseLerp(float min, float max, float val) {
            //the saturate function clamps its value between 0 and 1
            return saturate((val - min) / (max - min));
        }

        //a texture mapping technique called triplanar mapping. we blend between each projection depending on the normal
        float3 triplanar(float3 worldPos, float scale, float3 blendAxes, int textureIndex) {
            float3 scaledWorldPos = worldPos / scale;
            float3 xProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.y, scaledWorldPos.z, textureIndex)) * blendAxes.x;
            float3 yProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.x, scaledWorldPos.z, textureIndex)) * blendAxes.y;
            float3 zProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.x, scaledWorldPos.y, textureIndex)) * blendAxes.z;
            return xProjection + yProjection + zProjection;
        }

        void surf (Input IN, inout SurfaceOutputStandard o) {
            float heightPercent = inverseLerp(minHeight, maxHeight, IN.worldPos.y);
            //the normal
            float3 blendAxes = abs(IN.worldNormal);
            blendAxes /= blendAxes.x + blendAxes.y + blendAxes.z;
            for (int i = 0; i < layerCount; i++) {
                //interpolates from 0 when the height is half a baseBlends below that base's starting height to 1 when the height is half a baseBlends above the starting height
                float drawStrength = inverseLerp(-baseBlends[i]/2 - epsilon, baseBlends[i]/2, heightPercent - baseStartHeights[i]);

                //get the triplanar mapped textures, tint them according to the tint strengths set
                float3 baseColor = baseColors[i] * baseColorStrengths[i];
                float3 textureColor = triplanar(IN.worldPos, baseTextureScales[i], blendAxes, i) * (1 - baseColorStrengths[i]);

                o.Albedo = o.Albedo * (1 - drawStrength) + (baseColor + textureColor) * drawStrength;
            }

        }
        ENDCG
    }
    FallBack "Diffuse"
}
