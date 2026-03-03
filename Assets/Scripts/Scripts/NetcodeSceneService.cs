using System;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Pool;
using Unity.Netcode.Transports.UTP;
using Unity.Netcode.Transports.SinglePlayer;

public class NetcodeSceneService : NetworkBehaviour
{
   // private UnityTransport unityTransport;
    private SinglePlayerTransport singlePlayerTransport;
   [field:SerializeField] public Transform playersContainersTransform { get; private set; }
   [field:SerializeField] public TMP_Text clientsMessegeText { get; private set; }
   [field:SerializeField] public Transform _carView { get; private set; }
   [field:SerializeField] public NetworkObject networkObjectsMapContainer { get; private set; }
   [field:SerializeField] public NetworkObject carPartStuffContainerView { get; private set; }
   [field:SerializeField] public NetworkObject gunContainer { get; private set; }
   [field:SerializeField] public NetworkObject spriteRenderer { get; private set; }
   [field:SerializeField] public NetworkObject laserLineRenderer { get; private set; }
   [field:SerializeField] public NetworkObject meleeCarPartStuffView { get; private set; }
   [field:SerializeField] public NetworkObject notUsedNetworkObjectsContainer { get; private set; }
   private Action<CarView, int> _carSetupAction;

   private Action<int, SpriteRenderer, Transform, SpriteRenderer, LineRenderer, CarPartStuffView[], Transform, MeleeCarPartStuffView>
       _carPartSetupAction;
   private ObjectPool<NetworkObject> _carPartsPool;
   private ObjectPool<NetworkObject> _gunContainersPool;
   private ObjectPool<NetworkObject> _spriteRenderersPool;
   private ObjectPool<NetworkObject> _laserLineRenderersPool;
   private ObjectPool<NetworkObject> _meleeCarPartStuffViewsPool;
    private void Awake() {
        NetcodeBetweenScenesService.Instance.NetcodeSceneService = this;
      
        notUsedNetworkObjectsContainer.Spawn();
    }

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        
        _carPartsPool = new ObjectPool<NetworkObject>(() => {
            var needObject = Instantiate(carPartStuffContainerView);
         
            return needObject;
        });
        _gunContainersPool = new ObjectPool<NetworkObject>(() => {
            var needObject = Instantiate(gunContainer);
           
            return needObject;
        });
        _spriteRenderersPool = new ObjectPool<NetworkObject>(() => {
            var needObject = Instantiate(spriteRenderer);
          
            return needObject;
        });
        _laserLineRenderersPool = new ObjectPool<NetworkObject>(() => {
            var needObject = Instantiate(laserLineRenderer);
         
            return needObject;
        });
        _meleeCarPartStuffViewsPool = new ObjectPool<NetworkObject>(() => {
            var needObject = Instantiate(meleeCarPartStuffView);
          
            return needObject;
        });
    }


    public void SetupService(Action<CarView, int> carSetupAction,Action< int, SpriteRenderer, Transform, SpriteRenderer, LineRenderer, CarPartStuffView[], Transform, MeleeCarPartStuffView>carPartSetupAction) {
        _carSetupAction = carSetupAction;
        _carPartSetupAction = carPartSetupAction;
    }
    public NetworkObject InstantiateObject(bool isSpawn,NetworkObject spawnObject, Vector2 spawnPosition = default,
        Quaternion rotation = default) {
        var needObject = Instantiate(spawnObject, spawnPosition,rotation);
        
        if (isSpawn)
            needObject.Spawn(true);
        return needObject;
    }
    public void ShowMessageClient(string text) {
        if (NetworkManager.Singleton.IsHost) return;
        clientsMessegeText.text = GetLocalization(text);
        clientsMessegeText.transform.parent.gameObject.SetActive(true);
    }
    private string GetLocalization(string needString) => BetweenScenesService.Instance.CsvLocalization.GetLocalizedValue(needString);
    [ClientRpc]
    public void HideMessageClientRpc() {
        if (NetworkManager.Singleton.IsHost) return;
        clientsMessegeText.transform.parent.gameObject.SetActive(false);
    }
    [ServerRpc(RequireOwnership = false)]
    public void InstantiateCarViewServerRpc( int entity,ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        
        var carTransform = Instantiate(_carView).GetComponent<NetworkObject>();
       
        carTransform.SpawnWithOwnership(clientId);
        SendCarToClientRpc(clientId,carTransform, entity);
    }
    
    [Rpc(SendTo.ClientsAndHost)]
    private void SendCarToClientRpc(ulong targetClientId, NetworkObjectReference carReference, int entity)
    {
        // Этот RPC выполнят все клиенты, но нас интересует только целевой
        if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
  
        if (carReference.TryGet(out NetworkObject netObj))
        {    
            var carView = netObj.GetComponentInChildren<CarView>();
            _carSetupAction.Invoke(carView, entity);
        }
    }
    [ServerRpc(RequireOwnership = false)]
    public void SpawnCarPartSpritesServerRpc(int mainEntity, int cellEntity, int carPartId, int rotateAngle, bool isPlayer, int containerSizeX,
        int containerSizeY, NetworkObjectReference carView, int[] carpartStuffPositionsX, int[] carpartStuffPositionsY,
        ServerRpcParams rpcParams = default) {
        
        carView.TryGet(out NetworkObject carViewObject);
        if (carViewObject.transform.GetComponentInChildren<CarView>().CarPartsContainer.GetComponent<NetworkObject>() == null)
        { 
            var containerNetObj = carViewObject.transform.GetComponentInChildren<CarView>().CarPartsContainer.gameObject.AddComponent<NetworkObject>();
           var carViewNetworkObject = carViewObject.transform.GetChild(0).gameObject.AddComponent<NetworkObject>();
           containerNetObj.Spawn(); // Важно заспавнить!
           carViewNetworkObject.Spawn();
           carViewNetworkObject.TrySetParent(carViewObject);
          
           
            containerNetObj.TrySetParent(carViewNetworkObject);
            carViewNetworkObject.GetComponent<CarView>().CarPartsContainer = containerNetObj.transform;
        }
        
        
        ulong clientId = rpcParams.Receive.SenderClientId;
        var carPartConfig = BetweenScenesService.Instance._carStuffListsConfig.CarPartConfigs[carPartId];
        var cellCarPartsStuffViews = new List<NetworkObjectReference>();
        for (int i = 0; i < carpartStuffPositionsX.Length; i++) {
            var carPartView = _carPartsPool.Get();
            if (!carPartView.IsSpawned)
                carPartView.Spawn(true);
            cellCarPartsStuffViews.Add(carPartView);
        }

        NetworkObject spriteBase = _spriteRenderersPool.Get();
        if (!spriteBase.IsSpawned)
            spriteBase.Spawn(true);
        NetworkObject upPartContainer = null;
        NetworkObject firePointTransform = null;
        NetworkObject weaponSpriteRenderer = null;
        NetworkObject laserLineRenderer = null;
        NetworkObject meleeCarPartStuffView = null;
        if (carPartConfig.PartType == CarPartType.Gun) {
            if (carPartConfig is GunConfig gunConfig) {
                if (gunConfig.UpPartSprite != null) {
                    upPartContainer = _gunContainersPool.Get();
                    if (!upPartContainer.IsSpawned)
                        upPartContainer.Spawn(true);
                    firePointTransform = _gunContainersPool.Get();
                    if (!firePointTransform.IsSpawned)
                        firePointTransform.Spawn(true);
                    weaponSpriteRenderer = _spriteRenderersPool.Get();
                    if (!weaponSpriteRenderer.IsSpawned)
                        weaponSpriteRenderer.Spawn(true);
                } else {
                    firePointTransform = _gunContainersPool.Get();
                    if (!firePointTransform.IsSpawned)
                        firePointTransform.Spawn(true);
                }

                if (gunConfig.MissleType == MissleType.laser) {
                    laserLineRenderer = _laserLineRenderersPool.Get();
                    if (!laserLineRenderer.IsSpawned)
                        laserLineRenderer.Spawn(true);
                }
            }
        } else if (carPartConfig.PartType == CarPartType.MeleeWeapon) {
            if (carPartConfig is MeleeWeaponConfig meleeConfig) {
                if (meleeConfig.UpSprite != null) {
                    weaponSpriteRenderer = _spriteRenderersPool.Get();
                    if (!weaponSpriteRenderer.IsSpawned)
                        weaponSpriteRenderer.Spawn(true);
                }

                meleeCarPartStuffView = _meleeCarPartStuffViewsPool.Get();
                if (!meleeCarPartStuffView.IsSpawned)
                    meleeCarPartStuffView.Spawn(true);
            }
        }

        SpawnCarPartSpritesClientRpc(clientId, mainEntity, cellEntity, carPartId, rotateAngle, isPlayer, spriteBase, upPartContainer,
            weaponSpriteRenderer, laserLineRenderer, cellCarPartsStuffViews.ToArray(), firePointTransform, meleeCarPartStuffView,
            containerSizeX, containerSizeY, carViewObject, carpartStuffPositionsX, carpartStuffPositionsY);
    }
    [Rpc(SendTo.ClientsAndHost)]
    private void SpawnCarPartSpritesClientRpc(ulong clietId,int mainEntity,int cellEntity, int carPartId, int rotateAngle, bool isPlayer,
        NetworkObjectReference spriteBase, NetworkObjectReference weaponContainer, NetworkObjectReference weaponSpriteRendererReference,
        NetworkObjectReference laserLineRendererReference, NetworkObjectReference[] carPartStuff, NetworkObjectReference firePoint,
        NetworkObjectReference meleeStuff, int containerSizeX,
        int containerSizeY, NetworkObjectReference carView, int[] carpartStuffPositionsX, int[] carpartStuffPositionsY) {
       
        var carPartConfig = BetweenScenesService.Instance._carStuffListsConfig.CarPartConfigs[carPartId];

        var containersSize = new Vector2(containerSizeX, containerSizeY);
        bool isContainer = GridUtils.IsContainerType(carPartConfig);
        int needSpriteLevel = isContainer ? 1 : 2;
        int needRotate = rotateAngle;
        var needList = carPartStuff;
        Vector2Int carPartSize = GridUtils.GetCarPartSize(carPartConfig);
        var positionsList = new List< Vector2Int>();
        for (int i = 0; i < carpartStuffPositionsX.Length; i++) 
            positionsList.Add(new Vector2Int(carpartStuffPositionsX[i],carpartStuffPositionsY[i]));
        
       // carPartCmp.CellCarPartsStuffViews = new List<CarPartStuffView>();
        int carPartStuffIndex = 0;
        var carPartStuffsList = new CarPartStuffView[needList.Length];
        foreach (var carPartInfo in needList) {
           carPartInfo.TryGet(out NetworkObject carPart);
           var carPartStuffView = carPart.GetComponent<CarPartStuffView>();
          //  var carPart = isContainer ? carPartStuff : carCmp.CarPartViewsGrid[carPartInfo.x, carPartInfo.y];
          var needGridPosition = new Vector2Int(positionsList[carPartStuffIndex].x, positionsList[carPartStuffIndex].y);
            if (isContainer) {
                if (NetworkManager.Singleton.LocalClientId == clietId) {
                    carView.TryGet(out NetworkObject carViewObject);
                  Debug.Log(carViewObject.GetComponentInChildren<CarView>().CarPartsContainer);
                    carPart.TrySetParent(carViewObject.transform.GetComponentInChildren<CarView>().CarPartsContainer.transform);
                //    carPart.TrySetParent(carViewObject.transform.GetChild(0).GetComponent<CarView>().CarPartsContainer.transform);
                    carPartStuffView.transform.localPosition = new Vector2(needGridPosition.x, needGridPosition.y) -
                                                               new Vector2(containersSize.x / 2f, containersSize.y / 2f);
                    carPartStuffView.transform.localRotation = Quaternion.Euler(0, 0, needRotate);
                }
                string needLayer = isPlayer ? "CarBase" : "CarBaseEnemy";
                carPartStuffView.SetupCarPart(needGridPosition, needLayer, mainEntity);
               
                carPartStuffView.SetColliderEnableState(true);
                carPartStuffView.gameObject.tag = isPlayer ? "Player" : "Enemy";
              //  carCmp.CarPartViewsGrid[needGridPosition.x, needGridPosition.y] = carPart;
            } else {
                string needLayer = isPlayer ? "UpCarPart" : "UpCarPartEnemy";
                carPartStuffView.SetupCarPart(needGridPosition, needLayer, mainEntity);
                carPart.transform.localRotation = Quaternion.Euler(0, 0, needRotate);
            }
            carPartStuffsList[carPartStuffIndex] =carPartStuffView;
            carPartStuffIndex++;
        }

        spriteBase.TryGet(out NetworkObject carPartCellRendererNetworkObject);
        if (NetworkManager.Singleton.LocalClientId== clietId) {
            carView.TryGet(out NetworkObject carViewNetworkObject);
            carPartCellRendererNetworkObject.TrySetParent(carViewNetworkObject.transform.GetComponentInChildren<CarView>().CarPartsContainer.transform);
            if (needRotate == 90 || needRotate == 270)
                carPartSize = new Vector2Int(carPartSize.y, carPartSize.x);
            carPartCellRendererNetworkObject.transform.localPosition = (Vector3Int)GridUtils.GetEdgePointFromCarPart(positionsList) +
                                                                       new Vector3((carPartSize.x - containersSize.x) / 2f,
                                                                           (carPartSize.y - containersSize.y) / 2f);
            carPartCellRendererNetworkObject.transform.localRotation = Quaternion.Euler(0, 0, needRotate);
        }

        var carPartCellRenderer = carPartCellRendererNetworkObject.GetComponent<SpriteRenderer>();

        carPartCellRenderer.sprite = carPartConfig.PartSprite;
        carPartCellRenderer.sortingOrder = needSpriteLevel;

        NetworkObject weaponContainerTransform = null;
        SpriteRenderer weaponSpriteRenderer = null;
        LineRenderer laserLineRenderer = null;
        NetworkObject firePointTransform = null;
        MeleeCarPartStuffView meleeCarPartStuffView = null;
        
        if (carPartConfig.PartType == CarPartType.Gun && carPartConfig is GunConfig gunConfig) {
            
            if (gunConfig.UpPartSprite != null && weaponContainer.TryGet(out weaponContainerTransform)) {
                if (NetworkManager.Singleton.LocalClientId == clietId) {
                    weaponContainerTransform.TrySetParent(carPartCellRendererNetworkObject);
                    weaponContainerTransform.transform.localPosition = gunConfig.GunContainerPosition;
                    weaponContainerTransform.transform.localRotation = Quaternion.Euler(0, 0, 0);
                }
                
                //gunCmp.UpPartContainer = _sceneService.Value.GetGunContainer(carPartCmp.CarPartCellRenderer.transform);

                if (firePoint.TryGet(out firePointTransform) && NetworkManager.Singleton.LocalClientId == clietId) {
                    //   netObjFirePoint.FirePointTransform = _sceneService.Value.GetGunContainer(gunCmp.UpPartContainer);

                    firePointTransform.TrySetParent(weaponContainerTransform);
                    firePointTransform.transform.localPosition = gunConfig.FirePointPosition - gunConfig.GunContainerPosition;
                    firePointTransform.transform.localRotation = Quaternion.Euler(0, 0, 0);
                }

                weaponSpriteRendererReference.TryGet(out NetworkObject netObjWeaponSprite);
                weaponSpriteRenderer = netObjWeaponSprite.GetComponent<SpriteRenderer>();
                if (NetworkManager.Singleton.LocalClientId == clietId) {
                    netObjWeaponSprite.TrySetParent(weaponContainerTransform);
                    weaponSpriteRenderer.transform.localPosition = gunConfig.UpPartPosition - gunConfig.GunContainerPosition;
                    weaponSpriteRenderer.transform.localRotation = Quaternion.Euler(0, 0, 0);
                }
              
       //         gunSpriteRenderer = _sceneService.Value.GetSpriteRenderer(gunCmp.UpPartContainer);
       weaponSpriteRenderer.sprite = gunConfig.UpPartSprite;
       weaponSpriteRenderer.sortingOrder = 3;
               
            } else {
                if (weaponSpriteRendererReference.TryGet(out NetworkObject netObjFirePoint) && NetworkManager.Singleton.LocalClientId == clietId) {
                    netObjFirePoint.TrySetParent(carPartCellRendererNetworkObject);
                    netObjFirePoint.transform.localPosition = gunConfig.FirePointPosition;
                    netObjFirePoint.transform.localRotation = Quaternion.Euler(0, 0, 0);
                }
            }

            if (gunConfig.MissleType == MissleType.laser && NetworkManager.Singleton.LocalClientId == clietId) {
                laserLineRendererReference.TryGet(out NetworkObject netObjLaserLine);
                netObjLaserLine.TrySetParent(notUsedNetworkObjectsContainer);
                laserLineRenderer = netObjLaserLine.GetComponent<LineRenderer>();
            }

        } else if (carPartConfig.PartType == CarPartType.MeleeWeapon && carPartConfig is MeleeWeaponConfig meleeConfig) {
            if (meleeConfig.UpSprite != null && weaponSpriteRendererReference.TryGet(out NetworkObject netObj)) {
                weaponSpriteRenderer = netObj.GetComponent<SpriteRenderer>();
                weaponSpriteRenderer.sprite = meleeConfig.UpSprite;
                netObj.TrySetParent(carPartCellRendererNetworkObject);
                weaponSpriteRenderer.transform.localRotation = Quaternion.Euler(0, 0, 0);
                weaponSpriteRenderer.sortingOrder = 3;
                weaponSpriteRenderer.transform.localPosition = meleeConfig.UpSpritePosition;

                if (meleeStuff.TryGet(out NetworkObject netObjMeleeStuff)) {
                    meleeCarPartStuffView = netObjMeleeStuff.GetComponent<MeleeCarPartStuffView>();
                    netObjMeleeStuff.TrySetParent(netObj);
                    meleeCarPartStuffView.transform.localRotation = Quaternion.Euler(0, 0, 0);
                    meleeCarPartStuffView.transform.localPosition = Vector3.zero;
                    meleeCarPartStuffView.MakeTriggerCollider();
                    meleeCarPartStuffView.SetColliderEnableState(true);
                }
            }

            if (NetworkManager.Singleton.LocalClientId == clietId) {
                Transform firePointTransformToAction = firePointTransform ? firePointTransform.transform:null;
                Transform weaponContainerTransformToAction = weaponContainerTransform ? weaponContainerTransform.transform:null;
                _carPartSetupAction.Invoke(cellEntity, carPartCellRenderer, weaponContainerTransformToAction, weaponSpriteRenderer,
                    laserLineRenderer, carPartStuffsList, firePointTransformToAction, meleeCarPartStuffView);
            }
                
            //set to component method
            //  _constructMeleeCellEventsPool.Value.Add(cellEntity);
        }
    }
    
    public Transform InstantiateObject(Transform spawnObject, Transform parent, Vector2 spawnPosition = default)
    {
        return Instantiate(spawnObject, spawnPosition, Quaternion.identity, parent).transform;
    }
}
