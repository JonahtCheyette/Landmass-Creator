using UnityEngine;

//the class that takes in textures and meshData and applies it to the plane or Mesh apropriately
public class MapPreview : MonoBehaviour {
    //do we wan't to draw a black on white flat texture, make a colored mesh, or a Falloff Map
    public enum DrawMode { NoiseMap, Mesh, FalloffMap };
    public DrawMode drawMode;

    //storing references to the settings we're using this time
    public MeshSettings meshSettings;
    public HeightMapSettings heightMapSettings;
    public TextureData textureData;

    //the material of the terrain
    public Material terrainMaterial;

    //what level of detail we want
    [Range(0, MeshSettings.numSupportedLODs - 1)]
    public int editorLOD;

    //do we want it to update every time we change a value in the editor
    public bool autoUpdate;

    //the renderer for the plane texture
    public Renderer textureRenderer;
    //The thing that finds the mesh in program memory. I think. Filters are confusing
    public MeshFilter meshFilter;
    // the thing that renders the mesh
    public MeshRenderer meshRenderer;

    public void DrawTexture(Texture2D texture) {
        //Changes the texture of the plane and scales the plane up apropriately
        //has to be sharedMaterial, otherwise it will only work at runtime
        textureRenderer.sharedMaterial.mainTexture = texture;
        textureRenderer.transform.localScale = new Vector3(texture.width, 1, texture.height) / 10f;

        //just makes sure the objects are visible
        textureRenderer.gameObject.SetActive(true);
        meshFilter.gameObject.SetActive(false);
    }

    public void DrawMesh(MeshData meshData) {
        //Alters the mesh, and it has to be the shared versions of the mesh and material, otherwise it will only work at runtime
        meshFilter.sharedMesh = meshData.CreateMesh();

        //changes visibility of the objects
        textureRenderer.gameObject.SetActive(false);
        meshFilter.gameObject.SetActive(true);
    }

    //generates a chunk-wide noise and color map, then applys it to a plane or mesh appropriately
    public void DrawMapInEditor() {
        //updates the material
        textureData.ApplyToMaterial(terrainMaterial);
        //update the shader with new min and max height values
        textureData.UpdateMeshHeights(terrainMaterial, heightMapSettings.minHeight, heightMapSettings.maxHeight);
        //generates a map
        HeightMap heightMap = HeightMapGenerator.GenerateHeightMap(meshSettings.numVertsPerLine, meshSettings.numVertsPerLine, heightMapSettings, Vector2.zero);

        if (drawMode == DrawMode.NoiseMap) {
            //generate a black and white texture, apply it to the plane
            DrawTexture(TextureGenerator.TextureFromHeightMap(heightMap));
        } else if (drawMode == DrawMode.Mesh) {
            //generate a colored texture, generate a mesh from the heightmap in the heightMap generated above, color it appropriately
            DrawMesh(MeshGenerator.GenerateTerrainMesh(heightMap.values, meshSettings, editorLOD));
        } else if (drawMode == DrawMode.FalloffMap) {
            //generates a falloff mesh
            DrawTexture(TextureGenerator.TextureFromHeightMap(new HeightMap(FalloffGenerator.GenerateFalloffMap(meshSettings.numVertsPerLine), 0, 1)));
        }
    }

    //what we want to happen when either the terrainData or NoiseData is changed in the editor with autoUpdate on
    void OnValuesUpdated() {
        if (!Application.isPlaying) {
            DrawMapInEditor();
        }
    }

    //ditto, but for textureData
    void OnTextureValuesUpdated() {
        textureData.ApplyToMaterial(terrainMaterial);
    }

    //is called any time a value is changed in the editor
    void OnValidate() {
        //subscribing our OnValuesUpdated method to their actions so that it will be called if we change the values of one of them and they have autoUpdate on
        if (meshSettings != null) {
            //fun fact: this unsubscribe operator doesn't do anything if the method isn't already subscribed to the delegate
            //this is to keep the OnValuesUpdated method in this class from being called 100s of times each time we update a value, as without it
            //the OnValuesUpdated method would be subscribed each time we change a value, and those subscriptions will add up
            meshSettings.OnValuesUpdated -= OnValuesUpdated;
            meshSettings.OnValuesUpdated += OnValuesUpdated;
        }
        if (heightMapSettings != null) {
            heightMapSettings.OnValuesUpdated -= OnValuesUpdated;
            heightMapSettings.OnValuesUpdated += OnValuesUpdated;
        }
        if (textureData != null) {
            textureData.OnValuesUpdated -= OnTextureValuesUpdated;
            textureData.OnValuesUpdated += OnTextureValuesUpdated;
        }
    }
}
