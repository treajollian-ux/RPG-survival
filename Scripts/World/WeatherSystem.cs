using UnityEngine;
using System.Collections;

public class WeatherSystem : MonoBehaviour
{
    public static WeatherSystem Instance;
    
    [Header("Weather Settings")]
    public WeatherType currentWeather = WeatherType.Clear;
    public float weatherChangeChance = 0.01f;
    public float minWeatherDuration = 60f;
    public float maxWeatherDuration = 300f;
    
    [Header("Rain Settings")]
    public ParticleSystem rainParticles;
    public AudioClip rainSound;
    public float rainIntensity = 1f;
    public Color rainColor = Color.gray;
    
    [Header("Snow Settings")]
    public ParticleSystem snowParticles;
    public AudioClip snowSound;
    public float snowIntensity = 0.5f;
    
    [Header("Fog Settings")]
    public bool enableFog = true;
    public float fogDensity = 0.01f;
    public Color fogColor = Color.gray;
    
    [Header("Wind Settings")]
    public float windStrength = 1f;
    public float windDirection = 0f; // 0-360 degrees
    public AudioClip windSound;
    
    [Header("Lightning")]
    public Light lightningLight;
    public AudioClip thunderSound;
    public float lightningChance = 0.1f;
    public float minLightningDelay = 5f;
    public float maxLightningDelay = 30f;
    
    public enum WeatherType
    {
        Clear,
        Rainy,
        Stormy,
        Snowy,
        Foggy,
        Windy
    }
    
