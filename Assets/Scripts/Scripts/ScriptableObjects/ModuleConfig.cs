using UnityEngine;

[CreateAssetMenu(fileName = "ModuleConfig", menuName = "Scriptable Objects/ModuleConfig")]
public class ModuleConfig : ScriptableObject {
    public int ModuleId;
    public string ModuleName;
    public Sprite ModuleIcon;
    public ModuleType ModuleType;
    public float Cost;
    public ModuleBuff[] ModuleBuffs;
}
[System.Serializable]
public class ModuleBuff {
    public float BuffCount;
    public Buff Buff;
}

public enum Buff {
    None,
    Health,
    AttackSpeed,
    Damage,
    TargetPierceing,
    UpgradeCost,
    ReloadSpeed, 
    MaxWeight,
    CarRotateSpeed,
    MaxMagazineSize
}