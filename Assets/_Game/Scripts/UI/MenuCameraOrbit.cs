using UnityEngine;

namespace Momentum.UI
{
    /// <summary>Slowly orbits the menu camera around a point for an animated background.</summary>
    public class MenuCameraOrbit : MonoBehaviour
    {
        [SerializeField] Vector3 center = new Vector3(0f, 6f, 0f);
        [SerializeField] float radius = 38f;
        [SerializeField] float height = 14f;
        [SerializeField] float speed = 4f;

        float angle;

        void Update()
        {
            angle += speed * Time.unscaledDeltaTime;
            Vector3 offset = Quaternion.Euler(0f, angle, 0f) * new Vector3(0f, height, -radius);
            transform.position = center + offset;
            transform.LookAt(center);
        }
    }
}
