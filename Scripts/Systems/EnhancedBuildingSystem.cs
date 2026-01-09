using UnityEngine;
using System.Collections.Generic;

public class EnhancedBuildingSystem : MonoBehaviour
{
    [Header("Building Settings")]
    public GameObject[] buildingBlocks;
    public LayerMask buildableLayer;
    public float gridSize = 1f;
    
    [Header("Audio Settings")]
    public AudioClip placeSound;
    public AudioClip destroySound;
    public AudioClip rotateSound;
    
    [Header("Visual Effects")]
    public ParticleSystem buildParticles;
    public ParticleSystem destroyParticles;
    public GameObject previewGhost;
    
    private GameObject currentBlock;
    private int currentBlockIndex = 0;
    private bool isBuilding = false;
    private List<GameObject> placedBlocks = new List<GameObject>();
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            ToggleBuildingMode();
        }
        
        if (isBuilding)
        {
            UpdateBuildingPreview();
            
            if (Input.GetMouseButtonDown(0))
            {
                PlaceBlock();
            }
            
            if (Input.GetMouseButtonDown(1))
            {
                DestroyBlock();
            }
            
            if (Input.GetKeyDown(KeyCode.R))
            {
                RotateBlock();
            }
            
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                ChangeBlockType(0);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                ChangeBlockType(1);
            }
        }
    }
    
    void ToggleBuildingMode()
    {
        isBuilding = !isBuilding;
        
        if (isBuilding)
        {
            EnterBuildingMode();
            AudioManager.Instance.PlayDynamicMusic(AudioManager.MusicIntensity.Building);
        }
        else
        {
            ExitBuildingMode();
            AudioManager.Instance.PlayDynamicMusic(AudioManager.MusicIntensity.Calm);
        }
    }
    
    void EnterBuildingMode()
    {
        if (buildingBlocks.Length > 0)
        {
            currentBlock = buildingBlocks[currentBlockIndex];
            CreatePreviewGhost();
        }
    }
    
    void ExitBuildingMode()
    {
        if (previewGhost != null)
        {
            Destroy(previewGhost);
        }
    }
    
    void CreatePreviewGhost()
    {
        if (previewGhost != null)
        {
            Destroy(previewGhost);
        }
        
        previewGhost = Instantiate(currentBlock);
        
        // جعل البريفيو شفاف
        Renderer[] renderers = previewGhost.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            Material mat = renderer.material;
            Color color = mat.color;
            color.a = 0.5f;
            mat.color = color;
        }
        
        // إزالة الكولايذر المؤقت
        Collider2D collider = previewGhost.GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.enabled = false;
        }
    }
    
    void UpdateBuildingPreview()
    {
        if (previewGhost == null) return;
        
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3 snappedPos = new Vector3(
            Mathf.Round(mousePos.x / gridSize) * gridSize,
            Mathf.Round(mousePos.y / gridSize) * gridSize,
            0f
        );
        
        previewGhost.transform.position = snappedPos;
        
        // التحقق مما إذا كان المكان متاح للبناء
        bool canBuild = CheckBuildPosition(snappedPos);
        
        // تغيير لون البريفيو بناءً على الإمكانية
        Renderer[] renderers = previewGhost.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            Material mat = renderer.material;
            mat.color = canBuild ? 
                new Color(0.2f, 1f, 0.2f, 0.5f) : 
                new Color(1f, 0.2f, 0.2f, 0.5f);
        }
    }
    
    bool CheckBuildPosition(Vector3 position)
    {
        Collider2D[] colliders = Physics2D.OverlapBoxAll(position, 
            new Vector2(gridSize * 0.8f, gridSize * 0.8f), 0f);
        
        foreach (Collider2D collider in colliders)
        {
            if (collider.gameObject != previewGhost && 
                collider.gameObject.CompareTag("Building"))
            {
                return false;
            }
        }
        
        return true;
    }
    
    void PlaceBlock()
    {
        if (previewGhost == null) return;
        
        Vector3 buildPos = previewGhost.transform.position;
        
        if (!CheckBuildPosition(buildPos)) return;
        
        // إنشاء البلوك الحقيقي
        GameObject newBlock = Instantiate(currentBlock, buildPos, 
            previewGhost.transform.rotation);
        newBlock.tag = "Building";
        
        // إضافة تأثيرات بناء
        if (buildParticles != null)
        {
            Instantiate(buildParticles, buildPos, Quaternion.identity);
        }
        
        // تشغيل صوت البناء
        if (placeSound != null)
        {
            AudioManager.Instance.PlaySFX(placeSound.name, buildPos);
            
            // نغمة بناء متفاوتة حسب نوع البلوك
            if (currentBlockIndex == 0) // حجر
                AudioManager.Instance.PlayEventTone("craft_success");
            else if (currentBlockIndex == 1) // خشب
                AudioManager.Instance.PlayEventTone("item_found");
        }
        
        // اهتزاز خفيف للكاميرا (إذا كان معتمد)
        CameraShake.Instance?.Shake(0.1f, 0.05f);
        
        placedBlocks.Add(newBlock);
        
        // إنشاء بريفيو جديد
        CreatePreviewGhost();
    }
    
    void DestroyBlock()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Collider2D hit = Physics2D.OverlapPoint(mousePos);
        
        if (hit != null && hit.CompareTag("Building"))
        {
            // تأثيرات التدمير
            if (destroyParticles != null)
            {
                Instantiate(destroyParticles, hit.transform.position, 
                    Quaternion.identity);
            }
            
            // تشغيل صوت التدمير
            if (destroySound != null)
            {
                AudioManager.Instance.PlaySFX(destroySound.name, 
                    hit.transform.position);
            }
            
            // إزالة من القائمة
            placedBlocks.Remove(hit.gameObject);
            
            // تدمير الكائن
            Destroy(hit.gameObject);
        }
    }
    
    void RotateBlock()
    {
        if (previewGhost == null) return;
        
        previewGhost.transform.Rotate(0f, 0f, 90f);
        
        // تشغيل صوت الدوران
        if (rotateSound != null)
        {
            AudioManager.Instance.PlaySFX(rotateSound.name, 
                previewGhost.transform.position);
        }
    }
    
    void ChangeBlockType(int index)
    {
        if (index >= 0 && index < buildingBlocks.Length)
        {
            currentBlockIndex = index;
            currentBlock = buildingBlocks[index];
            CreatePreviewGhost();
            
            // نغمة تغيير البلوك
            AudioManager.Instance.PlayEventTone("warning");
        }
    }
    
    public void SaveCastle(string castleName)
    {
        CastleData castleData = new CastleData();
        castleData.castleName = castleName;
        
        foreach (GameObject block in placedBlocks)
        {
            BlockData blockData = new BlockData
            {
                position = block.transform.position,
                rotation = block.transform.rotation,
                blockType = block.name.Replace("(Clone)", "")
            };
            castleData.blocks.Add(blockData);
        }
        
        string json = JsonUtility.ToJson(castleData);
        PlayerPrefs.SetString("Castle_" + castleName, json);
        PlayerPrefs.Save();
        
        Debug.Log($"Castle '{castleName}' saved with {castleData.blocks.Count} blocks");
    }
    
    public void LoadCastle(string castleName)
    {
        if (PlayerPrefs.HasKey("Castle_" + castleName))
        {
            string json = PlayerPrefs.GetString("Castle_" + castleName);
            CastleData castleData = JsonUtility.FromJson<CastleData>(json);
            
            // تنظيف القلعة الحالية
            foreach (GameObject block in placedBlocks)
            {
                Destroy(block);
            }
            placedBlocks.Clear();
            
            // بناء القلعة من البيانات
            foreach (BlockData blockData in castleData.blocks)
            {
                GameObject blockPrefab = System.Array.Find(buildingBlocks, 
                    b => b.name == blockData.blockType);
                
                if (blockPrefab != null)
                {
                    GameObject newBlock = Instantiate(blockPrefab, 
                        blockData.position, blockData.rotation);
                    newBlock.tag = "Building";
                    placedBlocks.Add(newBlock);
                }
            }
            
            Debug.Log($"Castle '{castleName}' loaded with {castleData.blocks.Count} blocks");
        }
    }
    
    [System.Serializable]
    public class CastleData
    {
        public string castleName;
        public List<BlockData> blocks = new List<BlockData>();
    }
    
    [System.Serializable]
    public class BlockData
    {
        public Vector3 position;
        public Quaternion rotation;
        public string blockType;
    }
}
