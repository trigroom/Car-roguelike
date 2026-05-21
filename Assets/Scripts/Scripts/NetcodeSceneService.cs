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
   private Action<Vector2Int, float, bool> _setCarPartHealthPointAction;
   private Action<Vector2Int, float, bool,int> _setCarPartHealthToClientAction;
   private Action<float, float, float, float> _setFuelBarsAction;
   private Action<Vector2, AudioClip> _playSoundToPlayer;
   private Action<int, SpriteRenderer, Transform, SpriteRenderer, LineRenderer, CarPartStuffView[], Transform, MeleeCarPartStuffView>
       _carPartSetupAction;

   private Action<Vector2> _setMapPointForClients;
   private Action _openSupportBoxForClientsAction;
   private Action<SpriteRenderer> _destroyCarPartAction;
   private Action<Vector2> _setZonePositionAction;
   private Action<float> _setZoneFillAmountAction;
   private Action _endRoundForClientAction;
   private Action _setDeathPlayersCountAction;
   private Action _setLoseStateForAllAction;
   private Action<bool> _setIsReadyCountAction;
   private Action _setMinimumHealthLevelAction;
   private Action< Vector2 , int>  _setEnemyAirplanePosition;
   private Action<bool> _endVersusBattleAction;
   private Action<float> _endRoundExpAddAction;
   private Action<CarPartsHpVersusDataForRpc[], Vector2 ,Vector2 > _setupVersusBattleUiAction;
   private Action<Vector2Int[], CarPartType, bool, float, float> _setVersusBattleUiAction;
   private Action<CarPartDataForRpc[], CarPartDataForRpc[], int, ulong, bool, int, Vector2> _setupCarOnServerAction;
   private Action< float, float,Vector2, int> _setMovementToTornadoAction;
   private Action<int ,float ,Vector2 ,int> _spawnTornadoAction;
   private Action _endMapGenerateAction;
   private Action<Vector2> _spawnThunderAction;
   private Action<Vector2> _sendBomberPlaneAction;
   
   private ObjectPool<CarPartStuffView> _carPartsPool;
   private ObjectPool<Transform> _gunContainersPool;
   private ObjectPool<SpriteRenderer> _spriteRenderersPool;
   private ObjectPool<LineRenderer> _laserLineRenderersPool;
   private ObjectPool<MeleeCarPartStuffView> _meleeCarPartStuffViewsPool;
   private ObjectPool<NetworkObject> _floatingTextsPool;
   private ObjectPool<ProjectileView> _projectilesPool;
   private ObjectPool<NetworkObject> _hpBarsPool;
  
    private void Awake() {
        if (NetcodeBetweenScenesService.Instance != null)
            NetcodeBetweenScenesService.Instance.NetcodeSceneService = this;

        if (!networkObjectsMapContainer.IsSpawned && !BetweenScenesService.Instance.IsCoop)
            networkObjectsMapContainer.Spawn();
        
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

    private void Start() {
        if (!BetweenScenesService.Instance.IsCoop) {
            if (TryGetComponent(out NetworkObject netObj) && !netObj.IsSpawned)
                netObj.Spawn();

            if (_hpBarsContainer.TryGetComponent(out NetworkObject containerObj))
                containerObj.Spawn();
        }
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
   /* [Rpc(SendTo.NotServer)]
    public void SetProjectileVisualClientRpc(NetworkObjectReference networkObject, bool isEnemy, int gunId, Vector2 projectileSpeed,
        float angle,  Vector2 worldFirePoint) {
        networkObject.TryGet(out var projectileNetworkObject);

        var projectile = projectileNetworkObject.GetComponent<ProjectileView>();
        projectile.rigidbody2D.linearVelocity = projectileSpeed;
        projectile.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        projectile.transform.position =  worldFirePoint;
        if (BetweenScenesService.Instance._carStuffListsConfig.CarPartConfigs[gunId] is GunConfig gunConfig)
            projectile.SetProjectileVisual(gunConfig.MissleSprite, isEnemy ? "Enemy" : "Player",
                isEnemy
                    ? BetweenScenesService.Instance._carStuffListsConfig.DefaultMaterial
                    : BetweenScenesService.Instance._carStuffListsConfig.EnemyProjectilesMaterial);
    }*/
 

   [Rpc(SendTo.NotServer)]
   public void SetProjectileVisualClientRpc(NetworkObjectReference[] networkObject, bool isEnemy, int gunId, Vector2[] projectileSpeed, float[] angle,
       Vector2 worldFirePoint) {
       for (int i = 0; i < networkObject.Length; i++) 
            ExecuteSetProjectileVisual(networkObject[i], isEnemy, gunId, projectileSpeed[i], angle[i], worldFirePoint);
       if (BetweenScenesService.Instance._carStuffListsConfig.CarPartConfigs[gunId] is GunConfig gunConfig) {
           _playSoundToPlayer.Invoke(worldFirePoint, gunConfig.ShotSound);
       }

       
   }

   private void ExecuteSetProjectileVisual(NetworkObjectReference networkObject, bool isEnemy, int gunId, Vector2 projectileSpeed, float angle,
       Vector2 worldFirePoint) {
       if (!networkObject.TryGet(out NetworkObject projectileNetworkObject)) return;

       var projectile = projectileNetworkObject.GetComponent<ProjectileView>();
       projectile.rigidbody2D.linearVelocity = projectileSpeed;
       projectile.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
       projectile.transform.position = worldFirePoint;

       if (BetweenScenesService.Instance._carStuffListsConfig.CarPartConfigs[gunId] is GunConfig gunConfig) {
           projectile.SetProjectileVisual(gunConfig.MissleSprite, isEnemy ? "Enemy" : "Player",
               isEnemy
                   ? BetweenScenesService.Instance._carStuffListsConfig.DefaultMaterial
                   : BetweenScenesService.Instance._carStuffListsConfig.EnemyProjectilesMaterial);
       }
   }

   [Rpc(SendTo.ClientsAndHost)]
   public void ReleaseGunProjectileClientRpc(NetworkObjectReference networkObject, float radius, int projectileSpritesIndex = 0)
   {
       ExecuteReleaseGunProjectile(networkObject, radius, projectileSpritesIndex);
   }

   private void ExecuteReleaseGunProjectile(NetworkObjectReference networkObject, float radius, int projectileSpritesIndex)
   {
       if (!networkObject.TryGet(out NetworkObject projectileNetworkObject)) return;
      
       var projectile = projectileNetworkObject.GetComponent<ProjectileView>();
       projectile.rigidbody2D.linearVelocity = Vector2.zero;
       PlayHitAnimation(projectile, radius, projectileSpritesIndex);
   }

    private void PlayHitAnimation(ProjectileView projectile, float radius ,int projectileSpritesIndex = 0) {
        Sprite[] animation =projectileSpritesIndex == 0? null: _bulletSprites;
        projectile.GetComponent<ProjectileView>().PlayHitAnimation(animation,radius, RelaseProjectileYourself);
    }

    public void RelaseProjectileYourself(ProjectileView projectile) {
        if (!projectile.gameObject.activeInHierarchy) return;
        projectile.gameObject.SetActive(false);
        if (!NetworkManager.IsServer) return;
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
            carPartSetupAction, Action<CarPartDataForRpc[], CarPartDataForRpc[], int, ulong, bool, int, Vector2> setupCarOnServerAction,
        Action<ulong, Vector2, bool> setSpeedParametersAction, Action<CarView> setCarViewAction,
        Action<float, float, float, float> setFuelBarsAction, Action<Vector2Int, float, bool> setCarPartHealthPointAction,
        Action<SpriteRenderer> destroyCarPartAction, Action<Vector2> setZonePositionAction, Action<float> setZoneFillAmountAction,
        Action<Vector2> setMapPointForClients, Action endRoundForClientAction,
        Action<Vector2Int, float, bool, int> setCarPartHealthToClientAction, Action<Vector2, int> setEnemyAirplanePosition,
        Action<bool> endVersusBattleAction, Action<CarPartsHpVersusDataForRpc[], Vector2 ,Vector2> setupVersusBattleUiAction,
        Action<bool> setIsReadyCountAction,Action endMapGenerateAction) {
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
        _setCarPartHealthPointAction = setCarPartHealthPointAction;
        _destroyCarPartAction = destroyCarPartAction;
        _setZonePositionAction = setZonePositionAction;
        _setZoneFillAmountAction = setZoneFillAmountAction;
        _setMapPointForClients = setMapPointForClients;
        _endRoundForClientAction = endRoundForClientAction;
        _setCarPartHealthToClientAction = setCarPartHealthToClientAction;
        _setEnemyAirplanePosition = setEnemyAirplanePosition;
        _endVersusBattleAction = endVersusBattleAction;
        _setupVersusBattleUiAction = setupVersusBattleUiAction;
        _setIsReadyCountAction = setIsReadyCountAction;
        _endMapGenerateAction = endMapGenerateAction;
    }
    [ServerRpc(RequireOwnership = false)]
    public void SetReadyActionServerRpc(bool isReady) {
        _setIsReadyCountAction.Invoke(isReady);
    }
    public void SetupServiceFromAttackSystem(Action setDeathPlayersCountAction, Action setLoseStateForAllAction,
        Action<Vector2Int[], CarPartType, bool, float, float> setVersusBattleUiAction,Action openSupportBoxForClientsAction,
        Action<int,float ,Vector2 ,int> spawnTornadoAction,Action<Vector2> spawnThunderAction,Action<Vector2> sendBomberPlaneAction) {
        _setDeathPlayersCountAction = setDeathPlayersCountAction;
        _setLoseStateForAllAction = setLoseStateForAllAction;
        _setVersusBattleUiAction = setVersusBattleUiAction;
        _openSupportBoxForClientsAction = openSupportBoxForClientsAction;
        _spawnTornadoAction = spawnTornadoAction;
        _spawnThunderAction = spawnThunderAction;
        _sendBomberPlaneAction = sendBomberPlaneAction;
    }
    
    public void SetupServiceFromInventorySystem(Action setMinimumHealthLevelAction,Action <float>endRoundExpAddAction,Action <Vector2, AudioClip>playSoundToPlayer) {
        _setMinimumHealthLevelAction = setMinimumHealthLevelAction;
        _endRoundExpAddAction = endRoundExpAddAction;
        _playSoundToPlayer = playSoundToPlayer;
    }
    public void SetupServiceFromMovementSystem(Action <float, float,Vector2, int> setMovementToTornadoAction) {
        _setMovementToTornadoAction = setMovementToTornadoAction;
    }
    public NetworkObject InstantiateObject(bool isSpawn,NetworkObject spawnObject, Vector2 spawnPosition = default,
        Quaternion rotation = default) {
        var needObject = Instantiate(spawnObject, spawnPosition,rotation);
        
        if (isSpawn)
            needObject.Spawn(true);
        return needObject;
    }

    [ServerRpc(RequireOwnership = false, DeferLocal = true)]
    public void GetFloatingTextPrefabServerRpc(Vector2 position, string text, Color textColor, ServerRpcParams rpcParams = default) {
        var floatingText = _floatingTextsPool.Get();
        if (!floatingText.IsSpawned) {
            floatingText.Spawn();
            floatingText.TrySetParent(floatingTextMapContainer);
        }

        SetFloatingTextClientRpc(position, text, textColor, floatingText);
    }
    [Rpc(SendTo.ClientsAndHost)]
    public void SendAirplaneClientRpc(Vector2 position, int needAirplaneIndex) {
       
        _setEnemyAirplanePosition.Invoke(position, needAirplaneIndex);
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void EndGenerateMapClientRpc() {
        _endMapGenerateAction.Invoke();
    }

    [Rpc(SendTo.NotServer)]
    public void SetTornadoMovementValuesClientRpc(Vector2 targetPosition, float targetSpeed, float timeToChangeDirection, int hostEntity) {
        _setMovementToTornadoAction.Invoke(targetSpeed, timeToChangeDirection, targetPosition, hostEntity);
    }
    [Rpc(SendTo.NotServer)]
    public void SpawnThunderClientRpc(Vector2 targetPosition) {
        _spawnThunderAction.Invoke(targetPosition);
    }
    [Rpc(SendTo.NotServer)]
    public void SpawnTornadoClientRpc(int tornadoEntity, float lifeTime, Vector2 spawnPosition, int tornadoIndex) {
        _spawnTornadoAction.Invoke(tornadoEntity,lifeTime, spawnPosition, tornadoIndex);
    }
    [Rpc(SendTo.ClientsAndHost)]
    public void EndRoundExpAddClientRpc(float addedExp) {
       
        _endRoundExpAddAction.Invoke(addedExp);
    }
    
    [Rpc(SendTo.ClientsAndHost)]
    private void SetFloatingTextClientRpc(Vector2 position, string text, Color textColor, NetworkObjectReference floatingTextNO,
        int entity = -1, ulong clientId = 0) {
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

        if (entity != -1 && clientId == NetworkManager.LocalClientId)
            _laserTextGetAction.Invoke(networkObject, sequence, entity);
    }
    
    
    
    [ServerRpc(RequireOwnership = false)]
    public void SetDeathPlayersCountServerRpc() {
      _setDeathPlayersCountAction.Invoke();
    }
    
    [Rpc(SendTo.ClientsAndHost)]
    public void SetLoseStateForAllClientRpc() {
        _setLoseStateForAllAction.Invoke();
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void GetHpBarPrefabServerRpc(NetworkObjectReference carNetworkObject,Vector2 position, float healthPercent, int cellEntity, float healthPercentBefore,ServerRpcParams rpcParams = default) {
      
        var hpBar = _hpBarsPool.Get();
        if (!hpBar.IsSpawned) {
            hpBar.Spawn();
            hpBar.TrySetParent(_hpBarsContainer);
        }
        //hpBar.transform.position = position;
       // hpBar.transform.gameObject.SetActive(true);

       _getHpBarAction.Invoke(hpBar,cellEntity);
        SetHPBarClientRpc(carNetworkObject,hpBar, position, healthPercent, cellEntity, rpcParams.Receive.SenderClientId, healthPercentBefore);
    }
    [Rpc(SendTo.NotServer)]
    public void SpawnTrainClientRpc(NetworkObjectReference trainTransform) {
        trainTransform.TryGet(out var networkObject);
       
        networkObject.gameObject.SetActive(false);
    }
    
    [Rpc(SendTo.ClientsAndHost)]
    public void MoveTrainClientRpc(Vector2 startPosition, Vector2 endPosition, NetworkObjectReference trainTransform,float rotateAngle) {
        trainTransform.TryGet(out var networkObject);
        networkObject.transform.localEulerAngles =  new Vector3(0, 0, rotateAngle );
        networkObject.transform.position =startPosition;
        if (endPosition.x == 0)
            networkObject.transform.DOMoveY(endPosition.y, 12);
        else 
            networkObject.transform.DOMoveX(endPosition.x, 12);
        
        networkObject.gameObject.SetActive(true);
    }
    [Rpc(SendTo.ClientsAndHost)]
    public void SetHPBarClientRpc(NetworkObjectReference carNetworkObject,NetworkObjectReference imageNetworkObject,Vector2 position, float healthPercent, int cellEntity, ulong needClientId,
        float healthPercentBefore = -1) {
        imageNetworkObject.TryGet(out var networkObject);
        networkObject.transform.gameObject.SetActive(DataPersistenceManagerSystem._gameData.UseHpBars);
        var hpBarImage = networkObject.transform.GetChild(0).GetComponent<Image>();
        networkObject.transform.position = position;
        var needSequence = networkObject.gameObject.GetComponent<SequenceStoreView>();
        needSequence.ImageSequence?.Kill();
        needSequence.ImageSequence = DOTween.Sequence();
      
        if (!IsServer) { 
            carNetworkObject.TryGet(out var carNetworkObjectTransform);
            var childTransform =  carNetworkObjectTransform.transform.GetChild(0);
            needSequence.TargetTransform = childTransform;  
            Vector3 localOffset = childTransform.InverseTransformPoint(position);
            needSequence.OffsetPosition =  localOffset;
          
        //    Debug.Log("offset pos" + localOffset);
        }
        if (healthPercentBefore != -1) {
            hpBarImage.fillAmount = healthPercentBefore;
            hpBarImage.color = _healthGradient.Evaluate(healthPercentBefore);
        }
//        Debug.Log(hpBarImage + " hpBarImage");

        needSequence.ImageSequence.Append(hpBarImage.DOFillAmount(healthPercent, 0.7f));
        needSequence.ImageSequence.Join(hpBarImage.DOColor(_healthGradient.Evaluate(healthPercent), 0.7f));
        // Первая группа завершилась, запускаем вторую
        // hpBarImage.DOColor(_healthGradient.Evaluate(healthPercent), 0)
        //   .SetDelay(0.3f); // Мгновенно, так как параллельно с затуханием
        needSequence.ImageSequence.Append(hpBarImage.transform.parent.GetComponent<Image>().DOFade(0, 0.3f));
        needSequence.ImageSequence.Join(hpBarImage.DOFade(0, 0.3f)).OnComplete(() => {
            if (IsServer)
                ReleaseHpBar(imageNetworkObject);
            else
                needSequence.TargetTransform = null;
            
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

    [Rpc(SendTo.NotOwner)]
    public void BrokeCarPartSpritesClientRpc(NetworkObjectReference carView, int childIndex) {
        carView.TryGet(out var networkObject);
        if (networkObject.transform.GetChild(0).GetComponent<CarView>().CarPartsContainer.GetChild(childIndex).gameObject.TryGetComponent<SpriteRenderer>(out var spriteRenderer)) 
            _destroyCarPartAction.Invoke(spriteRenderer);
    }

    [Rpc(SendTo.NotOwner)]
    public void EndRoundClientRpc() {
        _endRoundForClientAction.Invoke();
    }
    [Rpc(SendTo.NotOwner)]
    public void SetMapPointClientRpc(Vector2 mapPointPosition) {
            _setMapPointForClients.Invoke(mapPointPosition);
    }
    [Rpc(SendTo.NotOwner)]
    public void SetZoneFillAmountClientRpc(float fillAmount) {
 
        _setZoneFillAmountAction.Invoke(fillAmount);
    }
    
    [Rpc(SendTo.NotOwner)]
    public void SetZonePositionClientRpc(Vector2 zonePosition) {
  
        _setZonePositionAction.Invoke(zonePosition);
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

   /* public void TryInstantiateCarView(int entity, bool isPlayer) {
        Debug.Log(this.IsSpawned + " isSpawned"+ NetworkManager.Singleton.LocalClientId + " is host"+ NetworkManager.Singleton.IsHost);
       // if (BetweenScenesService.Instance.IsCoop)
            InstantiateCarViewServerRpc(entity, isPlayer);
      //  else
        //    InstantiateCarView(entity, isPlayer);
    }

    public void InstantiateCarView(int entity, bool isPlayer) {
        var carTransform = Instantiate(_carView).GetComponent<NetworkObject>();
        carTransform.Spawn();

        var carView = carTransform.GetComponentInChildren<CarView>();
        _carSetupAction.Invoke(carView, entity, isPlayer);
    }*/
    
    [ServerRpc(RequireOwnership = false, DeferLocal = true)]
    public void InstantiateCarViewServerRpc( int entity,bool isPlayer,Vector2 spawnPosition, ServerRpcParams rpcParams = default)
    {
        
        ulong clientId =  rpcParams.Receive.SenderClientId;
        
        var carTransform = Instantiate(_carView).GetComponent<NetworkObject>();
        carTransform.Spawn();
        carTransform.transform.GetChild(0).transform.position = spawnPosition;
        SendCarToClientRpc(clientId,carTransform, entity, isPlayer);
    }

    [Rpc(SendTo.NotServer)]
    public void EndVersusBattleClientRpc(bool isRightWin) {
        _endVersusBattleAction.Invoke(isRightWin);
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

  /*  public void TrySpawnCarPartSprites(int mainEntity, int cellEntity, int carPartId, int rotateAngle, bool isPlayer, int containerSizeX,
        int containerSizeY, NetworkObjectReference carView, int[] carpartStuffPositionsX, int[] carpartStuffPositionsY) {
        if (BetweenScenesService.Instance.IsCoop) {
            ulong clientId = NetworkManager.Singleton.LocalClientId;
            SpawnCarPartSpritesClientRpc(clientId, mainEntity, cellEntity, carPartId, rotateAngle, isPlayer, containerSizeX, containerSizeY,
                carView, carpartStuffPositionsX, carpartStuffPositionsY);
        } else {
            SpawnCarPartSprites(mainEntity, cellEntity, carPartId, rotateAngle, isPlayer, containerSizeX, containerSizeY, carView,
                carpartStuffPositionsX, carpartStuffPositionsY);
        }
    }
    public void SpawnCarPartSprites(int mainEntity, int cellEntity, int carPartId, int rotateAngle, bool isPlayer,
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
                carPartView.SetupCarPart(needGridPosition, needLayer, mainEntity, 0);

                carPartView.SetColliderEnableState(true);
                carPartView.gameObject.tag = isPlayer ? "Player" : "Enemy";
                carViewCmp.CarPartStuffViews[needGridPosition.x, needGridPosition.y] = carPartView;
            } else {
                string needLayer = isPlayer ? "UpCarPart" : "UpCarPartEnemy";
                var carPartView = carViewCmp.CarPartStuffViews[needGridPosition.x, needGridPosition.y];
                cellCarPartsStuffViews.Add(carPartView);
                carPartView.SetupCarPart(needGridPosition, needLayer, mainEntity,0);
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

            Transform firePointTransformToAction = firePointTransform ? firePointTransform.transform : null;
            // Transform weaponContainerTransformToAction = weaponContainerTransform ? weaponContainerTransform.transform:null;
            _carPartSetupAction.Invoke(cellEntity, spriteBase, upPartContainer, weaponSpriteRenderer, laserLineRenderer,
                cellCarPartsStuffViews.ToArray(), firePointTransformToAction, meleeCarPartStuffView);
        
    }*/
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

    /*[Rpc(SendTo.ClientsAndHost)]
    public void SpawnWheelsClientRpc(NetworkObjectReference carViewNO, float[] wheelsPositionsX, float[] wheelsPositionsY, float maxY,
        int[] wheelIds, int[] wheelComponentEntities, ulong clientId) {
        carViewNO.TryGet(out NetworkObject carViewObject);
        var carView = carViewObject.GetComponentInChildren<CarView>();
        List<Animator> forwardWheels = new List<Animator>();
        Animator[] allWheels = new Animator[wheelIds.Length];
        ParticleSystem[] wheelParticles = new ParticleSystem[wheelsPositionsX.Length];
        for (int i = 0; i < wheelsPositionsY.Length; i++) {
            var wheel = CreateWheel(wheelIds[i], carView, new Vector2(wheelsPositionsX[i], wheelsPositionsY[i]), wheelsPositionsX[i] > 0);
         //   Debug.Log((int)wheel.transform.localPosition.y +"=="+ (int)maxY);
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
    }*/
  
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

    [Rpc(SendTo.NotServer)]
    public void OpenSupportBoxClientRpc() {
        _openSupportBoxForClientsAction.Invoke();
    }
    
    [Rpc(SendTo.ClientsAndHost)]
    public void SetCarPartsHpDataClientRpc(CarPartsHpDataForRpc[] carPartsHpData) {
        Debug.Log(carPartsHpData.Length + "SetCarPartsHpDataClientRpc");
        foreach (var carPartData in carPartsHpData) {
            if (NetworkManager.Singleton.LocalClientId == carPartData.ClientId) {
                _setCarPartHealthPointAction.Invoke(new Vector2Int((int)carPartData.GridPosition.x, (int)carPartData.GridPosition.y),
                    carPartData.HealthPoints, carPartData.IsUpperPart);
            }
        }
    }
    
    [Rpc(SendTo.NotServer)]
    public void SetCarPartsHpDataVersusBattleClientRpc(CarPartsHpVersusDataForRpc[] carPartsHpData) {
        Debug.Log(carPartsHpData.Length + "SetCarPartsHpDataVersusClientRpc");
        foreach (var carPartData in carPartsHpData) {
                _setVersusBattleUiAction.Invoke(carPartData.GridPositions,carPartData.Type,
                     carPartData.IsLeft, carPartData.HealthPoints, carPartData.HealthPercent);
        }
    }
    
    [Rpc(SendTo.NotServer)]
    public void SetUiVersusBattleClientRpc(CarPartsHpVersusDataForRpc[] carPartsHpData, Vector2 leftCarSize, Vector2 rightCarSize) {
        Debug.Log(carPartsHpData.Length + "SetupVersusBattle");
      
        _setupVersusBattleUiAction.Invoke(carPartsHpData,leftCarSize,rightCarSize);
    }
    private void ProcessHealthData(int[] healthValues, Vector2Int gridSize, bool isUpperPart,int teammateIndex) // true - для HealthValues, false - для Containers
    {
       // if (healthValues == null) return;

        for (int i = 0; i < healthValues.Length; i++) {
            float health = healthValues[i];
            if (health != 0) {
                Vector2Int gridPosition = ConvertIndexToGridPosition(i, gridSize);
                _setCarPartHealthToClientAction.Invoke(gridPosition, health == -1 ? 0 : health, isUpperPart,teammateIndex);
            }
        }
    }

    private Vector2Int ConvertIndexToGridPosition(int index, Vector2Int gridSize) {
        // Исправленная формула: row = index / cols, col = index % cols
        int cols = gridSize.y; // предполагая, что y - это количество столбцов
        int row = index / cols;
        int col = index % cols;
        return new Vector2Int(row, col);
    }
    [Rpc(SendTo.ClientsAndHost)]
    public void SetBomberPlaneClientRpc(Vector2 finalPosition ) {
        _sendBomberPlaneAction.Invoke(finalPosition);
    }

    [Rpc(SendTo.NotServer)]
    public void SetTimeScaleClietRpc(float timeScale) {
        Time.timeScale = timeScale;
    }
    [Rpc(SendTo.NotServer)]
    public void SetEndRoundHealthValuesClientRpc(CarPartHealthDataForRpc[] carPartsHpData) {
        Debug.Log( "Set Car Parts Hp After Round ClientRpc");
        foreach (var carPartData in carPartsHpData) {
            if (NetworkManager.Singleton.LocalClientId == carPartData.Id) {
                // Player
                ProcessHealthData(carPartData.HealthValuesContainersPlayer, carPartData.PlayerGridSize, false,0);
                ProcessHealthData(carPartData.HealthValuesPlayer, carPartData.PlayerGridSize, true,0);
    
                // First Teammate
                if (carPartData.FirstTeammateGridSize != Vector2Int.zero)
                {
                    ProcessHealthData(carPartData.HealthValuesContainersFirstTeammate, carPartData.FirstTeammateGridSize, false,1);
                    ProcessHealthData(carPartData.HealthValuesFirstTeammate, carPartData.FirstTeammateGridSize, true,1);
                }
    
                // Second Teammate
                if (carPartData.SecondTeammateGridSize != Vector2Int.zero)
                {
                    ProcessHealthData(carPartData.HealthValuesContainersSecondTeammate, carPartData.SecondTeammateGridSize, false,2);
                    ProcessHealthData(carPartData.HealthValuesSecondTeammate, carPartData.SecondTeammateGridSize, true,2);
                }
                
                _setMinimumHealthLevelAction.Invoke();
                break;
            }
        }
    }

    [Rpc(SendTo.NotServer)]
    public void SetEndRoundMovementValuesClientRpc(float[] carMoveDistances, ulong[] playerIds) {
        Debug.Log("Set move distance ClientRpc");
        for (int i = 0; i < playerIds.Length; i++) {
            if (NetworkManager.Singleton.LocalClientId == playerIds[i])
                DataPersistenceManagerSystem._gameData.DistanceDrived += carMoveDistances[i];
        }
    }
    
    private Animator CreateWheel(int wheelId, CarView carView, Vector2 pos, bool isLeftSide) {
      //  Debug.Log("wheel id" + wheelId);
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
public struct CarPartHealthDataForRpc : INetworkSerializable
{
    public ulong Id;
    public int[] HealthValuesPlayer;
    public int[] HealthValuesContainersPlayer;
    public Vector2Int PlayerGridSize;
    public int[] HealthValuesFirstTeammate;
    public int[] HealthValuesContainersFirstTeammate;
    public Vector2Int FirstTeammateGridSize;
    public int[] HealthValuesSecondTeammate;
     public int[] HealthValuesContainersSecondTeammate;
    public Vector2Int SecondTeammateGridSize;

    public CarPartHealthDataForRpc(ulong id, int[] healthValuesPlayer, int[] healthValuesContainersPlayer, Vector2Int playerGridSize,
        int[] healthValuesFirstTeammate, int[] healthValuesContainersFirstTeammate, Vector2Int firstTeammateGridSize,
        int[] healthValuesSecondTeammate, int[] healthValuesContainersSecondTeammate, Vector2Int secondTeammateGridSize) {
        Id = id;
        HealthValuesPlayer = healthValuesPlayer;
        HealthValuesFirstTeammate = healthValuesFirstTeammate;
        HealthValuesSecondTeammate = healthValuesSecondTeammate;
        FirstTeammateGridSize = firstTeammateGridSize;
        SecondTeammateGridSize = secondTeammateGridSize;
        PlayerGridSize = playerGridSize;
        HealthValuesContainersPlayer = healthValuesContainersPlayer;
        HealthValuesContainersFirstTeammate = healthValuesFirstTeammate;
        HealthValuesContainersSecondTeammate = healthValuesSecondTeammate;
    }
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        // Сериализация массива
       
        int healthValuesPlayerLength = HealthValuesPlayer?.Length ?? 0;
        int healthValuesFirstTeammateLength = HealthValuesFirstTeammate?.Length ?? 0;
        int healthValuesSecondTeammateLength = HealthValuesSecondTeammate?.Length ?? 0;
        int healthValuesContainersPlayerLength = HealthValuesContainersPlayer?.Length ?? 0;
        int healthValuesContainersFirstTeammateLength = HealthValuesContainersFirstTeammate?.Length ?? 0;
        int healthValuesContainersSecondTeammateLength = HealthValuesContainersSecondTeammate?.Length ?? 0;
        
        serializer.SerializeValue(ref healthValuesPlayerLength);
        serializer.SerializeValue(ref healthValuesFirstTeammateLength);
        serializer.SerializeValue(ref healthValuesSecondTeammateLength);
        serializer.SerializeValue(ref healthValuesContainersPlayerLength);
        serializer.SerializeValue(ref healthValuesContainersFirstTeammateLength);
        serializer.SerializeValue(ref healthValuesContainersSecondTeammateLength);

        if (serializer.IsReader) {
               HealthValuesContainersPlayer = new int[healthValuesContainersPlayerLength];
               HealthValuesContainersFirstTeammate = new int[healthValuesContainersFirstTeammateLength];
               HealthValuesContainersSecondTeammate = new int[healthValuesContainersSecondTeammateLength];
               HealthValuesPlayer = new int[healthValuesPlayerLength];
               HealthValuesFirstTeammate = new int[healthValuesFirstTeammateLength];
               HealthValuesSecondTeammate = new int[healthValuesSecondTeammateLength];
        }
         
        
        for (int i = 0; i < healthValuesPlayerLength; i++)
            serializer.SerializeValue(ref HealthValuesPlayer[i]);
        for (int i = 0; i < healthValuesFirstTeammateLength; i++)
            serializer.SerializeValue(ref HealthValuesFirstTeammate[i]);
        for (int i = 0; i < healthValuesSecondTeammateLength; i++)
            serializer.SerializeValue(ref HealthValuesSecondTeammate[i]);
        
        for (int i = 0; i < healthValuesContainersPlayerLength; i++)
            serializer.SerializeValue(ref HealthValuesContainersPlayer[i]);
        for (int i = 0; i < healthValuesContainersFirstTeammateLength; i++)
            serializer.SerializeValue(ref HealthValuesContainersFirstTeammate[i]);
        for (int i = 0; i < healthValuesContainersSecondTeammateLength; i++)
            serializer.SerializeValue(ref HealthValuesContainersSecondTeammate[i]);
        // Сериализация остальных полей
        serializer.SerializeValue(ref Id);
        
        serializer.SerializeValue(ref PlayerGridSize);
        serializer.SerializeValue(ref FirstTeammateGridSize);
        serializer.SerializeValue(ref SecondTeammateGridSize);
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

[System.Serializable]
public struct CarPartsHpDataForRpc : INetworkSerializable
{
    public Vector2 GridPosition;
    public float HealthPoints;
    public ulong ClientId;
    public bool IsUpperPart;

    public CarPartsHpDataForRpc(Vector2 gridPosition, float healthPoints, ulong clientId, bool isUpperPart) {
        GridPosition = gridPosition;
        HealthPoints = healthPoints;
        ClientId = clientId;
        IsUpperPart = isUpperPart;
    }
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref GridPosition);
        serializer.SerializeValue(ref HealthPoints);
        serializer.SerializeValue(ref ClientId);
        serializer.SerializeValue(ref IsUpperPart);
    }
}

[System.Serializable]
public struct CarPartsHpVersusDataForRpc : INetworkSerializable {
    public Vector2Int[] GridPositions;
    public float HealthPoints;
    public float HealthPercent;
    public CarPartType Type;
    public bool IsLeft;

    public CarPartsHpVersusDataForRpc(Vector2Int[] gridPositions, float healthPoints,bool isLeftPart, CarPartType type,float healthPercent) {
        GridPositions = gridPositions;
        HealthPoints = healthPoints;
        IsLeft = isLeftPart;
        Type = type;
        HealthPercent = healthPercent;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter {
        int positionsLength = GridPositions?.Length ?? 0;

        serializer.SerializeValue(ref positionsLength);

        if (serializer.IsReader) {
            GridPositions = new Vector2Int[positionsLength];
        }

        for (int i = 0; i < positionsLength; i++)
            serializer.SerializeValue(ref GridPositions[i]);
        serializer.SerializeValue(ref Type);
        serializer.SerializeValue(ref HealthPoints);
        serializer.SerializeValue(ref IsLeft);
        serializer.SerializeValue(ref HealthPercent);
    }
}