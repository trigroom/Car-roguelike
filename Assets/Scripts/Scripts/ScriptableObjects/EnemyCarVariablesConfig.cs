using UnityEngine;

[CreateAssetMenu(fileName = "EnemyCarVariablesConfig", menuName = "Scriptable Objects/EnemyCarVariablesConfig")]
public class EnemyCarVariablesConfig : ScriptableObject
{
    public EnemyCarPartsMaps[] MapsForCar;
    
    public int CarId;
}
