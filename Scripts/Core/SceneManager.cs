using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class SceneManager : MonoBehaviour
{
    public static SceneManager Instance;
    
    [Header("Scene Settings")]
    public string mainMenuScene = "MainMenu";
    public string gameScene = "GameWorld";
    public string loadingScene = "LoadingScene";
    
    [Header("Loading Screen")]
    public GameObject loadingScreen;
    public UnityEngine.UI.Slider loadingSlider;
    public UnityEngine.UI.Text loadingText;
    
    [Header("Scene Transitions")]
    public Animator sceneTransitionAnimator;
    public float transitionTime = 1f;
    
    private string targetScene;
    private bool isLoading = false;
    
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
    
    void Start()
    {
        if (loadingScreen != null)
            loadingScreen.SetActive(false);
    }
    
    public void LoadMainMenu()
    {
        LoadScene(mainMenuScene);
    }
    
    public void LoadGame()
    {
        LoadScene(gameScene);
    }
    
    public void LoadScene(string sceneName)
    {
        if (isLoading) return;
        
        targetScene = sceneName;
        StartCoroutine(LoadSceneAsync());
    }
    
    IEnumerator LoadSceneAsync()
    {
        isLoading = true;
        
        // بدء الانتقال
        if (sceneTransitionAnimator != null)
        {
            sceneTransitionAnimator.SetTrigger("Start");
            yield return new WaitForSeconds(transitionTime);
        }
        
        // تفعيل شاشة التحميل
        if (loadingScreen != null)
        {
            loadingScreen.SetActive(true);
        }
        
        // بدء تحميل المشهد
        AsyncOperation asyncLoad = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(targetScene);
        asyncLoad.allowSceneActivation = false;
        
        // تحديث شريط التقدم
        while (!asyncLoad.isDone)
        {
            float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
            
            if (loadingSlider != null)
                loadingSlider.value = progress;
            
            if (loadingText != null)
                loadingText.text = $"جاري التحميل... {(int)(progress * 100)}%";
            
            // عندما يكتمل التحميل بنسبة 90%، ننتظر الإذن بالتفعيل
            if (asyncLoad.progress >= 0.9f)
            {
                // هنا يمكنك إضافة شروط إضافية (مثل انتظار تحميل الأصول)
                asyncLoad.allowSceneActivation = true;
            }
            
            yield return null;
        }
        
        // إخفاء شاشة التحميل
        if (loadingScreen != null)
        {
            loadingScreen.SetActive(false);
        }
        
        // انتهاء الانتقال
        if (sceneTransitionAnimator != null)
        {
            sceneTransitionAnimator.SetTrigger("End");
            yield return new WaitForSeconds(transitionTime);
        }
        
        isLoading = false;
        
        // تهيئة المشهد الجديد
        InitializeNewScene();
    }
    
    void InitializeNewScene()
    {
        // تهيئة المشهد بناءً على اسم المشهد
        switch (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name)
        {
            case "MainMenu":
                InitializeMainMenu();
                break;
            case "GameWorld":
                InitializeGameWorld();
                break;
            case "BossArena":
                InitializeBossArena();
                break;
        }
    }
    
    void InitializeMainMenu()
    {
        // تهيئة القائمة الرئيسية
        AudioManager.Instance.PlayMusic(Resources.Load<AudioClip>("Audio/Music/main_menu"));
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        Time.timeScale = 1f;
    }
    
    void InitializeGameWorld()
    {
        // تهيئة عالم اللعبة
        AudioManager.Instance.PlayDynamicMusic(AudioManager.MusicIntensity.Calm);
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        Time.timeScale = 1f;
        
        // تهيئة اللاعب
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            // أي تهيئة إضافية للاعب
        }
    }
    
    void InitializeBossArena()
    {
        // تهيئة ساحة الزعيم
        AudioManager.Instance.PlayDynamicMusic(AudioManager.MusicIntensity.Boss);
    }
    
    public void ReloadCurrentScene()
    {
        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        LoadScene(currentScene);
    }
    
    public void QuitGame()
    {
        // حفظ قبل الخروج
        if (SaveSystem.Instance != null)
        {
            SaveSystem.Instance.SaveGame();
        }
        
        Application.Quit();
        
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
    
    public bool IsLoading()
    {
        return isLoading;
    }
    
    public string GetCurrentScene()
    {
        return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
    }
}
