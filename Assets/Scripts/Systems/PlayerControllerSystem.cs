using Data;
using Unity.Entities;
using UnityEngine;
using Utils;

namespace Systems
{
    /// <summary>
    /// Main control system for player input.
    /// </summary>
    public partial class PlayerControllerSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            Entities.WithAll<PlayerControllerComponentData>().ForEach((
                Entity entity,
                ref CameraFollowComponentData camera,
                ref CharacterControllerComponentData controller) =>
            {
                var horizontalMovement = 0;
                if (Input.GetKey(KeyCode.A))
                    horizontalMovement = -1;
                if (Input.GetKey(KeyCode.D))
                    horizontalMovement = 1;

                var verticalMovement = 0;
                if (Input.GetKey(KeyCode.S))
                    verticalMovement = -1;
                if (Input.GetKey(KeyCode.W))
                    verticalMovement = 1;
                
                var currentMagnitude = Input.GetKey(KeyCode.LeftShift) ? 1.5f : 1.0f;
                var jump = Input.GetKey(KeyCode.Space);
            
                Vector3 forward = new Vector3(camera.Forward.x, 0.0f, camera.Forward.z).normalized;
                Vector3 right = new Vector3(camera.Right.x, 0.0f, camera.Right.z).normalized;
            
                if (!MathUtilities.IsZero(horizontalMovement) || !MathUtilities.IsZero(verticalMovement))
                {
                    controller.CurrentDirection = (right * horizontalMovement + forward * verticalMovement).normalized;
                    controller.CurrentMagnitude = currentMagnitude;
                }
                else
                {
                    controller.CurrentMagnitude = 0.0f;
                }
            
                controller.Jump = jump;
            }).Run();
        }
    }
}