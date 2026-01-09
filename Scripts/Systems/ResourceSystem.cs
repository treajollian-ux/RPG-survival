using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class ResourceSystem : MonoBehaviour
{
    public static ResourceSystem Instance;
    
    [Header("Resource Settings")]
    public List<ResourceDefinition> resourceDefinitions = new List<ResourceDefinition>();
    public float globalResourceMultiplier = 1f;
    
    [Header("Harvesting")]
    public float baseHarvestTime = 2f;
    public float harvestRange = 2f;
    public LayerMask resourceLayer;
    
    [Header("Audio")]
    public AudioClip harvestSound;
    public AudioClip resourceDepletedSound;
    
    [System.Serializable]
    public class ResourceDefinition
    {
        public string resourceId;
        public string displayName;
        public Sprite icon;
        public Color resourceColor = Color.white;
        public ResourceType type;
        public GameObject harvestEffect;
        public AudioClip harvestSound;
        
        [Header("Harvest Settings")]
        public ToolType requiredTool;
        public float harvestTimeMultiplier = 1f;
        public int baseYield = 1;
        public float respawnTime = 300f; // 5 دقائق
        
        [Header("Usage")]
        public bool isConsumable;
        public float hungerRestore; // إذا كان طعاماً
        public float thirstRestore; // إذا كان شراباً
        public float healthRestore; // إذا كان دواء
        
        [Header("Crafting")]
        public string[] craftingRecipes; // الوصفات التي يستخدم فيها
    }
    
    public enum ResourceType
    {
        RawMaterial,    // مواد خام
        Food,           // طعام
        Drink,          شراب
        Medicine,       // دواء
        Fuel,           وقود
        Special         // خاص
    }
    
    public enum ToolType
    {
        None,           // لا يحتاج أداة
        Hand,           // باليد
        Axe,            // فأس
        Pickaxe,        // معول
        Knife,          // سكين
        FishingRod,     صنارة صيد
        AnyTool         // أي أداة
    }
    
    private Dictionary<string, ResourceDefinition> resourceDictionary = new Dictionary<string, ResourceDefinition>();
    private Dictionary<string, int> resourceInventory = new Dictionary<string, int>();
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializeResources();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void InitializeResources()
    {
        // تحميل التعريفات إلى القاموس
        foreach (ResourceDefinition def in resourceDefinitions)
        {
            if (!resourceDictionary.ContainsKey(def.resourceId))
            {
                resourceDictionary[def.resourceId] = def;
            }
        }
        
        // إضافة الموارد الأساسية إذا كانت القائمة فارغة
        if (resourceDefinitions.Count == 0)
        {
            AddDefaultResources();
        }
        
        // تهيئة المخزون
        InitializeInventory();
    }
    
    void AddDefaultResources()
    {
        // خشب
        ResourceDefinition wood = new ResourceDefinition
        {
            resourceId = "wood",
            displayName = "خشب",
            type = ResourceType.RawMaterial,
            requiredTool = ToolType.Axe,
            harvestTimeMultiplier = 1f,
            baseYield = 3,
            respawnTime = 180f,
            resourceColor = new Color(0.55f, 0.27f, 0.07f)
        };
        resourceDefinitions.Add(wood);
        
        // حجر
        ResourceDefinition stone = new ResourceDefinition
        {
            resourceId = "stone",
            displayName = "حجر",
            type = ResourceType.RawMaterial,
            requiredTool = ToolType.Pickaxe,
            harvestTimeMultiplier = 1.2f,
            baseYield = 2,
            respawnTime = 240f,
            resourceColor = Color.gray
        };
        resourceDefinitions.Add(stone);
        
        // حديد
        ResourceDefinition iron = new ResourceDefinition
        {
            resourceId = "iron_ore",
            displayName = "خام حديد",
            type = ResourceType.RawMaterial,
            requiredTool = ToolType.Pickaxe,
            harvestTimeMultiplier = 1.5f,
            baseYield = 1,
            respawnTime = 600f,
            resourceColor = new Color(0.66f, 0.66f, 0.66f)
        };
        resourceDefinitions.Add(iron);
        
        // تفاح
        ResourceDefinition apple = new ResourceDefinition
        {
            resourceId = "apple",
            displayName = "تفاح",
            type = ResourceType.Food,
            requiredTool = ToolType.None,
            harvestTimeMultiplier = 0.5f,
            baseYield = 1,
            isConsumable = true,
            hungerRestore = 20f,
            healthRestore = 5f,
            respawnTime = 120f,
            resourceColor = Color.red
        };
        resourceDefinitions.Add(apple);
        
        // توت
        ResourceDefinition berries = new ResourceDefinition
        {
            resourceId = "berries",
            displayName = "توت",
            type = ResourceType.Food,
            requiredTool = ToolType.None,
            harvestTimeMultiplier = 0.3f,
            baseYield = 3,
            isConsumable = true,
            hungerRestore = 10f,
            thirstRestore = 5f,
            respawnTime = 90f,
            resourceColor = new Color(0.58f, 0f, 0.83f)
        };
        resourceDefinitions.Add(berries);
    }
    
    void InitializeInventory()
    {
        // إضافة بعض الموارد الأولية
        AddResourceToInventory("wood", 10);
        AddResourceToInventory("stone", 5);
        AddResourceToInventory("berries", 3);
    }
    
    public bool CanHarvest(GameObject resourceObject, ToolType equippedTool)
    {
        ResourceNode resourceNode = resourceObject.GetComponent<ResourceNode>();
        if (resourceNode == null) return false;
        
        ResourceDefinition definition = GetResourceDefinition(resourceNode.resourceType);
        if (definition == null) return false;
        
        // التحقق من الأداة المطلوبة
        if (definition.requiredTool != ToolType.None && definition.requiredTool != ToolType.AnyTool)
        {
            if (equippedTool != definition.requiredTool)
            {
                Debug.Log($"تحتاج إلى {definition.requiredTool} لجمع {definition.displayName}");
                return false;
            }
        }
        
        return !resourceNode.IsDepleted();
    }
    
    public float GetHarvestTime(string resourceId, ToolType tool, float skillMultiplier = 1f)
    {
        ResourceDefinition definition = GetResourceDefinition(resourceId);
        if (definition == null) return baseHarvestTime;
        
        float time = baseHarvestTime * definition.harvestTimeMultiplier;
        
        // تخفيض الوقت بناءً على الأداة والمهارة
        if (tool != ToolType.None)
        {
            time *= 0.7f; // 30% أسرع مع الأداة الصحيحة
        }
        
        time /= skillMultiplier;
        
        return Mathf.Max(0.5f, time); // لا يقل عن 0.5 ثانية
    }
    
    public int HarvestResource(GameObject resourceObject, ToolType tool, float skillMultiplier = 1f)
    {
        ResourceNode resourceNode = resourceObject.GetComponent<ResourceNode>();
        if (resourceNode == null) return 0;
        
        ResourceDefinition definition = GetResourceDefinition(resourceNode.resourceType);
        if (definition == null) return 0;
        
        // حساب الكمية
        int yield = definition.baseYield;
        
        // تأثير المهارة
        yield = Mathf.RoundToInt(yield * skillMultiplier);
        
        // تأثير المضاعف العام
        yield = Mathf.RoundToInt(yield * globalResourceMultiplier);
        
        // بعض العشوائية
        yield += Random.Range(-1, 2);
        yield = Mathf.Max(1, yield);
        
        // الحصاد الفعلي
        int harvested = resourceNode.Harvest(yield);
        
        if (harvested > 0)
        {
            // إضافة إلى المخزون
            AddResourceToInventory(resourceNode.resourceType, harvested);
            
            // تشغيل تأثيرات الحصاد
            PlayHarvestEffects(resourceObject.transform.position, definition);
            
            // زيادة مهارة الحصاد
            IncreaseHarvestingSkill(resourceNode.resourceType);
            
            // تحديث واجهة المستخدم
            UpdateResourceUI();
            
            Debug.Log($"جمعت {harvested} من {definition.displayName}");
        }
        
        return harvested;
    }
    
    void PlayHarvestEffects(Vector3 position, ResourceDefinition definition)
    {
        // تشغيل الصوت
        AudioClip sound = definition.harvestSound ?? harvestSound;
        if (sound != null)
        {
            AudioManager.Instance.PlaySFX(sound.name, position);
        }
        
        // تشغيل التأثير البصري
        if (definition.harvestEffect != null)
        {
            Instantiate(definition.harvestEffect, position, Quaternion.identity);
        }
        
        // جسيمات الحصاد
        GameObject particles = Resources.Load<GameObject>("Particles/HarvestParticles");
        if (particles != null)
        {
            GameObject particleObj = Instantiate(particles, position, Quaternion.identity);
            
            ParticleSystem particleSystem = particleObj.GetComponent<ParticleSystem>();
            if (particleSystem != null)
            {
                var main = particleSystem.main;
                main.startColor = definition.resourceColor;
            }
            
            Destroy(particleObj, 2f);
        }
    }
    
    void IncreaseHarvestingSkill(string resourceId)
    {
        if (PlayerStats.Instance != null)
        {
            // زيادة مهارة البقاء عند جمع الموارد
            PlayerStats.Instance.ImproveSkill("survival");
            
            // منح خبرة
            float xp = 10f * globalResourceMultiplier;
            PlayerStats.Instance.AddExperience(xp);
        }
    }
    
    public bool ConsumeResource(string resourceId)
    {
        ResourceDefinition definition = GetResourceDefinition(resourceId);
        if (definition == null || !definition.isConsumable) return false;
        
        if (HasResource(resourceId, 1))
        {
            RemoveResourceFromInventory(resourceId, 1);
            
            // تطبيق التأثيرات
            if (PlayerStats.Instance != null)
            {
                if (definition.hungerRestore > 0)
                    PlayerStats.Instance.Eat(definition.hungerRestore);
                
                if (definition.thirstRestore > 0)
                    PlayerStats.Instance.Drink(definition.thirstRestore);
                
                if (definition.healthRestore > 0)
                    PlayerStats.Instance.Heal(definition.healthRestore);
            }
            
            // نغمة الاستهلاك
            AudioManager.Instance.PlayEventTone("craft_success");
            
            return true;
        }
        
        return false;
    }
    
    public void AddResourceToInventory(string resourceId, int amount)
    {
        if (resourceInventory.ContainsKey(resourceId))
        {
            resourceInventory[resourceId] += amount;
        }
        else
        {
            resourceInventory[resourceId] = amount;
        }
        
        // تحديث نظام المخزون إذا كان موجوداً
        if (InventorySystem.Instance != null)
        {
            ResourceDefinition definition = GetResourceDefinition(resourceId);
            if (definition != null)
            {
                InventorySystem.Instance.AddItem(
                    resourceId,
                    definition.displayName,
                    definition.icon,
                    amount,
                    ConvertResourceTypeToItemType(definition.type)
                );
            }
        }
    }
    
    public bool RemoveResourceFromInventory(string resourceId, int amount)
    {
        if (HasResource(resourceId, amount))
        {
            resourceInventory[resourceId] -= amount;
            
            if (resourceInventory[resourceId] <= 0)
            {
                resourceInventory.Remove(resourceId);
            }
            
            UpdateResourceUI();
            return true;
        }
        
        return false;
    }
    
    public bool HasResource(string resourceId, int amount = 1)
    {
        return resourceInventory.ContainsKey(resourceId) && resourceInventory[resourceId] >= amount;
    }
    
    public int GetResourceCount(string resourceId)
    {
        return resourceInventory.ContainsKey(resourceId) ? resourceInventory[resourceId] : 0;
    }
    
    public ResourceDefinition GetResourceDefinition(string resourceId)
    {
        return resourceDictionary.ContainsKey(resourceId) ? resourceDictionary[resourceId] : null;
    }
    
    InventorySystem.ItemType ConvertResourceTypeToItemType(ResourceType resourceType)
    {
        switch (resourceType)
        {
            case ResourceType.Food:
            case ResourceType.Drink:
            case ResourceType.Medicine:
                return InventorySystem.ItemType.Consumable;
            case ResourceType.Fuel:
                return InventorySystem.ItemType.Resource;
            case ResourceType.Special:
                return InventorySystem.ItemType.Resource;
            default:
                return InventorySystem.ItemType.Resource;
        }
    }
    
    public List<string> GetAllResourceIds()
    {
        return new List<string>(resourceInventory.Keys);
    }
    
    public Dictionary<string, int> GetAllResources()
    {
        return new Dictionary<string, int>(resourceInventory);
    }
    
    public void UpdateResourceUI()
    {
        if (UIManager.Instance != null)
        {
            // يمكنك إضافة تحديث لواجهة الموارد هنا
        }
    }
    
    public float GetResourceValue(string resourceId)
    {
        // قيمة الموارد للتجارة
        ResourceDefinition definition = GetResourceDefinition(resourceId);
        if (definition == null) return 0f;
        
        float value = 10f; // قيمة أساسية
        
        switch (definition.type)
        {
            case ResourceType.RawMaterial:
                value *= 1f;
                break;
            case ResourceType.Food:
                value *= 2f;
                break;
            case ResourceType.Medicine:
                value *= 5f;
                break;
            case ResourceType.Special:
                value *= 10f;
                break;
        }
        
        // تعديل بناءً على الوقت المستغرق للحصاد
        value /= definition.harvestTimeMultiplier;
        
        return value;
    }
    
    public void SetGlobalMultiplier(float multiplier)
    {
        globalResourceMultiplier = Mathf.Max(0.1f, multiplier);
        Debug.Log($"مضاعف الموارد الجديد: {globalResourceMultiplier}");
    }
    
    public void SaveResources()
    {
        foreach (var resource in resourceInventory)
        {
            PlayerPrefs.SetInt($"Resource_{resource.Key}", resource.Value);
        }
        
        PlayerPrefs.SetFloat("ResourceMultiplier", globalResourceMultiplier);
        PlayerPrefs.Save();
    }
    
    public void LoadResources()
    {
        resourceInventory.Clear();
        
        foreach (ResourceDefinition definition in resourceDefinitions)
        {
            int amount = PlayerPrefs.GetInt($"Resource_{definition.resourceId}", 0);
            if (amount > 0)
            {
                resourceInventory[definition.resourceId] = amount;
            }
        }
        
        globalResourceMultiplier = PlayerPrefs.GetFloat("ResourceMultiplier", 1f);
    }
    
    void OnApplicationQuit()
    {
        SaveResources();
    }
}
