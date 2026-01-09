using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class WorldGenerator : MonoBehaviour
{
    public static WorldGenerator Instance;
    
    [Header("World Settings")]
    public int worldSeed = 0;
    public Vector2 worldSize = new Vector2(100, 100);
    public float tileSize = 1f;
    public int chunkSize = 16;
    
    [Header("Terrain Settings")]
    public float noiseScale = 0.1f;
    public float terrainHeight = 10f;
    public int octaves = 4;
    public float persistence = 0.5f;
    public float lacunarity = 2f;
    
    [Header("Biomes")]
    public List<Biome> biomes = new List<Biome>();
    public AnimationCurve biomeBlendCurve;
    
    [Header("Resources")]
    public List<ResourceSpawn> resourceSpawns = new List<ResourceSpawn>();
    public float resourceDensity = 0.01f;
    
    [Header("Structures")]
    public List<StructureSpawn> structureSpawns = new List<StructureSpawn>();
    public float structureChance = 0.05f;
    
    [Header("Performance")]
    public bool generateOnStart = true;
    public bool useChunkLoading = true;
    public float chunkLoadDistance = 50f;
    
    [System.Serializable]
    public class Biome
    {
        public string biomeName;
        public Color biomeColor;
        public float heightMin;
        public float heightMax;
        public float temperatureMin;
        public float temperatureMax;
        public float moistureMin;
        public float moistureMax;
        
        [Header("Terrain")]
        public GameObject[] groundPrefabs;
        public GameObject[] treePrefabs;
        public GameObject[] rockPrefabs;
        public GameObject[] foliagePrefabs;
        
        [Header("Resources")]
        public string[] availableResources;
        
        public bool IsInBiome(float height, float temperature, float moisture)
        {
            return height >= heightMin && height <= heightMax &&
                   temperature >= temperatureMin && temperature <= temperatureMax &&
                   moisture >= moistureMin && moisture <= moistureMax;
        }
    }
    
    [System.Serializable]
    public class ResourceSpawn
    {
        public string resourceId;
        public GameObject resourcePrefab;
        public float minHeight = 0f;
        public float maxHeight = 1f;
        public float spawnChance = 0.1f;
        public int minGroupSize = 1;
        public int maxGroupSize = 5;
        public float groupRadius = 2f;
    }
    
    [System.Serializable]
    public class StructureSpawn
    {
        public string structureId;
        public GameObject structurePrefab;
        public float minHeight = 0.3f;
        public float maxHeight = 0.7f;
        public float spawnChance = 0.01f;
        public Vector2Int size = Vector2Int.one;
    }
    
    private Dictionary<Vector2Int, Chunk> loadedChunks = new Dictionary<Vector2Int, Chunk>();
    private Vector2Int playerChunkCoord;
    private Transform playerTransform;
    
    [System.Serializable]
    public class Chunk
    {
        public Vector2Int coord;
        public GameObject chunkObject;
        public List<GameObject> terrainTiles = new List<GameObject>();
        public List<GameObject> resources = new List<GameObject>();
        public List<GameObject> structures = new List<GameObject>();
        
        public bool IsActive { get; private set; }
        
        public void SetActive(bool active)
        {
            IsActive = active;
            if (chunkObject != null)
                chunkObject.SetActive(active);
        }
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
        
        // إعداد البذور العشوائية
        if (worldSeed == 0)
        {
            worldSeed = Random.Range(1, 999999);
        }
        Random.InitState(worldSeed);
        
        // تعريف البيومات الأساسية إذا كانت فارغة
        if (biomes.Count == 0)
        {
            InitializeDefaultBiomes();
        }
        
        // تعريف الموارد الأساسية
        if (resourceSpawns.Count == 0)
        {
            InitializeDefaultResources();
        }
    }
    
    void Start()
    {
        if (generateOnStart)
        {
            FindPlayer();
            GenerateInitialWorld();
        }
    }
    
    void Update()
    {
        if (useChunkLoading && playerTransform != null)
        {
            UpdateChunkLoading();
        }
    }
    
    void InitializeDefaultBiomes()
    {
        // بيوم الغابة
        Biome forest = new Biome
        {
            biomeName = "Forest",
            biomeColor = new Color(0.1f, 0.6f, 0.1f),
            heightMin = 0.3f,
            heightMax = 0.7f,
            temperatureMin = 0.4f,
            temperatureMax = 0.8f,
            moistureMin = 0.5f,
            moistureMax = 1f,
            availableResources = new string[] { "wood", "stone", "berries" }
        };
        biomes.Add(forest);
        
        // بيوم الجبل
        Biome mountain = new Biome
        {
            biomeName = "Mountain",
            biomeColor = new Color(0.5f, 0.5f, 0.5f),
            heightMin = 0.7f,
            heightMax = 1f,
            temperatureMin = 0f,
            temperatureMax = 0.6f,
            moistureMin = 0f,
            moistureMax = 0.5f,
            availableResources = new string[] { "stone", "iron", "coal" }
        };
        biomes.Add(mountain);
        
        // بيوم السهل
        Biome plains = new Biome
        {
            biomeName = "Plains",
            biomeColor = new Color(0.4f, 0.7f, 0.3f),
            heightMin = 0.1f,
            heightMax = 0.4f,
            temperatureMin = 0.5f,
            temperatureMax = 0.9f,
            moistureMin = 0.2f,
            moistureMax = 0.6f,
            availableResources = new string[] { "wood", "stone", "fiber" }
        };
        biomes.Add(plains);
        
        // بيوم الصحراء
        Biome desert = new Biome
        {
            biomeName = "Desert",
            biomeColor = new Color(0.9f, 0.8f, 0.4f),
            heightMin = 0f,
            heightMax = 0.3f,
            temperatureMin = 0.7f,
            temperatureMax = 1f,
            moistureMin = 0f,
            moistureMax = 0.2f,
            availableResources = new string[] { "sand", "cactus" }
        };
        biomes.Add(desert);
    }
    
    void InitializeDefaultResources()
    {
        // خشب
        ResourceSpawn woodSpawn = new ResourceSpawn
        {
            resourceId = "wood",
            resourcePrefab = Resources.Load<GameObject>("Prefabs/Resources/Wood"),
            minHeight = 0.3f,
            maxHeight = 0.8f,
            spawnChance = 0.05f,
            minGroupSize = 3,
            maxGroupSize = 8,
            groupRadius = 3f
        };
        resourceSpawns.Add(woodSpawn);
        
        // حجر
        ResourceSpawn stoneSpawn = new ResourceSpawn
        {
            resourceId = "stone",
            resourcePrefab = Resources.Load<GameObject>("Prefabs/Resources/Stone"),
            minHeight = 0.1f,
            maxHeight = 1f,
            spawnChance = 0.03f,
            minGroupSize = 1,
            maxGroupSize = 5,
            groupRadius = 2f
        };
        resourceSpawns.Add(stoneSpawn);
        
        // حديد
        ResourceSpawn ironSpawn = new ResourceSpawn
        {
            resourceId = "iron",
            resourcePrefab = Resources.Load<GameObject>("Prefabs/Resources/Iron"),
            minHeight = 0.5f,
            maxHeight = 1f,
            spawnChance = 0.01f,
            minGroupSize = 1,
            maxGroupSize = 3,
            groupRadius = 1.5f
        };
        resourceSpawns.Add(ironSpawn);
    }
    
    void FindPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
        else
        {
            Debug.LogWarning("لم يتم العثور على اللاعب. تأكد من وجود GameObject مع tag 'Player'");
        }
    }
    
    void GenerateInitialWorld()
    {
        if (playerTransform == null) return;
        
        Vector3 playerPos = playerTransform.position;
        playerChunkCoord = WorldToChunkCoord(playerPos);
        
        // توليد الكنكات حول اللاعب
        int loadRadius = Mathf.CeilToInt(chunkLoadDistance / chunkSize);
        
        for (int x = -loadRadius; x <= loadRadius; x++)
        {
            for (int y = -loadRadius; y <= loadRadius; y++)
            {
                Vector2Int chunkCoord = new Vector2Int(
                    playerChunkCoord.x + x,
                    playerChunkCoord.y + y
                );
                
                float distance = Vector2.Distance(
                    new Vector2(chunkCoord.x, chunkCoord.y),
                    new Vector2(playerChunkCoord.x, playerChunkCoord.y)
                );
                
                if (distance <= loadRadius)
                {
                    GenerateChunk(chunkCoord);
                }
            }
        }
    }
    
    void UpdateChunkLoading()
    {
        Vector3 playerPos = playerTransform.position;
        Vector2Int newPlayerChunkCoord = WorldToChunkCoord(playerPos);
        
        if (newPlayerChunkCoord != playerChunkCoord)
        {
            playerChunkCoord = newPlayerChunkCoord;
            
            // تحديث الكنكات
            UpdateActiveChunks();
        }
    }
    
    void UpdateActiveChunks()
    {
        List<Vector2Int> chunksToRemove = new List<Vector2Int>();
        
        // تحديد الكنكات البعيدة عن اللاعب
        foreach (var chunk in loadedChunks)
        {
            float distance = Vector2.Distance(
                new Vector2(chunk.Key.x, chunk.Key.y),
                new Vector2(playerChunkCoord.x, playerChunkCoord.y)
            ) * chunkSize;
            
            if (distance > chunkLoadDistance)
            {
                chunksToRemove.Add(chunk.Key);
            }
            else
            {
                chunk.Value.SetActive(true);
            }
        }
        
        // إزالة الكنكات البعيدة
        foreach (Vector2Int chunkCoord in chunksToRemove)
        {
            UnloadChunk(chunkCoord);
        }
        
        // توليد الكنكات الجديدة حول اللاعب
        int loadRadius = Mathf.CeilToInt(chunkLoadDistance / chunkSize);
        
        for (int x = -loadRadius; x <= loadRadius; x++)
        {
            for (int y = -loadRadius; y <= loadRadius; y++)
            {
                Vector2Int chunkCoord = new Vector2Int(
                    playerChunkCoord.x + x,
                    playerChunkCoord.y + y
                );
                
                if (!loadedChunks.ContainsKey(chunkCoord))
                {
                    float distance = Vector2.Distance(
                        new Vector2(chunkCoord.x, chunkCoord.y),
                        new Vector2(playerChunkCoord.x, playerChunkCoord.y)
                    ) * chunkSize;
                    
                    if (distance <= chunkLoadDistance)
                    {
                        GenerateChunk(chunkCoord);
                    }
                }
            }
        }
    }
    
    void GenerateChunk(Vector2Int chunkCoord)
    {
        if (loadedChunks.ContainsKey(chunkCoord)) return;
        
        // إنشاء كائن الكنك
        GameObject chunkObj = new GameObject($"Chunk_{chunkCoord.x}_{chunkCoord.y}");
        chunkObj.transform.position = new Vector3(
            chunkCoord.x * chunkSize,
            0f,
            chunkCoord.y * chunkSize
        );
        chunkObj.transform.SetParent(transform);
        
        Chunk chunk = new Chunk
        {
            coord = chunkCoord,
            chunkObject = chunkObj
        };
        
        // توليد التضاريس
        GenerateTerrain(chunk);
        
        // توليد الموارد
        GenerateResources(chunk);
        
        // توليد الهياكل
        GenerateStructures(chunk);
        
        loadedChunks.Add(chunkCoord, chunk);
    }
    
    void GenerateTerrain(Chunk chunk)
    {
        Vector3 chunkWorldPos = new Vector3(
            chunk.coord.x * chunkSize,
            0f,
            chunk.coord.y * chunkSize
        );
        
        for (int x = 0; x < chunkSize; x++)
        {
            for (int z = 0; z < chunkSize; z++)
            {
                Vector3 tileWorldPos = chunkWorldPos + new Vector3(x, 0f, z);
                
                // حساب الضوضاء للتضاريس
                float height = CalculateHeight(tileWorldPos);
                float temperature = CalculateTemperature(tileWorldPos);
                float moisture = CalculateMoisture(tileWorldPos);
                
                // تحديد البيوم المناسب
                Biome biome = GetBiomeAt(height, temperature, moisture);
                
                if (biome != null && biome.groundPrefabs.Length > 0)
                {
                    // إنشاء الأرضية
                    GameObject groundPrefab = biome.groundPrefabs[
                        Random.Range(0, biome.groundPrefabs.Length)
                    ];
                    
                    if (groundPrefab != null)
                    {
                        Vector3 spawnPos = tileWorldPos;
                        spawnPos.y = height * terrainHeight;
                        
                        GameObject tile = Instantiate(
                            groundPrefab,
                            spawnPos,
                            Quaternion.identity,
                            chunk.chunkObject.transform
                        );
                        
                        chunk.terrainTiles.Add(tile);
                        
                        // إضافة الأشجار بشكل عشوائي
                        if (biome.treePrefabs.Length > 0 && Random.value < 0.02f)
                        {
                            GameObject treePrefab = biome.treePrefabs[
                                Random.Range(0, biome.treePrefabs.Length)
                            ];
                            
                            if (treePrefab != null)
                            {
                                Vector3 treePos = spawnPos + new Vector3(
                                    Random.Range(-0.3f, 0.3f),
                                    0f,
                                    Random.Range(-0.3f, 0.3f)
                                );
                                
                                GameObject tree = Instantiate(
                                    treePrefab,
                                    treePos,
                                    Quaternion.Euler(0f, Random.Range(0f, 360f), 0f),
                                    chunk.chunkObject.transform
                                );
                                
                                chunk.terrainTiles.Add(tree);
                            }
                        }
                        
                        // إضافة الصخور بشكل عشوائي
                        if (biome.rockPrefabs.Length > 0 && Random.value < 0.01f)
                        {
                            GameObject rockPrefab = biome.rockPrefabs[
                                Random.Range(0, biome.rockPrefabs.Length)
                            ];
                            
                            if (rockPrefab != null)
                            {
                                Vector3 rockPos = spawnPos + new Vector3(
                                    Random.Range(-0.4f, 0.4f),
                                    0f,
                                    Random.Range(-0.4f, 0.4f)
                                );
                                
                                GameObject rock = Instantiate(
                                    rockPrefab,
                                    rockPos,
                                    Quaternion.Euler(
                                        Random.Range(0f, 10f),
                                        Random.Range(0f, 360f),
                                        Random.Range(0f, 10f)
                                    ),
                                    chunk.chunkObject.transform
                                );
                                
                                chunk.terrainTiles.Add(rock);
                            }
                        }
                    }
                }
            }
        }
    }
    
    void GenerateResources(Chunk chunk)
    {
        Vector3 chunkWorldPos = new Vector3(
            chunk.coord.x * chunkSize,
            0f,
            chunk.coord.y * chunkSize
        );
        
        foreach (ResourceSpawn resourceSpawn in resourceSpawns)
        {
            if (resourceSpawn.resourcePrefab == null) continue;
            
            // عدد المحاولات لتوليد الموارد
            int attempts = Mathf.RoundToInt(chunkSize * chunkSize * resourceDensity);
            
            for (int i = 0; i < attempts; i++)
            {
                if (Random.value <= resourceSpawn.spawnChance)
                {
                    Vector3 spawnPos = chunkWorldPos + new Vector3(
                        Random.Range(0f, chunkSize),
                        0f,
                        Random.Range(0f, chunkSize)
                    );
                    
                    // حساب الارتفاع عند هذا الموضع
                    float height = CalculateHeight(spawnPos);
                    
                    // التحقق من نطاق الارتفاع
                    if (height >= resourceSpawn.minHeight && height <= resourceSpawn.maxHeight)
                    {
                        // توليد مجموعة من الموارد
                        int groupSize = Random.Range(
                            resourceSpawn.minGroupSize,
                            resourceSpawn.maxGroupSize + 1
                        );
                        
                        for (int j = 0; j < groupSize; j++)
                        {
                            Vector3 resourcePos = spawnPos + new Vector3(
                                Random.Range(-resourceSpawn.groupRadius, resourceSpawn.groupRadius),
                                0f,
                                Random.Range(-resourceSpawn.groupRadius, resourceSpawn.groupRadius)
                            );
                            
                            // إعادة حساب الارتفاع للموضع الجديد
                            resourcePos.y = CalculateHeight(resourcePos) * terrainHeight;
                            
                            GameObject resource = Instantiate(
                                resourceSpawn.resourcePrefab,
                                resourcePos,
                                Quaternion.Euler(0f, Random.Range(0f, 360f), 0f),
                                chunk.chunkObject.transform
                            );
                            
                            // تعيين بيانات المورد
                            ResourceNode resourceNode = resource.GetComponent<ResourceNode>();
                            if (resourceNode != null)
                            {
                                resourceNode.resourceType = resourceSpawn.resourceId;
                                resourceNode.resourceAmount = Random.Range(3, 10);
                            }
                            
                            chunk.resources.Add(resource);
                        }
                    }
                }
            }
        }
    }
    
    void GenerateStructures(Chunk chunk)
    {
        Vector3 chunkWorldPos = new Vector3(
            chunk.coord.x * chunkSize,
            0f,
            chunk.coord.y * chunkSize
        );
        
        foreach (StructureSpawn structureSpawn in structureSpawns)
        {
            if (structureSpawn.structurePrefab == null) continue;
            
            if (Random.value <= structureSpawn.spawnChance)
            {
                Vector3 spawnPos = chunkWorldPos + new Vector3(
                    Random.Range(0f, chunkSize - structureSpawn.size.x),
                    0f,
                    Random.Range(0f, chunkSize - structureSpawn.size.y)
                );
                
                // حساب الارتفاع
                float height = CalculateHeight(spawnPos);
                
                if (height >= structureSpawn.minHeight && height <= structureSpawn.maxHeight)
                {
                    spawnPos.y = height * terrainHeight;
                    
                    // التحقق من عدم وجود عوائق
                    if (!IsPositionOccupied(spawnPos, structureSpawn.size))
                    {
                        GameObject structure = Instantiate(
                            structureSpawn.structurePrefab,
                            spawnPos,
                            Quaternion.Euler(0f, Random.Range(0f, 360f), 0f),
                            chunk.chunkObject.transform
                        );
                        
                        chunk.structures.Add(structure);
                        
                        // تعيين بيانات الهيكل
                        structure.name = $"{structureSpawn.structureId}_{chunk.coord.x}_{chunk.coord.y}";
                    }
                }
            }
        }
    }
    
    float CalculateHeight(Vector3 position)
    {
        float xCoord = (position.x / worldSize.x) * noiseScale + worldSeed;
        float zCoord = (position.z / worldSize.y) * noiseScale + worldSeed;
        
        float value = 0f;
        float frequency = 1f;
        float amplitude = 1f;
        float maxValue = 0f;
        
        for (int i = 0; i < octaves; i++)
        {
            float noise = Mathf.PerlinNoise(xCoord * frequency, zCoord * frequency) * 2f - 1f;
            value += noise * amplitude;
            
            maxValue += amplitude;
            amplitude *= persistence;
            frequency *= lacunarity;
        }
        
        value = value / maxValue;
        return (value + 1f) * 0.5f; // تحويل النطاق إلى [0, 1]
    }
    
    float CalculateTemperature(Vector3 position)
    {
        // درجة الحرارة تعتمد على الارتفاع والموقع
        float height = CalculateHeight(position);
        float latitude = Mathf.Abs(position.z) / worldSize.y;
        
        // أكثر برودة في الأعلى وفي القطبين
        float temp = 1f - (height * 0.3f) - (latitude * 0.2f);
        temp += Random.Range(-0.1f, 0.1f); // بعض التباين
        
        return Mathf.Clamp01(temp);
    }
    
    float CalculateMoisture(Vector3 position)
    {
        // الرطوبة تعتمد على درجة الحرارة والارتفاع
        float temp = CalculateTemperature(position);
        float height = CalculateHeight(position);
        
        // أقل رطوبة في المناطق المرتفعة والدافئة
        float moisture = 1f - (temp * 0.5f) - (height * 0.3f);
        moisture += Random.Range(-0.15f, 0.15f);
        
        return Mathf.Clamp01(moisture);
    }
    
    Biome GetBiomeAt(float height, float temperature, float moisture)
    {
        foreach (Biome biome in biomes)
        {
            if (biome.IsInBiome(height, temperature, moisture))
            {
                return biome;
            }
        }
        
        // العودة إلى البيوم الافتراضي إذا لم يتم العثور على تطابق
        return biomes.Count > 0 ? biomes[0] : null;
    }
    
    bool IsPositionOccupied(Vector3 position, Vector2Int size)
    {
        // التحقق من التضاريس
        Collider[] colliders = Physics.OverlapBox(
            position + Vector3.up * 2f,
            new Vector3(size.x * 0.5f, 5f, size.y * 0.5f)
        );
        
        foreach (Collider collider in colliders)
        {
            if (collider.CompareTag("Terrain") || collider.CompareTag("Building") || collider.CompareTag("Resource"))
            {
                return true;
            }
        }
        
        return false;
    }
    
    void UnloadChunk(Vector2Int chunkCoord)
    {
        if (loadedChunks.TryGetValue(chunkCoord, out Chunk chunk))
        {
            // حفظ الكنك إذا لزم الأمر
            // ...
            
            // تدمير الكائن
            if (chunk.chunkObject != null)
            {
                Destroy(chunk.chunkObject);
            }
            
            loadedChunks.Remove(chunkCoord);
        }
    }
    
    Vector2Int WorldToChunkCoord(Vector3 worldPos)
    {
        return new Vector2Int(
            Mathf.FloorToInt(worldPos.x / chunkSize),
            Mathf.FloorToInt(worldPos.z / chunkSize)
        );
    }
    
    public Vector3 GetSpawnPoint()
    {
        // العثور على نقطة ظهور مناسبة (منخفضة الارتفاع، بيوم آمن)
        Vector3 spawnPos = Vector3.zero;
        float bestScore = float.MaxValue;
        
        for (int i = 0; i < 100; i++)
        {
            Vector3 testPos = new Vector3(
                Random.Range(-worldSize.x * 0.1f, worldSize.x * 0.1f),
                0f,
                Random.Range(-worldSize.y * 0.1f, worldSize.y * 0.1f)
            );
            
            float height = CalculateHeight(testPos);
            float temperature = CalculateTemperature(testPos);
            float moisture = CalculateMoisture(testPos);
            
            // تفضيل المناطق المنخفضة والمعتدلة
            float score = height * 2f + Mathf.Abs(temperature - 0.5f) + Mathf.Abs(moisture - 0.5f);
            
            if (score < bestScore)
            {
                bestScore = score;
                spawnPos = testPos;
                spawnPos.y = height * terrainHeight;
            }
        }
        
        return spawnPos;
    }
    
    public void RegenerateWorld()
    {
        // حذف العالم الحالي
        foreach (var chunk in loadedChunks.Values)
        {
            if (chunk.chunkObject != null)
            {
                Destroy(chunk.chunkObject);
            }
        }
        
        loadedChunks.Clear();
        
        // تغيير البذرة
        worldSeed = Random.Range(1, 999999);
        Random.InitState(worldSeed);
        
        // إعادة التوليد
        if (playerTransform != null)
        {
            GenerateInitialWorld();
        }
    }
    
    public Biome GetCurrentBiome(Vector3 position)
    {
        float height = CalculateHeight(position);
        float temperature = CalculateTemperature(position);
        float moisture = CalculateMoisture(position);
        
        return GetBiomeAt(height, temperature, moisture);
    }
    
    void OnDrawGizmosSelected()
    {
        // عرض حدود العالم
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(
            Vector3.zero + new Vector3(0f, terrainHeight * 0.5f, 0f),
            new Vector3(worldSize.x, terrainHeight, worldSize.y)
        );
        
        // عرض الكنكات المحملة
        Gizmos.color = Color.blue;
        foreach (var chunk in loadedChunks.Values)
        {
            Vector3 center = new Vector3(
                chunk.coord.x * chunkSize + chunkSize * 0.5f,
                terrainHeight * 0.5f,
                chunk.coord.y * chunkSize + chunkSize * 0.5f
            );
            
            Gizmos.DrawWireCube(
                center,
                new Vector3(chunkSize, terrainHeight, chunkSize)
            );
        }
    }
}

