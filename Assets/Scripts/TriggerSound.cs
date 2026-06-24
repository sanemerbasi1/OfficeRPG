using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class TriggerSound : MonoBehaviour
{
    [Header("Audio Settings")]
    [Tooltip("The sound effect to play when the player enters the trigger.")]
    [SerializeField] private AudioClip triggerSound;
    
    [Tooltip("Volume of the sound effect.")]
    [Range(0f, 1f)] 
    [SerializeField] private float volume = 1f;

    [Tooltip("Should this sound only play the first time the player enters?")]
    [SerializeField] private bool playOnlyOnce = true;

    private bool hasPlayed = false;
    private AudioSource audioSource;

    private void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (playOnlyOnce && hasPlayed) return;

            if (triggerSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(triggerSound, volume);
                hasPlayed = true;
            }
            else
            {
                Debug.LogWarning($"[TriggerSound] No AudioClip assigned on {gameObject.name}!");
            }
        }
    }
}