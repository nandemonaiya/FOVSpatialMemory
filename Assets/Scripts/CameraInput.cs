using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraInput : MonoBehaviour {

    public GameObject target;
    public float rotateSpeed = 5;
    Vector3 offset;

    // Use this for initialization
    void Start () {
        offset = target.transform.position - transform.position;
    }
	
	// Update is called once per frame
	void LateUpdate () {
        float horizontal = Input.GetAxis("Mouse X") * rotateSpeed;
        float vertical = Input.GetAxis("Mouse Y") * rotateSpeed;
        target.transform.Rotate(vertical, horizontal, 0);

        float desiredAngleY = target.transform.eulerAngles.y;
        float desiredAngleX = target.transform.eulerAngles.x;
        Quaternion rotation = Quaternion.Euler(desiredAngleX, desiredAngleY, 0);
        transform.position = target.transform.position - (rotation * offset);

        transform.LookAt(target.transform);
    }
}
