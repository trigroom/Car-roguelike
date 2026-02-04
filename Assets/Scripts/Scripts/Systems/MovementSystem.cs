using System.Collections.Generic;
using DG.Tweening;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using Leopotam.EcsLite.ExtendedSystems;
using UnityEngine;

public class MovementSystem : IEcsRunSystem {
    private EcsWorldInject _world;
    private EcsCustomInject<SceneService> _sceneData;

    private EcsPoolInject<MovementComponent> _movementComponentsPool;
    private EcsPoolInject<FuelComponent> _fuelComponentsPool;
    private EcsPoolInject<CarComponent> _carComponentsPool;
    private EcsPoolInject<PlayerDeathEvent> _playerDeathEventsPool;
    private EcsPoolInject<EnemyAIComponent> _enemyAIComponentsPool;
    private EcsPoolInject<InventoryComponent> _inventoryComponentsPool;
    private EcsPoolInject<CurrentGameComponent> _currentGameComponentsPool;
    private EcsPoolInject<WindComponent> _windComponentsPool;
    private EcsPoolInject<QuicksandComponent> _quicksandComponentsPool;
    private EcsPoolInject<TornadoComponent> _tornadoComponentsPool;

    private EcsFilterInject<Inc<MovementComponent>> _movementComponentsFilter;
    private EcsFilterInject<Inc<EndRoundEvent>> _endRoundEventsFilter;
    private EcsFilterInject<Inc<TornadoComponent>> _tornadoComponentsFilter;


