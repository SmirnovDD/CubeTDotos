using Unity.Mathematics;
using UnityEngine;
using Utils;

public static class BallisticShootingMath
    {
        public static int SolveBallisticArc(float3 projPos, float projSpeed, float3 target, float gravity, out float3 s0, out float3 s1, out float shootAngle) 
        {
            Debug.Assert(projSpeed > 0 && gravity > 0, "Solve ballistic arc called with invalid data");

            s0 = float3.zero;
            s1 = float3.zero;
            shootAngle = 0;

            float3 diff = target - projPos;
            float3 diffXZ = new float3(diff.x, 0f, diff.z);
            float groundDist = diffXZ.Length();

            float speed2 = projSpeed*projSpeed;
            float speed4 = speed2*speed2;
            float y = diff.y;
            float x = groundDist;
            float gx = gravity*x;

            float root = speed4 - gravity*(gravity*x*x + 2*y*speed2);

            // No solution
            if (root < 0)
            {
                Debug.LogError("No shooting solution!");
                return 0;
            }

            root = math.sqrt(root);

            float lowAng = math.atan2(speed2 - root, gx);
            float highAng = math.atan2(speed2 + root, gx);
            int numSolutions = lowAng != highAng ? 2 : 1;
            shootAngle = lowAng;
            float3 groundDir = diffXZ.Normalize();
            s0 = groundDir * (math.cos(lowAng) * projSpeed) + math.up() * (math.sin(lowAng) * projSpeed);
            if (numSolutions > 1)
                s1 = groundDir * (math.cos(highAng) * projSpeed) + math.up() * (math.sin(highAng) * projSpeed);
            
            return numSolutions;
        }
        
        public static float3 ApproximateTargetPositionBallisticSimple(float3 ammoPosition, float ammoVelocity, float shootAngle, float3 targetPosition, float3 targetVelocity)
        {
            if (targetVelocity.IsEqualTo(float3.zero))
                return targetPosition;
	        
            float3 diff = targetPosition - ammoPosition;
            float3 diffXZ = new float3(diff.x, 0f, diff.z);
            float groundDist = diffXZ.Length();

            float timeToReachCurrentTargetPos = GetTimeToHitTargetWithBallistics(groundDist, ammoVelocity, shootAngle);
	    
            diff = PredictPos(targetPosition, targetVelocity, timeToReachCurrentTargetPos) - ammoPosition;
            diffXZ = new float3(diff.x, 0f, diff.z);
            groundDist = diffXZ.Length();
	    
            float timeToReachPredictedTargetPos = GetTimeToHitTargetWithBallistics(groundDist, ammoVelocity, shootAngle);
		
            return PredictPos(targetPosition, targetVelocity, timeToReachPredictedTargetPos);
        }

        private static float GetTimeToHitTargetWithBallistics(float toTargetGroundDistance, float projectileVelocity, float shootAngle)
        {
            if (projectileVelocity * math.cos(shootAngle) == 0)
                return float.MaxValue;
	    
            float time = toTargetGroundDistance / (projectileVelocity * math.cos(shootAngle));
            return time;
        }
        
        private static float3 PredictPos(float3 position, float3 velocity, float time)
        {
            return velocity * time + position;
        }
    }