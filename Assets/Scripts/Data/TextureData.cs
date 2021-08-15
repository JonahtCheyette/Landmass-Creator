using System.Linq;
using UnityEngine;

[CreateAssetMenu()]
public class TextureData : UpdatableData {
    //size of each individual texture
    const int textureSize = 512;
    //the format of the textures in the Texture2DArray, RGB565 is a 16bit color format
    const TextureFormat textureFormat = TextureFormat.RGB565;

    //the layers to use
    public Layer[] layers;

    //the last used min and max heights
    float savedMinHeight;
    float savedMaxHeight;
    
    public void ApplyToMaterial(Material material) {
        //giving all our info to the material
        material.SetInt("layerCount", layers.Length);
        material.SetColorArray("baseColors", layers.Select(x => x.tint).ToArray());
        material.SetFloatArray("baseStartHeights", layers.Select(x => x.startHeight).ToArray());
        material.SetFloatArray("baseBlends", layers.Select(x => x.blendStrength).ToArray());
        material.SetFloatArray("baseColorStrengths", layers.Select(x => x.tintStrength).ToArray());
        material.SetFloatArray("baseTextureScales", layers.Select(x => x.textureScale).ToArray());
        Texture2DArray texturesArray = GenerateTextureArray(layers.Select(x => x.texture).ToArray());
        material.SetTexture("baseTextures", texturesArray);
        UpdateMeshHeights(material, savedMinHeight, savedMaxHeight);
    }

    //updates the material's min and max height variables
    public void UpdateMeshHeights(Material material, float minHeight, float maxHeight) {
        savedMinHeight = minHeight;
        savedMaxHeight = maxHeight;

        material.SetFloat("minHeight", minHeight);
        material.SetFloat("maxHeight", maxHeight);
    }

    //for passing into the material
    Texture2DArray GenerateTextureArray(Texture2D[] textures) {
        Texture2DArray textureArray = new Texture2DArray(textureSize, textureSize, textures.Length, textureFormat, true);
        for(int i = 0; i < textures.Length; i++) {
            textureArray.SetPixels(textures[i].GetPixels(), i);
        }
        textureArray.Apply();
        return textureArray;
    }

    //a class to hold all the necessary data for a layer
    [System.Serializable]
    public class Layer {
        public Texture2D texture;
        public Color tint;
        [Range(0,1)]
        public float tintStrength;
        [Range(0, 1)]
        public float startHeight;
        [Range(0, 1)]
        public float blendStrength;
        public float textureScale;
    }
}
