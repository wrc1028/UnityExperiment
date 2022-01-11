using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float speed = 10.0f;
    public float gravity = -9.81f;
    public float jumpHeight = 1.0f;
    public LayerMask ground;
    private CharacterController controller;
    private Vector3 velocity;
    private bool isTouchGround;

    private void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        isTouchGround = Physics.CheckSphere(transform.position + Vector3.down, 0.2f, ground);
        if (isTouchGround && velocity.y < 0)
        {
            velocity.y = 0;
        }
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 move = transform.right * horizontal + transform.forward * vertical;
        controller.Move(move * speed * Time.deltaTime);
        if (Input.GetButtonDown("Jump") && isTouchGround)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    
}
