using UnityEngine;
public class MusicController : MonoBehaviour
{
    public static MusicController Instance;
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource battleSource;
    [SerializeField] private AudioSource sfxSource;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void PauseBGM() => bgmSource.Pause();
    public void ResumeBGM() => bgmSource.UnPause();
    
    public void PlayBattleMusic(AudioClip clip)
    {
        battleSource.clip = clip;
        battleSource.loop = true;
        battleSource.Play();
    }

    public void StopBattleMusic() => battleSource.Stop();
    public void PlaySFX(AudioClip clip)
    {
        if (sfxSource != null && clip != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }
}