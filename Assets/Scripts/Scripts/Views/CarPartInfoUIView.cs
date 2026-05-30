using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CarPartInfoUIView : MonoBehaviour
{
    [field: SerializeField] public TMP_Text _nameModuleWindowText { get; private set; }
    [field: SerializeField] public TMP_Text _descriptionModuleWindowText { get; private set; }
    [field: SerializeField] public RectTransform _moduleWindow { get; private set; }
    
    [field: SerializeField] public Transform _moduleContainer { get; private set; }
    [field: SerializeField] public ModuleStuffView[] _moduleViews { get; private set; }
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
    [field: SerializeField] public TMP_Text _expBarText { get; private set; }
    [field: SerializeField] public Slider _expSlider { get; private set; }
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

    public void SetPanelForModuleInfo(ModuleConfig moduleConfig) {
        _moveToInventoryButton.gameObject.SetActive(false);
        _upgradeButton.gameObject.SetActive(false);
        _moduleContainer.gameObject.SetActive(false);
        _nameText.text = GetLocalization(moduleConfig.ModuleName);
        _carPartIconUpLevel.gameObject.SetActive(false);
        _carPartIcon.sprite = moduleConfig.ModuleIcon;
        _descriptionText.text = GetModuleDescription(moduleConfig);
        _healCarPartButton.gameObject.SetActive(false);
        _sellCarPartText.text = $"{GetLocalization("Sell for")} {moduleConfig.Cost}$";
    }
  
 
    public string GetModuleDescription(ModuleConfig moduleConfig) {
         var _descriptionText = "";
         int i = 0;
         foreach (var moduleBuff in moduleConfig.ModuleBuffs) {
             switch (moduleBuff.Buff) {
                 case Buff.Damage:
                     _descriptionText +=
                         $"{GetLocalization("Damage")}: {(moduleBuff.BuffCount > 0 ? "+" : "")}{moduleBuff.BuffCount * 100}%";
                     break;
                 case Buff.Health:
                     _descriptionText +=
                         $"{GetLocalization("HP")}: {(moduleBuff.BuffCount > 0 ? "+" : "")}{moduleBuff.BuffCount * 100}%";
                     break;
                 case Buff.AttackSpeed:
                     _descriptionText +=
                         $"{GetLocalization("Attack countdown")}: {(moduleBuff.BuffCount > 0 ? "+" : "")}{moduleBuff.BuffCount * 100}%";
                     break;
                 case Buff.UpgradeCost:
                     _descriptionText +=
                         $"{GetLocalization("Upgrade cost")}: {(moduleBuff.BuffCount > 0 ? "+" : "")}{moduleBuff.BuffCount * 100}%";
                     break;
                 case Buff.ReloadSpeed:
                     _descriptionText +=
                         $"{GetLocalization("Reload")}: {(moduleBuff.BuffCount > 0 ? "+" : "")}{moduleBuff.BuffCount * 100}%";
                     break;
                 case Buff.CarRotateSpeed:
                     _descriptionText +=
                         $"{GetLocalization("Rotate speed")}: {(moduleBuff.BuffCount > 0 ? "+" : "")}{moduleBuff.BuffCount * 100}%";
                     break;
                 case Buff.MaxMagazineSize:
                     _descriptionText +=
                         $"{GetLocalization("Bullets in mag")}: {(moduleBuff.BuffCount > 0 ? "+" : "")}{moduleBuff.BuffCount * 100}%";
                     break;
                 case Buff.MaxWeight:
                     _descriptionText +=
                         $"{GetLocalization("Car max weight")}: {(moduleBuff.BuffCount > 0 ? "+" : "")}{moduleBuff.BuffCount * 100}%";
                     break;
                 case Buff.TargetPierceing:
                     _descriptionText += $"{GetLocalization("Pierce count")}: +{moduleBuff.BuffCount}";
                     break;
                 case Buff.CarMaxSpeed:
                     _descriptionText += $"{GetLocalization("Max car speed")}: {(moduleBuff.BuffCount > 0 ? "+" : "")}{moduleBuff.BuffCount * 100}%";
                     break;
                 case Buff.SpreadCount:
                     _descriptionText += $"{GetLocalization("Spread angle")}: {(moduleBuff.BuffCount > 0 ? "+" : "")}{moduleBuff.BuffCount}";
                     break;
                 case Buff.SpreadPercent:
                     _descriptionText += $"{GetLocalization("Spread angle")}: {(moduleBuff.BuffCount > 0 ? "+" : "")}{moduleBuff.BuffCount * 100}%";
                     break;
             }

                 _descriptionText += "\n";
             
         }
       //  _descriptionText += $"{GetLocalization("Sell for")} {moduleConfig.Cost}$";
         return _descriptionText;
    }

    public void SetPanelForNeededPart(CarPartConfig carPartConfig, bool showDestroyButton, bool showMoveButton, int carPartLevel,
        float currentHealth, float healCost, bool isShop, int[] moduleIds,float currentExp) {
        _destroyButton.gameObject.SetActive(showDestroyButton);
        _moveToInventoryButton.gameObject.SetActive(showMoveButton);
        _level = carPartLevel;
        _moduleContainer.gameObject.SetActive(true);
        CurrentMarkedCarPartHealth = currentHealth;

        _nameText.text = $"{GetLocalization(carPartConfig.PartName)}";

        if (!isShop) {
            if (GridUtils._maxLevelsForRare[carPartConfig.RartRare] > carPartLevel) {
                float upgradeCost = GridUtils.GetUpgradeCost(carPartLevel, carPartConfig, moduleIds);
                _upgradeButtonText.text =
                    $"{upgradeCost} <sprite name=\"ExpIcon\"> {GetLocalization("to upgrade")}";
                _upgradeButton.enabled = true;
                _expSlider.value = Mathf.Max(currentExp / upgradeCost, 0.093f) ;
                _expBarText.text = $"{currentExp:F2} / {upgradeCost:F2} <sprite name=\"ExpIcon\">";
            } else {
                _upgradeButton.enabled = false;
                _upgradeButtonText.text = GetLocalization("Max level");
                _expSlider.value = 1;
                _expBarText.text  = GetLocalization("Max level");
            }

            _nameText.text += $"\n{carPartLevel + 1} {GetLocalization("lvl")}.";
        }

        for (int i = 0; i < _moduleViews.Length; i++) {
            _moduleViews[i].gameObject.SetActive((int)carPartConfig.RartRare + 1 > i && !isShop);
            if ((int)carPartConfig.RartRare + 1 > i && !isShop && moduleIds[i] != -1) {
                _moduleViews[i].ModuleIcon.sprite = BetweenScenesService.Instance._carStuffListsConfig.ModuleConfigs[moduleIds[i]].ModuleIcon;
                _moduleViews[i].ModuleIcon.color = Color.white;
            } else
                _moduleViews[i].ModuleIcon.color = Color.clear;
        }

        _healCarPartButton.gameObject.SetActive(healCost > 0.001f);
        if (healCost > 0.001f) {
            _healCarPartText.text = $"{GetLocalization("Heal car part for")} {healCost:F3} <sprite name=\"CogIcon\">";
            _healCarPartText.color = Color.clear;
        }

        _carPartIconUpLevel.gameObject.SetActive(false);
        _currentCarPartConfig = carPartConfig;
        _carPartIcon.sprite = carPartConfig.PartSprite;

        SetDescriptionInfo(false, moduleIds);

        _sellCarPartText.text = $"{GetLocalization("Sell for")} {GridUtils.GetSellValueForCarPart(carPartConfig, carPartLevel, moduleIds)} <sprite name=\"CogIcon\">";
    }

    public void SetDescriptionInfo(bool isUpdate, int[] moduleIds) {
        var moduleMultipliers  = GridUtils.GetModulesMultipliers(moduleIds);
        int needLevel = isUpdate ? _level + 1 : _level;
        float maxHealth = _currentCarPartConfig.MaxHealth * GridUtils.GetHealthLevelMultiplier(needLevel)*moduleMultipliers.healthMultiplayer;
        _descriptionText.text = $"{CurrentMarkedCarPartHealth:F2}/";
        _descriptionText.text += isUpdate ?  $"<color=#5bca4a>{maxHealth:F2}</color>":$"{maxHealth:F2}";
        _descriptionText.text += $" {GetLocalization("HP")} \n{GetLocalization("Part rare")}:" +
                                 $" {GetLocalization(_currentCarPartConfig.RartRare.ToString())}\n{GetLocalization("Weight")}: {_currentCarPartConfig.Weight} {GetLocalization("kg")}";
       
        
        if (_currentCarPartConfig.PartType == CarPartType.Gun) {
            if (_currentCarPartConfig is GunConfig gunConfig) {
                SetGunInfo(gunConfig, needLevel, isUpdate, moduleMultipliers.damageMultiplaier, moduleMultipliers.attackSpeedMultiplayer,
                    moduleMultipliers.targetPiercingAddedCount, moduleMultipliers.reloadSpeedMultiplayer, moduleMultipliers.magSizeMultiplier,
                    moduleMultipliers.spreadCount, moduleMultipliers.spreadMultiplier);
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
                var damage = moduleMultipliers.damageMultiplaier * meleeConfig.Damage * damageMultiplier;
                var attackCountdown = meleeConfig.AttackCountdown * moduleMultipliers.attackSpeedMultiplayer;
                var damageAddedFromSpeed = meleeConfig.DamageAddedFromOneSpeed * damageBySpeedMultiplier * damageMultiplier;
                if (isUpdate) {
                    if (damageMultiplier != 1f)
                        _descriptionText.text += $"\n{GetLocalization("Damage")}: <color=#5bca4a>{damage}</color> ";
                    else
                        _descriptionText.text += $"\n{GetLocalization("Damage")}: {damage}";
                    if (damageBySpeedMultiplier != 1f)
                        _descriptionText.text +=
                            $"\n{GetLocalization("Physical damage")}: <color=#5bca4a>{damageAddedFromSpeed:F2}</color>\n";
                    else
                        _descriptionText.text += $"\n{GetLocalization("Physical damage")}: {damageAddedFromSpeed:F2}\n";

                } else
                    _descriptionText.text += $"\n{GetLocalization("Damage")}: {damage} \n{GetLocalization("Physical damage")}: {damageAddedFromSpeed:F2}\n";

                _descriptionText.text += $"{GetLocalization("Attack countdown")}: {attackCountdown:F2}s\n";
                if (meleeConfig.FuelConsumption != 0)
                    _descriptionText.text += $"{GetLocalization("Constant fuel consumption")}: {meleeConfig.FuelConsumption:F2}";
            }
        } else if (_currentCarPartConfig.PartType == CarPartType.Armor) {
            if (_currentCarPartConfig is ArmorConfig armorConfig) {
                float defenceMultiplayer = GridUtils.GetArmorLevelMultipliers(needLevel);
                float defencePoints = armorConfig.DefencePercent * defenceMultiplayer;
                if (!isUpdate)
                    _descriptionText.text += $"\n{GetLocalization("Defence")}: {defencePoints} ({defencePoints / (defencePoints + 100f) * 100f:F2}%)";
                else
                    _descriptionText.text +=
                        $"\n{GetLocalization("Defence")}: <color=#5bca4a>{defencePoints} ({defencePoints / (defencePoints + 100f) * 100f:F2}%)</color>";

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
                var maxWeight = wheelConfig.MaxWeight * GridUtils.GetMaxWeightLevelMultiplier(needLevel)*moduleMultipliers.maxWeightMultiplier;
                _descriptionText.text += $"\n{GetLocalization("Car max weight")}: ";
                _descriptionText.text += isUpdate ? $"<color=#5bca4a>{maxWeight}</color>" : maxWeight;
                _descriptionText.text +=$"\n{GetLocalization("Stability radius")}:{wheelConfig.StabilityRadius} {GetLocalization("m")}";
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
                    $"\n{GetLocalization("Boost fuel consumption")}: +";
                var needCountTurbine = supportConfig.FirstCount * firstMultiplayer;
                var needSecondCountTurbine = supportConfig.SecondCount * secondMultiplayer;
                _descriptionText.text += isUpdate ? $"<color=#5bca4a>{needCountTurbine}</color>" : needCountTurbine;
                _descriptionText.text +=
                    $"\n {GetLocalization("Boost max move speed")}: +";
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
            case SupportModuleType.FireExtinguisher or SupportModuleType.Heater:
                if (supportConfig.ModuleType == SupportModuleType.FireExtinguisher)
                    _descriptionText.text += $"\n{GetLocalization("Extinguishes fire per second")}: ";
                else
                    _descriptionText.text += $"\n{GetLocalization("Heat ice per second")}: ";
                var needCountFireExtinguisher = supportConfig.FirstCount * firstMultiplayer;
                _descriptionText.text += isUpdate ? $"<color=#5bca4a>{needCountFireExtinguisher}</color>" : needCountFireExtinguisher;
                _descriptionText.text +=
                    $"\n{GetLocalization("Radius")}: {supportConfig.SecondCount}";
                if (supportConfig.ModuleType ==SupportModuleType.Heater) {
                    var consumption = supportConfig.SecondCount*supportConfig.FirstCount*firstMultiplayer/25f;
                     _descriptionText.text += $"\n{GetLocalization("Electricity consumption")}: ";
                     _descriptionText.text += isUpdate ? $"<color=#5bca4a>{consumption}</color>" : consumption;
                }
                break;
            case SupportModuleType.Battery:
                _descriptionText.text += $"\n{GetLocalization("Added electricity")}: ";
                var needCountElectricity = supportConfig.FirstCount * firstMultiplayer;
                _descriptionText.text += isUpdate ? $"<color=#5bca4a>{needCountElectricity}</color>" : needCountElectricity;
                break;
            case SupportModuleType.ElectricGenerator:
                _descriptionText.text += $"\n{GetLocalization("Electricity generation")}: ";
                var needCountElectricGenerator = supportConfig.FirstCount * firstMultiplayer;
              
                _descriptionText.text += isUpdate ? $"<color=#5bca4a>{needCountElectricGenerator}</color>" : needCountElectricGenerator;
                _descriptionText.text += $"{GetLocalization("w/s")}: ";
                if (supportConfig.SecondCount != 0) {
                      var electricGeneratorFuelConsumption = supportConfig.SecondCount * secondMultiplayer;
                      _descriptionText.text += $"\n{GetLocalization("Constant fuel consumption")}: ";
                      _descriptionText.text += isUpdate ? $"<color=#5bca4a>{electricGeneratorFuelConsumption}</color>" : electricGeneratorFuelConsumption;
                }
                break;
        }
    }

    private void SetGunInfo(GunConfig gunConfig, int level, bool isUpdate, float damageMultiplayer, float attackSpeedMultiplayer,
        int targetPiercingAddedCount, float reloadSpeedMultiplayer, float magSizeMultiplier, float spreadCount, float spreadMultipliers) {
        (float damageMultiplier, float reloadMultiplier, float attackCountdownMultiplayer) = GridUtils.GetGunLevelMultipliers(gunConfig, level);

        if (gunConfig.UpPartSprite != null) {
            var carPartSize = GridUtils.GetCarPartSize(gunConfig);
            _carPartIconUpLevel.rectTransform.sizeDelta = _carPartIcon.rectTransform.sizeDelta /
                (Math.Max(carPartSize.x, carPartSize.y) * GridUtils.InWorldCellSize) * gunConfig.UpPartSprite.rect.size;
            _carPartIconUpLevel.transform.localPosition = _carPartIcon.rectTransform.sizeDelta /
                Math.Max(carPartSize.x, carPartSize.y) * gunConfig.UpPartPosition;
            _carPartIconUpLevel.sprite = gunConfig.UpPartSprite;
            _carPartIconUpLevel.gameObject.SetActive(true);
        }

        float damage = gunConfig.Damage * damageMultiplier * damageMultiplayer;
        float reloadSpeed = gunConfig.ReloadDuration / reloadMultiplier * reloadSpeedMultiplayer;
        if (gunConfig.MissleType == MissleType.laser) {
            if (isUpdate)
                _descriptionText.text +=
                    $"\n{GetLocalization("Damage")}: <color=#5bca4a>{damage}/s</color> \n{GetLocalization("Laser range")}: " +
                    $"{gunConfig.BulletSpeed}" + $"\n{GetLocalization("Reload")}: <color=#5bca4a>{reloadSpeed:F2}s</color>\n" +
                    $"{GetLocalization("Laser time")}: <color=#5bca4a>{gunConfig.AttackCouldown / attackCountdownMultiplayer / attackSpeedMultiplayer:F2}s</color>";

            else
                _descriptionText.text +=
                    $"\n{GetLocalization("Damage")}: {damage}/s \n{GetLocalization("Laser range")}: {gunConfig.BulletSpeed}" +
                    $"\nReload: {reloadSpeed:F2}s\n" +
                    $"{GetLocalization("Laser time")}: {gunConfig.AttackCouldown / attackCountdownMultiplayer / attackSpeedMultiplayer:F2}s";
            
            if (gunConfig.ElectricityConsumption != 0)
                _descriptionText.text += $"\n{GetLocalization("Electricity consumption")}: {gunConfig.ElectricityConsumption} w/s";
        } else {
            if (isUpdate) {
                if (gunConfig.BulletsCountInMagazine != 1)
                    _descriptionText.text +=
                        $"\n{GetLocalization("Rate of fire")}: <color=#5bca4a>{60f / (gunConfig.AttackCouldown / attackCountdownMultiplayer * attackSpeedMultiplayer):F2}/min</color> ";
                _descriptionText.text += $"\n{GetLocalization("Reload")} : <color=#5bca4a>{reloadSpeed:F2}s</color>" +
                                         $"\n{GetLocalization("Damage")}: <color=#5bca4a>{damage}</color>";
            } else {
                if (gunConfig.BulletsCountInMagazine != 1)
                    _descriptionText.text +=
                        $"\n{GetLocalization("Rate of fire")}: {60f / (gunConfig.AttackCouldown / attackCountdownMultiplayer * attackSpeedMultiplayer):F2}/min ";
                _descriptionText.text += $"\n{GetLocalization("Reload")} : {reloadSpeed:F2}s" + $"\n{GetLocalization("Damage")}: {damage}";
            }

            if (gunConfig.BulletsCount > 1)
                _descriptionText.text += $"x{gunConfig.BulletsCount}";
            _descriptionText.text += $" \n{GetLocalization("Projectile speed")}: {gunConfig.BulletSpeed} \n" +
                                     $"{GetLocalization("Spread angle")}: {(gunConfig.SpreadAngle+spreadCount)*spreadMultipliers}°\n" +
                                     $"{GetLocalization("Bullets in mag")}: {Mathf.FloorToInt(gunConfig.BulletsCountInMagazine * magSizeMultiplier)}";
            if (gunConfig.ExplosionRadius != 0)
                _descriptionText.text += $"\n{GetLocalization("Explosion radius")}: {gunConfig.ExplosionRadius}\n" +
                                         $"{GetLocalization("Explosion damage")}: {gunConfig.ExplosionDamage}";

            if (gunConfig.BreakingThroughCount > 1) {
                _descriptionText.text += $"\n{GetLocalization("Pierce count")}: {gunConfig.BreakingThroughCount + targetPiercingAddedCount}" +
                                         $"\n{GetLocalization("Damage fall off")}: {gunConfig.BreakingThroughDamageFalloff*100}%";
                
            }
                

            if (gunConfig.CarRecoil > 0)
                _descriptionText.text += $"\n{GetLocalization("Car recoil")}: {gunConfig.CarRecoil}";
            
            if (gunConfig.ElectricityConsumption != 0)
                _descriptionText.text += $"\n{GetLocalization("Electricity consumption")}: {gunConfig.ElectricityConsumption} w";
        }

        if (gunConfig.MaxRotateAngle != 0) {
            _descriptionText.text += $"\n{GetLocalization("Max rotate angle")}: {gunConfig.MaxRotateAngle}°" +
                                     $"\n{GetLocalization("Rotate speed")}: {gunConfig.RotateSpeed}" +
                                     $"\n{GetLocalization("Rotate electricity consumption")}: {gunConfig.RotateSpeed / 20f} w/s";
        }
        if (gunConfig.ProjectileRotateSpeed != 0) {
            _descriptionText.text += $"\n{GetLocalization("Homing projectiles")}" +
                                     $"\n{GetLocalization("Projectile turning speed")}: {gunConfig.ProjectileRotateSpeed}°/s";
        }
           

       

    }
}
