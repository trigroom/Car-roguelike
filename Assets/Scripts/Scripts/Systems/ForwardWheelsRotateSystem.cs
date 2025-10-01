using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

sealed class ForwardWheelsRotateSystem : IEcsRunSystem {        
    
        private EcsPoolInject<CarComponent> _carComponentsPool;
        
        private EcsFilterInject<Inc<CarComponent>> _carComponentsFilter;
        public void Run (IEcsSystems systems) {
          /*  foreach (var carEntity in _carComponentsFilter.Value) {
                var carCmp = _carComponentsPool.Value.Get(carEntity);
                if(carCmp.ForwardWheels == null) continue;
                var needRot = Quaternion.Euler(0, 0, carCmp.WheelsRotateAngle);
                foreach (var wheel in carCmp.ForwardWheels ) {
                    wheel.localRotation = needRot;
                }

                
            }*/
        }
    }