// مكون عقدة المورد (يجب وضعه في ملف منفصل)
public class ResourceNode : MonoBehaviour
{
    public string resourceType = "wood";
    public int resourceAmount = 10;
    public float respawnTime = 300f; // 5 دقائق
    public GameObject depletedEffect;
    public AudioClip harvestSound;
    
    private int currentAmount;
    private float respawnTimer = 0f;
    private bool isDepleted = false;
    private MeshRenderer meshRenderer;
    private Collider collider;
    
    void Start()
    {
        currentAmount = resourceAmount;
        meshRenderer = GetComponent<MeshRenderer>();
        collider = GetComponent<Collider>();
    }
    
    void Update()
    {
        if (isDepleted)
        {
            respawnTimer -= Time.deltaTime;
            
            if (respawnTimer <= 0f)
            {
                Respawn();
            }
        }
    }
    
    public int Harvest(int amount)
    {
        if (isDepleted) return 0;
        
        int harvested = Mathf.Min(amount, currentAmount);
        currentAmount -= harvested;
        
        // تشغيل صوت الحصاد
        if (harvestSound != null)
        {
            AudioManager.Instance.PlaySFX(harvestSound.name, transform.position);
        }
        
        // جسيمات الحصاد
        if (harvested > 0)
        {
            SpawnHarvestParticles();
            
            // تحديث مهمة جمع الموارد
            if (QuestSystem.Instance != null)
            {
                QuestSystem.Instance.UpdateCollectObjective(resourceType, harvested);
            }
        }
        
        if (currentAmount <= 0)
        {
            Deplete();
        }
        
        return harvested;
    }
    
