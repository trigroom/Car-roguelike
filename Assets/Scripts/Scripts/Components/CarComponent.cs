using System.Collections.Generic;
using UnityEngine;

public struct CarComponent {
    public int CarId; //

    public float CurrentWeight;
    public int ShockedMainPartsCount;

    public ParticleSystem[] ForwardWheelsParticles;
    public Transform[] ForwardWheels;
    public CarView CarView;

    public bool isWheelParticle;

     public Dictionary<int, List<Vector2Int>> CarPartGroupsList;
     public int[,] CarPartGroupsGrid;
     public CarPartStuffView[,] CarPartViewsGrid;
     
     public Dictionary<int, List<Vector2Int>> CarPartContainerGroupsList;
     public int[,] CarPartContainerGroupsGrid;
    
    public float WheelsRotateAngle;
}
