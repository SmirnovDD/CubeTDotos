using Data;
using Utils;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using Unity.VisualScripting;
using UnityEngine;
using CapsuleCollider = Unity.Physics.CapsuleCollider;
using Random = UnityEngine.Random;
using RaycastHit = Unity.Physics.RaycastHit;
using SphereCollider = Unity.Physics.SphereCollider;

namespace Systems
{
    /// <summary>
    /// Base controller for character movement.
    /// Is not physics-based, but uses physics to check for collisions.
    /// </summary>
    // [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))] 
    // [UpdateAfter(typeof(ExportPhysicsWorld))]
    // [UpdateBefore(typeof(EndFramePhysicsSystem))]
    [DisableAutoCreation]

    public sealed partial class CharacterControllerSystem : SystemBase
    {
        private BuildPhysicsWorld _buildPhysicsWorld;        

        private EntityQuery _characterControllerGroup;

        protected override void OnCreate()
        {
            _buildPhysicsWorld = World.GetExistingSystem<BuildPhysicsWorld>();
            
            _characterControllerGroup = GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadWrite<CharacterControllerComponentData>(),
                    ComponentType.ReadWrite<Translation>(),
                    ComponentType.ReadWrite<Rotation>(),
                    ComponentType.ReadWrite<PhysicsCollider>()
                }
            });
        }

        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            _buildPhysicsWorld.RegisterPhysicsRuntimeSystemReadOnly();
        }

        protected override void OnUpdate()
        {
            if (_characterControllerGroup.CalculateChunkCount() == 0)
            {
                return;
            }

            var entityTypeHandle = GetEntityTypeHandle();
            var colliderData = GetComponentDataFromEntity<PhysicsCollider>(true);
            var characterControllerTypeHandle = GetComponentTypeHandle<CharacterControllerComponentData>();
            var translationTypeHandle = GetComponentTypeHandle<Translation>();
            var rotationTypeHandle = GetComponentTypeHandle<Rotation>();
            
            var controllerJob = new CharacterControllerJob()
            {
                DeltaTime = Time.DeltaTime,
                PhysicsWorld = _buildPhysicsWorld.PhysicsWorld,
                EntityHandles = entityTypeHandle,
                ColliderData = colliderData,
                CharacterControllerHandles = characterControllerTypeHandle,
                TranslationHandles = translationTypeHandle,
                RotationHandles = rotationTypeHandle
            };
            
            Dependency = controllerJob.ScheduleParallel(_characterControllerGroup, Dependency);
            //Dependency.Complete();
        }

        /// <summary>
        /// The job that performs all of the logic of the character controller.
        /// </summary>
        [BurstCompile]
        private partial struct CharacterControllerJob : IJobChunk
        {
            public float DeltaTime;
            
            [ReadOnly] public PhysicsWorld PhysicsWorld;
            [ReadOnly] public EntityTypeHandle EntityHandles;
            [ReadOnly] public ComponentDataFromEntity<PhysicsCollider> ColliderData;

            public ComponentTypeHandle<CharacterControllerComponentData> CharacterControllerHandles;
            public ComponentTypeHandle<Translation> TranslationHandles;
            public ComponentTypeHandle<Rotation> RotationHandles;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var collisionWorld = PhysicsWorld.CollisionWorld;
                
                var chunkEntityData = chunk.GetNativeArray(EntityHandles);
                var chunkCharacterControllerData = chunk.GetNativeArray(CharacterControllerHandles);
                var chunkTranslationData = chunk.GetNativeArray(TranslationHandles);
                var chunkRotationData = chunk.GetNativeArray(RotationHandles);
                
                for (var i = 0; i < chunk.Count; i++)
                {
                    var entity = chunkEntityData[i];
                    var controller = chunkCharacterControllerData[i];
                    var position = chunkTranslationData[i];
                    var rotation = chunkRotationData[i];
                    var collider = ColliderData[entity];
                
                    HandleChunk(ref entity, ref controller, ref position, ref rotation, ref collider, ref collisionWorld);
                
                    chunkTranslationData[i] = position;
                    chunkCharacterControllerData[i] = controller;
                }
            }

            /// <summary>
            /// Processes a specific entity in the chunk.
            /// </summary>
            /// <param name="entity"></param>
            /// <param name="controller"></param>
            /// <param name="position"></param>
            /// <param name="rotation"></param>
            /// <param name="collider"></param>
            /// <param name="collisionWorld"></param>
            private void HandleChunk(ref Entity entity, ref CharacterControllerComponentData controller, ref Translation position, ref Rotation rotation, ref PhysicsCollider collider, ref CollisionWorld collisionWorld)
            {
                //var epsilon = new float3(0.0f, MathUtilities.Epsilon, 0.0f);
                var currPos = position.Value;// + epsilon;
                var currRot = rotation.Value;

                var gravityVelocity = controller.Gravity * DeltaTime;
                var verticalVelocity = controller.VerticalVelocity + gravityVelocity;
                var horizontalVelocity = controller.CurrentDirection * controller.CurrentMagnitude * controller.Speed * DeltaTime;
                if (controller.IsGrounded)
                {
                    if (controller.Jump)
                    {
                        verticalVelocity = controller.JumpStrength * new float3(0,1,0);
                    }
                    else
                    {
                        verticalVelocity = float3.zero;
                    }
                }
                //TODO use collectors instead of filters
                HandleHorizontalMovement(ref horizontalVelocity, ref entity, ref currPos, ref currRot, ref controller, ref collider, ref collisionWorld);
                //currPos += horizontalVelocity;
                
                HandleVerticalMovement(ref verticalVelocity, ref entity, ref currPos, ref currRot, ref controller, ref collider, ref collisionWorld);
                currPos += verticalVelocity;
                if(verticalVelocity.y < 0)
                    Debug.Log("Oopsie");
                //CorrectForCollision(ref entity, ref currPos, ref currRot, ref controller, ref collider, ref collisionWorld);
                //DetermineIfGrounded(entity, ref currPos, ref epsilon, ref controller, ref collider, ref collisionWorld);

                position.Value = currPos; // - epsilon;
            }
            
            private void CorrectForCollision(ref Entity entity, ref float3 currPos, ref quaternion currRot, ref CharacterControllerComponentData controller, ref PhysicsCollider collider, ref CollisionWorld collisionWorld)
            {
                var transform = new RigidTransform()
                {
                    pos = currPos,
                    rot = currRot
                };

                // Use a subset sphere within our collider to test against.
                // We do not use the collider itself as some intersection (such as on ramps) is ok.

                var offset = -math.normalize(controller.Gravity) * 0.1f;
                var sampleCollider = new PhysicsCollider()
                {
                    Value = SphereCollider.Create(new SphereGeometry()
                    {
                        Center = currPos + offset,
                        Radius = 0.1f
                    })
                };

                if (PhysicsUtilities.ColliderDistance(out var smallestHit, sampleCollider, 0.1f, transform, ref collisionWorld, entity, CollisionFilters.DynamicWithPhysical, null, ColliderData, Allocator.Temp))
                {
                    if (smallestHit.Distance < 0.0f)
                    {
                        currPos += math.abs(smallestHit.Distance) * smallestHit.SurfaceNormal;
                    }
                }
            }
            
            private void HandleHorizontalMovement(
                ref float3 horizontalVelocity,
                ref Entity entity,
                ref float3 currPos,
                ref quaternion currRot,
                ref CharacterControllerComponentData controller,
                ref PhysicsCollider collider,
                ref CollisionWorld collisionWorld)
            {
                if (horizontalVelocity.IsZero())
                {
                    return;
                }
                var targetPos = currPos + horizontalVelocity;
                var horizontalVelocityLength = math.length(horizontalVelocity);
                var horizontalCollisions = PhysicsUtilities.ColliderCastAll(collider, currPos, targetPos,  in collisionWorld, entity, CollisionFilters.DynamicWithPhysical, Allocator.Temp);
                
                var colliderHight = 0f;
                var colliderRadius = 0f;
                var topColliderPointY = 0f;
                var bottomColliderPointY = 0f;
                
                if (horizontalCollisions.Length > 0)
                {
                    unsafe //TODO carefull
                    {
                        var capsuleCollider = (CapsuleCollider*) collider.ColliderPtr;
                        colliderRadius = capsuleCollider->Radius;
                        var heightVector = capsuleCollider->Vertex0 - capsuleCollider->Vertex1;
                        colliderHight = heightVector.y;
                        colliderHight++; //it is 1 meter shorter by default then the height of the capsule collider
                        topColliderPointY = currPos.y + colliderHight / 2;
                        bottomColliderPointY = currPos.y - colliderHight / 2;
                    }
                }
                
                for (var i = horizontalCollisions.Length - 1; i >= 0; i--)
                {
                     var horizontalCollision = horizontalCollisions[i];
                     var horizontalCollisionPosition = horizontalCollision.Position;
                     if (horizontalCollisionPosition.y >= topColliderPointY - 0.01f || horizontalCollisionPosition.y <= bottomColliderPointY + 0.01f) //"epsilon" is too small
                     {
                         horizontalCollisions.RemoveAt(i);
                     }
                }
                
                if (horizontalCollisions.Length > 0)
                {
                    // We either have to step or slide as something is in our way.
                    horizontalCollisions.SortCollidersByDistanceWithInsertionSort();
                    
                    int? closestFractionIndex = null;
                    
                    for (var i = 0; i < horizontalCollisions.Length; i++)
                    {
                         var collisionPosition = horizontalCollisions[i].Position;
                         var checkForStepUpPos = new float3(collisionPosition.x, currPos.y + controller.MaxStep, collisionPosition.z);
                         var checkForStepCollision = PhysicsUtilities.ColliderCastAll(collider, checkForStepUpPos + new float3(0,0.01f,0), checkForStepUpPos,  in collisionWorld, entity, CollisionFilters.DynamicWithPhysicalExcludingTerrain, Allocator.Temp);
                         // if (checkForStepCollision.Length != 0) //we hit something in the position where we want to step up, so don't go there and slide instead
                         // {//the algorithm is not perfect, as it will not allow to step on several small steps close to each other if one is a bit higher then another
                         //     closestFractionIndex = i;
                         //     break;
                         // }
                         // //if the space is free we raycast down to determine the Y position of collision - the height of the step to
                         // {
                         //     var raycastStartPos = new float3(checkForStepUpPos.x, checkForStepUpPos.y - colliderHight / 2, checkForStepUpPos.z);
                         //     var raycastEndPos = new float3(raycastStartPos.x, raycastStartPos.y - controller.MaxStep, raycastStartPos.z);
                         //     PhysicsUtilities.Raycast(out var nearestHit, raycastStartPos, raycastEndPos, ref collisionWorld, entity, CollisionFilters.DynamicWithPhysicalExcludingTerrain, Allocator.Temp);
                         //     horizontalVelocity.y += (1 - nearestHit.Fraction) * controller.MaxStep;
                         // }
                        checkForStepCollision.Dispose();
                    }
                    //todo getting stuck in the wall
                    var slidingVelocity = horizontalVelocity;
                    // if (closestFractionIndex.HasValue)
                    // {
                        // var closestObstacleCollision = horizontalCollisions[closestFractionIndex.Value];
                        // var closestObstacleSurfaceNormal = closestObstacleCollision.SurfaceNormal;
                        // var closestObstacleSurfaceNormalNoY = new float3(closestObstacleSurfaceNormal.x, 0,
                        //     closestObstacleSurfaceNormal.z);
                        // var closestObstacleCollisionFraction = closestObstacleCollision.Fraction;
                        //
                        // slidingVelocity = horizontalVelocity + closestObstacleSurfaceNormalNoY *
                        //     (1 - closestObstacleCollisionFraction) * horizontalVelocityLength;
                        // var slidingTargetPosition = currPos + slidingVelocity;
                        //
                        // var horizontalSlidingCollisions = PhysicsUtilities.ColliderCastAll(collider, currPos,
                        //     slidingTargetPosition, in collisionWorld, entity,
                        //     CollisionFilters.DynamicWithPhysicalExcludingTerrain, Allocator.Temp);
                    
                        // if (horizontalSlidingCollisions.Length > 0)
                        // {
                        //     horizontalSlidingCollisions.SortCollidersByDistanceWithInsertionSort();
                        //
                        //     var collisionOnSlidingPathBlockingSlidingFraction = 1f;
                        //
                        //     for (int i = 0; i < horizontalSlidingCollisions.Length; i++)
                        //     {
                        //         var currentSlidingCollisionSurfaceNormal = horizontalSlidingCollisions[i].SurfaceNormal;
                        //
                        //         if (!currentSlidingCollisionSurfaceNormal.IsEqualTo(closestObstacleSurfaceNormalNoY))
                        //         {
                        //             var slidingVelocityNormalized = math.normalize(slidingVelocity);
                        //             var dotBetweenSurfaceNormalAndSlidingVelocity =
                        //                 math.dot(currentSlidingCollisionSurfaceNormal, slidingVelocityNormalized);
                        //             if (horizontalSlidingCollisions[i].Fraction < MathUtilities.Epsilon &&
                        //                 dotBetweenSurfaceNormalAndSlidingVelocity >= 0f)
                        //                 continue;
                        //             collisionOnSlidingPathBlockingSlidingFraction = horizontalSlidingCollisions[i].Fraction;
                        //             break;
                        //         }
                        //     }
                        //
                        //     slidingVelocity *= collisionOnSlidingPathBlockingSlidingFraction;
                        //     horizontalSlidingCollisions.Dispose();
                        // }
                    //}
                    horizontalVelocity = slidingVelocity.y < 0 ? slidingVelocity : float3.zero;
                }

                horizontalCollisions.Dispose();
            }
            
            private void HandleVerticalMovement(
                ref float3 verticalVelocity,
                ref Entity entity,
                ref float3 currPos,
                ref quaternion currRot,
                ref CharacterControllerComponentData controller,
                ref PhysicsCollider collider,
                ref CollisionWorld collisionWorld)
            {
                controller.VerticalVelocity = verticalVelocity;
                verticalVelocity *= DeltaTime;
                
                var verticalCollisions = PhysicsUtilities.ColliderCastAll(collider, currPos, currPos + verticalVelocity, in collisionWorld, entity, CollisionFilters.DynamicWithPhysical, Allocator.Temp);
                for (int i = verticalCollisions.Length - 1; i >= 0; i--)
                {
                    var verticalCollisionPosition = verticalCollisions[i].Position;
                    if (verticalCollisionPosition.y < currPos.y && verticalVelocity.y > 0)
                        verticalCollisions.RemoveAt(i);
                    else if (verticalCollisionPosition.y > currPos.y && verticalVelocity.y < 0)
                        verticalCollisions.RemoveAt(i);
                    else
                    {
                        unsafe //TODO carefull
                        {
                            var capsuleCollider = (CapsuleCollider*)collider.ColliderPtr;
                            var radius = capsuleCollider->Radius;
                            var vectorFromPointToCenter2D = verticalCollisionPosition.ToFloat2() - currPos.ToFloat2();
                            if (vectorFromPointToCenter2D.SqrMagnitude() >= (radius * radius) - MathUtilities.Epsilon) 
                                verticalCollisions.RemoveAt(i);
                        }
                    }
                }
                
                if (verticalCollisions.Length > 0)
                {
                    verticalCollisions.SortCollidersByDistanceWithInsertionSort();
                    var closestCollision = verticalCollisions[0];
                    controller.IsGrounded = closestCollision.Fraction == 0 && verticalVelocity.y <= 0; //we are falling down and are about to stop
                    verticalVelocity *= closestCollision.Fraction;
                    if (closestCollision.Position.y > currPos.y)
                        controller.VerticalVelocity = verticalVelocity; //if hitting the sealing then reset velocity
                }
                else
                {
                    controller.IsGrounded = false;
                }
                
                verticalCollisions.Dispose();
            }

            /// <summary>
            /// Determines if the character is resting on a surface.
            /// </summary>
            /// <param name="entity"></param>
            /// <param name="currPos"></param>
            /// <param name="epsilon"></param>
            /// <param name="collider"></param>
            /// <param name="collisionWorld"></param>
            /// <returns></returns>
            private static unsafe void DetermineIfGrounded(Entity entity, ref float3 currPos, ref float3 epsilon, ref CharacterControllerComponentData controller, ref PhysicsCollider collider, ref CollisionWorld collisionWorld)
            {
                var aabb = collider.ColliderPtr->CalculateAabb();
                var mod = 0.15f;

                var samplePos = currPos + new float3(0.0f, aabb.Min.y, 0.0f);
                var gravity = math.normalize(controller.Gravity);
                var offset = (gravity * 0.1f);

                var posLeft = samplePos - new float3(aabb.Extents.x * mod, 0.0f, 0.0f);
                var posRight = samplePos + new float3(aabb.Extents.x * mod, 0.0f, 0.0f);
                var posForward = samplePos + new float3(0.0f, 0.0f, aabb.Extents.z * mod);
                var posBackward = samplePos - new float3(0.0f, 0.0f, aabb.Extents.z * mod);

                controller.IsGrounded = PhysicsUtilities.Raycast(out var centerHit, samplePos, samplePos + offset, ref collisionWorld, entity, CollisionFilters.DynamicWithPhysical, Allocator.Temp) ||
                                        PhysicsUtilities.Raycast(out var leftHit, posLeft, posLeft + offset, ref collisionWorld, entity, CollisionFilters.DynamicWithPhysical, Allocator.Temp) ||
                                        PhysicsUtilities.Raycast(out var rightHit, posRight, posRight + offset, ref collisionWorld, entity, CollisionFilters.DynamicWithPhysical, Allocator.Temp) ||
                                        PhysicsUtilities.Raycast(out var forwardHit, posForward, posForward + offset, ref collisionWorld, entity, CollisionFilters.DynamicWithPhysical, Allocator.Temp) ||
                                        PhysicsUtilities.Raycast(out var backwardHit, posBackward, posBackward + offset, ref collisionWorld, entity, CollisionFilters.DynamicWithPhysical, Allocator.Temp);
            }
        }
    }
}
