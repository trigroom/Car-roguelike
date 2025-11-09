using System.Collections.Generic;
using AYellowpaper.SerializedCollections;
using UnityEngine;

[CreateAssetMenu(fileName = "CarConfig", menuName = "Scriptable Objects/CarConfig")]
public class CarConfig : ScriptableObject {
    [field: SerializeField]
    public int CarId { get; private set; }
    [field: SerializeField]
    public string CarName { get; private set; }
    [field: SerializeField]
    public Sprite CarBackground { get; private set; }
    [field: SerializeField]
    public float MaxWeight { get; private set; }
    [field: SerializeField]
    public int Height { get; private set; }
    [field: SerializeField]
    public int Width { get; private set; }
    [field: SerializeField]
    public int MaxHealth { get; private set; }
    [field: SerializeField]
    public int[] StartCarParts { get; private set; }
    [field: SerializeField]
    public Vector2[] ForwardWheelsPositions { get; private set; }
    [field: SerializeField]
    public Vector2[] BackWheelsPositions { get; private set; }
    [field: SerializeField]
    public int WheelId { get; private set; }
    [field: SerializeField]
    public float FuelСonsumption { get; private set; }
    [field: SerializeField]
    public float AccelerationFuelСonsumption { get; private set; }
    [field: SerializeField]
    public float RotateSpeed { get; private set; }
    [field: SerializeField]
    public float BoostSpeedMultiplayer { get; private set; }
    [field: SerializeField]
    public float DefaultMaxSpeed { get; private set; }
    [field: SerializeField]
    public float Acceleration { get; private set; }
    [field: SerializeField]
    public float Deceleration { get; private set; }
    [field: SerializeField]
    public float DefaultMaxFuel { get; private set; }
    
    [SerializedDictionary]
    public SerializedDictionary<int, List<int>> CarPartForLevel = new SerializedDictionary<int, List<int>>();
    [field: SerializeField]
    public float CarUpgradeCost { get; private set; }
}
