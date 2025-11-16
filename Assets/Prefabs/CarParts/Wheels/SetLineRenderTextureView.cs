using UnityEngine;

public class SetLineRenderTextureView : MonoBehaviour
{
    [Header("Trail Appearance")]
    public Texture2D trailTexture;
    [Range(0.1f, 5f)] public float trailDuration = 1f;
    [Range(0.01f, 1f)] public float trailWidth = 0.1f;
    
    void Start()
    {
        SetupTrail();
    }
    
    void SetupTrail()
    {
        TrailRenderer tr = GetComponent<TrailRenderer>();
        
        // Очищаем существующий трейл
        tr.Clear();
        
        // Базовые настройки
        tr.time = trailDuration;
        tr.widthMultiplier = trailWidth;
        
        // Настройка материала и текстуры
        if (trailTexture != null)
        {
            tr.material = new Material(Shader.Find("Sprites/Default"));
            tr.material.mainTexture = trailTexture;
            tr.material.mainTextureScale = new Vector2(2f, 1f);
        }
    }
    
    // Обновление в реальном времени при изменении в инспекторе
    void OnValidate()
    {
        if (Application.isPlaying && GetComponent<TrailRenderer>() != null)
        {
            SetupTrail();
        }
    }
  
}
