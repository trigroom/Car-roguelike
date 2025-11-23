using UnityEngine;

[CreateAssetMenu(fileName = "CarPartConfig", menuName = "Scriptable Objects/CarPartConfig")]
public class CarPartConfig : ScriptableObject {
    [field: SerializeField]
    public int Id { get; private set; }

    [field: SerializeField]
    public float MaxHealth { get; private set; }

    [field: SerializeField]
    public float Weight { get; private set; }

    [field: SerializeField]
    public Sprite PartSprite { get; private set; }

    [field: SerializeField]
    public CarPartType PartType { get; private set; }

    [field: SerializeField]
    public CarPartRare RartRare { get; private set; }

    [field: SerializeField]
    public int UpgradedPartId { get; private set; }

    [field: SerializeField]
    public int UpgradedPartCost { get; private set; }

    [field: SerializeField]
    public string PartName { get; private set; }

    [field: SerializeField]
    public int DefaultModuleCount { get; private set; }

    [field: SerializeField]
    public Vector2Int[] PartForm { get; private set; }

    [field: SerializeField]
    public float MoneyCost { get; private set; }
}

public enum CarPartRare {
    Low,
    Medium,
    High,
    VeryHigh
}

public enum CarPartType {
    None,
    Armor,
    Main,
    Support,
    WheelsContainer,
    CarStuffContainer,
    Gun,
    MeleeWeapon,
    All
}

public enum ModuleType {
    
}