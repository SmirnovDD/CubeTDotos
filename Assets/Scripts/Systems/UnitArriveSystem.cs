using Data;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;

namespace Systems
{
    // [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    // [UpdateAfter(typeof(StepPhysicsWorld))]
    // 
    [DisableAutoCreation]
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
                    ComponentType.ReadWrite<CharacterControllerComponentData>(),
                    ComponentType.ReadWrite<PhysicsCollider>(),
                    ComponentType.ReadOnly<UnitArriveTag>(),
                    ComponentType.ReadOnly<Translation>(),
                    ComponentType.ReadOnly<Rotation>(),
                    ComponentType.ReadOnly<AIMovementData>()
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
            //
            var target = GetSingleton<TargetsCollectionData>().Target;
            var targetTranslation = GetComponentDataFromEntity<Translation>(true);
            _targetPosition = targetTranslation[target].Value;
            //
            // var entityTypeHandle = GetEntityTypeHandle();
            var colliderData = GetComponentDataFromEntity<PhysicsCollider>();
            // var characterControllerTypeHandle = GetComponentTypeHandle<CharacterControllerComponentData>();
            // var aiMovementDataTypeHandle = GetComponentTypeHandle<AIMovementData>();
            // var translationTypeHandle = GetComponentTypeHandle<Translation>();
            // var rotationTypeHandle = GetComponentTypeHandle<Rotation>();
            //
            var job = new UnitArriveControllerSetValuesJob
            {
                TargetPos = _targetPosition,
                DeltaTime = Time.DeltaTime,
                //EntityHandles = entityTypeHandle,
                ColliderData = colliderData,
                CollisionWorld = _buildPhysicsWorld.PhysicsWorld.CollisionWorld,
                ColliderCastDirections = new NativeArray<float3>(3, Allocator.TempJob)

                // CharacterControllerHandles = characterControllerTypeHandle,
                // AIMovementDataHandles = aiMovementDataTypeHandle,
                // TranslationHandles = translationTypeHandle,
                // RotationHandles = rotationTypeHandle
            };
            //
            
            Dependency = job.ScheduleParallel(_movementQuery, Dependency);
            
            Dependency.Complete();

            job.ColliderCastDirections.Dispose();
        }
    }
}