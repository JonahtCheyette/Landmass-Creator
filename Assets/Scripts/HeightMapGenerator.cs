using UnityEngine;

public static class HeightMapGenerator {
    
    public static HeightMap GenerateHeightMap(int width, int height, HeightMapSettings settings, Vector2 sampleCenter) {
        //get the base perlin noise we generate the heightmap from
        float[,] values = Noise.GenerateNoiseMap(width, height, settings.noiseSettings, sampleCenter);

        //the animation curve has some optimizations that cause it to give incorrect values if it is accessed by multiple threads at once
        //so in order to prevent this from happening, we make a copy
        AnimationCurve heightCurve_threadSafe = new AnimationCurve(settings.heightCurve.keys);

        //used for local normalization
        float minValue = float.MaxValue;
        float maxValue = float.MinValue;

        //dealing with falloff maps
        if (settings.useFalloff) {
            float[,] fMap = FalloffGenerator.GenerateFalloffMap(width);
            for (int i = 0; i < width; i++) {
                for (int j = 0; j < height; j++) {
                    values[i, j] = Mathf.Clamp01(values[i, j] - fMap[i, j]);
                }
            }
        }

        //evaluating the heightcurve on everything and fincing the min and max values
        for (int i = 0; i < width; i++) {
            for (int j = 0; j < height; j++) {
                values[i, j] = heightCurve_threadSafe.Evaluate(values[i, j]) * settings.heightMultiplier;

                if (values[i, j] > maxValue) {
                    maxValue = values[i, j];
                }
                if (values[i, j] < minValue) {
                    minValue = values[i, j];
                }
            }
        }
        return new HeightMap(values, minValue, maxValue);
    }
}

//holds all the data of note about our heightmaps
public struct HeightMap {
    public readonly float[,] values;
    public readonly float minValue;
    public readonly float maxValue;

    public HeightMap(float[,] values, float minValue, float maxValue) {
        this.values = values;
        this.minValue = minValue;
        this.maxValue = maxValue;
    }
}
