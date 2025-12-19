using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CarPartInfoUIView : MonoBehaviour
{
    [field: SerializeField] public TMP_Text _healCarPartText { get; private set; }
    [field: SerializeField] public Button _healCarPartButton { get; private set; }
    [field: SerializeField] public Button _closeInfoPanelButton { get; private set; }
    [field: SerializeField] public Button _moveToInventoryButton { get; private set; }
    [field: SerializeField] public EventTrigger _upgradeButton { get; private set; }
    [field: SerializeField] public TMP_Text _upgradeButtonText { get; private set; }
    [field: SerializeField] public Button _destroyButton { get; private set; }
    [field: SerializeField] public Button _backButton { get; private set; }
    [field: SerializeField] public Button _nextButton { get; private set; }

    [field: SerializeField]
    public Transform _switchButtonsContainer;
    [field: SerializeField] public CarPartConfig _currentCarPartConfig { get; private set; }
    [field: SerializeField] public TMP_Text _nameText { get; private set; }
    [field: SerializeField] public TMP_Text _descriptionText { get; private set; }
    public TMP_Text CarDescriptionText;
    [field: SerializeField] public Image _carPartIcon { get; private set; }
    [field: SerializeField] public Image _carPartIconUpLevel { get; private set; }
    [field: SerializeField] public TMP_Text _sellCarPartText { get; private set; }
    public int _level;
    public float CurrentMarkedCarPartHealth;

    public void SetupButtonsInfoPanel(Action backCarPart, Action destroy, Action upgrade, Action upgradeTriggerEnter, Action upgradeTriggerExit,
        Action moveToInventory) {
        _backButton.onClick.AddListener(() => backCarPart?.Invoke());
        _moveToInventoryButton.onClick.AddListener(() => moveToInventory?.Invoke());
        _destroyButton.onClick.AddListener(() => destroy?.Invoke());
        //_upgradeButton.onClick.AddListener(() => upgrade?.Invoke());
        

        EventTrigger.Entry clickEntry = new EventTrigger.Entry();
        clickEntry.eventID = EventTriggerType.PointerClick;
        clickEntry.callback.AddListener((data) => { upgrade.Invoke(); });
            
        EventTrigger.Entry enterEntry = new EventTrigger.Entry();
        enterEntry.eventID = EventTriggerType.PointerEnter;
        enterEntry.callback.AddListener((data) => {upgradeTriggerEnter.Invoke(); });
            
        EventTrigger.Entry exitEntry = new EventTrigger.Entry();
        exitEntry.eventID = EventTriggerType.PointerExit;
        exitEntry.callback.AddListener((data) => {upgradeTriggerExit.Invoke(); });
        
        _upgradeButton.triggers.Add(clickEntry);
        _upgradeButton.triggers.Add(enterEntry);
        _upgradeButton.triggers.Add(exitEntry);
    }
    private string GetLocalization(string needString) => BetweenScenesService.Instance.CsvLocalization.GetLocalizedValue(needString);
    public void SetPanelForNeededPart(CarPartConfig carPartConfig, bool showDestroyButton, bool showMoveButton, int carPartLevel,
        float currentHealth, float healCost) {
        _destroyButton.gameObject.SetActive(showDestroyButton);
        _moveToInventoryButton.gameObject.SetActive(showMoveButton);
        _level = carPartLevel;
        CurrentMarkedCarPartHealth = currentHealth;
        if (GridUtils.GetUpgradeCost(carPartLevel, carPartConfig) != 0 && _maxLevelsForRare[carPartConfig.RartRare] > carPartLevel) {
            _upgradeButtonText.text = $"{GridUtils.GetUpgradeCost(carPartLevel, carPartConfig).ToString()}$ {GetLocalization("to upgrade")}";
            _upgradeButton.enabled = true;
        } else {
            _upgradeButton.enabled = false;
            _upgradeButtonText.text = GetLocalization("Max level");
        }
            
        
        //_upgradeButton.gameObject.SetActive(carPartConfig.UpgradedPartId != 0);

        _nameText.text = GetLocalization(carPartConfig.PartName);

        if (carPartLevel != 0)
            _nameText.text += $"\n{carPartLevel} {GetLocalization("lvl")}.";
        _healCarPartButton.gameObject.SetActive(healCost > 0);
        if (healCost > 0) {
            _healCarPartText.text = $"{GetLocalization("Heal car part for")} {healCost}$";
            _healCarPartText.color = Color.clear;
        }

        _carPartIconUpLevel.gameObject.SetActive(false);
        _currentCarPartConfig = carPartConfig;
        _carPartIcon.sprite = carPartConfig.PartSprite;

        SetDescriptionInfo(false);

        _sellCarPartText.text = $"{GetLocalization("Sell for")} {GridUtils.GetSellValueForCarPart(carPartConfig, carPartLevel)}$";
    }

    private Dictionary<CarPartRare, int> _maxLevelsForRare = new Dictionary<CarPartRare, int>() {
        {CarPartRare.Low, 3},
        {CarPartRare.Medium, 5},
        {CarPartRare.High, 7},
        {CarPartRare.VeryHigh, 10}
    };

    public void SetDescriptionInfo(bool isUpdate) {
    
        _descriptionText.text =
            $"{CurrentMarkedCarPartHealth:F2}/{_currentCarPartConfig.MaxHealth} {GetLocalization("HP")} \n{GetLocalization("Part rare")}:" +
            $" {GetLocalization(_currentCarPartConfig.RartRare.ToString())}\n{GetLocalization("Weight")}: {_currentCarPartConfig.Weight} {GetLocalization("kg")}";
        int needLevel = isUpdate ? _level + 1 : _level;
        
        if (_currentCarPartConfig.PartType == CarPartType.Gun) {
            if (_currentCarPartConfig is GunConfig gunConfig) {
                SetGunInfo(gunConfig, needLevel, isUpdate);
            }
        } else if (_currentCarPartConfig.PartType == CarPartType.MeleeWeapon) {
            if (_currentCarPartConfig is MeleeWeaponConfig meleeConfig) {
                
                if (meleeConfig.UpSprite != null) {
                    var carPartSize = GridUtils.GetCarPartSize(meleeConfig);
                    _carPartIconUpLevel.rectTransform.sizeDelta = _carPartIcon.rectTransform.sizeDelta /
                        (Math.Max(carPartSize.x, carPartSize.y) * GridUtils.InWorldCellSize) * meleeConfig.UpSprite.rect.size;
                    _carPartIconUpLevel.transform.localPosition = _carPartIcon.rectTransform.sizeDelta /
                        Math.Max(carPartSize.x, carPartSize.y) * meleeConfig.UpSpritePosition;
                    _carPartIconUpLevel.sprite = meleeConfig.UpSprite;
                    _carPartIconUpLevel.gameObject.SetActive(true);
                }
                (float damageMultiplier, float damageBySpeedMultiplier) = GridUtils.GetMeleeLevelMultipliers(meleeConfig,needLevel);
                if (isUpdate) {
                    if (damageMultiplier != 1f)
                        _descriptionText.text += $"\n{GetLocalization("Damage")}: <color=#5bca4a>{meleeConfig.Damage * damageMultiplier}/s</color> ";
                    else
                        _descriptionText.text += $"\n{GetLocalization("Damage")}: {meleeConfig.Damage * damageMultiplier}/s ";
                    if (damageBySpeedMultiplier != 1f)
                        _descriptionText.text +=
                            $"\n{GetLocalization("Added damage")}: <color=#5bca4a>{meleeConfig.DamageAddedFromOneSpeed * damageBySpeedMultiplier:F2}/1 m/s</color>\n";
                    else
                        _descriptionText.text += $"\n{GetLocalization("Added damage")}: {meleeConfig.DamageAddedFromOneSpeed * damageBySpeedMultiplier:F2}/1 m/s\n";
                    _descriptionText.text += $"{GetLocalization("Attack countdown")}: {meleeConfig.AttackCountdown:F2}s";
                } else
                    _descriptionText.text +=
                        $"\n{GetLocalization("Damage")}: {meleeConfig.Damage * damageMultiplier}/s \nAdded damage: {meleeConfig.DamageAddedFromOneSpeed * damageBySpeedMultiplier:F2}/1 m/s\n" +
                        $"{GetLocalization("Attack countdown")}: {meleeConfig.AttackCountdown:F2}s";
            }
        } else if (_currentCarPartConfig.PartType == CarPartType.Armor) {
            if (_currentCarPartConfig is ArmorConfig armorConfig) {
                float defenceMultiplayer = GridUtils.GetArmorLevelMultipliers(needLevel);
                float defencePoints = armorConfig.DefencePercent * defenceMultiplayer;
                if (!isUpdate)
                    _descriptionText.text += $"\n{GetLocalization("Defence")}: {defencePoints} ({defencePoints / (defencePoints + 100f) * 100f:F2}%)";
                else
                    _descriptionText.text +=
                        $"\n{GetLocalization("")}Defence: <color=#5bca4a>{defencePoints} ({defencePoints / (defencePoints + 100f) * 100f:F2}%)</color>";

                if (armorConfig.DefenceDirections.Count == 4)
                    _descriptionText.text += $"\n{GetLocalization("Defence from all directions")}";
                else
                    _descriptionText.text += $"\n{GetLocalization("Defence from")}: " + string.Join(", ", armorConfig.DefenceDirections);

                if (armorConfig.ExplosionDefence)
                    _descriptionText.text += $" \n{GetLocalization("Prevents explosion")}";
            }
        } else if (_currentCarPartConfig.PartType == CarPartType.Support) {
            if (_currentCarPartConfig is SupportModuleCarPartConfig supportConfig) {
                SetSupportPartInfo(supportConfig, needLevel, isUpdate);
            }
        }
        else if (_currentCarPartConfig.PartType == CarPartType.WheelsContainer) {
            if (_currentCarPartConfig is WheelsContainerConfig wheelConfig) {
                _descriptionText.text += $"\n{GetLocalization("Car max weight")}: {wheelConfig.MaxWeight}";
            }
        }
    }

    private void SetSupportPartInfo(SupportModuleCarPartConfig supportConfig, int level, bool isUpdate) {
        //now level doesnt scale stats
        (float firstMultiplayer,float secondMultiplayer)= GridUtils.GetSupportLevelMultipliers(level, supportConfig.ModuleType);
        switch (supportConfig.ModuleType) {
            case SupportModuleType.Fuel:
                _descriptionText.text += $"\n{GetLocalization("Added fuel")}: ";
                var needCount = supportConfig.FirstCount * firstMultiplayer;
                _descriptionText.text += isUpdate ? $"<color=#5bca4a>{needCount}</color>" : needCount;
                break;
            case SupportModuleType.Turbine:
                _descriptionText.text +=
                    $"\n{GetLocalization("Added fuel consumption")}: ";
                var needCountTurbine = supportConfig.FirstCount * firstMultiplayer;
                var needSecondCountTurbine = supportConfig.FirstCount * firstMultiplayer;
                _descriptionText.text += isUpdate ? $"<color=#5bca4a>{needCountTurbine}</color>" : needCountTurbine;
                _descriptionText.text +=
                    $"\n {GetLocalization("Added max move speed")}: ";
                _descriptionText.text += isUpdate ? $"<color=#5bca4a>{needSecondCountTurbine}</color>" : needSecondCountTurbine;
                break;
            case SupportModuleType.Money:
                _descriptionText.text += $"\n{GetLocalization("Added money after end round")}: ";
                var needCountMoney = supportConfig.FirstCount * firstMultiplayer;
                _descriptionText.text += isUpdate ? $"<color=#5bca4a>{needCountMoney}</color>" : needCountMoney;
                break;
            case SupportModuleType.Ammo:
                _descriptionText.text += $"\n{GetLocalization("Boost reload speed neighboring cells")}: ";
                var needCountAmmo = supportConfig.FirstCount * firstMultiplayer * 100;
                _descriptionText.text += isUpdate ? $"<color=#5bca4a>{needCountAmmo}</color>" : needCountAmmo;
                _descriptionText.text += "%";
                break;
        }
    }
    private void SetGunInfo(GunConfig gunConfig, int level, bool isUpdate) {
        (float damageMultiplier, float reloadMultiplier, float attackCountdownMultiplayer) = GridUtils.GetGunLevelMultipliers(gunConfig,level);
        
        if (gunConfig.UpPartSprite != null) {
            var carPartSize = GridUtils.GetCarPartSize(gunConfig);
            _carPartIconUpLevel.rectTransform.sizeDelta = _carPartIcon.rectTransform.sizeDelta /
                (Math.Max(carPartSize.x, carPartSize.y) * GridUtils.InWorldCellSize) * gunConfig.UpPartSprite.rect.size;
            _carPartIconUpLevel.transform.localPosition = _carPartIcon.rectTransform.sizeDelta /
                Math.Max(carPartSize.x, carPartSize.y) * gunConfig.UpPartPosition;
            _carPartIconUpLevel.sprite = gunConfig.UpPartSprite;
            _carPartIconUpLevel.gameObject.SetActive(true);
        }

        if (gunConfig.MissleType == MissleType.laser) {
            if (isUpdate)
                _descriptionText.text +=
                    $"\n{GetLocalization("Damage")}: <color=#5bca4a>{gunConfig.Damage * damageMultiplier}/s</color> \n{GetLocalization("Laser range")}: " +
                    $"{gunConfig.BulletSpeed}" +
                    $"\n{GetLocalization("Reload")}: <color=#5bca4a>{gunConfig.ReloadDuration / reloadMultiplier:F2}s</color>\n" +
                    $"{GetLocalization("Laser time")}: <color=#5bca4a>{gunConfig.AttackCouldown / attackCountdownMultiplayer:F2}s</color>";

            else
                _descriptionText.text +=
                    $"\n{GetLocalization("Damage")}: {gunConfig.Damage * damageMultiplier}/s \n{GetLocalization("Laser range")}: {gunConfig.BulletSpeed}" +
                    $"\nReload: {gunConfig.ReloadDuration / reloadMultiplier:F2}s\n" +
                    $"{GetLocalization("Laser time")}: {gunConfig.AttackCouldown / attackCountdownMultiplayer:F2}s";
        } else {
            if (isUpdate) {
                if (gunConfig.BulletsCountInMagazine != 1)
                    _descriptionText.text +=
                        $"\n{GetLocalization("Rate of fire")}: <color=#5bca4a>{60f / (gunConfig.AttackCouldown / attackCountdownMultiplayer):F2}/min</color> ";
                _descriptionText.text += $"\n{GetLocalization("Reload")} : <color=#5bca4a>{gunConfig.ReloadDuration / reloadMultiplier:F2}s</color>" +
                                         $"\n{GetLocalization("Damage")}: <color=#5bca4a>{gunConfig.Damage * damageMultiplier}</color>";
            } else {
                if (gunConfig.BulletsCountInMagazine != 1)
                    _descriptionText.text += $"\n{GetLocalization("Rate of fire")}: {60f / (gunConfig.AttackCouldown / attackCountdownMultiplayer):F2}/min ";
                _descriptionText.text += $"\n{GetLocalization("Reload")} : {gunConfig.ReloadDuration / reloadMultiplier:F2}s" +
                                         $"\n{GetLocalization("Damage")}: {gunConfig.Damage * damageMultiplier}";
            }

            if (gunConfig.BulletsCount > 1)
                _descriptionText.text += $"x{gunConfig.BulletsCount}";
            _descriptionText.text += $" \n{GetLocalization("Projectile speed")}: {gunConfig.BulletSpeed} \n" +
                                     $"{GetLocalization("Spread angle")}: {gunConfig.SpreadAngle}°\n" +
                                     $"{GetLocalization("Bullets in mag")}: {gunConfig.BulletsCountInMagazine}";
            if (gunConfig.ExplosionRadius != 0)
                _descriptionText.text += $"\n{GetLocalization("Explosion radius")}: {gunConfig.ExplosionRadius}\n" +
                                         $"{GetLocalization("")}Explosion damage: {gunConfig.ExplosionDamage}";

            if (gunConfig.BreakingThroughCount > 1)
                _descriptionText.text += $"\n{GetLocalization("Pierce count")}: {gunConfig.BreakingThroughCount}";
        }

        if (gunConfig.MaxRotateAngle != 0) {
            _descriptionText.text += $"\n{GetLocalization("Max rotate angle")}: {gunConfig.MaxRotateAngle}°" +
                                     $"\n{GetLocalization("Rotate speed")}: {gunConfig.RotateSpeed}";
        }
    }
}
