using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

sealed class ForwardWheelsRotateSystem : IEcsRunSystem {        
    private EcsCustomInject<SceneService> _sceneService;
    
        private EcsPoolInject<CarComponent> _carComponentsPool;
        private EcsPoolInject<MovementComponent> _movementComponentsPool;
        private EcsPoolInject<WindComponent> _windComponentsPool;
        private EcsPoolInject<CurrentGameComponent> _currentGameComponentsPool;
        
        private EcsFilterInject<Inc<MovementComponent>> _movementComponentsFilter;
        public void Run (IEcsSystems systems) {
           var roundType = _currentGameComponentsPool.Value.Get(_sceneService.Value.PlayerEntity).CurrentRoundType;
           if(RoundUtilits.IsNotBattleRound(roundType) && roundType != RoundType.VersusBattle)return;
            foreach (var carEntity in _movementComponentsFilter.Value) {
                var carCmp = _carComponentsPool.Value.Get(carEntity);
                var moveCmp = _movementComponentsPool.Value.Get(carEntity);
                if(!moveCmp.MovableObjectRigidbody)continue;
                if (moveCmp.CantMoveTime <= 0 && carCmp.ShockedMainPartsCount == 0 && moveCmp.MaxMoveSpeed > 0) {
                     carCmp.CarView.SyncRigidbodyView.RequestPushForceServerRpc(
                           moveCmp.MovableObjectRigidbody.transform.up * moveCmp.CurrentMoveSpeed * carCmp.CurrentWeight / 10);
                   /*   moveCmp.MovableObjectRigidbody.AddForce(
                        moveCmp.MovableObjectRigidbody.transform.up * moveCmp.CurrentMoveSpeed * carCmp.CurrentWeight / 10, ForceMode2D.Force);*/
                }

                if (_windComponentsPool.Value.Has(_sceneService.Value.PlayerEntity)) {
                    var windCmp = _windComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);
                     carCmp.CarView.SyncRigidbodyView.RequestPushForceServerRpc(windCmp.WindDirection * windCmp.WindSpeed);
                   //  moveCmp.MovableObjectRigidbody.AddForce(windCmp.WindDirection * windCmp.WindSpeed, ForceMode2D.Force);
                }
                
                
                
                
                /*    if(carCmp.ForwardWheels == null) continue;
                   var needRot = Quaternion.Euler(0, 0, carCmp.WheelsRotateAngle);
                   foreach (var wheel in carCmp.ForwardWheels ) {
                       wheel.localRotation = needRot;
                   }

                   */
            }
        }
    }
