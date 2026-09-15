using System.Collections;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;
    [SerializeField] private AudioDatabase_DataSO audioDatabase;
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;
    [Space]
    [SerializeField] private bool bgmShouldPlay;

    private AudioClip lastMusicPlayed;
    private Transform player;
    private string currentMusicGroup;
    private Coroutine currentMusicCoroutine;

    private void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if(!bgmSource.isPlaying && bgmShouldPlay)
        {
            if(!string.IsNullOrEmpty(currentMusicGroup))
            {
                SwitchMusic(currentMusicGroup);
            }
        }

        if(bgmSource.isPlaying && !bgmShouldPlay)
        {
            StopBGM();
        }
    }

    public void StartBGM(string musicGroup)
    {
        bgmShouldPlay = true;
        if(musicGroup == currentMusicGroup)
        {
            return;
        }

        SwitchMusic(musicGroup);
    }

    public void SwitchMusic(string musicGroup)
    {
        bgmShouldPlay = true;
        currentMusicGroup = musicGroup;

        if(currentMusicCoroutine != null)
        {
            StopCoroutine(currentMusicCoroutine);
        }

        currentMusicCoroutine = StartCoroutine(SwitchMusicCo(musicGroup));
    }

    public void StopBGM()
    {
        bgmShouldPlay = false;

        StartCoroutine(FadeVolumeCo(bgmSource, 0f, 1f));

        if(currentMusicCoroutine != null)
        {
            StopCoroutine(currentMusicCoroutine);
        }
    }

    private IEnumerator SwitchMusicCo(string musicGroup)
    {
        AudioClipData newMusicData = audioDatabase?.GetAudioClipData(musicGroup);

        AudioClip newMusicClip = newMusicData?.GetRandomClip();

        if(newMusicClip == null || newMusicData.clips.Count == 0)
        {
            Debug.LogWarning($"AudioManager: No audio clip found for music group '{musicGroup}'");
            yield break;
        }

        if(newMusicData.clips.Count > 1)
        {
            while(newMusicClip == lastMusicPlayed)
            {
                newMusicClip = newMusicData?.GetRandomClip();
            }
        }

        if(bgmSource.isPlaying)
        {
            yield return FadeVolumeCo(bgmSource, 0f, 1f);
        }

        lastMusicPlayed = newMusicClip;
        bgmSource.clip = newMusicClip;
        bgmSource.volume = 0;
        bgmSource.Play();

        StartCoroutine(FadeVolumeCo(bgmSource, newMusicData.volume, 1f));
    }

    private IEnumerator FadeVolumeCo(AudioSource source, float targetVolume, float duration)
    {
        float elapsedTime = 0f;
        float startVolume = source.volume;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            source.volume = Mathf.Lerp(startVolume, targetVolume, elapsedTime / duration);
            yield return null;
        }

        source.volume = targetVolume;
    }

    public void PlaySFX(string soundName, AudioSource sfxSource, float minDistanceToHearSound = 5.0f)
    {
        if(player == null)
            player = Player.Instance.transform;

        var clipData = audioDatabase.GetAudioClipData(soundName);
        if (clipData == null)
        {
            Debug.LogWarning($"AudioManager: No audio clip data found for sound name '{soundName}'");
            return;
        }

        var clip = clipData.GetRandomClip();
        if (clip == null)
        {
            Debug.LogWarning($"AudioManager: No audio clips found in AudioClipData for sound name '{soundName}'");
            return;
        }

        float maxVolume = clipData.volume;
        float distance = Vector2.Distance(sfxSource.transform.position, player.position);
        float t = Mathf.Clamp01(1 - (distance / minDistanceToHearSound));

        sfxSource.pitch = Random.Range(0.95f, 1.1f); // Slightly randomize pitch for variation
        sfxSource.volume = Mathf.Lerp(0, maxVolume, t * t); // Adjust volume based on distance, with exponential falloff for smoother transition
        sfxSource.PlayOneShot(clip);
    }

    public void PlayGlobalSFX(string soundName)
    {
        var audioData = audioDatabase.GetAudioClipData(soundName);

        if(audioData == null)
        {
            Debug.LogWarning($"AudioManager: No audio clip data found for sound name '{soundName}'");
            return;
        }

        var clip = audioData.GetRandomClip();
        if(clip == null)
        {
            Debug.LogWarning($"AudioManager: No audio clips found in AudioClipData for sound name '{soundName}'");
            return;
        }

        Debug.Log($"Playing global SFX: {soundName}");

        sfxSource.pitch = Random.Range(0.95f, 1.1f); // Slightly randomize pitch for variation
        sfxSource.volume = audioData.volume; // Use the volume from the AudioClipData
        sfxSource.PlayOneShot(clip);    
    }
}
