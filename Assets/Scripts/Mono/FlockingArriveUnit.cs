using UnityEngine;
using UnityMovementAI.UnityMovementAI;

namespace UnityMovementAI
{
    public class FlockingArriveUnit : MonoBehaviour
    {
        public Transform target;
        // public float cohesionWeight = 1.5f;
        // public float separationWeight = 2f;
        // public float velocityMatchWeight = 1f;
        // public NearSensorCharacterController flockingUnitsensor;
        // public NearSensorCharacterController leaderUnitsensor;
        SteeringBasicsCharacterController steeringBasics;
        // CohesionCharacterController cohesion;
        // SeparationCharacterController separation;
        // VelocityMatchCharacterController velocityMatch;
        WallAvoidanceCharacterController wallAvoidance;
        // private Vector3 _posLastCheck;
        // private float _checkDistanceMagnitudeSqr = 1;
        // private float _checkTime = 3f;
        // private float _timer;
        void Start()
        {
            steeringBasics = GetComponent<SteeringBasicsCharacterController>();
            // cohesion = GetComponent<CohesionCharacterController>();
            // separation = GetComponent<SeparationCharacterController>();
            // velocityMatch = GetComponent<VelocityMatchCharacterController>();
            wallAvoidance = GetComponent<WallAvoidanceCharacterController>();
        }

        void Update()
        {
            if (target == null)
            {    
                steeringBasics.Stop();
                return;
            }
            
            var (accel, isJumping) = wallAvoidance.GetSteering();

            
            //if (accel.magnitude < 0.005f)
            //{
                // if (flockingUnitsensor.targets.Count > 0)
                // {
                //     accel += separation.GetSteering(flockingUnitsensor.targets) * separationWeight;
                // }
                // if (leaderUnitsensor.targets.Count > 0)
                // {
                //     accel += cohesion.GetSteering(leaderUnitsensor.targets) * cohesionWeight;
                //     accel += velocityMatch.GetSteering(leaderUnitsensor.targets) * velocityMatchWeight;
                // }
                //
                // if (leaderUnitsensor.targets.Count == 0)
                // {
                    accel = steeringBasics.Arrive(target.position);
                // }
            //}

            //steeringBasics.SetTryJump(isJumping);
            steeringBasics.Steer(accel);
            steeringBasics.LookWhereYoureGoing();
            //
            // if ((transform.position - _posLastCheck).sqrMagnitude < _checkDistanceMagnitudeSqr)
            // {
            //     if (Time.time > _timer)
            //     {
            //         steeringBasics.Steer(-transform.forward * Random.Range(5, 15) + Vector3.left * Random.Range(5, 15));
            //         _timer = Time.time + _checkTime;
            //     }
            // }
            // else
            // {
            //     _posLastCheck = transform.position;
            //     _timer = Time.time + _checkTime;
            // }
        }
    }
}