using UnityEngine;
using System.Collections.Generic;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance;
    
    [Header("Input Settings")]
    public float mouseSensitivity = 2f;
    public bool invertMouseY = false;
    public KeyCode interactKey = KeyCode.E;
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode sprintKey = KeyCode.LeftShift;
    public KeyCode crouchKey = KeyCode.LeftControl;
    
    [Header("Action Keys")]
    public KeyCode inventoryKey = KeyCode.I;
    public KeyCode craftingKey = KeyCode.C;
    public KeyCode buildingKey = KeyCode.B;
    public KeyCode questKey = KeyCode.Q;
    public KeyCode mapKey = KeyCode.M;
    public KeyCode pauseKey = KeyCode.Escape;
    
    [Header("Hotbar Keys")]
    public KeyCode[] hotbarKeys = new KeyCode[]
    {
        KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3,
        KeyCode.Alpha4, KeyCode.Alpha5, KeyCode.Alpha6,
        KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9, KeyCode.Alpha0
    };
    
    [Header("Mobile Settings")]
    public bool isMobile = false;
    public float virtualJoystickDeadzone = 0.1f;
    
    private Vector2 movementInput = Vector2.zero;
    private Vector2 lookInput = Vector2.zero;
    private bool isJumpPressed = false;
    private bool isSprintPressed = false;
    private bool isCrouchPressed = false;
    private bool isInteractPressed = false;
    
    // أحداث الإدخال
    public delegate void InputAction();
    public event InputAction OnJump;
    public event InputAction OnInteract;
    public event InputAction OnSprintStart;
    public event InputAction OnSprintEnd;
    public event InputAction OnCrouchToggle;
    public event InputAction OnInventoryToggle;
    public event InputAction OnCraftingToggle;
    public event InputAction OnBuildingToggle;
    public event InputAction OnPauseToggle;
    
    // أحداث الهوتبار
    public delegate void HotbarAction(int slot);
    public event HotbarAction OnHotbarSelect;
    
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
        if (isMobile)
        {
            ProcessMobileInput();
        }
        else
        {
            ProcessDesktopInput();
        }
        
        ProcessCommonInput();
    }
    
    void ProcessDesktopInput()
    {
        // الحركة
        movementInput.x = Input.GetAxisRaw("Horizontal");
        movementInput.y = Input.GetAxisRaw("Vertical");
        
        // النظر
        lookInput.x = Input.GetAxis("Mouse X") * mouseSensitivity;
        lookInput.y = Input.GetAxis("Mouse Y") * mouseSensitivity * (invertMouseY ? 1f : -1f);
        
        // القفز
        if (Input.GetKeyDown(jumpKey))
        {
            isJumpPressed = true;
            OnJump?.Invoke();
        }
        
        // الجري
        bool sprinting = Input.GetKey(sprintKey);
        if (sprinting != isSprintPressed)
        {
            isSprintPressed = sprinting;
            if (isSprintPressed)
                OnSprintStart?.Invoke();
            else
                OnSprintEnd?.Invoke();
        }
        
        // الانحناء
        if (Input.GetKeyDown(crouchKey))
        {
            isCrouchPressed = !isCrouchPressed;
            OnCrouchToggle?.Invoke();
        }
        
        // التفاعل
        if (Input.GetKeyDown(interactKey))
        {
            isInteractPressed = true;
            OnInteract?.Invoke();
        }
        
        // المخزون
        if (Input.GetKeyDown(inventoryKey))
        {
            OnInventoryToggle?.Invoke();
        }
        
        // الصناعة
        if (Input.GetKeyDown(craftingKey))
        {
            OnCraftingToggle?.Invoke();
        }
        
        // البناء
        if (Input.GetKeyDown(buildingKey))
        {
            OnBuildingToggle?.Invoke();
        }
        
        // الإيقاف المؤقت
        if (Input.GetKeyDown(pauseKey))
        {
            OnPauseToggle?.Invoke();
        }
        
        // الهوتبار
        for (int i = 0; i < hotbarKeys.Length; i++)
        {
            if (Input.GetKeyDown(hotbarKeys[i]))
            {
                OnHotbarSelect?.Invoke(i);
            }
        }
        
        // إطلاق النار
        if (Input.GetMouseButtonDown(0))
        {
            OnPrimaryAction();
        }
        
        if (Input.GetMouseButtonDown(1))
        {
            OnSecondaryAction();
        }
        
        // التمرير
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            OnMouseScroll(scroll);
        }
    }
    
    void ProcessMobileInput()
    {
        // هنا يمكنك إضافة منطق الإدخال المحمول
        // مثل الجويستيك الافتراضي واللمس
    }
    
    void ProcessCommonInput()
    {
        // أي إدخالات مشتركة بين المنصات
    }
    
    void OnPrimaryAction()
    {
        // الهجوم أو استخدام العنصر
        if (CombatSystem.Instance != null)
        {
            // يمكنك تفعيل الهجوم هنا
        }
    }
    
    void OnSecondaryAction()
    {
        // التصويب أو الإجراء الثانوي
    }
    
    void OnMouseScroll(float scroll)
    {
        // تبديل العناصر في الهوتبار
        if (UIManager.Instance != null)
        {
            // UIManager.Instance.ScrollHotbar(scroll);
        }
    }
    
    // دوال الحصول على الإدخال
    public Vector2 GetMovementInput()
    {
        return movementInput.normalized;
    }
    
    public Vector2 GetLookInput()
    {
        return lookInput;
    }
    
    public bool IsJumpPressed()
    {
        bool pressed = isJumpPressed;
        isJumpPressed = false; // إعادة تعيين
        return pressed;
    }
    
    public bool IsInteractPressed()
    {
        bool pressed = isInteractPressed;
        isInteractPressed = false; // إعادة تعيين
        return pressed;
    }
    
    public bool IsSprinting()
    {
        return isSprintPressed;
    }
    
    public bool IsCrouching()
    {
        return isCrouchPressed;
    }
    
    public void SetMouseSensitivity(float sensitivity)
    {
        mouseSensitivity = Mathf.Clamp(sensitivity, 0.1f, 10f);
    }
    
    public void ToggleInvertMouse(bool invert)
    {
        invertMouseY = invert;
    }
    
    public void RebindKey(string action, KeyCode newKey)
    {
        switch (action)
        {
            case "Jump":
                jumpKey = newKey;
                break;
            case "Sprint":
                sprintKey = newKey;
                break;
            case "Crouch":
                crouchKey = newKey;
                break;
            case "Interact":
                interactKey = newKey;
                break;
            case "Inventory":
                inventoryKey = newKey;
                break;
        }
    }
    
    public KeyCode GetKeyForAction(string action)
    {
        switch (action)
        {
            case "Jump": return jumpKey;
            case "Sprint": return sprintKey;
            case "Crouch": return crouchKey;
            case "Interact": return interactKey;
            case "Inventory": return inventoryKey;
            case "Crafting": return craftingKey;
            case "Building": return buildingKey;
            case "Pause": return pauseKey;
            default: return KeyCode.None;
        }
    }
    
    public string GetKeyName(string action)
    {
        return GetKeyForAction(action).ToString();
    }
    
    public void SaveKeybinds()
    {
        PlayerPrefs.SetFloat("MouseSensitivity", mouseSensitivity);
        PlayerPrefs.SetInt("InvertMouse", invertMouseY ? 1 : 0);
        
        PlayerPrefs.SetInt("Key_Jump", (int)jumpKey);
        PlayerPrefs.SetInt("Key_Sprint", (int)sprintKey);
        PlayerPrefs.SetInt("Key_Crouch", (int)crouchKey);
        PlayerPrefs.SetInt("Key_Interact", (int)interactKey);
        PlayerPrefs.SetInt("Key_Inventory", (int)inventoryKey);
        
        PlayerPrefs.Save();
    }
    
    public void LoadKeybinds()
    {
        mouseSensitivity = PlayerPrefs.GetFloat("MouseSensitivity", 2f);
        invertMouseY = PlayerPrefs.GetInt("InvertMouse", 0) == 1;
        
        jumpKey = (KeyCode)PlayerPrefs.GetInt("Key_Jump", (int)KeyCode.Space);
        sprintKey = (KeyCode)PlayerPrefs.GetInt("Key_Sprint", (int)KeyCode.LeftShift);
        crouchKey = (KeyCode)PlayerPrefs.GetInt("Key_Crouch", (int)KeyCode.LeftControl);
        interactKey = (KeyCode)PlayerPrefs.GetInt("Key_Interact", (int)KeyCode.E);
        inventoryKey = (KeyCode)PlayerPrefs.GetInt("Key_Inventory", (int)KeyCode.I);
    }
}
