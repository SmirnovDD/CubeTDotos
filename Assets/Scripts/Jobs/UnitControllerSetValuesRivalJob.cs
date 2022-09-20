using System.Collections;
using System.Collections.Generic;
using Data;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using Utils;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(ThirdPersonPlayerSystem))]
[BurstCompile]
public partial struct UnitControllerSetValuesRivalJob : IJobEntity
{
    public float3 TargetPos;
    public float DeltaTime;
    [NativeDisableParallelForRestriction] public NativeArray<float3> ColliderCastDirections;

    [ReadOnly] public ComponentDataFromEntity<PhysicsCollider> ColliderData;
    [ReadOnly] public CollisionWorld CollisionWorld;


    private float3 _obstacleCheckColliderCastPosition;
    private BlittableBool _obstacleIsInTheWay;
    private float3 _obstacleCollisionPoint;
    private float3 _obstacleCollisionNormal;
    private float _closesFraction;

    public void Execute(Entity entity, ref ThirdPersonCharacterInputs thirdPersonCharacterInputs, in PhysicsCollider collider, in Translation position, in Rotation rotation, in AIMovementData aiMovementData)
    {
        {
            CheckForObstacles(entity, in thirdPersonCharacterInputs, in collider, in position, in rotation, in aiMovementData);

            if (_obstacleIsInTheWay == true)
            {
                TargetPos = _obstacleCollisionPoint + _obstacleCollisionNormal * aiMovementData.ObstacleAvoidanceDistance;
                // Debug.DrawLine(position.Value, _obstacleCollisionPoint, Color.magenta);
                // Debug.DrawLine(position.Value + new float3(0,0.1f,0), targetPosition + new float3(0,0.1f,0), Color.green);
                //Debug.DrawLine(_obstacleCollisionPoint, _obstacleCollisionPoint + _obstacleCollisionNormal, Color.red);
                // var controllerDirection = new float2(characterControllerComponentData.CurrentDirection.x, characterControllerComponentData.CurrentDirection.z);
                // var collisionNormal = new float3(_obstacleCollisionNormal.x, 0, _obstacleCollisionNormal.z);
                // var collisionNormal2D = new float2(collisionNormal.x, collisionNormal.z);
                // var angle = (float)MathUtilities.AngleBetween(controllerDirection, collisionNormal2D);
                //
                // if (math.abs(angle) > 165)
                // {
                //     var perp = -math.cross(new float3(0, 1, 0), collisionNormal);
                //     //Debug.DrawLine(position.Value, position.Value + perp, Color.magenta);
                //     //Debug.Log($"{angle}");
                //     TargetPos = position.Value + perp; // * (math.sin((angle - 165f) * MathUtilities.Deg2Rad) * 2f * aiMovementData.ObstacleAvoidanceDistance));
                // }
            }

            MoveToTarget(ref thirdPersonCharacterInputs, position, aiMovementData);
        }
    }

    private void MoveToTarget(ref ThirdPersonCharacterInputs thirdPersonCharacterInputs, in Translation position, in AIMovementData aiMovementData)
    {
        var vectorToTarget = TargetPos - position.Value;
        vectorToTarget.y = 0;
        var normalizedVectorToTarget = math.normalize(vectorToTarget);
        thirdPersonCharacterInputs.MoveVector = math.lerp(thirdPersonCharacterInputs.MoveVector, normalizedVectorToTarget, DeltaTime * aiMovementData.RotationSpeed);
    }

    private void CheckForObstacles(Entity entity, in ThirdPersonCharacterInputs controller,
        in PhysicsCollider collider, in Translation position, in Rotation rotation, in AIMovementData aiMovementData)
    {
        var directionToTarget = controller.MoveVector;
        if (directionToTarget.IsZero() || directionToTarget.IsNan())
            return;

        var orientation = VectorToOrientation(directionToTarget);

        ColliderCastDirections[0] = directionToTarget;
        ColliderCastDirections[1] = OrientationToVector(orientation + 45 * MathUtilities.Deg2Rad);
        ColliderCastDirections[2] = OrientationToVector(orientation - 45 * MathUtilities.Deg2Rad);
        // Debug.DrawLine(position.Value, position.Value + ColliderCastDirections[0] * aiMovementData.ObstacleAvoidanceDistance, Color.grey);
        // Debug.DrawLine(position.Value, position.Value + ColliderCastDirections[1] * aiMovementData.ObstacleAvoidanceDistance, Color.grey);
        // Debug.DrawLine(position.Value, position.Value + ColliderCastDirections[2] * aiMovementData.ObstacleAvoidanceDistance, Color.grey);
        CheckDifferentDirections(ref entity, collider, position, rotation, aiMovementData);
    }

    private float VectorToOrientation(float3 direction)
    {
        return -1 * math.atan2(direction.z, direction.x);
    }

    private float3 OrientationToVector(float orientation)
    {
        return math.normalize(new float3(math.cos(-orientation), 0, math.sin(-orientation)));
    }

    private void CheckDifferentDirections(ref Entity entity, in PhysicsCollider collider, in Translation position, in Rotation rotation, in AIMovementData aiMovementData)
    {
        _closesFraction = math.INFINITY;
        for (int i = 0; i < ColliderCastDirections.Length; i++)
        {
            _obstacleCheckColliderCastPosition = position.Value + ColliderCastDirections[i] * aiMovementData.ObstacleAvoidanceDistance;
            CastCollidersAtDirections(ref entity, collider, position, rotation, aiMovementData);
        }
    }

    private void CastCollidersAtDirections(ref Entity entity, in PhysicsCollider collider, in Translation position, in Rotation rotation, in AIMovementData aiMovementData)
    {
        float3 targetPos = _obstacleCheckColliderCastPosition;
        ClosestHitCollector<ColliderCastHit> collector = new ClosestHitCollector<ColliderCastHit>(1f);
        var closestHit = PhysicsUtilities.ColliderCastAll(collider, position.Value, targetPos, CollisionWorld, collector);
        
        if (collector.NumHits > 0)
        {
            _closesFraction = closestHit.Fraction;
            _obstacleCollisionPoint = closestHit.Position;
            _obstacleCollisionNormal = closestHit.SurfaceNormal;
        
            _obstacleIsInTheWay = true;
        }
    }
}