    private float weatherTimer = 0f;
    private float currentWeatherDuration = 0f;
    private Coroutine lightningCoroutine;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        InitializeWeather();
        StartRandomWeather();
    }
    
    void Update()
    {
        UpdateWeatherTimer();
        UpdateWeatherEffects();
        
        // فرصة عشوائية لتغيير الطقس
        if (Random.Range(0f, 1f) < weatherChangeChance * Time.deltaTime)
        {
            ChangeWeatherRandomly();
        }
    }
    
    void InitializeWeather()
    {
        // إيقاف جميع تأثيرات الطقس
        if (rainParticles != null) rainParticles.Stop();
        if (snowParticles != null) snowParticles.Stop();
        if (lightningLight != null) lightningLight.enabled = false;
        
        // إعداد الضباب
        if (enableFog)
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = 0f;
            RenderSettings.fogColor = fogColor;
        }
    }
    
    void StartRandomWeather()
    {
        ChangeWeather((WeatherType)Random.Range(0, System.Enum.GetValues(typeof(WeatherType)).Length));
    }
    
    void UpdateWeatherTimer()
    {
        weatherTimer += Time.deltaTime;
        
        if (weatherTimer >= currentWeatherDuration)
        {
            ChangeWeatherRandomly();
        }
    }
    
    void UpdateWeatherEffects()
    {
        switch (currentWeather)
        {
            case WeatherType.Rainy:
                UpdateRainEffects();
                break;
                
            case WeatherType.Stormy:
                UpdateStormEffects();
                break;
                
            case WeatherType.Snowy:
                UpdateSnowEffects();
                break;
                
            case WeatherType.Foggy:
                UpdateFogEffects();
                break;
                
            case WeatherType.Windy:
                UpdateWindEffects();
                break;
        }
    }
    
    void UpdateRainEffects()
    {
        if (rainParticles != null && !rainParticles.isPlaying)
        {
            rainParticles.Play();
            
            var main = rainParticles.main;
            main.startColor = rainColor;
            
            var emission = rainParticles.emission;
            emission.rateOverTime = 100f * rainIntensity;
        }
        
        // تشغيل صوت المطر
        if (!AudioManager.Instance.ambientSource.isPlaying || AudioManager.Instance.ambientSource.clip != rainSound)
        {
            AudioManager.Instance.PlayAmbientSound("rain", true);
        }
    }
    
    void UpdateStormEffects()
    {
        UpdateRainEffects();
        
        // زيادة شدة المطر
        if (rainParticles != null)
        {
            var emission = rainParticles.emission;
            emission.rateOverTime = 300f * rainIntensity;
        }
        
        // تشغيل البرق
        if (lightningCoroutine == null)
        {
            lightningCoroutine = StartCoroutine(LightningRoutine());
        }
        
        // رياح أقوى
        windStrength = 3f;
    }
    
    void UpdateSnowEffects()
    {
        if (snowParticles != null && !snowParticles.isPlaying)
        {
            snowParticles.Play();
            
            var emission = snowParticles.emission;
            emission.rateOverTime = 50f * snowIntensity;
        }
        
        // تشغيل صوت الثلج
        if (!AudioManager.Instance.ambientSource.isPlaying || AudioManager.Instance.ambientSource.clip != snowSound)
        {
            AudioManager.Instance.PlayAmbientSound("snow", true);
        }
    }
    
    void UpdateFogEffects()
    {
        if (enableFog)
        {
            RenderSettings.fogDensity = Mathf.Lerp(RenderSettings.fogDensity, fogDensity, Time.deltaTime * 0.5f);
        }
    }
    
    void UpdateWindEffects()
    {
        // تشغيل صوت الرياح
        if (!AudioManager.Instance.ambientSource.isPlaying || AudioManager.Instance.ambientSource.clip != windSound)
        {
            AudioManager.Instance.PlayAmbientSound("wind", true);
        }
    }
    
    IEnumerator LightningRoutine()
    {
        while (currentWeather == WeatherType.Stormy)
        {
            yield return new WaitForSeconds(Random.Range(minLightningDelay, maxLightningDelay));
            
            if (Random.Range(0f, 1f) < lightningChance)
            {
                // فلاش البرق
                if (lightningLight != null)
                {
                    lightningLight.enabled = true;
                    lightningLight.intensity = Random.Range(3f, 8f);
                    
                    yield return new WaitForSeconds(0.1f);
                    
                    lightningLight.intensity = Random.Range(1f, 3f);
                    
                    yield return new WaitForSeconds(0.05f);
                    
                    lightningLight.enabled = false;
                }
                
                // صوت الرعد
                if (thunderSound != null)
                {
                    AudioManager.Instance.PlaySFX(thunderSound.name, transform.position);
                    
                    // اهتزاز الكاميرا
                    CameraShake.Instance?.Shake(0.5f, 0.1f);
                }
            }
        }
        
        lightningCoroutine = null;
    }
    
    public void ChangeWeather(WeatherType newWeather)
    {
        // تنظيف الطقس الحالي
        CleanupCurrentWeather();
        
        // تعيين الطقس الجديد
        currentWeather = newWeather;
        weatherTimer = 0f;
        currentWeatherDuration = Random.Range(minWeatherDuration, maxWeatherDuration);
        
        Debug.Log($"تغير الطقس إلى: {currentWeather}");
        
        // إشعار التغيير
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowNotification($"الطقس: {GetWeatherName(newWeather)}", GetWeatherColor(newWeather));
        }
        
        // نغمة تغيير الطقس
        AudioManager.Instance.PlayEventTone("item_found");
    }
    
    void CleanupCurrentWeather()
    {
        // إيقاف جميع الجسيمات
        if (rainParticles != null) rainParticles.Stop();
        if (snowParticles != null) snowParticles.Stop();
        
        // إيقاف البرق
        if (lightningCoroutine != null)
        {
            StopCoroutine(lightningCoroutine);
            lightningCoroutine = null;
        }
        
        if (lightningLight != null)
        {
            lightningLight.enabled = false;
        }
        
        // إيقاف صوت الطقس
        AudioManager.Instance.StopAmbientSound(1f);
        
        // إعادة تعيين الضباب
        if (enableFog)
        {
            RenderSettings.fogDensity = 0f;
        }
        
        // إعادة تعيين الرياح
        windStrength = 1f;
    }
    
    void ChangeWeatherRandomly()
    {
        WeatherType newWeather;
        
        do
        {
            newWeather = (WeatherType)Random.Range(0, System.Enum.GetValues(typeof(WeatherType)).Length);
        } while (newWeather == currentWeather);
        
        ChangeWeather(newWeather);
    }
    
    string GetWeatherName(WeatherType weather)
    {
        switch (weather)
        {
            case WeatherType.Clear: return "صافي";
            case WeatherType.Rainy: return "ممطر";
            case WeatherType.Stormy: return "عاصف";
            case WeatherType.Snowy: return "ثلجي";
            case WeatherType.Foggy: return "ضبابي";
            case WeatherType.Windy: return "عاصف بالرياح";
            default: return "غير معروف";
        }
    }
    
    Color GetWeatherColor(WeatherType weather)
    {
        switch (weather)
        {
            case WeatherType.Clear: return Color.cyan;
            case WeatherType.Rainy: return Color.blue;
            case WeatherType.Stormy: return Color.red;
            case WeatherType.Snowy: return Color.white;
            case WeatherType.Foggy: return Color.gray;
            case WeatherType.Windy: return Color.yellow;
            default: return Color.white;
        }
    }
    
    public float GetTemperatureEffect()
    {
        switch (currentWeather)
        {
            case WeatherType.Snowy: return -10f;
            case WeatherType.Rainy: return -5f;
            case WeatherType.Stormy: return -8f;
            case WeatherType.Clear: return 5f;
            default: return 0f;
        }
    }
    
    public void ForceWeather(WeatherType weather, float duration = 0f)
    {
        ChangeWeather(weather);
        
        if (duration > 0f)
        {
            currentWeatherDuration = duration;
        }
    }
    
    public bool IsRaining()
    {
        return currentWeather == WeatherType.Rainy || currentWeather == WeatherType.Stormy;
    }
    
    public bool IsSnowing()
    {
        return currentWeather == WeatherType.Snowy;
    }
    
    public bool IsStormy()
    {
        return currentWeather == WeatherType.Stormy;
    }
}
