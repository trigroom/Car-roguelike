using UnityEngine;

[CreateAssetMenu(fileName = "CarStuffListsConfig", menuName = "Scriptable Objects/CarStuffListsConfig")]
public class CarStuffListsConfig : ScriptableObject
{
    [field: SerializeField]
    public Material DefaultMaterial { get;private set; }
    [field: SerializeField]
    public Material LowDamagedMaterial { get;private set; }
    [field: SerializeField]
    public Material HighDamagedMaterial { get;private set; }
    [field: SerializeField]
    public CarConfig[] CarConfigs { get;private set; }
    [field: SerializeField]
    public CarPartConfig[] CarPartConfigs { get;private set; }
    [field: SerializeField]
    public CarPartStuffView CarPartStuffView { get;private set; }
    [field: SerializeField]
    public MeleeCarPartStuffView MeleeCarPartStuffView { get;private set; }
    
    [field: SerializeField]
    public ShopItem[] ShopItems { get;private set; }
    
    [field: SerializeField]
    public WheelPrefabConfig[] Wheels { get;private set; }
    
    [field: SerializeField]
    public Sprite[] CarPartUIBordersSprites { get;private set; }
    [field: SerializeField]
    public Color DestroyedCarPartColor { get;private set; }
}
[System.Serializable]
public struct ShopItem {
    public int PartId;
    public int Price;
}


