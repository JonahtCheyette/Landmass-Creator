using UnityEngine;

//Class in vharge of the actual generation of heightmaps using perlin noise
public static class Noise {
    //whether to normalize based on only one chunk or all chunks
    public enum NormalizeMode { Local, Global };

    //generates the noise map
    public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, NoiseSettings settings, Vector2 sampleCenter) {
        // the map to be filled
        float[,] noiseMap = new float[mapWidth, mapHeight];

        //the RNG generator
        System.Random psuedoRNG = new System.Random(settings.seed);
        //offsetting the octaves so we don't end up with the same values, just compressed
        Vector2[] octaveOffsets = new Vector2[settings.octaves];

        //for normalization and offsets
        float maxPossibleHeight = 0;
        float amplitude = 1;
        float frequency = 1;

        //genreating the actual offsets
        for (var i = 0; i < settings.octaves; i++) {
            //we subtract from the y value in order to flip the y axis
            float offsetX = psuedoRNG.Next(-100000, 100000) + settings.offset.x + sampleCenter.x;
            float offsetY = psuedoRNG.Next(-100000, 100000) - settings.offset.y - sampleCenter.y;

            octaveOffsets[i] = new Vector2(offsetX, offsetY);

            //for global normalization
            maxPossibleHeight += amplitude;
            amplitude *= settings.persistance;
        }

        //for local normalization
        float maxLocalNoiseHeight = float.MinValue;
        float minLocalNoiseHeight = float.MaxValue;

        //makes it so that when we scale the noise, we zoom in on the center
        float halfWidth = mapWidth / 2f;
        float halfHeight = mapHeight / 2f;

        for (int y = 0; y < mapHeight; y++) {
            for (int x = 0; x < mapWidth; x++) {
                //dealing with octaves
                amplitude = 1;
                frequency = 1;
                //for local normalization
                float noiseHeight = 0;

                for (int i = 0; i < settings.octaves; i++) {
                    //generating the noise
                    float sampleX = (x - halfWidth + octaveOffsets[i].x) / settings.scale * frequency;
                    float sampleY = (y - halfHeight + octaveOffsets[i].y) / settings.scale * frequency;

                    //scaling it to [-1, 1)
                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
                    //getting the noise height of that part of the map
                    noiseHeight += perlinValue * amplitude;

                    //dealing with octaves
                    amplitude *= settings.persistance;
                    frequency *= settings.lacunarity;
                }

                //keeping track of min and max for local normalization
                if (noiseHeight > maxLocalNoiseHeight) {
                    maxLocalNoiseHeight = noiseHeight;
                }
                if (noiseHeight < minLocalNoiseHeight) {
                    minLocalNoiseHeight = noiseHeight;
                }

                //filling in the actual noise map
                noiseMap[x, y] = noiseHeight;

                //global normalization
                if (settings.normalizeMode == NormalizeMode.Global) {
                    float normalizedHeight = (noiseMap[x, y] + 1) / (maxPossibleHeight);
                    noiseMap[x, y] = Mathf.Clamp(normalizedHeight, 0, int.MaxValue);
                }
            }
        }

        //local normalization
        if (settings.normalizeMode == NormalizeMode.Local) {
            for (int y = 0; y < mapHeight; y++) {
                for (int x = 0; x < mapWidth; x++) {
                    //doing the normalization
                    noiseMap[x, y] = Mathf.InverseLerp(minLocalNoiseHeight, maxLocalNoiseHeight, noiseMap[x, y]);
                }
            }
        }

        return noiseMap;
    }

}

//holds everything you need to generate noise
[System.Serializable()]
public class NoiseSettings {
    //do we want to normalize it globally or locally
    public Noise.NormalizeMode normalizeMode;

    //how much we want the noise to scale
    public float scale = 50;

    //dealing with octaves
    public int octaves = 8;
    [Range(0, 1)]
    public float persistance = 0.5f;
    public float lacunarity = 2;

    //the RNG seed
    public int seed;
    //just for moving around the noise
    public Vector2 offset;

    //clamp all the values
    public void ValidateValues() {
        scale = Mathf.Max(scale, 0.001f);
        octaves = Mathf.Max(octaves, 1);
        lacunarity = Mathf.Max(lacunarity, 1);
        persistance = Mathf.Clamp01(persistance);
    }
}