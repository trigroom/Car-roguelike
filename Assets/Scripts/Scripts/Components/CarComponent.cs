using System.Collections.Generic;
using UnityEngine;

public struct CarComponent {
    public int CarId; //

    public int CarLevel;
    
    public float CurrentWeight;
    public float CurrentMaxWeight;
    public int ShockedMainPartsCount;

    public List<ParticleSystem> ForwardWheelsParticles;
    public List<Animator> ForwardWheels;
    public CarView CarView;

    public bool isWheelParticle;

     public Dictionary<int, List<Vector2Int>> CarPartGroupsList;
     public int[,] CarPartGroupsGrid;
     public CarPartStuffView[,] CarPartViewsGrid;
     
     public Dictionary<int, List<Vector2Int>> CarPartContainerGroupsList;
     public int[,] CarPartContainerGroupsGrid;
    
    public float WheelsRotateAngle;
}
