using UnityEngine;
using System.Collections;

public class InteractiveBackground : MonoBehaviour
{
    [Header("Interaction Settings")]
    public InteractionType interactionType = InteractionType.Animate;
    public float interactionCooldown = 2f;
    private bool canInteract = true;
    
    [Header("Visual Effects")]
    public ParticleSystem interactionParticles;
    public Light interactionLight;
    public float lightIntensity = 2f;
    public float fadeDuration = 1f;
    
    [Header("Audio Feedback")]
    public AudioClip interactSound;
    public float pitchVariation = 0.1f;
    
    public enum InteractionType
    {
        Animate,
        EmitParticles,
        ChangeColor,
        SpawnItem,
        PlaySound
    }
    
    void Start()
    {
        if (interactionLight != null)
        {
            interactionLight.intensity = 0f;
        }
    }
    
    public void Interact(Vector3 playerPosition)
    {
        if (!canInteract) return;
        
        StartCoroutine(InteractionRoutine(playerPosition));
    }
    
    IEnumerator InteractionRoutine(Vector3 playerPosition)
    {
        canInteract = false;
        
        // تشغيل الصوت
        if (interactSound != null)
        {
            AudioManager.Instance.PlaySFX(
                interactSound.name, 
                transform.position, 
                pitchVariation
            );
        }
        
        // تطبيق نوع التفاعل
        switch (interactionType)
        {
            case InteractionType.Animate:
                StartCoroutine(AnimateInteraction());
                break;
                
            case InteractionType.EmitParticles:
                if (interactionParticles != null)
                {
                    interactionParticles.Play();
                }
                break;
                
            case InteractionType.ChangeColor:
                StartCoroutine(ChangeColorInteraction());
                break;
                
            case InteractionType.SpawnItem:
                // منطق ظهور العناصر
                break;
                
            case InteractionType.PlaySound:
                // تشغيل صوت إضافي
                break;
        }
        
        // تفعيل الضوء
        if (interactionLight != null)
        {
            yield return StartCoroutine(FadeLight(true));
            yield return new WaitForSeconds(0.5f);
            yield return StartCoroutine(FadeLight(false));
        }
        
        yield return new WaitForSeconds(interactionCooldown);
        canInteract = true;
    }
    
    IEnumerator AnimateInteraction()
    {
        Vector3 originalScale = transform.localScale;
        float duration = 0.3f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float scale = Mathf.Lerp(1f, 1.2f, Mathf.Sin(t * Mathf.PI));
            transform.localScale = originalScale * scale;
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        transform.localScale = originalScale;
    }
    
    IEnumerator ChangeColorInteraction()
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) yield break;
        
        Color originalColor = spriteRenderer.color;
        Color targetColor = new Color(
            Random.Range(0.8f, 1f),
            Random.Range(0.8f, 1f),
            Random.Range(0.8f, 1f)
        );
        
        float duration = 0.5f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            spriteRenderer.color = Color.Lerp(originalColor, targetColor, t);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        yield return new WaitForSeconds(0.2f);
        
        elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            spriteRenderer.color = Color.Lerp(targetColor, originalColor, t);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
    }
    
    IEnumerator FadeLight(bool fadeIn)
    {
        if (interactionLight == null) yield break;
        
        float startIntensity = interactionLight.intensity;
        float targetIntensity = fadeIn ? lightIntensity : 0f;
        
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            float t = elapsed / fadeDuration;
            interactionLight.intensity = Mathf.Lerp(startIntensity, targetIntensity, t);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        interactionLight.intensity = targetIntensity;
    }
    
    void OnMouseDown()
    {
        // يمكن التفاعل بالنقر المباشر في محرر Unity
        if (Camera.main != null)
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Interact(mousePos);
        }
    }
}
