
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
            
            [ReadOnly] public ComponentDataFromEntity<PhysicsCollider> ColliderData;
            [ReadOnly] public CollisionWorld CollisionWorld;

            
            private float3 _obstacleCheckColliderCastPosition;
            private bool _obstacleIsInTheWay;
            private float3 _obstacleCollisionPoint;
            private float3 _obstacleCollisionNormal;
            private NativeArray<float3> _colliderCastDirections;

            public void Execute(Entity entity, ref CharacterControllerComponentData characterControllerComponentData, in PhysicsCollider collider, in Translation position, in Rotation rotation, in AIMovementData aiMovementData)
            {
                // var collisionWorld = PhysicsWorld.CollisionWorld;
                //
                // var chunkEntityData = chunk.GetNativeArray(EntityHandles);
                // var chunkCharacterControllerData = chunk.GetNativeArray(CharacterControllerHandles);
                // var chunkTranslationData = chunk.GetNativeArray(TranslationHandles);
                // var chunkRotationData = chunk.GetNativeArray(RotationHandles);
                //
                // for (int i = 0; i < chunk.Count; ++i)
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
                
                // {
                //     CheckForObstacles(entity, ref characterControllerComponentData, collider, position, rotation, aiMovementData);
                //
                //     if (!_obstacleIsInTheWay)
                //         MoveToTarget(ref characterControllerComponentData, position, aiMovementData);
                //
                //     _colliderCastDirections.Dispose();
                // }
            }
            
        //     private void MoveToTarget(ref CharacterControllerComponentData characterControllerComponentData, in Translation position, in AIMovementData aiMovementData)
        //     {
        //         var vectorToTarget = TargetPos - position.Value;
        //         var squaredDistanceToTarget = math.lengthsq(vectorToTarget);
        //         vectorToTarget.y = 0;
        //         characterControllerComponentData.CurrentDirection = math.normalize(vectorToTarget);
        //         characterControllerComponentData.CurrentMagnitude = squaredDistanceToTarget <= aiMovementData.SquaredStoppingDistance ? 0 : 1f;
        //     }
        //
        //     private void CheckForObstacles(Entity entity, ref CharacterControllerComponentData controller, in PhysicsCollider collider, in Translation position, in Rotation rotation, in AIMovementData aiMovementData)
        //     {
        //         var facingDir = math.normalize(controller.CurrentDirection);
        //         
        //         _colliderCastDirections = new NativeArray<float3>(3, Allocator.Temp);
        //         
        //         _colliderCastDirections[0] = facingDir;
        //         
        //         float orientation = VectorToOrientation(facingDir);
        //         
        //         _colliderCastDirections[1] = OrientationToVector(orientation + 45 * MathUtilities.Deg2Rad);
        //         _colliderCastDirections[2] = OrientationToVector(orientation - 45 * MathUtilities.Deg2Rad);
        //         
        //         CastCollidersAtDirections(ref entity, collider, position, rotation, aiMovementData);
        //     }
        //     
        //     private float VectorToOrientation(float3 direction)
        //     {
        //         return -1 * math.atan2(direction.z, direction.x);
        //     }
        //
        //     private float3 OrientationToVector(float orientation)
        //     {
        //         return math.normalize(new float3(math.cos(-orientation), 0, math.sin(-orientation)));
        //     }
        //     
        //     private void CastCollidersAtDirections(ref Entity entity, in PhysicsCollider collider, in Translation position, in Rotation rotation, in AIMovementData aiMovementData)
        //     {
        //         quaternion currRot = rotation.Value;
        //         float3 targetPos = _obstacleCheckColliderCastPosition;
        //
        //         NativeList<ColliderCastHit> horizontalCollisions = PhysicsUtilities.ColliderCastAll(collider, position.Value, targetPos, CollisionWorld, entity, Allocator.Temp);
        //         PhysicsUtilities.TrimByFilter(ref horizontalCollisions, ColliderData, PhysicsCollisionFilters.DynamicWithPhysical);
        //
        //         if (horizontalCollisions.Length > 0)
        //         {
        //             NativeList<DistanceHit> horizontalDistances = PhysicsUtilities.ColliderDistanceAll(collider, aiMovementData.ObstacleAvoidanceDistance,
        //                 new RigidTransform {pos = targetPos, rot = currRot}, CollisionWorld, entity, Allocator.Temp);
        //     
        //             PhysicsUtilities.TrimByFilter(ref horizontalDistances, ColliderData, PhysicsCollisionFilters.DynamicWithPhysical);
        //
        //             var closestCollisionIndex = 0;
        //             var closestDistance = 0f;
        //     
        //             for (int i = 0; i < horizontalDistances.Length; ++i)
        //             {
        //                 if (closestDistance < horizontalDistances[i].Distance)
        //                 {
        //                     closestDistance = horizontalDistances[i].Distance;
        //                     closestCollisionIndex = i;
        //                 }
        //             }
        //
        //             _obstacleIsInTheWay = true;
        //             _obstacleCollisionPoint = horizontalDistances[closestCollisionIndex].Position;
        //             _obstacleCollisionNormal = horizontalDistances[closestCollisionIndex].SurfaceNormal;
        //     
        //             horizontalDistances.Dispose();
        //         }
        //
        //         horizontalCollisions.Dispose();
        //     }
        }
    }
