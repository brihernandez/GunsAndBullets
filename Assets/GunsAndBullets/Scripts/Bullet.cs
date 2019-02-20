using UnityEngine;

namespace GNB
{
    public class Bullet : MonoBehaviour
    {
        [Tooltip("Time until the projectile expires.")]
        [SerializeField] private float timeToLive = 5f;

        [Tooltip("Layers to check for collisions.")]
        [SerializeField] private LayerMask hitMask = -1;

        [Tooltip("Effect prefab to spawn when the bullet hits something.")]
        [SerializeField] private ParticleSystem impactFxPrefab = null;

        [Tooltip("How much gravity is applied to the bullet.")]
        [SerializeField] private float gravityModifier = 0f;

        [Tooltip("Bullet will rotate so that it is always aligned with its velocity vector. Recommended if using gravity.")]
        [SerializeField] private bool alignToVelocity = false;

        [Tooltip("Move the bullet only during the fixed update instead of regular update.")]
        [SerializeField] private bool useFixedUpdate = true;

        private Vector3 velocity = Vector3.forward;
        private float destructionTime = 0f;
        private bool isFired = false;

        // How far ahead in seconds the projectile looks forwards when doing its hit calculations.
        // 1 usually works fine, but there might be cases where you want the bullet to look ahead
        // more or less depending on the speeds involved.
        private const float kVelocityMult = 1f;

        private void Update()
        {
            if (isFired == false)
                return;

            if (Time.time > destructionTime)
                DestroyBullet(transform.position, false);
            else if (useFixedUpdate == false)
                MoveBullet();
        }

        private void FixedUpdate()
        {
            if (isFired == false)
                return;

            if (useFixedUpdate == true)
                MoveBullet();
        }

        /// <summary>
        /// Fires the bullet.
        /// </summary>
        /// <param name="position">Position the bullet will start at.</param>
        /// <param name="rotation">Rotation the bullet will start at.</param>
        /// <param name="inheritedVelocity">Any extra velocity to add to the bullet that it might be inheriting from its firer.</param>
        /// <param name="muzzleVelocity">Starting forward velocity of the bullet.</param>
        /// <param name="deviation">Maximum random deviation in degrees to apply to the bullet.</param>
        public void Fire(Vector3 position, Quaternion rotation, Vector3 inheritedVelocity, float muzzleVelocity, float deviation)
        {
            // Start position.
            transform.position = position;

            // Calculate a random deviation.
            Vector3 deviationAngle = Vector3.zero;
            deviationAngle.x = Random.Range(-deviation, deviation);
            deviationAngle.y = Random.Range(-deviation, deviation);
            Quaternion deviationRotation = Quaternion.Euler(deviationAngle);

            // Rotate the bullet to the direction requested, plus some random deviation.
            transform.rotation = rotation * deviationRotation;

            velocity = (transform.forward * muzzleVelocity) + inheritedVelocity;
            destructionTime = Time.time + timeToLive;
            isFired = true;
        }

        /// <summary>
        /// Destroy the bullet and play the necessary events.
        /// </summary>
        /// <param name="position">Where the event that destroyed the bullet happened.</param>
        /// <param name="fromImpact">Whether the bullet was destroyed because it was impacted, or something else.</param>
        public void DestroyBullet(Vector3 position, bool fromImpact)
        {
            if (fromImpact == true && impactFxPrefab != null)
            {
                var impactFx = Instantiate(impactFxPrefab, position, transform.rotation);
                impactFx.Play();
            }

            Destroy(gameObject);
        }


        /// <summary>
        /// Moves the bullet forwards and handles collision detection.
        /// </summary>
        private void MoveBullet()
        {
            // Perform the raycast. Shoots a ray forwards of the bullet that covers all the distance
            // that it will cover in this frame. This guarantees a hit in all but the most extenuating
            // circumstances (against other extremely fast and small moving targets it may miss) and
            // works at practically any bullet speed.
            RaycastHit rayHit;
            Ray velocityRay = new Ray(transform.position, velocity.normalized);
            bool rayHasHit = Physics.Raycast(velocityRay, out rayHit, velocity.magnitude * kVelocityMult * Time.deltaTime, hitMask);

            if (rayHasHit == true)
            {
                // Bullet hit something.
                // Put code here to damage the thing you hit using your components.
                DestroyBullet(rayHit.point, true);
            }
            else
            {
                // Bullet didn't hit anything, continue moving.
                transform.Translate(velocity * Time.deltaTime, Space.World);

                // Account for bullet drop.
                velocity += Physics.gravity * gravityModifier * Time.deltaTime;

                // Align to velocity
                if (alignToVelocity == true)
                    transform.rotation = Quaternion.LookRotation(velocity);
            }
        }
    }
}
