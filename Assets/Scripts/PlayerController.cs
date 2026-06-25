using UnityEngine;
using UnityEngine.InputSystem; 

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

    public float moveSpeed = 5f;
    
    [Header("Combat State")]
    public bool isInBattle = false; 

    private Rigidbody2D rb;
    private Vector2 movement;
    
    [Header("Animation & Visuals")]
    private Animator animator; 
    private SpriteRenderer sr; 

    void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        rb = GetComponent<Rigidbody2D>();
        
        // Grab both components from the child object
        animator = GetComponentInChildren<Animator>();
        sr = GetComponentInChildren<SpriteRenderer>();
        
        rb.gravityScale = 0;
        rb.freezeRotation = true;
    }

    void Update()
    {
        if (isInBattle)
        {
            movement = Vector2.zero;
            if (animator != null) animator.SetFloat("Speed", 0f);
            return;
        }

        if (Keyboard.current != null)
        {
            Vector2 input = Vector2.zero;
            
            if (Keyboard.current.wKey.isPressed) input.y = 1;
            if (Keyboard.current.sKey.isPressed) input.y = -1;
            if (Keyboard.current.aKey.isPressed) input.x = -1;
            if (Keyboard.current.dKey.isPressed) input.x = 1;

            movement = input;
        }

        // --- NEW: Handle Flipping via Script for Diagonal Support ---
        if (sr != null)
        {
            if (movement.x > 0) sr.flipX = true;
            else if (movement.x < 0) sr.flipX = false;
        }

        if (animator != null)
        {
            if (movement != Vector2.zero)
            {
                animator.SetFloat("MoveX", movement.x);
                animator.SetFloat("MoveY", movement.y);
            }
            
            animator.SetFloat("Speed", movement.sqrMagnitude);
        }
    }

    void FixedUpdate()
    {
        if (isInBattle) return;
        rb.MovePosition(rb.position + movement.normalized * moveSpeed * Time.fixedDeltaTime);
    }

    public void TeleportTo(Vector3 position)
    {
        rb.position = position;
        movement = Vector2.zero; 
        if (animator != null) animator.SetFloat("Speed", 0f); 
    }
}