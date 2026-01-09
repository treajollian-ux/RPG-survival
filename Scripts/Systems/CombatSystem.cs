using UnityEngine;
using System.Collections;

public class CombatSystem : MonoBehaviour
{
    [Header("Combat Settings")]
    public float baseDamage = 10f;
    public float attackRange = 2f;
    public float attackCooldown = 0.5f;
    public LayerMask enemyLayer;
    
    [Header("Weapon Settings")]
    public Weapon currentWeapon;
    public Transform weaponHolder;
    public GameObject weaponModel;
    
    [Header("Audio")]
    public AudioClip attackSound;
    public AudioClip hitSound;
    public AudioClip missSound;
    public AudioClip critSound;
    
    [Header("Visual Effects")]
    public ParticleSystem attackParticles;
    public ParticleSystem hitParticles;
    public GameObject hitMarkerPrefab;
    
    [System.Serializable]
    public class Weapon
    {
        public string weaponId;
        public string weaponName;
        public WeaponType type;
        public float damage;
        public float range;
        public float speed;
        public float critChance;
        public AudioClip swingSound;
        public GameObject weaponPrefab;
    }
    
    public enum WeaponType
    {
        Sword,
        Axe,
        Spear,
        Bow,
        Hammer,
        Magic
    }
    
    private bool canAttack = true;
    private Animator animator;
    private Camera mainCamera;
    
    void Start()
    {
        animator = GetComponent<Animator>();
        mainCamera = Camera.main;
        
        // تحميل السلاح الافتراضي إذا لم يكن هناك سلاح
        if (currentWeapon == null)
        {
            LoadDefaultWeapon();
        }
    }
    
    void Update()
    {
        HandleCombatInput();
    }
    
