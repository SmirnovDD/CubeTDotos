using Data;
using Rival;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;

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
            
            var player = GetSingleton<PlayerTag>();
            var targetTranslation = GetComponentDataFromEntity<Translation>(true);
            _targetPosition = targetTranslation[player.Entity].Value;
            //
            // var targetsCollection = GetSingleton<TargetsCollectionData>();
            // var target = targetsCollection.Target;
            
            //
            // var entityTypeHandle = GetEntityTypeHandle();
            // var characterControllerTypeHandle = GetComponentTypeHandle<CharacterControllerComponentData>();
            // var aiMovementDataTypeHandle = GetComponentTypeHandle<AIMovementData>();
            // var translationTypeHandle = GetComponentTypeHandle<Translation>();
            // var rotationTypeHandle = GetComponentTypeHandle<Rotation>();
            //
            var job = new UnitControllerSetValuesRivalJob
            {
                TargetPos = _targetPosition,
                DeltaTime = Time.DeltaTime,
                //EntityHandles = entityTypeHandle,
                CollisionWorld = _buildPhysicsWorld.PhysicsWorld.CollisionWorld,

                // CharacterControllerHandles = characterControllerTypeHandle,
                // AIMovementDataHandles = aiMovementDataTypeHandle,
                // TranslationHandles = translationTypeHandle,
                // RotationHandles = rotationTypeHandle
            };
            //
            
            Dependency = job.ScheduleParallel(_movementQuery, Dependency);
            
            Dependency.Complete();
        }
    }
}