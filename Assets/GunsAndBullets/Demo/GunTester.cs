using UnityEngine;

namespace GNB.Demo
{
    public class GunTester : MonoBehaviour
    {
        public Gun[] guns;

        private void Update()
        {
            if (Input.GetMouseButton(0) == true)
            {
                foreach (var gun in guns)
                {
                    gun.Fire(Vector3.zero);
                }
            }
        }
    }
}
