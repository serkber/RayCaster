using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField]
    private float acceleration = 100f;
    
    [SerializeField]
    private float deceleration = 100f;
    
    [SerializeField]
    private float maxSpeed = 10f;
    
    [SerializeField]
    private float mouseSensitivity = 30f;
    
    private Rigidbody2D rb;

    private Vector2 lastMousePos;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        lastMousePos = Input.mousePosition;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Confined;
    }

    private void Update()
    {
        Movement();
        Look();
    }

    private void Movement()
    {

        bool canAccelerate = rb.velocity.magnitude < maxSpeed;
        Vector2 speed = Vector2.zero;
        float modifier = Time.deltaTime * acceleration;
        bool moving = false;
        
        if (Input.GetKey(KeyCode.W)) {
            speed += (Vector2)transform.up;
            moving = true;
        }

        if (Input.GetKey(KeyCode.A)) {
            speed += -(Vector2) transform.right;
            moving = true;
        }
        
        if (Input.GetKey(KeyCode.D)) {
            speed += (Vector2) transform.right;
            moving = true;
        }
        
        if (Input.GetKey(KeyCode.S)) {
            speed += -(Vector2) transform.up;
            moving = true;
        }

        if (canAccelerate) {
            rb.AddForce(speed * modifier);
        }

        if (!moving) {
            rb.AddForce(-rb.velocity * Time.deltaTime * deceleration);
        }
    }

    private void Look()
    {
        Vector2 mouseDelta = (Vector2)Input.mousePosition - lastMousePos;
        mouseDelta *= Time.deltaTime * mouseSensitivity;
        
        transform.Rotate(-transform.forward, mouseDelta.x);

        lastMousePos = Input.mousePosition;
    }
}
