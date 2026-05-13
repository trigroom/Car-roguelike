using Leopotam.EcsLite;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Pool;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneService : MonoBehaviour {
    private Sequence _warningSequence;
    [field: SerializeField] public SlotMachineUiView _slotMachineUiView { get; private set; }
    [field: SerializeField] public Transform _hpBarPrefab { get; private set; }
    [field: SerializeField] public Transform _hpBarsContainer{ get; private set; }
    [field: SerializeField] public MapEventsContentView _mapEventsContentView{ get; private set; }
    [field: SerializeField] public VersusBattleView _versusBattleView{ get; private set; }
    [field: SerializeField] public Material _defaultMaterial{ get; private set; }
    [field: SerializeField] public Sprite _whiteCircle{ get; private set; }
    [field: SerializeField] public SpriteRenderer _cloudsSpriteRenderer { get; private set; }
    [field: SerializeField] public SpriteRenderer _behindMapBorderSpriteRenderer { get; private set; }
    [field: SerializeField] public SpriteRenderer _mapGroundSpriteRenderer { get; private set; }
    [field: SerializeField] public SpriteRenderer _behindMapGroundSpriteRenderer { get; private set; }
    [field: SerializeField] public ParticleSystem _carPartsParticleSystemPrefab { get; private set; }
    [field: SerializeField] public TutorialStuffView _tutorialStuffView{ get; private set; }
    [field: SerializeField] public SupportBoxStuffView _supportBoxStuffView{ get; private set; }
    [field: SerializeField] public TMP_Text _warningText { get; private set; }
    [field: SerializeField] public StaticTextsView _staticTextsView { get; private set; }
    [field: SerializeField] public Transform _dontUsedUIImagesPool{ get; private set; }
    [field: SerializeField] public Transform _markForTeammatesContainer{ get; private set; }
    [field: SerializeField] public Transform _mapContainer{ get; private set; }
    [field: SerializeField] public Transform _pixelizedEffectsImage{ get; private set; }
    [field: SerializeField] public Transform _pixelizedEffectsUnderCarImage{ get; private set; }
    [field: SerializeField] public SettingsView _settingsView{ get; private set; }
    [field: SerializeField] public GivingCarPartsUIView _givingCarPartsUIView{ get; private set; }
    [field: SerializeField] public Transform _brokenCarPartsContainer { get; private set; }
    [SerializeField]
    private RenderTexture _pixelEffectsLayerTexture;
    [SerializeField]
    private RenderTexture _pixelEffectsUnderCarLayerTexture;
    [field: SerializeField] public EvemiesCarBuilderView _enemiesCarBuilderView { get; private set; }
    [field: SerializeField] public Transform _gunContainerPrefab { get; private set; }
  [field: SerializeField] public SubGridView _subGridTransform { get; private set; }
  [field: SerializeField] public RectTransform _gridMaskRect { get; private set; }
    [field: SerializeField] public SpriteRenderer _carPartSpriteRenderer { get; private set; }
    [field: SerializeField] public Image _carPartUIImage { get; private set; }
    [field: SerializeField] public Transform _floatingTextContainer { get; private set; }
    [field: SerializeField] public TMP_Text _floatingTextPrefab { get; private set; }
    [field: SerializeField] public MergeRoundView _mergeRoundView { get; private set; }
    [field: SerializeField] public MapFilledZoneView _mapFilledZoneView { get; private set; }
    [field: SerializeField] public SpriteRenderer _explosionEffectPrefab { get; private set; }
    [field: SerializeField] public MapPointColiderView _mapPointPrefab { get; private set; }
    [field: SerializeField] public CarPartInGameUIView _carPartInGameUIView { get; private set; }
    [field: SerializeField] public EndGameScreenView _endGameScreenView { get; private set; }
    [field: SerializeField] public CarStuffListsConfig _carStuffListsConfig { get; private set; }
    [field: SerializeField] public Transform _carView { get; private set; }
    [field: SerializeField] public MapUIView _mapUIView { get; private set; }
    [field: SerializeField] public LineRenderer _laserLineRenderer { get; private set; }
    [field: SerializeField] public Transform _notUsedCarPartsContainer { get; private set; }
    [field: SerializeField] public PlayerUIView _playerUIView { get; private set; }
    [field: SerializeField] public SpriteRenderer _wheelSpriteRenderer { get; private set; }
    [field: SerializeField] public InventoryUIView _inventoryUIView { get; private set; }
    [field: SerializeField] public Gradient _healthGradient { get; private set; }
    [field: SerializeField] public ShopUIView _shopUIView { get; private set; }
    [field: SerializeField] public Sprite[] _bulletSprites { get; private set; }
    [field: SerializeField] public int _defaultCarConfigId { get; private set; }
    [field: SerializeField] public CarPartInfoUIView _carPartInfoUIView { get; private set; }
    [field: SerializeField] public Sprite _transparentPixel { get; private set; }
    [field: SerializeField] public Sprite _buildCellSprite { get; private set; }
    [field: SerializeField] public RectTransform towerButtonsContainer { get; private set; }
    [field: SerializeField] public Button closeTowerActionsContainerButton { get; private set; }
    [field: SerializeField] public Button destroyTowerButton { get; private set; }
    [field: SerializeField] public Button moveTowerButton { get; private set; }
    [field: SerializeField] public Button[] buyButtons { get; private set; }
    [field: SerializeField] public Transform cellsContainer { get; private set; }
    [field: SerializeField] public Transform firstEntryGuide { get; private set; }
    [field: SerializeField] public AudioSource backgroundAudioSource { get; private set; }
    [field: SerializeField] public RectTransform mainCanvas { get; private set; }
    [field: SerializeField] public Canvas mainCanvasForCells { get; private set; }
    [field: SerializeField] public Animator warningInfoTextAnimator { get; private set; }
    [field: SerializeField] public TMP_Text warningInfoText { get; private set; }
    [field: SerializeField] public AudioSource uiAudioSourse { get; private set; }
    [field: SerializeField] public AudioClip equipItemSound { get; private set; }
    [field: SerializeField] public TMP_Text gameVersionText { get; private set; }
    [field: SerializeField] public CellView emptyCell { get; private set; }
    [field: SerializeField] public TMP_Text moneyText { get; private set; }
    [field: SerializeField] public Light2D globalLight { get; set; }
    [field: SerializeField] public Camera mainCamera { get; private set; }
    [field: SerializeField] public EnemyAirplaneView enemyAirplaneView { get; set; }
    public float aspectRatio;
   // public InputSystem_Actions gameInput;

    public int PlayerEntity;
    public BuildingInfo buildingInfo;
    public BuildingView buildingViewForTest;
    private ObjectPool<CarPartStuffView> _carPartsPool;
    private ObjectPool<TMP_Text> _floatingTextsPool;
    private ObjectPool<EnemyAirplaneView> _enemyAirplanesPool;
    private ObjectPool<LineRenderer> _laserLineRenderersPool;
    private ObjectPool<MeleeCarPartStuffView> _meleeCarPartsPool;
    private ObjectPool<CarPartInGameUIView> _сarPartInGameUIViewsPool;
    private ObjectPool<SpriteRenderer> _spriteRenderersPool;
    private ObjectPool<Transform> _gunContainersPool;
    private ObjectPool<Image> _imagesPool;
    private ObjectPool<Transform> _hpBarsPool;
    private ObjectPool<ParticleSystem> _carPartsParticleSystemPool;
    

    private void Awake() {


        if (BetweenScenesService.Instance == null) {
              SceneManager.LoadScene("Scenes/LoadingScene");
              return;
        }
          
        else {
          _defaultCarConfigId = BetweenScenesService.Instance.CurrentSelectedCar;
                  BetweenScenesService.Instance.HideBlackScreen();  
        }
       
        NetcodeBetweenScenesService.Instance.MapContainer = _mapContainer;
        NetcodeBetweenScenesService.Instance.MapEventsContentView = _mapEventsContentView;
        NetcodeBetweenScenesService.Instance.GroundSpriteRenderer = _mapGroundSpriteRenderer;
   
        _endGameScreenView.OnInitialize();
        _endGameScreenView.gameObject.SetActive(false);
    //    gameInput = new InputSystem_Actions();
  //      gameInput.Enable();
        gameVersionText.text = Application.version;
        aspectRatio =(mainCanvas.rect.width / mainCanvas.rect.height)/(16f/9f) ;
        aspectRatio = 7.5f;
        mainCamera = Camera.main;
        _gunContainersPool = new ObjectPool<Transform>(() => Instantiate(_gunContainerPrefab));
        _hpBarsPool = new ObjectPool<Transform>(() => Instantiate(_hpBarPrefab, _hpBarsContainer));
        _laserLineRenderersPool = new ObjectPool<LineRenderer>(() => Instantiate(_laserLineRenderer, _notUsedCarPartsContainer));
        _floatingTextsPool = new ObjectPool<TMP_Text>(() => Instantiate(_floatingTextPrefab, _floatingTextContainer));
        _carPartsPool = new ObjectPool<CarPartStuffView>(() => Instantiate(_carStuffListsConfig.CarPartStuffView));
        _meleeCarPartsPool = new ObjectPool<MeleeCarPartStuffView>(() => Instantiate(_carStuffListsConfig.MeleeCarPartStuffView));
        _spriteRenderersPool = new ObjectPool<SpriteRenderer>(() => Instantiate(_carPartSpriteRenderer));
        _carPartsParticleSystemPool = new ObjectPool<ParticleSystem>(() => Instantiate(_carPartsParticleSystemPrefab,_notUsedCarPartsContainer));
        _imagesPool = new ObjectPool<Image>(() => Instantiate(_carPartUIImage));
        _enemyAirplanesPool = new ObjectPool<EnemyAirplaneView>(() => Instantiate(enemyAirplaneView));
        _сarPartInGameUIViewsPool =
            new ObjectPool<CarPartInGameUIView>(() => Instantiate(_carPartInGameUIView, _playerUIView._carPratsDownLevelHealthContainer));
        
        _pixelEffectsLayerTexture.width = Screen.width;
        _pixelEffectsLayerTexture.height = Screen.height;
        _pixelEffectsUnderCarLayerTexture.width = Screen.width;
        _pixelEffectsUnderCarLayerTexture.height = Screen.height;
    }

    public CarPartInGameUIView GetCarPartInGameUIView() {
        var projectile = _сarPartInGameUIViewsPool.Get();
        projectile.gameObject.SetActive(true);
        return projectile;
    }
    public EnemyAirplaneView GetEnemyAirplaneView() {
        var airplane = _enemyAirplanesPool.Get();
        airplane.gameObject.SetActive(true);
        return airplane;
    }
    public void ReleaseEnemyAirplane(EnemyAirplaneView airplane) {
        _enemyAirplanesPool.Release(airplane);
        airplane.gameObject.SetActive(false);
    }
  /*  public SpriteRenderer GetCarPartSpriteRenderer(Transform parent = null) {
        return parent ? Instantiate(_carPartSpriteRenderer, parent) : Instantiate(_carPartSpriteRenderer);
    }*/
    public Transform GetGunContainer(Transform parent) {
        var gunContainer = _gunContainersPool.Get();
        gunContainer.gameObject.SetActive(true);
        gunContainer.parent = parent;
        return gunContainer;
    }
    public Image GetHpBar(Transform parent) {
        var hpBar = _hpBarsPool.Get();
        hpBar.transform.position = parent.position;
        hpBar.transform.gameObject.SetActive(true);
        return hpBar.GetChild(0).GetComponent<Image>();
    }
    public void ReleaseHpBar(Image hpBar) {
        _hpBarsPool.Release(hpBar.transform.parent);
        hpBar.transform.parent.gameObject.SetActive(false);
    }
    public ParticleSystem GetCarPartParticles() {
        var gunContainer = _carPartsParticleSystemPool.Get();
        gunContainer.gameObject.SetActive(true);
        return gunContainer;
    }
    public void ReleaseCarPartParticles(ParticleSystem gunContainer) {
        gunContainer.gameObject.SetActive(false);
        _carPartsParticleSystemPool.Release(gunContainer);
    }
    public Image GetCarPartUIImage(Transform parent, Vector2 position) {
        var carPartImage = _imagesPool.Get();
        carPartImage.transform.SetParent(parent, false); 
        carPartImage.transform.position = position;
        carPartImage.gameObject.SetActive(true);
        carPartImage.transform.localScale = Vector3.one;
        return carPartImage;
    }

    public void ReleaseCarPartUIImage(Image carPartImage) {
        carPartImage.gameObject.SetActive(false);
       
        carPartImage.transform.SetParent(_dontUsedUIImagesPool, false); 
        _imagesPool.Release(carPartImage);
    }
    public (TMP_Text, Sequence) GetFloatingTextPrefab(Vector2 position, string text, Color textColor) {
        if (!DataPersistenceManagerSystem._gameData.UseDamageNumbers) return(null,null);
        var floatingText = _floatingTextsPool.Get();
        var curSequence = SetFloatingText(position, text, textColor, floatingText);
        return (floatingText,curSequence);
    }
    
    

    public Sequence SetFloatingText(Vector2 position, string text, Color textColor, TMP_Text floatingText, Sequence needSequence = null) {
        floatingText.text = text;
        floatingText.transform.position = position;
        floatingText.color = textColor;
        floatingText.gameObject.SetActive(true);
        Sequence sequence =DOTween.Sequence();
        sequence.Append(floatingText.transform.DOLocalMoveY(position.y+1, 0.7f)
            .SetEase(Ease.OutCubic));
        sequence.Join(floatingText.DOColor(new Color(textColor.r, textColor.g, textColor.b,0), 0.7f));
        sequence.OnComplete(()=>ReleaseFloatingText(floatingText));
        sequence.Play();
        return sequence;
    }

    public void ReleaseFloatingText(TMP_Text floatingText) {
        floatingText.gameObject.SetActive(false);
        _floatingTextsPool.Release(floatingText);
    }
    
    public void ReleaseGunContainer(Transform gunContainer) {
        gunContainer.gameObject.SetActive(false);
        gunContainer.SetParent(_notUsedCarPartsContainer);
        _gunContainersPool.Release(gunContainer);
    }
    
    public LineRenderer GetLaserLineRenderer() {
        var laser = _laserLineRenderersPool.Get();
      
        return laser;
    }
    
    public void ReleaseLaserLineRenderer(LineRenderer laser) {
        laser.gameObject.SetActive(false);
        _laserLineRenderersPool.Release(laser);
    }

    public SpriteRenderer GetSpriteRenderer(Transform parent = null) {
        var explosion = _spriteRenderersPool.Get();
        explosion.color = Color.white;
        explosion.transform.localScale = Vector3.one;
        explosion.sortingOrder = 0;
        explosion.material = _defaultMaterial;
        explosion.gameObject.layer = LayerMask.NameToLayer("Default");
        explosion.gameObject.SetActive(true);
        if (parent)
            explosion.transform.parent = parent;
        transform.localEulerAngles = Vector3.zero;
        return explosion;
    }
    
    public void ReleaseSpriteRenderer(SpriteRenderer explosion) {
        explosion.gameObject.SetActive(false);
        explosion.transform.SetParent(_notUsedCarPartsContainer);
        _spriteRenderersPool.Release(explosion);
    }

    public EventTrigger InstantiateRoundButton() {
        return Instantiate(_mapUIView.RoundButton,_mapUIView.MapImage.transform);
    }

    public void ReleaseCarPartInGameUIView(CarPartInGameUIView projectile) {
        projectile.gameObject.SetActive(false);
        _сarPartInGameUIViewsPool.Release(projectile);
    }

    public CarConfig GetCarConfig(int id) {
        return _carStuffListsConfig.CarConfigs[id];
    }
    
    public MeleeWeaponConfig GetMeleeWeaponFromCarPart(int id)
    {
        if (GetCarPartConfig(id) is MeleeWeaponConfig armor)
            return armor;
        return null; 
    }
    
    public WheelsContainerConfig GetWheelContainerFromCarPart(int id)
    {
        if (GetCarPartConfig(id) is WheelsContainerConfig wheels)
            return wheels;
        return null; 
    }
    
    public ArmorConfig GetArmorFromCarPart(int id)
    {
        if (GetCarPartConfig(id) is ArmorConfig armor)
            return armor;
        return null; 
    }
    public WeaponContainerConfig GetWeaponContainerFromCarPart(int id)
    {
        if (GetCarPartConfig(id) is WeaponContainerConfig container)
            return container;
        return null; 
    }
    
    public GunConfig GetGunFromCarPart(int id)
    {
        if (GetCarPartConfig(id) is GunConfig gun)
            return gun;
        return null; 
    }
    
    public SupportModuleCarPartConfig GetSupportModuleFromCarPart(int id)
    {
        if (GetCarPartConfig(id) is SupportModuleCarPartConfig support)
            return support;
        return null; 
    }
    
    public CarPartConfig GetCarPartConfig(int id) {
        return _carStuffListsConfig.CarPartConfigs[id];
    }
    public CarPartStuffView GetCarPartCell(Transform parent) {
        var carPart = _carPartsPool.Get();
        carPart.gameObject.SetActive(true);
        carPart.transform.SetParent(parent);
        return carPart;
    }

    public void ReleaseCarPartCell(CarPartStuffView carPart) {
        if (!carPart.gameObject.activeInHierarchy) return;
        carPart.gameObject.SetActive(false);
        carPart.transform.SetParent(_notUsedCarPartsContainer);
        _carPartsPool.Release(carPart);
    }
    
    public MeleeCarPartStuffView GetMeleeCarPartCell(Transform parent) {
        var carPart = _meleeCarPartsPool.Get();
        carPart.gameObject.SetActive(true);
        carPart.transform.SetParent(parent);
        return carPart;
    }
    
    public void ReleaseMeleeCarPartCell(MeleeCarPartStuffView carPart) {
        carPart.gameObject.SetActive(false);
        carPart.transform.SetParent(_notUsedCarPartsContainer);
        _meleeCarPartsPool.Release(carPart);
    }
    
    public CarView InstantiateCarView()
    {
        return Instantiate(_carView).GetComponentInChildren<CarView>();
    }
    public CellView InstantiateCell(CellView instantiatedObject, Transform parent)
    {
        return Instantiate(instantiatedObject, parent);
    }
    public Animator InstantiateAnimator(Animator instantiatedObject, Transform parent)
    {
        return Instantiate(instantiatedObject, parent);
    }
    public InventoryCellView InstantiateInventoryCell(InventoryCellView instantiatedObject, Transform parent)
    {
        return Instantiate(instantiatedObject, parent);
    }
    public BuildingView InstantiateBuilding(BuildingView instantiatedObject)
    {
        return Instantiate(instantiatedObject);
    }
    public void DestroyGameobject(Transform objectToDestroy)
    {
      //  Debug.Log(objectToDestroy.name);
        Destroy(objectToDestroy.gameObject);
    }

    public Vector2 GetCurrentMousePositionOnUI()
    {
        return (Input.mousePosition - mainCanvas.position) / mainCanvas.localScale.x;
    }
    public void ShowWarningText(string needText)
    {
        var screenPoint = (Input.mousePosition - mainCanvas.position) / mainCanvas.localScale.x;
        warningInfoText.rectTransform.anchoredPosition = screenPoint;
        warningInfoTextAnimator.SetTrigger("SetText");
        warningInfoText.text = needText;
    }

    public Vector3 GetOutOfScreenPosition()
    {
        var randomX = Random.Range(-1000, 1000);
        var randomY = Random.Range(-1000, 1000);
        var randomPosition = new Vector3(randomX, randomY);
        var randomDirection = (mainCamera.transform.position - randomPosition).normalized;
        var cameraHeight = mainCamera.orthographicSize * 2;
        var cameraWidth = cameraHeight * mainCamera.aspect;
        return new Vector3(randomDirection.x * cameraHeight, randomDirection.y * cameraWidth);
    }


    public Transform InstantiateObject(Transform spawnObject, Vector2 spawnPosition = default)
    {
        return Instantiate(spawnObject, spawnPosition, Quaternion.identity).transform;
    }
    
    public Transform InstantiateObject(Transform spawnObject, Transform parent, Vector2 spawnPosition = default)
    {
        return Instantiate(spawnObject, spawnPosition, Quaternion.identity, parent).transform;
    }

    public void PlayWarningAnimation(string message, bool inScreenCentre)
    {
        // Устанавливаем текст
        _warningText.text = message;
        _warningText.gameObject.SetActive(true);
        if (inScreenCentre)
            _warningText.transform.localPosition = new Vector2(0, 300);
        else
            _warningText.transform.position = Input.mousePosition;
        
        var needColor =inScreenCentre? new Color(0.4f, 0.6f, 0.3f, 1):new Color(0.8f, 0.2f, 0.2f,1);
        
        _warningSequence.Kill();
        _warningSequence = DOTween.Sequence();
        
        _warningSequence.Append(_warningText.DOColor(needColor, 0.3f));
        
        float endPos = _warningText.transform.position.y + 50;
        _warningSequence.Join(_warningText.transform.DOMoveY(endPos, 0.3f).SetEase(Ease.OutCubic));
        
        _warningSequence.Join(_warningText.transform.DOScale(1.3f,0.3f)
            .SetEase(Ease.OutBack)
            .OnComplete(() => 
            {
                _warningText.transform.DOScale(1, 0.3f).SetEase(Ease.InBack);
            }));
        
        _warningSequence.AppendInterval(1f);
        
        float disappearPos = endPos + 10f;
        _warningSequence.Append(_warningText.transform.DOMoveY(disappearPos, 0.3f));
        
        _warningSequence.Join(_warningText.DOColor(new Color(0f, 0f, 0f, 0), 0.3f));
        
        _warningSequence.OnComplete(() => _warningText.gameObject.SetActive(false));
        
        _warningSequence.Play();
    }
}
