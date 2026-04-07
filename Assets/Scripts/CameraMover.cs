using UnityEngine;
using System.Collections;

public class CameraMover : MonoBehaviour
{
    [Header("Targeting")]
    public Transform playerTransform;
    public Vector3 offset = new Vector3(0, 0, -10);

    [Header("Smoothing")]
    [Range(0, 1)]
    public float smoothSpeed = 0.125f;
    
    private Vector3 velocity = Vector3.zero;

    void Start()
    {
        // If we don't have a player assigned, try to find one by Tag
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;
        }
        
        // Snap immediately to player on start so the camera doesn't "slide" from (0,0)
        if (playerTransform != null)
        {
            transform.position = playerTransform.position + offset;
        }
    }

    void LateUpdate()
    {
        if (playerTransform == null) return;

        // Calculate where we want to be
        Vector3 targetPosition = playerTransform.position + offset;

        // Smoothly move the Boom to the target
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothSpeed);
    }
}