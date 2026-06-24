using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem; 
using UnityEngine.UI;
using System.Collections.Generic;

public class GlobalCursorSound : MonoBehaviour
{
    [Header("Audio Settings")]
    public AudioClip cursorHitSound;
    [Range(0f, 1f)] public float volume = 1f;

    [Header("Filter Settings")]
    [Tooltip("If true, the sound only plays when clicking UI or objects tagged 'Clickable'. If false, it plays on any valid collider.")]
    public bool requireClickableTag = false;

    private AudioSource audioSource;
    private Camera mainCam;

    void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        
        mainCam = Camera.main;
    }

    void Update()
    {
        // NEW INPUT SYSTEM: Check if mouse exists and if the left button was clicked this frame
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            CheckForClickableTarget();
        }
    }

    private void CheckForClickableTarget()
{
    // 1. Advanced UI Check (Finds the exact UI element under the mouse)
    if (EventSystem.current != null)
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Mouse.current.position.ReadValue();
        
        List<RaycastResult> uiHits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, uiHits);

        if (uiHits.Count > 0)
        {
            // Get the topmost UI object directly under the cursor
            GameObject hitUIObject = uiHits[0].gameObject;

            if (requireClickableTag)
            {
                // Checks if the UI object itself (or its parent, like a Button text child) has the tag
                if (hitUIObject.CompareTag("Clickable") || 
                    (hitUIObject.transform.parent != null && hitUIObject.transform.parent.CompareTag("Clickable")))
                {
                    PlayClickSound();
                }
                // OPTIONAL QUALITY OF LIFE: Un-comment the lines below if you want 
                // ALL standard UI Buttons to automatically click without tagging them manually!
                /*
                else if (hitUIObject.GetComponentInParent<Selectable>() != null)
                {
                    PlayClickSound();
                }
                */
            }
            else
            {
                // If filter is off, play sound for any UI element that blocks raycasts
                PlayClickSound();
            }

            return; // Exit early so UI clicks don't "click through" to world objects behind them
        }
    }

    // 2. Did we click a 2D Object in the world? (Grid Units, Enemies, etc.)
    Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();
    Vector2 mousePos = mainCam.ScreenToWorldPoint(mouseScreenPosition);
    RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

    if (hit.collider != null)
    {
        if (requireClickableTag)
        {
            if (hit.collider.CompareTag("Clickable")) PlayClickSound();
        }
        else
        {
            PlayClickSound();
        }
    }
}

    private void PlayClickSound()
    {
        if (cursorHitSound != null)
        {
            audioSource.PlayOneShot(cursorHitSound, volume);
        }
    }
}