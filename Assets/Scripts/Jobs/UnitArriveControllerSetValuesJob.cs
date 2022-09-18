
using Data;
using Utils;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
using RaycastHit = Unity.Physics.RaycastHit;
using SphereCollider = Unity.Physics.SphereCollider;

namespace Systems
{
        [BurstCompile]
        public partial struct UnitControllerSetValuesJobRival : IJobEntity
        {
            public float3 TargetPos;
            public float DeltaTime;
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> ColliderCastDirections;
            
            [ReadOnly] public ComponentDataFromEntity<PhysicsCollider> ColliderData;
            [ReadOnly] public CollisionWorld CollisionWorld;
            
            
            private float3 _obstacleCheckColliderCastPosition;
            private BlittableBool _obstacleIsInTheWay;
            private float3 _obstacleCollisionPoint;
            private float3 _obstacleCollisionNormal;
            private float _closestDistance;

            public void Execute(Entity entity, ref CharacterControllerComponentData characterControllerComponentData, in PhysicsCollider collider, in Translation position, in Rotation rotation, in AIMovementData aiMovementData)
            {
                // var collisionWorld = PhysicsWorld.CollisionWorld;
                //
                // var chunkEntityData = chunk.GetNativeArray(EntityHandles);
                // var chunkCharacterControllerData = chunk.GetNativeArray(CharacterControllerHandles);
                // var chunkTranslationData = chunk.GetNativeArray(TranslationHandles);
                // var chunkRotationData = chunk.GetNativeArray(RotationHandles);
                //
                // for (int i = 0; i < chunk.Count; ++i)m
                // {
                //     var entity = chunkEntityData[i];
                //     var controller = chunkCharacterControllerData[i];
                //     var position = chunkTranslationData[i];
                //     var rotation = chunkRotationData[i];
                //     var collider = ColliderData[entity];
                //
                //     HandleChunk(ref entity, ref controller, ref position, ref rotation, ref collider, ref collisionWorld);
                //
                //     chunkTranslationData[i] = position;
                //     chunkCharacterControllerData[i] = controller;
                // }
                
                {
                    CheckForObstacles(entity, ref characterControllerComponentData, collider, position, rotation, aiMovementData);
                    
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

                    MoveToTarget(ref characterControllerComponentData, position, aiMovementData);
                }
            }
            
        private void MoveToTarget(ref CharacterControllerComponentData characterControllerComponentData, in Translation position, in AIMovementData aiMovementData)
        {
            var vectorToTarget = TargetPos - position.Value;
            //var squaredDistanceToTarget = math.lengthsq(vectorToTarget);
            vectorToTarget.y = 0;
            var normalizedVectorToTarget = math.normalize(vectorToTarget);
            characterControllerComponentData.CurrentDirection = math.lerp(characterControllerComponentData.CurrentDirection, normalizedVectorToTarget, DeltaTime * aiMovementData.RotationSpeed);
            characterControllerComponentData.CurrentMagnitude = 1;//squaredDistanceToTarget <= aiMovementData.SquaredStoppingDistance ? 0 : 1f;
            //Debug.DrawLine(position.Value, TargetPos, Color.cyan);
        }
        
        private void CheckForObstacles(Entity entity, ref CharacterControllerComponentData controller, in PhysicsCollider collider, in Translation position, in Rotation rotation, in AIMovementData aiMovementData)
        {
            var directionToTarget = controller.CurrentDirection;
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
            _closestDistance = math.INFINITY;
            for (int i = 0; i < ColliderCastDirections.Length; i++)
            {
                _obstacleCheckColliderCastPosition = position.Value + ColliderCastDirections[i] * aiMovementData.ObstacleAvoidanceDistance;
                CastCollidersAtDirections(ref entity, collider, position, rotation, aiMovementData);
            }
        }
        
        private void CastCollidersAtDirections(ref Entity entity, in PhysicsCollider collider, in Translation position, in Rotation rotation, in AIMovementData aiMovementData)
        {
            quaternion currRot = rotation.Value;
            float3 targetPos = _obstacleCheckColliderCastPosition;
            NativeList<ColliderCastHit> horizontalCollisions = PhysicsUtilities.ColliderCastAll(collider, position.Value, targetPos,  CollisionWorld, entity, CollisionFilters.ObstacleAvoidanceCollider, Allocator.Temp);

            if (horizontalCollisions.Length > 0)
            {
                NativeList<DistanceHit> horizontalDistances = PhysicsUtilities.ColliderDistanceAll(collider, aiMovementData.ObstacleAvoidanceDistance, new RigidTransform {pos = targetPos, rot = currRot}, CollisionWorld, entity, CollisionFilters.ObstacleAvoidanceCollider, Allocator.Temp);
        
                for (int i = 0; i < horizontalDistances.Length; ++i)
                {
                    if (horizontalDistances[i].Distance < _closestDistance)
                    {
                        _closestDistance = horizontalDistances[i].Distance;
                        _obstacleCollisionPoint = horizontalDistances[i].Position;
                        _obstacleCollisionNormal = horizontalDistances[i].SurfaceNormal;
                    }
                }
        
                _obstacleIsInTheWay = true;
                //Debug.DrawLine(position.Value, _obstacleCollisionPoint, Color.yellow);

                horizontalDistances.Dispose();
            }
        
            horizontalCollisions.Dispose();
        }
    }
}
