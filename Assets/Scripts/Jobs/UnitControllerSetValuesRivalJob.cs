using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using Utils;
using CapsuleCollider = Unity.Physics.CapsuleCollider;
using MathUtilities = Utils.MathUtilities;
using PhysicsUtilities = Utils.PhysicsUtilities;

namespace DEMO
{
    [BurstCompile]
    public partial struct UnitControllerSetValuesRivalJob : IJobEntity
    {
        public float3 TargetPos;
        public float DeltaTime;

        [ReadOnly] public CollisionWorld CollisionWorld;


        private float3 _obstacleCheckColliderCastPosition;
        private BlittableBool _obstacleIsInTheWay;
        private float3 _obstacleCollisionPoint;
        private float3 _obstacleCollisionNormal;
        private float3 _localTargetPos;
        
        public void Execute(Entity entity, ref ThirdPersonCharacterInputs thirdPersonCharacterInputs, in PhysicsCollider collider, in Translation position, in AIMovementData aiMovementData)
        {
            _localTargetPos = TargetPos;
            _obstacleIsInTheWay = false;
            var colliderCastDirections = new NativeArray<float3>(3, Allocator.Temp);
            CheckForObstacles(ref colliderCastDirections, in entity, in thirdPersonCharacterInputs, in collider, in position, in aiMovementData);
            
            if (_obstacleIsInTheWay)
            {
                _localTargetPos = _obstacleCollisionPoint + _obstacleCollisionNormal * aiMovementData.ObstacleAvoidanceDistance;
                //Debug.DrawLine(_obstacleCollisionPoint, _localTargetPos, Color.magenta);
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

            MoveToTarget(ref thirdPersonCharacterInputs, in position, in aiMovementData);
            colliderCastDirections.Dispose();
        }

        private void MoveToTarget(ref ThirdPersonCharacterInputs thirdPersonCharacterInputs, in Translation position, in AIMovementData aiMovementData)
        {
            var vectorToTarget = _localTargetPos - position.Value;
            var normalizedVectorToTarget = math.normalize(vectorToTarget);
            var normalizedMoveVector = math.normalize(thirdPersonCharacterInputs.MoveVector);
            thirdPersonCharacterInputs.MoveVector = normalizedMoveVector.IsNan() ? normalizedVectorToTarget : math.lerp(normalizedMoveVector, normalizedVectorToTarget, DeltaTime * aiMovementData.RotationSpeed);
            // Debug.DrawLine(position.Value, position.Value + normalizedVectorToTarget, Color.green);
            // Debug.DrawLine(position.Value, position.Value + thirdPersonCharacterInputs.MoveVector, Color.red);
            // Debug.DrawLine(position.Value, position.Value + vectorToTarget);
        }

        private void CheckForObstacles(ref NativeArray<float3> colliderCastDirections, in Entity entity, in ThirdPersonCharacterInputs thirdPersonCharacterInputs, in PhysicsCollider collider, in Translation position, in AIMovementData aiMovementData)
        {
            var directionToTarget = math.normalize(thirdPersonCharacterInputs.MoveVector);
            
            if (directionToTarget.IsZero() || directionToTarget.IsNan())
                return;
            
            var orientation = VectorToOrientation(directionToTarget);

            colliderCastDirections[0] = directionToTarget;
            colliderCastDirections[1] = OrientationToVector(orientation + 45 * MathUtilities.Deg2Rad);
            colliderCastDirections[2] = OrientationToVector(orientation - 45 * MathUtilities.Deg2Rad);
            // Debug.DrawLine(position.Value, position.Value + colliderCastDirections[0] * aiMovementData.ObstacleAvoidanceDistance, Color.grey);
            // Debug.DrawLine(position.Value, position.Value + colliderCastDirections[1] * aiMovementData.ObstacleAvoidanceDistance, Color.grey);
            // Debug.DrawLine(position.Value, position.Value + colliderCastDirections[2] * aiMovementData.ObstacleAvoidanceDistance, Color.grey);
            CheckDifferentDirections(in entity, in colliderCastDirections, in collider, in position, in aiMovementData);
        }

        private float VectorToOrientation(float3 direction)
        {
            return -1 * math.atan2(direction.z, direction.x);
        }

        private float3 OrientationToVector(float orientation)
        {
            return math.normalize(new float3(math.cos(-orientation), 0, math.sin(-orientation)));
        }

        private void CheckDifferentDirections(in Entity entity, in NativeArray<float3> colliderCastDirections, in PhysicsCollider collider, in Translation position, in AIMovementData aiMovementData)
        {
            var closestHitFraction = float.MaxValue;
            for (int i = 0; i < colliderCastDirections.Length; i++)
            {
                _obstacleCheckColliderCastPosition = position.Value + colliderCastDirections[i] * aiMovementData.ObstacleAvoidanceDistance;
                var (hitObstacle, minFraction, closestHitPosition, closestHitNormal) = CastCollidersAtDirections(in entity, in collider, position);
                if (hitObstacle && minFraction < closestHitFraction)
                {
                    _obstacleCollisionPoint = closestHitPosition;
                    _obstacleCollisionNormal = closestHitNormal;
                    _obstacleIsInTheWay = true;
                }
            }
        }

        private (bool hitObstacle, float minFraction, float3 closestHitPosition, float3 closestHitNormal)  CastCollidersAtDirections(in Entity entity, in PhysicsCollider collider, in Translation position)
        {
            float3 targetPos = _obstacleCheckColliderCastPosition;
            DemoCollectors.ObstacleAvoidanceCollector collector = new DemoCollectors.ObstacleAvoidanceCollector(1f, math.normalize(targetPos - position.Value), entity);
            unsafe
            {
                var capsuleCollider = (CapsuleCollider*) collider.ColliderPtr;
                var colliderForCheck = CapsuleCollider.Create(capsuleCollider->Geometry, CollisionFilters.ObstacleAvoidanceCollider, Material.Default);
                var closestHit = PhysicsUtilities.ColliderCastAll(in colliderForCheck, position.Value, targetPos, in CollisionWorld, ref collector);
                
                colliderForCheck.Dispose();

                if (collector.NumHits > 0)
                {
                    return (true, collector.MaxFraction, closestHit.Position, closestHit.SurfaceNormal);
                }
            }
            
            return (false, 0, float3.zero, float3.zero);
        }
    }
}
