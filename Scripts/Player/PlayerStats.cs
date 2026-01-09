using UnityEngine;
using System;

public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance;
    
    [Header("Health")]
    public float maxHealth = 100f;
    public float health = 100f;
    public float healthRegenRate = 1f;
    
    [Header("Hunger & Thirst")]
    public float maxHunger = 100f;
    public float hunger = 100f;
    public float hungerRate = 0.5f;
    public float maxThirst = 100f;
    public float thirst = 100f;
    public float thirstRate = 0.8f;
    
    [Header("Stamina")]
    public float maxStamina = 100f;
    public float stamina = 100f;
    public float staminaRegenRate = 10f;
    public float staminaDrainRate = 15f;
    
    [Header("Experience & Level")]
    public int level = 1;
    public float experience = 0f;
    public float[] levelXPRequirements = { 100, 250, 500, 1000, 2000, 4000, 8000, 15000 };
    
    [Header("Skills")]
    public int skillPoints = 0;
    public int combatSkill = 1;
    public int craftingSkill = 1;
    public int buildingSkill = 1;
    public int survivalSkill = 1;
    
    [Header("Temperature")]
    public float bodyTemperature = 37f;
    public float idealTemperature = 37f;
    
    [Header("Audio")]
    public AudioClip levelUpSound;
    public AudioClip lowHealthSound;
    public AudioClip lowHungerSound;
    
    // أحداث
    public event Action OnHealthChanged;
    public event Action OnLevelUp;
    public event Action OnStatChanged;
    
    private float lastStatUpdateTime = 0f;
    private float statUpdateInterval = 1f; // تحديث الإحصائيات كل ثانية
    
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
    }
    
    void Update()
    {
        // تحديث الإحصائيات على فترات
        if (Time.time - lastStatUpdateTime >= statUpdateInterval)
        {
            UpdateStats();
            lastStatUpdateTime = Time.time;
        }
        
        // تحديث التحمل في الوقت الفعلي
        UpdateStamina();
    }
    
    void UpdateStats()
    {
        // الجوع والعطش
        hunger = Mathf.Max(0, hunger - hungerRate);
        thirst = Mathf.Max(0, thirst - thirstRate);
        
        // تأثير الجوع والعطش على الصحة
        if (hunger <= 0)
        {
            TakeDamage(5f * statUpdateInterval, "الجوع");
            
            if (lowHungerSound != null && Time.frameCount % 60 == 0)
            {
                AudioManager.Instance.PlaySFX(lowHungerSound.name, transform.position);
            }
        }
        
        if (thirst <= 0)
        {
            TakeDamage(8f * statUpdateInterval, "العطش");
        }
        
        // تجديد الصحة إذا لم يكن هناك ضرر مؤخراً
        if (health < maxHealth && hunger > 20f && thirst > 20f)
        {
            health = Mathf.Min(maxHealth, health + healthRegenRate);
        }
        
        // تحديث الواجهة
        UpdateUI();
        
        // تشغيل الأحداث
        OnStatChanged?.Invoke();
    }
    
    void UpdateStamina()
    {
        // استنزاف التحمل عند الجري
        if (Input.GetKey(KeyCode.LeftShift) && IsMoving())
        {
            stamina = Mathf.Max(0, stamina - staminaDrainRate * Time.deltaTime);
        }
        else
        {
            // تجديد التحمل
            stamina = Mathf.Min(maxStamina, stamina + staminaRegenRate * Time.deltaTime);
        }
    }
    
    bool IsMoving()
    {
        return Input.GetAxis("Horizontal") != 0 || Input.GetAxis("Vertical") != 0;
    }
    
    public void TakeDamage(float amount, string source = "غير معروف")
    {
        health = Mathf.Max(0, health - amount);
        
        OnHealthChanged?.Invoke();
        UpdateUI();
        
        // صوت الضرر المنخفض
        if (health < maxHealth * 0.3f && lowHealthSound != null && Time.frameCount % 30 == 0)
        {
            AudioManager.Instance.PlaySFX(lowHealthSound.name, transform.position);
        }
        
        // التحقق من الوفاة
        if (health <= 0)
        {
            Die(source);
        }
    }
    
    public void Heal(float amount)
    {
        health = Mathf.Min(maxHealth, health + amount);
        OnHealthChanged?.Invoke();
        UpdateUI();
    }
    
    public void Eat(float hungerRestore, float healthRestore = 0f)
    {
        hunger = Mathf.Min(maxHunger, hunger + hungerRestore);
        
        if (healthRestore > 0)
        {
            Heal(healthRestore);
        }
        
        // نغمة الأكل
        AudioManager.Instance.PlayEventTone("craft_success");
    }
    
    public void Drink(float thirstRestore, float healthRestore = 0f)
    {
        thirst = Mathf.Min(maxThirst, thirst + thirstRestore);
        
        if (healthRestore > 0)
        {
            Heal(healthRestore);
        }
        
        // نغمة الشرب
        AudioManager.Instance.PlayEventTone("item_found");
    }
    
    public void AddExperience(float amount)
    {
        experience += amount;
        
        // التحقق من الترقية
        while (experience >= GetNextLevelXP())
        {
            LevelUp();
        }
        
        UpdateUI();
    }
    
    void LevelUp()
    {
        level++;
        skillPoints++;
        
        // إعادة تعيين الخبرة للنموذج الجديد
        experience -= GetNextLevelXP();
        
        // زيادة الإحصائيات القصوى
        maxHealth += 10f;
        maxStamina += 5f;
        
        // استعادة الصحة والتحمل
        health = maxHealth;
        stamina = maxStamina;
        
        // تشغيل صوت الترقية
        if (levelUpSound != null)
        {
            AudioManager.Instance.PlaySFX(levelUpSound.name, transform.position);
        }
        
        // نغمة الترقية
        AudioManager.Instance.PlayEventTone("level_up");
        
        // عرض إشعار
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowNotification($"لقد ارتقيت إلى المستوى {level}!", Color.yellow);
        }
        
        // تشغيل الحدث
        OnLevelUp?.Invoke();
        
        Debug.Log($"Level Up! New Level: {level}");
    }
    
    public float GetNextLevelXP()
    {
        int index = Mathf.Min(level - 1, levelXPRequirements.Length - 1);
        return levelXPRequirements[index];
    }
    
    public bool UseStamina(float amount)
    {
        if (stamina >= amount)
        {
            stamina -= amount;
            UpdateUI();
            return true;
        }
        return false;
    }
    
    public bool HasEnoughStamina(float amount)
    {
        return stamina >= amount;
    }
    
    void UpdateUI()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateHealthBar(health, maxHealth);
            UIManager.Instance.UpdateHungerBar(hunger, maxHunger);
            UIManager.Instance.UpdateThirstBar(thirst, maxThirst);
            UIManager.Instance.UpdateStaminaBar(stamina, maxStamina);
        }
    }
    
    void Die(string cause)
    {
        Debug.Log($"لقد ماتت! السبب: {cause}");
        
        // عرض شاشة الموت
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowGameOver(cause);
        }
        
        // إيقاف اللعبة
        Time.timeScale = 0f;
        
        // تشغيل صوت الموت
        AudioManager.Instance.PlaySFX("player_death", transform.position);
    }
    
    public void ImproveSkill(string skill, int amount = 1)
    {
        switch (skill.ToLower())
        {
            case "combat":
                combatSkill += amount;
                break;
            case "crafting":
                craftingSkill += amount;
                break;
            case "building":
                buildingSkill += amount;
                break;
            case "survival":
                survivalSkill += amount;
                break;
        }
        
        // استخدام نقاط المهارة إذا كانت متاحة
        if (skillPoints > 0)
        {
            skillPoints--;
        }
    }
    
    public float GetSkillMultiplier(string skill)
    {
        float baseMultiplier = 1f;
        
        switch (skill.ToLower())
        {
            case "combat":
                return baseMultiplier + (combatSkill * 0.1f);
            case "crafting":
                return baseMultiplier + (craftingSkill * 0.05f);
            case "building":
                return baseMultiplier + (buildingSkill * 0.08f);
            case "survival":
                return baseMultiplier + (survivalSkill * 0.03f);
            default:
                return baseMultiplier;
        }
    }
    
    public void SaveStats()
    {
        PlayerPrefs.SetFloat("PlayerHealth", health);
        PlayerPrefs.SetFloat("PlayerMaxHealth", maxHealth);
        PlayerPrefs.SetFloat("PlayerHunger", hunger);
        PlayerPrefs.SetFloat("PlayerThirst", thirst);
        PlayerPrefs.SetFloat("PlayerStamina", stamina);
        PlayerPrefs.SetInt("PlayerLevel", level);
        PlayerPrefs.SetFloat("PlayerExperience", experience);
        PlayerPrefs.SetInt("PlayerSkillPoints", skillPoints);
        PlayerPrefs.SetInt("CombatSkill", combatSkill);
        PlayerPrefs.SetInt("CraftingSkill", craftingSkill);
        PlayerPrefs.SetInt("BuildingSkill", buildingSkill);
        PlayerPrefs.SetInt("SurvivalSkill", survivalSkill);
        PlayerPrefs.Save();
    }
    
    public void LoadStats()
    {
        health = PlayerPrefs.GetFloat("PlayerHealth", maxHealth);
        maxHealth = PlayerPrefs.GetFloat("PlayerMaxHealth", maxHealth);
        hunger = PlayerPrefs.GetFloat("PlayerHunger", maxHunger);
        thirst = PlayerPrefs.GetFloat("PlayerThirst", maxThirst);
        stamina = PlayerPrefs.GetFloat("PlayerStamina", maxStamina);
        level = PlayerPrefs.GetInt("PlayerLevel", 1);
        experience = PlayerPrefs.GetFloat("PlayerExperience", 0f);
        skillPoints = PlayerPrefs.GetInt("PlayerSkillPoints", 0);
        combatSkill = PlayerPrefs.GetInt("CombatSkill", 1);
        craftingSkill = PlayerPrefs.GetInt("CraftingSkill", 1);
        buildingSkill = PlayerPrefs.GetInt("BuildingSkill", 1);
        survivalSkill = PlayerPrefs.GetInt("SurvivalSkill", 1);
    }
    
    void OnApplicationQuit()
    {
        SaveStats();
    }
}
