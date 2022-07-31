using Systems;
using Data;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using Utils;

namespace VertexFragment
{
    /// <summary>
    /// Basic system which follows the entity with the <see cref="CameraFollowComponent"/>.
    /// </summary>
    [UpdateBefore(typeof(CharacterControllerSystem))]
    public partial class CameraFollowSystem : SystemBase
    {
        private Transform _cameraTransform;
        
        protected override void OnCreate()
        {
            base.OnCreate();
            _cameraTransform = Camera.main.transform;
            Cursor.lockState = CursorLockMode.Locked;
        }

        protected override void OnUpdate()
        {
            Entities.ForEach((
                Entity entity,
                ref Translation position,
                ref Rotation rotation,
                ref CameraFollowComponentData camera) =>
            {
                ProcessCameraInput(ref camera);

                Vector3 currPos = _cameraTransform.position;
                Vector3 targetPos = new Vector3(position.Value.x, position.Value.y + 1.0f, position.Value.z);

                targetPos += (_cameraTransform.forward * -camera.Zoom);
                _cameraTransform.rotation = Quaternion.Euler(camera.Pitch, camera.Yaw, 0.0f);
                _cameraTransform.position = Vector3.Lerp(currPos, targetPos, Time.DeltaTime * 30f);

                camera.Forward = _cameraTransform.forward;
                camera.Right = _cameraTransform.right;
            }).WithoutBurst().Run();
        }

        /// <summary>
        /// Handles all camera related input.
        /// </summary>
        /// <param name="camera"></param>
        /// <returns></returns>
        private bool ProcessCameraInput(ref CameraFollowComponentData camera)
        {
            return ProcessCameraZoom(ref camera) ||
                   ProcessCameraYawPitch(ref camera);
        }

        /// <summary>
        /// Handles input for zooming the camera in and out.
        /// </summary>
        /// <param name="camera"></param>
        /// <returns></returns>
        private bool ProcessCameraZoom(ref CameraFollowComponentData camera)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");

            if (!MathUtilities.IsZero(scroll))
            {
                camera.Zoom -= scroll;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Handles input for manipulating the camera yaw (rotating around).
        /// </summary>
        /// <param name="camera"></param>
        /// <returns></returns>
        private bool ProcessCameraYawPitch(ref CameraFollowComponentData camera)
        {
            camera.Yaw += Input.GetAxis("Mouse X") * camera.RotateSpeed;
            camera.Pitch -= Input.GetAxis("Mouse Y") * camera.RotateSpeed;
            camera.Yaw = ClampAngle(camera.Yaw, 0, 360);
            camera.Pitch = Mathf.Clamp(camera.Pitch, 0, 90);
            return true;
        }
        
        private float ClampAngle (float angle, float min, float max) {
            if (angle < 0) 
            {
                angle += 360F;
            }

            if (angle > 360F) 
            {
                angle -= 360F;
            }
            return Mathf.Clamp (angle, min, max);
        }
    }
}