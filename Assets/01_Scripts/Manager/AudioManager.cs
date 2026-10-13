using UnityEngine;

public class AudioManager : MonoBehaviour
{
    //싱글톤 오디오 매니저
    public static AudioManager Instance { get; private set; }
    [SerializeField] private AudioSource audioSource = null;
    [SerializeField] private AudioSource holdSource = null;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    //사운드 재생 함수
    public void PlaySfx(AudioClip clip)
    {
        audioSource.PlayOneShot(clip);
    }

    public void PlayHoldSound(AudioClip clip)
    {
        holdSource.clip = clip;
        holdSource.Play();
    }

    public void StopHoldSound()
    {
        if (holdSource.isPlaying)
        {
            holdSource.Stop();
        }
    }
}
