using UnityEngine;
using UnityEngine.InputSystem; // You need this namespace!

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Vector2 movement;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        rb.gravityScale = 0;
        rb.freezeRotation = true;
    }

    // This is the "New Input System" way to get WASD
    void Update()
    {
        // Keyboard.current checks if a keyboard is plugged in
        if (Keyboard.current != null)
        {
            Vector2 input = Vector2.zero;
            
            if (Keyboard.current.wKey.isPressed) input.y = 1;
            if (Keyboard.current.sKey.isPressed) input.y = -1;
            if (Keyboard.current.aKey.isPressed) input.x = -1;
            if (Keyboard.current.dKey.isPressed) input.x = 1;

            movement = input;
        }

        HandleSpriteMirroring();
    }

    void HandleSpriteMirroring()
    {
        if (movement.x > 0) sr.flipX = false;
        else if (movement.x < 0) sr.flipX = true;
    }

    void FixedUpdate()
    {
        rb.MovePosition(rb.position + movement.normalized * moveSpeed * Time.fixedDeltaTime);
    }
}