    public void Run(IEcsSystems systems) {
        foreach (var endRound in _endRoundEventsFilter.Value) {
            _sceneData.Value._playerUIView._fuelLampLightImage.DOKill();
            _sceneData.Value._playerUIView._fuelLampLightImage.gameObject.SetActive(false);
            
            foreach (var tornadoEntity in _tornadoComponentsFilter.Value) {
                ref var tornadoCmp = ref _tornadoComponentsPool.Value.Get(tornadoEntity);
                tornadoCmp.CounterTween.Kill();
               _sceneData.Value.DestroyGameobject(tornadoCmp.TornadoAnimator.transform);
               _tornadoComponentsPool.Value.Del(tornadoEntity);
            }
            return;
        }

        var deltaTime = Time.deltaTime;
        foreach (var tornadoEntity in _tornadoComponentsFilter.Value) {
            ref var tornadoCmp = ref _tornadoComponentsPool.Value.Get(tornadoEntity);
            tornadoCmp.LifeTime -= deltaTime;
            tornadoCmp.TimeToChangeDirection -= deltaTime;
            if (tornadoCmp.LifeTime <= 0) {
                _sceneData.Value.DestroyGameobject(tornadoCmp.TornadoAnimator.transform);
                _tornadoComponentsPool.Value.Del(tornadoEntity);
                continue;
            }

            if (tornadoCmp.LifeTime <= 1f) {
            
                tornadoCmp.TornadoAnimator.SetBool("isEnd", true);
                tornadoCmp.TornadoTargetSpeed = 0;
                tornadoCmp.CounterTween.Kill();
                tornadoCmp.CounterTween = DOTween.To(() => _tornadoComponentsPool.Value.Get(tornadoEntity).TornadoSpeed,
                        x => _tornadoComponentsPool.Value.Get(tornadoEntity).TornadoSpeed = x, tornadoCmp.TornadoTargetSpeed, 0.5f)
                    .SetEase(Ease.InOutQuad);
            }
            var tornadoPos = tornadoCmp.TornadoAnimator.transform.position;
            if (tornadoCmp.TimeToChangeDirection <= 0 || tornadoPos.x > 110 || tornadoPos.x < -110 || tornadoPos.y > 110 ||
                tornadoPos.y < -110) {
                var movementArray = _movementComponentsFilter.Value;
                var randomMovementEntity = movementArray.GetRawEntities()[Random.Range(1, movementArray.GetEntitiesCount()+1)];
                
              //  Debug.Log(" move target tornado "+ randomMovementEntity);
                var moveCmp = _movementComponentsPool.Value.Get(randomMovementEntity);
                tornadoCmp.TornadoDirection = (moveCmp.PreviousPosition - (Vector2)tornadoPos).normalized;
                tornadoCmp.TimeToChangeDirection = Random.Range(3f, 9f);

                tornadoCmp.TornadoTargetSpeed = Random.Range(3f, 6f);
                tornadoCmp.CounterTween.Kill();
                tornadoCmp.CounterTween= DOTween.To(() => _tornadoComponentsPool.Value.Get(tornadoEntity).TornadoSpeed,
                        x => _tornadoComponentsPool.Value.Get(tornadoEntity).TornadoSpeed = x, tornadoCmp.TornadoTargetSpeed, 1)
                    .SetEase(Ease.InOutQuad);
                tornadoCmp.TargetSpeedIsChanged = false;
            }
            else if (tornadoCmp.TimeToChangeDirection <= 1 && !tornadoCmp.TargetSpeedIsChanged) {
                tornadoCmp.TargetSpeedIsChanged = true;
                tornadoCmp.TornadoTargetSpeed = 0;
                tornadoCmp.CounterTween.Kill();
                tornadoCmp.CounterTween= DOTween.To(
                    () => _tornadoComponentsPool.Value.Get(tornadoEntity).TornadoSpeed,          
                    x => _tornadoComponentsPool.Value.Get(tornadoEntity).TornadoSpeed = x,   
                    tornadoCmp.TornadoTargetSpeed,               
                    1                    
                ).SetEase(Ease.InOutQuad);
            }

            tornadoCmp.TornadoAnimator.transform.position += (Vector3)tornadoCmp.TornadoDirection * tornadoCmp.TornadoSpeed * deltaTime;
        }
        
        var curGameCmp = _currentGameComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        if (Input.GetMouseButtonDown(1) && RoundUtilits.IsBattleRound(curGameCmp.CurrentRoundType)) {
            var inventoryCmp = _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
            if (inventoryCmp.PlayerTeammateCarsEntity.Count == 1) return;
            Vector3 worldPosition = _sceneData.Value.mainCamera.ScreenToWorldPoint(Input.mousePosition);
            worldPosition.z = 0;
            if (HasObstacleAtPoint(worldPosition)) return;
            var markContainer = _sceneData.Value._markForTeammatesContainer;
            // Проверяем расстояние до существующей метки
            float distanceToMarker = Vector3.Distance(markContainer.position, worldPosition);

            if (distanceToMarker <= 2f && markContainer.localScale != Vector3.zero) {
                curGameCmp.mapPointSequence.Kill();
                curGameCmp.mapPointSequence = DOTween.Sequence();
                curGameCmp.mapPointSequence.Append(markContainer.DOScale(0, 0.5f).SetEase(Ease.OutQuad));

                foreach (var carEntity in inventoryCmp.PlayerTeammateCarsEntity) {
                    if (carEntity == _sceneData.Value.PlayerEntity) continue;
                    ref var enemyAiCmp = ref _enemyAIComponentsPool.Value.Get(carEntity);
                    if (enemyAiCmp.MoveToPlayerTarget) {
                        enemyAiCmp.MoveToPlayerTarget = false;
                        enemyAiCmp.LastTargetCheckTime = 0;
                    }
                }
            } else {
                markContainer.localScale = Vector3.zero;

                var targetPosition = worldPosition;
                markContainer.position = targetPosition;
                curGameCmp.mapPointSequence.Kill();
                curGameCmp.mapPointSequence = DOTween.Sequence();

                curGameCmp.mapPointSequence.Append(markContainer.DOScale(1.2f, 0.5f));
                curGameCmp.mapPointSequence.Join(markContainer.DOMoveY(targetPosition.y + 1, 0.5f));

                curGameCmp.mapPointSequence.Append(markContainer.DOScale(1, 0.5f));
                curGameCmp.mapPointSequence.Join(markContainer.DOMoveY(targetPosition.y, 0.5f));

                curGameCmp.mapPointSequence.SetEase(Ease.OutQuad);

                foreach (var carEntity in inventoryCmp.PlayerTeammateCarsEntity) {
                    if (carEntity == _sceneData.Value.PlayerEntity) continue;
                    ref var enemyAiCmp = ref _enemyAIComponentsPool.Value.Get(carEntity);

                    enemyAiCmp.MoveToPlayerTarget = true;
                    enemyAiCmp.TargetToMove = markContainer;
                }
            }
        }

        foreach (var movementEntity in _movementComponentsFilter.Value) {
            ref var carCmp = ref _carComponentsPool.Value.Get(movementEntity);
            ref var movementCmp = ref _movementComponentsPool.Value.Get(movementEntity);
            if (movementCmp.MovableObjectRigidbody == null) continue;

            bool isPlayer = movementEntity == _sceneData.Value.PlayerEntity;
            

            if (_enemyAIComponentsPool.Value.Has(movementEntity)) {
                ref var enemyAiCmp = ref _enemyAIComponentsPool.Value.Get(movementEntity);
                if (enemyAiCmp.TargetToMove) {
                    movementCmp.TargetPosition = enemyAiCmp.TargetToMove.position;
                    if (enemyAiCmp.MoveToPlayerTarget &&
                        Vector3.Distance(enemyAiCmp.TargetToMove.position, movementCmp.MovableObjectRigidbody.position) < 3f) {
                        enemyAiCmp.MoveToPlayerTarget = false;
                        enemyAiCmp.LastTargetCheckTime = 0;
                    }
                }

                enemyAiCmp.LastCheckedTime -= deltaTime;
                enemyAiCmp.LastCollisionWithObstacleTime -= deltaTime;
                enemyAiCmp.LastTargetCheckTime -= deltaTime;
                if (enemyAiCmp.LastTargetCheckTime <= 0 && !enemyAiCmp.MoveToPlayerTarget) {
                    if (CheckTarget(enemyAiCmp.RadiusToCheckTarget, movementEntity)) {
                        enemyAiCmp.RadiusToCheckTarget = 7;
                        enemyAiCmp.LastTargetCheckTime = 5f;
                    } else {
                        enemyAiCmp.RadiusToCheckTarget += 5;
                        enemyAiCmp.LastTargetCheckTime = 0.3f;
                    }
                }

                if (enemyAiCmp.LastCheckedTime <= 0) {
                    CheckForObstacles(movementEntity);
                }

                if (!enemyAiCmp.TargetToMove) {
                    carCmp.CarView.ForwardWheelsContainer.position = carCmp.CarView.transform.position;
                    carCmp.CarView.ForwardWheelsContainer.rotation = carCmp.CarView.transform.rotation;
                    continue;
                }
            }

            ref var fuelCmp = ref _fuelComponentsPool.Value.Get(movementEntity); 
            var electricityConsumption = fuelCmp.ConstantElectricityReplenishment - fuelCmp.ConstantElectricityConsumption;
            var fuelConsumption = fuelCmp.ConstantFuelConsumption;
            if (fuelCmp.CurrentFuel > 0) {
                if (fuelCmp.CurrentElectricity < fuelCmp.MaxElectricity) {
                    fuelConsumption += fuelCmp.ConstantFuelToElectricityConsumption;
                    electricityConsumption += fuelCmp.ConstantFuelElectricityReplenishment;
                }

                fuelCmp.CurrentFuel -= fuelConsumption * deltaTime;
            }
               
           

            if ((electricityConsumption > 0 && fuelCmp.CurrentElectricity < fuelCmp.MaxElectricity) ||
                (fuelCmp.CurrentElectricity > 0 && electricityConsumption < 0)) {
                fuelCmp.CurrentElectricity += electricityConsumption * deltaTime;
                _sceneData.Value._playerUIView._batteryImage.color =
                    _sceneData.Value._healthGradient.Evaluate(fuelCmp.CurrentElectricity / fuelCmp.MaxElectricity);
                _sceneData.Value._playerUIView._batteryCount.text = $"{fuelCmp.CurrentElectricity}/{fuelCmp.MaxElectricity}";
            }
                
            var moveObjectTransform = movementCmp.MovableObjectRigidbody.transform;
            movementCmp.Velocity = ((Vector2)moveObjectTransform.position - movementCmp.PreviousPosition) / deltaTime;
            if (isPlayer)
                DataPersistenceManagerSystem._gameData.DistanceDrived +=
                    Vector2.Distance(movementCmp.PreviousPosition, moveObjectTransform.position);
            movementCmp.PreviousPosition = moveObjectTransform.position;
            if (movementCmp.CantMoveTime <= 0 && carCmp.ShockedMainPartsCount == 0 && movementCmp.MaxMoveSpeed > 0) {
                Vector3 directionToTarget = (movementCmp.TargetPosition - movementCmp.MovableObjectRigidbody.position).normalized;
                float distanceToTarget = Vector3.Distance(movementCmp.MovableObjectRigidbody.position, movementCmp.TargetPosition);
                bool isReversing = false;
                float needAngle = 0;
               
                if ((distanceToTarget > 3f || !isPlayer) && fuelCmp.CurrentFuel > 0) {
                    var needTargetRotation = Mathf.Atan2(directionToTarget.y, directionToTarget.x) * Mathf.Rad2Deg - 90f;

                    float angleToTarget = Vector3.SignedAngle(moveObjectTransform.up, directionToTarget, Vector3.forward);
                    isReversing = isPlayer && Mathf.Abs(angleToTarget) > 90f;
                    if (isReversing)
                        needTargetRotation = Mathf.Atan2(-directionToTarget.y, -directionToTarget.x) * Mathf.Rad2Deg - 90f;
                    else if (!isPlayer && _enemyAIComponentsPool.Value.Get(movementEntity).CurrentEnemyDirection != EnemyDirection.None) {
                        var aiCmp = _enemyAIComponentsPool.Value.Get(movementEntity);
                        if (aiCmp.CurrentEnemyDirection == EnemyDirection.Back) {
                            isReversing = true;
                        }

                        var needDir = aiCmp.CurrentEnemyDirection == EnemyDirection.Back
                            ? -moveObjectTransform.up.normalized
                            : (Quaternion.Euler(0, 0, 30) * moveObjectTransform.up).normalized;
                        if (aiCmp.CurrentEnemyDirection == EnemyDirection.Right)
                            needTargetRotation = Mathf.Atan2((Quaternion.Euler(0, 0, -30) * moveObjectTransform.up).normalized.y,
                                (Quaternion.Euler(0, 0, -30) * moveObjectTransform.up).normalized.x) * Mathf.Rad2Deg - 90f;
                        else if (aiCmp.CurrentEnemyDirection == EnemyDirection.Left)
                            needTargetRotation = Mathf.Atan2(needDir.y, needDir.x) * Mathf.Rad2Deg - 90f;

                        fuelCmp.UseSpeedBoost = false;
                    } else if (!isPlayer) {
                        fuelCmp.UseSpeedBoost = _enemyAIComponentsPool.Value.Get(movementEntity).LastCollisionWithObstacleTime <= 0;
                    }

                    float angleDifference = Mathf.DeltaAngle(moveObjectTransform.eulerAngles.z, needTargetRotation);
                    float rotationInput = Mathf.Clamp(angleDifference / 45f, -1f, 1f);
                    float targetAngularSpeed = rotationInput * movementCmp.RotateSpeed;

                    if (Mathf.Abs(angleDifference) > 1f) {
                        movementCmp.CurrentRotateSpeed = Mathf.MoveTowards(movementCmp.CurrentRotateSpeed, targetAngularSpeed,
                            movementCmp.RotateSpeed * Time.fixedDeltaTime);
                    } else {
                        movementCmp.CurrentRotateSpeed = Mathf.MoveTowards(movementCmp.CurrentRotateSpeed, 0f,
                            movementCmp.RotateSpeed * Time.fixedDeltaTime);
                    }

                    RotateWheels(angleToTarget, movementCmp.CurrentRotateSpeed, movementEntity);
                    SetWheelParticleSystemState(movementEntity, true);

                 //   var carConfig = _sceneData.Value.GetCarConfig(carCmp.CarId);
                    float maxSpeed = fuelCmp.UseSpeedBoost
                        ? movementCmp.MaxMoveSpeed + movementCmp.MaxBoostMoveSpeed
                        : movementCmp.MaxMoveSpeed;
                    
                    if (_quicksandComponentsPool.Value.Has(movementEntity))
                        maxSpeed *= 0.35f;
                    
                    if (!isReversing) {
                        movementCmp.CurrentMoveSpeed = Mathf.Lerp(movementCmp.CurrentMoveSpeed, maxSpeed, movementCmp.Acceleration * deltaTime);
                    } else {
                        movementCmp.CurrentMoveSpeed = Mathf.Lerp(movementCmp.CurrentMoveSpeed, -maxSpeed / 2f,
                            movementCmp.Deceleration * deltaTime);
                    }

                    UseFuel(movementEntity);
                } else {
                    SetWheelParticleSystemState(movementEntity, false);
                    movementCmp.CurrentMoveSpeed = Mathf.Lerp(movementCmp.CurrentMoveSpeed, 0f, movementCmp.Deceleration * deltaTime);
                    movementCmp.CurrentRotateSpeed = Mathf.MoveTowards(
                        movementCmp.CurrentRotateSpeed, 0f, movementCmp.RotateSpeed * Time.fixedDeltaTime);
                }

                movementCmp.MovableObjectRigidbody.angularVelocity = movementCmp.CurrentRotateSpeed;
               // if (movementCmp.MovableObjectRigidbody.linearVelocity.magnitude > movementCmp.Acceleration && movementCmp.MovableObjectRigidbody.linearVelocity.magnitude < movementCmp.Deceleration)
                //    movementCmp.MovableObjectRigidbody.AddForce(moveObjectTransform.up * movementCmp.CurrentMoveSpeed*carCmp.CurrentWeight/10, ForceMode2D.Force);
                
            } else {
                  movementCmp.CantMoveTime -= deltaTime;
                  SetWheelParticleSystemState(movementEntity, false);
            }
              

          /*  if (_windComponentsPool.Value.Has(_sceneData.Value.PlayerEntity)) {
                var windCmp = _windComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
                movementCmp.MovableObjectRigidbody.AddForce(windCmp.WindDirection * windCmp.WindSpeed, ForceMode2D.Force);
            }*/
                
            
            carCmp.CarView.ForwardWheelsContainer.position = carCmp.CarView.transform.position;
            carCmp.CarView.ForwardWheelsContainer.rotation = carCmp.CarView.transform.rotation;

            if (isPlayer)
                UpdateCameraPosition(carCmp.CarView.transform.position);
        }
    }
    private bool HasObstacleAtPoint(Vector2 point)
    {
        int obstacleLayerMask = 1 << LayerMask.NameToLayer("Obstacle");
    
        Collider2D hit = Physics2D.OverlapCircle(point, 0.1f, obstacleLayerMask);
    
        return hit != null;
    }
   