    void HandleCombatInput()
    {
        if (Input.GetMouseButtonDown(0) && canAttack) // زر الفأرة الأيسر
        {
            Attack();
        }
        
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SwitchWeapon(0);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SwitchWeapon(1);
        }
        else if (Input.GetKeyDown(KeyCode.R))
        {
            PerformSpecialAttack();
        }
    }
    
    void LoadDefaultWeapon()
    {
        currentWeapon = new Weapon
        {
            weaponId = "wooden_sword",
            weaponName = "سيف خشبي",
            type = WeaponType.Sword,
            damage = 15f,
            range = 2.5f,
            speed = 1f,
            critChance = 0.1f
        };
        
        EquipWeapon(currentWeapon);
    }
    
    void Attack()
    {
        if (!canAttack) return;
        
        StartCoroutine(AttackRoutine());
    }
    
    IEnumerator AttackRoutine()
    {
        canAttack = false;
        
        // تشغيل صوت الهجوم
        AudioClip attackClip = currentWeapon?.swingSound ?? attackSound;
        if (attackClip != null)
        {
            AudioManager.Instance.PlaySFX(attackClip.name, transform.position, 0.1f);
        }
        
        // تشغيل الرسوم المتحركة
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }
        
        // جسيمات الهجوم
        if (attackParticles != null)
        {
            attackParticles.Play();
        }
        
        // الانتظار لبداية الهجوم
        yield return new WaitForSeconds(0.2f);
        
        // التحقق من الضرب
        PerformAttackCheck();
        
        // وقت الانتظار بين الهجمات
        float cooldown = currentWeapon != null ? attackCooldown / currentWeapon.speed : attackCooldown;
        yield return new WaitForSeconds(cooldown);
        
        canAttack = true;
    }
    
    void PerformAttackCheck()
    {
        Vector3 attackDirection = transform.forward;
        Vector3 attackStart = transform.position + Vector3.up * 1f; // ارتفاع الصدر
        
        // رسم خط الهجوم للتشخيص
        Debug.DrawRay(attackStart, attackDirection * attackRange, Color.red, 1f);
        
        // إجراء الـ Raycast
        RaycastHit[] hits = Physics.SphereCastAll(
            attackStart,
            0.5f,
            attackDirection,
            attackRange,
            enemyLayer
        );
        
        bool hitEnemy = false;
        
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.CompareTag("Enemy"))
            {
                // حساب الضرر
                float damage = CalculateDamage();
                
                // تطبيق الضرر على العدو
                EnemyHealth enemyHealth = hit.collider.GetComponent<EnemyHealth>();
                if (enemyHealth != null)
                {
                    enemyHealth.TakeDamage(damage);
                    hitEnemy = true;
                    
                    // عرض ضرر النص
                    ShowDamageNumber(hit.point, damage);
                    
                    // تشغيل صوت الضرب
                    if (hitSound != null)
                    {
                        AudioManager.Instance.PlaySFX(hitSound.name, hit.point);
                    }
                    
                    // جسيمات الضرب
                    if (hitParticles != null)
                    {
                        ParticleSystem particles = Instantiate(hitParticles, hit.point, Quaternion.identity);
                        Destroy(particles.gameObject, 1f);
                    }
                    
                    // مؤشر الضرب
                    if (hitMarkerPrefab != null)
                    {
                        GameObject hitMarker = Instantiate(hitMarkerPrefab, hit.point + Vector3.up * 0.5f, Quaternion.identity);
                        Destroy(hitMarker, 0.5f);
                    }
                    
                    // اهتزاز الكاميرا
                    CameraShake.Instance?.Shake(0.1f, 0.05f);
                }
            }
        }
        
        // إذا لم يصب أي عدو
        if (!hitEnemy && missSound != null)
        {
            AudioManager.Instance.PlaySFX(missSound.name, transform.position);
        }
        
        // تحديث الموسيقى للقتال
        if (hitEnemy)
        {
            AudioManager.Instance.PlayDynamicMusic(AudioManager.MusicIntensity.Combat);
        }
    }
    
    float CalculateDamage()
    {
        float damage = baseDamage;
        
        if (currentWeapon != null)
        {
            damage = currentWeapon.damage;
        }
        
        // حساب الضربات الحرجة
        bool isCritical = Random.Range(0f, 1f) <= (currentWeapon?.critChance ?? 0.1f);
        
        if (isCritical)
        {
            damage *= 2f; // ضرر مضاعف للضربة الحرجة
            
            // تشغيل صوت الضربة الحرجة
            if (critSound != null)
            {
                AudioManager.Instance.PlaySFX(critSound.name, transform.position);
            }
            
            Debug.Log("ضربة حرجة!");
        }
        
        // تأثيرات العشوائية
        damage *= Random.Range(0.9f, 1.1f);
        
        return Mathf.Round(damage);
    }
    
    void ShowDamageNumber(Vector3 position, float damage)
    {
        // إنشاء نص الضرر
        GameObject damageTextObj = new GameObject("DamageText");
        damageTextObj.transform.position = position + Vector3.up * 2f;
        
        TextMesh textMesh = damageTextObj.AddComponent<TextMesh>();
        textMesh.text = damage.ToString("F0");
        textMesh.fontSize = 20;
        textMesh.color = Color.red;
        textMesh.anchor = TextAnchor.MiddleCenter;
        
        // إضافة حركة للأعلى
        StartCoroutine(FloatDamageText(damageTextObj));
    }
    
    IEnumerator FloatDamageText(GameObject textObj)
    {
        float duration = 1f;
        float elapsed = 0f;
        Vector3 startPos = textObj.transform.position;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            textObj.transform.position = startPos + Vector3.up * (2f * t);
            
            // التلاشي
            TextMesh textMesh = textObj.GetComponent<TextMesh>();
            Color color = textMesh.color;
            color.a = 1f - t;
            textMesh.color = color;
            
            yield return null;
        }
        
        Destroy(textObj);
    }
    
    public void EquipWeapon(Weapon weapon)
    {
        currentWeapon = weapon;
        
        // إزالة السلاح القديم
        if (weaponModel != null)
        {
            Destroy(weaponModel);
        }
        
        // إنشاء نموذج السلاح الجديد
        if (weapon.weaponPrefab != null && weaponHolder != null)
        {
            weaponModel = Instantiate(weapon.weaponPrefab, weaponHolder);
            weaponModel.transform.localPosition = Vector3.zero;
            weaponModel.transform.localRotation = Quaternion.identity;
        }
        
        Debug.Log($"تم تجهيز {weapon.weaponName}");
        
        // نغمة التجهيز
        AudioManager.Instance.PlayEventTone("item_found");
    }
    
    void SwitchWeapon(int weaponSlot)
    {
        // يمكنك تعديل هذا بناءً على نظام المخزون الخاص بك
        // هذا مثال مبسط
        if (InventorySystem.Instance != null)
        {
            var equippedWeapon = InventorySystem.Instance.GetEquippedItem(InventorySystem.ItemType.Weapon);
            if (equippedWeapon != null)
            {
                // هنا يجب تحميل بيانات السلاح من قاعدة البيانات
                Weapon newWeapon = new Weapon
                {
                    weaponId = equippedWeapon.itemId,
                    weaponName = equippedWeapon.itemName,
                    damage = 20f,
                    range = 2f,
                    speed = 1f,
                    critChance = 0.15f
                };
                
                EquipWeapon(newWeapon);
            }
        }
    }
    
    void PerformSpecialAttack()
    {
        // هجوم خاص (يحتاج إلى شحن أو موارد)
        if (canAttack && HasSpecialAttackResources())
        {
            StartCoroutine(SpecialAttackRoutine());
        }
    }
    
    IEnumerator SpecialAttackRoutine()
    {
        canAttack = false;
        
        // رسوم متحركة للهجوم الخاص
        if (animator != null)
        {
            animator.SetTrigger("SpecialAttack");
        }
        
        // جسيمات خاصة
        if (attackParticles != null)
        {
            var main = attackParticles.main;
            main.startSize = 2f;
            main.startColor = Color.yellow;
            attackParticles.Play();
        }
        
        // صوت خاص
        AudioManager.Instance.PlaySFX("special_attack", transform.position);
        
        yield return new WaitForSeconds(0.5f);
        
        // نطاق هجوم أوسع
        Vector3 attackStart = transform.position + Vector3.up * 1f;
        Collider[] hitEnemies = Physics.OverlapSphere(attackStart, attackRange * 2f, enemyLayer);
        
        foreach (Collider enemy in hitEnemies)
        {
            EnemyHealth enemyHealth = enemy.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                float damage = CalculateDamage() * 1.5f; // ضرر مضاعف
                enemyHealth.TakeDamage(damage);
                
                ShowDamageNumber(enemy.transform.position + Vector3.up * 2f, damage);
            }
        }
        
        yield return new WaitForSeconds(1f);
        
        canAttack = true;
    }
    
    bool HasSpecialAttackResources()
    {
        // التحقق من موارد الهجوم الخاص (مثل المانا أو الطاقة)
        // هذا مثال، يمكنك تعديله حسب نظام اللعبة
        return true;
    }
    
    public void UpgradeWeapon(float damageBonus, float speedBonus)
    {
        if (currentWeapon != null)
        {
            currentWeapon.damage += damageBonus;
            currentWeapon.speed += speedBonus;
            
            Debug.Log($"تم ترقية {currentWeapon.weaponName}");
            
            // نغمة الترقية
            AudioManager.Instance.PlayEventTone("level_up");
        }
    }
    
    void OnDrawGizmosSelected()
    {
        // عرض نطاق الهجوم
        Gizmos.color = Color.red;
        Vector3 attackStart = transform.position + Vector3.up * 1f;
        Gizmos.DrawWireSphere(attackStart, attackRange);
        
        // عرض نطاق الهجوم الخاص
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(attackStart, attackRange * 2f);
    }
}

