using System.Collections.Generic;
using UnityEngine;

//does all the handling of generating the endless terrain
public class TerrainGenerator : MonoBehaviour{
    //how far the viewer has to move from the last update in order to update again
    const float viewerMoveThresholdForChunkUpdate = 25f;
    const float sqrviewerMoveThresholdForChunkUpdate = viewerMoveThresholdForChunkUpdate * viewerMoveThresholdForChunkUpdate;

    // which LOD to use for colliding
    public int colliderLODIndex;
    //the levels of detail
    public LODInfo[] detailLevels;

    public MeshSettings meshSettings;
    public HeightMapSettings heightMapSettings;
    public TextureData textureSettings;

    //position of the viewer
    public Transform viewer;
    public Material mapMaterial;

    //x-z coordinates of the viewer, static for easy access from other classes
    Vector2 viewerPosition;
    //for keeeping track of the old viewer position for comparison for updates
    Vector2 oldViewerPosition;

    float meshWorldSize;
    int chunksVisibleInViewDist;

    //contains all the chunks loaded, ever, so we don't have to waste resources loading old chunks
    Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
    //contains exactly what you'd think
    List<TerrainChunk> visibleTerrainChunks = new List<TerrainChunk>();

    void Start() {
        //give all the data to the shader
        textureSettings.ApplyToMaterial(mapMaterial);
        //update the material with new min and max height values
        textureSettings.UpdateMeshHeights(mapMaterial, heightMapSettings.minHeight, heightMapSettings.maxHeight);

        //setting the maxViewDist
        float maxViewDist = detailLevels[detailLevels.Length - 1].visibleDistThreshhold;
        meshWorldSize = meshSettings.meshWorldSize;
        chunksVisibleInViewDist = Mathf.RoundToInt(maxViewDist / meshWorldSize);

        UpdateVisibleChunks();
    }

    void Update() {
        //update the viewer position
        viewerPosition = new Vector2(viewer.position.x, viewer.position.z);

        //update the collider, if necessary
        if(viewerPosition != oldViewerPosition) {
            foreach(TerrainChunk chunk in visibleTerrainChunks) {
                chunk.UpdateCollisionMesh();
            }
        }

        //if we've moved far enough
        if((oldViewerPosition - viewerPosition).sqrMagnitude > sqrviewerMoveThresholdForChunkUpdate) {
            //update the oldViewerPosition and update all chunks
            oldViewerPosition = viewerPosition;
            UpdateVisibleChunks();
        }
    }

    void UpdateVisibleChunks() {
        //this is for keeping track of which chunks have already been updated so we don't waste time doing so twice
        HashSet<Vector2> alreadyUpdatedChunkCoords = new HashSet<Vector2>();
        //getting rid of all the old chunks
        for(int i = visibleTerrainChunks.Count - 1; i >= 0; i--) {
            alreadyUpdatedChunkCoords.Add(visibleTerrainChunks[i].coord);
            visibleTerrainChunks[i].UpdateTerrainChunk();
        }

        // the coodinates of the chunk the player is currently in
        int currentChunkCoordX = Mathf.RoundToInt (viewerPosition.x / meshWorldSize);
        int currentChunkCoordY = Mathf.RoundToInt (viewerPosition.y / meshWorldSize);

        //looping through all the terrain chunks visible
        for(int yOffset = -chunksVisibleInViewDist; yOffset <= chunksVisibleInViewDist; yOffset++) {
            for (int xOffset = -chunksVisibleInViewDist; xOffset <= chunksVisibleInViewDist; xOffset++) {
                //gets the chunk coordinates of the chunk we're trying to load/make
                Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);

                //check to see if we've already updated the chunk
                if (!alreadyUpdatedChunkCoords.Contains(viewedChunkCoord)) {
                    //check if we've already created the terrainchunk
                    if (terrainChunkDictionary.ContainsKey(viewedChunkCoord)) {
                        //updates the LOD and potentially sets the collider
                        terrainChunkDictionary[viewedChunkCoord].UpdateTerrainChunk();
                    } else {
                        //make a new terrainchunk
                        TerrainChunk newChunk = new TerrainChunk(viewedChunkCoord, heightMapSettings, meshSettings, detailLevels, colliderLODIndex, transform, viewer, mapMaterial);
                        terrainChunkDictionary.Add(viewedChunkCoord, newChunk);
                        newChunk.onVisibilityChanged += OnTerrainChunkVisibilityChanged;
                        newChunk.Load();
                    }
                }
            }
        }
    }

    //the method for adding the terrainChunk to the visibleTerrainChunks List, is subscribed to an event in the terrainChunk class
    void OnTerrainChunkVisibilityChanged(TerrainChunk chunk, bool isVisible) {
        if (isVisible) {
            visibleTerrainChunks.Add(chunk);
        } else {
            visibleTerrainChunks.Remove(chunk);
        }
    }
}

//for holding all the data a level of detail needs
[System.Serializable]
public struct LODInfo {
    //the actual level of detail for use in the MapGenerator class and the maximum distance you can view it at.
    [Range(0, MeshSettings.numSupportedLODs - 1)]
    public int lod;
    public float visibleDistThreshhold;

    //exactly what it sounds like
    public float sqrVisibleDistThreshhold {
        get {
            return visibleDistThreshhold * visibleDistThreshhold;
        }
    }
}
