using UnityEngine;
using System.Collections.Generic;

namespace GNB
{
    public class Gun : MonoBehaviour
    {
        [Header("Ballistics")]
        [Tooltip("Time in seconds between shots.")]
        [SerializeField] private float fireDelay = 0.2f;
        [Tooltip("When \"Aimed\" property is set to true, this is how far off boresight the gun can gimbal to a target set using the \"TargetPosition\" property.")]
        [SerializeField] [Range(0f, 180f)] private float gimbalRange = 10f;
        [Tooltip("Muzzle velocity in m/s of a fired bullet.")]
        [SerializeField] private float muzzleVelocity = 200f;

        [Header("Firing pattern")]
        [Tooltip("Maximum random aim error in degrees.")]
        [SerializeField] private float deviation = 0.1f;
        [Tooltip("If using multiple barrels, should the barrels fire in sequence or all at once.")]
        [SerializeField] private bool sequentialBarrels = false;
        [Tooltip("Reference transform from which bullets will be spawned. Multiple barrels can be assigned. If no barrels are assigned, bullets will come from the GameObject's center.")]
        [SerializeField] private Transform[] barrels = null;

        [Header("Prefabs")]
        [Tooltip("Bullet prefab to fire from the gun.")]
        [SerializeField] private Bullet bulletPrefab = null;
        [Tooltip("Muzzle flash particle system effect to play from the barrel when the gun is fired.")]
        [SerializeField] private ParticleSystem muzzleFlashPrefab = null;

        [Header("Ammo")]
        [Tooltip("When true, the gun will require ammo to fire. One ammo is consumed per shot, no matter how many barrels the gun has.")]
        [SerializeField] private bool useAmmo = false;
        [Tooltip("Maximum amount of ammo carried by the gun. Ammo can be refilled using the \"ReloadAmmo()\" function.")]
        [SerializeField] private int maxAmmo = 300;

        private Dictionary<Transform, ParticleSystem> barrelToMuzzleFlash = new Dictionary<Transform, ParticleSystem>();
        private Queue<Transform> barrelQueue = null;
        private float cooldown = 0f;

        // =========================
        // Gimballing properties
        // =========================

        /// <summary>
        /// Set to true to enable gimballing functionality. When true, the gun will
        /// automatically try to aim at location defined by the TargetPosition property.
        /// </summary>
        public bool UseGimballedAiming { get; set; }

        /// <summary>
        /// When gimballed aiming is enabled with the "UseGimballedAiming" property,
        /// the gun will try its best to aim at this position within the limits defined
        /// by the gimbalRange field in the component's Inspector.
        /// </summary>
        public Vector3 TargetPosition { get; set; }

        // =========================
        // Firing properties
        // =========================

        /// <summary>
        /// True if the gun has enough ammo to fire.
        /// </summary>
        public bool HasAmmo { get { return !useAmmo || (useAmmo && AmmoCount > 0); } }

        /// <summary>
        /// How much ammo remains.
        /// </summary>
        public int AmmoCount { get; private set; }

        /// <summary>
        /// True when the gun is loaded and ready to fire.
        /// </summary>
        public bool ReadyToFire { get { return (cooldown <= 0f) && (HasAmmo); } }

        // =========================
        // Generic gun properties
        // =========================

        public float FireDelay { get { return fireDelay; } }
        public float GimbalRange { get { return gimbalRange; } }
        public float MuzzleVelocity { get { return muzzleVelocity; } }
        public float Deviation { get { return deviation; } }
        public Bullet BulletPrefab { get { return bulletPrefab; } }
        public bool UseAmmo { get { return useAmmo; } set { useAmmo = value; } }
        public int MaxAmmo { get { return maxAmmo; } }

        private void Awake()
        {
            AmmoCount = maxAmmo;
            UseGimballedAiming = false;
            barrelQueue = new Queue<Transform>(barrels);

            // Attach muzzle flashes to all the barrels.
            if (muzzleFlashPrefab != null)
            {
                foreach (var barrel in barrels)
                {
                    var muzzleFlash = Instantiate(muzzleFlashPrefab, barrel, false).GetComponent<ParticleSystem>();
                    barrelToMuzzleFlash.Add(barrel, muzzleFlash);
                }
            }
        }

        private void Update()
        {
            cooldown = Mathf.MoveTowards(cooldown, 0f, Time.deltaTime);
        }

        /// <summary>
        /// Resets the ammo count of the gun to the maximum ammo count.
        /// </summary>
        public void ReloadAmmo()
        {
            AmmoCount = maxAmmo;
        }

        public void Fire(Vector3 inheritedVelocity)
        {
            if (ReadyToFire == false)
                return;

            if (barrelQueue.Count == 0)
            {
                // No barrels given at all, use the transform as a fallback.
                SpawnAndFireBulletFromBarrel(transform, inheritedVelocity);
            }
            else if (sequentialBarrels)
            {
                // Fire from the next barrel in queue.
                var barrel = barrelQueue.Dequeue();
                SpawnAndFireBulletFromBarrel(barrel, inheritedVelocity);
                barrelQueue.Enqueue(barrel);
            }
            else
            {
                // Fire from all the barrels at once.
                foreach (var barrel in barrelQueue)
                {
                    SpawnAndFireBulletFromBarrel(barrel, inheritedVelocity);
                }
            }

            if (useAmmo)
                AmmoCount--;

            cooldown = fireDelay;
        }

        private void SpawnAndFireBulletFromBarrel(Transform barrel, Vector3 inheritedVelocity)
        {
            Vector3 spawnPos = barrel.position;
            Quaternion aimRotation = barrel.rotation;

            // Gimbal the bullet towards the target only if needed.
            if (UseGimballedAiming == true)
            {
                Vector3 gimballedVec = transform.forward;
                gimballedVec = Vector3.RotateTowards(gimballedVec,
                                                     TargetPosition - barrel.position,
                                                     Mathf.Deg2Rad * gimbalRange,
                                                     1f);
                gimballedVec.Normalize();
                aimRotation = Quaternion.LookRotation(gimballedVec);
            }

            // Play muzzle flash if it exists.
            if (barrelToMuzzleFlash.ContainsKey(barrel))
                barrelToMuzzleFlash[barrel].Play();

            // Instantiate and fire bullet.
            var bullet = Instantiate(bulletPrefab, spawnPos, aimRotation);
            bullet.Fire(spawnPos, aimRotation, inheritedVelocity, muzzleVelocity, deviation);
        }
    }
}
