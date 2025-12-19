using System;
using System.Collections.Generic;
using Client;
using DG.Tweening;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using Leopotam.EcsLite.ExtendedSystems;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public enum AttackType {
            Melee,
            Projectile,
            Laser
        }
    sealed class AttackSystem : IEcsRunSystem {  
        private EcsWorldInject _world;
        private EcsCustomInject<SceneService> _sceneService;
        private EcsCustomInject<BetweenScenesService> _betweenScenesService;

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
        private EcsPoolInject<EnemyAIComponent> _enemyAIComponentsPool;
        private EcsPoolInject<FuelComponent> _fuelComponentsPool;
        private EcsPoolInject<EndRoundEvent> _endRoundEventsPool;
        private EcsPoolInject<CompleteTaskEvent> _completeTaskEventsPool;
        private EcsPoolInject<ShopComponent> _shopComponentsPool;
        private EcsPoolInject<UpdateCarsStatsInfoEvent> _updateCarsStatsInfoEventsPool;
        private EcsPoolInject<WheelContainerComponent> _wheelContainerComponentsPool;
        
        private EcsFilterInject<Inc<ShockEffectComponent>> _shockEffectComponentsFilter;
        private EcsFilterInject<Inc<CarComponent>> _carComponentsFilter;
        private EcsFilterInject<Inc<FireComponent>> _fireComponentsFilter;
        private EcsFilterInject<Inc<CompleteTaskEvent>> _completeTaskEventsFilter;
        private EcsFilterInject<Inc<TaskComponent>> _taskComponentsFilter;
        private EcsFilterInject<Inc<MeleeWeaponComponent>> _meleeWeaponComponentsFilter;
        private EcsFilterInject<Inc<GunComponent>> _gunComponentsFilter;
        private EcsFilterInject<Inc<EndRoundEvent>> _endRoundEventsFilter;
        private EcsFilterInject<Inc<CarPartCellComponent>> _carPartCellComponentsFilter;
        private EcsFilterInject<Inc<ProjectileComponent>> _projectileComponentsFilter;
        private EcsFilterInject<Inc<ConstructMeleeCellEvent>> _constructMeleeCellEventsFilter;

        public void Run(IEcsSystems systems) {
            foreach (var endRoundEntity in _endRoundEventsFilter.Value) {
                EndRound();
                ClearHealthUI();
                foreach (var completeTask in _completeTaskEventsFilter.Value) {
                    _completeTaskEventsPool.Value.Del(completeTask);
                    
                   CheckTasks(_taskComponentsPool.Value.Get(completeTask).TaskType); 
                }
               
            }

            float deltaTime = Time.deltaTime;
            ref var curGameCmp = ref _currentGameComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);
            if (curGameCmp.CurrentRoundType != RoundType.BattleDefault && curGameCmp.CurrentRoundType != RoundType.FromAToB &&
                curGameCmp.CurrentRoundType != RoundType.ZoneDefence&&curGameCmp.CurrentRoundType != RoundType.BossBattle) return;
            foreach (var gunEntity in _gunComponentsFilter.Value) {
                if (_shockEffectComponentsPool.Value.Has(gunEntity)) {
                    if (_laserComponentsPool.Value.Has(gunEntity))
                        _laserComponentsPool.Value.Get(gunEntity).LaserLineRenderer.gameObject.SetActive(false);
                    continue;
                }

                ref var gunCmp = ref _gunComponentsPool.Value.Get(gunEntity);
                var carPartCmp = _carPartComponentsPool.Value.Get(gunEntity);
                var gunConfig = _sceneService.Value.GetGunFromCarPart(carPartCmp.CarPartId);

                gunCmp.CurrentCountdown -= deltaTime;

                if (gunCmp.UpPartContainer != null && gunConfig.RotateSpeed != 0) {
                    gunCmp.GunOnTargetTime -= deltaTime;
                    if (gunCmp.GunOnTargetTime <= 0) {
                        var needLayer =
                            carPartCmp.MainEntity == _sceneService.Value.PlayerEntity ||
                            !_enemyAIComponentsPool.Value.Get(carPartCmp.MainEntity).IsEnemy
                                ? LayerMask.GetMask("UpCarPartEnemy")
                                : LayerMask.GetMask("UpCarPart");
                        RaycastHit2D hit = Physics2D.Raycast(gunCmp.FirePointTransform.position, gunCmp.FirePointTransform.up, 7, needLayer);

                        gunCmp.GunOnTarget = hit.collider != null;

                        gunCmp.GunOnTargetTime += 0.2f;
                    }

                    if (!gunCmp.GunOnTarget) {
                        gunCmp.CurrentRotateDegrees += deltaTime * gunConfig.RotateSpeed;

                        float pingPongValue = Mathf.PingPong(gunCmp.CurrentRotateDegrees, 1f);

                        float targetAngle = Mathf.Lerp(-gunConfig.MaxRotateAngle, gunConfig.MaxRotateAngle, pingPongValue);

                        gunCmp.UpPartContainer.localEulerAngles = new Vector3(0, 0, targetAngle);
                    }
                }

                if (gunCmp.CurrentCountdown <= 0) {
                    if (gunConfig.MissleType != MissleType.laser) {
                        SpawnProjectile(gunEntity);
                    } else {
                        TrySpawnLaser(gunEntity);
                        if (gunCmp.CurrentCountdown < -gunCmp.CurrentMaxAttackCountdown) {
                            gunCmp.CurrentCountdown = gunCmp.CurrentReloadSpeed;
                            ref var laserCmp = ref _laserComponentsPool.Value.Get(gunEntity);
                            laserCmp.LaserLineRenderer.gameObject.SetActive(false);
                            //   _sceneData.Value.ReleaseFloatingText(laserCmp.LaserDamageText);
                            laserCmp.CurrentSequence = null;
                            laserCmp.LaserDamageText = null;
                            laserCmp.LastDamagedEntity = 0;
                            laserCmp.DamageCountForLastPart = 0;
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
                    _sceneService.Value.ReleaseGunProjectile(projectileCmp.ProjectileView, projectileCmp.ExplosionRadius);
                }
            }

            foreach (var cellEntity in _constructMeleeCellEventsFilter.Value) {
                ConstructMeleeWeaponCell(cellEntity);
            }

            foreach (var cellEntity in _meleeWeaponComponentsFilter.Value) {
                ref var meleeCmp = ref _meleeWeaponComponentsPool.Value.Get(cellEntity);
                if (meleeCmp.Countdown > 0)
                    meleeCmp.Countdown -= deltaTime;
                else if (_sceneService.Value.GetMeleeWeaponFromCarPart(_carPartComponentsPool.Value.Get(cellEntity).CarPartId)
                             .IsAttackAfterCountdown && meleeCmp.isTriggered) {
                    CastMeleeWeapon(cellEntity);
                }
            }

            foreach (var shockEntity in _shockEffectComponentsFilter.Value) {
                ref var shockCmp = ref _shockEffectComponentsPool.Value.Get(shockEntity);
                shockCmp.effectDuration -= deltaTime;
                if (shockCmp.effectDuration <= 0) {
                    var damagedCarPart = _carPartComponentsPool.Value.Get(shockEntity).CarPartId;
                    if (_sceneService.Value.GetCarPartConfig(damagedCarPart).PartType == CarPartType.Main)
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
                    int damage = 1+(Mathf.CeilToInt(fireCmp.Duration/5));
                    ChangeHealth(damage, burnedEntity);
                    if (_carPartComponentsPool.Value.Get(burnedEntity).CurrentHealth <= 0 ||
                        _carPartComponentsPool.Value.Get(burnedEntity).CarPartCellRenderer == null) {
                        _fireComponentsPool.Value.Del(burnedEntity);
                        continue;
                    }

                    _sceneService.Value.GetFloatingTextPrefab(
                        _carPartComponentsPool.Value.Get(burnedEntity).CarPartCellRenderer.transform.position, damage.ToString("F2"),
                        new Color(1, 0.5f, 0.1f, 1));
                    if (damage <= 1) {
                        _fireComponentsPool.Value.Del(burnedEntity);
                    }
                }
            }
        }

     
        
        private void UpdateHealAllCarPartButton() {
            float healCost = GetHealAllCarPartCost();
            _sceneService.Value._inventoryUIView._healCarButton.gameObject.SetActive(healCost>0);
            _sceneService.Value._inventoryUIView._healCarText.text = $"Heal all car for {healCost}$";
        }

        private float GetHealAllCarPartCost() {
            var inventoryCmp = _inventoryComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);
            ref var carCmp = ref _carComponentsPool.Value.Get(inventoryCmp.CurrentRedactedCarEntity);
            float healCost = 0;
            foreach (var carPartEntity in carCmp.CarPartContainerGroupsGrid) {
                var carPartCmp = _carPartComponentsPool.Value.Get(carPartEntity);
                healCost += GetHealCostForCarPart(carPartCmp.MaxHealth, carPartCmp.CurrentHealth,
                    _sceneService.Value.GetCarPartConfig(carPartCmp.CarPartId));
            }

            foreach (var carPartEntity in carCmp.CarPartGroupsGrid) {
                var carPartCmp = _carPartComponentsPool.Value.Get(carPartEntity);
                healCost += GetHealCostForCarPart(carPartCmp.MaxHealth, carPartCmp.CurrentHealth,
                    _sceneService.Value.GetCarPartConfig(carPartCmp.CarPartId));
            }

            return healCost;
        }

        private float GetHealCostForCarPart(float maxHp,float currentHp, CarPartConfig carPartConfig) {
            return carPartConfig.MoneyCost * (1f-currentHp / maxHp) / 4;
        }

        private void TrySpawnLaser(int entity) {
            ref var carPartCmp = ref _carPartComponentsPool.Value.Get(entity);
            ref var gunCmp = ref _gunComponentsPool.Value.Get(entity);
            if (gunCmp.FirePointTransform == null || !_laserComponentsPool.Value.Has(entity)) return;
            var laserLineRenderer = _laserComponentsPool.Value.Get(entity).LaserLineRenderer;
            var gunConfig = _sceneService.Value.GetGunFromCarPart(carPartCmp.CarPartId);
            float laserRange = gunConfig.BulletSpeed;
            var firePoint = gunCmp.FirePointTransform;

            Vector2 worldFirePoint = firePoint.position;

            Vector2 direction = firePoint.transform.up;

            LayerMask layerMask =
                carPartCmp.MainEntity != _sceneService.Value.PlayerEntity && _enemyAIComponentsPool.Value.Get(carPartCmp.MainEntity).IsEnemy
                    ? LayerMask.GetMask(new string[] {
                         "Obstacle", "UpCarPart"
                    })
                    : LayerMask.GetMask(new string[] {
                        "Obstacle", "UpCarPartEnemy"
                    });
            RaycastHit2D hit = Physics2D.Raycast(worldFirePoint, direction, laserRange, layerMask);
            Vector2 endPoint;
            if (hit.collider != null) {
                endPoint = hit.point;

                if (hit.collider.gameObject.layer != 13) {
                    var damagedCarPart = hit.collider.GetComponent<CarPartStuffView>();
                    ProjectileHit(AttackType.Laser, gunConfig.Id, damagedCarPart._gridPosition, damagedCarPart._mainEntity, hit.point, null,
                        entity);
                }
            } else {
                endPoint = worldFirePoint + direction * laserRange;
            }

            laserLineRenderer.SetPosition(0, worldFirePoint);
            laserLineRenderer.SetPosition(1, endPoint);
            laserLineRenderer.gameObject.SetActive(true);
        }
        private void EndRound() {
            Debug.Log("End round attack system");
            DestroyAllProjectiles();
            DestroyAllCars();

            DestroyBrokenParts();
            SetPlayerCarGrids();
            if(!_updateCarsStatsInfoEventsPool.Value.Has(_sceneService.Value.PlayerEntity))
            _updateCarsStatsInfoEventsPool.Value.Add(_sceneService.Value.PlayerEntity);
        }

        private void SetPlayerCarGrids() {
            var inventoryCmp = _inventoryComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);

            foreach (var carEntity in inventoryCmp.PlayerTeammateCarsEntity) {
                var carCmp = _carComponentsPool.Value.Get(carEntity);

                foreach (var carPartInfo in carCmp.CarPartContainerGroupsList) {
                    foreach (var pos in carPartInfo.Value)
                        carCmp.CarPartContainerGroupsGrid[pos.x, pos.y] = carPartInfo.Key;
                }

                foreach (var carPartInfo in carCmp.CarPartGroupsList) {
                    foreach (var pos in carPartInfo.Value)
                        carCmp.CarPartGroupsGrid[pos.x, pos.y] = carPartInfo.Key;
                }
            }
        }
        private void LoseGame() {
            if (_currentGameComponentsPool.Value.Get(_sceneService.Value.PlayerEntity).CurrentRoundType != RoundType.None)
                EndRound();
            Time.timeScale = 0;
            int currentRound = _currentGameComponentsPool.Value.Get(_sceneService.Value.PlayerEntity).CurrentRound;
            int scrapCount = _betweenScenesService.Value._carStuffListsConfig.MapConfigs[_betweenScenesService.Value.CurrentSelectedMap]
                .MapDifficltyConfigs[_betweenScenesService.Value.CurrentSelectedMapDifficlty].ScrapForRound*currentRound;
            DataPersistenceManagerSystem._gameData.ScrapCount += scrapCount;
            _sceneService.Value._endGameScreenView.SetLoseScreenInfo(currentRound,scrapCount);
            DataPersistenceManagerSystem.SaveGame();
            //go to meta
        }
        private void DestroyAllProjectiles() {
            foreach (var projectileEntity in _projectileComponentsFilter.Value) {
                ref var projectileCmp = ref _projectileComponentsPool.Value.Get(projectileEntity);
                if(!projectileCmp.IsActive)continue;
                _sceneService.Value.ReleaseGunProjectile(projectileCmp.ProjectileView,0);
                projectileCmp.IsActive = false;
            }
          
        }
        private void SetPlayerCarPartsGridFromList() {
            ref var carCmp= ref _carComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);
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

        private void CastMeleeWeapon(int entity) {
            ref var meleeWeaponComponent = ref _meleeWeaponComponentsPool.Value.Get(entity);
            var carPartCmp = _carPartComponentsPool.Value.Get(entity);
            Vector2 size = meleeWeaponComponent.MeleeStuff._carPartCollider.size;
            Vector2 origin = (Vector2)meleeWeaponComponent.MeleeStuff.transform.position +
                             meleeWeaponComponent.MeleeStuff._carPartCollider.offset; 
            var needTag = carPartCmp.MainEntity == _sceneService.Value.PlayerEntity ||
                          !_enemyAIComponentsPool.Value.Get(carPartCmp.MainEntity).IsEnemy
                ? "Enemy"
                : "Player";
            RaycastHit2D[] hits = Physics2D.BoxCastAll(origin, size, meleeWeaponComponent.MeleeStuff.transform.eulerAngles.z, Vector2.zero, 0f,
                meleeWeaponComponent.MeleeStuff._layerMask);
            if (hits.Length > 0) {
                foreach (RaycastHit2D hit in hits) {
                    var damagedCarPart = hit.collider.gameObject.GetComponent<CarPartStuffView>();

                    Debug.Log(damagedCarPart._mainEntity);

                    ProjectileHit(AttackType.Melee, carPartCmp.CarPartId, damagedCarPart._gridPosition, damagedCarPart._mainEntity, hit.point,
                        null, entity, needTag);
                }
            } else {
                meleeWeaponComponent.isTriggered = false;
            }
        }

        private void ConstructMeleeWeaponCell(int entity) {
            var carPartComponent = _carPartComponentsPool.Value.Get(entity);
            var meleeConfig = _sceneService.Value.GetMeleeWeaponFromCarPart(carPartComponent.CarPartId);
            var meleeStuffCarPart = _meleeWeaponComponentsPool.Value.Get(entity).MeleeStuff;

            meleeStuffCarPart.ConstructMelee(meleeConfig.UpSprite, ProjectileHit,
                carPartComponent.MainEntity == _sceneService.Value.PlayerEntity ||
                !_enemyAIComponentsPool.Value.Get(carPartComponent.MainEntity).IsEnemy
                    ? "Enemy"
                    : "Player", carPartComponent.CarPartId, entity);
        }

        private void CreateExplosionEffect(float radius, Vector2 position, EffectType effectType) {
            var explosionPrefab = _sceneService.Value.GetExplosionEffect();

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
                _sceneService.Value.ReleaseExplosionEffect(explosionPrefab);
            });
        }

        private void SpawnProjectile(int gunEntity) {
            var carPartCmp = _carPartComponentsPool.Value.Get(gunEntity);
            ref var gunCmp = ref _gunComponentsPool.Value.Get(gunEntity);
            var gunConfig = _sceneService.Value.GetGunFromCarPart(carPartCmp.CarPartId);
           
            if (carPartCmp.CellCarPartsStuffViews == null || gunCmp.FirePointTransform == null) return;  
            for (int i = 0; i < gunConfig.BulletsCount; i++) {
            
                Vector2 worldFirePoint = gunCmp.FirePointTransform.position;

                Vector2 direction = gunCmp.FirePointTransform.up;

                float randomSpread = Random.Range(-gunConfig.SpreadAngle, gunConfig.SpreadAngle);
                direction = Quaternion.Euler(0, 0, randomSpread) * direction;

                ProjectileView currentProjectile = _sceneService.Value.GetGunProjectile(worldFirePoint);

                if (currentProjectile.entity != 0) {
                    ref var projectileCmp = ref _projectileComponentsPool.Value.Get(currentProjectile.entity);
                    projectileCmp.MainEntity = carPartCmp.MainEntity;
                    projectileCmp.IsActive = true;
                    projectileCmp.LifeTime = 50f / gunConfig.BulletSpeed;
                    projectileCmp.Damage = gunCmp.CurrentDamage;
                    projectileCmp.CurrentBreakingThroughCount = gunConfig.BreakingThroughCount;
                } else {
                    int newEntity = _world.Value.NewEntity();
                    currentProjectile.entity = newEntity;
                    ref var projectileCmp = ref _projectileComponentsPool.Value.Add(currentProjectile.entity);
                    projectileCmp.MainEntity = carPartCmp.MainEntity;
                    projectileCmp.IsActive = true;
                    projectileCmp.ProjectileView = currentProjectile;
                    projectileCmp.LifeTime = 50f / gunConfig.BulletSpeed;
                    projectileCmp.Damage = gunCmp.CurrentDamage;
                    projectileCmp.CurrentBreakingThroughCount = gunConfig.BreakingThroughCount;
                }

                if (carPartCmp.MainEntity == _sceneService.Value.PlayerEntity || !_enemyAIComponentsPool.Value.Get(carPartCmp.MainEntity).IsEnemy)
                    currentProjectile.Construct(ProjectileHit, gunConfig.Id, gunConfig.MissleSprite, "Enemy", _sceneService.Value._carStuffListsConfig.DefaultMaterial);
                else
                    currentProjectile.Construct(ProjectileHit, gunConfig.Id, gunConfig.MissleSprite, "Player", _sceneService.Value._carStuffListsConfig.EnemyProjectilesMaterial);

                currentProjectile.rigidbody2D.linearVelocity = direction * gunConfig.BulletSpeed;

                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                currentProjectile.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            }

            if (gunConfig.CarRecoil != 0) {
                Vector2 recoilDirection = -gunCmp.GunSpriteRenderer.transform.up;
                  _movementComponentsPool.Value.Get(carPartCmp.MainEntity).MovableObjectRigidbody
                                    .AddForce(gunConfig.CarRecoil * recoilDirection, ForceMode2D.Impulse);
            }
              
            gunCmp.CurrentMagazineCount--;
            if (gunCmp.CurrentMagazineCount > 0) {
                gunCmp.CurrentCountdown += gunCmp.CurrentMaxAttackCountdown;
            } else {
                gunCmp.CurrentMagazineCount = gunConfig.BulletsCountInMagazine;
                gunCmp.CurrentCountdown += gunCmp.CurrentReloadSpeed;
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
                        if (_sceneService.Value.GetCarPartConfig(damagedCarPart).PartType == CarPartType.Main)
                            _carComponentsPool.Value.Get(_carPartComponentsPool.Value.Get(carPartEntity).MainEntity).ShockedMainPartsCount++;
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

        private void ReleaseProjectileForObstacle(ProjectileView projectile) {
            if (!_projectileComponentsPool.Value.Get(projectile.entity).IsActive) return;
            ref var projectileCmp = ref _projectileComponentsPool.Value.Get(projectile.entity);

            projectileCmp.IsActive = false;
            _sceneService.Value.ReleaseGunProjectile(projectile, projectileCmp.ExplosionRadius, _sceneService.Value._bulletSprites);
            projectile.rigidbody2D.linearVelocity = Vector2.zero;
        }
        private void ProjectileHit(AttackType attackType, int weaponId, Vector2Int damagedEntityGridPosition, int damagedMainEntity,
            Vector2 collisionPoint, ProjectileView projectile = null, int weaponCellEntity = -1, string layerName = null) {
            float damagePoints;
            
            if (damagedEntityGridPosition == -Vector2Int.one) {
                ReleaseProjectileForObstacle(projectile);
                return;
            }

            int damagedEntity = 0;
            if (damagedEntityGridPosition == -Vector2Int.one * 10) {
                damagedEntity = damagedMainEntity;
            } else {
                var carCmpDamaged = _carComponentsPool.Value.Get(damagedMainEntity);
                damagedEntity = carCmpDamaged.CarPartGroupsGrid[damagedEntityGridPosition.x, damagedEntityGridPosition.y] == 0
                    ? carCmpDamaged.CarPartContainerGroupsGrid[damagedEntityGridPosition.x, damagedEntityGridPosition.y]
                    : carCmpDamaged.CarPartGroupsGrid[damagedEntityGridPosition.x, damagedEntityGridPosition.y];
            }
            
            if(damagedEntity == 0)return;
            bool isPlayer = false;
            if /*gunProjectile*/ (attackType == AttackType.Projectile) {
                if (!_projectileComponentsPool.Value.Get(projectile.entity).IsActive) return;
                ref var projectileCmp = ref _projectileComponentsPool.Value.Get(projectile.entity);

                projectileCmp.CurrentBreakingThroughCount--;
                if (projectileCmp.CurrentBreakingThroughCount <= 0) {
                    projectileCmp.IsActive = false;
                    _sceneService.Value.ReleaseGunProjectile(projectile, projectileCmp.ExplosionRadius, _sceneService.Value._bulletSprites);
                    projectile.rigidbody2D.linearVelocity = Vector2.zero;
                }

                isPlayer = projectileCmp.MainEntity == _sceneService.Value.PlayerEntity;
                
                var gunConfig = _sceneService.Value.GetGunFromCarPart(weaponId);
                damagePoints = projectileCmp.Damage;

                if (gunConfig.ImpulseForce != 0)
                    ProjectileImpulse(gunConfig.ImpulseForce, damagedEntity, projectile.rigidbody2D.linearVelocity.normalized);

                LayerMask layerMask = layerName == "Player"
                    ? LayerMask.GetMask(new string[] { "CarBase", "UpCarPart" })
                    : LayerMask.GetMask(new string[] { "CarBaseEnemy", "UpCarPartEnemy" });
                var carPartDamagedCmp = _carPartComponentsPool.Value.Get(damagedEntity);

                if (projectileCmp.CurrentBreakingThroughCount <= 0 && gunConfig.ExplosionRadius != 0 &&
                    (_sceneService.Value.GetCarPartConfig(carPartDamagedCmp.CarPartId).PartType != CarPartType.Armor ||
                     !_sceneService.Value.GetArmorFromCarPart(carPartDamagedCmp.CarPartId).ExplosionDefence)) {
                    CreateExplosionEffect(gunConfig.ExplosionRadius, collisionPoint, gunConfig.EffectType);
                    var castedObjects = Physics2D.OverlapCircleAll(collisionPoint, gunConfig.ExplosionRadius, layerMask);

                    foreach (var castedObject in castedObjects) {
                        float explosionDamagePoints = gunConfig.ExplosionDamage *
                            Vector2.Distance(castedObject.transform.position, collisionPoint) / gunConfig.ExplosionRadius;
                    
                        if (carPartDamagedCmp.MainEntity == _sceneService.Value.PlayerEntity)
                            explosionDamagePoints *= _currentGameComponentsPool.Value.Get(_sceneService.Value.PlayerEntity).RoundMoneyMultiplayer;

                        Vector2Int currentDamagedEntityGridPos = castedObject.GetComponent<CarPartStuffView>()._gridPosition;
                        var currentCarCmpDamaged = _carComponentsPool.Value.Get(damagedMainEntity);
                       
                        if (currentCarCmpDamaged.CarPartGroupsGrid == null) break;
                        int currentDamagedEntity =
                            currentCarCmpDamaged.CarPartGroupsGrid[currentDamagedEntityGridPos.x, currentDamagedEntityGridPos.y] == 0
                                ? currentCarCmpDamaged.CarPartContainerGroupsGrid[currentDamagedEntityGridPos.x, currentDamagedEntityGridPos.y]
                                : currentCarCmpDamaged.CarPartGroupsGrid[currentDamagedEntityGridPos.x, currentDamagedEntityGridPos.y];
                        if(currentDamagedEntity == 0)break;
                        var damagedCarPartCmp = _carPartComponentsPool.Value.Get(currentDamagedEntity);
                        
                        if (_sceneService.Value.GetCarPartConfig(damagedCarPartCmp.CarPartId).PartType == CarPartType.Armor) {
                            var armorConfig = _sceneService.Value.GetArmorFromCarPart(damagedCarPartCmp.CarPartId);
                            if (GetBulletDirection(castedObject.transform, collisionPoint, armorConfig)) {
                                float defencePercent = armorConfig.DefencePercent *
                                                       GridUtils.GetArmorLevelMultipliers(damagedCarPartCmp.PartLevel);

                                explosionDamagePoints *= 1f - defencePercent / (defencePercent + 100f);
                            }
                        }

                        _sceneService.Value.GetFloatingTextPrefab(castedObject.transform.position, explosionDamagePoints.ToString("F2"),
                            Color.white);
                        if(isPlayer)
                        DataPersistenceManagerSystem._gameData.PlayerDamage += explosionDamagePoints;
                        ChangeHealth(explosionDamagePoints, currentDamagedEntity);

                        SetEffectComponent(currentDamagedEntity, gunConfig.EffectType, gunConfig.EffectDuration);
                        //lenght from explode higher = damage worse
                    }
                }

                SetEffectComponent(damagedEntity, gunConfig.EffectType, gunConfig.EffectDuration);
            } else if (attackType == AttackType.Melee) {
                ref var meleeCmp = ref _meleeWeaponComponentsPool.Value.Get(weaponCellEntity);
                if (meleeCmp.Countdown > 0) return;
                meleeCmp.isTriggered = true;
                var meleeWeaponConfig = _sceneService.Value.GetMeleeWeaponFromCarPart(weaponId);
                meleeCmp.Countdown += meleeWeaponConfig.AttackCountdown;
                int mainEntity = _carPartComponentsPool.Value.Get(weaponCellEntity).MainEntity;
                var moveCmp = _movementComponentsPool.Value.Get(mainEntity);
                isPlayer = mainEntity == _sceneService.Value.PlayerEntity;
                float damagedObjectMass = 10;
                float damagedObjectSpeed = 0;
                if (_movementComponentsPool.Value.Has(damagedMainEntity)) {
                     var moveCmpDamaged = _movementComponentsPool.Value.Get(damagedMainEntity);
                     damagedObjectMass = moveCmpDamaged.MovableObjectRigidbody.mass;
                     damagedObjectSpeed = moveCmpDamaged.CurrentMoveSpeed;
                }
                float damageByCollision = meleeCmp.DamageBySpeed *
                                          ((damagedObjectMass * moveCmp.MovableObjectRigidbody.mass) /
                                           (moveCmp.MovableObjectRigidbody.mass + damagedObjectMass)) *
                                          Mathf.Pow(Mathf.Abs(moveCmp.CurrentMoveSpeed - damagedObjectSpeed), 2)/5;
                damagePoints = meleeCmp.CurrentDamage + damageByCollision;
                
            } else /*laser*/ {
                float distanceBetween =
                    Vector2.Distance(_carPartComponentsPool.Value.Get(weaponCellEntity).CarPartCellRenderer.transform.position, collisionPoint);
                var gunConfig = _sceneService.Value.GetGunFromCarPart(weaponId);
                var gunCmp = _gunComponentsPool.Value.Get(weaponCellEntity);
                float radiusDamageMultiplayer = (1 - distanceBetween / gunConfig.BulletSpeed) * gunConfig.ExplosionRadius +
                                                (1 - gunConfig.ExplosionRadius);
                damagePoints = gunCmp.CurrentDamage * radiusDamageMultiplayer * Time.deltaTime;
                int mainEntity = _carPartComponentsPool.Value.Get(weaponCellEntity).MainEntity;
                isPlayer = mainEntity == _sceneService.Value.PlayerEntity;
                // gunConfig.ExplosionRadius is damage that can be minus from all damage from distance - smaller = higher damage
            }

            if (damagedMainEntity == _sceneService.Value.PlayerEntity) {
                var curGameCmp = _currentGameComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);
                damagePoints *= curGameCmp.RoundMoneyMultiplayer;
                damagePoints *= curGameCmp.DifficultyDamageMultiplayer;
            }
                

            if (damagePoints > 0) {
                var damagedCarPartCmp = _carPartComponentsPool.Value.Get(damagedEntity);

                if (damagedEntityGridPosition.x >= 0 && _sceneService.Value.GetCarPartConfig(damagedCarPartCmp.CarPartId).PartType == CarPartType.Armor) {
                    var carCmpDamaged = _carComponentsPool.Value.Get(damagedMainEntity);
                    var damagedCell = carCmpDamaged.CarPartViewsGrid[damagedEntityGridPosition.x, damagedEntityGridPosition.y];
                    var armorConfig = _sceneService.Value.GetArmorFromCarPart(damagedCarPartCmp.CarPartId);
                    if (GetBulletDirection(damagedCell.transform, collisionPoint, armorConfig)) {
                        float defencePercent = armorConfig.DefencePercent * GridUtils.GetArmorLevelMultipliers(damagedCarPartCmp.PartLevel);
                        damagePoints *= 1f - defencePercent / (defencePercent + 100f);
                    }
                }

                if (weaponCellEntity > 0 && _laserComponentsPool.Value.Has(weaponCellEntity)) {
                    ref var laserCmp = ref _laserComponentsPool.Value.Get(weaponCellEntity);
                    if (laserCmp.LaserDamageText == null || laserCmp.LastDamagedEntity != damagedEntity ||
                        !laserCmp.LaserDamageText.gameObject.activeInHierarchy) {
                        laserCmp.DamageCountForLastPart = 0;
                        laserCmp.LastDamagedEntity = damagedEntity;
                        (laserCmp.LaserDamageText, laserCmp.CurrentSequence) =
                            _sceneService.Value.GetFloatingTextPrefab(collisionPoint, damagePoints.ToString("F2"), Color.white);
                    } else {
                        laserCmp.CurrentSequence.Kill();
                        laserCmp.CurrentSequence = _sceneService.Value.SetFloatingText(collisionPoint,
                            laserCmp.DamageCountForLastPart.ToString("F2"), Color.white, laserCmp.LaserDamageText, laserCmp.CurrentSequence);
                        laserCmp.DamageCountForLastPart += damagePoints;
                    }
                } else
                    _sceneService.Value.GetFloatingTextPrefab(collisionPoint, damagePoints.ToString("F2"), Color.white);
               
                if(isPlayer)
                    DataPersistenceManagerSystem._gameData.PlayerDamage += damagePoints;
                
                ChangeHealth(damagePoints, damagedEntity);
            }
        }

        private void ProjectileImpulse(float impulseForce, int damagedEntity, Vector2 collisionPoint) {
            int mainEntity = _carPartComponentsPool.Value.Get(damagedEntity).MainEntity;
            if(mainEntity < 0)return;
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
            if(carPartCellCmp.CurrentHealth <= 0)return;
            carPartCellCmp.CurrentHealth -= health;

            if (carPartCellCmp.MainEntity == -10) {
                if (carPartCellCmp.CurrentHealth <= 0)
                    OpenSupportBox();
                return;
            }
            
            var healthPercent = carPartCellCmp.CurrentHealth / carPartCellCmp.MaxHealth;
            var carPartConfig = _sceneService.Value.GetCarPartConfig(carPartCellCmp.CarPartId);
            if (carPartCellCmp.HealthMaterialIndex == 0 && healthPercent < 0.6f) {
                var needMaterial = _sceneService.Value._carStuffListsConfig.LowDamagedMaterial;
                carPartCellCmp.HealthMaterialIndex = 1;
                ChangeCarPartMaterial(cellEntity, carPartCellCmp, needMaterial, carPartConfig);
            } 
            if (carPartCellCmp.HealthMaterialIndex == 1 && healthPercent < 0.15f) {
                carPartCellCmp.HealthMaterialIndex = 2;
                var needMaterial = _sceneService.Value._carStuffListsConfig.HighDamagedMaterial;
                ChangeCarPartMaterial(cellEntity, carPartCellCmp, needMaterial, carPartConfig);
            }

            if (carPartCellCmp.MainEntity == _sceneService.Value.PlayerEntity) {
                var inventoryCmp = _inventoryComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);
                foreach (var carPartStuff in carPartCellCmp.CellCarPartsStuffViews) {
                    if (carPartConfig.PartType == CarPartType.WheelsContainer || carPartConfig.PartType == CarPartType.CarStuffContainer)
                        inventoryCmp.CarPartInfoUIViewsGrid[carPartStuff._gridPosition.x, carPartStuff._gridPosition.y]
                            .SetHealthCount(_sceneService.Value._healthGradient.Evaluate(healthPercent), (int)carPartCellCmp.CurrentHealth);
                    else {
                        if (carPartConfig.PartType == CarPartType.Main) {
                            _sceneService.Value._playerUIView._lampLightImage.DOKill();
                            _sceneService.Value._playerUIView._lampLightImage.DOColor(new Color(1, 0, 0, 0.6f), 0.45f)
                                .OnComplete( ()=>_sceneService.Value._playerUIView._lampLightImage?.DOColor(new Color(1, 0, 0, 0), 0.45f));
                        }
                        inventoryCmp.CarPartInfoUIViewsUpLevelGrid[carPartStuff._gridPosition.x, carPartStuff._gridPosition.y]
                                                    .SetHealthCount(_sceneService.Value._healthGradient.Evaluate(healthPercent), (int)carPartCellCmp.CurrentHealth);
                    }
                        
                }
            }

            if (carPartCellCmp.CurrentHealth <= 0 && carPartCellCmp.CellCarPartsStuffViews != null) {
                carPartCellCmp.CurrentHealth = 0;
                DestroyCarPartCell(cellEntity);

                if (carPartConfig.PartType == CarPartType.Main) {
                    DestroyNeedCar(carPartCellCmp.MainEntity);
                    if (carPartCellCmp.MainEntity == _sceneService.Value.PlayerEntity)
                        LoseGame();
                }
                
                carPartCellCmp.CellCarPartsStuffViews = null;
            }
            
        }
        private string GetLocalization(string needString) => BetweenScenesService.Instance.CsvLocalization.GetLocalizedValue(needString);
        private void OpenSupportBox() {
            _sceneService.Value.PlayWarningAnimation(GetLocalization("Supplies received")+"!",true);
            _sceneService.Value._supportBoxStuffView._supportBoxcarPartStuffView.gameObject.SetActive(false);
            ref var inventoryCmp = ref _inventoryComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);
            DataPersistenceManagerSystem._gameData.SupportBoxOpened++;
            int itemsCount = _carPartComponentsPool.Value.Get(_sceneService.Value._supportBoxStuffView._supportBoxcarPartStuffView._mainEntity)
                .MaxHealth == 100
                ? 1
                : 3;
            var shopCmp = _shopComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);
            for (int i = 0; i < itemsCount; i++) {
                int rareIndex = GridUtils.GetRandomIndexFromChancesList(shopCmp.CurrentShopItemsChances);
                inventoryCmp.CurrentGivenItems.Add(
                    shopCmp.ListShopItemsForRares[rareIndex][Random.Range(0, shopCmp.ListShopItemsForRares[rareIndex].Count)]);
            }
        }

        private void ChangeCarPartMaterial(int cellEntity, CarPartComponent carPartCellCmp, Material needMaterial, CarPartConfig carPartConfig) {
            if(carPartCellCmp.CarPartCellRenderer != null)
            carPartCellCmp.CarPartCellRenderer.material = needMaterial;

            if (carPartConfig.PartType == CarPartType.MeleeWeapon) 
                _meleeWeaponComponentsPool.Value.Get(cellEntity).MeleeWeaponRenderer.material = needMaterial;
                
            else if (carPartConfig.PartType == CarPartType.Gun && _gunComponentsPool.Value.Get(cellEntity).GunSpriteRenderer != null) 
                _gunComponentsPool.Value.Get(cellEntity).GunSpriteRenderer.material = needMaterial;
        }

        private bool DestroyCarPartCell(int cellEntity, bool checkConnectedGroup = true) {
            ref CarPartComponent carPartCmp = ref _carPartComponentsPool.Value.Get(cellEntity);
            
            if (!carPartCmp.CarPartCellRenderer || !carPartCmp.CarPartCellRenderer.gameObject.activeInHierarchy) return false;
            var carPartConfig = _sceneService.Value.GetCarPartConfig(carPartCmp.CarPartId);      
            ref var inventoryCmp = ref _inventoryComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);
            bool isPlayer =carPartCmp.MainEntity == _sceneService.Value.PlayerEntity;
            bool isEnemy = !inventoryCmp.PlayerTeammateCarsEntity.Contains(carPartCmp.MainEntity);

          ref var fuelCmp = ref _fuelComponentsPool.Value.Get(carPartCmp.MainEntity);
            
            if (carPartConfig.PartType == CarPartType.Gun) {
                ref var gunCmp = ref _gunComponentsPool.Value.Get(cellEntity);
                if (gunCmp.UpPartContainer != null) {
                    gunCmp.GunSpriteRenderer.sortingOrder -= 5;
                    gunCmp.GunSpriteRenderer.color = _sceneService.Value._carStuffListsConfig.DestroyedCarPartColor;
                }

                if (_laserComponentsPool.Value.Has(cellEntity)) {
                 //   Debug.Log("Release laser");
                    ref var laserCmp = ref _laserComponentsPool.Value.Get(cellEntity);
                    _sceneService.Value.ReleaseLaserLineRenderer(laserCmp.LaserLineRenderer);
                    laserCmp.LaserLineRenderer = null;
                    _laserComponentsPool.Value.Del(cellEntity);
                }
            }

            if (carPartConfig.PartType == CarPartType.MeleeWeapon) {
                ref var meleeCmp = ref _meleeWeaponComponentsPool.Value.Get(cellEntity);
                _sceneService.Value.ReleaseMeleeCarPartCell(meleeCmp.MeleeStuff);

                meleeCmp.MeleeWeaponRenderer.sortingOrder -= 5;
                meleeCmp.MeleeWeaponRenderer.color = _sceneService.Value._carStuffListsConfig.DestroyedCarPartColor;
            }

            carPartCmp.CarPartCellRenderer.transform.parent = _sceneService.Value._brokenCarPartsContainer;
            carPartCmp.CarPartCellRenderer.sortingOrder -= 5;
            carPartCmp.CarPartCellRenderer.color = _sceneService.Value._carStuffListsConfig.DestroyedCarPartColor;
            carPartCmp.CarPartCellRenderer = null;
            
            bool destroyCarAfterCarPart = false;
            ref var carCmp = ref _carComponentsPool.Value.Get(carPartCmp.MainEntity);
            if (_shockEffectComponentsPool.Value.Has(cellEntity))
                _shockEffectComponentsPool.Value.Del(cellEntity);
            if ( GridUtils.IsContainerType(carPartConfig)) {
                for (int i = 0; i < carPartCmp.CellCarPartsStuffViews.Count; i++) {
                    var carPart = carPartCmp.CellCarPartsStuffViews[i];
                    var gridPos = carCmp.CarPartContainerGroupsList[cellEntity][i];

                    carCmp.CarPartContainerGroupsGrid[gridPos.x, gridPos.y] = 0;
                    if (carCmp.CarPartGroupsGrid[gridPos.x, gridPos.y] != 0) {
                        if (_sceneService.Value
                                .GetCarPartConfig(_carPartComponentsPool.Value.Get(carCmp.CarPartGroupsGrid[gridPos.x, gridPos.y]).CarPartId)
                                .PartType == CarPartType.Main)
                            destroyCarAfterCarPart = true;

                        DestroyCarPartCell(carCmp.CarPartGroupsGrid[gridPos.x, gridPos.y]);
                    }

                    _sceneService.Value.ReleaseCarPartCell(carPart);
                 /*   if (isPlayer && inventoryCmp.CarPartInfoUIViewsGrid[gridPos.x, gridPos.y]) {
                        _sceneService.Value.ReleaseCarPartInGameUIView(inventoryCmp.CarPartInfoUIViewsGrid[gridPos.x, gridPos.y]);
                        inventoryCmp.CarPartInfoUIViewsGrid[gridPos.x, gridPos.y] = null;
                    }*/
                    
                        
                }

                if (carPartConfig.PartType == CarPartType.WheelsContainer) {
                    var wheelContainer = _wheelContainerComponentsPool.Value.Get(cellEntity);
                    foreach (var wheel in wheelContainer.Wheels) {
                        wheel.transform.GetChild(0).gameObject.SetActive(false);
                        wheel.enabled = false;

                        var wheelSpriteRenderer = wheel.gameObject.GetComponent<SpriteRenderer>();
                        wheelSpriteRenderer.sprite = _sceneService.Value._carStuffListsConfig
                            .Wheels[_sceneService.Value.GetWheelContainerFromCarPart(carPartCmp.CarPartId).WheelId].UpWheelSprite;
                        wheelSpriteRenderer.color = _sceneService.Value._carStuffListsConfig.DestroyedCarPartColor;
                        wheel.transform.parent = _sceneService.Value._brokenCarPartsContainer;
                    }

                    _wheelContainerComponentsPool.Value.Del(cellEntity);
                    carCmp.CurrentMaxWeight -= _sceneService.Value.GetWheelContainerFromCarPart(carPartCmp.CarPartId).MaxWeight;
                }

                if (isEnemy)
                    carCmp.CarPartContainerGroupsList.Remove(cellEntity);

            } else {
                for (int i = 0; i < carPartCmp.CellCarPartsStuffViews.Count; i++) {
                    var carPart = carPartCmp.CellCarPartsStuffViews[i];
                    var gridPos = carCmp.CarPartGroupsList[cellEntity][i];
                    carCmp.CarPartGroupsGrid[gridPos.x, gridPos.y] = 0;
                    string needLayer = !isEnemy ? "CarBase" : "CarBaseEnemy";
                    carPart.SetupCarPart(gridPos, needLayer, carPartCmp.MainEntity);
                   /* if (isPlayer && inventoryCmp.CarPartInfoUIViewsUpLevelGrid[gridPos.x, gridPos.y]) {
                         _sceneService.Value.ReleaseCarPartInGameUIView(inventoryCmp.CarPartInfoUIViewsUpLevelGrid[gridPos.x, gridPos.y]);
                         inventoryCmp.CarPartInfoUIViewsUpLevelGrid[gridPos.x, gridPos.y] = null;
                    }*/
                       
                }

                if (carPartConfig.PartType == CarPartType.Support) {
                    var supportCarPartConfig = _sceneService.Value.GetSupportModuleFromCarPart(carPartCmp.CarPartId); 
                    (float firstMultiplayer,float secondMultiplayer)= GridUtils.GetSupportLevelMultipliers(carPartCmp.PartLevel, supportCarPartConfig.ModuleType);
                    switch (supportCarPartConfig.ModuleType) {
                        case SupportModuleType.Fuel:
                          
                            fuelCmp.MaxFuel -= supportCarPartConfig.FirstCount*firstMultiplayer;
                            fuelCmp.CurrentFuel = Mathf.Min(fuelCmp.MaxFuel, fuelCmp.CurrentFuel);
                            break;
                        case SupportModuleType.Turbine:
                            _fuelComponentsPool.Value.Get(carPartCmp.MainEntity).FuelConsumption -= supportCarPartConfig.FirstCount*firstMultiplayer;
                            _movementComponentsPool.Value.Get(carPartCmp.MainEntity).MaxMoveSpeed -= supportCarPartConfig.SecondCount*secondMultiplayer;
                            break;
                    }
                }

                if (isEnemy) {
                    if (_currentGameComponentsPool.Value.Get(_sceneService.Value.PlayerEntity).CurrentRoundType != RoundType.None &&
                        carPartConfig.PartType != CarPartType.Main) {
                        float randomChance = Random.value;
                        if (randomChance > (0.90f + inventoryCmp.CurrentGivenItems.Count * 0.02f)) {
                            inventoryCmp.CurrentGivenItems.Add(carPartConfig.Id);
                            //in game reward text
                        }
                    }

                    carCmp.CarPartGroupsList.Remove(cellEntity);
                }
            }

            carCmp.CurrentWeight -= carPartConfig.Weight;
            var carConfig = _sceneService.Value.GetCarConfig(carCmp.CarId);

            ref var moveCmp = ref _movementComponentsPool.Value.Get(carPartCmp.MainEntity);
            moveCmp.MovableObjectRigidbody.mass = carCmp.CurrentWeight;
            moveCmp.MaxMoveSpeed = carConfig.DefaultMaxSpeed *
                                   (1 - carCmp.CurrentWeight / carCmp.CurrentMaxWeight);
            

            if (isEnemy && !_destroyEntityEventsPool.Value.Has(cellEntity)) {
                _destroyEntityEventsPool.Value.Add(cellEntity);
                CheckTasks(TaskType.BrokenParts);
            }

            if (GridUtils.IsContainerType(carPartConfig) && !destroyCarAfterCarPart && checkConnectedGroup) {
                int firstMainCarPartContainerEntity = 0;
                int secondMainCarPartContainerEntity = 0;

                foreach (var carPartsInfo in carCmp.CarPartGroupsList) {
                    var checkedCarPartCmp = _carPartComponentsPool.Value.Get(carPartsInfo.Key);
                    if (_sceneService.Value.GetCarPartConfig(checkedCarPartCmp.CarPartId).PartType == CarPartType.Main) {
                        var cellPos = carPartsInfo.Value[0];
                        if (firstMainCarPartContainerEntity != 0)
                            secondMainCarPartContainerEntity = carCmp.CarPartContainerGroupsGrid[cellPos.x, cellPos.y];
                        else
                            firstMainCarPartContainerEntity = carCmp.CarPartContainerGroupsGrid[cellPos.x, cellPos.y];
                    }
                }

                var connectedWithMainPartContainerContainers =
                    GridUtils.FindContactedValues(carCmp.CarPartContainerGroupsGrid, firstMainCarPartContainerEntity);

                if (!connectedWithMainPartContainerContainers.Contains(secondMainCarPartContainerEntity)) {
                    destroyCarAfterCarPart = true;
                } else {
                    var carPartGroupListClone = new Dictionary<int, List<Vector2Int>>(carCmp.CarPartContainerGroupsList);
                    foreach (var containerInfo in carPartGroupListClone) {
                        if (cellEntity == containerInfo.Key) continue;
                        if (!connectedWithMainPartContainerContainers.Contains(containerInfo.Key) &&
                            DestroyCarPartCell(containerInfo.Key, false))
                            return true;
                    }
                }

                if (!isPlayer)
                    SetEnemyCollisionCheckers(carCmp.CarPartContainerGroupsGrid, carCmp.CarView,
                        ref _enemyAIComponentsPool.Value.Get(carPartCmp.MainEntity));
                BuildNewBorder(carPartCmp.MainEntity);
            }
            
            if (destroyCarAfterCarPart) {
                if (!_destroyEntityEventsPool.Value.Has(carPartCmp.MainEntity) || !isEnemy)
                    DestroyNeedCar(carPartCmp.MainEntity);
                if (isPlayer && _currentGameComponentsPool.Value.Get(_sceneService.Value.PlayerEntity).CurrentRoundType != RoundType.None)
                    LoseGame();
            }
            return destroyCarAfterCarPart;
        }

        private void SetEnemyCollisionCheckers(int[,] grid, CarView carView, ref EnemyAIComponent enemyAiCmp) {
            foreach (var leftPoint in enemyAiCmp.CheckedEnemyLeftPoints)
                _sceneService.Value.ReleaseGunContainer(leftPoint);
            foreach (var rightPoint in enemyAiCmp.CheckedEnemyRightPoints)
                _sceneService.Value.ReleaseGunContainer(rightPoint);

            enemyAiCmp.CheckedEnemyLeftPoints = new List<Transform>();
            enemyAiCmp.CheckedEnemyRightPoints = new List<Transform>();

            for (int j = 0; j < grid.GetLength(0); j++) {
                for (int k = 0; k < grid.GetLength(1); k++) {
                    if (grid[j, k] != 0) {
                        var obstacleCheckerTransform = _sceneService.Value.GetGunContainer(carView.CarPartsContainer);
                        obstacleCheckerTransform.localPosition = new Vector3(j - grid.GetLength(0) / 2f, k + grid.GetLength(1) / 2f, 0);
                        if (j > grid.GetLength(0) / 2)
                            enemyAiCmp.CheckedEnemyLeftPoints.Add(obstacleCheckerTransform);
                        else
                            enemyAiCmp.CheckedEnemyRightPoints.Add(obstacleCheckerTransform);
                        break;
                    }
                }
            }
        }

        public bool GetBulletDirection(Transform shieldTransform, Vector2 bulletPosition, ArmorConfig armorConfig) {
            Vector2 directionToBullet = (bulletPosition - (Vector2)shieldTransform.position).normalized;
            Vector2 shieldForward = shieldTransform.right;
            Vector2 shieldRight = shieldTransform.up; // В 2D up обычно это "право" для повернутого объекта
    
            float angle = Vector2.SignedAngle(shieldForward, directionToBullet);
    
            DefenceDirection attackDirection;
    
            if (Mathf.Abs(angle) <= 45f) { 
                attackDirection = DefenceDirection.Right;
            } else if (angle > 45f && angle <= 135f) { 
                attackDirection = DefenceDirection.Up;
            } else if (Mathf.Abs(angle) > 135f) {
             attackDirection = DefenceDirection.Left;
            } else { // angle < -45f && angle >= -135f
                attackDirection = DefenceDirection.Down;  
            }
    
            return armorConfig.DefenceDirections.Contains(attackDirection);
        }
        private void DestroyBrokenParts() {
            foreach (Transform child in _sceneService.Value._brokenCarPartsContainer)
            {
                _sceneService.Value.DestroyGameobject(child.transform);
            }
        }

        private void DestroyNeedCar(int carEntity) {
            ref var carCmp = ref _carComponentsPool.Value.Get(carEntity);
            carCmp.ForwardWheels = new List<Animator>();
            var containersList = new Dictionary<int, List<Vector2Int>>(carCmp.CarPartContainerGroupsList);

            foreach (var carPartEntity in containersList) {
                if (_carPartComponentsPool.Value.Get(carPartEntity.Key).CarPartCellRenderer)
                    DestroyCarPartCell(carPartEntity.Key, false);
            }

            var partsList = new Dictionary<int, List<Vector2Int>>(carCmp.CarPartGroupsList);
            foreach (var carPartEntity in partsList) {
                if (_carPartComponentsPool.Value.Get(carPartEntity.Key).CarPartCellRenderer)
                    DestroyCarPartCell(carPartEntity.Key,false);
                //fix double delete
            }

            if (carCmp.CarView.MainCarContainer)
                _sceneService.Value.DestroyGameobject(carCmp.CarView.MainCarContainer);
            if (_currentGameComponentsPool.Value.Get(_sceneService.Value.PlayerEntity).CurrentRoundType == RoundType.None) return;
            if (!_inventoryComponentsPool.Value.Get(_sceneService.Value.PlayerEntity).PlayerTeammateCarsEntity.Contains(carEntity) && !_destroyEntityEventsPool.Value.Has(carEntity)) {
                _destroyEntityEventsPool.Value.Add(carEntity);
                DataPersistenceManagerSystem._gameData.BrokenCars++;
                CheckTasks(TaskType.Kills);
            }
        }

        private void ClearHealthUI() {
            ref var inventoryCmp = ref _inventoryComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);
        

            for (int i = 0; i < inventoryCmp.CarPartInfoUIViewsGrid.GetLength(0); i++) {
                for (int j = 0; j < inventoryCmp.CarPartInfoUIViewsGrid.GetLength(1); j++) {
                    if (inventoryCmp.CarPartInfoUIViewsGrid[i, j]) {
                        _sceneService.Value.ReleaseCarPartInGameUIView(inventoryCmp.CarPartInfoUIViewsGrid[i, j]);
                        inventoryCmp.CarPartInfoUIViewsGrid[i, j] = null;
                    }
                }
            }
          
            for (int i = 0; i < inventoryCmp.CarPartInfoUIViewsUpLevelGrid.GetLength(0); i++) {
                for (int j = 0; j < inventoryCmp.CarPartInfoUIViewsUpLevelGrid.GetLength(1); j++) {
                    if (inventoryCmp.CarPartInfoUIViewsUpLevelGrid[i, j]) {
                        _sceneService.Value.ReleaseCarPartInGameUIView(inventoryCmp.CarPartInfoUIViewsUpLevelGrid[i, j]);
                        inventoryCmp.CarPartInfoUIViewsUpLevelGrid[i, j] = null;
                    }
                }
            }
        }
        private void BuildNewBorder(int entity) {
            var carCmp = _carComponentsPool.Value.Get(entity);
            PolygonCollider2D collider = carCmp.CarView.CarBaseCollider;
        
            var grid = carCmp.CarPartContainerGroupsGrid;
      
            List<Vector2> contour = GridUtils.FindBorderContour(grid);
       
            collider.SetPath(0, contour.ToArray());
        }
        public void CheckTasks(TaskType taskType) {
            foreach (var taskEntity in _taskComponentsFilter.Value) {
                ref var taskCmp = ref _taskComponentsPool.Value.Get(taskEntity);
                if (taskCmp.TaskType != taskType) continue;
                taskCmp.CurrentCount++;
                TaskUtilits.UpdateTaskText(taskCmp);
                if (taskCmp.CurrentCount >= taskCmp.NeedCount) {
                    taskCmp.TaskText.gameObject.SetActive(false);
                    _playerCurrencyComponentsPool.Value.Get(_sceneService.Value.PlayerEntity).MoneyCount += taskCmp.MoneyReward;
                    DataPersistenceManagerSystem._gameData.MoneyEarnedInGame += taskCmp.MoneyReward;
                }
            }
        }
    }