    private bool CheckTarget(float checkRadius, int entity) {
        ref var enemyAiCmp = ref _enemyAIComponentsPool.Value.Get(entity);
        ref var moveCmp = ref _movementComponentsPool.Value.Get(entity);
        
        int targetLayerMask = enemyAiCmp.IsEnemy? 1 << LayerMask.NameToLayer("UpCarPart"): 1 << LayerMask.NameToLayer("UpCarPartEnemy");
    
        // Проверяем, есть ли коллайдер Obstacle в точке
        Collider2D target = Physics2D.OverlapCircle(moveCmp.MovableObjectRigidbody.position, checkRadius, targetLayerMask);
        if (target)
            enemyAiCmp.TargetToMove = target.transform;
        return target;
    }

    private void CheckForObstacles(int entity) {
        ref var enemyAiCmp = ref _enemyAIComponentsPool.Value.Get(entity);
        enemyAiCmp.LastCollisionWithObstacleTime = 1f;
        string[] layerNames;
        if (enemyAiCmp.TargetToMove && Vector2.Distance(enemyAiCmp.TargetToMove.position, _movementComponentsPool.Value.Get(entity).PreviousPosition) < 7)
            layerNames = new[] { "Obstacle" };
        else
            layerNames = enemyAiCmp.IsEnemy
                ? new[] { "Obstacle", "UpCarPartEnemy", "CarBaseEnemy","PlayerDamageable" }
                : new[] { "Obstacle", "UpCarPart", "CarBase","PlayerDamageable" };
        var leftSide = CheckOneSideForObstacles(enemyAiCmp.CheckedEnemyLeftPoints, layerNames);
        if (leftSide < 2) {
            enemyAiCmp.CurrentEnemyDirection = EnemyDirection.Back;
            enemyAiCmp.LastCheckedTime += 0.8f;
            return;
        }

        if (leftSide < 5.1f) {
            enemyAiCmp.CurrentEnemyDirection =
                enemyAiCmp.CurrentEnemyDirection == EnemyDirection.Back ? EnemyDirection.Back : EnemyDirection.Left;
            enemyAiCmp.LastCheckedTime += 0.35f;
            return;
        }

        var rightSide = CheckOneSideForObstacles(enemyAiCmp.CheckedEnemyRightPoints, layerNames);

        if (rightSide < 2) {
            enemyAiCmp.CurrentEnemyDirection = EnemyDirection.Back;
            enemyAiCmp.LastCheckedTime += 0.8f;
        } else if (rightSide < 5.1f) {
            enemyAiCmp.CurrentEnemyDirection =
                enemyAiCmp.CurrentEnemyDirection == EnemyDirection.Back ? EnemyDirection.Back : EnemyDirection.Right;
            enemyAiCmp.LastCheckedTime += 0.35f;
        } else {
            enemyAiCmp.CurrentEnemyDirection = EnemyDirection.None;
            enemyAiCmp.LastCheckedTime += 0.15f;
            enemyAiCmp.LastCollisionWithObstacleTime = 0f;
        }
    }

