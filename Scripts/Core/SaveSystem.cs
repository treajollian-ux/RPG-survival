using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public class SaveSystem : MonoBehaviour
{
    public static SaveSystem Instance;
    
    [Header("Save Settings")]
    public string saveFileName = "savegame";
    public int maxSaveSlots = 5;
    public bool autoSaveEnabled = true;
    public float autoSaveInterval = 300f; // 5 دقائق
    
    [Header("Encryption")]
    public bool useEncryption = true;
    public string encryptionKey = "MySecretKey123!";
    
    private float autoSaveTimer = 0f;
    private string savePath;
    
    [System.Serializable]
    public class GameData
    {
        public PlayerData playerData;
        public WorldData worldData;
        public InventoryData inventoryData;
        public QuestData questData;
        public DateTime saveTime;
        public int saveSlot;
        public string saveName;
        public float playTime;
        
        public GameData()
        {
            saveTime = DateTime.Now;
            playTime = 0f;
            playerData = new PlayerData();
            worldData = new WorldData();
            inventoryData = new InventoryData();
            questData = new QuestData();
        }
    }
    
    [System.Serializable]
    public class PlayerData
    {
        public Vector3 position;
        public Quaternion rotation;
        public float health;
        public float maxHealth;
        public float hunger;
        public float thirst;
        public float stamina;
        public int level;
        public float experience;
        public int skillPoints;
        public string equippedWeapon;
        public string equippedArmor;
    }
    
    [System.Serializable]
    public class WorldData
    {
        public float gameTime;
        public int dayCount;
        public WeatherSystem.WeatherType currentWeather;
        public float weatherTimer;
        public List<BuildingData> buildings;
        public List<ResourceData> resources;
    }
    
    [System.Serializable]
    public class BuildingData
    {
        public string prefabName;
        public Vector3 position;
        public Quaternion rotation;
        public float health;
    }
    
    [System.Serializable]
    public class ResourceData
    {
        public string resourceType;
        public Vector3 position;
        public int remainingAmount;
    }
    
    [System.Serializable]
    public class InventoryData
    {
        public List<ItemData> items;
        public int currency;
        public string[] hotbarItems;
        
        public InventoryData()
        {
            items = new List<ItemData>();
            hotbarItems = new string[8];
        }
    }
    
    [System.Serializable]
    public class ItemData
    {
        public string itemId;
        public int quantity;
        public bool isEquipped;
        public int slotIndex;
    }
    
    [System.Serializable]
    public class QuestData
    {
        public List<QuestProgressData> activeQuests;
        public List<string> completedQuests;
    }
    
    [System.Serializable]
    public class QuestProgressData
    {
        public string questId;
        public Dictionary<string, int> objectiveProgress;
    }
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // تحديد مسار الحفظ
            savePath = Application.persistentDataPath + "/Saves/";
            
            // إنشاء مجلد الحفظ إذا لم يكن موجوداً
            if (!Directory.Exists(savePath))
            {
                Directory.CreateDirectory(savePath);
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Update()
    {
        // الحفظ التلقائي
        if (autoSaveEnabled)
        {
            autoSaveTimer += Time.deltaTime;
            if (autoSaveTimer >= autoSaveInterval)
            {
                AutoSave();
                autoSaveTimer = 0f;
            }
        }
    }
    
    public void SaveGame(int slot = 0, string saveName = "")
    {
        if (slot < 0 || slot >= maxSaveSlots)
        {
            Debug.LogError($"رقم فتحة الحفظ غير صالح: {slot}");
            return;
        }
        
        GameData data = new GameData();
        
        // جمع بيانات اللاعب
        if (PlayerStats.Instance != null)
        {
            data.playerData = new PlayerData
            {
                position = GetPlayerPosition(),
                rotation = GetPlayerRotation(),
                health = PlayerStats.Instance.health,
                maxHealth = PlayerStats.Instance.maxHealth,
                hunger = PlayerStats.Instance.hunger,
                thirst = PlayerStats.Instance.thirst,
                stamina = PlayerStats.Instance.stamina,
                level = PlayerStats.Instance.level,
                experience = PlayerStats.Instance.experience,
                skillPoints = PlayerStats.Instance.skillPoints
            };
        }
        
        // جمع بيانات العالم
        data.worldData = CollectWorldData();
        
        // جمع بيانات المخزون
        if (InventorySystem.Instance != null)
        {
            data.inventoryData = CollectInventoryData();
        }
        
        // جمع بيانات المهام
        if (QuestSystem.Instance != null)
        {
            data.questData = CollectQuestData();
        }
        
        // معلومات الحفظ
        data.saveSlot = slot;
        data.saveName = string.IsNullOrEmpty(saveName) ? $"الحفظ {slot + 1}" : saveName;
        data.playTime = Time.timeSinceLevelLoad;
        data.saveTime = DateTime.Now;
        
        // تشفير البيانات إذا كان مفعلاً
        string jsonData = JsonUtility.ToJson(data, true);
        
        if (useEncryption)
        {
            jsonData = EncryptDecrypt(jsonData);
        }
        
        // حفظ الملف
        string filePath = GetSaveFilePath(slot);
        File.WriteAllText(filePath, jsonData);
        
        Debug.Log($"تم حفظ اللعبة في الفتحة {slot}: {filePath}");
        
        // إشعار النجاح
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowNotification("تم حفظ اللعبة بنجاح!", Color.green);
        }
        
        // نغمة الحفظ
        AudioManager.Instance.PlayEventTone("craft_success");
    }
    
    public bool LoadGame(int slot = 0)
    {
        string filePath = GetSaveFilePath(slot);
        
        if (!File.Exists(filePath))
        {
            Debug.LogWarning($"ملف الحفظ غير موجود: {filePath}");
            return false;
        }
        
        try
        {
            string jsonData = File.ReadAllText(filePath);
            
            if (useEncryption)
            {
                jsonData = EncryptDecrypt(jsonData);
            }
            
            GameData data = JsonUtility.FromJson<GameData>(jsonData);
            
            // تحميل بيانات اللاعب
            if (PlayerStats.Instance != null)
            {
                PlayerStats.Instance.health = data.playerData.health;
                PlayerStats.Instance.maxHealth = data.playerData.maxHealth;
                PlayerStats.Instance.hunger = data.playerData.hunger;
                PlayerStats.Instance.thirst = data.playerData.thirst;
                PlayerStats.Instance.stamina = data.playerData.stamina;
                PlayerStats.Instance.level = data.playerData.level;
                PlayerStats.Instance.experience = data.playerData.experience;
                PlayerStats.Instance.skillPoints = data.playerData.skillPoints;
                
                // تحميل موقع اللاعب
                SetPlayerPosition(data.playerData.position);
                SetPlayerRotation(data.playerData.rotation);
            }
            
            // تحميل بيانات العالم
            LoadWorldData(data.worldData);
            
            // تحميل بيانات المخزون
            if (InventorySystem.Instance != null)
            {
                LoadInventoryData(data.inventoryData);
            }
            
            // تحميل بيانات المهام
            if (QuestSystem.Instance != null)
            {
                LoadQuestData(data.questData);
            }
            
            Debug.Log($"تم تحميل اللعبة من الفتحة {slot}");
            
            // إشعار النجاح
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowNotification("تم تحميل اللعبة بنجاح!", Color.green);
            }
            
            // نغمة التحميل
            AudioManager.Instance.PlayEventTone("item_found");
            
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"خطأ في تحميل اللعبة: {e.Message}");
            return false;
        }
    }
    
    public void DeleteSave(int slot)
    {
        string filePath = GetSaveFilePath(slot);
        
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            Debug.Log($"تم حذف الحفظ في الفتحة {slot}");
        }
    }
    
    public bool SaveExists(int slot)
    {
        return File.Exists(GetSaveFilePath(slot));
    }
    
    public GameData GetSaveInfo(int slot)
    {
        string filePath = GetSaveFilePath(slot);
        
        if (!File.Exists(filePath))
            return null;
        
        try
        {
            string jsonData = File.ReadAllText(filePath);
            
            if (useEncryption)
            {
                jsonData = EncryptDecrypt(jsonData);
            }
            
            return JsonUtility.FromJson<GameData>(jsonData);
        }
        catch
        {
            return null;
        }
    }
    
    public List<GameData> GetAllSaves()
    {
        List<GameData> saves = new List<GameData>();
        
        for (int i = 0; i < maxSaveSlots; i++)
        {
            GameData save = GetSaveInfo(i);
            if (save != null)
            {
                saves.Add(save);
            }
        }
        
        return saves;
    }
    
    string GetSaveFilePath(int slot)
    {
        return Path.Combine(savePath, $"{saveFileName}_{slot}.save");
    }
    
    string EncryptDecrypt(string data)
    {
        // تشفير بسيط XOR
        System.Text.StringBuilder result = new System.Text.StringBuilder();
        
        for (int i = 0; i < data.Length; i++)
        {
            result.Append((char)(data[i] ^ encryptionKey[i % encryptionKey.Length]));
        }
        
        return result.ToString();
    }
    
    WorldData CollectWorldData()
    {
        WorldData worldData = new WorldData();
        
        // جمع بيانات الوقت
        if (GardenBackgroundSystem.Instance != null)
        {
            // يمكنك إضافة جمع بيانات الوقت هنا
        }
        
        // جمع بيانات الطقس
        if (WeatherSystem.Instance != null)
        {
            worldData.currentWeather = WeatherSystem.Instance.currentWeather;
        }
        
        // جمع بيانات المباني
        worldData.buildings = CollectBuildingData();
        
        // جمع بيانات الموارد
        worldData.resources = CollectResourceData();
        
        return worldData;
    }
    
    List<BuildingData> CollectBuildingData()
    {
        List<BuildingData> buildings = new List<BuildingData>();
        
        // البحث عن جميع المباني في المشهد
        GameObject[] buildingObjects = GameObject.FindGameObjectsWithTag("Building");
        
        foreach (GameObject building in buildingObjects)
        {
            BuildingData data = new BuildingData
            {
                prefabName = building.name.Replace("(Clone)", ""),
                position = building.transform.position,
                rotation = building.transform.rotation
            };
            
            buildings.Add(data);
        }
        
        return buildings
