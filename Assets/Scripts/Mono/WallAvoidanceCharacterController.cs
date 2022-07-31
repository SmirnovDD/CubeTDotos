using UnityEngine;
using System.Collections;
using UnityMovementAI.UnityMovementAI;

namespace UnityMovementAI
{
    public class WallAvoidanceCharacterController : MonoBehaviour
    {
        [SerializeField] private float _movementTimeAfterNoWallsFound = 1f;
        public float maxAcceleration = 40f;

        public enum WallDetection { Raycast, Spherecast }

        public WallDetection wallDetection = WallDetection.Spherecast;

        public LayerMask castMask = Physics.DefaultRaycastLayers;

        /// <summary>
        /// The distance away from the collision that we wish go
        /// </summary>
        public float wallAvoidDistance = 0.5f;

        /// <summary>
        /// How far ahead the ray should extend
        /// </summary>
        public float mainWhiskerLen = 1.25f;

        public float sideWhiskerLen = 0.701f;

        public float sideWhiskerAngle = 45f;

        public bool PathBlocked { get; private set; }
        
        CharacterController _characterController;
        SteeringBasicsCharacterController steeringBasics;
        private Vector3 _storedAcceleration;
        private float _timer;
        
        void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            steeringBasics = GetComponent<SteeringBasicsCharacterController>();
        }

        public (Vector3 steering, bool jump) GetSteering()
        {
            var direction = _characterController.velocity.magnitude > 0.005f
                ? _characterController.velocity
                : _characterController.transform.forward;
            return GetSteering(direction);
        }

        public (Vector3 steering, bool jump) GetSteering(Vector3 facingDir)
        {
            Vector3 acceleration = Vector3.zero;

            GenericCastHit hit;

             /* If no collision do nothing */
             if (FindObstacle(facingDir, out hit)) //CHECK IF CAN JUMP
             {
                 PathBlocked = true;
                 // var placedObject = hit.collider.GetComponentInParent<IPlacedObject>();
                 // if (placedObject != null)
                 // {
                 //     if (placedObject.Type == ObjectToPlaceType.SmallCubeStairs)
                 //     {
                 //         if (ShouldJumpOverStares(placedObject))
                 //             return (Vector3.zero, true);
                 //         return (Vector3.zero, false);
                 //     }
                 //     if (!placedObject.HasNeighbourFromSide(ConnectedFromSide.Top))
                 //     {
                 //         return (Vector3.zero, true);
                 //     }
                 // }
             }
            else
             {
                 PathBlocked = false;
                 if (Time.time < _timer)
                     return (_storedAcceleration, false);
                 return (acceleration, false);
             }
            /* Create a target away from the wall to seek */
            Vector3 targetPosition = hit.point + hit.normal * wallAvoidDistance;

            /* If velocity and the collision normal are parallel then move the target a bit to
             * the left or right of the normal */
            float angle = Vector3.Angle(_characterController.velocity, hit.normal);
            if (angle > 165f)
            {
                Vector3 perp;

                perp = new Vector3(-hit.normal.z, hit.normal.y, hit.normal.x);

                /* Add some perp displacement to the target position propotional to the angle between the wall normal
                 * and facing dir and propotional to the wall avoidance distance (with 2f being a magic constant that
                 * feels good) */
                targetPosition = targetPosition + (perp * (Mathf.Sin((angle - 165f) * Mathf.Deg2Rad) * 2f * wallAvoidDistance));
            }

            //SteeringBasics.debugCross(targetPostition, 0.5f, new Color(0.612f, 0.153f, 0.69f), 0.5f, false);
            _storedAcceleration = steeringBasics.Seek(targetPosition, maxAcceleration);
            _timer = Time.time + _movementTimeAfterNoWallsFound;
            return (_storedAcceleration, false);
        }

        // private bool ShouldJumpOverStares(IPlacedObject placedObject)
        // {
        //     if (Vector3.Dot(transform.forward, placedObject.Collider.transform.forward) < 0) //TODO depends on ladder forward vector
        //         return true;
        //     return false;
        // }
        
        bool FindObstacle(Vector3 facingDir, out GenericCastHit firstHit)
        {
            facingDir = facingDir.normalized;

            /* Create the direction vectors */
            Vector3[] dirs = new Vector3[3];
            dirs[0] = facingDir;

            float orientation = SteeringBasicsCharacterController.VectorToOrientation(facingDir, true);

            dirs[1] = SteeringBasicsCharacterController.OrientationToVector(orientation + sideWhiskerAngle * Mathf.Deg2Rad, true);
            dirs[2] = SteeringBasicsCharacterController.OrientationToVector(orientation - sideWhiskerAngle * Mathf.Deg2Rad, true);

            return CastWhiskers(dirs, out firstHit);
        }

        bool CastWhiskers(Vector3[] dirs, out GenericCastHit firstHit)
        {
            firstHit = new GenericCastHit();
            bool foundObs = false;

            for (int i = 0; i < dirs.Length; i++)
            {
                float dist = (i == 0) ? mainWhiskerLen : sideWhiskerLen;

                GenericCastHit hit;

                if (GenericCast(dirs[i], out hit, dist))
                {
                    foundObs = true;
                    firstHit = hit;
                    break;
                }
            }

            return foundObs;
        }

        bool GenericCast(Vector3 direction, out GenericCastHit hit, float distance = Mathf.Infinity)
        {
            bool result = false;
            Vector3 origin = _characterController.transform.position;

            RaycastHit h;

            if (wallDetection == WallDetection.Raycast)
            {
                result = Physics.Raycast(origin, direction, out h, distance, castMask.value);
            }
            else
            {
                result = Physics.SphereCast(origin, (_characterController.radius * 0.5f), direction, out h, distance,
                    castMask.value);
            }

            hit = new GenericCastHit(h);

            /* If the character is grounded and we have a result check that we've hit a wall */
            if (result)
            {
                /* If the normal is less than our slope limit then we've hit the ground and not a wall */
                float angle = Vector3.Angle(Vector3.up, hit.normal);

                if (angle < _characterController.slopeLimit)
                {
                    result = false;
                }
            }

            return result;
        }

        struct GenericCastHit
        {
            public Vector3 point;
            public Vector3 normal;
            public Collider collider;
            
            public GenericCastHit(RaycastHit h)
            {
                point = h.point;
                normal = h.normal;
                collider = h.collider;
            }
        }
    }
}