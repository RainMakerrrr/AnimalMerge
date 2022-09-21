using UnityEngine;
using System.Collections;

public class RotatorZ : MonoBehaviour {

	public float Speed = 2.0f;

	// Update is called once per frame.
	void Update () {
		// Rotates object around the Z axis.
		transform.Rotate(0, 0, Speed * Time.deltaTime);
	}
}