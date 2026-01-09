using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;
    
    [Header("Audio Sources")]
    public AudioSource musicSource;
    public AudioSource sfxSource;
    public AudioSource ambientSource;
    public AudioSource uiSource;
    
    [Header("Dynamic Music System")]
    public List<MusicTrack> musicTracks;
    public MusicIntensity currentIntensity = MusicIntensity.Calm;
    
    [Header("Spatial Audio")]
    public bool enable3DSound = true;
    public float maxHearingDistance = 50f;
    
    public enum MusicIntensity
    {
        Calm,           // وقت الاستكشاف
        Tense,          // وجود أعداء قريبين
        Combat,         // أثناء القتال
        Boss,           // قتال الزعيم
        Building,       // أثناء البناء
        Crafting        // أثناء الصناعة
    }
    
    [System.Serializable]
    public class MusicTrack
    {
        public string name;
        public AudioClip clip;
        public MusicIntensity intensity;
        public bool loop = true;
        public float volume = 0.7f;
        public float fadeDuration = 2f;
    }
    
    private Dictionary<string, AudioClip> soundEffects = new Dictionary<string, AudioClip>();
    private MusicTrack currentTrack;
    private Coroutine fadeCoroutine;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudioLibrary();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void InitializeAudioLibrary()
    {
        // تحميل جميع المؤثرات الصوتية
        LoadAllSFX("SFX/Player");
        LoadAllSFX("SFX/UI");
        LoadAllSFX("SFX/Environment");
        LoadAllSFX("SFX/Creatures");
    }
    
    void LoadAllSFX(string path)
    {
        AudioClip[] clips = Resources.LoadAll<AudioClip>(path);
        foreach (var clip in clips)
        {
            soundEffects[clip.name] = clip;
        }
    }
    
    public void PlayDynamicMusic(MusicIntensity intensity)
    {
        if (currentIntensity == intensity && musicSource.isPlaying)
            return;
        
        currentIntensity = intensity;
        
        // اختيار المسار الموسيقي المناسب
        var availableTracks = musicTracks.Where(t => t.intensity == intensity).ToList();
        if (availableTracks.Count == 0) return;
        
        MusicTrack newTrack = availableTracks[Random.Range(0, availableTracks.Count)];
        
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);
        
        fadeCoroutine = StartCoroutine(FadeMusic(currentTrack, newTrack));
    }
    
    System.Collections.IEnumerator FadeMusic(MusicTrack fromTrack, MusicTrack toTrack)
    {
        float elapsed = 0f;
        
        // تهدئة الصوت الحالي
        while (elapsed < (fromTrack?.fadeDuration ?? 1f))
        {
            if (musicSource.isPlaying)
            {
                musicSource.volume = Mathf.Lerp(fromTrack?.volume ?? 0.7f, 0f, elapsed / (fromTrack?.fadeDuration ?? 1f));
            }
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // تغيير المسار
        currentTrack = toTrack;
        musicSource.clip = toTrack.clip;
        musicSource.volume = 0f;
        musicSource.loop = toTrack.loop;
        musicSource.Play();
        
        // رفع الصوت الجديد
        elapsed = 0f;
        while (elapsed < toTrack.fadeDuration)
        {
            musicSource.volume = Mathf.Lerp(0f, toTrack.volume, elapsed / toTrack.fadeDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        musicSource.volume = toTrack.volume;
    }
    
    public void PlaySFX(string soundName, Vector3 position = default, float pitchVariation = 0f)
    {
        if (!soundEffects.ContainsKey(soundName))
        {
            Debug.LogWarning($"Sound effect {soundName} not found!");
            return;
        }
        
        AudioClip clip = soundEffects[soundName];
        
        // إنشاء مصدر صوت مؤقت للأصوات ثلاثية الأبعاد
        if (enable3DSound && position != default)
        {
            GameObject tempGO = new GameObject("TempAudio");
            tempGO.transform.position = position;
            AudioSource tempSource = tempGO.AddComponent<AudioSource>();
            tempSource.clip = clip;
            tempSource.spatialBlend = 1f;
            tempSource.maxDistance = maxHearingDistance;
            tempSource.pitch = Random.Range(1f - pitchVariation, 1f + pitchVariation);
            tempSource.Play();
            Destroy(tempGO, clip.length + 0.1f);
        }
        else
        {
            // استخدام مصدر الصوت العادي
            sfxSource.pitch = Random.Range(1f - pitchVariation, 1f + pitchVariation);
            sfxSource.PlayOneShot(clip);
        }
    }
    
    public void PlayAmbientSound(string ambientName, bool loop = true)
    {
        if (soundEffects.ContainsKey(ambientName))
        {
            ambientSource.clip = soundEffects[ambientName];
            ambientSource.loop = loop;
            ambientSource.Play();
        }
    }
    
    public void StopAmbientSound(float fadeDuration = 1f)
    {
        StartCoroutine(FadeOutAmbient(fadeDuration));
    }
    
    System.Collections.IEnumerator FadeOutAmbient(float duration)
    {
        float startVolume = ambientSource.volume;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            ambientSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        ambientSource.Stop();
        ambientSource.volume = startVolume;
    }
    
    public void AdjustPitchBasedOnSpeed(float gameSpeed)
    {
        // ضبط نغمة الصوت بناءً على سرعة اللعبة
        musicSource.pitch = Mathf.Clamp(gameSpeed, 0.5f, 2f);
        sfxSource.pitch = Mathf.Clamp(gameSpeed, 0.8f, 1.2f);
    }
    
    // نظام النغمات التفاعلية مع الأحداث
    public void PlayEventTone(string eventType)
    {
        switch (eventType)
        {
            case "craft_success":
                PlayToneSequence(new float[] { 523.25f, 659.25f, 783.99f }); // C5, E5, G5
                break;
            case "level_up":
                PlayToneSequence(new float[] { 392.00f, 493.88f, 587.33f, 783.99f }); // G4, B4, D5, G5
                break;
            case "item_found":
                PlayToneSequence(new float[] { 659.25f, 830.61f }); // E5, G#5
                break;
            case "warning":
                PlayToneSequence(new float[] { 349.23f, 329.63f }, 0.3f); // F4, E4
                break;
        }
    }
    
    void PlayToneSequence(float[] frequencies, float duration = 0.2f)
    {
        StartCoroutine(PlayTonesCoroutine(frequencies, duration));
    }
    
    System.Collections.IEnumerator PlayTonesCoroutine(float[] frequencies, float duration)
    {
        foreach (float freq in frequencies)
        {
            PlayTone(freq, duration);
            yield return new WaitForSeconds(duration * 0.8f);
        }
    }
    
    void PlayTone(float frequency, float duration)
    {
        // إنشاء نغمة باستخدام مولد النغمات
        AnimationCurve volumeCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
        StartCoroutine(GenerateTone(frequency, duration, volumeCurve));
    }
    
    System.Collections.IEnumerator GenerateTone(float frequency, float duration, AnimationCurve volumeCurve)
    {
        float sampleRate = 44100;
        float[] samples = new float[Mathf.CeilToInt(sampleRate * duration)];
        
        for (int i = 0; i < samples.Length; i++)
        {
            float time = i / sampleRate;
            float volume = volumeCurve.Evaluate(time / duration);
            samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * time) * volume;
        }
        
        AudioClip clip = AudioClip.Create("Tone", samples.Length, 1, (int)sampleRate, false);
        clip.SetData(samples, 0);
        
        AudioSource.PlayClipAtPoint(clip, Camera.main.transform.position, 0.5f);
        yield return null;
    }
}
