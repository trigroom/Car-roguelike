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

    private EcsFilterInject<Inc<MovementComponent>> _movementComponentsFilter;

    public void Init(IEcsSystems systems) {
        ref var playerFuelCmp = ref _fuelComponentsPool.Value.Add(_sceneData.Value.PlayerEntity);
        playerFuelCmp.MaxFuel = _sceneData.Value.GetCarConfig(_carComponentsPool.Value.Get(_sceneData.Value.PlayerEntity).CarId).DefaultMaxFuel;
        playerFuelCmp.CurrentFuel = playerFuelCmp.MaxFuel;
        playerFuelCmp.FuelConsumption = 3;
    }

    public void Run(IEcsSystems systems) {
        foreach (var movementEntity in _movementComponentsFilter.Value) {
            ref var movementCmp = ref _movementComponentsPool.Value.Get(movementEntity);
            if (movementCmp.MovableObjectRigidbody == null) continue;

            var deltaTime = Time.deltaTime;
            ref var carCmp = ref _carComponentsPool.Value.Get(movementEntity);
            movementCmp.Velocity = ((Vector2)movementCmp.MovableObjectRigidbody.transform.position - movementCmp.PreviousPosition) / deltaTime;
            movementCmp.PreviousPosition = movementCmp.MovableObjectRigidbody.transform.position;
            if (movementCmp.CantMoveTime <= 0 && carCmp.ShockedMainPartsCount == 0) {
                Vector3 directionToTarget = (movementCmp.TargetPosition - movementCmp.MovableObjectRigidbody.position).normalized;
                float distanceToTarget = Vector3.Distance(movementCmp.MovableObjectRigidbody.position, movementCmp.TargetPosition);
                bool isReversing = false;
                float needAngle = 0;
                if (distanceToTarget > 3f && _fuelComponentsPool.Value.Get(movementEntity).CurrentFuel > 0) {
                    var needTargetRotation = Mathf.Atan2(directionToTarget.y, directionToTarget.x) * Mathf.Rad2Deg - 90f;

                    float angleToTarget =
                        Vector3.SignedAngle(movementCmp.MovableObjectRigidbody.transform.up, directionToTarget, Vector3.forward);
                    isReversing = movementEntity == _sceneData.Value.PlayerEntity && Mathf.Abs(angleToTarget) > 90f;
                    if (isReversing)
                        needTargetRotation = Mathf.Atan2(-directionToTarget.y, -directionToTarget.x) * Mathf.Rad2Deg - 90f;
                    float angleDifference = Mathf.DeltaAngle(movementCmp.MovableObjectRigidbody.transform.eulerAngles.z, needTargetRotation);
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
                    if (!isReversing) {
                        movementCmp.CurrentMoveSpeed = Mathf.Lerp(movementCmp.CurrentMoveSpeed, movementCmp.MaxMoveSpeed,
                            movementCmp.Acceleration * deltaTime);
                        UseFuel(movementEntity);
                    } else {
                        movementCmp.CurrentMoveSpeed = Mathf.Lerp(movementCmp.CurrentMoveSpeed, -movementCmp.MaxMoveSpeed / 2f,
                            movementCmp.Deceleration * deltaTime);
                    }
                } else {
                    SetWheelParticleSystemState(movementEntity, false);
                    movementCmp.CurrentMoveSpeed = Mathf.Lerp(movementCmp.CurrentMoveSpeed, 0f, movementCmp.Deceleration * deltaTime);
                    movementCmp.CurrentRotateSpeed = Mathf.MoveTowards(
                        movementCmp.CurrentRotateSpeed, 0f, movementCmp.RotateSpeed * Time.fixedDeltaTime);
                }

                movementCmp.MovableObjectRigidbody.angularVelocity = movementCmp.CurrentRotateSpeed;
                movementCmp.MovableObjectRigidbody.linearVelocity =
                    movementCmp.MovableObjectRigidbody.transform.up * movementCmp.CurrentMoveSpeed;
            } else
                movementCmp.CantMoveTime -= deltaTime;

            carCmp.CarView.ForwardWheelsContainer.position = carCmp.CarView.transform.position;
            carCmp.CarView.ForwardWheelsContainer.rotation = carCmp.CarView.transform.rotation;

            if (movementEntity == _sceneData.Value.PlayerEntity)
                UpdateCameraPosition(carCmp.CarView.transform.position);
        }
    }

    private void UpdateCameraPosition(Vector2 needPosition) {
        _sceneData.Value.mainCamera.transform.position = Vector3.Slerp(_sceneData.Value.mainCamera.transform.position,
            new Vector3(needPosition.x, needPosition.y, _sceneData.Value.mainCamera.transform.position.z), Time.deltaTime * 2);
    }
    private void UseFuel(int entity) {
        ref var playerFuelCmp = ref _fuelComponentsPool.Value.Get(entity);
        playerFuelCmp.CurrentFuel -= playerFuelCmp.FuelConsumption * Time.deltaTime;

        if (playerFuelCmp.CurrentFuel <= 0) {
            if (!_playerDeathEventsPool.Value.Has(entity))
                _playerDeathEventsPool.Value.Add(entity);
            playerFuelCmp.CurrentFuel = 0;
            return;
        }

        if (entity != _sceneData.Value.PlayerEntity) return;
        _sceneData.Value._playerUIView._fuelFillImage.fillAmount = playerFuelCmp.CurrentFuel / playerFuelCmp.MaxFuel;
        _sceneData.Value._playerUIView._fuelText.text = $"{(int)playerFuelCmp.CurrentFuel}/{playerFuelCmp.MaxFuel}";
    }

    private void RotateWheels(float targetWheelAngle, float rotateSpeed, int debug) {
        if (rotateSpeed < 0)
            rotateSpeed *= -1;
        ref var carCmp = ref _carComponentsPool.Value.Get(debug);
        var wheels = carCmp.ForwardWheels;
        if (wheels == null || wheels.Count > 0 && wheels[0] == null) return;
        var currentWheelAngle = wheels[0].localRotation;

        targetWheelAngle = Mathf.Clamp(targetWheelAngle, -40, 40);
        var needRot = Quaternion.Euler(0, 0, targetWheelAngle);
        currentWheelAngle = Quaternion.Lerp(currentWheelAngle, needRot, rotateSpeed * Time.deltaTime);
        carCmp.WheelsRotateAngle = currentWheelAngle.eulerAngles.z;
      
      

        foreach (var wheel in wheels)
            wheel.localRotation = currentWheelAngle;
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

