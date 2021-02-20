using UnityEngine;

namespace Boids
{
    public class Boid : MonoBehaviour
    {
        /// State
        [HideInInspector]
        public Vector3 position;
        [HideInInspector]
        public Vector3 forward;

        /// To update:
        private Vector3 acceleration;
        [HideInInspector]
        public Vector3 avgFlockHeading;
        [HideInInspector]
        public Vector3 avgAvoidanceHeading;
        [HideInInspector]
        public Vector3 centreOfFlockmates;
        [HideInInspector]
        public int numPerceivedFlockmates;

        /// Cached
        private BoidSettings settings;
        private Transform cachedTransform;
        private Transform target;

        public Vector3 velocity = Vector3.zero;
        private Vector3 dir = Vector3.zero;

        public void Initialize(BoidSettings settings, Transform target)
        {
            this.target = target;
            this.settings = settings;

            cachedTransform = transform;
            position = cachedTransform.position;
            forward = cachedTransform.forward;

            float startSpeed = (settings.minSpeed + settings.maxSpeed) / 2;
            velocity = transform.forward * startSpeed;
        }

        public void UpdateBoid()
        {
            acceleration = Vector3.zero;

            FollowTarget();

            if (numPerceivedFlockmates != 0)
            {
                centreOfFlockmates /= numPerceivedFlockmates;

                Vector3 offsetToFlockmatesCentre = centreOfFlockmates - position;

                var alignmentForce = SteerTowards(avgFlockHeading) * settings.alignWeight;
                var cohesionForce = SteerTowards(offsetToFlockmatesCentre) * settings.cohesionWeight;
                var seperationForce = SteerTowards(avgAvoidanceHeading) * settings.seperateWeight;

                acceleration += alignmentForce;
                acceleration += cohesionForce;
                acceleration += seperationForce;
            }

            if (IsHeadingForCollision())
            {
                Vector3 collisionAvoidDir = ObstacleRays();
                Vector3 collisionAvoidForce = SteerTowards(collisionAvoidDir) * settings.avoidCollisionWeight;
                acceleration += collisionAvoidForce;
            }

            SetVelocity();

            position = cachedTransform.position += velocity * Time.deltaTime;
            forward = cachedTransform.forward = dir;
        }

        private void FollowTarget()
        {
            if (target != null)
            {
                Vector3 offsetToTarget = target.position - position;
                acceleration = SteerTowards(offsetToTarget) * settings.targetWeight;
            }
        }

        private void SetVelocity()
        {
            velocity += acceleration * Time.deltaTime;

            float speed = velocity.magnitude;
            dir = velocity / speed;

            speed = Mathf.Clamp(speed, settings.minSpeed, settings.maxSpeed);
            velocity = dir * speed;
        }

        #region Collision Detect
        private bool IsHeadingForCollision()
        {
            return Physics.SphereCast(position, settings.boundsRadius, forward, out _, settings.collisionAvoidDst, settings.obstacleMask);
        }

        private Vector3 ObstacleRays()
        {
            Vector3[] rayDirections = BoidHelper.directions;

            for (int i = 0; i < rayDirections.Length; i++)
            {
                Vector3 dir = cachedTransform.TransformDirection(rayDirections[i]);
                Ray ray = new Ray(position, dir);

                if (!Physics.SphereCast(ray, settings.boundsRadius, settings.collisionAvoidDst, settings.obstacleMask))
                    return dir;
            }

            return forward;
        }
        #endregion

        private Vector3 SteerTowards(Vector3 vector)
        {
            Vector3 v = vector.normalized * settings.maxSpeed - velocity;
            return Vector3.ClampMagnitude(v, settings.maxSteerForce);
        }
    }
}