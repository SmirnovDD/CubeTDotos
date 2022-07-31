using Data;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Systems
{
    /// <summary>
    /// Main control system for player input.
    /// </summary>
    public partial class UnitArriveSystem : SystemBase
    {
        private EntityQuery _movementQuery;
        private EntityQueryDesc _entityQueryDesc;
        private float3 _targetPosition;

        protected override void OnCreate()
        {
            base.OnCreate();
            
            _entityQueryDesc = new EntityQueryDesc()
            {
                All = new[]
                {
                    ComponentType.ReadWrite<CharacterControllerComponentData>(),
                    ComponentType.ReadOnly<UnitArriveTag>(),
                    ComponentType.ReadOnly<Translation>(),
                    ComponentType.ReadOnly<AIMovementData>()
                }
            };
        }

        protected override void OnStartRunning()
        {
            _movementQuery = GetEntityQuery(_entityQueryDesc);
        }
        
        protected override void OnUpdate()
        {
            var target = GetSingleton<TargetsCollectionData>().Target;
            var targetTranslation = GetComponentDataFromEntity<Translation>(true);
            _targetPosition = targetTranslation[target].Value;
            var job = new UnitArriveControllerSetValuesJob {TargetPos = _targetPosition};
            Dependency = job.ScheduleParallel(_movementQuery, Dependency);
        }
    }
}