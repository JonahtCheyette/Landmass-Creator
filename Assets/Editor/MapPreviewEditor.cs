using UnityEngine;
using UnityEditor;

//says to use this editor for the MapGenerator Script
[CustomEditor (typeof (MapPreview))]
public class MapPreviewEditor : Editor {
    
    public override void OnInspectorGUI() {
        //gets the mapPreview reference
        MapPreview mapPreview = (MapPreview) target;

        //checks if the values have been updated
        if (DrawDefaultInspector()) {
            //updates the map
            if (mapPreview.autoUpdate) {
                mapPreview.DrawMapInEditor();
            }
        }

        //draws the map
        if(GUILayout.Button ("Generate")) {
            mapPreview.DrawMapInEditor();
        }
    }
}
