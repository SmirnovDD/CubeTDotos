using Data;
using Rival;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using Utils;

namespace DEMO
{
    [BurstCompile]
    public partial struct TurretJob : IJobEntityBatch
    {
        [ReadOnly] public NativeArray<UnitTag> AllUnits;
        [ReadOnly] public ComponentDataFromEntity<Translation> AllTranslationsHandle;
        [ReadOnly] public ComponentDataFromEntity<KinematicCharacterBody> AllCharacterControllerData;
        [ReadOnly] public ComponentTypeHandle<TurretData> AllTurretsHandle;
        [ReadOnly] public CollisionWorld CollisionWorld;
        public EntityCommandBuffer.ParallelWriter  ECB;
        public float DeltaTime;
        public float Gravity;


        public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
        {
            var allTurrets = batchInChunk.GetNativeArray(AllTurretsHandle);
             
            for (var i = 0; i < allTurrets.Length; i++)
            {
                var turretData = allTurrets[i];
                 
                var turretTranslation = AllTranslationsHandle[turretData.Turret];
            
                if (turretData.ShotCooldown > 0)
                {
                    turretData.ShotCooldown -= DeltaTime;
                    return;
                }
            
                var closestSquaredDistanceToUnit = float.MaxValue;
                Translation closestUnitTranslation = new Translation();
                UnitTag closestUnitTag = new UnitTag();
            
                for (var j = 0; j < AllUnits.Length; j++)
                {
                    var unitTag = AllUnits[j];
                    var unitTranslation = AllTranslationsHandle[unitTag.Unit];
                    var squaredDistanceToUnit = (unitTranslation.Value - turretTranslation.Value).SqrMagnitude();
            
                    if (squaredDistanceToUnit > turretData.SquaredShootDistance)
                        continue;
            
                    //if raycasting to target hits wall continue (use exitEarly collector with static filter)
            
                    if (squaredDistanceToUnit < closestSquaredDistanceToUnit)
                    {
                        closestSquaredDistanceToUnit = squaredDistanceToUnit;
                        closestUnitTranslation = unitTranslation;
                        closestUnitTag = unitTag;
                    }
                }
            
                if (!closestSquaredDistanceToUnit.IsApproximately(float.MaxValue))
                {
                    //shoot
                    float3 targetDirLow;
                    float3 targetDirHigh;
                    float shootAngle;
            
                    var muzzlePosition = turretTranslation.Value;
                    var ammoVelocity = turretData.ProjectileVelocity;
            
                    var targetVelocity = AllCharacterControllerData[closestUnitTag.Unit].RelativeVelocity;
            
                    BallisticShootingMath.SolveBallisticArc(muzzlePosition, ammoVelocity, closestUnitTranslation.Value,
                        Gravity, out targetDirLow, out targetDirHigh, out shootAngle);
                    var ammo = ECB.Instantiate(0, turretData.Ammo);
            
                    float3 targetPredictedPos = BallisticShootingMath.ApproximateTargetPositionBallisticSimple(
                        muzzlePosition, ammoVelocity, shootAngle, closestUnitTranslation.Value, targetVelocity);
            
                    BallisticShootingMath.SolveBallisticArc(muzzlePosition, ammoVelocity, targetPredictedPos, Gravity,
                        out targetDirLow, out targetDirHigh, out shootAngle);
            
                    var ammoPositionComponent = new Translation {Value = muzzlePosition};
                    var ammoRotation = quaternion.LookRotation(targetDirLow, math.up());
                    var ammoRotationComponent = new Rotation {Value = ammoRotation};
                    ECB.SetComponent(0, ammo, ammoPositionComponent);
                    ECB.SetComponent(0, ammo, ammoRotationComponent);
            
                    turretData.ShotCooldown = turretData.ReloadTime;
                }
            }

            allTurrets.Dispose();
        }
    }
}