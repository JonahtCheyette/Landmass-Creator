using UnityEditor;
using UnityEngine;

//the true makes it so that this editor script works on things that inherit from UpdatableData
[CustomEditor(typeof(UpdatableData), true)]
public class UpdatableDataEditor : Editor {

    public override void OnInspectorGUI() {
        base.OnInspectorGUI();

        UpdatableData data = (UpdatableData)target;

        if (GUILayout.Button("Update")) {
            data.NotifyOfUpdatedValues();
            EditorUtility.SetDirty(target);
        }
    }

}
