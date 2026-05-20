using Unity.Netcode;
using UnityEngine;

[CreateAssetMenu(fileName = "MapVisualInfoConfig", menuName = "Scriptable Objects/MapVisualInfoConfig")]
public class MapVisualInfoConfig : ScriptableObject {
    public Transform[] MapObstacles;
    public Sprite[] MapGroundPatterns;
    public Material GroundMaterial;
    public Material BehindMapGroundMaterial;
    public float[] NoiseObstaclesCountChances;
    public Color WheelParticlesColor;
    public AudioClip[] defaultBackgroundMusic;
    public AudioClip[] bossBackgroundMusic;
}
