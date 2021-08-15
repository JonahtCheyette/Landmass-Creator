using UnityEngine;

//generates a grid of nums from 0-1 that are how the falloff map is scaled
public static class FalloffGenerator {
    public static float[,] GenerateFalloffMap(int size) {
        float[,] map = new float[size, size];

        for(int i = 0; i < size; i++) {
            for (int j = 0; j < size; j++) {
                //get a num from -1 to 1 that represents distance from center
                float x = i / (float)size * 2 - 1;
                float y = j / (float)size * 2 - 1;
                float value = Mathf.Max(Mathf.Abs(x), Mathf.Abs(y));
                //evaluate it using our expression and then put it in the map
                map[i, j] = Evaluate(value);
            }
        }

        return map;
    }

    //just a mathematical expression that makes the dropoff sharper. put it in desmos if you don't understand
    static float Evaluate(float value) {
        float a = 3f;
        float b = 2.2f;

        return Mathf.Pow(value, a) / (Mathf.Pow(value, a) + Mathf.Pow(b - b * value, a));
    }
}
