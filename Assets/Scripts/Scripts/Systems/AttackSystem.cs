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
        private EcsPoolInject<FrozenComponent> _frozenComponentsPool;
        private EcsPoolInject<CarPartComponent> _carPartComponentsPool;
        private EcsPoolInject<InventoryComponent> _inventoryComponentsPool;
        private EcsPoolInject<EnemyAIComponent> _enemyAIComponentsPool;
        private EcsPoolInject<FuelComponent> _fuelComponentsPool;
        private EcsPoolInject<EndRoundEvent> _endRoundEventsPool;
        private EcsPoolInject<CompleteTaskEvent> _completeTaskEventsPool;
        private EcsPoolInject<ShopComponent> _shopComponentsPool;
        private EcsPoolInject<UpdateCarsStatsInfoEvent> _updateCarsStatsInfoEventsPool;
        private EcsPoolInject<WheelContainerComponent> _wheelContainerComponentsPool;
        private EcsPoolInject<CarPartInfoComponent> _carPartInfoComponentsPool;
        private EcsPoolInject<ThunderstormComponent> _thunderstormComponentsPool;
        private EcsPoolInject<TornadoSpawnControllerComponent> _tornadoSpawnControllerComponentsPool;
        private EcsPoolInject<TornadoComponent> _tornadoComponentsPool;
        private EcsPoolInject<BomberPlaneControllerComponent> _bomberPlaneControllerComponentsPool;
        private EcsPoolInject<BomberProjectile> _bomberProjectilesPool;
        
        private EcsFilterInject<Inc<ShockEffectComponent>> _shockEffectComponentsFilter;
        private EcsFilterInject<Inc<CarComponent>> _carComponentsFilter;
        private EcsFilterInject<Inc<FireComponent>> _fireComponentsFilter;
        private EcsFilterInject<Inc<FrozenComponent>> _frozenComponentsFilter;
        private EcsFilterInject<Inc<CompleteTaskEvent>> _completeTaskEventsFilter;
        private EcsFilterInject<Inc<TaskComponent>> _taskComponentsFilter;
        private EcsFilterInject<Inc<MeleeWeaponComponent>> _meleeWeaponComponentsFilter;
        private EcsFilterInject<Inc<GunComponent>> _gunComponentsFilter;
        private EcsFilterInject<Inc<EndRoundEvent>> _endRoundEventsFilter;
        private EcsFilterInject<Inc<CarPartCellComponent>> _carPartCellComponentsFilter;
        private EcsFilterInject<Inc<ProjectileComponent>> _projectileComponentsFilter;
        private EcsFilterInject<Inc<ConstructMeleeCellEvent>> _constructMeleeCellEventsFilter;
        private EcsFilterInject<Inc<ThunderstormComponent>> _thunderstormComponentsFilter;
        private EcsFilterInject<Inc<BomberProjectile>> _bomberProjectilesFilter;
        
        public void Run(IEcsSystems systems) {
            foreach (var endRoundEntity in _endRoundEventsFilter.Value) {
                EndRound();
                EndAllMapEvent();
                ClearHealthUI();
                foreach (var completeTask in _completeTaskEventsFilter.Value) {
                    _completeTaskEventsPool.Value.Del(completeTask);
                    
                   CheckTasks(_taskComponentsPool.Value.Get(completeTask).TaskType); 
                }
               
            }

            float deltaTime = Time.deltaTime;
            ref var curGameCmp = ref _currentGameComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);
            if (RoundUtilits.IsNotBattleRound(curGameCmp.CurrentRoundType)) return;
            
            if (_tornadoSpawnControllerComponentsPool.Value.Has(_sceneService.Value.PlayerEntity)) {
                CheckTornadoController();
            }

            if (_bomberPlaneControllerComponentsPool.Value.Has(_sceneService.Value.PlayerEntity)) {
                CheckBomberController();
            }
            
            foreach (var thunderEntity in _thunderstormComponentsFilter.Value) {
                ref var thunderstormCmp = ref _thunderstormComponentsPool.Value.Get(thunderEntity);
                thunderstormCmp.TimeToNextThunder -= deltaTime;
                if (thunderstormCmp.TimeToNextThunder <= 0) {
                    if (thunderstormCmp.ThunderTransform) {
                       _sceneService.Value.DestroyGameobject(thunderstormCmp.ThunderTransform); 
                       thunderstormCmp.ThunderTransform = null;
                       thunderstormCmp.TimeToNextThunder = Random.Range(5, 15);
                    } else { 
                        thunderstormCmp.ExplosionIsSpawned = false;
                        var needPosition = _movementComponentsPool.Value.Get(_sceneService.Value.PlayerEntity).MovableObjectRigidbody.transform
                            .position;
                        Vector2 randomCircle = Random.insideUnitCircle * 15f;

                        if (randomCircle.magnitude < 2f)
                            randomCircle = randomCircle.normalized * 2f;

                        needPosition += new Vector3(randomCircle.x, randomCircle.y, 0);
                        thunderstormCmp.TimeToNextThunder = 1;
                        thunderstormCmp.ThunderTransform = _sceneService.Value.InstantiateObject(_sceneService.Value._thunderPrefab, needPosition);
                    }
                } else if (thunderstormCmp.ThunderTransform && !thunderstormCmp.ExplosionIsSpawned &&
                           thunderstormCmp.TimeToNextThunder <= 0.5f) {
                    thunderstormCmp.ExplosionIsSpawned = true;
                    //thunder explosion

                    LayerMask layerMask = LayerMask.GetMask(new string[] { "CarBase", "UpCarPart", "CarBaseEnemy", "UpCarPartEnemy" });

                    var castedObjects = Physics2D.OverlapCircleAll(thunderstormCmp.ThunderTransform.position, 3, layerMask);

                    foreach (var damagedObject in castedObjects) {
                        damagedObject.TryGetComponent(out CarPartStuffView damagedCarPartView);
                        if (!damagedCarPartView || !_carComponentsPool.Value.Has(damagedCarPartView._mainEntity)) continue;

                        var carCmp = _carComponentsPool.Value.Get(damagedCarPartView._mainEntity);
                        int carPartEntity = carCmp.CarPartGroupsGrid[damagedCarPartView._gridPosition.x, damagedCarPartView._gridPosition.y];
                        if (carPartEntity == 0)
                            carPartEntity =
                                carCmp.CarPartContainerGroupsGrid[damagedCarPartView._gridPosition.x, damagedCarPartView._gridPosition.y];
                        SetEffectComponent(carPartEntity, EffectType.shock, 3);
                        SetEffectComponent(carPartEntity, EffectType.fire, 10);
                    }
                }
                
               
            }
            
            foreach (var gunEntity in _gunComponentsFilter.Value) {
                if (_shockEffectComponentsPool.Value.Has(gunEntity)) {
                    if (_laserComponentsPool.Value.Has(gunEntity))
                        _laserComponentsPool.Value.Get(gunEntity).LaserLineRenderer.gameObject.SetActive(false);
                    continue;
                }

                ref var gunCmp = ref _gunComponentsPool.Value.Get(gunEntity);
                var carPartCmp = _carPartComponentsPool.Value.Get(gunEntity);
                var carPartInfoCmp = _carPartInfoComponentsPool.Value.Get(gunEntity);
                var gunConfig = _sceneService.Value.GetGunFromCarPart(carPartInfoCmp.CarPartId);

                gunCmp.CurrentCountdown -= deltaTime;

                if (gunCmp.UpPartContainer != null && gunConfig.RotateSpeed != 0) {
                    gunCmp.GunOnTargetTime -= deltaTime;
                    if (gunCmp.GunOnTargetTime <= 0) {
                        var needLayer =
                            carPartCmp.MainEntity == _sceneService.Value.PlayerEntity ||
                            !_enemyAIComponentsPool.Value.Get(carPartCmp.MainEntity).IsEnemy
                                ? LayerMask.GetMask("UpCarPartEnemy","PlayerDamageable")
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
                var carPartInfoCmp = _carPartInfoComponentsPool.Value.Get(cellEntity);
                var meleeConfig = _sceneService.Value.GetMeleeWeaponFromCarPart(carPartInfoCmp.CarPartId);
                if (meleeConfig.FuelConsumption != 0) {
                    ref var fuelCmp = ref _fuelComponentsPool.Value.Get(_carPartComponentsPool.Value.Get(cellEntity).MainEntity);
                    if (fuelCmp.CurrentFuel <= 0) return;
                    fuelCmp.CurrentFuel -= meleeConfig.FuelConsumption * Time.deltaTime;
                }

                if (meleeCmp.Countdown > 0)
                    meleeCmp.Countdown -= deltaTime;
                else if (meleeConfig.IsAttackAfterCountdown && meleeCmp.isTriggered) {
                    CastMeleeWeapon(cellEntity);
                }
            }

            foreach (var shockEntity in _shockEffectComponentsFilter.Value) {
                if(!_shockEffectComponentsPool.Value.Has(shockEntity))continue;
                ref var shockCmp = ref _shockEffectComponentsPool.Value.Get(shockEntity);
                shockCmp.EffectDuration -= deltaTime;
                if (shockCmp.EffectDuration <= 0) {
                    var damagedCarPart = _carPartInfoComponentsPool.Value.Get(shockEntity).CarPartId;
                    if (_sceneService.Value.GetCarPartConfig(damagedCarPart).PartType == CarPartType.Main)
                        _carComponentsPool.Value.Get(_carPartComponentsPool.Value.Get(shockEntity).MainEntity).ShockedMainPartsCount--;
                  
                    foreach (var particles in shockCmp.ParticleSystem) {
                        _sceneService.Value.ReleaseCarPartParticles(particles);
                    }  
                    _shockEffectComponentsPool.Value.Del(shockEntity);
                }
            }
            
            foreach (var frozeEntity in _frozenComponentsFilter.Value) {
                if(!_frozenComponentsPool.Value.Has(frozeEntity))continue;
                ref var frozeCmp = ref _frozenComponentsPool.Value.Get(frozeEntity);
                frozeCmp.Duration -= deltaTime;
                if (frozeCmp.Duration <= 0) {
                
                    foreach (var particles in frozeCmp.ParticleSystem) {
                        _sceneService.Value.ReleaseCarPartParticles(particles);
                    }  
                    _frozenComponentsPool.Value.Del(frozeEntity);
                }
            }

            foreach (var burnedEntity in _fireComponentsFilter.Value) {
                if(!_fireComponentsPool.Value.Has(burnedEntity))continue;
                ref var fireCmp = ref _fireComponentsPool.Value.Get(burnedEntity);
                fireCmp.Duration -= deltaTime;
                fireCmp.LastFireSeconds += deltaTime;
                if (fireCmp.LastFireSeconds >= 1) {
                    fireCmp.LastFireSeconds--;
                    int damage = 1+(Mathf.CeilToInt(fireCmp.Duration/5));
                    ChangeHealth(damage, burnedEntity);
                    if (_carPartInfoComponentsPool.Value.Get(burnedEntity).CurrentHealth <= 0 ||
                        _carPartComponentsPool.Value.Get(burnedEntity).CarPartCellRenderer == null) {
                     //   foreach (var particles in fireCmp.ParticleSystem) {
                    //        _sceneService.Value.ReleaseCarPartParticles(particles);
                     //   }  
                      //  _fireComponentsPool.Value.Del(burnedEntity);
                        continue;
                    }

                    _sceneService.Value.GetFloatingTextPrefab(
                        _carPartComponentsPool.Value.Get(burnedEntity).CarPartCellRenderer.transform.position, damage.ToString("F2"),
                        new Color(1, 0.5f, 0.1f, 1));
                    if (damage <= 1) {
                        foreach (var particles in fireCmp.ParticleSystem) {
                            _sceneService.Value.ReleaseCarPartParticles(particles);
                        }  
                        _fireComponentsPool.Value.Del(burnedEntity);
                    }
                }
            }
        }

        private void CheckTornadoController() {
            ref var tornadoControllerCmp = ref _tornadoSpawnControllerComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);
            tornadoControllerCmp.TimeToNextTornado -= Time.deltaTime;
            if (tornadoControllerCmp.TimeToNextTornado <= 0) {
                tornadoControllerCmp.TimeToNextTornado = Random.Range(10f, 15f);
                int tornadoEntity = _world.Value.NewEntity();
                ref var tornadoCmp = ref _tornadoComponentsPool.Value.Add(tornadoEntity);
                tornadoCmp.LifeTime = Random.Range(20f, 30f);
               
                int tornadoIndex = tornadoControllerCmp.TornadoToSpawn[Random.Range(0, tornadoControllerCmp.TornadoToSpawn.Count)];
                tornadoCmp.TornadoAnimator = _sceneService.Value
                    .InstantiateObject(
                        _sceneService.Value._tornadoPrefabs[tornadoIndex],
                        Random.insideUnitCircle * 80).GetComponent<Animator>();
                tornadoCmp.TornadoAnimator.gameObject.GetComponent<TornadoView>().Construct(StayInTornado,tornadoEntity);
                tornadoCmp.EffectType = (EffectType)tornadoIndex;
            }
        }
        
        private void CheckBomberController() {
            ref var bomberControllerCmp = ref _bomberPlaneControllerComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);
            var bomberShadow = bomberControllerCmp.BomberShadowTransform;
            if(!bomberShadow.gameObject.activeInHierarchy)
            bomberControllerCmp.TimeToNextBomber -= Time.deltaTime;
            if (bomberControllerCmp.TimeToNextBomber <= 0) {
                if (!bomberShadow.gameObject.activeInHierarchy) {
                    int sideIndex = Random.Range(0, 4);
                    float xPos;
                    float yPos;
                    if (sideIndex > 1) {
                        xPos = sideIndex == 2 ? 140 : -140;
                        yPos = _movementComponentsPool.Value.Get(_sceneService.Value.PlayerEntity).PreviousPosition.y;
                        bomberShadow.position = new Vector2(-xPos, yPos);
                        bomberShadow.localEulerAngles = new Vector3(0, 0, -90*xPos/140);
                    } else {
                        yPos = sideIndex == 1 ? 140 : -140;

                        xPos = _movementComponentsPool.Value.Get(_sceneService.Value.PlayerEntity).PreviousPosition.x;
                        bomberShadow.position = new Vector2(xPos, -yPos);
                        bomberShadow.localEulerAngles = new Vector3(0, 0, yPos<0?180:0);
                    }

                    bomberControllerCmp.TimeToNextBomb = 3f;
                    bomberControllerCmp.FinalPosition = new Vector2(xPos, yPos);
                    bomberShadow.gameObject.SetActive(true);

                    bomberShadow.DOKill();
                    bomberShadow.DOMove(bomberControllerCmp.FinalPosition, 20).SetEase(Ease.Linear).OnComplete(() => {
                        bomberShadow.gameObject.SetActive(false);
                        _bomberPlaneControllerComponentsPool.Value.Get(_sceneService.Value.PlayerEntity).TimeToNextBomber = Random.Range(8f, 15f);
                    });

                } else {
                    bomberControllerCmp.TimeToNextBomb -= Time.deltaTime;
                    if (bomberControllerCmp.TimeToNextBomb <= 0) {
                        
                        bomberControllerCmp.TimeToNextBomb = 3;
                        var bombSpriteRenderer = _sceneService.Value.GetSpriteRenderer();
                        bombSpriteRenderer.sortingOrder = 16;
                        bombSpriteRenderer.sprite = _sceneService.Value._bomberProjectilesShadows[bomberControllerCmp.BombIndex];
                        bombSpriteRenderer.transform.position = bomberShadow.position;
                        bombSpriteRenderer.transform.rotation = bomberShadow.rotation;
                        bombSpriteRenderer.color = new Color(0,0,0,0.3f);
                        int bombEntity = _world.Value.NewEntity();
                        ref var bombCmp = ref _bomberProjectilesPool.Value.Add(bombEntity);
                        bombCmp.BombLifetime = 5;
                        bombCmp.BombSpriteRenderer = bombSpriteRenderer;
                    }
                }
            }

            foreach (var bombEntity in _bomberProjectilesFilter.Value) {
                ref var bombCmp = ref _bomberProjectilesPool.Value.Get(bombEntity);
                bombCmp.BombLifetime -= Time.deltaTime;

                if (bombCmp.BombLifetime > 3) {
                    bombCmp.BombSpriteRenderer.transform.localScale = Vector3.one * (3-bombCmp.BombLifetime/2);
                } else if (bombCmp.BombLifetime > 0) {
                    if (bombCmp.BombSpriteRenderer.color != Color.white) {
                         bombCmp.BombSpriteRenderer.sprite = _sceneService.Value._bomberProjectiles[bomberControllerCmp.BombIndex];
                         bombCmp.BombSpriteRenderer.color = Color.white;
                    }
                   
                    bombCmp.BombSpriteRenderer.transform.localScale = Vector3.one * (bombCmp.BombLifetime * 2 + 2);
                } else
                    ExplodeBomberProjectile(bombEntity);
            }

          
        }

        private void ExplodeBomberProjectile(int bombEntity) {
            var bombCmp = _bomberProjectilesPool.Value.Get(bombEntity);
            var bomberControllerCmp = _bomberPlaneControllerComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);
            CalculateExplodeDamage((EffectType)bomberControllerCmp.BombIndex, bomberControllerCmp.BombIndex == 0 ? 250 : 15, 5,
                bombCmp.BombSpriteRenderer.transform.position);
            CreateExplosionEffect(5f, bombCmp.BombSpriteRenderer.transform.position, (EffectType)bomberControllerCmp.BombIndex);
           
            _sceneService.Value.ReleaseSpriteRenderer(bombCmp.BombSpriteRenderer);
            _bomberProjectilesPool.Value.Del(bombEntity);
        }

        private void CalculateExplodeDamage(EffectType effect, float duration, float radius, Vector2 explodePosition) {
            LayerMask layerMask = LayerMask.GetMask( "CarBase", "UpCarPart", "CarBaseEnemy", "UpCarPartEnemy" );

            var castedObjects = Physics2D.OverlapCircleAll(explodePosition, radius, layerMask);

            foreach (var damagedObject in castedObjects) {
                damagedObject.TryGetComponent(out CarPartStuffView damagedCarPartView);
                if (!damagedCarPartView || !_carComponentsPool.Value.Has(damagedCarPartView._mainEntity)) continue;

                var carCmp = _carComponentsPool.Value.Get(damagedCarPartView._mainEntity);
                if(!carCmp.CarView)continue;
                int carPartEntity = carCmp.CarPartGroupsGrid[damagedCarPartView._gridPosition.x, damagedCarPartView._gridPosition.y];
                if (carPartEntity == 0)
                    carPartEntity = carCmp.CarPartContainerGroupsGrid[damagedCarPartView._gridPosition.x, damagedCarPartView._gridPosition.y];
           
                float distanceMultiplayer = Vector2.Distance(damagedCarPartView.transform.position, explodePosition) / radius;
                if (effect != EffectType.none)
                    SetEffectComponent(carPartEntity, effect, duration * distanceMultiplayer);
                else {
                     ChangeHealth(duration * distanceMultiplayer, carPartEntity);
                     _sceneService.Value.GetFloatingTextPrefab(explodePosition, (duration * distanceMultiplayer).ToString("F2"), Color.white);
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
                var carPartInfoCmp = _carPartInfoComponentsPool.Value.Get(carPartEntity);
                healCost += GetHealCostForCarPart(carPartCmp.MaxHealth, carPartInfoCmp.CurrentHealth,
                    _sceneService.Value.GetCarPartConfig(carPartInfoCmp.CarPartId));
            }

            foreach (var carPartEntity in carCmp.CarPartGroupsGrid) {
                var carPartCmp = _carPartComponentsPool.Value.Get(carPartEntity);
                var carPartInfoCmp = _carPartInfoComponentsPool.Value.Get(carPartEntity);
                healCost += GetHealCostForCarPart(carPartCmp.MaxHealth, carPartInfoCmp.CurrentHealth,
                    _sceneService.Value.GetCarPartConfig(carPartInfoCmp.CarPartId));
            }

            return healCost;
        }

        private float GetHealCostForCarPart(float maxHp,float currentHp, CarPartConfig carPartConfig) {
            return carPartConfig.MoneyCost * (1f-currentHp / maxHp) / 4;
        }

        private void TrySpawnLaser(int entity) {
            ref var carPartCmp = ref _carPartComponentsPool.Value.Get(entity);
            var carPartInfoCmp = _carPartInfoComponentsPool.Value.Get(entity);
            ref var gunCmp = ref _gunComponentsPool.Value.Get(entity);
            if (gunCmp.FirePointTransform == null || !_laserComponentsPool.Value.Has(entity)) return;
            var laserLineRenderer = _laserComponentsPool.Value.Get(entity).LaserLineRenderer;
            var gunConfig = _sceneService.Value.GetGunFromCarPart(carPartInfoCmp.CarPartId);
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
                        "Obstacle", "UpCarPartEnemy","PlayerDamageable"
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
            
            DestroyAllProjectiles();
            DestroyAllCars();

            DestroyBrokenParts();
            SetPlayerCarGrids();
            if(!_updateCarsStatsInfoEventsPool.Value.Has(_sceneService.Value.PlayerEntity))
            _updateCarsStatsInfoEventsPool.Value.Add(_sceneService.Value.PlayerEntity);

         
        }

        private void EndAllMapEvent() {
            if (_bomberPlaneControllerComponentsPool.Value.Has(_sceneService.Value.PlayerEntity)) {
                ref var bomberControllerCmp = ref _bomberPlaneControllerComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);
                bomberControllerCmp.BomberShadowTransform.gameObject.SetActive(false);
                bomberControllerCmp.TimeToNextBomber = 5f;
                bomberControllerCmp.BomberShadowTransform.DOKill();
                foreach (var bombEntity in _bomberProjectilesFilter.Value) {
                    var bombCmp = _bomberProjectilesPool.Value.Get(bombEntity);
                    _sceneService.Value.ReleaseSpriteRenderer(bombCmp.BombSpriteRenderer);
                    _bomberProjectilesPool.Value.Del(bombEntity);
                }
            }
           
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
            _currentGameComponentsPool.Value.Get(_sceneService.Value.PlayerEntity).CurrentRoundType = RoundType.None;
         //   if (_currentGameComponentsPool.Value.Get(_sceneService.Value.PlayerEntity).CurrentRoundType != RoundType.None)
       //      EndRound();
            Time.timeScale = 0;
            int currentRound = _currentGameComponentsPool.Value.Get(_sceneService.Value.PlayerEntity).CurrentRound;
            int scrapCount = _betweenScenesService.Value._carStuffListsConfig.MapConfigs[_betweenScenesService.Value.CurrentSelectedMap]
                .MapDifficltyConfigs[_betweenScenesService.Value.CurrentSelectedMapDifficlty].ScrapForRound*currentRound;
            DataPersistenceManagerSystem._gameData.ScrapCount += scrapCount;
            _sceneService.Value._endGameScreenView.SetLoseScreenInfo(currentRound,scrapCount);
            CheckCoreAchievements();
            DataPersistenceManagerSystem.SaveGame();
            //go to meta
        }
        private void CheckCoreAchievements() {
        var carCmp = _carComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);
        int upgradedCount = carCmp.CarLevel;
        float maxFuel = _sceneService.Value.GetCarConfig(carCmp.CarId).DefaultMaxFuel *
                        GridUtils.GetCarLevelMultipliers(carCmp.CarLevel).baseMaxFuelMultiplier;
        foreach (var carPartContainer in carCmp.CarPartContainerGroupsList) {
            var carPartCmp = _carPartInfoComponentsPool.Value.Get(carPartContainer.Key);
            upgradedCount += carPartCmp.PartLevel;
        }

        foreach (var carPart in carCmp.CarPartGroupsList) {
            var carPartCmp = _carPartInfoComponentsPool.Value.Get(carPart.Key);
            upgradedCount += carPartCmp.PartLevel;

            var carPartConfig = _sceneService.Value.GetCarPartConfig(carPartCmp.CarPartId);
            if (carPartConfig is SupportModuleCarPartConfig supportConfig) {
                if (supportConfig.ModuleType == SupportModuleType.Fuel)
                    maxFuel += supportConfig.FirstCount * GridUtils.GetSupportLevelMultipliers(carPartCmp.PartLevel, supportConfig.ModuleType)
                        .firstMultiplayer;
            }
        }

        foreach (var achievementConfig in _betweenScenesService.Value._carStuffListsConfig.AchievementConfigs) {
            if (DataPersistenceManagerSystem._gameData.AchievementsCompleted[achievementConfig.achievementId])
                switch (achievementConfig.achievementType) {
                    case AchievementType.ModulesOnOneCarPart:
                        if (achievementConfig.needCount <= GetMaxModulesOnCarPart())
                            _betweenScenesService.Value.CompletedAchievments.Add(achievementConfig.achievementId);
                        break;
                    case AchievementType.MaxFuelInCar:
                        if (achievementConfig.needCount <= maxFuel)
                            _betweenScenesService.Value.CompletedAchievments.Add(achievementConfig.achievementId);
                        break;
                }
        }
    }

    private int GetMaxModulesOnCarPart() {
        var carCmp = _carComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);
        int maxModules = 0;
        foreach (var carPartContainer in carCmp.CarPartContainerGroupsList) {
            var carPartCmp = _carPartInfoComponentsPool.Value.Get(carPartContainer.Key);
            int modulesOnCarPart = 0;
            foreach (var moduleId in carPartCmp.ModuleIds) {
                if (moduleId != -1)
                    modulesOnCarPart++;
            }
            maxModules = Mathf.Max(maxModules, modulesOnCarPart);
        }

        foreach (var carPart in carCmp.CarPartGroupsList) {
            var carPartCmp = _carPartInfoComponentsPool.Value.Get(carPart.Key);
            int modulesOnCarPart = 0;
            foreach (var moduleId in carPartCmp.ModuleIds) {
                if (moduleId != -1)
                    modulesOnCarPart++;
            }
            maxModules = Mathf.Max(maxModules, modulesOnCarPart);
        }

        return maxModules;
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
            var carPartInfoCmp = _carPartInfoComponentsPool.Value.Get(entity);
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

                    ProjectileHit(AttackType.Melee, carPartInfoCmp.CarPartId, damagedCarPart._gridPosition, damagedCarPart._mainEntity, hit.point,
                        null, entity, needTag);
                }
            } else {
                meleeWeaponComponent.isTriggered = false;
            }
        }

        private void ConstructMeleeWeaponCell(int entity) {
            var carPartComponent = _carPartComponentsPool.Value.Get(entity);
            var carPartInfoCmp = _carPartInfoComponentsPool.Value.Get(entity);
            var meleeConfig = _sceneService.Value.GetMeleeWeaponFromCarPart(carPartInfoCmp.CarPartId);
            var meleeStuffCarPart = _meleeWeaponComponentsPool.Value.Get(entity).MeleeStuff;

            meleeStuffCarPart.ConstructMelee(meleeConfig.UpSprite, ProjectileHit,
                carPartComponent.MainEntity == _sceneService.Value.PlayerEntity ||
                !_enemyAIComponentsPool.Value.Get(carPartComponent.MainEntity).IsEnemy
                    ? "Enemy"
                    : "Player", carPartInfoCmp.CarPartId, entity);
        }

        private void CreateExplosionEffect(float radius, Vector2 position, EffectType effectType) {
            var explosionPrefab = _sceneService.Value.GetSpriteRenderer();
            explosionPrefab.gameObject.layer = LayerMask.NameToLayer("PixelPerfect");
            explosionPrefab.sortingOrder = 8;
            explosionPrefab.sprite = _sceneService.Value._whiteCircle;
            explosionPrefab.transform.localScale = Vector3.zero;
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
                _sceneService.Value.ReleaseSpriteRenderer(explosionPrefab);
            });
        }

        private void SpawnProjectile(int gunEntity) {
            var carPartCmp = _carPartComponentsPool.Value.Get(gunEntity);
            var carPartInfoCmp = _carPartInfoComponentsPool.Value.Get(gunEntity);
            ref var gunCmp = ref _gunComponentsPool.Value.Get(gunEntity);
            var gunConfig = _sceneService.Value.GetGunFromCarPart(carPartInfoCmp.CarPartId);
           
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
                    projectileCmp.LifeTime = gunConfig.ProjectileLifeTime;
                    projectileCmp.Damage = gunCmp.CurrentDamage;
                    projectileCmp.CurrentBreakingThroughCount = gunCmp.MaxPiercingCount;
                } else {
                    int newEntity = _world.Value.NewEntity();
                    currentProjectile.entity = newEntity;
                    ref var projectileCmp = ref _projectileComponentsPool.Value.Add(currentProjectile.entity);
                    projectileCmp.MainEntity = carPartCmp.MainEntity;
                    projectileCmp.IsActive = true;
                    projectileCmp.ProjectileView = currentProjectile;
                    projectileCmp.LifeTime = gunConfig.ProjectileLifeTime;
                    projectileCmp.Damage = gunCmp.CurrentDamage;
                    projectileCmp.CurrentBreakingThroughCount = gunCmp.MaxPiercingCount;
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
            ref var carPartCmp = ref _carPartComponentsPool.Value.Get(carPartEntity); 
            if(carPartCmp.CellCarPartsStuffViews == null)return;
            switch (effectType) {
                case EffectType.shock:
                    if (_shockEffectComponentsPool.Value.Has(carPartEntity)) {
                        ref var shockCmp = ref _shockEffectComponentsPool.Value.Get(carPartEntity);
                        if (shockCmp.EffectDuration < duration)
                            shockCmp.EffectDuration = duration;
                    } else {
                        
                      
                        var damagedCarPart = _carPartInfoComponentsPool.Value.Get(carPartEntity).CarPartId;
                        if (_sceneService.Value.GetCarPartConfig(damagedCarPart).PartType == CarPartType.Main)
                            _carComponentsPool.Value.Get(carPartCmp.MainEntity).ShockedMainPartsCount++;

                        ref var shockCmp = ref _shockEffectComponentsPool.Value.Add(carPartEntity);
                        shockCmp.EffectDuration = duration;
                        shockCmp.ParticleSystem = new List<ParticleSystem>();
                        
                        foreach (var carPartStuff in carPartCmp.CellCarPartsStuffViews) {
                            var particleSystem = _sceneService.Value.GetCarPartParticles();
                            shockCmp.ParticleSystem.Add(particleSystem);
                            particleSystem.transform.parent = carPartStuff.transform;
                            particleSystem.transform.localPosition = Vector3.zero;
                            var particleMain = particleSystem.main;
                            particleMain.startColor = new Color(1f, 1f, 1f, 0.9f);
                        }
                    }

                    break;
                case EffectType.fire:
                    if (_fireComponentsPool.Value.Has(carPartEntity)) {
                        ref var fireCmp = ref _fireComponentsPool.Value.Get(carPartEntity);
                        fireCmp.Duration += duration;
                    } else {
                        if (_frozenComponentsPool.Value.Has(carPartEntity)) {
                            ref var shockCmp = ref _frozenComponentsPool.Value.Get(carPartEntity);
                            shockCmp.Duration -= duration;
                        } else {
                            ref var fireCmp = ref _fireComponentsPool.Value.Add(carPartEntity);
                            fireCmp.Duration = duration;
                            fireCmp.ParticleSystem = new List<ParticleSystem>();
                            foreach (var carPartStuff in carPartCmp.CellCarPartsStuffViews) {
                                var particleSystem = _sceneService.Value.GetCarPartParticles();
                                fireCmp.ParticleSystem.Add(particleSystem);
                                particleSystem.transform.parent = carPartStuff.transform;
                                particleSystem.transform.localPosition = Vector3.zero;
                                var particleMain = particleSystem.main;
                                particleMain.startColor = new Color(0.9f, 0.4f, 0.2f, 0.8f);
                            }
                        }
                    }

                    break;

                case EffectType.froze:
                    if (_frozenComponentsPool.Value.Has(carPartEntity)) {
                        ref var shockCmp = ref _frozenComponentsPool.Value.Get(carPartEntity);
                        shockCmp.Duration += duration;
                    } else {
                        if (_fireComponentsPool.Value.Has(carPartEntity)) {
                            ref var fireCmp = ref _fireComponentsPool.Value.Get(carPartEntity);
                            fireCmp.Duration -= duration;
                        } else {
                            ref var frozenCmp = ref _frozenComponentsPool.Value.Add(carPartEntity);
                            frozenCmp.ParticleSystem = new List<ParticleSystem>();
                            foreach (var carPartStuff in carPartCmp.CellCarPartsStuffViews) {
                                var particleSystem = _sceneService.Value.GetCarPartParticles();
                                frozenCmp.ParticleSystem.Add(particleSystem);
                                particleSystem.transform.parent = carPartStuff.transform;
                                particleSystem.transform.localPosition = Vector3.zero;
                                var particleMain = particleSystem.main;
                                particleMain.startColor = new Color(0.25f, 0.5f, 0.8f, 0.7f);
                            }

                            //  _carPartComponentsPool.Value.Get(carPartEntity).CarPartCellRenderer.materials.Ad
                        }
                    }

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
            if (damagedEntityGridPosition.x <= -10) {
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
                
                var gunConfig = _sceneService.Value.GetGunFromCarPart(weaponId);
                damagePoints = projectileCmp.Damage;
                
                projectileCmp.CurrentBreakingThroughCount--;
                if (projectileCmp.CurrentBreakingThroughCount <= 0) {
                    projectileCmp.IsActive = false;
                    _sceneService.Value.ReleaseGunProjectile(projectile, projectileCmp.ExplosionRadius, _sceneService.Value._bulletSprites);
                    projectile.rigidbody2D.linearVelocity = Vector2.zero;
                } else 
                    projectileCmp.Damage *= 1 - gunConfig.BreakingThroughDamageFalloff;
                
                isPlayer = projectileCmp.MainEntity == _sceneService.Value.PlayerEntity;
                
                if (gunConfig.ImpulseForce != 0)
                    ProjectileImpulse(gunConfig.ImpulseForce, damagedEntity, projectile.rigidbody2D.linearVelocity.normalized);

                LayerMask layerMask = layerName == "Player"
                    ? LayerMask.GetMask(new string[] { "CarBase", "UpCarPart" })
                    : LayerMask.GetMask(new string[] { "CarBaseEnemy", "UpCarPartEnemy","PlayerDamageable" });
                var carPartDamagedCmp = _carPartComponentsPool.Value.Get(damagedEntity);
                var carPartInfoCmp = _carPartInfoComponentsPool.Value.Get(damagedEntity);
                if (projectileCmp.CurrentBreakingThroughCount <= 0 && gunConfig.ExplosionRadius != 0 &&
                    (_sceneService.Value.GetCarPartConfig(carPartInfoCmp.CarPartId).PartType != CarPartType.Armor ||
                     !_sceneService.Value.GetArmorFromCarPart(carPartInfoCmp.CarPartId).ExplosionDefence)) {
                    CreateExplosionEffect(gunConfig.ExplosionRadius, collisionPoint, gunConfig.EffectType);
                    var castedObjects = Physics2D.OverlapCircleAll(collisionPoint, gunConfig.ExplosionRadius, layerMask);

                    foreach (var castedObject in castedObjects) {
                        float explosionDamagePoints = gunConfig.ExplosionDamage *
                            Vector2.Distance(castedObject.transform.position, collisionPoint) / gunConfig.ExplosionRadius;

                        if (!_inventoryComponentsPool.Value.Get(_sceneService.Value.PlayerEntity).PlayerTeammateCarsEntity.Contains(carPartDamagedCmp.MainEntity) )
                            explosionDamagePoints *=
                                _currentGameComponentsPool.Value.Get(_sceneService.Value.PlayerEntity).RoundMoneyMultiplayer;
                        var carPartStuff = castedObject.GetComponent<CarPartStuffView>();
                        Vector2Int currentDamagedEntityGridPos = carPartStuff._gridPosition;
                        var currentCarCmpDamaged = _carComponentsPool.Value.Get(carPartStuff._mainEntity);

                        if (currentCarCmpDamaged.CarPartGroupsGrid == null) break;
                        int currentDamagedEntity =
                            currentCarCmpDamaged.CarPartGroupsGrid[currentDamagedEntityGridPos.x, currentDamagedEntityGridPos.y] == 0
                                ? currentCarCmpDamaged.CarPartContainerGroupsGrid[currentDamagedEntityGridPos.x, currentDamagedEntityGridPos.y]
                                : currentCarCmpDamaged.CarPartGroupsGrid[currentDamagedEntityGridPos.x, currentDamagedEntityGridPos.y];
                        if (currentDamagedEntity == 0) break;
                        var damagedCarPartInfoCmp = _carPartInfoComponentsPool.Value.Get(currentDamagedEntity);
                        if (_sceneService.Value.GetCarPartConfig(damagedCarPartInfoCmp.CarPartId).PartType == CarPartType.Armor) {
                            var armorConfig = _sceneService.Value.GetArmorFromCarPart(damagedCarPartInfoCmp.CarPartId);
                            if (GetBulletDirection(castedObject.transform, collisionPoint, armorConfig)) {
                                float defencePercent = armorConfig.DefencePercent *
                                                       GridUtils.GetArmorLevelMultipliers(damagedCarPartInfoCmp.PartLevel);

                                explosionDamagePoints *= 1f - defencePercent / (defencePercent + 100f);
                            }
                        }

                        if (_frozenComponentsPool.Value.Has(currentDamagedEntity)) {
                            damagePoints *= 1f + _frozenComponentsPool.Value.Get(currentDamagedEntity).Duration / 100;
                        }

                        _sceneService.Value.GetFloatingTextPrefab(castedObject.transform.position, explosionDamagePoints.ToString("F2"),
                            Color.white);
                        if (isPlayer)
                            DataPersistenceManagerSystem._gameData.PlayerDamage += explosionDamagePoints;
                        ChangeHealth(explosionDamagePoints, currentDamagedEntity);

                        SetEffectComponent(currentDamagedEntity, gunConfig.EffectType, gunConfig.EffectDuration);
                        //lenght from explode higher = damage worse
                    }
                }

                SetEffectComponent(damagedEntity, gunConfig.EffectType, gunConfig.EffectDuration);
            } else if (attackType == AttackType.Melee) {
                
                if(_shockEffectComponentsPool.Value.Has(weaponCellEntity))return;
                
                ref var meleeCmp = ref _meleeWeaponComponentsPool.Value.Get(weaponCellEntity);
                if (meleeCmp.Countdown > 0) return;
                meleeCmp.isTriggered = true;
                meleeCmp.Countdown += meleeCmp.CurrentMaxCountdown;
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
                var damagedCarPartCmp = _carPartInfoComponentsPool.Value.Get(damagedEntity);

                if (damagedEntityGridPosition.x >= 0 && _sceneService.Value.GetCarPartConfig(damagedCarPartCmp.CarPartId).PartType == CarPartType.Armor) {
                    var carCmpDamaged = _carComponentsPool.Value.Get(damagedMainEntity);
                    var damagedCell = carCmpDamaged.CarPartViewsGrid[damagedEntityGridPosition.x, damagedEntityGridPosition.y];
                    var armorConfig = _sceneService.Value.GetArmorFromCarPart(damagedCarPartCmp.CarPartId);
                    if (GetBulletDirection(damagedCell.transform, collisionPoint, armorConfig)) {
                        float defencePercent = armorConfig.DefencePercent * GridUtils.GetArmorLevelMultipliers(damagedCarPartCmp.PartLevel);
                        damagePoints *= 1f - defencePercent / (defencePercent + 100f);
                    }
                }

                if (_frozenComponentsPool.Value.Has(damagedEntity)) {
                    damagePoints *= 1f + _frozenComponentsPool.Value.Get(damagedEntity).Duration/100;
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
        
        private void StayInTornado(Vector2Int gridPosition, int mainEntity, int tornadoEntity) {
        
            ref var tornadoCmp = ref _tornadoComponentsPool.Value.Get(tornadoEntity);
            ref var carCmp = ref _carComponentsPool.Value.Get(mainEntity);
            if (_movementComponentsPool.Value.Has(mainEntity)) {
                var carPartEntity = carCmp.CarPartGroupsGrid[gridPosition.x, gridPosition.y] == 0
                    ? carCmp.CarPartContainerGroupsGrid[gridPosition.x, gridPosition.y]
                    : carCmp.CarPartGroupsGrid[gridPosition.x, gridPosition.y];
            
                ref var moveCmp = ref _movementComponentsPool.Value.Get(mainEntity);
                moveCmp.MovableObjectRigidbody.AddForce(100*tornadoCmp.TornadoSpeed*tornadoCmp.TornadoDirection);
                if (tornadoCmp.EffectType == EffectType.none) {
                    ChangeHealth(5f*Time.fixedDeltaTime, carPartEntity);
                } else {
                    SetEffectComponent(carPartEntity,tornadoCmp.EffectType,2f*Time.fixedDeltaTime);
                }
            }
        }

        private void ExplodeBarrel(EffectType barrelType, int barrelEntity) {
            ref var carPartCmp = ref _carPartComponentsPool.Value.Get(barrelEntity);
            var barrelPosition = carPartCmp.CellCarPartsStuffViews[0].transform.position;
            CreateExplosionEffect(4f, barrelPosition, barrelType);
            _sceneService.Value.DestroyGameobject(carPartCmp.CellCarPartsStuffViews[0].transform);
            
            CalculateExplodeDamage(barrelType,(int)barrelType == 0 ? 100 : 10,4f,barrelPosition);

            _destroyEntityEventsPool.Value.Add(barrelEntity) ;
            //circlecast and set effect or take damage
        }
        
        private void ChangeHealth(float health, int cellEntity) {
            ref var carPartCmp = ref _carPartComponentsPool.Value.Get(cellEntity);
            ref var carPartInfoCmp = ref _carPartInfoComponentsPool.Value.Get(cellEntity);
            if (carPartInfoCmp.CurrentHealth <= 0) return;
            carPartInfoCmp.CurrentHealth -= health;

            if (carPartCmp.MainEntity < 0) {
                if (carPartCmp.MainEntity == -10) {
                    if (carPartInfoCmp.CurrentHealth <= 0)
                        OpenSupportBox();
                } else if (carPartCmp.MainEntity == -11) {
                    if (carPartInfoCmp.CurrentHealth <= 0)
                        ExplodeBarrel(EffectType.none, cellEntity);
                } else if (carPartCmp.MainEntity == -12) {
                    if (carPartInfoCmp.CurrentHealth <= 0)
                        ExplodeBarrel(EffectType.fire, cellEntity);
                } else if (carPartCmp.MainEntity == -13) {
                    if (carPartInfoCmp.CurrentHealth <= 0)
                        ExplodeBarrel(EffectType.froze, cellEntity);
                }

                return;
            }
            

            var healthPercent = carPartInfoCmp.CurrentHealth / carPartCmp.MaxHealth;
            var carPartConfig = _sceneService.Value.GetCarPartConfig(carPartInfoCmp.CarPartId);
            if (carPartCmp.HealthMaterialIndex == 0 && healthPercent < 0.6f) {
                var needMaterial = _sceneService.Value._carStuffListsConfig.LowDamagedMaterial;
                carPartCmp.HealthMaterialIndex = 1;
                ChangeCarPartMaterial(cellEntity, carPartCmp, needMaterial, carPartConfig);
            }

            if (carPartCmp.HealthMaterialIndex == 1 && healthPercent < 0.15f) {
                carPartCmp.HealthMaterialIndex = 2;
                var needMaterial = _sceneService.Value._carStuffListsConfig.HighDamagedMaterial;
                ChangeCarPartMaterial(cellEntity, carPartCmp, needMaterial, carPartConfig);
            }

            var inventoryCmp = _inventoryComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);
            if (carPartCmp.MainEntity == _sceneService.Value.PlayerEntity) {
                foreach (var carPartStuff in carPartCmp.CellCarPartsStuffViews) {
                    if (carPartConfig.PartType == CarPartType.WheelsContainer || carPartConfig.PartType == CarPartType.CarStuffContainer)
                        inventoryCmp.CarPartInfoUIViewsGrid[carPartStuff._gridPosition.x, carPartStuff._gridPosition.y]
                            .SetHealthCount(_sceneService.Value._healthGradient.Evaluate(healthPercent), (int)carPartInfoCmp.CurrentHealth);
                    else {
                        if (carPartConfig.PartType == CarPartType.Main) {
                            _sceneService.Value._playerUIView._lampLightImage.DOKill();
                            _sceneService.Value._playerUIView._lampLightImage.DOColor(new Color(1, 0, 0, 0.6f), 0.45f).OnComplete(() =>
                                _sceneService.Value._playerUIView._lampLightImage?.DOColor(new Color(1, 0, 0, 0), 0.45f));
                        }

                        inventoryCmp.CarPartInfoUIViewsUpLevelGrid[carPartStuff._gridPosition.x, carPartStuff._gridPosition.y]
                            .SetHealthCount(_sceneService.Value._healthGradient.Evaluate(healthPercent), (int)carPartInfoCmp.CurrentHealth);
                    }
                }
            }

            if (carPartInfoCmp.CurrentHealth <= 0 && carPartCmp.CellCarPartsStuffViews != null) {
                carPartInfoCmp.CurrentHealth = 0;
                DestroyCarPartCell(cellEntity);

                if (carPartConfig.PartType == CarPartType.Main) {
                    if (!inventoryCmp.PlayerTeammateCarsEntity.Contains(carPartCmp.MainEntity))
                        DestroyNeedCar(carPartCmp.MainEntity);
                    else if (carPartCmp.MainEntity == _sceneService.Value.PlayerEntity)
                        LoseGame();
                }

                carPartCmp.CellCarPartsStuffViews = null;
            }
            
        }
        private string GetLocalization(string needString) => BetweenScenesService.Instance.CsvLocalization.GetLocalizedValue(needString);

        private void OpenSupportBox() {
            _sceneService.Value.PlayWarningAnimation(GetLocalization("Supplies received") + "!", true);
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
            ref var carPartCmp = ref _carPartComponentsPool.Value.Get(cellEntity);
            ref var carPartInfoCmp = ref _carPartInfoComponentsPool.Value.Get(cellEntity);
            
            if (!carPartCmp.CarPartCellRenderer || !carPartCmp.CarPartCellRenderer.gameObject.activeInHierarchy) return false;
            var carPartConfig = _sceneService.Value.GetCarPartConfig(carPartInfoCmp.CarPartId);      
            ref var inventoryCmp = ref _inventoryComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);
            bool isPlayer =carPartCmp.MainEntity == _sceneService.Value.PlayerEntity;
            bool isEnemy = !inventoryCmp.PlayerTeammateCarsEntity.Contains(carPartCmp.MainEntity);

          ref var fuelCmp = ref _fuelComponentsPool.Value.Get(carPartCmp.MainEntity);
            ClearEffectsFromCarPart(cellEntity);
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
            bool dontDestroyCarAfterMainPart = !_betweenScenesService.Value.GetMapDifficultyConfig().DestroyEnemyCarsAfterDeath && isEnemy;
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
                                .GetCarPartConfig(_carPartInfoComponentsPool.Value.Get(carCmp.CarPartGroupsGrid[gridPos.x, gridPos.y]).CarPartId)
                                .PartType == CarPartType.Main && !dontDestroyCarAfterMainPart)
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
                            .Wheels[_sceneService.Value.GetWheelContainerFromCarPart(carPartInfoCmp.CarPartId).WheelId].UpWheelSprite;
                        wheelSpriteRenderer.color = _sceneService.Value._carStuffListsConfig.DestroyedCarPartColor;
                        wheel.transform.parent = _sceneService.Value._brokenCarPartsContainer;
                    }

                    _wheelContainerComponentsPool.Value.Del(cellEntity);
                    carCmp.CurrentMaxWeight -= _sceneService.Value.GetWheelContainerFromCarPart(carPartInfoCmp.CarPartId).MaxWeight * GridUtils.GetModulesMultipliers(carPartInfoCmp.ModuleIds).maxWeightMultiplier* GridUtils.GetMaxWeightLevelMultiplier(carPartInfoCmp.PartLevel);
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
                    var supportCarPartConfig = _sceneService.Value.GetSupportModuleFromCarPart(carPartInfoCmp.CarPartId); 
                    (float firstMultiplayer,float secondMultiplayer)= GridUtils.GetSupportLevelMultipliers(carPartInfoCmp.PartLevel, supportCarPartConfig.ModuleType);
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
                if (_currentGameComponentsPool.Value.Get(_sceneService.Value.PlayerEntity).CurrentRoundType != RoundType.None)
                    CheckTasks(TaskType.BrokenParts);
            }

            if (GridUtils.IsContainerType(carPartConfig) && !destroyCarAfterCarPart && checkConnectedGroup) {
                int firstMainCarPartContainerEntity = 0;
                int secondMainCarPartContainerEntity = 0;

                if (!dontDestroyCarAfterMainPart)
                    foreach (var carPartsInfo in carCmp.CarPartGroupsList) {
                        var checkedCarPartCmp = _carPartInfoComponentsPool.Value.Get(carPartsInfo.Key);
                        if (_sceneService.Value.GetCarPartConfig(checkedCarPartCmp.CarPartId).PartType == CarPartType.Main) {
                            var cellPos = carPartsInfo.Value[0];
                            if (firstMainCarPartContainerEntity != 0)
                                secondMainCarPartContainerEntity = carCmp.CarPartContainerGroupsGrid[cellPos.x, cellPos.y];
                            else
                                firstMainCarPartContainerEntity = carCmp.CarPartContainerGroupsGrid[cellPos.x, cellPos.y];
                        }
                    }
                else {
                    foreach (var carPartInfo in carCmp.CarPartContainerGroupsList) {
                          var positionCell = carCmp.CarPartContainerGroupsList[carPartInfo.Key][0];
                                           firstMainCarPartContainerEntity = carCmp.CarPartContainerGroupsGrid[positionCell.x,positionCell.y]; 
                    }
                }

                var connectedWithMainPartContainerContainers =
                    GridUtils.FindContactedValues(carCmp.CarPartContainerGroupsGrid, firstMainCarPartContainerEntity);

                if (connectedWithMainPartContainerContainers.Count == 1 || (!dontDestroyCarAfterMainPart &&
                                                                            !connectedWithMainPartContainerContainers.Contains(
                                                                                secondMainCarPartContainerEntity))) {
                    destroyCarAfterCarPart = true;
                } else {
                    var carPartGroupListClone = new Dictionary<int, List<Vector2Int>>(carCmp.CarPartContainerGroupsList);
                    foreach (var containerInfo in carPartGroupListClone) {
                        if (cellEntity == containerInfo.Key) continue;
                        if (!connectedWithMainPartContainerContainers.Contains(containerInfo.Key) &&
                            DestroyCarPartCell(containerInfo.Key, false)) {
                            if (dontDestroyCarAfterMainPart)
                                Debug.Log("need set border");
                            return true;
                        }
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

        private void ClearEffectsFromCarPart(int cellEntity) {
            if (_fireComponentsPool.Value.Has(cellEntity)) {
                foreach (var particles in _fireComponentsPool.Value.Get(cellEntity).ParticleSystem) {
                    _sceneService.Value.ReleaseCarPartParticles(particles);
                }

                _fireComponentsPool.Value.Del(cellEntity);
            }

            if (_frozenComponentsPool.Value.Has(cellEntity)) {
                foreach (var particles in _frozenComponentsPool.Value.Get(cellEntity).ParticleSystem) {
                    _sceneService.Value.ReleaseCarPartParticles(particles);
                }

                _frozenComponentsPool.Value.Del(cellEntity);
            }

            if (_shockEffectComponentsPool.Value.Has(cellEntity)) {
                foreach (var particles in _shockEffectComponentsPool.Value.Get(cellEntity).ParticleSystem) {
                    _sceneService.Value.ReleaseCarPartParticles(particles);
                }

                _shockEffectComponentsPool.Value.Del(cellEntity);
            }
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
       /* private void DestroyBrokenParts() {
            foreach (Transform child in _sceneService.Value._brokenCarPartsContainer)
            {
                if(child.TryGetComponent<SpriteRenderer>(out var spriteRenderer))
                _sceneService.Value.DestroyGameobject(child.transform);
                else {
                    _sceneService.Value.ReleaseGunContainer(child.transform);
                }
            }
        }*/

       private void DestroyBrokenParts() {
           Transform container = _sceneService.Value._brokenCarPartsContainer;

           List<Transform> allObjects = GetAllObjectsRecursively(container);

           allObjects.Sort((a, b) => GetDepth(b).CompareTo(GetDepth(a)));

           foreach (Transform obj in allObjects) {
               if (obj.TryGetComponent(out SpriteRenderer spriteRenderer)) {
                   _sceneService.Value.ReleaseSpriteRenderer(spriteRenderer);
                   Debug.Log("Rls sr");
               } 
               else if (obj.GetComponents<Component>().Length > 1 ) {
                   _sceneService.Value.DestroyGameobject(obj);
                   Debug.Log("Des obj");
               } else {
                   _sceneService.Value.ReleaseGunContainer(obj);
                   Debug.Log("Rls obj");
               }
                   
           }
       }

       private int GetDepth(Transform obj) {
           int depth = 0;
           while (obj.parent) {
               depth++;
               obj = obj.parent;
           }

           return depth;
       }

       private List<Transform> GetAllObjectsRecursively(Transform parent, bool isContainer = true) {
           List<Transform> result = new List<Transform>();

           if (!isContainer)
               result.Add(parent);

           foreach (Transform child in parent)
               result.AddRange(GetAllObjectsRecursively(child, false));

           return result;
       }
        
        

        private void DestroyNeedCar(int carEntity) {
            ref var carCmp = ref _carComponentsPool.Value.Get(carEntity);
            if (!carCmp.CarView)return;
            carCmp.ForwardWheels = new List<Animator>();
            bool isEnemy = !_inventoryComponentsPool.Value.Get(_sceneService.Value.PlayerEntity).PlayerTeammateCarsEntity.Contains(carEntity);
            var containersList = new Dictionary<int, List<Vector2Int>>(carCmp.CarPartContainerGroupsList);
            var currentRoundType = _currentGameComponentsPool.Value.Get(_sceneService.Value.PlayerEntity).CurrentRoundType;
            var dontDestroyEnemyCar = currentRoundType != RoundType.None &&
                                      !_betweenScenesService.Value.GetMapDifficultyConfig().DestroyEnemyCarsAfterDeath && isEnemy;
            if (carCmp.CarPartContainerGroupsList.Count == 0 || !dontDestroyEnemyCar) {
                foreach (var carPartEntity in containersList) {
                    if (_carPartComponentsPool.Value.Get(carPartEntity.Key).CarPartCellRenderer)
                        DestroyCarPartCell(carPartEntity.Key, false);
                }

                var partsList = new Dictionary<int, List<Vector2Int>>(carCmp.CarPartGroupsList);
                foreach (var carPartEntity in partsList) {
                    if (_carPartComponentsPool.Value.Get(carPartEntity.Key).CarPartCellRenderer)
                        DestroyCarPartCell(carPartEntity.Key, false);
                    //fix double delete
                }

                if (carCmp.CarView && carCmp.CarView.MainCarContainer)
                    _sceneService.Value.DestroyGameobject(carCmp.CarView.MainCarContainer);
            }
            
            var inventoryCmp = _inventoryComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);
            if (currentRoundType == RoundType.None) return;
            if (dontDestroyEnemyCar && carCmp.CarPartContainerGroupsList.Count == 0 && !_destroyEntityEventsPool.Value.Has(carEntity) ) {
                _destroyEntityEventsPool.Value.Add(carEntity);
                carCmp.CarView = null;
            } else if (!dontDestroyEnemyCar && !_destroyEntityEventsPool.Value.Has(carEntity)) {
              //  if (carEntity != _sceneService.Value.PlayerEntity)
                    
                carCmp.CarView = null;
                if (!inventoryCmp.PlayerTeammateCarsEntity.Contains(carEntity)) {
                    DataPersistenceManagerSystem._gameData.BrokenCars++;
                    CheckTasks(TaskType.Kills);
                    _destroyEntityEventsPool.Value.Add(carEntity);
                }
            }
            else if (dontDestroyEnemyCar && carCmp.ShockedMainPartsCount < 5) {
                carCmp.ShockedMainPartsCount = 10;
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
                    Debug.Log("Task complete:"+taskCmp.MoneyReward+ " money");
                    _playerCurrencyComponentsPool.Value.Get(_sceneService.Value.PlayerEntity).MoneyCount += taskCmp.MoneyReward;
                    DataPersistenceManagerSystem._gameData.MoneyEarnedInGame += taskCmp.MoneyReward;
                    _taskComponentsPool.Value.Del(taskEntity);
                    DataPersistenceManagerSystem._gameData.TasksCompleted++;
                }
            }
        }
    }
