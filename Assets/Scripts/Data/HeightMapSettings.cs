using UnityEngine;

//This tag just makes it automatically show up in the create menu in the unity editor and stored in the project as .asset file
[CreateAssetMenu()]
public class HeightMapSettings : UpdatableData {
    //the settings for the noise
    public NoiseSettings noiseSettings;

    //how much we want to scale up the y values and the AnimationCurve is for keeping the oceans flat
    public float heightMultiplier;
    public AnimationCurve heightCurve;

    //do we want it to be a falloff map
    public bool useFalloff;

    //min and max height valuess for the mesh
    public float minHeight {
        get {
            return heightMultiplier * heightCurve.Evaluate(0);
        }
    }
    public float maxHeight {
        get {
            return heightMultiplier * heightCurve.Evaluate(1);
        }
    }

    //what these lines do is to tell unity to not compile these lines in a standalone build
    #if UNITY_EDITOR
    protected override void OnValidate() {
        noiseSettings.ValidateValues();
        base.OnValidate();
    }
    #endif
}
