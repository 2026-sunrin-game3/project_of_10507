using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class SoundManager : MonoBehaviour
{
    public static SoundManager instance;

    public AudioMixer audioMixer;

    [Header("오디오 소스")]
    public AudioSource bgmPlayer;
    public AudioSource sfxPlayer;

    [Header("사운드 클립 설정")]
    public AudioClip[] bgmClips;
    public AudioClip[] sfxClips;

    [Header("SFX 개별 피치 설정")]
    public float[] sfxPitches;


    void Awake()
    {
        if (null == instance)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public static SoundManager Instance
    {
        get
        {
            if (null == instance)
            {
                return null;
            }
            return instance;
        }
    }


    public void ApplyVolume(string mixerParam, string prefsKey, float value)
    {
        // -80f(완전 무음)는 Auto Mixer Suspend 버그를 유발할 수 있어 -60f로 제한
        float dB = value <= 0.0001f ? -60f : Mathf.Log10(value) * 20f;
        audioMixer.SetFloat(mixerParam, dB);
        PlayerPrefs.SetFloat(prefsKey, value);
        PlayerPrefs.Save();
    }

    public void PlayBGM(int index)
    {
        if (index < 0 || index >= bgmClips.Length) return;

        // 같은 곡이 이미 재생 중이면 다시 트는 대신 무시
        if (bgmPlayer.clip == bgmClips[index] && bgmPlayer.isPlaying) return;

        bgmPlayer.clip = bgmClips[index];
        bgmPlayer.loop = true; // BGM은 보통 반복 재생
        bgmPlayer.Play();
    }

    public void StopBGM()
    {
        bgmPlayer.Stop();
        bgmPlayer.clip = null;
    }

    public void PlaySFX(int index)
{
    if (index < 0 || index >= sfxClips.Length) return;

    float pitch = 1f;
    if (sfxPitches != null && index < sfxPitches.Length && sfxPitches[index] > 0f)
    {
        pitch = sfxPitches[index];
    }

    sfxPlayer.pitch = pitch;
    sfxPlayer.PlayOneShot(sfxClips[index]);
}
}