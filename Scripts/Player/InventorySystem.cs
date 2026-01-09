using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class InventorySystem : MonoBehaviour
{
    public static InventorySystem Instance;
    
    [Header("Inventory Settings")]
    public int maxSlots = 20;
    public List<InventoryItem> items = new List<InventoryItem>();
    
    [Header("UI References")]
    public GameObject inventoryPanel;
    public Transform itemContainer;
    public GameObject itemSlotPrefab;
    
    [Header("Audio")]
    public AudioClip addItemSound;
    public AudioClip removeItemSound;
    public AudioClip equipSound;
    
    [System.Serializable]
    public class InventoryItem
    {
        public string itemId;
        public string itemName;
        public Sprite icon;
        public int quantity;
        public ItemType type;
        public bool isEquipped;
        public GameObject itemPrefab; // للعناصر القابلة للاستخدام
    }
    
    public enum ItemType
    {
        Resource,
        Tool,
        Weapon,
        Armor,
        Consumable,
        Building
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
    }
    
    void Start()
    {
        InitializeInventory();
    }
    
    void InitializeInventory()
    {
        // إضافة بعض العناصر الأولية للاختبار
        AddItem("wood", "خشب", null, 10, ItemType.Resource);
        AddItem("stone", "حجر", null, 5, ItemType.Resource);
        AddItem("axe", "فأس", null, 1, ItemType.Tool);
    }
    
    public bool AddItem(string id, string name, Sprite icon, int quantity, ItemType type)
    {
        // البحث عن العنصر الموجود
        InventoryItem existingItem = items.Find(item => item.itemId == id);
        
        if (existingItem != null)
        {
            // زيادة الكمية
            existingItem.quantity += quantity;
            UpdateUI();
            
            // تشغيل صوت الإضافة
            if (addItemSound != null)
            {
                AudioManager.Instance.PlaySFX(addItemSound.name, transform.position);
            }
            
            // نغمة نجاح
            AudioManager.Instance.PlayEventTone("item_found");
            
            return true;
        }
        else if (items.Count < maxSlots)
        {
            // إضافة عنصر جديد
            InventoryItem newItem = new InventoryItem
            {
                itemId = id,
                itemName = name,
                icon = icon,
                quantity = quantity,
                type = type,
                isEquipped = false
            };
            
            items.Add(newItem);
            UpdateUI();
            
            if (addItemSound != null)
            {
                AudioManager.Instance.PlaySFX(addItemSound.name, transform.position);
            }
            
            return true;
        }
        
        return false; // المخزون ممتلئ
    }
    
    public bool RemoveItem(string id, int quantity)
    {
        InventoryItem item = items.Find(i => i.itemId == id);
        
        if (item != null && item.quantity >= quantity)
        {
            item.quantity -= quantity;
            
            if (item.quantity <= 0)
            {
                items.Remove(item);
                
                if (item.isEquipped)
                {
                    UnequipItem(item.itemId);
                }
            }
            
            UpdateUI();
            
            if (removeItemSound != null)
            {
                AudioManager.Instance.PlaySFX(removeItemSound.name, transform.position);
            }
            
            return true;
        }
        
        return false; // العنصر غير موجود أو الكمية غير كافية
    }
    
    public bool HasItem(string id, int quantity = 1)
    {
        InventoryItem item = items.Find(i => i.itemId == id);
        return item != null && item.quantity >= quantity;
    }
    
    public int GetItemQuantity(string id)
    {
        InventoryItem item = items.Find(i => i.itemId == id);
        return item != null ? item.quantity : 0;
    }
    
    public void ToggleInventory()
    {
        bool isActive = inventoryPanel.activeSelf;
        inventoryPanel.SetActive(!isActive);
        
        if (inventoryPanel.activeSelf)
        {
            UpdateUI();
            
            // تغيير الموسيقى عند فتح المخزون
            AudioManager.Instance.PlayDynamicMusic(AudioManager.MusicIntensity.Crafting);
        }
        else
        {
            AudioManager.Instance.PlayDynamicMusic(AudioManager.MusicIntensity.Calm);
        }
    }
    
    void UpdateUI()
    {
        if (!inventoryPanel.activeSelf) return;
        
        // تنظيف العناصر القديمة
        foreach (Transform child in itemContainer)
        {
            Destroy(child.gameObject);
        }
        
        // إنشاء عناصر جديدة
        foreach (InventoryItem item in items)
        {
            GameObject slot = Instantiate(itemSlotPrefab, itemContainer);
            InventorySlotUI slotUI = slot.GetComponent<InventorySlotUI>();
            
            if (slotUI != null)
            {
                slotUI.Setup(item, this);
            }
            
            // إضافة معلومات نصية
            Text itemText = slot.GetComponentInChildren<Text>();
            if (itemText != null)
            {
                itemText.text = $"{item.itemName} x{item.quantity}";
            }
            
            // إضافة أيقونة
            Image itemImage = slot.GetComponentInChildren<Image>();
            if (itemImage != null && item.icon != null)
            {
                itemImage.sprite = item.icon;
            }
        }
    }
    
    public void EquipItem(string itemId)
    {
        InventoryItem item = items.Find(i => i.itemId == itemId);
        
        if (item != null && (item.type == ItemType.Weapon || item.type == ItemType.Tool || item.type == ItemType.Armor))
        {
            // إلغاء تجهيز العناصر الأخرى من نفس النوع
            foreach (InventoryItem otherItem in items)
            {
                if (otherItem.type == item.type && otherItem.isEquipped)
                {
                    otherItem.isEquipped = false;
                }
            }
            
            item.isEquipped = true;
            
            if (equipSound != null)
            {
                AudioManager.Instance.PlaySFX(equipSound.name, transform.position);
            }
            
            Debug.Log($"تم تجهيز {item.itemName}");
        }
    }
    
    public void UnequipItem(string itemId)
    {
        InventoryItem item = items.Find(i => i.itemId == itemId);
        
        if (item != null)
        {
            item.isEquipped = false;
            Debug.Log($"تم إلغاء تجهيز {item.itemName}");
        }
    }
    
    public InventoryItem GetEquippedItem(ItemType type)
    {
        return items.Find(item => item.type == type && item.isEquipped);
    }
    
    public void SaveInventory()
    {
        InventoryData data = new InventoryData();
        
        foreach (InventoryItem item in items)
        {
            ItemData itemData = new ItemData
            {
                itemId = item.itemId,
                quantity = item.quantity,
                isEquipped = item.isEquipped
            };
            data.items.Add(itemData);
        }
        
        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString("PlayerInventory", json);
        PlayerPrefs.Save();
    }
    
    public void LoadInventory()
    {
        if (PlayerPrefs.HasKey("PlayerInventory"))
        {
            string json = PlayerPrefs.GetString("PlayerInventory");
            InventoryData data = JsonUtility.FromJson<InventoryData>(json);
            
            items.Clear();
            
            // ملاحظة: تحتاج إلى نظام لإدارة بيانات العناصر
            // هذا مثال مبسط
            foreach (ItemData itemData in data.items)
            {
                // هنا يجب تحميل بيانات العنصر من قاعدة البيانات
                AddItem(itemData.itemId, "Item", null, itemData.quantity, ItemType.Resource);
            }
        }
    }
    
    [System.Serializable]
    public class InventoryData
    {
        public List<ItemData> items = new List<ItemData>();
    }
    
    [System.Serializable]
    public class ItemData
    {
        public string itemId;
        public int quantity;
        public bool isEquipped;
    }
}

// مكون واجهة المستخدم لفئات المخزون
public class InventorySlotUI : MonoBehaviour
{
    private InventorySystem.InventoryItem item;
    private InventorySystem inventorySystem;
    
    public void Setup(InventorySystem.InventoryItem item, InventorySystem system)
    {
        this.item = item;
        this.inventorySystem = system;
    }
    
    public void OnClick()
    {
        // عند النقر على العنصر
        if (item != null)
        {
            Debug.Log($"نقرت على {item.itemName}");
            
            // يمكنك إضافة قائمة سياقية هنا
            ShowContextMenu();
        }
    }
    
    void ShowContextMenu()
    {
        // عرض قائمة خيارات للعنصر
        // (استخدم، رمي، تجهيز، إلخ)
    }
}
