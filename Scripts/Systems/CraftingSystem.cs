using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class CraftingSystem : MonoBehaviour
{
    public static CraftingSystem Instance;
    
    [Header("Crafting Settings")]
    public List<CraftingRecipe> recipes = new List<CraftingRecipe>();
    public Transform craftingPanel;
    public GameObject recipeButtonPrefab;
    
    [Header("Audio")]
    public AudioClip craftingSound;
    public AudioClip craftingFailedSound;
    
    [System.Serializable]
    public class CraftingRecipe
    {
        public string recipeId;
        public string recipeName;
        public Sprite resultIcon;
        public string resultItemId;
        public int resultQuantity = 1;
        
        public List<RecipeIngredient> ingredients = new List<RecipeIngredient>();
        public CraftingCategory category;
        public float craftingTime = 2f; // الوقت اللازم للصناعة بالثواني
    }
    
    [System.Serializable]
    public class RecipeIngredient
    {
        public string itemId;
        public int quantity;
    }
    
    public enum CraftingCategory
    {
        Tools,
        Weapons,
        Armor,
        Building,
        Consumables,
        Resources
    }
    
    private bool isCrafting = false;
    private float craftingTimer = 0f;
    private CraftingRecipe currentRecipe;
    
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
        
        InitializeRecipes();
    }
    
    void InitializeRecipes()
    {
        // مثال: وصفة صنع فأس
        CraftingRecipe axeRecipe = new CraftingRecipe
        {
            recipeId = "axe_recipe",
            recipeName = "فأس حديدي",
            resultItemId = "iron_axe",
            resultQuantity = 1,
            category = CraftingCategory.Tools,
            craftingTime = 5f
        };
        
        axeRecipe.ingredients.Add(new RecipeIngredient { itemId = "wood", quantity = 5 });
        axeRecipe.ingredients.Add(new RecipeIngredient { itemId = "iron", quantity = 3 });
        
        recipes.Add(axeRecipe);
        
        // مثال: وصفة بناء جدار
        CraftingRecipe wallRecipe = new CraftingRecipe
        {
            recipeId = "wall_recipe",
            recipeName = "جدار حجري",
            resultItemId = "stone_wall",
            resultQuantity = 1,
            category = CraftingCategory.Building,
            craftingTime = 3f
        };
        
        wallRecipe.ingredients.Add(new RecipeIngredient { itemId = "stone", quantity = 10 });
        
        recipes.Add(wallRecipe);
    }
    
    void Update()
    {
        if (isCrafting)
        {
            craftingTimer -= Time.deltaTime;
            
            // تحديث شريط التقدم
            UpdateCraftingProgress();
            
            if (craftingTimer <= 0f)
            {
                CompleteCrafting();
            }
        }
    }
    
    public void ToggleCraftingPanel()
    {
        bool isActive = craftingPanel.gameObject.activeSelf;
        craftingPanel.gameObject.SetActive(!isActive);
        
        if (craftingPanel.gameObject.activeSelf)
        {
            UpdateCraftingUI();
            
            // تغيير الموسيقى
            AudioManager.Instance.PlayDynamicMusic(AudioManager.MusicIntensity.Crafting);
        }
        else
        {
            AudioManager.Instance.PlayDynamicMusic(AudioManager.MusicIntensity.Calm);
        }
    }
    
    void UpdateCraftingUI()
    {
        // تنظيف الواجهة القديمة
        foreach (Transform child in craftingPanel)
        {
            Destroy(child.gameObject);
        }
        
        // إنشاء أزرار الوصفات
        foreach (CraftingRecipe recipe in recipes)
        {
            if (CanCraft(recipe))
            {
                CreateRecipeButton(recipe);
            }
        }
    }
    
    void CreateRecipeButton(CraftingRecipe recipe)
    {
        GameObject buttonObj = Instantiate(recipeButtonPrefab, craftingPanel);
        Button button = buttonObj.GetComponent<Button>();
        
        // تعيين نص الزر
        Text buttonText = buttonObj.GetComponentInChildren<Text>();
        if (buttonText != null)
        {
            buttonText.text = $"{recipe.recipeName} ({GetCraftingTimeString(recipe.craftingTime)})";
        }
        
        // تعيين الأيقونة
        Image iconImage = buttonObj.transform.Find("Icon")?.GetComponent<Image>();
        if (iconImage != null && recipe.resultIcon != null)
        {
            iconImage.sprite = recipe.resultIcon;
        }
        
        // إضافة حدث النقر
        button.onClick.AddListener(() => StartCrafting(recipe));
        
        // إضافة معلومات عن المكونات
        GameObject ingredientsPanel = buttonObj.transform.Find("IngredientsPanel")?.gameObject;
        if (ingredientsPanel != null)
        {
            Text ingredientsText = ingredientsPanel.GetComponentInChildren<Text>();
            if (ingredientsText != null)
            {
                ingredientsText.text = GetIngredientsString(recipe);
            }
        }
    }
    
    string GetIngredientsString(CraftingRecipe recipe)
    {
        string result = "المكونات: ";
        foreach (var ingredient in recipe.ingredients)
        {
            result += $"{ingredient.itemId} x{ingredient.quantity}, ";
        }
        return result.TrimEnd(',', ' ');
    }
    
    string GetCraftingTimeString(float time)
    {
        return $"{time:F1}ث";
    }
    
    public bool CanCraft(CraftingRecipe recipe)
    {
        if (InventorySystem.Instance == null) return false;
        
        foreach (RecipeIngredient ingredient in recipe.ingredients)
        {
            if (!InventorySystem.Instance.HasItem(ingredient.itemId, ingredient.quantity))
            {
                return false;
            }
        }
        
        return true;
    }
    
    public void StartCrafting(CraftingRecipe recipe)
    {
        if (isCrafting)
        {
            Debug.Log("جاري الصناعة بالفعل!");
            return;
        }
        
        if (!CanCraft(recipe))
        {
            Debug.Log("لا تملك المواد الكافية!");
            
            if (craftingFailedSound != null)
            {
                AudioManager.Instance.PlaySFX(craftingFailedSound.name, transform.position);
            }
            
            AudioManager.Instance.PlayEventTone("warning");
            return;
        }
        
        // استهلاك المواد
        foreach (RecipeIngredient ingredient in recipe.ingredients)
        {
            InventorySystem.Instance.RemoveItem(ingredient.itemId, ingredient.quantity);
        }
        
        // بدء الصناعة
        isCrafting = true;
        currentRecipe = recipe;
        craftingTimer = recipe.craftingTime;
        
        Debug.Log($"بدأت صناعة {recipe.recipeName}...");
        
        if (craftingSound != null)
        {
            AudioManager.Instance.PlaySFX(craftingSound.name, transform.position);
        }
    }
    
    void UpdateCraftingProgress()
    {
        // تحديث شريط التقدم في الواجهة
        float progress = 1f - (craftingTimer / currentRecipe.craftingTime);
        
        // يمكنك تحديث واجهة المستخدم هنا
        // مثال: UIManager.Instance.UpdateCraftingProgress(progress);
    }
    
    void CompleteCrafting()
    {
        isCrafting = false;
        
        // إضافة العنصر المصنوع إلى المخزون
        if (InventorySystem.Instance != null && currentRecipe != null)
        {
            InventorySystem.Instance.AddItem(
                currentRecipe.resultItemId,
                currentRecipe.recipeName,
                currentRecipe.resultIcon,
                currentRecipe.resultQuantity,
                GetItemTypeFromCategory(currentRecipe.category)
            );
            
            Debug.Log($"تم صناعة {currentRecipe.resultQuantity}x {currentRecipe.recipeName}!");
            
            // نغمة النجاح
            AudioManager.Instance.PlayEventTone("craft_success");
            
            // جسيمات التأثير
            SpawnCraftingParticles();
        }
        
        currentRecipe = null;
        craftingTimer = 0f;
        
        // تحديث واجهة الصناعة
        UpdateCraftingUI();
    }
    
    InventorySystem.ItemType GetItemTypeFromCategory(CraftingCategory category)
    {
        switch (category)
        {
            case CraftingCategory.Tools: return InventorySystem.ItemType.Tool;
            case CraftingCategory.Weapons: return InventorySystem.ItemType.Weapon;
            case CraftingCategory.Armor: return InventorySystem.ItemType.Armor;
            case CraftingCategory.Building: return InventorySystem.ItemType.Building;
            case CraftingCategory.Consumables: return InventorySystem.ItemType.Consumable;
            default: return InventorySystem.ItemType.Resource;
        }
    }
    
    void SpawnCraftingParticles()
    {
        // إنشاء جسيمات عند اكتمال الصناعة
        GameObject particleSystemPrefab = Resources.Load<GameObject>("Particles/CraftingSuccess");
        if (particleSystemPrefab != null)
        {
            Instantiate(particleSystemPrefab, transform.position, Quaternion.identity);
        }
    }
    
    public void CancelCrafting()
    {
        if (isCrafting && currentRecipe != null)
        {
            // إرجاع المواد
            foreach (RecipeIngredient ingredient in currentRecipe.ingredients)
            {
                InventorySystem.Instance.AddItem(
                    ingredient.itemId,
                    ingredient.itemId,
                    null,
                    ingredient.quantity,
                    InventorySystem.ItemType.Resource
                );
            }
            
            isCrafting = false;
            currentRecipe = null;
            craftingTimer = 0f;
            
            Debug.Log("تم إلغاء الصناعة!");
        }
    }
    
    // للتحكم من خلال المدخلات
    void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            ToggleCraftingPanel();
        }
        
        if (Input.GetKeyDown(KeyCode.Escape) && isCrafting)
        {
            CancelCrafting();
        }
    }
}
