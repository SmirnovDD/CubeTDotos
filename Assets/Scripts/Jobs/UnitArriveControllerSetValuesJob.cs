
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
        public partial struct UnitArriveControllerSetValuesJob : IJobEntity
        {
            public float3 TargetPos;
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> ColliderCastDirections;
            
            [ReadOnly] public ComponentDataFromEntity<PhysicsCollider> ColliderData;
            [ReadOnly] public CollisionWorld CollisionWorld;
            
            
            private float3 _obstacleCheckColliderCastPosition;
            private BlittableBool _obstacleIsInTheWay;
            private float3 _obstacleCollisionPoint;
            private float3 _obstacleCollisionNormal;

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
                         var targetPosition = _obstacleCollisionPoint + _obstacleCollisionNormal * aiMovementData.ObstacleAvoidanceDistance;
                         Debug.DrawLine(position.Value, _obstacleCollisionPoint, Color.magenta);
                         Debug.DrawLine(position.Value + new float3(0,0.1f,0), targetPosition + new float3(0,0.1f,0), Color.green);
                         Debug.DrawLine(_obstacleCollisionPoint, _obstacleCollisionPoint + _obstacleCollisionNormal * aiMovementData.ObstacleAvoidanceDistance, Color.black);
                         Debug.DrawLine(_obstacleCollisionPoint + new float3(0, -0.1f, 0), _obstacleCollisionPoint + _obstacleCollisionNormal + new float3(0, -0.1f, 0), Color.red);
                         var controllerDirection = new float2(characterControllerComponentData.CurrentDirection.x, characterControllerComponentData.CurrentDirection.z);
                         var collisionNormal = new float2(_obstacleCollisionNormal.x, _obstacleCollisionNormal.z);
                         var angle = (float)MathUtilities.AngleBetween(controllerDirection, collisionNormal);
                         
                         if (angle > 165f)
                         {
                             var perp = new float3(-_obstacleCollisionNormal.z, _obstacleCollisionNormal.y, _obstacleCollisionNormal.x);
                             TargetPos = targetPosition + (perp * (math.sin((angle - 165f) * MathUtilities.Deg2Rad) * 2f * aiMovementData.ObstacleAvoidanceDistance));
                         }
                     }

                    MoveToTarget(ref characterControllerComponentData, position, aiMovementData);
                }
            }
            
        private void MoveToTarget(ref CharacterControllerComponentData characterControllerComponentData, in Translation position, in AIMovementData aiMovementData)
        {
            var vectorToTarget = TargetPos - position.Value;
            var squaredDistanceToTarget = math.lengthsq(vectorToTarget);
            vectorToTarget.y = 0;
            characterControllerComponentData.CurrentDirection = math.normalize(vectorToTarget);
            characterControllerComponentData.CurrentMagnitude = squaredDistanceToTarget <= aiMovementData.SquaredStoppingDistance ? 0 : 1f;
        }
        
        private void CheckForObstacles(Entity entity, ref CharacterControllerComponentData controller, in PhysicsCollider collider, in Translation position, in Rotation rotation, in AIMovementData aiMovementData)
        {
            if (MathUtilities.IsZero(controller.CurrentDirection) || controller.CurrentDirection.IsNan())
                return;
            
            var orientation = VectorToOrientation(controller.CurrentDirection);
            
            ColliderCastDirections[0] = controller.CurrentDirection;
            ColliderCastDirections[1] = OrientationToVector(orientation + 45 * MathUtilities.Deg2Rad);
            ColliderCastDirections[2] = OrientationToVector(orientation - 45 * MathUtilities.Deg2Rad);
            
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
            for (int i = 0; i < ColliderCastDirections.Length; i++)
            {
                _obstacleCheckColliderCastPosition = position.Value + ColliderCastDirections[i];
                CastCollidersAtDirections(ref entity, collider, position, rotation, aiMovementData);
                if (_obstacleIsInTheWay == true)
                    break;
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
        
                var closestCollisionIndex = 0;
                var closestDistance = math.INFINITY;
        
                for (int i = 0; i < horizontalDistances.Length; ++i)
                {
                    if (horizontalDistances[i].Distance < closestDistance)
                    {
                        closestDistance = horizontalDistances[i].Distance;
                        closestCollisionIndex = i;
                        Debug.Log($"{closestDistance}");
                    }
                }
        
                _obstacleIsInTheWay = true;
                _obstacleCollisionPoint = horizontalDistances[closestCollisionIndex].Position;
                _obstacleCollisionNormal = horizontalDistances[closestCollisionIndex].SurfaceNormal;
                Debug.DrawLine(position.Value, _obstacleCollisionPoint + _obstacleCollisionNormal, Color.blue);

                horizontalDistances.Dispose();
            }
        
            horizontalCollisions.Dispose();
        }
    }
}
