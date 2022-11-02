using Unity.Entities;
using UnityEngine;

namespace Data
{
    [GenerateAuthoringComponent]
    public struct CameraFollowComponentData : IComponentData
    {
        public float Pitch;
        public float Zoom;
        public float RotateSpeed;
        
        /// <summary>
        /// The Yaw angle of the camera. For the standard camera this determines how it is rotated around the followed entity.
        /// </summary>
        public float Yaw { get; set; }

        /// <summary>
        /// The normalized camera forward vector.
        /// </summary>
        public Vector3 Forward { get; set; }

        /// <summary>
        /// The normalize camera right vector.
        /// </summary>
        public Vector3 Right { get; set; }
    }
}