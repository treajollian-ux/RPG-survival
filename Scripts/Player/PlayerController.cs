using UnityEngine;
using System.Collections;

public class EnhancedPlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float runMultiplier = 1.5f;
    private Vector2 movement;
    
    [Header("Audio Settings")]
    public FootstepSounds footstepSounds;
    public float footstepInterval = 0.5f;
    private float footstepTimer;
    
    [Header("Background Interaction")]
    public LayerMask backgroundLayer;
    public ParticleSystem interactionParticles;
    public float backgroundEffectRadius = 3f;
    
    [System.Serializable]
    public class FootstepSounds
    {
        public AudioClip[] grassSteps;
        public AudioClip[] stoneSteps;
        public AudioClip[] waterSteps;
        public AudioClip[] woodSteps;
        
        public AudioClip GetStepSound(string surfaceType)
        {
            AudioClip[] steps = grassSteps; // الافتراضي
            
            switch (surfaceType)
            {
                case "Stone": steps = stoneSteps; break;
                case "Water": steps = waterSteps; break;
                case "Wood": steps = woodSteps; break;
            }
            
            return steps.Length > 0 ? steps[Random.Range(0, steps.Length)] : null;
        }
    }
    
    private Rigidbody2D rb;
    private Animator animator;
    private string currentSurface = "Grass";
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        
        // بدء نظام التفاعل مع الخلفية
        StartCoroutine(BackgroundInteractionUpdate());
    }
    
    void Update()
    {
        HandleInput();
        UpdateFootstepSounds();
        UpdateAnimations();
    }
    
    void FixedUpdate()
    {
        MovePlayer();
    }
    
    void HandleInput()
    {
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");
        
        // تسريع الجري
        if (Input.GetKey(KeyCode.LeftShift))
        {
            movement *= runMultiplier;
        }
        
        // التفاعل مع البيئة
        if (Input.GetMouseButtonDown(0))
        {
            InteractWithBackground();
        }
    }
    
    void MovePlayer()
    {
        Vector2 moveDirection = movement.normalized;
        rb.velocity = moveDirection * moveSpeed * (Input.GetKey(KeyCode.LeftShift) ? runMultiplier : 1f);
        
        // تغيير الموسيقى بناءً على سرعة الحركة
        if (rb.velocity.magnitude > 0.1f)
        {
            AudioManager.Instance.PlayDynamicMusic(AudioManager.MusicIntensity.Tense);
        }
        else
        {
            AudioManager.Instance.PlayDynamicMusic(AudioManager.MusicIntensity.Calm);
        }
    }
    
    void UpdateFootstepSounds()
    {
        if (rb.velocity.magnitude > 0.1f)
        {
            footstepTimer -= Time.deltaTime;
            
            if (footstepTimer <= 0)
            {
                PlayFootstepSound();
                footstepTimer = footstepInterval / (Input.GetKey(KeyCode.LeftShift) ? runMultiplier : 1f);
            }
        }
        else
        {
            footstepTimer = 0;
        }
    }
    
    void PlayFootstepSound()
    {
        AudioClip stepSound = footstepSounds.GetStepSound(currentSurface);
        if (stepSound != null)
        {
            AudioManager.Instance.PlaySFX(stepSound.name, transform.position, 0.1f);
            
            // تأثيرات بصرية للخطوات
            if (interactionParticles != null)
            {
                ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams();
                emitParams.position = transform.position;
                emitParams.velocity = Vector3.up * 2f;
                interactionParticles.Emit(emitParams, 3);
            }
        }
    }
    
    void UpdateAnimations()
    {
        if (animator != null)
        {
            animator.SetFloat("Speed", rb.velocity.magnitude);
            animator.SetFloat("Horizontal", movement.x);
            animator.SetFloat("Vertical", movement.y);
            
            if (movement.x != 0 || movement.y != 0)
            {
                animator.SetFloat("LastHorizontal", movement.x);
                animator.SetFloat("LastVertical", movement.y);
            }
        }
    }
    
    IEnumerator BackgroundInteractionUpdate()
    {
        while (true)
        {
            DetectSurfaceType();
            AffectBackgroundElements();
            yield return new WaitForSeconds(0.2f);
        }
    }
    
    void DetectSurfaceType()
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, 1f, backgroundLayer);
        if (hit.collider != null)
        {
            string newSurface = hit.collider.tag;
            if (newSurface != currentSurface)
            {
                currentSurface = newSurface;
                OnSurfaceChanged(newSurface);
            }
        }
    }
    
    void OnSurfaceChanged(string newSurface)
    {
        // تغيير تأثيرات الصوت والبصر عند تغيير السطح
        Debug.Log($"Surface changed to: {newSurface}");
        
        // تشغيل صوت تغيير السطح
        AudioManager.Instance.PlaySFX("surface_change", transform.position);
        
        // تغيير تأثيرات الجسيمات
        switch (newSurface)
        {
            case "Water":
                if (interactionParticles != null)
                {
                    var main = interactionParticles.main;
                    main.startColor = new Color(0.2f, 0.5f, 0.8f, 0.3f);
                }
                break;
            case "Grass":
                if (interactionParticles != null)
                {
                    var main = interactionParticles.main;
                    main.startColor = new Color(0.2f, 0.8f, 0.3f, 0.3f);
                }
                break;
        }
    }
    
    void AffectBackgroundElements()
    {
        // تأثير اللاعب على عناصر الخلفية (مثل تحريك العشب، إثارة الماء)
        Collider2D[] affectedObjects = Physics2D.OverlapCircleAll(
            transform.position, 
            backgroundEffectRadius, 
            backgroundLayer
        );
        
        foreach (var obj in affectedObjects)
        {
            BackgroundElement bgElement = obj.GetComponent<BackgroundElement>();
            if (bgElement != null)
            {
                float distance = Vector2.Distance(transform.position, obj.transform.position);
                float intensity = 1f - (distance / backgroundEffectRadius);
                
                bgElement.AffectByPlayer(intensity);
            }
        }
    }
    
    void InteractWithBackground()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 mousePos2D = new Vector2(mousePos.x, mousePos.y);
        
        RaycastHit2D hit = Physics2D.Raycast(mousePos2D, Vector2.zero, 0f, backgroundLayer);
        
        if (hit.collider != null)
        {
            InteractiveBackground interactiveBg = hit.collider.GetComponent<InteractiveBackground>();
            if (interactiveBg != null)
            {
                interactiveBg.Interact(transform.position);
                
                // تشغيل صوت التفاعل
                AudioManager.Instance.PlaySFX("background_interact", hit.point);
                
                // نغمة تفاعلية
                AudioManager.Instance.PlayEventTone("item_found");
            }
        }
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, backgroundEffectRadius);
    }
}
