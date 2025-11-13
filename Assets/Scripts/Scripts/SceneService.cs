using Leopotam.EcsLite;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Pool;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class SceneService : MonoBehaviour {
    [field: SerializeField] public Transform _mapContainer{ get; private set; }
    [field: SerializeField] public Transform[] _mountains{ get; private set; }
    [field: SerializeField] public Transform _pixelizedEffectsImage{ get; private set; }
    [field: SerializeField] public SettingsView _settingsView{ get; private set; }
    [field: SerializeField] public GivingCarPartsUIView _givingCarPartsUIView{ get; private set; }
    [field: SerializeField] public Transform _brokenCarPartsContainer { get; private set; }
    [SerializeField]
    private RenderTexture _pixelEffectsLayerTexture;
    [field: SerializeField] public EvemiesCarBuilderView _enemiesCarBuilderView { get; private set; }
    [field: SerializeField] public Transform _gunContainerPrefab { get; private set; }
  [field: SerializeField] public SubGridView _subGridTransform { get; private set; }
  [field: SerializeField] public RectTransform _gridMaskRect { get; private set; }
    [field: SerializeField] public RectTransform _carPartsContainersContainer { get; private set; }
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
    [field: SerializeField] public ProjectileView _projectileView { get; private set; }
    [field: SerializeField] public int _defaultCarConfigId { get; private set; }
    [field: SerializeField] public GameProcessConfig _gameProcessConfig { get; private set; }
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
    [field: SerializeField] public Light gloabalLight { get; set; }
    [field: SerializeField] public Camera mainCamera { get; private set; }
    public float aspectRatio;
    public InputSystem_Actions gameInput;

    public int PlayerEntity;
    public BuildingInfo buildingInfo;
    public BuildingView buildingViewForTest;
    private ObjectPool<ProjectileView> _projectilesPool;
    private ObjectPool<CarPartStuffView> _carPartsPool;
    private ObjectPool<TMP_Text> _floatingTextsPool;
    private ObjectPool<LineRenderer> _laserLineRenderersPool;
    private ObjectPool<MeleeCarPartStuffView> _meleeCarPartsPool;
    private ObjectPool<CarPartInGameUIView> _сarPartInGameUIViewsPool;
    private ObjectPool<SpriteRenderer> _explosionEffectsPool;
    private ObjectPool<Transform> _gunContainersPool;
    

    private void Awake() {
        _endGameScreenView.OnInitialize();
        _endGameScreenView.gameObject.SetActive(false);
        gameInput = new InputSystem_Actions();
        gameInput.Enable();
        gameVersionText.text = Application.version;
        aspectRatio =(mainCanvas.rect.width / mainCanvas.rect.height)/(16f/9f) ;
        aspectRatio = 7.5f;
        mainCamera = Camera.main;
        _gunContainersPool = new ObjectPool<Transform>(() => Instantiate(_gunContainerPrefab));
        _laserLineRenderersPool = new ObjectPool<LineRenderer>(() => Instantiate(_laserLineRenderer, _notUsedCarPartsContainer));
        _projectilesPool = new ObjectPool<ProjectileView>(() => Instantiate(_projectileView));
        _floatingTextsPool = new ObjectPool<TMP_Text>(() => Instantiate(_floatingTextPrefab, _floatingTextContainer));
        _carPartsPool = new ObjectPool<CarPartStuffView>(() => Instantiate(_carStuffListsConfig.CarPartStuffView));
        _meleeCarPartsPool = new ObjectPool<MeleeCarPartStuffView>(() => Instantiate(_carStuffListsConfig.MeleeCarPartStuffView));
        _explosionEffectsPool = new ObjectPool<SpriteRenderer>(() => Instantiate(_explosionEffectPrefab));
        _сarPartInGameUIViewsPool =
            new ObjectPool<CarPartInGameUIView>(() => Instantiate(_carPartInGameUIView, _playerUIView._carPratsDownLevelHealthContainer));
       
            _pixelEffectsLayerTexture.width = Screen.width;
        _pixelEffectsLayerTexture.height = Screen.height;
    }

    public CarPartInGameUIView GetCarPartInGameUIView() {
        var projectile = _сarPartInGameUIViewsPool.Get();
        projectile.gameObject.SetActive(true);
        return projectile;
    }
    public SpriteRenderer GetCarPartSpriteRenderer(Transform parent) {
        return Instantiate(_carPartSpriteRenderer, parent);
    }
    public Transform GetGunContainer(Transform parent) {
        var gunContainer = _gunContainersPool.Get();
        gunContainer.parent = parent;
        return gunContainer;
    }
    public Image GetCarPartUISprite(Transform parent, Vector2 position) {
        return Instantiate(_carPartUIImage, position, Quaternion.identity, parent);
    }
    public (TMP_Text, Sequence) GetFloatingTextPrefab(Vector2 position, string text, Color textColor) {
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
        _gunContainersPool.Release(gunContainer);
    }
    
    public LineRenderer GetLaserLineRenderer() {
        var laser = _laserLineRenderersPool.Get();
        laser.gameObject.SetActive(true);
        return laser;
    }
    
    public void ReleaseLaserLineRenderer(LineRenderer laser) {
        laser.gameObject.SetActive(false);
        _laserLineRenderersPool.Release(laser);
    }
    
    
    public SpriteRenderer GetExplosionEffect() {
        var explosion = _explosionEffectsPool.Get();
        explosion.color = Color.white;
        explosion.gameObject.SetActive(true);
        return explosion;
    }
    
    public void ReleaseExplosionEffect(SpriteRenderer explosion) {
        explosion.gameObject.SetActive(false);
        _explosionEffectsPool.Release(explosion);
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
    public ProjectileView GetGunProjectile(Vector2 spawnPosition)
    {
        var projectile = _projectilesPool.Get();
        projectile.gameObject.SetActive(true);
        projectile.transform.position = spawnPosition;
        return projectile;
    }
    public void ReleaseGunProjectile(ProjectileView projectile, float radius ,Sprite[] animation = null) {
       // if(!projectile.gameObject.activeInHierarchy)return;

        if (animation == null) 
            RelaseProjectileYourself(projectile);
        else {
            
            projectile.PlayHitAnimation(animation,radius, RelaseProjectileYourself);
        }
    }

    public void RelaseProjectileYourself(ProjectileView projectile) {
        if(!projectile.gameObject.activeInHierarchy)return;
        projectile.gameObject.SetActive(false);
                _projectilesPool.Release(projectile);
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


    public Transform InstantiateObject(Transform spawnObject, Vector2 spawnPosition)
    {
        return Instantiate(spawnObject, spawnPosition, Quaternion.identity).transform;
    }
}