    private float CheckOneSideForObstacles(List<Transform> pointsToCheck, string[] layerNames) {
        foreach (var point in pointsToCheck) {
            Vector3[] rayDirections = {
                point.up, // Прямо
             //   Quaternion.Euler(0, 0, 30) * point.up, // Под углом 30°
               // Quaternion.Euler(0, 0, -30) * point.up
            };

            foreach (Vector3 direction in rayDirections) {
                RaycastHit2D hit = Physics2D.Raycast(point.position, direction, 5,
                    LayerMask.GetMask(layerNames));

                if (hit.collider != null)
                    return Vector2.Distance(point.position, hit.point);
            }
        }

        return 6f;
    }
    private void UpdateCameraPosition(Vector2 needPosition) {
        _sceneData.Value.mainCamera.transform.position = Vector3.Slerp(_sceneData.Value.mainCamera.transform.position,
            new Vector3(needPosition.x, needPosition.y, -10), Time.deltaTime * 2);
    }
    private void UseFuel(int entity) {
        ref var fuelCmp = ref _fuelComponentsPool.Value.Get(entity);
        bool isPlayer = entity == _sceneData.Value.PlayerEntity;
        var carConfig = _sceneData.Value.GetCarConfig (_carComponentsPool.Value.Get(entity).CarId);
        float curFuelConsumption = fuelCmp.UseSpeedBoost ? fuelCmp.FuelConsumption+fuelCmp.AddedFuelConsumption : fuelCmp.FuelConsumption;
        
        fuelCmp.CurrentFuel -= curFuelConsumption * Time.deltaTime;

        if (fuelCmp.CurrentFuel <= 0) {
            if (!_playerDeathEventsPool.Value.Has(entity))
                _playerDeathEventsPool.Value.Add(entity);
            fuelCmp.CurrentFuel = 0;
            return;
        }
   
        if (!isPlayer) return;
        if (fuelCmp.CurrentFuel / fuelCmp.MaxFuel < 0.25f && !_sceneData.Value._playerUIView._fuelLampLightImage.gameObject.activeInHierarchy) {
           var fuelLampLight = _sceneData.Value._playerUIView._fuelLampLightImage;
           fuelLampLight.color = Color.clear;
           fuelLampLight.gameObject.SetActive(true);
           fuelLampLight.DOFade(1, 0.25f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InSine);
        }
                
       // _sceneData.Value._playerUIView._fuelFillImage.fillAmount = playerFuelCmp.CurrentFuel / playerFuelCmp.MaxFuel;
       _sceneData.Value._playerUIView._fuelArrowImage.transform.eulerAngles = new Vector3(0,0,-65f+130f*fuelCmp.CurrentFuel / fuelCmp.MaxFuel) ;
        _sceneData.Value._playerUIView._fuelText.text = $"{(int)fuelCmp.CurrentFuel}";
    }