    void Deplete()
    {
        isDepleted = true;
        respawnTimer = respawnTime;
        
        // إخفاء المورد
        if (meshRenderer != null)
            meshRenderer.enabled = false;
        
        if (collider != null)
            collider.enabled = false;
        
        // تأثير النضوب
        if (depletedEffect != null)
        {
            Instantiate(depletedEffect, transform.position, Quaternion.identity);
        }
        
        Debug.Log($"تم نضوب مورد {resourceType}");
    }
    
    void Respawn()
    {
        isDepleted = false;
        currentAmount = resourceAmount;
        
        // إعادة إظهار المورد
        if (meshRenderer != null)
            meshRenderer.enabled = true;
        
        if (collider != null)
            collider.enabled = true;
        
        Debug.Log($"تم إعادة نمو مورد {resourceType}");
    }
    
    void SpawnHarvestParticles()
    {
        GameObject particlePrefab = Resources.Load<GameObject>("Particles/HarvestParticles");
        if (particlePrefab != null)
        {
            GameObject particles = Instantiate(
                particlePrefab,
                transform.position + Vector3.up * 0.5f,
                Quaternion.identity
            );
            
            ParticleSystem particleSystem = particles.GetComponent<ParticleSystem>();
            if (particleSystem != null)
            {
                var main = particleSystem.main;
                
                // تغيير اللون بناءً على نوع المورد
                switch (resourceType)
                {
                    case "wood":
                        main.startColor = new Color(0.5f, 0.3f, 0.1f);
                        break;
                    case "stone":
                        main.startColor = Color.gray;
                        break;
                    case "iron":
                        main.startColor = new Color(0.7f, 0.5f, 0.3f);
                        break;
                }
            }
            
            Destroy(particles, 2f);
        }
    }
    
    public bool IsDepleted()
    {
        return isDepleted;
    }
    
    public int GetCurrentAmount()
    {
        return currentAmount;
    }
    
    public float GetRespawnProgress()
    {
        if (!isDepleted) return 1f;
        return 1f - (respawnTimer / respawnTime);
    }
}
