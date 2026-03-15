using Unity.Netcode;
using UnityEngine;

public class CarNetcodeView : NetworkBehaviour
{
    public NetworkVariable<int> ForwardWheelsRotateAngle = new NetworkVariable<int>(0);
}
