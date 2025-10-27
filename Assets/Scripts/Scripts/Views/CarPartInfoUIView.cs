using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CarPartInfoUIView : MonoBehaviour
{
    [field: SerializeField] public Button _moveToInventoryButton { get; private set; }
    [field: SerializeField] public Button _upgradeButton { get; private set; }
    [field: SerializeField] public TMP_Text _upgradeButtonText { get; private set; }
    [field: SerializeField] public Button _destroyButton { get; private set; }
    [field: SerializeField] public Button _backButton { get; private set; }
    [field: SerializeField] public Button _nextButton { get; private set; }

    [field: SerializeField]
    public Transform _switchButtonsContainer;
    [field: SerializeField] public int _currentPartInfoId { get; private set; }
    [field: SerializeField] public TMP_Text _nameText { get; private set; }
    [field: SerializeField] public TMP_Text _descriptionText { get; private set; }
    public TMP_Text CarDescriptionText;
    [field: SerializeField] public Image _carPartIcon { get; private set; }
    [field: SerializeField] public Image _carPartIconUpLevel { get; private set; }

    public void SetupButtonsInfoPanel(Action backCarPart, Action destroy, Action upgrade,Action moveToInventory) {
        _backButton.onClick.AddListener(() => backCarPart?.Invoke());
        _moveToInventoryButton.onClick.AddListener(() => moveToInventory?.Invoke());
        _destroyButton.onClick.AddListener(() => destroy?.Invoke());
        _upgradeButton.onClick.AddListener(() => upgrade?.Invoke());
    }

    public void SetPanelForNeededPart(CarPartConfig carPartConfig, bool showDestroyButton, bool showMoveButton) {
        _destroyButton.gameObject.SetActive(showDestroyButton);
        _moveToInventoryButton.gameObject.SetActive(showMoveButton);
        if (carPartConfig.UpgradedPartCost != 0)
            _upgradeButtonText.text = $"{carPartConfig.UpgradedPartCost.ToString()}$ to upgrade";
        //_upgradeButton.gameObject.SetActive(carPartConfig.UpgradedPartId != 0);
        _currentPartInfoId = carPartConfig.Id; 
        _carPartIcon.sprite = carPartConfig.PartSprite;
        _descriptionText.text = $"{carPartConfig.MaxHealth} HP \nPart type: {carPartConfig.PartType}\nWeight: {carPartConfig.Weight}";
        _nameText.text = carPartConfig.PartName;
        _carPartIconUpLevel.gameObject.SetActive(false);
        if (carPartConfig.PartType == CarPartType.Gun) {
            if (carPartConfig is GunConfig gunConfig) {
                SetGunInfo(carPartConfig, gunConfig);
            }
        }
        else if (carPartConfig.PartType == CarPartType.MeleeWeapon) {
            if (carPartConfig is MeleeWeaponConfig meleeConfig) {
                _descriptionText.text += $"\nDamage: {meleeConfig.Damage}/s \nAdded damage: {meleeConfig.DamageAddedFromOneSpeed}/1 m/s\n" +
                                         $"Attack countdown: {meleeConfig.AttackCountdown}s";
            }
        } else if (carPartConfig.PartType == CarPartType.Armor) {
            if (carPartConfig is ArmorConfig armorConfig) {
                _descriptionText.text += $"\nDefence: {armorConfig.DefencePercent * 100}%";
                if (armorConfig.DefenceDirections.Count == 4)
                    _descriptionText.text += "\nDefence from all directions";
                else
                    _descriptionText.text += "\nDefence from: " + string.Join(", ", armorConfig.DefenceDirections);

                if (armorConfig.ExplosionDefence)
                    _descriptionText.text += " \nPrevents explosion";
            }
        }
    }

    private void SetGunInfo(CarPartConfig carPartConfig, GunConfig gunConfig) {
        if (gunConfig.UpPartSprite != null) {
            var carPartSize = GridUtils.GetCarPartSize(carPartConfig);
            _carPartIconUpLevel.rectTransform.sizeDelta = _carPartIcon.rectTransform.sizeDelta /
                (Math.Max(carPartSize.x, carPartSize.y) * 16) * gunConfig.UpPartSprite.rect.size;
            _carPartIconUpLevel.transform.localPosition = _carPartIcon.rectTransform.sizeDelta /
                Math.Max(carPartSize.x, carPartSize.y) * gunConfig.UpPartPosition;
            _carPartIconUpLevel.sprite = gunConfig.UpPartSprite;
            _carPartIconUpLevel.gameObject.SetActive(true);
        }
                
        if (gunConfig.MissleType == MissleType.laser) {
            _descriptionText.text += $"\nDamage: {gunConfig.Damage}/s \nLaser range: {gunConfig.BulletSpeed}\nReload: {gunConfig.ReloadDuration}s\n" +
                                     $"Laser time: {gunConfig.AttackCouldown}s";
        } else {
            _descriptionText.text += $"\n Damage: {gunConfig.Damage}";
            if (gunConfig.BulletsCount > 1)
                _descriptionText.text += $"x{gunConfig.BulletsCount}";
            _descriptionText.text += $" \nProjectile speed: {gunConfig.BulletSpeed} \nSpread angle: {gunConfig.SpreadAngle}\n" +
                                     $"Reload duration: {gunConfig.ReloadDuration}s\n" +
                                     $"Bullets in mag: {gunConfig.BulletsCountInMagazine}\nRate of fire: {60f / gunConfig.AttackCouldown}/min ";
            if (gunConfig.ExplosionRadius != 0)
                _descriptionText.text +=
                    $"\nExplosion radius: {gunConfig.ExplosionRadius}\nExplosion damage: {gunConfig.ExplosionDamage}";
        }

        if (gunConfig.MaxRotateAngle != 0) {
            _descriptionText.text += $"\nMax rotate angle: {gunConfig.MaxRotateAngle} \nRotate speed: {gunConfig.RotateSpeed}";
        }
    }
}
