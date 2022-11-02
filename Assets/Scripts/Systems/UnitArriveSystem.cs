using Data;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.Systems;
using Unity.Transforms;

namespace DEMO
{
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    [UpdateBefore(typeof(ThirdPersonCharacterMovementSystem))]
    //[DisableAutoCreation]
    public partial class UnitArriveSystem : SystemBase
    {
        private EntityQuery _movementQuery;
        private EntityQueryDesc _entityQueryDesc;
        private float3 _targetPosition;
        private BuildPhysicsWorld _buildPhysicsWorld;

        protected override void OnCreate()
        {
            base.OnCreate();
            
            _buildPhysicsWorld = World.GetOrCreateSystem<BuildPhysicsWorld>();

            _entityQueryDesc = new EntityQueryDesc()
            {
                All = new[]
                {
                    ComponentType.ReadWrite<ThirdPersonCharacterInputs>(),
                    ComponentType.ReadOnly<UnitArriveTag>(),
                    ComponentType.ReadOnly<Translation>(),
                    ComponentType.ReadOnly<AIMovementData>(),
                }
            };
        }

        protected override void OnStartRunning()
        {
            _movementQuery = GetEntityQuery(_entityQueryDesc);
            this.RegisterPhysicsRuntimeSystemReadOnly();
        }
        
        protected override void OnUpdate()
        {
            if (_movementQuery.CalculateChunkCount() == 0)
                return;
            
            var targetsCollection = GetSingleton<TargetsCollectionData>();
            
            if (targetsCollection.Target == default)
                return;
            
            var targetTranslation = GetComponentDataFromEntity<Translation>(true);
            _targetPosition = targetTranslation[targetsCollection.Target].Value;
            
            var job = new UnitControllerSetValuesRivalJob
            {
                TargetPos = _targetPosition,
                DeltaTime = Time.DeltaTime,
                CollisionWorld = _buildPhysicsWorld.PhysicsWorld.CollisionWorld,
            };

            Dependency = job.ScheduleParallel(_movementQuery, Dependency);
            
            Dependency.Complete();
        }
    }
}