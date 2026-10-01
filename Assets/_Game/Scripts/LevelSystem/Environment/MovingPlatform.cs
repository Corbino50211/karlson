using UnityEngine;

namespace Momentum.Levels
{
    /// <summary>
    /// Kinematic platform moving between its start position and start + offset. The player inherits the
    /// platform's velocity while standing on it (and keeps it when jumping off).
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class MovingPlatform : MonoBehaviour, ILevelObjectConfigurable
    {
        [Tooltip("World-space offset of the far end of the path.")]
        [SerializeField] Vector3 moveOffset = new Vector3(0f, 0f, 10f);
        [SerializeField] float speed = 4f;
        [SerializeField] float waitTime = 0.75f;
        [SerializeField] bool pingPong = true;
        [Tooltip("Optional continuous yaw rotation (degrees per second).")]
        [SerializeField] float spinSpeed;
        [SerializeField] float startDelay;

        Rigidbody rb;
        Vector3 startPosition;
        Quaternion startRotation;
        float t;
        int direction = 1;
        float waitTimer;
        bool initialized;

        public Vector3 Velocity { get; private set; }
        public Vector3 MoveOffset => moveOffset;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.useGravity = false;
        }

        void Start()
        {
            Initialize();
        }

        void Initialize()
        {
            if (initialized) return;
            initialized = true;
            startPosition = transform.position;
            startRotation = transform.rotation;
            waitTimer = startDelay;
        }

        void FixedUpdate()
        {
            if (!initialized) Initialize();
            float dt = Time.fixedDeltaTime;
            float distance = moveOffset.magnitude;

            if (spinSpeed != 0f)
            {
                rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, spinSpeed * dt, 0f));
            }

            if (distance < 0.01f || speed <= 0f)
            {
                Velocity = Vector3.zero;
                return;
            }

            if (waitTimer > 0f)
            {
                waitTimer -= dt;
                Velocity = Vector3.zero;
                return;
            }

            t += direction * speed * dt / distance;
            if (t >= 1f)
            {
                t = 1f;
                if (pingPong) direction = -1;
                else t = 0f;
                waitTimer = waitTime;
            }
            else if (t <= 0f)
            {
                t = 0f;
                direction = 1;
                waitTimer = waitTime;
            }

            float eased = Mathf.SmoothStep(0f, 1f, t);
            Vector3 target = startPosition + moveOffset * eased;
            Velocity = (target - rb.position) / dt;
            rb.MovePosition(target);
        }

        public void ApplyLevelProperties(LevelObjectData data)
        {
            moveOffset = data.GetVector3("offset", moveOffset);
            speed = data.GetFloat("speed", speed);
            waitTime = data.GetFloat("wait", waitTime);
            spinSpeed = data.GetFloat("spin", spinSpeed);
            pingPong = data.GetBool("pingPong", pingPong);
            startDelay = data.GetFloat("delay", startDelay);
        }

        void OnDrawGizmosSelected()
        {
            Vector3 from = Application.isPlaying && initialized ? startPosition : transform.position;
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(from, from + moveOffset);
            Gizmos.DrawWireCube(from + moveOffset, transform.lossyScale);
        }
    }
}
