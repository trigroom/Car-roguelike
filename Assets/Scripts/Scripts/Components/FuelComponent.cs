using UnityEngine;

public struct FuelComponent {
    public float MaxElectricity;
    public float ElectricityConsumption;
    public float ConstantElectricityConsumption;
    public float ConstantElectricityReplenishment;
    public float ConstantFuelElectricityReplenishment;
    public float CurrentElectricity;

    public float MaxFuel;
    public float FuelConsumption;
    public float ConstantFuelConsumption;
    public float ConstantFuelToElectricityConsumption;
    public float CurrentFuel;
    
    public bool UseSpeedBoost;
}