// نظام صحة العدو (يجب وضعه في ملف منفصل)
public class EnemyHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    public float currentHealth;
    
    [Header("Visual Effects")]
    public GameObject deathEffect;
    public AudioClip deathSound;
    public AudioClip hurtSound;
    
    [Header("Loot")]
    public GameObject[] lootItems;
    public int minLootAmount = 1;
    public int maxLootAmount = 3;
    
    void Start()
    {
        currentHealth = maxHealth;
    }
    
    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        
        // تشغيل صوت الأذى
        if (hurtSound != null && currentHealth > 0)
        {
            AudioManager.Instance.PlaySFX(hurtSound.name, transform.position);
        }
        
        // تحديث شريط الصحة (إذا كان موجودًا)
        UpdateHealthBar();
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    void UpdateHealthBar()
    {
        // هنا يمكنك تحديث شريط صحة العدو
        // مثال: healthBar.fillAmount = currentHealth / maxHealth;
    }
    
    void Die()
    {
        // تشغيل صوت الموت
        if (deathSound != null)
        {
            AudioManager.Instance.PlaySFX(deathSound.name, transform.position);
        }
        
        // تأثيرات الموت
        if (deathEffect != null)
        {
            Instantiate(deathEffect, transform.position, Quaternion.identity);
        }
        
        // إسقاط الغنائم
        DropLoot();
        
        // تحديث المهام (قتل الأعداء)
        if (QuestSystem.Instance != null)
        {
            QuestSystem.Instance.UpdateKillObjective(gameObject.name);
        }
        
        // تدمير الكائن
        Destroy(gameObject);
    }
    
    void DropLoot()
    {
        if (lootItems.Length == 0) return;
        
        int lootAmount = Random.Range(minLootAmount, maxLootAmount + 1);
        
        for (int i = 0; i < lootAmount; i++)
        {
            GameObject lootItem = lootItems[Random.Range(0, lootItems.Length)];
            if (lootItem != null)
            {
                Vector3 dropPosition = transform.position + new Vector3(
                    Random.Range(-1f, 1f),
                    0.5f,
                    Random.Range(-1f, 1f)
                );
                
                Instantiate(lootItem, dropPosition, Quaternion.identity);
            }
        }
    }
}
