using UnityEngine;

namespace Momentum
{
    /// <summary>Idle animation for pickups and markers: spins around Y and bobs up and down.</summary>
    public class SpinAndBob : MonoBehaviour
    {
        [SerializeField] float spinSpeed = 90f;
        [SerializeField] float bobHeight = 0.15f;
        [SerializeField] float bobSpeed = 2f;
        [SerializeField] bool useUnscaledTime;

        Vector3 basePosition;
        float phase;

        void OnEnable()
        {
            basePosition = transform.localPosition;
            phase = Random.Range(0f, Mathf.PI * 2f);
        }

        void OnDisable()
        {
            transform.localPosition = basePosition;
        }

        void Update()
        {
            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float t = useUnscaledTime ? Time.unscaledTime : Time.time;
            transform.Rotate(0f, spinSpeed * dt, 0f, Space.Self);
            transform.localPosition = basePosition + Vector3.up * Mathf.Sin(t * bobSpeed + phase) * bobHeight;
        }
    }
}
