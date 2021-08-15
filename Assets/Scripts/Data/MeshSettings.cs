using UnityEngine;

[CreateAssetMenu()]
public class MeshSettings : UpdatableData {
   
    //are we using flatshading or not
    public bool useFlatShading;

    //just scales up the terrain chunks on all axes
    public float meshScale = 2.5f;

    //# of supported LOD and chunk sizes
    public const int numSupportedLODs = 5;
    public const int numSuppportedChunkSizes = 9;
    public const int numSuppportedFlatshadedChunkSizes = 3;
    public static readonly int[] supportedChunkSizes = { 48, 72, 96, 120, 144, 168, 192, 216, 240 };

    //what chunk size to use
    [Range(0, numSuppportedChunkSizes - 1)]
    public int chunkSizeIndex;
    [Range(0, numSuppportedFlatshadedChunkSizes - 1)]
    public int flatshadedChunkSizeIndex;

    //num verts per line of a mesh rendered with LOD = 0. includes the 2 extra verticies that are excluded in final mesh but used for calculating normals
    public int numVertsPerLine {
        get {
            return supportedChunkSizes[(useFlatShading) ? flatshadedChunkSizeIndex : chunkSizeIndex] + 5;
        }
    }

    public float meshWorldSize {
        get {
            return (numVertsPerLine - 3) * meshScale;
        }
    }
}
