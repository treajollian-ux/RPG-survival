using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public class DayNightCycle : MonoBehaviour
{
    public static DayNightCycle Instance;
    
    [Header("Time Settings")]
    public float dayDuration = 300f; // 5 دقائق حقيقية
    public float currentTime = 0.25f; // بداية الصباح
    public bool isPaused = false;
    
    [Header("Sun & Moon")]
    public Light sunLight;
    public Light moonLight;
    public float sunIntensity = 1f;
    public float moonIntensity = 0.3f;
    public Gradient sunColorGradient;
    public Gradient moonColorGradient;
    
    [Header("Skybox")]
    public Material skyboxMaterial;
    public Gradient skyColorGradient;
    public Gradient horizonColorGradient;
    public AnimationCurve starsIntensityCurve;
    
    [Header("Post Processing")]
    public PostProcessVolume postProcessVolume;
    private ColorGrading colorGrading;
    private Vignette vignette;
    
    [Header("Environment Effects")]
    public AudioClip dayAmbient;
    public AudioClip nightAmbient;
    public float ambientTransitionTime = 2f;
    
    [Header("Events")]
    public float dawnTime = 0.2f; // الفجر
    public float dayTime = 0.25f; // النهار
    public float duskTime = 0.7f; // الغسق
    public float nightTime = 0.75f; // الليل
    
    // أحداث الوقت
    public delegate void TimeEvent();
    public event TimeEvent OnDawn;
    public event TimeEvent OnDay;
    public event TimeEvent OnDusk;
    public event TimeEvent OnNight;
    
    private bool wasDay = true;
    private TimeOfDay currentTimeOfDay = TimeOfDay.Day;
    private float ambientTransitionTimer = 0f;
    
    public enum TimeOfDay
    {
        Dawn,
        Day,
        Dusk,
        Night
    }
    
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
        
        // الحصول على مكونات Post Processing
        if (postProcessVolume != null)
        {
            postProcessVolume.profile.TryGetSettings(out colorGrading);
            postProcessVolume.profile.TryGetSettings(out vignette);
        }
    }
    
    void Start()
    {
        UpdateTimeOfDay();
        UpdateLighting();
        
        // بدء الموسيقى المناسبة
        StartAmbientAudio();
    }
    
    void Update()
    {
        if (!isPaused)
        {
            UpdateTime();
        }
        
        UpdateLighting();
        UpdateTimeOfDay();
        UpdateAmbientAudio();
        UpdatePostProcessing();
    }
    
    void UpdateTime()
    {
        currentTime += Time.deltaTime / dayDuration;
        
        if (currentTime >= 1f)
        {
            currentTime = 0f;
            OnNewDay();
        }
    }
    
    void UpdateTimeOfDay()
    {
        TimeOfDay newTimeOfDay = GetTimeOfDay(currentTime);
        
        if (newTimeOfDay != currentTimeOfDay)
        {
            currentTimeOfDay = newTimeOfDay;
            
            // تشغيل الأحداث
            switch (currentTimeOfDay)
            {
                case TimeOfDay.Dawn:
                    OnDawn?.Invoke();
                    break;
                case TimeOfDay.Day:
                    OnDay?.Invoke();
                    break;
                case TimeOfDay.Dusk:
                    OnDusk?.Invoke();
                    break;
                case TimeOfDay.Night:
                    OnNight?.Invoke();
                    break;
            }
            
            Debug.Log($"الوقت الحالي: {currentTimeOfDay}");
        }
    }
    
    TimeOfDay GetTimeOfDay(float time)
    {
        if (time >= dawnTime && time < dayTime) return TimeOfDay.Dawn;
        if (time >= dayTime && time < duskTime) return TimeOfDay.Day;
        if (time >= duskTime && time < nightTime) return TimeOfDay.Dusk;
        return TimeOfDay.Night;
    }
    
    void UpdateLighting()
    {
        // حساب زوايا الشمس والقمر
        float sunAngle = currentTime * 360f;
        float moonAngle = (currentTime + 0.5f) * 360f;
        
        // تحديث مواقع الشمس والقمر
        if (sunLight != null)
        {
            sunLight.transform.rotation = Quaternion.Euler(sunAngle, -30f, 0f);
            
            // حساب شدة الشمس بناءً على الارتفاع
            float sunHeight = Mathf.Sin(sunAngle * Mathf.Deg2Rad);
            sunLight.intensity = Mathf.Clamp01(sunHeight) * sunIntensity;
            
            // تحديث لون الشمس
            sunLight.color = sunColorGradient.Evaluate(currentTime);
            
            // إطفاء الشمس ليلاً
            sunLight.enabled = sunLight.intensity > 0.01f;
        }
        
        if (moonLight != null)
        {
            moonLight.transform.rotation = Quaternion.Euler(moonAngle, -30f, 0f);
            
            // حساب شدة القمر
            float moonHeight = Mathf.Sin(moonAngle * Mathf.Deg2Rad);
            moonLight.intensity = Mathf.Clamp01(moonHeight) * moonIntensity;
            
            // تحديث لون القمر
            moonLight.color = moonColorGradient.Evaluate(currentTime);
            
            // إضاءة القمر ليلاً فقط
            moonLight.enabled = moonLight.intensity > 0.01f && currentTime > nightTime || currentTime < dawnTime;
        }
        
        // تحديث السكاي بوكس
        UpdateSkybox();
        
        // تحديث الضوء المحيط
        UpdateAmbientLight();
    }
    
    void UpdateSkybox()
    {
        if (skyboxMaterial != null)
        {
            // تحديث ألوان السكاي بوكس
            skyboxMaterial.SetColor("_SkyColor", skyColorGradient.Evaluate(currentTime));
            skyboxMaterial.SetColor("_HorizonColor", horizonColorGradient.Evaluate(currentTime));
            
            // تحديث النجوم ليلاً
            float starsIntensity = starsIntensityCurve.Evaluate(currentTime);
            skyboxMaterial.SetFloat("_StarsIntensity", starsIntensity);
            
            // تحدير سطوع السكاي بوكس
            float skyboxExposure = Mathf.Lerp(0.5f, 1.2f, 
                Mathf.SmoothStep(0f, 1f, Mathf.Abs(Mathf.Cos(currentTime * Mathf.PI * 2f)))
            );
            skyboxMaterial.SetFloat("_Exposure", skyboxExposure);
        }
    }
    
    void UpdateAmbientLight()
    {
        // تحديث الضوء المحيط
        Color ambientColor = skyColorGradient.Evaluate(currentTime);
        RenderSettings.ambientSkyColor = ambientColor;
        
        // تحديث الضوء المحيط المتجه
        float ambientIntensity = Mathf.Lerp(0.3f, 1f, 
            Mathf.SmoothStep(0f, 1f, 
                Mathf.Abs(Mathf.Cos(currentTime * Mathf.PI * 2f))
            )
        );
        RenderSettings.ambientIntensity = ambientIntensity;
    }
    
    void UpdatePostProcessing()
    {
        if (colorGrading != null)
        {
            // تعديل درجة الحرارة والصبغة حسب الوقت
            float temperature = Mathf.Lerp(-10f, 10f, 
                Mathf.SmoothStep(0f, 1f, Mathf.Abs(Mathf.Sin(currentTime * Mathf.PI)))
            );
            colorGrading.temperature.value = temperature;
            
            // تعديل التباين
            float contrast = Mathf.Lerp(-10f, 10f, currentTime);
            colorGrading.contrast.value = contrast;
        }
        
        if (vignette != null)
        {
            // زيادة الفيجنيت ليلاً
            float vignetteIntensity = Mathf.Lerp(0.1f, 0.3f, 
                Mathf.SmoothStep(0f, 1f, Mathf.Abs(Mathf.Sin(currentTime * Mathf.PI * 2f + Mathf.PI * 0.5f)))
            );
            vignette.intensity.value = vignetteIntensity;
        }
    }
    
    void StartAmbientAudio()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayAmbientSound(
                wasDay ? "day_ambient" : "night_ambient",
                true
            );
        }
    }
    
    void UpdateAmbientAudio()
    {
        bool isDay = currentTime >= dayTime && currentTime < duskTime;
        
        if (isDay != wasDay)
        {
            ambientTransitionTimer = ambientTransitionTime;
            wasDay = isDay;
        }
        
        if (ambientTransitionTimer > 0f)
        {
            ambientTransitionTimer -= Time.deltaTime;
            
            if (AudioManager.Instance != null)
            {
                // الانتقال بين الأصوات
                float transitionProgress = 1f - (ambientTransitionTimer / ambientTransitionTime);
                
                if (isDay && ambientTransitionTimer <= 0f)
                {
                    AudioManager.Instance.PlayAmbientSound("day_ambient", true);
                }
                else if (!isDay && ambientTransitionTimer <= 0f)
                {
                    AudioManager.Instance.PlayAmbientSound("night_ambient", true);
                }
            }
        }
    }
    
    void OnNewDay()
    {
        Debug.Log("بدأ يوم جديد!");
        
        // تحديث النباتات والموارد
        RegenerateResources();
        
        // تحديث المهام اليومية
        UpdateDailyQuests();
        
        // تأثير بصرية لبدء يوم جديد
        SpawnDawnEffect();
        
        // نغمة بدء اليوم
        AudioManager.Instance.PlayEventTone("level_up");
    }
    
    void RegenerateResources()
    {
        // إعادة توليد بعض الموارد
        ResourceNode[] resources = FindObjectsOfType<ResourceNode>();
        
        foreach (ResourceNode resource in resources)
        {
            if (resource.IsDepleted() && Random.value < 0.3f) // 30% فرصة لإعادة النمو
            {
                // هنا يمكنك تفعيل إعادة النمو
                // resource.Respawn();
            }
        }
    }
    
    void UpdateDailyQuests()
    {
        if (QuestSystem.Instance != null)
        {
            // تحديث المهام اليومية
            // QuestSystem.Instance.ResetDailyQuests();
        }
    }
    
    void SpawnDawnEffect()
    {
        // تأثير بصري لشروق الشمس
        GameObject dawnEffectPrefab = Resources.Load<GameObject>("Effects/DawnEffect");
        if (dawnEffectPrefab != null)
        {
            Instantiate(dawnEffectPrefab, Vector3.zero, Quaternion.identity);
        }
    }
    
    public void SetTime(float time)
    {
        currentTime = Mathf.Clamp01(time);
        UpdateTimeOfDay();
        UpdateLighting();
    }
    
    public void SetTimeOfDay(TimeOfDay timeOfDay)
    {
        switch (timeOfDay)
        {
            case TimeOfDay.Dawn:
                SetTime(dawnTime + 0.01f);
                break;
            case TimeOfDay.Day:
                SetTime(dayTime + 0.01f);
                break;
            case TimeOfDay.Dusk:
                SetTime(duskTime + 0.01f);
                break;
            case TimeOfDay.Night:
                SetTime(nightTime + 0.01f);
                break;
        }
    }
    
    public void PauseTime()
    {
        isPaused = true;
    }
    
    public void ResumeTime()
    {
        isPaused = false;
    }
    
    public void SetDayDuration(float duration)
    {
        dayDuration = Mathf.Max(10f, duration);
    }
    
    public float GetTimeOfDayProgress()
    {
        return currentTime;
    }
    
    public string GetTimeString()
    {
        float totalMinutes = currentTime * 24f * 60f;
        int hours = Mathf.FloorToInt(totalMinutes / 60f) % 24;
        int minutes = Mathf.FloorToInt(totalMinutes % 60f);
        
        return $"{hours:00}:{minutes:00}";
    }
    
    public bool IsNight()
    {
        return currentTime >= nightTime || currentTime < dawnTime;
    }
    
    public bool IsDay()
    {
        return currentTime >= dayTime && currentTime < duskTime;
    }
    
    public float GetTemperature()
    {
        // حساب درجة الحرارة بناءً على الوقت
        float baseTemp = 20f; // 20 درجة مئوية
        float dayNightVariation = Mathf.Sin(currentTime * Mathf.PI * 2f) * 10f; // ±10 درجة
        
        return baseTemp + dayNightVariation;
    }
    
    public float GetVisibility()
    {
        // مدى الرؤية بناءً على الوقت
        if (IsDay())
            return 1f;
        else if (currentTime >= dawnTime && currentTime < dayTime) // فجر
            return Mathf.Lerp(0.3f, 1f, (currentTime - dawnTime) / (dayTime - dawnTime));
        else if (currentTime >= duskTime && currentTime < nightTime) // غسق
            return Mathf.Lerp(1f, 0.3f, (currentTime - duskTime) / (nightTime - duskTime));
        else // ليل
            return 0.3f;
    }
    
    void OnDestroy()
    {
        // إعادة تعيين السكاي بوكس إذا لزم الأمر
        if (skyboxMaterial != null)
        {
            skyboxMaterial.SetColor("_SkyColor", Color.white);
            skyboxMaterial.SetColor("_HorizonColor", Color.gray);
            skyboxMaterial.SetFloat("_StarsIntensity", 0f);
        }
    }
}
