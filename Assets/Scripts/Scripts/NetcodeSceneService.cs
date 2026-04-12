using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Pool;
using Unity.Netcode.Transports.UTP;
using Unity.Netcode.Transports.SinglePlayer;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class NetcodeSceneService : NetworkBehaviour
{
    [field: SerializeField] public NetworkObject _hpBarPrefab { get; private set; }
    [field: SerializeField] public Transform _hpBarsContainer{ get; private set; }
   // private UnityTransport unityTransport;
    private SinglePlayerTransport singlePlayerTransport;
   [field:SerializeField] public Transform playersContainersTransform { get; private set; }
   [field:SerializeField] public TMP_Text clientsMessegeText { get; private set; }
   [field:SerializeField] public Transform _carView { get; private set; }
   [field:SerializeField] public NetworkObject networkObjectsMapContainer { get; private set; }
   [field:SerializeField] public NetworkObject floatingTextMapContainer { get; private set; }
   [field:SerializeField] public NetworkObject floatingTextPrefab { get; private set; }
   [field:SerializeField] public CarPartStuffView carPartStuffContainerView { get; private set; }
   [field:SerializeField] public Transform gunContainer { get; private set; }
   [field:SerializeField] public SpriteRenderer spriteRenderer { get; private set; }
   [field:SerializeField] public LineRenderer laserLineRenderer { get; private set; }
   [field:SerializeField] public MeleeCarPartStuffView meleeCarPartStuffView { get; private set; }
   [field:SerializeField] public Transform notUsedNetworkObjectsContainer { get; private set; }
   [field:SerializeField] public Gradient _healthGradient { get; private set; }
   [field: SerializeField] public ProjectileView _projectileView { get; private set; }
   [field: SerializeField] public Sprite[] _bulletSprites { get; private set; }
   private Action<CarView, int, bool> _carSetupAction;
   private Action<int> _deleteHpBarComponentAction;
   private Action<NetworkObject, int> _getHpBarAction;
   private Action<NetworkObject,Sequence, int> _laserTextGetAction;
   private Action<Animator[], int[]> _wheelComponentsSetupAction;
   private Action< ulong,Vector2,bool > _setSpeedParametersAction;
   private Action< CarView > _setCarViewAction;
   private Action<float, float, float, float> _setFuelBarsAction;
   private Action<int, SpriteRenderer, Transform, SpriteRenderer, LineRenderer, CarPartStuffView[], Transform, MeleeCarPartStuffView>
       _carPartSetupAction;

   private Action<CarPartDataForRpc[], CarPartDataForRpc[], int, ulong, bool, int, Vector2> _setupCarOnServerAction;
   private ObjectPool<CarPartStuffView> _carPartsPool;
   private ObjectPool<Transform> _gunContainersPool;
   private ObjectPool<SpriteRenderer> _spriteRenderersPool;
   private ObjectPool<LineRenderer> _laserLineRenderersPool;
   private ObjectPool<MeleeCarPartStuffView> _meleeCarPartStuffViewsPool;
   private ObjectPool<NetworkObject> _floatingTextsPool;
   private ObjectPool<ProjectileView> _projectilesPool;
   private ObjectPool<NetworkObject> _hpBarsPool;
  
    private void Awake() {
        NetcodeBetweenScenesService.Instance.NetcodeSceneService = this;
     
    }

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn(); 
        _projectilesPool = new ObjectPool<ProjectileView>(() => {
            return Instantiate(_projectileView);
        });
        _floatingTextsPool = new ObjectPool<NetworkObject>(() => {
            var needObject = Instantiate(floatingTextPrefab);
         
            return needObject;
        });
        
        _hpBarsPool = new ObjectPool<NetworkObject>(() => {
            var needObject = Instantiate(_hpBarPrefab);
         
            return needObject;
        });
        _carPartsPool = new ObjectPool<CarPartStuffView>(() => {
            var needObject = Instantiate(carPartStuffContainerView);
         
            return needObject;
        });
        _gunContainersPool = new ObjectPool<Transform>(() => {
            var needObject = Instantiate(gunContainer);
           
            return needObject;
        });
        _spriteRenderersPool = new ObjectPool<SpriteRenderer>(() => {
            var needObject = Instantiate(spriteRenderer);
          
            return needObject;
        });
        _laserLineRenderersPool = new ObjectPool<LineRenderer>(() => {
            var needObject = Instantiate(laserLineRenderer);
         
            return needObject;
        });
        _meleeCarPartStuffViewsPool = new ObjectPool<MeleeCarPartStuffView>(() => {
            var needObject = Instantiate(meleeCarPartStuffView);
          
            return needObject;
        });
    }
    
    public ProjectileView GetGunProjectile(Vector2 spawnPosition)
    {
        var projectile = _projectilesPool.Get();
        if (!projectile.networkObject.IsSpawned) 
            projectile.networkObject.Spawn();
        
        
        projectile.transform.position = spawnPosition;
        projectile.gameObject.SetActive(true);
        return projectile;
    }
    [Rpc(SendTo.ClientsAndHost)]
    public void ReleaseGunProjectileClientRpc(NetworkObjectReference networkObject, float radius ,int projectileSpritesIndex = 0) {
        // if(!projectile.gameObject.activeInHierarchy)return;
        networkObject.TryGet(out var projectileNetworkObject);
        var projectile = projectileNetworkObject.GetComponent<ProjectileView>();
     //   if (projectileSpritesIndex == 0) 
     //       RelaseProjectileYourself(projectile);
      //  else {
            PlayHitAnimation(projectile, radius, projectileSpritesIndex);
       // }
    }

    private void PlayHitAnimation(ProjectileView projectile, float radius ,int projectileSpritesIndex = 0) {
        Sprite[] animation =projectileSpritesIndex == 0? null: _bulletSprites;
        projectile.GetComponent<ProjectileView>().PlayHitAnimation(animation,radius, RelaseProjectileYourself);
    }

    public void RelaseProjectileYourself(ProjectileView projectile) {  
        projectile.gameObject.SetActive(false);
        if(!NetworkManager.IsServer)return;
        if(!projectile.gameObject.activeInHierarchy)return;
    
        _projectilesPool.Release(projectile);
    }
    [ServerRpc(RequireOwnership = false)]
    public void GetLaserFloatingTextServerRpc(int entity, Vector2 position,string text, ServerRpcParams rpcParams = default) {
        var floatingText = _floatingTextsPool.Get();
        if (!floatingText.IsSpawned) {
            floatingText.Spawn();
            floatingText.TrySetParent(floatingTextMapContainer);
        }
            
        SetFloatingTextClientRpc(position, text, Color.white, floatingText,entity,rpcParams.Receive.SenderClientId);
       
    }
    
    /*[ServerRpc(RequireOwnership = false)]
    public void SpawnCarPartSpritesServerRpc(int mainEntity, int cellEntity, int carPartId, int rotateAngle, bool isPlayer,
        int containerSizeX, int containerSizeY, NetworkObjectReference carView, int[] carpartStuffPositionsX, int[] carpartStuffPositionsY,
        ServerRpcParams rpcParams = default) {
        ulong clientId = rpcParams.Receive.SenderClientId;
        SpawnCarPartSpritesClientRpc(clientId,mainEntity,  cellEntity, carPartId, rotateAngle,  isPlayer, containerSizeX,
            containerSizeY, carView,  carpartStuffPositionsX, carpartStuffPositionsY);
    }*/

    public void SetupService(Action<NetworkObject, int> getHpBarAction, Action<int> deleteHpBarComponentAction,
        Action<NetworkObject, Sequence, int> laserTextGetAction, Action<CarView, int, bool> carSetupAction,
        Action<Animator[], int[]> wheelComponentsSetupAction,
        Action<int, SpriteRenderer, Transform, SpriteRenderer, LineRenderer, CarPartStuffView[], Transform, MeleeCarPartStuffView>
            carPartSetupAction, Action<CarPartDataForRpc[], CarPartDataForRpc[], int, ulong,bool, int, Vector2> setupCarOnServerAction,
        Action< ulong,Vector2,bool > setSpeedParametersAction,  Action< CarView > setCarViewAction,Action<float, float, float, float> setFuelBarsAction) {
        _carSetupAction = carSetupAction;
        _carPartSetupAction = carPartSetupAction;
        _wheelComponentsSetupAction = wheelComponentsSetupAction;
        _laserTextGetAction = laserTextGetAction;
        _deleteHpBarComponentAction = deleteHpBarComponentAction;
        _getHpBarAction = getHpBarAction;
        _setupCarOnServerAction = setupCarOnServerAction;
        _setSpeedParametersAction = setSpeedParametersAction;
        _setCarViewAction = setCarViewAction;
        _setFuelBarsAction = setFuelBarsAction;
    }
    public NetworkObject InstantiateObject(bool isSpawn,NetworkObject spawnObject, Vector2 spawnPosition = default,
        Quaternion rotation = default) {
        var needObject = Instantiate(spawnObject, spawnPosition,rotation);
        
        if (isSpawn)
            needObject.Spawn(true);
        return needObject;
    }
    [ServerRpc(RequireOwnership = false)]
    public void GetFloatingTextPrefabServerRpc(Vector2 position, string text, Color textColor, ServerRpcParams rpcParams = default) {
      
        var floatingText = _floatingTextsPool.Get();
        if (!floatingText.IsSpawned) {
             floatingText.Spawn();
             floatingText.TrySetParent(floatingTextMapContainer);
        }
           
        SetFloatingTextClientRpc(position, text, textColor, floatingText);
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void GetHpBarPrefabServerRpc(Vector2 position, float healthPercent, int cellEntity, float healthPercentBefore,ServerRpcParams rpcParams = default) {
      
        var hpBar = _hpBarsPool.Get();
        if (!hpBar.IsSpawned) {
            hpBar.Spawn();
            hpBar.TrySetParent(_hpBarsContainer);
        }
        //hpBar.transform.position = position;
       // hpBar.transform.gameObject.SetActive(true);

       _getHpBarAction.Invoke(hpBar,cellEntity);
        SetHPBarClientRpc(hpBar, position, healthPercent, cellEntity, rpcParams.Receive.SenderClientId, healthPercentBefore);
    }
    [Rpc(SendTo.ClientsAndHost)]
    private void SetFloatingTextClientRpc(Vector2 position, string text, Color textColor, NetworkObjectReference floatingTextNO,int entity = -1, ulong clientId = 0) {
        floatingTextNO.TryGet(out var networkObject);
       
        var floatingText = networkObject.GetComponent<TMP_Text>();
        floatingText.text = text;
        floatingText.transform.position = position;
        floatingText.color = textColor; 
        networkObject.gameObject.SetActive(DataPersistenceManagerSystem._gameData.UseDamageNumbers);
        Sequence sequence = DOTween.Sequence();
        sequence.Append(floatingText.transform.DOLocalMoveY(position.y + 1, 0.7f).SetEase(Ease.OutCubic));
        sequence.Join(floatingText.DOColor(new Color(textColor.r, textColor.g, textColor.b, 0), 0.7f));
        sequence.OnComplete(() => {
            if (IsServer)
                ReleaseFloatingText(floatingTextNO);
        });
        
        sequence.Play();
        
        if(entity != -1 && clientId == NetworkManager.LocalClientId)
            _laserTextGetAction.Invoke(networkObject,sequence, entity);
    }
    [Rpc(SendTo.ClientsAndHost)]
    public void SetHPBarClientRpc(NetworkObjectReference imageNetworkObject,Vector2 position, float healthPercent, int cellEntity, ulong needClientId,
        float healthPercentBefore = -1) {
        imageNetworkObject.TryGet(out var networkObject);
        networkObject.transform.gameObject.SetActive(DataPersistenceManagerSystem._gameData.UseHpBars);
        var hpBarImage = networkObject.transform.GetChild(0).GetComponent<Image>();
        networkObject.transform.position = position;
        var needSequence = networkObject.gameObject.GetComponent<SequenceStoreView>();
        needSequence.ImageSequence?.Kill();
        needSequence.ImageSequence = DOTween.Sequence();
        if (healthPercentBefore != -1) {
            hpBarImage.fillAmount = healthPercentBefore;
            hpBarImage.color = _healthGradient.Evaluate(healthPercentBefore);
        }
        Debug.Log(hpBarImage + " hpBarImage");

        needSequence.ImageSequence.Append(hpBarImage.DOFillAmount(healthPercent, 0.7f));
        needSequence.ImageSequence.Join(hpBarImage.DOColor(_healthGradient.Evaluate(healthPercent), 0.7f));
        // Первая группа завершилась, запускаем вторую
        // hpBarImage.DOColor(_healthGradient.Evaluate(healthPercent), 0)
        //   .SetDelay(0.3f); // Мгновенно, так как параллельно с затуханием
        needSequence.ImageSequence.Append(hpBarImage.transform.parent.GetComponent<Image>().DOFade(0, 0.3f));
        needSequence.ImageSequence.Join(hpBarImage.DOFade(0, 0.3f)).OnComplete(() => {
            if (IsServer)
                ReleaseHpBar(imageNetworkObject);
            networkObject.gameObject.SetActive(false);
            if (NetworkManager.Singleton.LocalClientId == needClientId)
                _deleteHpBarComponentAction.Invoke(cellEntity);
        });
        
       
    networkObject.GetComponent<Image>().color = new Color(0.22f, 0.22f, 0.22f, 1);
// Запускаем цвет параллельно с заполнением
   
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void SetupCarViewToClientRpc(NetworkObjectReference carView, ulong clientId) {
        if (NetworkManager.Singleton.LocalClientId == clientId) {
            carView.TryGet(out var networkObject);
        
            _setCarViewAction.Invoke(networkObject.transform.GetChild(0).GetComponent<CarView>());
        }
            
    }
    
    [Rpc(SendTo.NotOwner)]
    public void SetFuelValuesClientRpc( FuelDataForRpc[] fuelDataForClients) {
        for (int i = 0; i < fuelDataForClients.Length; i++) {
            var fuelData = fuelDataForClients[i];
            if (NetworkManager.Singleton.LocalClientId == fuelData.UserId) {
     
                _setFuelBarsAction.Invoke(fuelData.Fuel, fuelData.MaxFuel, fuelData.Electricity, fuelData.MaxElectricity);
                break;
            }
        }
    }


    private void ReleaseHpBar(NetworkObject imageNetworkObject) {
        _hpBarsPool.Release(imageNetworkObject);
    }
    public void ReleaseFloatingText(NetworkObjectReference textObject) {
       _floatingTextsPool.Release(textObject);
    }
    public void ShowMessageClient(string text) {
        if (NetworkManager.Singleton.IsHost) return;
        clientsMessegeText.text = GetLocalization(text);
        clientsMessegeText.transform.parent.gameObject.SetActive(true);
    }
    
    public void HideMessageClient() {
        if (NetworkManager.Singleton.IsHost) return;
        clientsMessegeText.transform.parent.gameObject.SetActive(false);
    }
    private string GetLocalization(string needString) => BetweenScenesService.Instance.CsvLocalization.GetLocalizedValue(needString);
    [ClientRpc]
    public void HideMessageClientRpc() {
        if (NetworkManager.Singleton.IsHost) return;
        clientsMessegeText.transform.parent.gameObject.SetActive(false);
    }
    [ServerRpc(RequireOwnership = false)]
    public void InstantiateCarViewServerRpc( int entity,bool isPlayer,ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        
        var carTransform = Instantiate(_carView).GetComponent<NetworkObject>();
        carTransform.Spawn();
       // carTransform.SpawnWithOwnership(clientId);
        SendCarToClientRpc(clientId,carTransform, entity, isPlayer);
    }
    
    [Rpc(SendTo.ClientsAndHost)]
    private void SendCarToClientRpc(ulong targetClientId, NetworkObjectReference carReference, int entity, bool isPlayer)
    {
        // Этот RPC выполнят все клиенты, но нас интересует только целевой
        if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
  
        if (carReference.TryGet(out NetworkObject netObj))
        {    
            var carView = netObj.GetComponentInChildren<CarView>();
            _carSetupAction.Invoke(carView, entity, isPlayer);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SpawnCarPartSpritesServerRpc(int mainEntity, int cellEntity, int carPartId, int rotateAngle, bool isPlayer,
        int containerSizeX, int containerSizeY, NetworkObjectReference carView, int[] carpartStuffPositionsX, int[] carpartStuffPositionsY,
        ServerRpcParams rpcParams = default) {
        ulong clientId = rpcParams.Receive.SenderClientId;
        SpawnCarPartSpritesClientRpc(clientId,mainEntity,  cellEntity, carPartId, rotateAngle,  isPlayer, containerSizeX,
            containerSizeY, carView,  carpartStuffPositionsX, carpartStuffPositionsY);
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void SpawnCarPartSpritesClientRpc(ulong clientId, int mainEntity, int cellEntity, int carPartId, int rotateAngle, bool isPlayer,
        int containerSizeX, int containerSizeY, NetworkObjectReference carView, int[] carpartStuffPositionsX, int[] carpartStuffPositionsY) {
        var carPartConfig = BetweenScenesService.Instance._carStuffListsConfig.CarPartConfigs[carPartId];

        var positionsList = new List<Vector2Int>();
        for (int i = 0; i < carpartStuffPositionsX.Length; i++)
            positionsList.Add(new Vector2Int(carpartStuffPositionsX[i], carpartStuffPositionsY[i]));
        bool isContainer = GridUtils.IsContainerType(carPartConfig);
        var cellCarPartsStuffViews = new List<CarPartStuffView>();
        carView.TryGet(out NetworkObject carViewObject);
        var carViewCmp = carViewObject.transform.GetComponentInChildren<CarView>();
        if (carViewCmp.CarPartStuffViews == null) 
            carViewCmp.CarPartStuffViews = new CarPartStuffView[containerSizeX, containerSizeY];
        
        for (int i = 0; i < carpartStuffPositionsX.Length; i++) {
            var needGridPosition = new Vector2Int(positionsList[i].x, positionsList[i].y);
            if (isContainer) {
                var carPartView = _carPartsPool.Get();
                cellCarPartsStuffViews.Add(carPartView);
                // if (NetworkManager.Singleton.LocalClientId == clientId) {
                carPartView.transform.SetParent(carViewCmp.CarPartsContainer.transform);
                carPartView.transform.localPosition = new Vector2(needGridPosition.x, needGridPosition.y) -
                                                      new Vector2(containerSizeX / 2f, containerSizeY / 2f);
                carPartView.transform.localRotation = Quaternion.Euler(0, 0, rotateAngle);

                string needLayer = isPlayer ? "CarBase" : "CarBaseEnemy";
                carPartView.SetupCarPart(needGridPosition, needLayer, mainEntity, clientId);

                carPartView.SetColliderEnableState(true);
                carPartView.gameObject.tag = isPlayer ? "Player" : "Enemy";
                carViewCmp.CarPartStuffViews[needGridPosition.x, needGridPosition.y] = carPartView;
            } else {
                string needLayer = isPlayer ? "UpCarPart" : "UpCarPartEnemy";
                var carPartView = carViewCmp.CarPartStuffViews[needGridPosition.x, needGridPosition.y];
                cellCarPartsStuffViews.Add(carPartView);
                carPartView.SetupCarPart(needGridPosition, needLayer, mainEntity,clientId);
                carPartView.transform.localRotation = Quaternion.Euler(0, 0, rotateAngle);
            }
        }

        SpriteRenderer spriteBase = _spriteRenderersPool.Get();
        carViewCmp.CarPartSprites.Add(cellEntity, spriteBase);
        Vector2Int carPartSize = GridUtils.GetCarPartSize(carPartConfig);

        spriteBase.transform.SetParent(carViewCmp.CarPartsContainer.transform);
        if (rotateAngle == 90 || rotateAngle == 270)
            carPartSize = new Vector2Int(carPartSize.y, carPartSize.x);
        spriteBase.transform.localPosition = (Vector3Int)GridUtils.GetEdgePointFromCarPart(positionsList) +
                                             new Vector3((carPartSize.x - containerSizeX) / 2f - 0.5f,
                                                 (carPartSize.y - containerSizeY) / 2f - 0.5f);
        spriteBase.transform.localRotation = Quaternion.Euler(0, 0, rotateAngle);
        spriteBase.sprite = carPartConfig.PartSprite;
        int needSpriteLevel = isContainer ? 1 : 2;
        spriteBase.sortingOrder = needSpriteLevel;

        Transform upPartContainer = null; //

        Transform firePointTransform = null;
        SpriteRenderer weaponSpriteRenderer = null;
        LineRenderer laserLineRenderer = null;
        MeleeCarPartStuffView meleeCarPartStuffView = null;
        if (carPartConfig.PartType == CarPartType.Gun) {
            if (carPartConfig is GunConfig gunConfig) {
                if (gunConfig.UpPartSprite != null) {
                    upPartContainer = _gunContainersPool.Get();
                    upPartContainer.SetParent(spriteBase.transform);
                    upPartContainer.transform.localPosition = gunConfig.GunContainerPosition;
                    upPartContainer.transform.localRotation = Quaternion.Euler(0, 0, 0);
                    firePointTransform = _gunContainersPool.Get();

                    weaponSpriteRenderer = _spriteRenderersPool.Get();

                    firePointTransform.SetParent(upPartContainer);
                    firePointTransform.transform.localPosition = gunConfig.FirePointPosition - gunConfig.GunContainerPosition;
                    firePointTransform.transform.localRotation = Quaternion.Euler(0, 0, 0);

                    weaponSpriteRenderer.transform.SetParent(upPartContainer);
                    weaponSpriteRenderer.transform.localPosition = gunConfig.UpPartPosition - gunConfig.GunContainerPosition;
                    weaponSpriteRenderer.transform.localRotation = Quaternion.Euler(0, 0, 0);

                    weaponSpriteRenderer.sprite = gunConfig.UpPartSprite;
                    weaponSpriteRenderer.sortingOrder = 3;
                } else {
                    firePointTransform = _gunContainersPool.Get();
                    firePointTransform.SetParent(spriteBase.transform);
                    firePointTransform.transform.localPosition = gunConfig.FirePointPosition;
                    firePointTransform.transform.localRotation = Quaternion.Euler(0, 0, 0);
                }

                if (gunConfig.MissleType == MissleType.laser) {
                    laserLineRenderer = _laserLineRenderersPool.Get();
                    laserLineRenderer.transform.SetParent(notUsedNetworkObjectsContainer);
                    laserLineRenderer.gameObject.SetActive(false);
                }
            }
        } else if (carPartConfig.PartType == CarPartType.MeleeWeapon) {
            if (carPartConfig is MeleeWeaponConfig meleeConfig) {
                if (meleeConfig.UpSprite != null) {
                    weaponSpriteRenderer = _spriteRenderersPool.Get();
                    weaponSpriteRenderer.sprite = meleeConfig.UpSprite;
                    weaponSpriteRenderer.transform.SetParent(spriteBase.transform);
                    weaponSpriteRenderer.transform.localRotation = Quaternion.Euler(0, 0, 0);
                    weaponSpriteRenderer.sortingOrder = 3;
                    weaponSpriteRenderer.transform.localPosition = meleeConfig.UpSpritePosition;
                }

//
                meleeCarPartStuffView = _meleeCarPartStuffViewsPool.Get();
                meleeCarPartStuffView.transform.SetParent(spriteBase.transform);
                meleeCarPartStuffView.transform.localRotation = Quaternion.Euler(0, 0, 0);
                meleeCarPartStuffView.transform.localPosition = Vector3.zero;
                meleeCarPartStuffView.MakeTriggerCollider();
                meleeCarPartStuffView.SetColliderEnableState(true);
            }
        }

        if (NetworkManager.Singleton.LocalClientId == clientId) {
            Transform firePointTransformToAction = firePointTransform ? firePointTransform.transform : null;
            // Transform weaponContainerTransformToAction = weaponContainerTransform ? weaponContainerTransform.transform:null;
            _carPartSetupAction.Invoke(cellEntity, spriteBase, upPartContainer, weaponSpriteRenderer, laserLineRenderer,
                cellCarPartsStuffViews.ToArray(), firePointTransformToAction, meleeCarPartStuffView);
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void SpawnWheelsClientRpc(NetworkObjectReference carViewNO, float[] wheelsPositionsX, float[] wheelsPositionsY, float maxY,
        int[] wheelIds, int[] wheelComponentEntities, ulong clientId) {
        carViewNO.TryGet(out NetworkObject carViewObject);
        var carView = carViewObject.GetComponentInChildren<CarView>();
        List<Animator> forwardWheels = new List<Animator>();
        Animator[] allWheels = new Animator[wheelIds.Length];
        ParticleSystem[] wheelParticles = new ParticleSystem[wheelsPositionsX.Length];
        for (int i = 0; i < wheelsPositionsY.Length; i++) {
            var wheel = CreateWheel(wheelIds[i], carView, new Vector2(wheelsPositionsX[i], wheelsPositionsY[i]), wheelsPositionsX[i] > 0);
            Debug.Log((int)wheel.transform.localPosition.y +"=="+ (int)maxY);
            if ((int)wheel.transform.localPosition.y == (int)maxY)
                forwardWheels.Add(wheel);
            if (!carView.Wheels.TryAdd(wheelComponentEntities[i], new List<Transform> { wheel.transform }))
                carView.Wheels[wheelComponentEntities[i]].Add(wheel.transform); 
            var wheelParticle = wheel.GetComponentInChildren<ParticleSystem>();
            wheelParticles[i] = wheelParticle;
            allWheels[i] = wheel;
            var mainParticle = wheelParticle.main;
            mainParticle.startColor = BetweenScenesService.Instance.GetMapConfig().MapVisualInfoConfigs[0].WheelParticlesColor;
        }

        carView.SetupForwardWheels(forwardWheels.ToArray(), wheelParticles);
        
        if (NetworkManager.Singleton.LocalClientId == clientId) {
            _wheelComponentsSetupAction.Invoke(allWheels, wheelComponentEntities);
        }
    }
    
    private Animator CreateWheel(int wheelId, CarView carView, Vector2 pos, bool isLeftSide) {
        Debug.Log("wheel id" + wheelId);
        var wheelTransform =Instantiate(BetweenScenesService.Instance._carStuffListsConfig.Wheels[wheelId].WheelAnimator,
            carView.ForwardWheelsContainer.transform);
        AnimatorClipInfo[] clipInfo = wheelTransform.GetCurrentAnimatorClipInfo(0);
        float clipLength = clipInfo[0].clip.length;

        float randomTime = Random.Range(0f, clipLength);
        wheelTransform.Play("Spin", 0, randomTime / clipLength);

        //   int scaleMultiplayer = pos.x > 0 ? 1 : -1;
        int scaleMultiplayer = isLeftSide ? 1 : -1;
        wheelTransform.transform.localScale = new Vector3(scaleMultiplayer, 1f, 1f);
        wheelTransform.transform.localPosition = pos;
        return wheelTransform;
        
       
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
              carView.TryGet(out NetworkObject carViewObject);
              carPartStuffView.transform.SetParent(carViewObject.transform.GetComponentInChildren<CarView>().CarPartsContainer.transform);
              carPartStuffView.transform.localPosition = new Vector2(needGridPosition.x, needGridPosition.y) -
                                                         new Vector2(containersSize.x / 2f, containersSize.y / 2f);
              carPartStuffView.transform.localRotation = Quaternion.Euler(0, 0, needRotate);

              string needLayer = isPlayer ? "CarBase" : "CarBaseEnemy";
              carPartStuffView.SetupCarPart(needGridPosition, needLayer, mainEntity,0);

              carPartStuffView.SetColliderEnableState(true);
              carPartStuffView.gameObject.tag = isPlayer ? "Player" : "Enemy";
              //  carCmp.CarPartViewsGrid[needGridPosition.x, needGridPosition.y] = carPart;
          } else {
              string needLayer = isPlayer ? "UpCarPart" : "UpCarPartEnemy";
              carPartStuffView.SetupCarPart(needGridPosition, needLayer, mainEntity,0);
              carPart.transform.localRotation = Quaternion.Euler(0, 0, needRotate);
          }

          carPartStuffsList[carPartStuffIndex] = carPartStuffView;
          carPartStuffIndex++;
        }
//
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
            //
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

    [ServerRpc(RequireOwnership = false)]
    public void SetCarToServerRpc (CarPartDataForRpc[]upParts,CarPartDataForRpc[] downParts, int carLevel, bool isPlayer, int carId, Vector2 carSize, ServerRpcParams rpcParams = default){
        _setupCarOnServerAction.Invoke(upParts, downParts, carLevel, rpcParams.Receive.SenderClientId, isPlayer, carId,carSize);
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void SetCarMovementServerRpc ( Vector2 targetPosition,bool isSpeedBoost, ServerRpcParams rpcParams = default){
        _setSpeedParametersAction.Invoke( rpcParams.Receive.SenderClientId, targetPosition, isSpeedBoost);
    }
    public Transform InstantiateObject(Transform spawnObject, Transform parent, Vector2 spawnPosition = default)
    {
        return Instantiate(spawnObject, spawnPosition, Quaternion.identity, parent).transform;
    }
}

[System.Serializable]
public struct CarPartDataForRpc : INetworkSerializable
{
    public Vector2Int EdgePosition;
    public int RotateAngle;
    // Массив позиций
    public int[] Modules; 
    public int Id;
    public int Level;
    public int Durability;
    public int ClientEntity;

    public CarPartDataForRpc(Vector2Int edgePosition,int rotateAngle, int[] modules,int id,int level, int durability,int clientEntity) {
        EdgePosition = edgePosition;
        RotateAngle = rotateAngle;
        Modules = modules;
        Id = id;
        Level = level;
        Durability = durability;
        ClientEntity = clientEntity;
    }
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        // Сериализация массива
       
        int modulesLength = Modules?.Length ?? 0;
        serializer.SerializeValue(ref modulesLength);
        
        if (serializer.IsReader)
            Modules = new int[modulesLength];
        
        for (int i = 0; i < modulesLength; i++)
            serializer.SerializeValue(ref Modules[i]);
        
        // Сериализация остальных полей
        serializer.SerializeValue(ref EdgePosition);
        serializer.SerializeValue(ref RotateAngle);
        serializer.SerializeValue(ref Id);
        serializer.SerializeValue(ref Level);
        serializer.SerializeValue(ref Durability);
        serializer.SerializeValue(ref ClientEntity);
    }
}

[System.Serializable]
public struct FuelDataForRpc : INetworkSerializable
{
   public ulong UserId;
    public float Electricity;
    public float MaxElectricity;
    public float Fuel;
    public float MaxFuel;

    public FuelDataForRpc(ulong userId, float electricity, float maxElectricity, float fuel, float maxFuel) {
        UserId = userId;
        Electricity = electricity;
        MaxElectricity = maxElectricity;
        Fuel = fuel;
        MaxFuel = maxFuel;
    }
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref UserId);
        serializer.SerializeValue(ref Electricity);
        serializer.SerializeValue(ref MaxElectricity);
        serializer.SerializeValue(ref Fuel);
        serializer.SerializeValue(ref MaxFuel);
    }
}