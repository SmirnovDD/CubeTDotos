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
    /// <summary>
    /// Base controller for character movement.
    /// Is not physics-based, but uses physics to check for collisions.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup)), UpdateAfter(typeof(PlayerControllerSystem))]
    public sealed partial class CharacterControllerSystem : SystemBase
    {
        private BuildPhysicsWorld _buildPhysicsWorld;        

        private EntityQuery _characterControllerGroup;

        protected override void OnCreate()
        {
            _buildPhysicsWorld = World.GetOrCreateSystem<BuildPhysicsWorld>();

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
                
                for (int i = 0; i < chunk.Count; ++i)
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
                float3 epsilon = new float3(0.0f, MathUtilities.Epsilon, 0.0f) * -math.normalize(controller.Gravity);
                float3 currPos = position.Value + epsilon;
                quaternion currRot = rotation.Value;

                float3 gravityVelocity = controller.Gravity * DeltaTime;
                float3 verticalVelocity = (controller.VerticalVelocity + gravityVelocity);
                float3 horizontalVelocity = (controller.CurrentDirection * controller.CurrentMagnitude * controller.Speed * DeltaTime);
                Debug.Log($"{controller.CurrentDirection}");

                if (controller.IsGrounded)
                {
                    if (controller.Jump)
                    {
                        verticalVelocity = controller.JumpStrength * -math.normalize(controller.Gravity);
                    }
                    else
                    {
                        float3 gravityDir = math.normalize(gravityVelocity);
                        float3 verticalDir = math.normalize(verticalVelocity);

                        if (MathUtilities.FloatEquals(math.dot(gravityDir, verticalDir), 1.0f))
                        {
                            verticalVelocity = new float3();
                        }
                    }
                }

                HandleHorizontalMovement(ref horizontalVelocity, ref entity, ref currPos, ref currRot, ref controller, ref collider, ref collisionWorld);
                currPos += horizontalVelocity;

                HandleVerticalMovement(ref verticalVelocity, ref entity, ref currPos, ref currRot, ref controller, ref collider, ref collisionWorld);
                currPos += verticalVelocity;

                CorrectForCollision(ref entity, ref currPos, ref currRot, ref controller, ref collider, ref collisionWorld);
                DetermineIfGrounded(entity, ref currPos, ref epsilon, ref controller, ref collider, ref collisionWorld);

                position.Value = currPos - epsilon;
            }

            /// <summary>
            /// Performs a collision correction at the specified position.
            /// </summary>
            /// <param name="entity"></param>
            /// <param name="currPos"></param>
            /// <param name="currRot"></param>
            /// <param name="controller"></param>
            /// <param name="collider"></param>
            /// <param name="collisionWorld"></param>
            private void CorrectForCollision(ref Entity entity, ref float3 currPos, ref quaternion currRot, ref CharacterControllerComponentData controller, ref PhysicsCollider collider, ref CollisionWorld collisionWorld)
            {
                RigidTransform transform = new RigidTransform()
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

                if (PhysicsUtilities.ColliderDistance(out DistanceHit smallestHit, sampleCollider, 0.1f, transform, ref collisionWorld, entity, PhysicsCollisionFilters.DynamicWithPhysical, null, ColliderData, Allocator.Temp))
                {
                    if (smallestHit.Distance < 0.0f)
                    {
                        currPos += math.abs(smallestHit.Distance) * smallestHit.SurfaceNormal;
                    }
                }
            }

            /// <summary>
            /// Handles horizontal movement on the XZ plane.
            /// </summary>
            /// <param name="horizontalVelocity"></param>
            /// <param name="entity"></param>
            /// <param name="currPos"></param>
            /// <param name="currRot"></param>
            /// <param name="controller"></param>
            /// <param name="collider"></param>
            /// <param name="collisionWorld"></param>
            private void HandleHorizontalMovement(
                ref float3 horizontalVelocity,
                ref Entity entity,
                ref float3 currPos,
                ref quaternion currRot,
                ref CharacterControllerComponentData controller,
                ref PhysicsCollider collider,
                ref CollisionWorld collisionWorld)
            {
                if (MathUtilities.IsZero(horizontalVelocity))
                {
                    return;
                }

                float3 targetPos = currPos + horizontalVelocity;

                NativeList<ColliderCastHit> horizontalCollisions = PhysicsUtilities.ColliderCastAll(collider, currPos, targetPos,  collisionWorld, entity, Allocator.Temp);
                PhysicsUtilities.TrimByFilter(ref horizontalCollisions, ColliderData, PhysicsCollisionFilters.DynamicWithPhysical);

                if (horizontalCollisions.Length > 0)
                {
                    // We either have to step or slide as something is in our way.
                    float3 step = new float3(0.0f, controller.MaxStep, 0.0f);
                    PhysicsUtilities.ColliderCast(out ColliderCastHit nearestStepHit, collider, targetPos + step, targetPos, ref collisionWorld, entity, PhysicsCollisionFilters.DynamicWithPhysical, null, ColliderData, Allocator.Temp);

                    if (!MathUtilities.IsZero(nearestStepHit.Fraction))
                    {
                        // We can step up.
                        targetPos += (step * (1.0f - nearestStepHit.Fraction));
                        horizontalVelocity = targetPos - currPos;
                    }
                    else
                    {
                        // We can not step up, so slide.
                        NativeList<DistanceHit> horizontalDistances = PhysicsUtilities.ColliderDistanceAll(collider, 1.0f, new RigidTransform() { pos = currPos + horizontalVelocity, rot = currRot },  collisionWorld, entity, Allocator.Temp);
                        PhysicsUtilities.TrimByFilter(ref horizontalDistances, ColliderData, PhysicsCollisionFilters.DynamicWithPhysical);

                        for (int i = 0; i < horizontalDistances.Length; ++i)
                        {
                            if (horizontalDistances[i].Distance >= 0.0f)
                            {
                                continue;
                            }

                            horizontalVelocity += (horizontalDistances[i].SurfaceNormal * -horizontalDistances[i].Distance);
                        }

                        horizontalDistances.Dispose();
                    }
                }

                horizontalCollisions.Dispose();
            }

            /// <summary>
            /// Handles vertical movement from gravity and jumping.
            /// </summary>
            /// <param name="entity"></param>
            /// <param name="currPos"></param>
            /// <param name="currRot"></param>
            /// <param name="controller"></param>
            /// <param name="collider"></param>
            /// <param name="collisionWorld"></param>
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

                if (MathUtilities.IsZero(verticalVelocity))
                {
                    return;
                }

                verticalVelocity *= DeltaTime;

                NativeList<ColliderCastHit> verticalCollisions = PhysicsUtilities.ColliderCastAll(collider, currPos, currPos + verticalVelocity,  collisionWorld, entity, Allocator.Temp);
                PhysicsUtilities.TrimByFilter(ref verticalCollisions, ColliderData, PhysicsCollisionFilters.DynamicWithPhysical);

                if (verticalCollisions.Length > 0)
                {
                    RigidTransform transform = new RigidTransform()
                    {
                        pos = currPos + verticalVelocity,
                        rot = currRot
                    };

                    if (PhysicsUtilities.ColliderDistance(out DistanceHit verticalPenetration, collider, 1.0f, transform, ref collisionWorld, entity, PhysicsCollisionFilters.DynamicWithPhysical, null, ColliderData, Allocator.Temp))
                    {
                        if (verticalPenetration.Distance < -0.01f)
                        {
                            verticalVelocity += (verticalPenetration.SurfaceNormal * verticalPenetration.Distance);

                            if (PhysicsUtilities.ColliderCast(out ColliderCastHit adjustedHit, collider, currPos, currPos + verticalVelocity, ref collisionWorld, entity, PhysicsCollisionFilters.DynamicWithPhysical, null, ColliderData, Allocator.Temp))
                            {
                                verticalVelocity *= adjustedHit.Fraction;
                            }
                        }
                    }
                }

                verticalVelocity = MathUtilities.ZeroOut(verticalVelocity, 0.0001f);
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
                float mod = 0.15f;

                float3 samplePos = currPos + new float3(0.0f, aabb.Min.y, 0.0f);
                float3 gravity = math.normalize(controller.Gravity);
                float3 offset = (gravity * 0.1f);

                float3 posLeft = samplePos - new float3(aabb.Extents.x * mod, 0.0f, 0.0f);
                float3 posRight = samplePos + new float3(aabb.Extents.x * mod, 0.0f, 0.0f);
                float3 posForward = samplePos + new float3(0.0f, 0.0f, aabb.Extents.z * mod);
                float3 posBackward = samplePos - new float3(0.0f, 0.0f, aabb.Extents.z * mod);

                controller.IsGrounded = PhysicsUtilities.Raycast(out RaycastHit centerHit, samplePos, samplePos + offset, ref collisionWorld, entity, PhysicsCollisionFilters.DynamicWithPhysical, Allocator.Temp) ||
                                        PhysicsUtilities.Raycast(out RaycastHit leftHit, posLeft, posLeft + offset, ref collisionWorld, entity, PhysicsCollisionFilters.DynamicWithPhysical, Allocator.Temp) ||
                                        PhysicsUtilities.Raycast(out RaycastHit rightHit, posRight, posRight + offset, ref collisionWorld, entity, PhysicsCollisionFilters.DynamicWithPhysical, Allocator.Temp) ||
                                        PhysicsUtilities.Raycast(out RaycastHit forwardHit, posForward, posForward + offset, ref collisionWorld, entity, PhysicsCollisionFilters.DynamicWithPhysical, Allocator.Temp) ||
                                        PhysicsUtilities.Raycast(out RaycastHit backwardHit, posBackward, posBackward + offset, ref collisionWorld, entity, PhysicsCollisionFilters.DynamicWithPhysical, Allocator.Temp);
            }
        }
    }
}
