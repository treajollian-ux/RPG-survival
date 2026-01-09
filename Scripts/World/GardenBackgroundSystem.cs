using UnityEngine;
using System.Collections.Generic;

public class GardenBackgroundSystem : MonoBehaviour
{
    [Header("Garden Elements")]
    public List<AnimatedPlant> plants;
    public List<WaterElement> waterElements;
    public WeatherSystem weatherSystem;
    
    [Header("Day/Night Cycle")]
    public Light sunLight;
    public Gradient dayNightGradient;
    public float dayDuration = 300f; // 5 دقائق حقيقية
    private float currentTime = 0f;
    
    [Header("Interactive Elements")]
    public float windStrength = 1f;
    public float waterFlowSpeed = 0.5f;
    
    [System.Serializable]
    public class AnimatedPlant
    {
        public Transform plantTransform;
        public AnimationCurve swayCurve;
        public float swaySpeed = 1f;
        public float maxSwayAngle = 15f;
        public bool reactsToWind = true;
        public bool reactsToPlayer = true;
        
        private Vector3 originalRotation;
        private float swayOffset;
        
        public void Initialize()
        {
            originalRotation = plantTransform.localEulerAngles;
            swayOffset = Random.Range(0f, 360f);
        }
        
        public void Update(float wind, float playerInfluence)
        {
            if (!plantTransform) return;
            
            float time = Time.time * swaySpeed + swayOffset;
            float baseSway = swayCurve.Evaluate(Mathf.Sin(time)) * maxSwayAngle;
            float windEffect = reactsToWind ? Mathf.Sin(time * 2f) * wind * 10f : 0f;
            float playerEffect = reactsToPlayer ? playerInfluence * 5f : 0f;
            
            float totalSway = baseSway + windEffect + playerEffect;
            
            plantTransform.localEulerAngles = originalRotation + new Vector3(
                0f, 0f, totalSway
            );
        }
    }
    
    [System.Serializable]
    public class WaterElement
    {
        public Transform waterTransform;
        public Material waterMaterial;
        public float flowSpeed = 0.1f;
        public float waveHeight = 0.1f;
        public Color waterColor = new Color(0.2f, 0.5f, 0.8f, 0.5f);
        
        private Vector2 uvOffset = Vector2.zero;
        private float waveOffset;
        
        public void Initialize()
        {
            if (waterMaterial)
            {
                waterMaterial.SetColor("_WaterColor", waterColor);
            }
            waveOffset = Random.Range(0f, 360f);
        }
        
        public void Update(float speedMultiplier)
        {
            if (!waterTransform) return;
            
            // تحريك UV للماء
            uvOffset.x += Time.deltaTime * flowSpeed * speedMultiplier;
            uvOffset.y = Mathf.Sin(Time.time * 0.5f + waveOffset) * waveHeight;
            
            if (waterMaterial)
            {
                waterMaterial.SetTextureOffset("_MainTex", uvOffset);
                
                // تأثير التموج
                float ripple = Mathf.PerlinNoise(
                    Time.time * 0.3f, 
                    waveOffset
                ) * 0.1f;
                
                waterMaterial.SetFloat("_RippleStrength", ripple);
            }
        }
    }
    
    void Start()
    {
        InitializeGarden();
        
        // بدء الموسيقى الخلفية للحديقة
        AudioManager.Instance.PlayAmbientSound("garden_ambient", true);
        
        // إضافة أصوات طيور عشوائية
        StartCoroutine(RandomBirdSounds());
    }
    
    void InitializeGarden()
    {
        foreach (var plant in plants)
        {
            plant.Initialize();
        }
        
        foreach (var water in waterElements)
        {
            water.Initialize();
        }
    }
    
    void Update()
    {
        UpdateDayNightCycle();
        UpdateGardenElements();
        UpdateWeatherEffects();
    }
    
