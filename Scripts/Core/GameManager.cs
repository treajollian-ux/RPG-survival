using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    
    [Header("Game Settings")]
    public float gameSpeed = 1f;
    public bool isPaused = false;
    
    [Header("Audio Settings")]
    public AudioSettings audioSettings;
    
    [Header("Dynamic Background System")]
    public List<BackgroundLayer> backgroundLayers;
    public float parallaxSpeed = 0.5f;
    
    [System.Serializable]
    public class AudioSettings
    {
        public float masterVolume = 1f;
        public float musicVolume = 0.8f;
        public float sfxVolume = 0.9f;
        public bool backgroundMusicEnabled = true;
        public bool ambientSoundsEnabled = true;
    }
    
    [System.Serializable]
    public class BackgroundLayer
    {
        public Transform layerTransform;
        public float parallaxMultiplier;
        public bool hasAnimation;
        public AnimationClip layerAnimation;
    }
    
    void Awake()
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
        
        InitializeAudioSystem();
        InitializeBackgroundSystem();
    }
    
    void InitializeAudioSystem()
    {
        // نظام الصوت الديناميكي مع الموسيقى التكيفية
        AudioManager.Instance.SetDynamicMusic(true);
        AudioManager.Instance.AddAmbientSounds("garden", new string[] {
            "birds_chirping", "water_stream", "leaves_rustling"
        });
    }
    
    void InitializeBackgroundSystem()
    {
        // تهيئة نظام الخلفيات الديناميكي
        StartCoroutine(AnimateBackgroundLayers());
    }
    
    System.Collections.IEnumerator AnimateBackgroundLayers()
    {
        while (true)
        {
            foreach (var layer in backgroundLayers)
            {
                if (layer.hasAnimation)
                {
                    // حركة خلفية بارالاكس مع حركات عشوائية
                    float randomOffset = Random.Range(-0.1f, 0.1f);
                    layer.layerTransform.Translate(
                        Vector3.left * (parallaxSpeed * layer.parallaxMultiplier + randomOffset) * Time.deltaTime
                    );
                }
            }
            yield return null;
        }
    }
    
    public void UpdateGameSpeed(float newSpeed)
    {
        gameSpeed = Mathf.Clamp(newSpeed, 0.1f, 3f);
        Time.timeScale = gameSpeed;
        
        // ضبط سرعة الصوت بناءً على سرعة اللعبة
        AudioManager.Instance.AdjustPitchBasedOnSpeed(gameSpeed);
    }
    
    public void TogglePause()
    {
        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : gameSpeed;
        
        // تغيير الموسيقى عند الإيقاف المؤقت
        if (isPaused)
        {
            AudioManager.Instance.PlayPauseMusic();
        }
        else
        {
            AudioManager.Instance.ResumeNormalMusic();
        }
    }
}
