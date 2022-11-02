using UnityEngine;
using Data;
using Rival;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Physics.Systems;
using Unity.Transforms;

namespace DEMO
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(FireballJob))]
    public partial class TurretShootSystem : SystemBase
    {
        private EntityQuery _query;
        private EntityQueryDesc _entityQueryDesc;
        private BuildPhysicsWorld _buildPhysicsWorld;
        private EndSimulationEntityCommandBufferSystem _ecb;


        protected override void OnCreate()
        {
            base.OnCreate();

            _buildPhysicsWorld = World.GetOrCreateSystem<BuildPhysicsWorld>();

            _entityQueryDesc = new EntityQueryDesc()
            {
                All = new[]
                {
                    ComponentType.ReadWrite<TurretData>(), //TODO change to something else the job runs foreach turret anyways
                }
            };
        }

        protected override void OnStartRunning()
        {
            _query = GetEntityQuery(_entityQueryDesc);
            _ecb = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
            this.RegisterPhysicsRuntimeSystemReadOnly();
        }

        protected override void OnUpdate()
        {
            if (_query.CalculateChunkCount() == 0)
                return;
            
            //var units = GetComponentTypeHandle<UnitTag>(true);
            var unitsQuery = GetEntityQuery(ComponentType.ReadOnly<UnitTag>());
            var allUnits = unitsQuery.ToComponentDataArray<UnitTag>(Allocator.TempJob);
            var translations = GetComponentDataFromEntity<Translation>(true);
            var characterControllers = GetComponentDataFromEntity<KinematicCharacterBody>(true);
            var allTurrets = GetComponentTypeHandle<TurretData>();
            
            var job = new TurretJob
            {
                AllUnits = allUnits,
                AllTranslationsHandle = translations,
                AllCharacterControllerData = characterControllers,
                AllTurretsHandle = allTurrets,
                DeltaTime = Time.DeltaTime,
                CollisionWorld = _buildPhysicsWorld.PhysicsWorld.CollisionWorld,
                Gravity = -Physics.gravity.y,
                ECB = _ecb.CreateCommandBuffer().AsParallelWriter()
            };

            Dependency = job.ScheduleParallel(_query, Dependency);

            Dependency.Complete();
            allUnits.Dispose();
        }
    }
}