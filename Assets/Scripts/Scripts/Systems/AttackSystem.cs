using System.Collections.Generic;
using Client;
using DG.Tweening;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using Leopotam.EcsLite.ExtendedSystems;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.UI;

    public enum AttackType {
            Melee,
            Projectile,
            Laser
        }
    sealed class AttackSystem : IEcsRunSystem {  
        private EcsWorldInject _world;
        private EcsCustomInject<SceneService> _sceneData;

        private EcsPoolInject<CarPartCellComponent> _carPartCellComponentsPool;
        private EcsPoolInject<CarComponent> _carComponentsPool;
        private EcsPoolInject<GunComponent> _gunComponentsPool;
        private EcsPoolInject<MovementComponent> _movementComponentsPool;
        private EcsPoolInject<DestroyEntityEvent> _destroyEntityEventsPool;
        private EcsPoolInject<ProjectileComponent> _projectileComponentsPool;
        private EcsPoolInject<CarPartInGameUIComponent> _carPartInGameUIComponentsPool;
        private EcsPoolInject<MeleeWeaponComponent> _meleeWeaponComponentsPool;
        private EcsPoolInject<CurrentGameComponent> _currentGameComponentsPool;
        private EcsPoolInject<TaskComponent> _taskComponentsPool;
        private EcsPoolInject<LaserComponent> _laserComponentsPool;
        private EcsPoolInject<ShockEffectComponent> _shockEffectComponentsPool;
        private EcsPoolInject<PlayerCurrencyComponent> _playerCurrencyComponentsPool;
        private EcsPoolInject<FireComponent> _fireComponentsPool;
        private EcsPoolInject<CarPartComponent> _carPartComponentsPool;
        private EcsPoolInject<InventoryComponent> _inventoryComponentsPool;
        private EcsPoolInject<WheelContainerComponent> _wheelContainerComponentsPool;
        
        private EcsFilterInject<Inc<ShockEffectComponent>> _shockEffectComponentsFilter;
        private EcsFilterInject<Inc<CarComponent>> _carComponentsFilter;
        private EcsFilterInject<Inc<FireComponent>> _fireComponentsFilter;
        private EcsFilterInject<Inc<TaskComponent>> _taskComponentsFilter;
        private EcsFilterInject<Inc<MeleeWeaponComponent>> _meleeWeaponComponentsFilter;
        private EcsFilterInject<Inc<GunComponent>> _gunComponentsFilter;
        private EcsFilterInject<Inc<EndRoundEvent>> _endRoundEventsFilter;
        private EcsFilterInject<Inc<CarPartCellComponent>> _carPartCellComponentsFilter;
        private EcsFilterInject<Inc<ProjectileComponent>> _projectileComponentsFilter;
        private EcsFilterInject<Inc<ConstructMeleeCellEvent>> _constructMeleeCellEventsFilter;

        public void Run(IEcsSystems systems) {
            foreach (var endRoundEntity in _endRoundEventsFilter.Value) {
                Debug.Log(endRoundEntity + " des all cars");
                EndRound();
            }

            float deltaTime = Time.deltaTime;
            ref var curGameCmp = ref _currentGameComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
            if (curGameCmp.CurrentRoundType == RoundType.None) return;
            foreach (var gunEntity in _gunComponentsFilter.Value) {
                if (_shockEffectComponentsPool.Value.Has(gunEntity)) {
                    if (_laserComponentsPool.Value.Has(gunEntity))
                        _laserComponentsPool.Value.Get(gunEntity).LaserLineRenderer.gameObject.SetActive(false);
                    continue;
                }

                ref var gunCmp = ref _gunComponentsPool.Value.Get(gunEntity);
                var carPartCmp = _carPartComponentsPool.Value.Get(gunEntity);
                var gunConfig = _sceneData.Value.GetGunFromCarPart(carPartCmp.CarPartId);

                gunCmp.CurrentCountdown -= deltaTime;

                if (gunCmp.UpPartContainer != null && gunConfig.RotateSpeed != 0) {
                    gunCmp.GunOnTargetTime -= deltaTime;
                    if (gunCmp.GunOnTargetTime <= 0) {
                        var needLayer = carPartCmp.MainEntity == _sceneData.Value.PlayerEntity
                            ? LayerMask.GetMask("UpCarPartEnemy")
                            : LayerMask.GetMask("UpCarPart");
                        RaycastHit2D hit = Physics2D.Raycast(gunCmp.FirePointTransform.position, gunCmp.FirePointTransform.up, 7, needLayer);
                      
                        gunCmp.GunOnTarget = hit.collider != null;
                       
                       
                        gunCmp.GunOnTargetTime += 0.2f;
                    }

                    if (!gunCmp.GunOnTarget) {
                        gunCmp.CurrentRotateDegrees += Time.deltaTime * gunConfig.RotateSpeed;

                        float pingPongValue = Mathf.PingPong(gunCmp.CurrentRotateDegrees, 1f);

                        float targetAngle = Mathf.Lerp(-gunConfig.MaxRotateAngle, gunConfig.MaxRotateAngle, pingPongValue);

                        gunCmp.UpPartContainer.localEulerAngles = new Vector3(0, 0, targetAngle);
                    }
                }

                if (gunCmp.CurrentCountdown <= 0) {
                    if (gunConfig.MissleType != MissleType.laser)
                        SpawnProjectile(gunEntity);
                    else {
                        TrySpawnLaser(gunEntity);
                        if (gunCmp.CurrentCountdown < -gunConfig.AttackCouldown) {
                            gunCmp.CurrentCountdown = gunConfig.ReloadDuration;
                            _laserComponentsPool.Value.Get(gunEntity).LaserLineRenderer.gameObject.SetActive(false);
                        }
                    }
                }
            }

            foreach (var projectileEntity in _projectileComponentsFilter.Value) {
                ref var projectileCmp = ref _projectileComponentsPool.Value.Get(projectileEntity);
                if (!projectileCmp.IsActive) continue;
                projectileCmp.LifeTime -= deltaTime;
                if (projectileCmp.LifeTime <= 0) {
                    projectileCmp.IsActive = false;
                    _sceneData.Value.ReleaseGunProjectile(projectileCmp.ProjectileView, projectileCmp.ExplosionRadius);
                }
            }

            foreach (var cellEntity in _constructMeleeCellEventsFilter.Value) {
                ConstructMeleeWeaponCell(cellEntity);
            }

            foreach (var cellEntity in _meleeWeaponComponentsFilter.Value) {
                ref var meleeCmp = ref _meleeWeaponComponentsPool.Value.Get(cellEntity);
                if (meleeCmp.Countdown > 0)
                    meleeCmp.Countdown -= deltaTime;
            }

            foreach (var shockEntity in _shockEffectComponentsFilter.Value) {
                ref var shockCmp = ref _shockEffectComponentsPool.Value.Get(shockEntity);
                shockCmp.effectDuration -= deltaTime;
                if (shockCmp.effectDuration <= 0) {
                    var damagedCarPart = _carPartComponentsPool.Value.Get(shockEntity).CarPartId;
                    if (_sceneData.Value.GetCarPartConfig(damagedCarPart).PartType == CarPartType.Main)
                        _carComponentsPool.Value.Get(_carPartComponentsPool.Value.Get(shockEntity).MainEntity).ShockedMainPartsCount--;
                    _shockEffectComponentsPool.Value.Del(shockEntity);
                }
            }

            foreach (var burnedEntity in _fireComponentsFilter.Value) {
                ref var fireCmp = ref _fireComponentsPool.Value.Get(burnedEntity);
                fireCmp.Duration -= deltaTime;
                fireCmp.LastFireSeconds += deltaTime;
                if (fireCmp.LastFireSeconds >= 1) {
                    fireCmp.LastFireSeconds--;
                    int damage = Mathf.CeilToInt(fireCmp.Duration);
                    ChangeHealth(damage, burnedEntity);
                    if (_carPartComponentsPool.Value.Get(burnedEntity).CurrentHealth <= 0 || _carPartComponentsPool.Value.Get(burnedEntity).CarPartCellRenderer == null) {
                        _fireComponentsPool.Value.Del(burnedEntity);
                        continue;
                    }

                    _sceneData.Value.GetFloatingTextPrefab(
                        _carPartComponentsPool.Value.Get(burnedEntity).CarPartCellRenderer.transform.position, damage.ToString("F2"),
                        new Color(1, 0.5f, 0.1f, 1));
                    if (damage <= 1) {
                        _fireComponentsPool.Value.Del(burnedEntity);
                    }
                }
            }
        }

        private void TrySpawnLaser(int entity) {
            ref var carPartCmp = ref _carPartComponentsPool.Value.Get(entity);
            ref var gunCmp = ref _gunComponentsPool.Value.Get(entity);
            if(gunCmp.FirePointTransform == null)return;
            var laserLineRenderer = _laserComponentsPool.Value.Get(entity).LaserLineRenderer;
            var gunConfig = _sceneData.Value.GetGunFromCarPart(carPartCmp.CarPartId);
            float laserRange = gunConfig.BulletSpeed;
            var firePoint = gunCmp.FirePointTransform;

            // gunCmp.

            Vector2 worldFirePoint = firePoint.position;

            Vector2 direction = firePoint.up;

            LayerMask layerMask = carPartCmp.MainEntity != _sceneData.Value.PlayerEntity
                ? LayerMask.GetMask(new string[] {
                    /* "CarBase",*/ "UpCarPart"
                })
                : LayerMask.GetMask(new string[] {
                    /*"EnemyCarBase",*/ "UpCarPartEnemy"
                });
            RaycastHit2D hit = Physics2D.Raycast(worldFirePoint, direction, laserRange, layerMask);
            Vector2 endPoint;
            if (hit.collider != null) {
                endPoint = hit.point;

                var damagedCarPart = hit.collider.GetComponent<CarPartStuffView>();
                ProjectileHit(AttackType.Laser, gunConfig.Id, damagedCarPart._gridPosition, damagedCarPart._mainEntity, hit.point, null,
                    entity);
            } else {
                // Если не попали, конечная точка на максимальной дистанции
                endPoint = worldFirePoint + direction * laserRange;
            }

            laserLineRenderer.SetPosition(0, worldFirePoint);
            laserLineRenderer.SetPosition(1, endPoint);
            laserLineRenderer.gameObject.SetActive(true);
        }
        private void EndRound() {
            DestroyAllProjectiles();
            DestroyAllCars();

            DestroyBrokenParts();
        }

        private void LoseGame() {
            Debug.Log("LoseGame");
            if (_currentGameComponentsPool.Value.Get(_sceneData.Value.PlayerEntity).CurrentRoundType != RoundType.None)
                EndRound();
            _sceneData.Value._endGameScreenView.SetLoseScreenInfo(_currentGameComponentsPool.Value.Get(_sceneData.Value.PlayerEntity).CurrentRound);
            //go to meta
        }
        private void DestroyAllProjectiles() {
            foreach (var projectileEntity in _projectileComponentsFilter.Value) {
                ref var projectileCmp = ref _projectileComponentsPool.Value.Get(projectileEntity);
                if(!projectileCmp.IsActive)continue;
                _sceneData.Value.ReleaseGunProjectile(projectileCmp.ProjectileView,0);
                projectileCmp.IsActive = false;
            }
          
        }
        private void SetPlayerCarPartsGridFromList() {
            ref var carCmp= ref _carComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
            foreach (var carPart in carCmp.CarPartGroupsList) {
                foreach (var cellPos in carPart.Value) {
                    carCmp.CarPartGroupsGrid[cellPos.x, cellPos.y]  = carPart.Key;
                }
            }
        }
        private void DestroyAllCars() {
            
            foreach (var carEntity in _carComponentsFilter.Value) 
               DestroyNeedCar(carEntity);

            SetPlayerCarPartsGridFromList();
        }
        private void ConstructMeleeWeaponCell(int entity) {
            var carPartComponent = _carPartComponentsPool.Value.Get(entity);
            var meleeConfig = _sceneData.Value.GetMeleeWeaponFromCarPart(carPartComponent.CarPartId);
              var meleeStuffCarPart =  _meleeWeaponComponentsPool.Value.Get(entity).MeleeStuff;
              
            meleeStuffCarPart.ConstructMelee(meleeConfig.UpSprite ,ProjectileHit, carPartComponent.MainEntity == _sceneData.Value.PlayerEntity?"Enemy":"Player",carPartComponent.CarPartId, entity);
        }

        private void CreateExplosionEffect(float radius, Vector2 position, EffectType effectType) {
            var explosionPrefab = _sceneData.Value.GetExplosionEffect();

            explosionPrefab.transform.position = position;
            var firstColor = effectType != EffectType.shock ? new Color(0.88f, 0.81f, 0.37f, 0.8f) : new Color(0.35f, 0.82f, 0.9f, 0.8f);
            var secondColor = effectType != EffectType.shock ? new Color(0.82f, 0.27f, 0.17f, 0.6f) : new Color(0.2f, 0.3f, 0.8f, 0.6f);
            var explosionSequence = DOTween.Sequence();
        
            explosionSequence.Append(
                explosionPrefab.transform.DOScale(radius*2, 0.35f)
                    .SetEase(Ease.OutBack)
            );
            
            explosionSequence.Join(
                explosionPrefab.DOColor(firstColor, 0.35f)
                    .SetEase(Ease.OutQuad)
            );
        
            explosionSequence.Append(
                explosionPrefab.transform.DOScale(Vector3.zero, 0.5f)
                    .SetEase(Ease.InOutSine)
            );
            
            explosionSequence.Join(
                explosionPrefab.DOColor(secondColor, 0.5f)
                    .SetEase(Ease.OutQuad)
            );
         
            explosionSequence.OnComplete(() => {
                  explosionSequence?.Kill();
                explosionPrefab.transform.localScale = Vector3.zero;
                _sceneData.Value.ReleaseExplosionEffect(explosionPrefab);
            });
        }
        
        private void SpawnProjectile(int gunEntity) {
            var carPartCmp = _carPartComponentsPool.Value.Get(gunEntity);
            ref var gunCmp = ref _gunComponentsPool.Value.Get(gunEntity);
            var gunConfig = _sceneData.Value.GetGunFromCarPart(carPartCmp.CarPartId);
            for (int i = 0; i < gunConfig.BulletsCount; i++) {
                if(carPartCmp.CellCarPartsStuffViews == null || gunCmp.FirePointTransform == null)return;
                
                Vector2 worldFirePoint = /*carPartCmp.CarPartCellRenderer.transform.TransformPoint(gunConfig.FirePointPosition)*/
                    gunCmp.FirePointTransform.position;

                Vector2 direction = gunCmp.FirePointTransform.up;

                // Добавляем случайный разброс
                float randomSpread = Random.Range(-gunConfig.SpreadAngle, gunConfig.SpreadAngle);
                direction = Quaternion.Euler(0, 0, randomSpread) * direction;

                ProjectileView currentProjectile = _sceneData.Value.GetGunProjectile(worldFirePoint);

                if (currentProjectile.entity != 0) {
                    ref var projectileCmp = ref _projectileComponentsPool.Value.Get(currentProjectile.entity);
                    projectileCmp.IsActive = true;
                    projectileCmp.LifeTime = 50f / gunConfig.BulletSpeed;
                } else {
                    int newEntity = _world.Value.NewEntity();
                    currentProjectile.entity = newEntity;
                    ref var projectileCmp = ref _projectileComponentsPool.Value.Add(currentProjectile.entity);
                    projectileCmp.IsActive = true;
                    projectileCmp.ProjectileView = currentProjectile;
                    projectileCmp.LifeTime = 50f / gunConfig.BulletSpeed;
                }

                if (carPartCmp.MainEntity == _sceneData.Value.PlayerEntity)
                    currentProjectile.Construct(ProjectileHit, gunConfig.Id, gunConfig.MissleSprite, "Enemy");
                else
                    currentProjectile.Construct(ProjectileHit, gunConfig.Id, gunConfig.MissleSprite, "Player");

                currentProjectile.rigidbody2D.linearVelocity = direction * gunConfig.BulletSpeed;

                // Поворачиваем снаряд в направлении движения
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                currentProjectile.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            }

            gunCmp.CurrentMagazineCount--;
            if (gunCmp.CurrentMagazineCount > 0)
                gunCmp.CurrentCountdown += gunConfig.AttackCouldown;
            else {
                gunCmp.CurrentMagazineCount = gunConfig.BulletsCountInMagazine;
                gunCmp.CurrentCountdown += gunConfig.ReloadDuration;
            }
        }

        private void SetEffectComponent(int carPartEntity, EffectType effectType, float duration) {
            switch (effectType) {
                case EffectType.shock:
                    if (_shockEffectComponentsPool.Value.Has(carPartEntity)) {
                        ref var shockCmp = ref _shockEffectComponentsPool.Value.Get(carPartEntity);
                        if (shockCmp.effectDuration < duration)
                            shockCmp.effectDuration = duration;
                    } else {
                        _shockEffectComponentsPool.Value.Add(carPartEntity).effectDuration = duration;
                        var damagedCarPart = _carPartComponentsPool.Value.Get(carPartEntity).CarPartId;
                        if (_sceneData.Value.GetCarPartConfig(damagedCarPart).PartType == CarPartType.Main)
                            _carComponentsPool.Value.Get(_carPartComponentsPool.Value.Get(carPartEntity).MainEntity)
                                .ShockedMainPartsCount++;
                    }

                    break;
                case EffectType.fire:
                    if (_fireComponentsPool.Value.Has(carPartEntity)) {
                        ref var shockCmp = ref _fireComponentsPool.Value.Get(carPartEntity);
                        shockCmp.Duration += duration;
                    } else 
                        _fireComponentsPool.Value.Add(carPartEntity).Duration = duration;

                    break;
            }
        }

        private void ProjectileHit(AttackType attackType, int weaponId, Vector2Int damagedEntityGridPosition, int damagedMainEntity,
            Vector2 collisionPoint, ProjectileView projectile = null, int weaponCellEntity = -1, string layerName = null) {
            
            float damagePoints;
                var carCmpDamaged = _carComponentsPool.Value.Get(damagedMainEntity);
                int damagedEntity = carCmpDamaged.CarPartGroupsGrid[damagedEntityGridPosition.x, damagedEntityGridPosition.y] == 0
                    ? carCmpDamaged.CarPartContainerGroupsGrid[damagedEntityGridPosition.x, damagedEntityGridPosition.y]
                    : carCmpDamaged.CarPartGroupsGrid[damagedEntityGridPosition.x, damagedEntityGridPosition.y];
                
            if /*gunProjectile*/ (attackType == AttackType.Projectile) {
                if (!_projectileComponentsPool.Value.Get(projectile.entity).IsActive) return;
                _projectileComponentsPool.Value.Get(projectile.entity).IsActive = false;

                _sceneData.Value.ReleaseGunProjectile(projectile, _projectileComponentsPool.Value.Get(projectile.entity).ExplosionRadius,
                    _sceneData.Value._bulletSprites);
                var gunConfig = _sceneData.Value.GetGunFromCarPart(weaponId);
                damagePoints = gunConfig.Damage;
                if (gunConfig.ImpulseForce != 0)
                    ProjectileImpulse(gunConfig.ImpulseForce, damagedEntity, projectile.rigidbody2D.linearVelocity.normalized);
                projectile.rigidbody2D.linearVelocity = Vector2.zero;

                LayerMask layerMask = layerName != "Player"
                    ? LayerMask.GetMask(new string[] { "CarBase", "UpCarPart" })
                    : LayerMask.GetMask(new string[] { "CarBaseEnemy", "UpCarPartEnemy" });
                var carPartDamagedCmp = _carPartComponentsPool.Value.Get(damagedEntity);

                if (gunConfig.ExplosionRadius != 0 &&
                    (_sceneData.Value.GetCarPartConfig(carPartDamagedCmp.CarPartId).PartType != CarPartType.Armor ||
                     !_sceneData.Value.GetArmorFromCarPart(carPartDamagedCmp.CarPartId).ExplosionDefence)) {
                    CreateExplosionEffect(gunConfig.ExplosionRadius, collisionPoint, gunConfig.EffectType);
                    var castedObjects = Physics2D.OverlapCircleAll(collisionPoint, gunConfig.ExplosionRadius, layerMask);

                    foreach (var castedObject in castedObjects) {
                        float explosionDamagePoints = gunConfig.ExplosionDamage *
                            Vector2.Distance(castedObject.transform.position, collisionPoint) / gunConfig.ExplosionRadius;
                        //    Debug.Log(Vector2.Distance(castedObject.transform.position, collisionPoint) / gunConfig.ExplosionRadius + "exp mult");
                        if (carPartDamagedCmp.MainEntity == _sceneData.Value.PlayerEntity)
                            explosionDamagePoints *= _currentGameComponentsPool.Value.Get(_sceneData.Value.PlayerEntity).RoundMoneyMultiplayer;

                        Vector2Int currentDamagedEntityGridPos = castedObject.GetComponent<CarPartStuffView>()._gridPosition;
                        var currentCarCmpDamaged = _carComponentsPool.Value.Get(damagedMainEntity);
                        //   Debug.Log(currentDamagedEntityGridPos + " damage grid pos");
                        int currentDamagedEntity =
                            currentCarCmpDamaged.CarPartGroupsGrid[currentDamagedEntityGridPos.x, currentDamagedEntityGridPos.y] == 0
                                ? currentCarCmpDamaged.CarPartContainerGroupsGrid[currentDamagedEntityGridPos.x, currentDamagedEntityGridPos.y]
                                : currentCarCmpDamaged.CarPartGroupsGrid[currentDamagedEntityGridPos.x, currentDamagedEntityGridPos.y];
                        var damagedCarPartCmp = _carPartComponentsPool.Value.Get(currentDamagedEntity);

                        if (_sceneData.Value.GetCarPartConfig(damagedCarPartCmp.CarPartId).PartType == CarPartType.Armor) {
                            var armorConfig = _sceneData.Value.GetArmorFromCarPart(damagedCarPartCmp.CarPartId);
                            if (GetBulletDirection(castedObject.transform, collisionPoint, armorConfig))
                                explosionDamagePoints -= explosionDamagePoints * armorConfig.DefencePercent;
                        }

                        _sceneData.Value.GetFloatingTextPrefab(castedObject.transform.position,
                            explosionDamagePoints.ToString("F2"), Color.white);

                        ChangeHealth(explosionDamagePoints, currentDamagedEntity);

                        SetEffectComponent(currentDamagedEntity, gunConfig.EffectType, gunConfig.EffectDuration);
                        //lenght from explode higher = damage worse
                    }
                }

                SetEffectComponent(damagedEntity, gunConfig.EffectType, gunConfig.EffectDuration);
            } else if (attackType == AttackType.Melee) {
                ref var meleeCmp = ref _meleeWeaponComponentsPool.Value.Get(weaponCellEntity);
                if (meleeCmp.Countdown > 0) return;
                var meleeWeaponConfig = _sceneData.Value.GetMeleeWeaponFromCarPart(weaponId);
                meleeCmp.Countdown += meleeWeaponConfig.AttackCountdown;

                damagePoints = meleeWeaponConfig.Damage +
                               (_movementComponentsPool.Value.Get(_carPartComponentsPool.Value.Get(weaponCellEntity).MainEntity)
                                   .CurrentMoveSpeed + _movementComponentsPool.Value.Get(damagedMainEntity).CurrentMoveSpeed) *
                               meleeWeaponConfig.DamageAddedFromOneSpeed;
                Debug.Log(damagePoints);
            } else /*laser*/ {
                float distanceBetween =
                    Vector2.Distance(_carPartComponentsPool.Value.Get(weaponCellEntity).CarPartCellRenderer.transform.position, collisionPoint);
                var gunConfig = _sceneData.Value.GetGunFromCarPart(weaponId);
                float radiusDamageMultiplayer = (1 - distanceBetween / gunConfig.BulletSpeed) * gunConfig.ExplosionRadius +
                                                (1 - gunConfig.ExplosionRadius);
                damagePoints = gunConfig.Damage * radiusDamageMultiplayer * Time.deltaTime;

                // gunConfig.ExplosionRadius is damage that can be minus from all damage from distance - smaller = higher damage
            }

            if (damagedMainEntity == _sceneData.Value.PlayerEntity)
                damagePoints *= _currentGameComponentsPool.Value.Get(_sceneData.Value.PlayerEntity).RoundMoneyMultiplayer;

            if (damagePoints > 0) {
                var damagedCarPartCmp = _carPartComponentsPool.Value.Get(damagedEntity);

                if (_sceneData.Value.GetCarPartConfig(damagedCarPartCmp.CarPartId).PartType == CarPartType.Armor) {
                    var damagedCell = carCmpDamaged.CarPartViewsGrid[damagedEntityGridPosition.x, damagedEntityGridPosition.y];
                    var armorConfig = _sceneData.Value.GetArmorFromCarPart(damagedCarPartCmp.CarPartId);
                    if (GetBulletDirection(damagedCell.transform, collisionPoint, armorConfig))
                        
                    damagePoints *= 1f - armorConfig.DefencePercent / (armorConfig.DefencePercent + 100f);
                }
                
                _sceneData.Value.GetFloatingTextPrefab(collisionPoint, damagePoints.ToString("F2"), Color.white);
             /*   var carCmpDamaged = _carComponentsPool.Value.Get(damagedMainEntity);
                int damagedEntity = carCmpDamaged.CarPartGroupsGrid[damagedEntityGridPosition.x, damagedEntityGridPosition.y] == 0
                    ? carCmpDamaged.CarPartContainerGroupsGrid[damagedEntityGridPosition.x, damagedEntityGridPosition.y]
                    : carCmpDamaged.CarPartGroupsGrid[damagedEntityGridPosition.x, damagedEntityGridPosition.y];*/
                ChangeHealth(damagePoints, damagedEntity);
            }
        }

        private void ProjectileImpulse(float impulseForce, int damagedEntity, Vector2 collisionPoint) {
            int mainEntity = _carPartComponentsPool.Value.Get(damagedEntity).MainEntity;
            ref var moveCmp = ref _movementComponentsPool.Value.Get(mainEntity);
            float weight = _carComponentsPool.Value.Get(mainEntity).CurrentWeight;
            moveCmp.MovableObjectRigidbody.AddForce(collisionPoint * impulseForce / weight * 2, ForceMode2D.Impulse);
            moveCmp.CantMoveTime = 0.3f;

            moveCmp.CurrentMoveSpeed -= impulseForce / weight / 2;
            if (moveCmp.CurrentMoveSpeed < 0)
                moveCmp.CurrentMoveSpeed = 0;
        }

        private void ChangeHealth(float health, int cellEntity) {
            ref var carPartCellCmp = ref _carPartComponentsPool.Value.Get(cellEntity);

            carPartCellCmp.CurrentHealth -= health;
            var healthPercent = carPartCellCmp.CurrentHealth / carPartCellCmp.MaxHealth;
            var carPartConfig = _sceneData.Value.GetCarPartConfig(carPartCellCmp.CarPartId);
            if (carPartCellCmp.HealthMaterialIndex == 0 && healthPercent < 0.6f) {
                var needMaterial = _sceneData.Value._carStuffListsConfig.LowDamagedMaterial;
                carPartCellCmp.HealthMaterialIndex = 1;
                ChangeCarPartMaterial(cellEntity, carPartCellCmp, needMaterial, carPartConfig);
            } 
            if (carPartCellCmp.HealthMaterialIndex == 1 && healthPercent < 0.15f) {
                carPartCellCmp.HealthMaterialIndex = 2;
                var needMaterial = _sceneData.Value._carStuffListsConfig.HighDamagedMaterial;
                ChangeCarPartMaterial(cellEntity, carPartCellCmp, needMaterial, carPartConfig);
            }

            if (_carPartInGameUIComponentsPool.Value.Has(cellEntity)) {
                _carPartInGameUIComponentsPool.Value.Get(cellEntity).view.SetHealthCount(
                    _sceneData.Value._healthGradient.Evaluate(healthPercent), (int)carPartCellCmp.CurrentHealth);
            }

            if (carPartCellCmp.CurrentHealth <= 0 && carPartCellCmp.CellCarPartsStuffViews != null) {
                carPartCellCmp.CurrentHealth = 0;
                DestroyCarPartCell(cellEntity);

                if (carPartConfig.PartType == CarPartType.Main) {
                    DestroyNeedCar(carPartCellCmp.MainEntity);
                    if (carPartCellCmp.MainEntity == _sceneData.Value.PlayerEntity)
                        LoseGame();
                }

                carPartCellCmp.CellCarPartsStuffViews = null;
            }
        }

        private void ChangeCarPartMaterial(int cellEntity, CarPartComponent carPartCellCmp, Material needMaterial, CarPartConfig carPartConfig) {
            carPartCellCmp.CarPartCellRenderer.material = needMaterial;

            if (carPartConfig.PartType == CarPartType.MeleeWeapon) 
                _meleeWeaponComponentsPool.Value.Get(cellEntity).MeleeWeaponRenderer.material = needMaterial;
                
            else if (carPartConfig.PartType == CarPartType.Gun && _gunComponentsPool.Value.Get(cellEntity).GunSpriteRenderer != null) 
                _gunComponentsPool.Value.Get(cellEntity).GunSpriteRenderer.material = needMaterial;
        }

        private void DestroyCarPartCell(int cellEntity) {
            ref CarPartComponent carPartCmp = ref _carPartComponentsPool.Value.Get(cellEntity);
            carPartCmp.CurrentHealth = 0;
            if (!carPartCmp.CarPartCellRenderer.gameObject.activeInHierarchy) return;
            var carPartConfig = _sceneData.Value.GetCarPartConfig(carPartCmp.CarPartId);
            bool isPlayer = carPartCmp.MainEntity == _sceneData.Value.PlayerEntity;
            //  if (carPartCellCmp.CarPartId.Count > 1) {
            if (carPartConfig.PartType == CarPartType.Gun) {
             //   _sceneData.Value.ReleaseCarPartCell(carPartCmp.CellCarPartsStuffViews[0].transform.GetChild(0)
              //      .GetComponent<CarPartStuffView>());
                // if (carPartCellCmp.MainEntity != _sceneData.Value.PlayerEntity)
                Debug.Log(carPartConfig.PartName + "  "+carPartCmp.MainEntity);
                ref var gunCmp  = ref  _gunComponentsPool.Value.Get(cellEntity);
                if (gunCmp.UpPartContainer != null) {
                    gunCmp.GunSpriteRenderer.sortingOrder -= 5;
                    gunCmp.GunSpriteRenderer.color = _sceneData.Value._carStuffListsConfig.DestroyedCarPartColor;
                }
                   
               // _gunComponentsPool.Value.Del(cellEntity);

                if (_laserComponentsPool.Value.Has(cellEntity)) {
                    _sceneData.Value.ReleaseLaserLineRenderer(_laserComponentsPool.Value.Get(cellEntity).LaserLineRenderer);
                  //  _laserComponentsPool.Value.Del(cellEntity);
                }
            }

            if (carPartConfig.PartType == CarPartType.MeleeWeapon) {
                ref var meleeCmp = ref _meleeWeaponComponentsPool.Value.Get(cellEntity);
                _sceneData.Value.ReleaseMeleeCarPartCell(meleeCmp.MeleeStuff);
               
                meleeCmp.MeleeWeaponRenderer.sortingOrder -= 5;
                meleeCmp.MeleeWeaponRenderer.color = _sceneData.Value._carStuffListsConfig.DestroyedCarPartColor;
                //if (carPartCellCmp.MainEntity != _sceneData.Value.PlayerEntity)
            //    _meleeWeaponComponentsPool.Value.Del(cellEntity);
            }
  ref var inventoryCmp = ref _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
            //  }
            ref var carCmp = ref _carComponentsPool.Value.Get(carPartCmp.MainEntity);
            if (_shockEffectComponentsPool.Value.Has(cellEntity))
                _shockEffectComponentsPool.Value.Del(cellEntity);
            if (carPartConfig.PartType == CarPartType.CarStuffContainer || carPartConfig.PartType == CarPartType.WheelsContainer) {
              
                for (int i = 0; i < carPartCmp.CellCarPartsStuffViews.Count; i++) {
                    var carPart = carPartCmp.CellCarPartsStuffViews[i];
                    var gridPos = carCmp.CarPartContainerGroupsList[cellEntity][i];
                    
                    if (carCmp.CarPartGroupsGrid[gridPos.x, gridPos.y] != 0)
                        DestroyCarPartCell(carCmp.CarPartGroupsGrid[gridPos.x, gridPos.y]);
                    _sceneData.Value.ReleaseCarPartCell(carPart);
                    if(isPlayer)
                      _sceneData.Value.ReleaseCarPartInGameUIView(inventoryCmp.CarPartInfoUIViewsGrid[gridPos.x, gridPos.y]);
                }

                if (carPartConfig.PartType == CarPartType.WheelsContainer) {
                    var wheelContainer = _wheelContainerComponentsPool.Value.Get(cellEntity);
                    foreach (var wheel in wheelContainer.Wheels) {
                        wheel.transform.GetChild(0).gameObject.SetActive(false);
                        wheel.enabled = false;
                      
                        var wheelSpriteRenderer = wheel.gameObject.GetComponent<SpriteRenderer>();
                        wheelSpriteRenderer.sprite = _sceneData.Value._carStuffListsConfig
                            .Wheels[_sceneData.Value.GetWheelContainerFromCarPart(carPartCmp.CarPartId).WheelId].UpWheelSprite;
                        wheelSpriteRenderer.color = _sceneData.Value._carStuffListsConfig.DestroyedCarPartColor;
                        wheel.transform.parent =_sceneData.Value._brokenCarPartsContainer;
                        _wheelContainerComponentsPool.Value.Del(cellEntity);
                    }
                }
                if(!isPlayer)
                carCmp.CarPartContainerGroupsList.Remove(cellEntity);
            } else {
                for (int i = 0; i < carPartCmp.CellCarPartsStuffViews.Count; i++) {
                    var carPart = carPartCmp.CellCarPartsStuffViews[i];
                    var gridPos = carCmp.CarPartGroupsList[cellEntity][i];
                    carCmp.CarPartGroupsGrid[gridPos.x, gridPos.y] = 0;
                    string needLayer = isPlayer? "CarBase":"CarBaseEnemy";
                    carPart.SetupCarPart(gridPos, needLayer,carPartCmp.MainEntity);
                    if(isPlayer)
                    _sceneData.Value.ReleaseCarPartInGameUIView(inventoryCmp.CarPartInfoUIViewsUpLevelGrid[gridPos.x, gridPos.y]);
                }
                if(!isPlayer)
                carCmp.CarPartGroupsList.Remove(cellEntity);
            }
            Debug.Log(" des cell"+cellEntity);
            carPartCmp.CarPartCellRenderer.transform.parent = _sceneData.Value._brokenCarPartsContainer;
            carPartCmp.CarPartCellRenderer.sortingOrder -= 5;
            carPartCmp.CarPartCellRenderer.color = _sceneData.Value._carStuffListsConfig.DestroyedCarPartColor;
            carPartCmp.CarPartCellRenderer = null;
            if (carPartCmp.MainEntity != _sceneData.Value.PlayerEntity && !_destroyEntityEventsPool.Value.Has(cellEntity)) {
                _destroyEntityEventsPool.Value.Add(cellEntity);
                //   _carPartCellComponentsPool.Value.Del(cellEntity);
                CheckTasks(TaskType.BrokenParts);
            }

            if (carPartCmp.MainEntity == _sceneData.Value.PlayerEntity) {
                Debug.Log("des player part");
            }
        }
        public bool GetBulletDirection(Transform targetObject, Vector2 bulletPosition, ArmorConfig armorConfig)
        {
            Vector2 localBulletPos = targetObject.InverseTransformPoint(bulletPosition);
        
            // Нормализуем направление в локальном пространстве
            Vector2 localDirection = localBulletPos.normalized;

            // Определяем сторону на основе локальных координат
            float angle = Mathf.Atan2(localDirection.y, localDirection.x) * Mathf.Rad2Deg;

            // Определяем сторону на основе угла
            if (Mathf.Abs(angle) <= 45f)
                return armorConfig.DefenceDirections.Contains(DefenceDirection.Right); // В 2D "вперед" обычно направлено вправо
            else if (Mathf.Abs(angle) >= 135f)
                return armorConfig.DefenceDirections.Contains(DefenceDirection.Left);
            else if (angle > 0f)
                return armorConfig.DefenceDirections.Contains(DefenceDirection.Up);
            else
                return armorConfig.DefenceDirections.Contains(DefenceDirection.Down);
        }
        
        private void DestroyBrokenParts() {
            foreach (Transform child in _sceneData.Value._brokenCarPartsContainer)
            {
                _sceneData.Value.DestroyGameobject(child.transform);
            }
        }

        private void DestroyNeedCar(int carEntity) {
            ref var carCmp = ref _carComponentsPool.Value.Get(carEntity);
            carCmp.ForwardWheels = new List<Animator>();
            var containersList = new Dictionary<int, List<Vector2Int>>(carCmp.CarPartContainerGroupsList);

            foreach (var carPartEntity in containersList) {
                Debug.Log(" delete cell" + carPartEntity.Key);
                if (_carPartComponentsPool.Value.Get(carPartEntity.Key).CurrentHealth > 0)
                    DestroyCarPartCell(carPartEntity.Key);
            }

            var partsList = new Dictionary<int, List<Vector2Int>>(carCmp.CarPartGroupsList);
            foreach (var carPartEntity in partsList) {
                Debug.Log(" delete cell" + carPartEntity.Key);
                if (_carPartComponentsPool.Value.Get(carPartEntity.Key).CurrentHealth > 0)
                    DestroyCarPartCell(carPartEntity.Key);
                //fix double delete
            }

            /*   foreach (var carPartEntity in _carPartCellComponentsFilter.Value) {
                    ref var carPartCmp = ref _carPartComponentsPool.Value.Get(carPartEntity);
                    if (carPartCmp.MainEntity == carEntity && carPartCmp.CurrentHealth > 0) {

                    }
                }*/

            _sceneData.Value.DestroyGameobject(carCmp.CarView.MainCarContainer);
            // Debug.Log("startDestroy");
            if (_currentGameComponentsPool.Value.Get(_sceneData.Value.PlayerEntity).CurrentRoundType == RoundType.None) return;
            if (carEntity != _sceneData.Value.PlayerEntity) {
                // _movementComponentsPool.Value.Del(carEntity);
                // _carComponentsPool.Value.Del(carEntity);
                _destroyEntityEventsPool.Value.Add(carEntity);
                CheckTasks(TaskType.Kills);
            }
        }
        
        public void CheckTasks(TaskType taskType) {
            foreach (var taskEntity in _taskComponentsFilter.Value) {
                ref var taskCmp = ref _taskComponentsPool.Value.Get(taskEntity);
                if (taskCmp.TaskType != taskType) continue;
                taskCmp.CurrentCount++;
                TaskUtilits.UpdateTaskText(taskCmp);
                if (taskCmp.CurrentCount >= taskCmp.NeedCount) {
                    taskCmp.TaskText.gameObject.SetActive(false);
                    _playerCurrencyComponentsPool.Value.Get(_sceneData.Value.PlayerEntity).MoneyCount += taskCmp.MoneyReward;
                }
            }
        }
    }
