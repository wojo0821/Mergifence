using UnityEngine;

public class AudioManager : MonoBehaviour
{
    //싱글톤 오디오 매니저
    public static AudioManager Instance { get; private set; }
    [SerializeField] private AudioSource SfxSource = null;
    [SerializeField] private AudioSource holdSource = null;
    [SerializeField] private AudioSource BgmSource = null;
    [SerializeField] private AudioClip[] bgmClips = null;
    private int[] bgmOrder;
    private int currentBgmIndex = 0;
    private bool isAppFocused = true;

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
            return;
        }

        bgmOrder = new int[bgmClips.Length];
        for (int i = 0; i < bgmClips.Length; i++)
        {
            bgmOrder[i] = i;
        }
        bgmOrder = FisherYatesShuffle(bgmOrder);
        PlayBgm();
    }
    private void PlayBgm()
    {
        if (currentBgmIndex >= bgmClips.Length)
        {
            bgmOrder = FisherYatesShuffle(bgmOrder);
            currentBgmIndex = 0;
        }
        BgmSource.clip = bgmClips[bgmOrder[currentBgmIndex]];
        BgmSource.Play();
    }

    //사운드 재생 함수
    public void PlaySfx(AudioClip clip)
    {
        SfxSource.PlayOneShot(clip);
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
    private int[] FisherYatesShuffle(int[] data)
    {
        int length = data.Length;
        for (int i = 0; i < length; i++)
        {
            int randomIndex = Random.Range(i, length);
            int temp = data[i];
            data[i] = data[randomIndex];
            data[randomIndex] = temp;
        }
        return data;
    }
    private void PlayNextBgm()
    {
        currentBgmIndex++;
        PlayBgm();
    }
    private void OnApplicationFocus(bool hasFocus)
    {
        isAppFocused = hasFocus;
    }
    private void Update()
    {
        if (!isAppFocused && !Application.runInBackground) return;
        if (!BgmSource.isPlaying)
        {
            PlayNextBgm();
        }
    }
}
