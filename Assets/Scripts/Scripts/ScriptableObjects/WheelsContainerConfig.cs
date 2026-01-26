using UnityEngine;

[CreateAssetMenu(fileName = "WheelsContainerConfig", menuName = "CarPartConfigs/WheelsContainerConfig")]
public class WheelsContainerConfig : CarPartConfig {
    public int WheelId;
    public Vector2[] WheelsPositions;
    public float MaxWeight;

    public bool CanBeMonoWheel; 
    public int StabilityRadius;
}