    private void RotateWheels(float targetWheelAngle, float rotateSpeed, int debug) {
        if (rotateSpeed < 0)
            rotateSpeed *= -1;
        ref var carCmp = ref _carComponentsPool.Value.Get(debug);
        var wheels = carCmp.ForwardWheels;
        var gridMap = _currentGameComponentsPool.Value.Get(_sceneData.Value.PlayerEntity).MapGrid;
        foreach (var wheelParticle in  carCmp.ForwardWheelsParticles) {
            bool showParticles;
            Vector2 wheelPosition = wheelParticle.transform.position;
            if(wheelPosition.x > 100 || wheelPosition.x < -100 || wheelPosition.y > 100 || wheelPosition.y < -100 )
                showParticles = true;
            else {
                Vector2Int gridPosition = new Vector2Int(Mathf.CeilToInt((wheelPosition.x+95)/5f),Mathf.CeilToInt((wheelPosition.y+95)/5f)) ;
                showParticles = gridMap[gridPosition.x, gridPosition.y] == 0;

            }

            if (wheelParticle.isPlaying && !showParticles) 
                wheelParticle.Stop();
            else if (!wheelParticle.isPlaying && showParticles) 
                wheelParticle.Play();
        }

       
        if (wheels == null || wheels.Count > 0 && wheels[0] == null) return;
        var currentWheelAngle = wheels[0].transform.localRotation;

        targetWheelAngle = Mathf.Clamp(targetWheelAngle, -40, 40);
        var needRot = Quaternion.Euler(0, 0, targetWheelAngle);
        currentWheelAngle = Quaternion.Lerp(currentWheelAngle, needRot, rotateSpeed * Time.deltaTime);
        carCmp.WheelsRotateAngle = currentWheelAngle.eulerAngles.z;
      
      

        foreach (var wheel in wheels)
            wheel.transform.localRotation = currentWheelAngle;
    }

    private void SetWheelParticleSystemState(int entity, bool needParticles) {
        var moveCmp = _movementComponentsPool.Value.Get(entity);
        if (needParticles)
            needParticles = moveCmp.CurrentMoveSpeed > 50;
        ref var carCmp = ref _carComponentsPool.Value.Get(entity);
        var wheelsParticles = carCmp.ForwardWheelsParticles;
        foreach (var particles in wheelsParticles) {
            var main = particles.main;
            main.startSpeed = moveCmp.CurrentMoveSpeed/15;
            var emission = particles.emission;
            emission.rateOverTime = ((int)moveCmp.CurrentMoveSpeed-10)/15;
        }

        if (carCmp.isWheelParticle != needParticles) {
            carCmp.isWheelParticle = needParticles;
            foreach (var particles in wheelsParticles) {
                if (needParticles)
                    particles.Play();
                else
                    particles.Stop();
            }
        }
    }
}

