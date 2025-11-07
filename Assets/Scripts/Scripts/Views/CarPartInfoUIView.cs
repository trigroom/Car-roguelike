using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CarPartInfoUIView : MonoBehaviour
{
    [field: SerializeField] public TMP_Text _carPartLevelText { get; private set; }
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
    public int _level;

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

    public void SetPanelForNeededPart(CarPartConfig carPartConfig, bool showDestroyButton, bool showMoveButton, int carPartLevel) {
        _destroyButton.gameObject.SetActive(showDestroyButton);
        _moveToInventoryButton.gameObject.SetActive(showMoveButton);
        _level = carPartLevel;
        if (GridUtils.GetUpgradeCost(carPartLevel,carPartConfig) != 0)
            _upgradeButtonText.text = $"{GridUtils.GetUpgradeCost(carPartLevel,carPartConfig).ToString()}$ to upgrade";
        //_upgradeButton.gameObject.SetActive(carPartConfig.UpgradedPartId != 0);

        _nameText.text = carPartConfig.PartName;
       
        if (carPartLevel != 0)
            _nameText.text += $"\n{carPartLevel} lvl.";

        _carPartIconUpLevel.gameObject.SetActive(false);
        _currentCarPartConfig = carPartConfig;
        _carPartIcon.sprite = carPartConfig.PartSprite;

        SetDescriptionInfo(false);
    }

    public void SetDescriptionInfo(bool isUpdate) {
    
        _descriptionText.text =
            $"{_currentCarPartConfig.MaxHealth} HP \nPart type: {_currentCarPartConfig.PartType}\nWeight: {_currentCarPartConfig.Weight}";
        int needLevel = isUpdate ? _level + 1 : _level;
        Debug.Log(needLevel);
        if (_currentCarPartConfig.PartType == CarPartType.Gun) {
            if (_currentCarPartConfig is GunConfig gunConfig) {
                SetGunInfo(gunConfig, needLevel, isUpdate);
            }
        } else if (_currentCarPartConfig.PartType == CarPartType.MeleeWeapon) {
            if (_currentCarPartConfig is MeleeWeaponConfig meleeConfig) {
                
                if (meleeConfig.UpSprite != null) {
                    var carPartSize = GridUtils.GetCarPartSize(meleeConfig);
                    _carPartIconUpLevel.rectTransform.sizeDelta = _carPartIcon.rectTransform.sizeDelta /
                        (Math.Max(carPartSize.x, carPartSize.y) * 16) * meleeConfig.UpSprite.rect.size;
                    _carPartIconUpLevel.transform.localPosition = _carPartIcon.rectTransform.sizeDelta /
                        Math.Max(carPartSize.x, carPartSize.y) * meleeConfig.UpSpritePosition;
                    _carPartIconUpLevel.sprite = meleeConfig.UpSprite;
                    _carPartIconUpLevel.gameObject.SetActive(true);
                }
                (float damageMultiplier, float damageBySpeedMultiplier) = GridUtils.GetMeleeLevelMultipliers(meleeConfig,needLevel);
                if (isUpdate) {
                    if (damageMultiplier != 1)
                        _descriptionText.text += $"\nDamage: <color=#5bca4a>{meleeConfig.Damage * damageMultiplier}/s</color> ";
                    else
                        _descriptionText.text += $"\nDamage: {meleeConfig.Damage * damageMultiplier}/s ";
                    if (damageBySpeedMultiplier != 1)
                        _descriptionText.text +=
                            $"\nAdded damage: <color=#5bca4a>{meleeConfig.DamageAddedFromOneSpeed * damageBySpeedMultiplier:F2}/1 m/s</color>\n";
                    else
                        _descriptionText.text += $"\nAdded damage: {meleeConfig.DamageAddedFromOneSpeed * damageBySpeedMultiplier:F2}/1 m/s\n";
                    _descriptionText.text += $"Attack countdown: {meleeConfig.AttackCountdown:F2}s";
                } else
                    _descriptionText.text +=
                        $"\nDamage: {meleeConfig.Damage * damageMultiplier}/s \nAdded damage: {meleeConfig.DamageAddedFromOneSpeed * damageBySpeedMultiplier:F2}/1 m/s\n" +
                        $"Attack countdown: {meleeConfig.AttackCountdown:F2}s";
            }
        } else if (_currentCarPartConfig.PartType == CarPartType.Armor) {
            if (_currentCarPartConfig is ArmorConfig armorConfig) {
                float defenceMultiplayer = GridUtils.GetArmorLevelMultipliers(needLevel);
                float defencePoints = armorConfig.DefencePercent * defenceMultiplayer;
                if (!isUpdate)
                    _descriptionText.text += $"\nDefence: {defencePoints} ({defencePoints / (defencePoints + 100f) * 100f:F2}%)";
                else
                    _descriptionText.text +=
                        $"\nDefence: <color=#5bca4a>{defencePoints} ({defencePoints / (defencePoints + 100f) * 100f:F2}%)</color>";

                if (armorConfig.DefenceDirections.Count == 4)
                    _descriptionText.text += "\nDefence from all directions";
                else
                    _descriptionText.text += "\nDefence from: " + string.Join(", ", armorConfig.DefenceDirections);

                if (armorConfig.ExplosionDefence)
                    _descriptionText.text += " \nPrevents explosion";
            }
        } else if (_currentCarPartConfig.PartType == CarPartType.Support) {
            if (_currentCarPartConfig is SupportModuleCarPartConfig supportConfig) {
                SetSupportPartInfo(supportConfig, needLevel, isUpdate);
            }
        }
        else if (_currentCarPartConfig.PartType == CarPartType.WheelsContainer) {
            if (_currentCarPartConfig is WheelsContainerConfig wheelConfig) {
                _descriptionText.text += $"\nCar max weight: {wheelConfig.MaxWeight}";
            }
        }
    }

    private void SetSupportPartInfo(SupportModuleCarPartConfig supportConfig, int level, bool isUpdate) {
        //now level doesnt scale stats

        switch (supportConfig.ModuleType) {
            case SupportModuleType.Fuel:
                _descriptionText.text += $"\nAdded fuel: {supportConfig.FirstCount}";
                break;
            case SupportModuleType.Turbine:
                _descriptionText.text += $"\nAdded fuel consumption: {supportConfig.FirstCount}\n Added max move speed: {supportConfig.SecondCount}";
                break;
        }
    }
    private void SetGunInfo(GunConfig gunConfig, int level, bool isUpdate) {
        (float damageMultiplier, float reloadMultiplier, float attackCountdownMultiplayer) = GridUtils.GetGunLevelMultipliers(gunConfig,level);
        
        if (gunConfig.UpPartSprite != null) {
            var carPartSize = GridUtils.GetCarPartSize(gunConfig);
            _carPartIconUpLevel.rectTransform.sizeDelta = _carPartIcon.rectTransform.sizeDelta /
                (Math.Max(carPartSize.x, carPartSize.y) * 16) * gunConfig.UpPartSprite.rect.size;
            _carPartIconUpLevel.transform.localPosition = _carPartIcon.rectTransform.sizeDelta /
                Math.Max(carPartSize.x, carPartSize.y) * gunConfig.UpPartPosition;
            _carPartIconUpLevel.sprite = gunConfig.UpPartSprite;
            _carPartIconUpLevel.gameObject.SetActive(true);
        }

        if (gunConfig.MissleType == MissleType.laser) {
            if (isUpdate)
                _descriptionText.text +=
                    $"\nDamage: <color=#5bca4a>{gunConfig.Damage * damageMultiplier}/s</color> \nLaser range: {gunConfig.BulletSpeed}" +
                    $"\nReload: <color=#5bca4a>{gunConfig.ReloadDuration / reloadMultiplier:F2}s</color>\n" +
                    $"Laser time: <color=#5bca4a>{gunConfig.AttackCouldown / attackCountdownMultiplayer:F2}s</color>";

            else
                _descriptionText.text +=
                    $"\nDamage: {gunConfig.Damage * damageMultiplier}/s \nLaser range: {gunConfig.BulletSpeed}\nReload: {gunConfig.ReloadDuration / reloadMultiplier:F2}s\n" +
                    $"Laser time: {gunConfig.AttackCouldown / attackCountdownMultiplayer:F2}s";
        } else {
            if (isUpdate) {
                _descriptionText.text += $"\nDamage: <color=#5bca4a>{gunConfig.Damage * damageMultiplier}</color>" +
                                         $"\nReload duration: <color=#5bca4a>{gunConfig.ReloadDuration / reloadMultiplier:F2}s</color>" +
                                         $"\nRate of fire: <color=#5bca4a>{60f / (gunConfig.AttackCouldown / attackCountdownMultiplayer):F2}/min</color> ";
            } else {
                _descriptionText.text += $"\nDamage: {gunConfig.Damage * damageMultiplier}" +
                                         $"\nReload duration: {gunConfig.ReloadDuration / reloadMultiplier:F2}s" +
                                         $"\nRate of fire: {60f / (gunConfig.AttackCouldown / attackCountdownMultiplayer):F2}/min ";
            }
            
            if (gunConfig.BulletsCount > 1)
                _descriptionText.text += $"x{gunConfig.BulletsCount}";
            _descriptionText.text += $" \nProjectile speed: {gunConfig.BulletSpeed} \nSpread angle: {gunConfig.SpreadAngle}\n" +
                                     $"Bullets in mag: {gunConfig.BulletsCountInMagazine}";
            if (gunConfig.ExplosionRadius != 0)
                _descriptionText.text += $"\nExplosion radius: {gunConfig.ExplosionRadius}\nExplosion damage: {gunConfig.ExplosionDamage}";
            
            if (gunConfig.BreakingThroughCount > 1)
                _descriptionText.text += $"\nPierce count: {gunConfig.BreakingThroughCount}";
        }

        if (gunConfig.MaxRotateAngle != 0) {
            _descriptionText.text += $"\nMax rotate angle: {gunConfig.MaxRotateAngle} \nRotate speed: {gunConfig.RotateSpeed}";
        }
    }
}
