using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour {
	public float speed=6;
	private Vector3 home;
	// Use this for initialization
	void Start () {
		home = transform.position;
	}
	
	// Update is called once per frame
	void Update () {
		Vector3 movement = new Vector3 (Input.GetAxis("Horizontal"),Input.GetAxis("Vertical"),0.0f);
		transform.Translate (movement*speed*Time.deltaTime);
		if(Input.GetKeyDown("space")){
			transform.position = home;
		}
	}
}
