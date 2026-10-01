using UnityEngine;

namespace Momentum.PlayerSystems
{
    /// <summary>
    /// Visual rope for the grappling hook: shoots out with a decaying wave, stays taut while attached and
    /// snaps back on release.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class GrappleRope : MonoBehaviour
    {
        [SerializeField] GrappleHook hook;
        [SerializeField] int segments = 28;
        [SerializeField] float travelTime = 0.09f;
        [SerializeField] float waveAmplitude = 0.5f;
        [SerializeField] float waveFrequency = 3f;
        [SerializeField] float waveDamping = 9f;
        [SerializeField] float retractTime = 0.12f;

        LineRenderer line;
        float shootTime;
        float retractStart;
        bool visible;
        bool retracting;
        Vector3 lastEnd;

        void Awake()
        {
            line = GetComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = segments;
            line.enabled = false;
        }

        public void Shoot()
        {
            visible = true;
            retracting = false;
            shootTime = Time.time;
            if (hook != null) lastEnd = hook.RopeOrigin.position;
        }

        public void Retract()
        {
            if (!visible) return;
            retracting = true;
            retractStart = Time.time;
        }

        public void Hide()
        {
            visible = false;
            retracting = false;
            if (line != null) line.enabled = false;
        }

        void LateUpdate()
        {
            if (!visible || hook == null)
            {
                if (line.enabled) line.enabled = false;
                return;
            }

            Vector3 start = hook.RopeOrigin.position;
            Vector3 end;
            if (retracting)
            {
                float k = Mathf.Clamp01((Time.time - retractStart) / retractTime);
                end = Vector3.Lerp(lastEnd, start, k);
                if (k >= 1f)
                {
                    Hide();
                    return;
                }
            }
            else
            {
                float k = Mathf.Clamp01((Time.time - shootTime) / travelTime);
                end = Vector3.Lerp(start, hook.AnchorPoint, k);
                lastEnd = end;
            }

            line.enabled = true;
            if (line.positionCount != segments) line.positionCount = segments;
            Vector3 dir = end - start;
            float length = dir.magnitude;
            Vector3 side = length > 0.001f ? Vector3.Cross(dir / length, Vector3.up) : Vector3.right;
            if (side.sqrMagnitude < 0.001f) side = Vector3.right;
            side.Normalize();
            Vector3 up = length > 0.001f ? Vector3.Cross(side, dir / length) : Vector3.up;
            float amp = retracting ? 0f : waveAmplitude * Mathf.Exp(-(Time.time - shootTime) * waveDamping);

            for (int i = 0; i < segments; i++)
            {
                float t = (float)i / (segments - 1);
                Vector3 p = Vector3.Lerp(start, end, t);
                float envelope = Mathf.Sin(t * Mathf.PI);
                float wave = Mathf.Sin(t * Mathf.PI * 2f * waveFrequency - Time.time * 30f) * amp * envelope;
                p += up * wave + side * wave * 0.5f;
                line.SetPosition(i, p);
            }
        }
    }
}
