using UnityEngine;

//guess what this does to any object it's attatched to
public class HideOnPlay : MonoBehaviour {
    // Start is called before the first frame update
    void Start() {
        gameObject.SetActive(false);
    }
}
