using UnityEngine;

[CreateAssetMenu(fileName = "SupportModuleConfig", menuName = "CarPartConfigs/SupportModuleCarPartConfig")]
public class SupportModuleCarPartConfig : CarPartConfig {
 public SupportModuleType ModuleType;
 public float FirstCount;
 public float SecondCount;
}

public enum SupportModuleType {
 None,
 Turbine,
 Fuel,
 Recovery,
 Ammo,
}
