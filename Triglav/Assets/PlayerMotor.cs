using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMotor : MonoBehaviour {

    private CharacterController controller;
    private float verticalVelocity;

	// Use this for initialization
	void Start () {
        controller = GetComponent<CharacterController>();
	}
	
	// Update is called once per frame
	void Update () {

        Vector3 inputs = Vector3.zero;

        inputs.x = Input.GetAxis("Horizontal");

        if (controller.isGrounded)
        {
            verticalVelocity = -1;
            if (Input.GetButton("Jump"))
            {
                verticalVelocity = 10;
            }

        }
        else
        {
            verticalVelocity -= 14.0f * Time.deltaTime;
        }
        inputs.y = verticalVelocity;

        controller.Move(inputs * Time.deltaTime);
	}
}
