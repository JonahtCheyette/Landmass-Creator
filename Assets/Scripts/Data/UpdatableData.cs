using UnityEngine;

//extending from ScriptableObject means unity will be able to make objects of this type (in this case, the NoiseData type)
public class UpdatableData : ScriptableObject {
    //The event keyword means that other classes cannot overwrite the othe methods subscribed to this delegate or call this delegate
    //if you're still confused, watch https://www.youtube.com/watch?v=TdiN18PR4zk
    //if you can't remember what delegates are, look it up or watch the first episode in that series
    public event System.Action OnValuesUpdated;
    public bool autoUpdate;

    //what these lines do is to tell unity to not compile these lines in a standalone build
    #if UNITY_EDITOR
    //is called whenever the values are changed in the editor
    protected virtual void OnValidate() {
        if (autoUpdate) {
            //the EditorApplication.update is every frame, and after recompile, so subscribing the method here and unsubscribing it when it gets called meanss the method gets called once, on the first frame after OnValidate is called or the recompile happens
            //we do it this way because the update for terrain on recompile needs to happen after the recompile of the shader, but shaders recompile after c# scripts.
            UnityEditor.EditorApplication.update += NotifyOfUpdatedValues;
        }
    }

    //call the action
    public void NotifyOfUpdatedValues() {
        UnityEditor.EditorApplication.update -= NotifyOfUpdatedValues;
        if (OnValuesUpdated != null) {
            OnValuesUpdated();
        }
    }
    #endif
}
