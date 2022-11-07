using UnityEngine;
using Rival;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics.Systems;
using Unity.Transforms;

namespace DEMO
{
    //[DisableAutoCreation]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(TransformSystemGroup))]
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
                    ComponentType.ReadWrite<TurretData>(),
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
            
            var unitsQuery = GetEntityQuery(ComponentType.ReadOnly<UnitTag>());
            var allUnits = unitsQuery.ToComponentDataArray<UnitTag>(Allocator.TempJob);
            var translations = GetComponentDataFromEntity<Translation>(true);
            var characterControllers = GetComponentDataFromEntity<KinematicCharacterBody>(true);
            
            var job = new TurretJob
            {
                AllUnits = allUnits,
                AllTranslationsHandle = translations,
                AllCharacterControllerData = characterControllers,
                DeltaTime = Time.DeltaTime,
                //CollisionWorld = _buildPhysicsWorld.PhysicsWorld.CollisionWorld,
                Gravity = 30,
                ECB = _ecb.CreateCommandBuffer().AsParallelWriter()
            };
            
            Dependency = job.ScheduleParallel(_query, Dependency);

            Dependency.Complete();
            allUnits.Dispose();
        }
    }
}