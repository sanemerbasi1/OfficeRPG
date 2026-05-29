using UnityEngine;
using UnityEngine.InputSystem; 

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    // Added a static instance link so EncounterTrigger and BattleManager can find this easily
    public static PlayerController Instance { get; private set; }

    public float moveSpeed = 5f;
    
    [Header("Combat State")]
    public bool isInBattle = false; // Toggled by EncounterTrigger to lock/unlock WASD

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Vector2 movement;

    void Awake()
    {
        // Singleton pattern initialization
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        rb = GetComponent<Rigidbody2D>();
        
        // FIXED: Changed to GetComponentInChildren so the root looks into the 0.4 scaled child object
        sr = GetComponentInChildren<SpriteRenderer>();
        
        rb.gravityScale = 0;
        rb.freezeRotation = true;
    }

    // This is the "New Input System" way to get WASD
    void Update()
    {
        // If we are currently locked in grid combat, force movement to zero and skip input loops
        if (isInBattle)
        {
            movement = Vector2.zero;
            return;
        }

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
        // FIXED: Added a safety net check to prevent MissingComponentException loops if visuals shift
        if (sr == null) return;

        if (movement.x > 0) sr.flipX = false;
        else if (movement.x < 0) sr.flipX = true;
    }

    void FixedUpdate()
    {
        // Safety lock: ensure the physics engine stops trying to move the player via input during battle
        if (isInBattle) return;

        rb.MovePosition(rb.position + movement.normalized * moveSpeed * Time.fixedDeltaTime);
    }

    public void TeleportTo(Vector3 position)
    {
        rb.position = position;
        // Reset movement so they don't slide into the new level
        movement = Vector2.zero; 
    }
}