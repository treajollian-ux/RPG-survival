using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;
    
    [Header("Main UI Elements")]
    public Canvas mainCanvas;
    public GameObject hudPanel;
    public GameObject pauseMenu;
    public GameObject gameOverPanel;
    public GameObject victoryPanel;
    
    [Header("Player Stats UI")]
    public Slider healthSlider;
    public Slider hungerSlider;
    public Slider thirstSlider;
    public Slider staminaSlider;
    public Text levelText;
    public Text experienceText;
    public Text timeText;
    
    [Header("Hotbar")]
    public Image[] hotbarSlots;
    public Text[] hotbarNumbers;
    public Image selectedSlotHighlight;
    private int selectedSlot = 0;
    
    [Header("Notifications")]
    public GameObject notificationPrefab;
    public Transform notificationPanel;
    public float notificationDuration = 3f;
    
    [Header("Crafting/Building UI")]
    public GameObject craftingPanel;
    public GameObject buildingPanel;
    public GameObject inventoryPanel;
    public GameObject questPanel;
    
    [Header("Audio")]
    public AudioClip uiClickSound;
    public AudioClip uiHoverSound;
    public AudioClip notificationSound;
    
    private bool isPaused = false;
    
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
        InitializeUI();
        UpdateAllUI();
    }
    
    void InitializeUI()
    {
        // إخفاء لوحات القوائم
        if (pauseMenu != null) pauseMenu.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (victoryPanel != null) victoryPanel.SetActive(false);
        if (craftingPanel != null) craftingPanel.SetActive(false);
        if (buildingPanel != null) buildingPanel.SetActive(false);
        if (inventoryPanel != null) inventoryPanel.SetActive(false);
        if (questPanel != null) questPanel.SetActive(false);
        
        // إظهار HUD
        if (hudPanel != null) hudPanel.SetActive(true);
        
        // تحديث شريط الاختيار
        UpdateHotbarSelection();
    }
    
    void Update()
    {
        HandleInput();
        UpdateDynamicUI();
    }
    
    void HandleInput()
    {
        // تبديل الإيقاف المؤقت
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePauseMenu();
        }
        
        // تبديل المخزون
        if (Input.GetKeyDown(KeyCode.I))
        {
            ToggleInventory();
        }
        
        // تبديل الصناعة
        if (Input.GetKeyDown(KeyCode.C))
        {
            ToggleCrafting();
        }
        
        // تبديل المهام
        if (Input.GetKeyDown(KeyCode.Q))
        {
            ToggleQuests();
        }
        
        // اختيار شريط الاختيار
        HandleHotbarInput();
    }
    
    void HandleHotbarInput()
    {
        // اختيار الشريط بالأرقام
        for (int i = 0; i < hotbarSlots.Length; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                SelectHotbarSlot(i);
            }
        }
        
        // اختيار الشريط بعجلة الفأرة
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            if (scroll > 0)
            {
                selectedSlot--;
                if (selectedSlot < 0) selectedSlot = hotbarSlots.Length - 1;
            }
            else
            {
                selectedSlot++;
                if (selectedSlot >= hotbarSlots.Length) selectedSlot = 0;
            }
            
            SelectHotbarSlot(selectedSlot);
        }
    }
    
    void SelectHotbarSlot(int slotIndex)
    {
        selectedSlot = slotIndex;
        UpdateHotbarSelection();
        
        // تشغيل صوت الاختيار
        PlayUISound(uiClickSound);
        
        // استخدام العنصر المحدد
        UseHotbarItem(slotIndex);
    }
    
    void UpdateHotbarSelection()
    {
        if (selectedSlotHighlight != null && hotbarSlots.Length > selectedSlot)
        {
            // تحريك الإضاءة إلى المكان المحدد
            selectedSlotHighlight.transform.position = hotbarSlots[selectedSlot].transform.position;
            
            // تغيير حجم العنصر المحدد
            for (int i = 0; i < hotbarSlots.Length; i++)
            {
                float scale = (i == selectedSlot) ? 1.2f : 1f;
                hotbarSlots[i].transform.localScale = Vector3.one * scale;
            }
        }
    }
    
    void UseHotbarItem(int slotIndex)
    {
        // هنا يمكنك إضافة منطق استخدام العنصر
        // مثال:
        // InventoryItem item = InventorySystem.Instance.GetHotbarItem(slotIndex);
        // if (item != null) item.Use();
        
        Debug.Log($"استخدام العنصر في الشريط {slotIndex + 1}");
    }
    
    void UpdateDynamicUI()
    {
        // تحديث الوقت في اللعبة
        if (timeText != null)
        {
            float gameTime = Time.timeSinceLevelLoad;
            int minutes = Mathf.FloorToInt(gameTime / 60f);
            int seconds = Mathf.FloorToInt(gameTime % 60f);
            timeText.text = $"{minutes:00}:{seconds:00}";
        }
        
        // تحديث الإحصائيات (مثال)
        if (PlayerStats.Instance != null)
        {
            UpdatePlayerStatsUI();
        }
    }
    
    void UpdatePlayerStatsUI()
    {
        // تحديث شرائط الصحة والجوع والعطش
        if (healthSlider != null)
            healthSlider.value = PlayerStats.Instance.health / PlayerStats.Instance.maxHealth;
        
        if (hungerSlider != null)
            hungerSlider.value = PlayerStats.Instance.hunger / PlayerStats.Instance.maxHunger;
        
        if (thirstSlider != null)
            thirstSlider.value = PlayerStats.Instance.thirst / PlayerStats.Instance.maxThirst;
        
        if (staminaSlider != null)
            staminaSlider.value = PlayerStats.Instance.stamina / PlayerStats.Instance.maxStamina;
        
        if (levelText != null)
            levelText.text = $"المستوى: {PlayerStats.Instance.level}";
        
        if (experienceText != null)
            experienceText.text = $"الخبرة: {PlayerStats.Instance.experience}/{PlayerStats.Instance.GetNextLevelXP()}";
    }
    
    public void UpdateAllUI()
    {
        UpdatePlayerStatsUI();
        UpdateHotbarUI();
    }
    
    void UpdateHotbarUI()
    {
        // تحديث شريط الاختيار بالعناصر
        if (InventorySystem.Instance != null)
        {
            // هذا مثال، تحتاج إلى تكييفه مع نظام المخزون الخاص بك
            for (int i = 0; i < hotbarSlots.Length; i++)
            {
                // InventoryItem item = InventorySystem.Instance.GetHotbarItem(i);
                // if (item != null)
                // {
                //     hotbarSlots[i].sprite = item.icon;
                //     hotbarNumbers[i].text = item.quantity > 1 ? item.quantity.ToString() : "";
                // }
            }
        }
    }
    
    public void TogglePauseMenu()
    {
        isPaused = !isPaused;
        
        if (pauseMenu != null)
        {
            pauseMenu.SetActive(isPaused);
        }
        
        Time.timeScale = isPaused ? 0f : 1f;
        
        // تشغيل صوت القائمة
        PlayUISound(uiClickSound);
        
        // تغيير وضع المؤشر
        Cursor.visible = isPaused;
        Cursor.lockState = isPaused ? CursorLockMode.None : CursorLockMode.Locked;
    }
    
    public void ToggleInventory()
    {
        if (inventoryPanel != null)
        {
            bool isActive = !inventoryPanel.activeSelf;
            inventoryPanel.SetActive(isActive);
            
            PlayUISound(uiClickSound);
            
            // إيقاف/استئناف الوقت إذا فتحت المخزون
            if (isActive)
            {
                Time.timeScale = 0f;
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
            else
            {
                Time.timeScale = 1f;
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
        }
    }
    
    public void ToggleCrafting()
    {
        if (craftingPanel != null)
        {
            bool isActive = !craftingPanel.activeSelf;
            craftingPanel.SetActive(isActive);
            
            PlayUISound(uiClickSound);
        }
    }
    
    public void ToggleBuilding()
    {
        if (buildingPanel != null)
        {
            bool isActive = !buildingPanel.activeSelf;
            buildingPanel.SetActive(isActive);
            
            PlayUISound(uiClickSound);
        }
    }
    
    public void ToggleQuests()
    {
        if (questPanel != null)
        {
            bool isActive = !questPanel.activeSelf;
            questPanel.SetActive(isActive);
            
            PlayUISound(uiClickSound);
        }
    }
    
    public void ShowNotification(string message, Color color)
    {
        if (notificationPrefab == null || notificationPanel == null) return;
        
        GameObject notification = Instantiate(notificationPrefab, notificationPanel);
        Text notificationText = notification.GetComponentInChildren<Text>();
        
        if (notificationText != null)
        {
            notificationText.text = message;
            notificationText.color = color;
        }
        
        // تشغيل صوت الإشعار
        PlayUISound(notificationSound);
        
        // تدمير الإشعار بعد فترة
        StartCoroutine(DestroyNotification(notification, notificationDuration));
    }
    
    IEnumerator DestroyNotification(GameObject notification, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // تأثير التلاشي
        CanvasGroup canvasGroup = notification.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            float fadeTime = 0.5f;
            float elapsed = 0f;
            
            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = 1f - (elapsed / fadeTime);
                yield return null;
            }
        }
        
        Destroy(notification);
    }
    
    public void ShowGameOver(string reason = "")
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            
            Text reasonText = gameOverPanel.GetComponentInChildren<Text>();
            if (reasonText != null && !string.IsNullOrEmpty(reason))
            {
                reasonText.text = $"سبب الوفاة: {reason}";
            }
            
            Time.timeScale = 0f;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            
            // تشغيل صوت الخسارة
            PlayUISound(notificationSound);
        }
    }
    
    public void ShowVictory()
    {
        if (victoryPanel != null)
        {
            victoryPanel.SetActive(true);
            
            Time.timeScale = 0f;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            
            // تشغيل صوت الفوز
            PlayUISound(notificationSound);
            
            // نغمة النصر
            AudioManager.Instance.PlayEventTone("level_up");
        }
    }
    
    public void UpdateHealthBar(float currentHealth, float maxHealth)
    {
        if (healthSlider != null)
        {
            healthSlider.value = currentHealth / maxHealth;
            
            // تغيير اللون بناءً على الصحة
            Image fillImage = healthSlider.fillRect.GetComponent<Image>();
            if (fillImage != null)
            {
                float healthPercent = currentHealth / maxHealth;
                if (healthPercent > 0.5f)
                    fillImage.color = Color.green;
                else if (healthPercent > 0.25f)
                    fillImage.color = Color.yellow;
                else
                    fillImage.color = Color.red;
            }
        }
    }
    
    public void UpdateHungerBar(float currentHunger, float maxHunger)
    {
        if (hungerSlider != null)
        {
            hungerSlider.value = currentHunger / maxHunger;
        }
    }
    
    public void UpdateThirstBar(float currentThirst, float maxThirst)
    {
        if (thirstSlider != null)
        {
            thirstSlider.value = currentThirst / maxThirst;
        }
    }
    
    public void UpdateStaminaBar(float currentStamina, float maxStamina)
    {
        if (staminaSlider != null)
        {
            staminaSlider.value = currentStamina / maxStamina;
        }
    }
    
    public void PlayUISound(AudioClip clip)
    {
        if (clip != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(clip.name, Vector3.zero);
        }
    }
    
    public void OnButtonHover()
    {
        PlayUISound(uiHoverSound);
    }
    
    public void OnButtonClick()
    {
        PlayUISound(uiClickSound);
    }
    
    // وظائف الأزرار
    public void OnResumeButton()
    {
        TogglePauseMenu();
    }
    
    public void OnSaveButton()
    {
        // حفظ اللعبة
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SaveGame();
            ShowNotification("تم حفظ اللعبة!", Color.green);
        }
    }
    
    public void OnLoadButton()
    {
        // تحميل اللعبة
        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoadGame();
            ShowNotification("تم تحميل اللعبة!", Color.green);
        }
    }
    
    public void OnSettingsButton()
    {
        // فتح إعدادات اللعبة
        ShowNotification("فتح الإعدادات قريباً!", Color.yellow);
    }
    
    public void OnQuitButton()
    {
        // الخروج للقائمة الرئيسية
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
    
    public void OnExitButton()
    {
        // الخروج من اللعبة
        Application.Quit();
    }
    
    public void OnRestartButton()
    {
        // إعادة تشغيل المشهد الحالي
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        );
    }
}
