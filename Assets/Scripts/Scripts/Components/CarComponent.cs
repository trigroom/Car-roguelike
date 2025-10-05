using UnityEngine;

public struct CarComponent {
    public int CarId; //

    public float CurrentWeight;
    public int ShockedMainPartsCount;

    public ParticleSystem[] ForwardWheelsParticles;
    public Transform[] ForwardWheels;
    public CarView CarView;

    public bool isWheelParticle;
    
    public float WheelsRotateAngle;
}
