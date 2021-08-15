using UnityEngine;

public class Move : MonoBehaviour {

    // Update is called once per frame
    void Update() {
        //just a basic class I don't even think I use anymore
        if (Input.GetKey("w")) {
            transform.Rotate(80f * Time.deltaTime, 0f, 0f);
        }

        if (Input.GetKey("s")) {
            transform.Rotate(-80f * Time.deltaTime, 0f, 0f);
        }

        if (Input.GetKey("a")) {
            transform.Rotate(0f, -80f * Time.deltaTime, 0f, Space.World);
        }

        if (Input.GetKey("d")) {
            transform.Rotate(0f, 80f * Time.deltaTime, 0f, Space.World);
        }

        if (Input.GetKey("space")) {
            transform.Translate(Vector3.forward * Time.deltaTime * 80);
        }

    }
}
