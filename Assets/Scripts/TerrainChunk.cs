using UnityEngine;

public class TerrainChunk {
    //how close the player has to be to the edge of the terrain chunk for it to load its collider
    const float colliderGenerationDistanceThreshold = 5;

    //a delegate for adding/removing the terrain chunk from the visibleTerrainChunks list
    public event System.Action<TerrainChunk, bool> onVisibilityChanged;

    //the coordinate of the terrain chunk
    public Vector2 coord;

    //the mesh object and position of the terrain chunk in world space, sans z axis
    GameObject meshObject;
    Vector2 sampleCenter;
    //the bounding box of the mesh
    Bounds bounds;

    //for rendering and handling LODs
    MeshRenderer meshRenderer;
    MeshFilter meshFilter;
    MeshCollider meshCollider;

    //what the data of the LOD are and holding each LOD mesh for this terrain chunk that's already been used
    LODInfo[] detailLevels;
    LODMesh[] lodMeshes;
    int colliderLODIndex;

    //mapData, and have we received it from the thread yet
    HeightMap heightMap;
    bool heightMapRecieved;
    //making sure we start off by loading the correct LOD
    int previousLODIndex = -1;
    //making sure we don't set the collider multiple times
    bool hasSetCollider;
    float maxViewDist;

    HeightMapSettings heightMapSettings;
    MeshSettings meshSettings;

    Transform viewer;

    public TerrainChunk(Vector2 coord, HeightMapSettings heightMapSettings, MeshSettings meshSettings, LODInfo[] detailLevels, int colliderLODIndex, Transform parent, Transform viewer, Material material) {
        this.coord = coord;
        this.detailLevels = detailLevels;
        this.colliderLODIndex = colliderLODIndex;
        this.heightMapSettings = heightMapSettings;
        this.meshSettings = meshSettings;
        this.viewer = viewer;

        //setting the position in the space based on chunk coords
        sampleCenter = coord * meshSettings.meshWorldSize / meshSettings.meshScale;
        //setting the position in the space based on chunk coords
        Vector2 position = coord * meshSettings.meshWorldSize;
        //setting the bounds of the chunk based on position and size
        bounds = new Bounds(position, Vector2.one * meshSettings.meshWorldSize);

        //creates a new Game object, adds a renderer, gets a reference to its material, gives it a filter, and positions it correctly, and scales it up correctly
        meshObject = new GameObject("Terrain Chunk");
        meshRenderer = meshObject.AddComponent<MeshRenderer>();
        meshRenderer.material = material;
        meshFilter = meshObject.AddComponent<MeshFilter>();
        meshCollider = meshObject.AddComponent<MeshCollider>();

        meshObject.transform.position = new Vector3(position.x, 0, position.y);
        meshObject.transform.parent = parent;
        SetVisible(false);

        //creating the LODMeshes, although not creating meshes from them yet
        lodMeshes = new LODMesh[detailLevels.Length];
        for (int i = 0; i < lodMeshes.Length; i++) {
            lodMeshes[i] = new LODMesh(detailLevels[i].lod);
            lodMeshes[i].updateCallback += UpdateTerrainChunk;
            if (i == colliderLODIndex) {
                lodMeshes[i].updateCallback += UpdateCollisionMesh;
            }
        }

        maxViewDist = detailLevels[detailLevels.Length - 1].visibleDistThreshhold;
    }

    //we only want this to happen after the OnTerrainChunkVisibilityChanged method has been subscribed to the onVisibilityChanged event in order to keep the terrain chunk from changing visibility without being added/removed from the terrainchunksvisible list
    public void Load() {
        //gets the mapData
        ThreadedDataRequester.RequestData(() => HeightMapGenerator.GenerateHeightMap(meshSettings.numVertsPerLine, meshSettings.numVertsPerLine, heightMapSettings, sampleCenter), OnHeightMapRecieved);
    }

    //The callback for when we receive the mapData from MapGenerator.cs
    void OnHeightMapRecieved(object heightMapObject) {
        heightMap = (HeightMap)heightMapObject;
        heightMapRecieved = true;

        //update yourself
        UpdateTerrainChunk();
    }

    Vector2 viewerPosition {
        get {
            return new Vector2(viewer.position.x, viewer.position.z);
        }
    }

    public void UpdateTerrainChunk() {
        if (heightMapRecieved) {
            //checking visibility based on distance from the viewer to the nearest edge
            float viewerDistFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));
            bool wasVisible = IsVisible();
            bool visible = viewerDistFromNearestEdge <= maxViewDist;

            if (visible) {
                //figuring out what LOD the chunk should be based on player distances
                int lodIndex = 0;
                for (int i = 0; i < detailLevels.Length - 1; i++) {
                    if (viewerDistFromNearestEdge > detailLevels[i].visibleDistThreshhold) {
                        lodIndex = i + 1;
                    } else {
                        break;
                    }
                }

                //if we need to change the LOD, load it if we have it, else request it from the mapGeneratorClass
                if (lodIndex != previousLODIndex) {
                    LODMesh lodMesh = lodMeshes[lodIndex];
                    if (lodMesh.hasMesh) {
                        previousLODIndex = lodIndex;
                        meshFilter.mesh = lodMesh.mesh;
                        //meshCollider.sharedMesh = lodMesh.mesh;
                    } else if (!lodMesh.meshHasBeenRequested) {
                        lodMesh.RequestMesh(heightMap, meshSettings);
                    }
                }
            }
            //setting visibility
            if (wasVisible != visible) {
                if (onVisibilityChanged != null) {
                    onVisibilityChanged(this, visible);
                }
                //setting visibility
                SetVisible(visible);
            }
        }
    }

    //checks to see if the player is in range, and loads the collsion mesh if so
    public void UpdateCollisionMesh() {
        if (!hasSetCollider) {
            float sqrDstFromViewerToEdge = bounds.SqrDistance(viewerPosition);

            //if we haven't yet requested the meshData, we REALLY need to
            if (sqrDstFromViewerToEdge < detailLevels[colliderLODIndex].sqrVisibleDistThreshhold) {
                if (!lodMeshes[colliderLODIndex].meshHasBeenRequested) {
                    lodMeshes[colliderLODIndex].RequestMesh(heightMap, meshSettings);
                }
            }

            if (sqrDstFromViewerToEdge < colliderGenerationDistanceThreshold * colliderGenerationDistanceThreshold) {
                if (lodMeshes[colliderLODIndex].hasMesh) {
                    meshCollider.sharedMesh = lodMeshes[colliderLODIndex].mesh;
                    hasSetCollider = true;
                }
            }
        }
    }

    //setting visibility
    public void SetVisible(bool visible) {
        meshObject.SetActive(visible);
    }

    //checking if the mesh is visible
    public bool IsVisible() {
        return meshObject.activeSelf;
    }
}

class LODMesh {
    public Mesh mesh;
    public bool meshHasBeenRequested;
    public bool hasMesh;
    int lod;
    public event System.Action updateCallback;

    //just instantiates the LODMesh with correct data
    public LODMesh(int lod) {
        this.lod = lod;
    }

    //The function for when the thread finishes generating the meshData
    void OnMeshDataRecieved(object meshDataObject) {
        mesh = ((MeshData)meshDataObject).CreateMesh();
        hasMesh = true;

        updateCallback();
    }

    //requesting the meshData
    public void RequestMesh(HeightMap heightMap, MeshSettings meshSettings) {
        meshHasBeenRequested = true;
        ThreadedDataRequester.RequestData(() => MeshGenerator.GenerateTerrainMesh(heightMap.values, meshSettings, lod), OnMeshDataRecieved);
    }
}
