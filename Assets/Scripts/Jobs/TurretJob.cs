using Rival;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Utils;
using MathUtilities = Utils.MathUtilities;

namespace DEMO
{
    [BurstCompile]
    public partial struct TurretJob : IJobEntity
    {
        [ReadOnly] public NativeArray<UnitTag> AllUnits;
        [ReadOnly] public ComponentDataFromEntity<Translation> AllTranslationsHandle;

        [ReadOnly] public ComponentDataFromEntity<KinematicCharacterBody> AllCharacterControllerData;

        //[ReadOnly] public CollisionWorld CollisionWorld;
        public EntityCommandBuffer.ParallelWriter ECB;
        public float DeltaTime;
        public float Gravity;


        public void Execute(ref TurretData turretData, in Translation translation)
        {
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
                var squaredDistanceToUnit = (unitTranslation.Value - translation.Value).SqrMagnitude();

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

                var muzzlePosition = translation.Value;
                var ammoVelocity = turretData.ProjectileVelocity;

                var targetVelocity = AllCharacterControllerData[closestUnitTag.Unit].RelativeVelocity;

                BallisticShootingMath.SolveBallisticArc(muzzlePosition, ammoVelocity, closestUnitTranslation.Value,
                    Gravity, out targetDirLow, out targetDirHigh, out shootAngle);
                
                var ammo = ECB.Instantiate(0, turretData.Ammo);

                float3 targetPredictedPos = BallisticShootingMath.ApproximateTargetPositionBallisticSimple(
                    muzzlePosition, ammoVelocity, shootAngle, closestUnitTranslation.Value, targetVelocity);

                BallisticShootingMath.SolveBallisticArc(muzzlePosition, ammoVelocity, targetPredictedPos,
                    Gravity, out targetDirLow, out targetDirHigh, out shootAngle);
                
                var initialRotation = quaternion.LookRotationSafe(targetDirLow, math.up());

                var rot = initialRotation;
                var pos = muzzlePosition;
                for (int i = 0; i < 30; i++)
                {
                    float3 forwardVector = MathUtilities.GetForwardVectorFromRotation(rot);
                    float3 curPos = pos;
                    float3 newPos = curPos + (forwardVector * (ammoVelocity * DeltaTime)) +
                                    math.up() * (-Gravity * (DeltaTime * DeltaTime));
                    float3 newForwardVector = newPos - curPos;
                    Debug.DrawLine(newPos, newPos + newForwardVector, Color.blue, 1f);
                    pos = newPos;
                    rot = quaternion.LookRotationSafe(newForwardVector, math.up());
                }

                var ammoPositionComponent = new Translation {Value = muzzlePosition};
                var ammoRotation = quaternion.LookRotationSafe(targetDirLow, math.up());
                var ammoRotationComponent = new Rotation {Value = ammoRotation};
                ECB.SetComponent(0, ammo, ammoPositionComponent);
                ECB.SetComponent(0, ammo, ammoRotationComponent);

                turretData.ShotCooldown = turretData.ReloadTime;
            }
        }
    }
}