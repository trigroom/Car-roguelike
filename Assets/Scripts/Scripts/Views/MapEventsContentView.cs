using Unity.Netcode;
using UnityEngine;

public class MapEventsContentView : MonoBehaviour
{
    [field: SerializeField] public Sprite[] _bomberProjectiles{ get; private set; }
    [field: SerializeField] public Sprite[] _bomberProjectilesShadows{ get; private set; }
    [field: SerializeField] public Transform _trainPrefab{ get; private set; }
    [field: SerializeField] public Transform _trainTunnelsPrefab{ get; private set; }
    [field: SerializeField] public Sprite _bomberSprite{ get; private set; }
    [field: SerializeField] public Transform[] _tornadoPrefabs { get; private set; }
    [field: SerializeField] public NetworkObject[] _quicksandPrefabs { get; private set; }
    [field: SerializeField] public Sprite _thunderCloudsSprite{ get; private set; }
    [field: SerializeField] public Transform _thunderPrefab{ get; private set; }
}
