using UnityEngine;

namespace Momentum
{
    /// <summary>
    /// Pooled line effect used for bullet tracers (a short streak travelling to the target) and railgun
    /// trails (a full beam that fades and thins out).
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class Tracer : MonoBehaviour, IPoolable
    {
        [SerializeField] float tracerSpeed = 420f;
        [SerializeField] float tracerLength = 7f;
        [SerializeField] float beamDuration = 0.6f;

        LineRenderer line;
        Vector3 from;
        Vector3 to;
        Color color;
        float width;
        bool beam;
        float age;
        float travelTime;
        bool playing;

        void Awake()
        {
            line = GetComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
        }

        public void Play(Vector3 start, Vector3 end, Color tint, float lineWidth, bool asBeam)
        {
            from = start;
            to = end;
            color = tint;
            width = lineWidth;
            beam = asBeam;
            age = 0f;
            playing = true;
            float distance = Vector3.Distance(start, end);
            travelTime = Mathf.Max(0.02f, distance / tracerSpeed);
            line.enabled = true;
            UpdateLine();
        }

        public void OnSpawned()
        {
            playing = false;
            if (line != null) line.enabled = false;
        }

        public void OnDespawned()
        {
            playing = false;
            if (line != null) line.enabled = false;
        }

        void Update()
        {
            if (!playing) return;
            age += Time.deltaTime;
            UpdateLine();
        }

        void UpdateLine()
        {
            if (beam)
            {
                float k = Mathf.Clamp01(age / beamDuration);
                var c = color;
                c.a = 1f - k;
                line.startColor = c;
                line.endColor = c;
                line.widthMultiplier = width * (1f - k * 0.8f);
                line.SetPosition(0, from);
                line.SetPosition(1, to);
                if (k >= 1f) Finish();
            }
            else
            {
                float k = Mathf.Clamp01(age / travelTime);
                Vector3 dir = (to - from);
                float total = dir.magnitude;
                if (total < 0.001f)
                {
                    Finish();
                    return;
                }
                dir /= total;
                float headDist = total * k;
                float tailDist = Mathf.Max(0f, headDist - tracerLength);
                line.SetPosition(0, from + dir * tailDist);
                line.SetPosition(1, from + dir * headDist);
                var head = color;
                var tail = color;
                tail.a = 0f;
                line.startColor = tail;
                line.endColor = head;
                line.widthMultiplier = width;
                if (k >= 1f) Finish();
            }
        }

        void Finish()
        {
            playing = false;
            line.enabled = false;
            PoolManager.Despawn(gameObject);
        }
    }
}
