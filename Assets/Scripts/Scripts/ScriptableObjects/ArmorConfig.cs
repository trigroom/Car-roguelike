using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ArmorConfig", menuName = "CarPartConfigs/ArmorConfig")]
public class ArmorConfig : CarPartConfig
{
    public float DefencePercent;
    public List<DefenceDirection> DefenceDirections;
    public bool ExplosionDefence;

}

public enum DefenceDirection {
    Up,
    Down,
    Left,
    Right
}
