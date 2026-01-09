using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class QuestSystem : MonoBehaviour
{
    public static QuestSystem Instance;
    
    [Header("Quest Settings")]
    public List<Quest> activeQuests = new List<Quest>();
    public List<Quest> completedQuests = new List<Quest>();
    
    [Header("UI References")]
    public GameObject questPanel;
    public Transform questListContainer;
    public GameObject questEntryPrefab;
    
    [Header("Audio")]
    public AudioClip questAcceptedSound;
    public AudioClip questCompletedSound;
    public AudioClip questUpdatedSound;
    
    [System.Serializable]
    public class Quest
    {
        public string questId;
        public string questTitle;
        public string questDescription;
        public QuestType type;
        public QuestStatus status = QuestStatus.NotStarted;
        public List<QuestObjective> objectives = new List<QuestObjective>();
        public List<QuestReward> rewards = new List<QuestReward>();
        
        // تتبع التقدم
        public Dictionary<string, int> objectiveProgress = new Dictionary<string, int>();
        
        public bool IsCompleted()
        {
            foreach (QuestObjective objective in objectives)
            {
                if (!objectiveProgress.ContainsKey(objective.objectiveId) ||
                    objectiveProgress[objective.objectiveId] < objective.requiredAmount)
                {
                    return false;
                }
            }
            return true;
        }
    }
    
    [System.Serializable]
    public class QuestObjective
    {
        public string objectiveId;
        public string description;
        public ObjectiveType type;
        public string targetId; // ID للهدف (مثل نوع الوحش أو العنصر)
        public int requiredAmount;
        public bool isOptional;
    }
    
    [System.Serializable]
    public class QuestReward
    {
        public RewardType type;
        public string itemId;
        public int amount;
        public int experience;
        public int gold;
    }
    
    public enum QuestType
    {
        Main,
        Side,
        Daily,
        Tutorial
    }
    
    public enum QuestStatus
    {
        NotStarted,
        InProgress,
        Completed,
        Failed
    }
    
    public enum ObjectiveType
    {
        CollectItem,
        KillEnemy,
        TalkToNPC,
        CraftItem,
        BuildStructure,
        ExploreArea,
        SurviveTime
    }
    
    public enum RewardType
    {
        Item,
        Experience,
        Gold,
        SkillPoint,
        Recipe
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
        
        InitializeStartingQuests();
    }
    
    void InitializeStartingQuests()
    {
        // المهمة التعليمية الأولى
        Quest tutorialQuest = new Quest
        {
            questId = "tutorial_1",
            questTitle = "البداية البسيطة",
            questDescription = "جمع 10 خشب و 5 حجارة للبداية",
            type = QuestType.Tutorial,
            status = QuestStatus.NotStarted
        };
        
        // أهداف المهمة
        QuestObjective woodObjective = new QuestObjective
        {
            objectiveId = "collect_wood",
            description = "جمع خشب",
            type = ObjectiveType.CollectItem,
            targetId = "wood",
            requiredAmount = 10,
            isOptional = false
        };
        
        QuestObjective stoneObjective = new QuestObjective
        {
            objectiveId = "collect_stone",
            description = "جمع حجر",
            type = ObjectiveType.CollectItem,
            targetId = "stone",
            requiredAmount = 5,
            isOptional = false
        };
        
        tutorialQuest.objectives.Add(woodObjective);
        tutorialQuest.objectives.Add(stoneObjective);
        
        // مكافآت المهمة
        QuestReward reward = new QuestReward
        {
            type = RewardType.Experience,
            experience = 100,
            gold = 50
        };
        
        tutorialQuest.rewards.Add(reward);
        
        // إضافة المهمة
        AddQuest(tutorialQuest);
    }
    
    public void AddQuest(Quest quest)
    {
        if (!activeQuests.Contains(quest) && !completedQuests.Contains(quest))
        {
            quest.status = QuestStatus.InProgress;
            
            // تهيئة تتبع التقدم
            foreach (QuestObjective objective in quest.objectives)
            {
                quest.objectiveProgress[objective.objectiveId] = 0;
            }
            
            activeQuests.Add(quest);
            UpdateQuestUI();
            
            if (questAcceptedSound != null)
            {
                AudioManager.Instance.PlaySFX(questAcceptedSound.name, transform.position);
            }
            
            Debug.Log($"تمت إضافة مهمة جديدة: {quest.questTitle}");
        }
    }
    
    public void UpdateObjective(string objectiveId, int amount = 1)
    {
        foreach (Quest quest in activeQuests)
        {
            foreach (QuestObjective objective in quest.objectives)
            {
                if (objective.objectiveId == objectiveId && quest.objectiveProgress.ContainsKey(objectiveId))
                {
                    quest.objectiveProgress[objectiveId] += amount;
                    
                    // التأكد من عدم تجاوز الحد المطلوب
                    if (quest.objectiveProgress[objectiveId] > objective.requiredAmount)
                    {
                        quest.objectiveProgress[objectiveId] = objective.requiredAmount;
                    }
                    
                    // تحديث الواجهة
                    UpdateQuestUI();
                    
                    // تشغيل صوت التحديث
                    if (questUpdatedSound != null && amount > 0)
                    {
                        AudioManager.Instance.PlaySFX(questUpdatedSound.name, transform.position);
                    }
                    
                    // التحقق من اكتمال المهمة
                    CheckQuestCompletion(quest);
                    
                    return;
                }
            }
        }
    }
    
    public void UpdateCollectObjective(string itemId, int amount)
    {
        // تحديث أهداف جمع العناصر
        foreach (Quest quest in activeQuests)
        {
            foreach (QuestObjective objective in quest.objectives)
            {
                if (objective.type == ObjectiveType.CollectItem && objective.targetId == itemId)
                {
                    UpdateObjective(objective.objectiveId, amount);
                }
            }
        }
    }
    
    public void UpdateKillObjective(string enemyId)
    {
        // تحديث أهداف قتل الأعداء
        foreach (Quest quest in activeQuests)
        {
            foreach (QuestObjective objective in quest.objectives)
            {
                if (objective.type == ObjectiveType.KillEnemy && objective.targetId == enemyId)
                {
                    UpdateObjective(objective.objectiveId, 1);
                }
            }
        }
    }
    
    void CheckQuestCompletion(Quest quest)
    {
        if (quest.IsCompleted() && quest.status != QuestStatus.Completed)
        {
            CompleteQuest(quest);
        }
    }
    
    void CompleteQuest(Quest quest)
    {
        quest.status = QuestStatus.Completed;
        
        // نقل المهمة إلى المهام المكتملة
        activeQuests.Remove(quest);
        completedQuests.Add(quest);
        
        // منح المكافآت
        GiveQuestRewards(quest);
        
        // تحديث الواجهة
        UpdateQuestUI();
        
        // تشغيل صوت الإكمال
        if (questCompletedSound != null)
        {
            AudioManager.Instance.PlaySFX(questCompletedSound.name, transform.position);
        }
        
        // نغمة النجاح
        AudioManager.Instance.PlayEventTone("level_up");
        
        Debug.Log($"تم إكمال المهمة: {quest.questTitle}");
        
        // فحص إذا كانت هناك مهمة تالية
        CheckForNextQuest(quest);
    }
    
    void GiveQuestRewards(Quest quest)
    {
        foreach (QuestReward reward in quest.rewards)
        {
            switch (reward.type)
            {
                case RewardType.Item:
                    if (InventorySystem.Instance != null)
                    {
                        InventorySystem.Instance.AddItem(
                            reward.itemId,
                            reward.itemId,
                            null,
                            reward.amount,
                            InventorySystem.ItemType.Resource
                        );
                    }
                    break;
                    
                case RewardType.Experience:
                    if (PlayerStats.Instance != null)
                    {
                        PlayerStats.Instance.AddExperience(reward.experience);
                    }
                    break;
                    
                case RewardType.Gold:
                    // إضافة الذهب
                    // CurrencySystem.Instance.AddGold(reward.gold);
                    break;
                    
                case RewardType.Recipe:
                    if (CraftingSystem.Instance != null)
                    {
                        // إضافة وصفة جديدة
                    }
                    break;
            }
        }
    }
    
    void CheckForNextQuest(Quest quest)
    {
        // يمكنك إضافة منطق لتفعيل المهام التالية هنا
        switch (quest.questId)
        {
            case "tutorial_1":
                // تفعيل المهمة التالية
                AddQuest(CreateNextQuest());
                break;
        }
    }
    
    Quest CreateNextQuest()
    {
        // إنشاء المهمة التالية
        Quest nextQuest = new Quest
        {
            questId = "tutorial_2",
            questTitle = "بناء الملجأ الأول",
            questDescription = "ابنِ ملجأ بسيط للحماية",
            type = QuestType.Tutorial,
            status = QuestStatus.NotStarted
        };
        
        QuestObjective buildObjective = new QuestObjective
        {
            objectiveId = "build_shelter",
            description = "بناء ملجأ",
            type = ObjectiveType.BuildStructure,
            targetId = "wooden_shelter",
            requiredAmount = 1,
            isOptional = false
        };
        
        nextQuest.objectives.Add(buildObjective);
        
        QuestReward reward = new QuestReward
        {
            type = RewardType.Experience,
            experience = 200,
            gold = 100
        };
        
        nextQuest.rewards.Add(reward);
        
        return nextQuest;
    }
    
    public void ToggleQuestPanel()
    {
        bool isActive = questPanel.activeSelf;
        questPanel.SetActive(!isActive);
        
        if (questPanel.activeSelf)
        {
            UpdateQuestUI();
        }
    }
    
    void UpdateQuestUI()
    {
        if (!questPanel.activeSelf) return;
        
        // تنظيف القائمة القديمة
        foreach (Transform child in questListContainer)
        {
            Destroy(child.gameObject);
        }
        
        // عرض المهام النشطة
        foreach (Quest quest in activeQuests)
        {
            GameObject questEntry = Instantiate(questEntryPrefab, questListContainer);
            QuestEntryUI entryUI = questEntry.GetComponent<QuestEntryUI>();
            
            if (entryUI != null)
            {
                entryUI.Setup(quest);
            }
            
            // تعيين النص
            Text questText = questEntry.GetComponentInChildren<Text>();
            if (questText != null)
            {
                string progressText = "";
                foreach (QuestObjective objective in quest.objectives)
                {
                    if (quest.objectiveProgress.ContainsKey(objective.objectiveId))
                    {
                        progressText += $"{objective.description}: {quest.objectiveProgress[objective.objectiveId]}/{objective.requiredAmount}\n";
                    }
                }
                
                questText.text = $"{quest.questTitle}\n{progressText}";
            }
        }
    }
    
    public void SaveQuests()
    {
        QuestData data = new QuestData();
        
        foreach (Quest quest in activeQuests)
        {
            QuestSaveData questData = new QuestSaveData
            {
                questId = quest.questId,
                progressData = new Dictionary<string, int>(quest.objectiveProgress)
            };
            data.activeQuests.Add(questData);
        }
        
        foreach (Quest quest in completedQuests)
        {
            data.completedQuestIds.Add(quest.questId);
        }
        
        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString("QuestData", json);
        PlayerPrefs.Save();
    }
    
    public void LoadQuests()
    {
        if (PlayerPrefs.HasKey("QuestData"))
        {
            string json = PlayerPrefs.GetString("QuestData");
            QuestData data = JsonUtility.FromJson<QuestData>(json);
            
            // هنا يجب إعادة بناء المهام من البيانات المحفوظة
            // (هذا مثال مبسط، ستحتاج إلى نظام إدارة للمهام)
        }
    }
    
    [System.Serializable]
    public class QuestData
    {
        public List<QuestSaveData> activeQuests = new List<QuestSaveData>();
        public List<string> completedQuestIds = new List<string>();
    }
    
    [System.Serializable]
    public class QuestSaveData
    {
        public string questId;
        public Dictionary<string, int> progressData = new Dictionary<string, int>();
    }
}

// مكون واجهة المستخدم لإدخال المهام
public class QuestEntryUI : MonoBehaviour
{
    private QuestSystem.Quest quest;
    
    public void Setup(QuestSystem.Quest quest)
    {
        this.quest = quest;
    }
    
    public void OnClick()
    {
        if (quest != null)
        {
            // عرض تفاصيل المهمة
            Debug.Log($"تفاصيل المهمة: {quest.questDescription}");
        }
    }
}
