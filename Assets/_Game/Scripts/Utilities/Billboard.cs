using UnityEngine;

namespace Momentum
{
    /// <summary>Rotates the object to face the active camera (used for markers and world labels).</summary>
    public class Billboard : MonoBehaviour
    {
        [SerializeField] bool lockY = true;

        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null) return;
            Vector3 dir = transform.position - cam.transform.position;
            if (lockY) dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) return;
            transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
        }
    }
}