    void UpdateDayNightCycle()
    {
        currentTime += Time.deltaTime / dayDuration;
        if (currentTime >= 1f) currentTime = 0f;
        
        // تحديث إضاءة الشمس
        if (sunLight)
        {
            float sunAngle = Mathf.Lerp(0f, 180f, currentTime);
            sunLight.transform.rotation = Quaternion.Euler(sunAngle, -30f, 0f);
            
            // تحديث لون الضوء
            sunLight.color = dayNightGradient.Evaluate(currentTime);
            
            // تغيير شدة الضوء
            sunLight.intensity = Mathf.Lerp(0.1f, 1f, 
                Mathf.SmoothStep(0f, 1f, 
                    Mathf.Abs(Mathf.Cos(currentTime * Mathf.PI))
                )
            );
        }
        
        // تغيير الموسيقى بين النهار والليل
        if (currentTime > 0.75f || currentTime < 0.25f) // ليل
        {
            AudioManager.Instance.PlayDynamicMusic(AudioManager.MusicIntensity.Calm);
            AudioManager.Instance.PlayAmbientSound("night_ambient", true);
        }
        else // نهار
        {
            AudioManager.Instance.PlayAmbientSound("garden_ambient", true);
        }
    }
    
    void UpdateGardenElements()
    {
        float wind = Mathf.PerlinNoise(Time.time * 0.1f, 0f) * windStrength;
        
        foreach (var plant in plants)
        {
            plant.Update(wind, 0f); // يمكن إضافة تأثير اللاعب هنا
        }
        
        foreach (var water in waterElements)
        {
            water.Update(waterFlowSpeed + wind * 0.1f);
        }
    }
    
    void UpdateWeatherEffects()
    {
        if (weatherSystem != null)
        {
            windStrength = weatherSystem.currentWindStrength;
            waterFlowSpeed = weatherSystem.waterFlowMultiplier;
        }
    }
    
    System.Collections.IEnumerator RandomBirdSounds()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(10f, 30f));
            
            if (currentTime > 0.25f && currentTime < 0.75f) // النهار فقط
            {
                AudioManager.Instance.PlaySFX("bird_chirp_" + Random.Range(1, 4), 
                    GetRandomGardenPosition(), 
                    0.2f);
            }
        }
    }
    
    Vector3 GetRandomGardenPosition()
    {
        // إرجاع موقع عشوائي في الحديقة
        return new Vector3(
            Random.Range(-20f, 20f),
            Random.Range(-10f, 10f),
            0f
        );
    }
    
    public void TriggerSpecialEvent(string eventName)
    {
        switch (eventName)
        {
            case "Rain":
                StartRain();
                break;
            case "Sunrise":
                TriggerSunrise();
                break;
            case "AnimalVisit":
                SpawnGardenAnimal();
                break;
        }
    }
    
    void StartRain()
    {
        // تشغيل صوت المطر
        AudioManager.Instance.PlayAmbientSound("rain", true);
        
        // تأثيرات بصرية للمطر
        // (يمكن إضافة نظام جسيمات للمطر هنا)
    }
    
    void TriggerSunrise()
    {
        // تأثير شروق الشمس الخاص
        StartCoroutine(SunriseRoutine());
        
        // نغمة شروق الشمس
        AudioManager.Instance.PlayEventTone("level_up");
    }
    
    System.Collections.IEnumerator SunriseRoutine()
    {
        float duration = 10f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            
            // تدرج الألوان خلال الشروق
            if (sunLight)
            {
                Color sunriseColor = Color.Lerp(
                    new Color(1f, 0.5f, 0.2f),
                    Color.white,
                    t
                );
                sunLight.color = sunriseColor;
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }
    }
    
    void SpawnGardenAnimal()
    {
        // ظهور حيوان عشوائي في الحديقة
        // (يمكن إضافة بريفاب للحيوانات هنا)
        
        AudioManager.Instance.PlaySFX("animal_appear", GetRandomGardenPosition());
    }
}
