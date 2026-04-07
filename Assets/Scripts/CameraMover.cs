using UnityEngine;

public class CameraMover : MonoBehaviour
{
    [Header("Targeting")]
    public Transform playerTransform;
    public Vector3 offset = new Vector3(0, 0, -10);

    [Header("Smoothing")]
    [Range(0, 1)] public float smoothSpeed = 0.125f;
    private Vector3 velocity = Vector3.zero;

    void LateUpdate()
    {
        // 1. If we don't have a player, look for one every frame
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) 
            {
                playerTransform = player.transform;
                // Snap immediately to the player's position so the camera doesn't "slide" from (0,0)
                transform.position = playerTransform.position + offset;
            }
            return; // Exit this frame and wait for the next one
        }

        // 2. If we HAVE a player, follow them smoothly
        Vector3 targetPosition = playerTransform.position + offset;
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothSpeed);
    }
}