using UnityEngine;

public class AudioManager : MonoBehaviour
{
    //싱글톤 오디오 매니저
    public static AudioManager Instance { get; private set; }
    [SerializeField] private AudioSource audioSource;

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
}
