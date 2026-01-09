using UnityEngine;
using System.Collections;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance;
    
    private Transform camTransform;
    private Vector3 originalPos;
    private float shakeDuration = 0f;
    private float shakeMagnitude = 0.1f;
    private float dampingSpeed = 1.0f;
    
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
        
        if (camTransform == null)
        {
            camTransform = GetComponent<Transform>();
        }
    }
    
    void OnEnable()
    {
        originalPos = camTransform.localPosition;
    }
    
    void Update()
    {
        if (shakeDuration > 0)
        {
            camTransform.localPosition = originalPos + Random.insideUnitSphere * shakeMagnitude;
            
            shakeDuration -= Time.deltaTime * dampingSpeed;
        }
        else
        {
            shakeDuration = 0f;
            camTransform.localPosition = originalPos;
        }
    }
    
    public void Shake(float duration, float magnitude)
    {
        shakeDuration = duration;
        shakeMagnitude = magnitude;
    }
    
    public void ShakeWithCurve(float duration, AnimationCurve magnitudeCurve)
    {
        StartCoroutine(ShakeRoutine(duration, magnitudeCurve));
    }
    
    IEnumerator ShakeRoutine(float duration, AnimationCurve magnitudeCurve)
    {
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            
            float curveValue = magnitudeCurve.Evaluate(elapsed / duration);
            camTransform.localPosition = originalPos + Random.insideUnitSphere * curveValue;
            
            yield return null;
        }
        
        camTransform.localPosition = originalPos;
    }
    
    // اهتزازات محددة مسبقاً
    public void ShakeSmall()
    {
        Shake(0.1f, 0.05f);
    }
    
    public void ShakeMedium()
    {
        Shake(0.2f, 0.1f);
    }
    
    public void ShakeLarge()
    {
        Shake(0.3f, 0.2f);
    }
    
    public void ShakeExplosion()
    {
        Shake(0.5f, 0.3f);
    }
}
