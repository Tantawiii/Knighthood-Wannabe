using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class UI_Options : MonoBehaviour
{
    private Player player;
    [SerializeField] private Toggle healthBarToggle;

    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private float mixerMultiplier = 25f;

    [Header("BGM Volume Settings")]
    [SerializeField] private Slider bgmVolumeSlider;
    [SerializeField] private string bgmParameter;

    [Header("SFX Volume Settings")]
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private string sfxParameter;

    private void OnEnable()
    {
        sfxVolumeSlider.value = PlayerPrefs.GetFloat(sfxParameter, .6f);
        bgmVolumeSlider.value = PlayerPrefs.GetFloat(bgmParameter, .6f);
    }

    private void OnDisable()
    {
        PlayerPrefs.SetFloat(sfxParameter, sfxVolumeSlider.value);
        PlayerPrefs.SetFloat(bgmParameter, bgmVolumeSlider.value);
    }

    private void Start()
    {
        player = FindFirstObjectByType<Player>();

        
        healthBarToggle.onValueChanged.AddListener(OnHealthBarToggleChanged);
    }

    public void BGMSliderValue(float value)
    {
        float newValue = Mathf.Log10(value) * mixerMultiplier;
        audioMixer.SetFloat(bgmParameter, newValue);
    }

    public void SFXSliderValue(float value)
    {
        float newValue = Mathf.Log10(value) * mixerMultiplier;
        audioMixer.SetFloat(sfxParameter, newValue);
    }

    private void OnHealthBarToggleChanged(bool isOn)
    {
        player.health.EnableHealthBar(isOn);
    }

    public void MainMenu() => GameManager.Instance.ChangeScene("MainMenu", RespawnType.NoneSpecific);

    public void LoadUpVolume()
    {
        sfxVolumeSlider.value = PlayerPrefs.GetFloat(sfxParameter, .6f);
        bgmVolumeSlider.value = PlayerPrefs.GetFloat(bgmParameter, .6f);
    }
}
