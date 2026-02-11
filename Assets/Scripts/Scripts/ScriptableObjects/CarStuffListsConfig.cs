using System.Collections.Generic;
using AYellowpaper.SerializedCollections;
using UnityEngine;

[CreateAssetMenu(fileName = "CarStuffListsConfig", menuName = "Scriptable Objects/CarStuffListsConfig")]
public class CarStuffListsConfig : ScriptableObject
{
    [field: SerializeField]
    public Material DefaultMaterial { get;private set; }
    [field: SerializeField]
        public Material EnemyProjectilesMaterial { get;private set; }
    [field: SerializeField]
    public Material LowDamagedMaterial { get;private set; }
    [field: SerializeField]
    public Material HighDamagedMaterial { get;private set; }
    [field: SerializeField]
    
    public Material FrozenMaterial { get;private set; }
    [field: SerializeField]
    public Material FireMaterial { get;private set; }
    [field: SerializeField]
    public CarConfig[] CarConfigs { get;private set; }
    [field: SerializeField]
    public CarPartConfig[] CarPartConfigs { get;private set; }
    [field: SerializeField]
    public CarPartStuffView CarPartStuffView { get;private set; }
    [field: SerializeField]
    public MeleeCarPartStuffView MeleeCarPartStuffView { get;private set; }
    
    [field: SerializeField]
    public int[] ShopItems { get;private set; }
    
    [field: SerializeField]
    public WheelPrefabConfig[] Wheels { get;private set; }
    
    [field: SerializeField]
    public Sprite[] CarPartUIBordersSprites { get;private set; }
    [field: SerializeField]
    public Color DestroyedCarPartColor { get;private set; }
    [field: SerializeField]
    public MapConfig[] MapConfigs { get;private set; }
    [field: SerializeField]
    public ShopIdList[] RoundShopChances{ get;private set; }
    [field: SerializeField]
    public AchivementConfig[] AchievementConfigs{ get;private set; }
    [field: SerializeField]
    public ShopItemConfig[] ShopItemConfigs{ get;private set; }
    [field: SerializeField]
    public ModuleConfig[] ModuleConfigs{ get;private set; }
    [field: SerializeField]
    public CarBasesForCarDictionary[] CarBasesForCars { get;private set; }
   
}

[System.Serializable]
public class ShopIdList {
    public List<float> Chances;
}
[System.Serializable]
public class CarBasesForCarDictionary {
     [SerializedDictionary]
        public SerializedDictionary<int, List<CarBaseMapConfig>> CarPartBasesForLevel = new SerializedDictionary<int, List<CarBaseMapConfig>>();
}
