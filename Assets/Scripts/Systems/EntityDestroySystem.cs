using Tags;
using Unity.Entities;

namespace DEMO
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class EntityDestroySystem : SystemBase
    {
        private EntityManager _entityManager;

        protected override void OnCreate()
        {
            base.OnCreate();
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        }

        protected override void OnUpdate()
        {
            Entities.WithAll<DestroyEntityTag>().WithStructuralChanges().ForEach((Entity entity) =>
            {
                _entityManager.DestroyEntity(entity);
            }).WithBurst().Run();
        }
    }
}