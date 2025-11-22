using System.Collections.Generic;
using DG.Tweening;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using Leopotam.EcsLite.ExtendedSystems;
using UnityEngine;

public class MovementSystem : IEcsRunSystem, IEcsInitSystem {
    private EcsWorldInject _world;
    private EcsCustomInject<SceneService> _sceneData;

    private EcsPoolInject<MovementComponent> _movementComponentsPool;
    private EcsPoolInject<FuelComponent> _fuelComponentsPool;
    private EcsPoolInject<CarComponent> _carComponentsPool;
    private EcsPoolInject<PlayerDeathEvent> _playerDeathEventsPool;
    private EcsPoolInject<EnemyAIComponent> _enemyAIComponentsPool;

    private EcsFilterInject<Inc<MovementComponent>> _movementComponentsFilter;
    private EcsFilterInject<Inc<EndRoundEvent>> _endRoundEventsFilter;

    public void Init(IEcsSystems systems) {
        ref var playerFuelCmp = ref _fuelComponentsPool.Value.Add(_sceneData.Value.PlayerEntity);
        playerFuelCmp.MaxFuel = _sceneData.Value.GetCarConfig(_carComponentsPool.Value.Get(_sceneData.Value.PlayerEntity).CarId).DefaultMaxFuel;
        playerFuelCmp.CurrentFuel = playerFuelCmp.MaxFuel;
        playerFuelCmp.FuelConsumption = 3;
    }

    public void Run(IEcsSystems systems) {
        foreach (var endRound in _endRoundEventsFilter.Value) {
            _sceneData.Value._playerUIView._fuelLampLightImage.DOKill();
            _sceneData.Value._playerUIView._fuelLampLightImage.gameObject.SetActive(false);
        }

        foreach (var movementEntity in _movementComponentsFilter.Value) {
            ref var carCmp = ref _carComponentsPool.Value.Get(movementEntity);
            ref var movementCmp = ref _movementComponentsPool.Value.Get(movementEntity);
            if (movementCmp.MovableObjectRigidbody == null) continue;

            bool isPlayer = movementEntity == _sceneData.Value.PlayerEntity;
            var deltaTime = Time.deltaTime;

            if (!isPlayer) {
                ref var enemyAiCmp = ref _enemyAIComponentsPool.Value.Get(movementEntity);
                if (enemyAiCmp.TargetToMove)
                    movementCmp.TargetPosition = enemyAiCmp.TargetToMove.position;
                enemyAiCmp.LastCheckedTime -= deltaTime;
                enemyAiCmp.LastCollisionWithObstacleTime -= deltaTime;
                enemyAiCmp.LastTargetCheckTime -= deltaTime;
                if (enemyAiCmp.LastTargetCheckTime <= 0) {
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

            var moveObjectTransform = movementCmp.MovableObjectRigidbody.transform;
            movementCmp.Velocity = ((Vector2)moveObjectTransform.position - movementCmp.PreviousPosition) / deltaTime;
            movementCmp.PreviousPosition = moveObjectTransform.position;
            if (movementCmp.CantMoveTime <= 0 && carCmp.ShockedMainPartsCount == 0) {
                Vector3 directionToTarget = (movementCmp.TargetPosition - movementCmp.MovableObjectRigidbody.position).normalized;
                float distanceToTarget = Vector3.Distance(movementCmp.MovableObjectRigidbody.position, movementCmp.TargetPosition);
                bool isReversing = false;
                float needAngle = 0;
                ref var fuelCmp = ref _fuelComponentsPool.Value.Get(movementEntity);
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

                    var carConfig = _sceneData.Value.GetCarConfig(carCmp.CarId);
                    float maxSpeed = fuelCmp.UseSpeedBoost
                        ? movementCmp.MaxMoveSpeed + movementCmp.MaxBoostMoveSpeed
                        : movementCmp.MaxMoveSpeed;

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

                movementCmp.MovableObjectRigidbody.linearVelocity = moveObjectTransform.up * movementCmp.CurrentMoveSpeed;
            } else
                movementCmp.CantMoveTime -= deltaTime;

            carCmp.CarView.ForwardWheelsContainer.position = carCmp.CarView.transform.position;
            carCmp.CarView.ForwardWheelsContainer.rotation = carCmp.CarView.transform.rotation;

            if (isPlayer)
                UpdateCameraPosition(carCmp.CarView.transform.position);
        }
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
        var layerNames = enemyAiCmp.IsEnemy
            ? new[] { "Obstacle", "UpCarPartEnemy", "CarBaseEnemy" }
            : new[] { "Obstacle", "UpCarPart", "CarBase" };
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
        float curFuelConsumption = fuelCmp.UseSpeedBoost ? fuelCmp.FuelConsumption+carConfig.AccelerationFuelСonsumption : fuelCmp.FuelConsumption;
        
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
            needParticles = moveCmp.CurrentMoveSpeed > 4;
        ref var carCmp = ref _carComponentsPool.Value.Get(entity);
        var wheelsParticles = carCmp.ForwardWheelsParticles;
        foreach (var particles in wheelsParticles) {
            var main = particles.main;
            main.startSpeed = moveCmp.CurrentMoveSpeed;
            var emission = particles.emission;
            emission.rateOverTime = (int)moveCmp.CurrentMoveSpeed-3;
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

