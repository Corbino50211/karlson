using UnityEngine;
using UnityEngine.EventSystems;

namespace Momentum.LevelEditor
{
    /// <summary>
    /// Level editor fly camera. Hold right mouse to look + WASD/QE to fly (Shift = fast, wheel = speed),
    /// middle mouse to pan, mouse wheel to dolly when not flying.
    /// </summary>
    public class EditorCameraController : MonoBehaviour
    {
        [SerializeField] float moveSpeed = 16f;
        [SerializeField] float fastMultiplier = 3f;
        [SerializeField] float lookSensitivity = 2.5f;
        [SerializeField] float panSpeed = 0.04f;
        [SerializeField] float dollySpeed = 6f;

        float yaw;
        float pitch;

        public bool IsFlying { get; private set; }
        public bool InputBlocked { get; set; }

        void OnEnable()
        {
            var e = transform.eulerAngles;
            yaw = e.y;
            pitch = e.x > 180f ? e.x - 360f : e.x;
        }

        void OnDisable()
        {
            if (IsFlying) StopFlying();
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

            if (Input.GetMouseButtonDown(1) && !overUI && !InputBlocked) StartFlying();
            if (IsFlying && !Input.GetMouseButton(1)) StopFlying();

            if (IsFlying)
            {
                float mx = SafeAxis("Mouse X");
                float my = SafeAxis("Mouse Y");
                yaw += mx * lookSensitivity;
                pitch = Mathf.Clamp(pitch - my * lookSensitivity, -89f, 89f);
                transform.rotation = Quaternion.Euler(pitch, yaw, 0f);

                Vector3 move = Vector3.zero;
                if (Input.GetKey(KeyCode.W)) move += transform.forward;
                if (Input.GetKey(KeyCode.S)) move -= transform.forward;
                if (Input.GetKey(KeyCode.D)) move += transform.right;
                if (Input.GetKey(KeyCode.A)) move -= transform.right;
                if (Input.GetKey(KeyCode.E)) move += Vector3.up;
                if (Input.GetKey(KeyCode.Q)) move -= Vector3.up;
                float speed = moveSpeed * (Input.GetKey(KeyCode.LeftShift) ? fastMultiplier : 1f);
                transform.position += move * speed * dt;

                float scroll = Input.mouseScrollDelta.y;
                if (Mathf.Abs(scroll) > 0.01f) moveSpeed = Mathf.Clamp(moveSpeed * (1f + scroll * 0.1f), 2f, 120f);
                return;
            }

            if (InputBlocked) return;

            if (Input.GetMouseButton(2))
            {
                float mx = SafeAxis("Mouse X");
                float my = SafeAxis("Mouse Y");
                transform.position -= (transform.right * mx + transform.up * my) * panSpeed * Mathf.Max(4f, moveSpeed);
            }

            if (!overUI)
            {
                float scroll = Input.mouseScrollDelta.y;
                if (Mathf.Abs(scroll) > 0.01f) transform.position += transform.forward * scroll * dollySpeed;
            }
        }

        void StartFlying()
        {
            IsFlying = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        void StopFlying()
        {
            IsFlying = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void SetPose(Vector3 position, float newYaw, float newPitch)
        {
            yaw = newYaw;
            pitch = newPitch;
            transform.SetPositionAndRotation(position, Quaternion.Euler(pitch, yaw, 0f));
        }

        /// <summary>Moves the camera so the bounds are framed.</summary>
        public void Focus(Bounds bounds)
        {
            float distance = Mathf.Max(6f, bounds.extents.magnitude * 2.2f);
            transform.position = bounds.center - transform.forward * distance;
        }

        static float SafeAxis(string axis)
        {
            try
            {
                return Input.GetAxisRaw(axis);
            }
            catch (System.ArgumentException)
            {
                return 0f;
            }
        }
    }
}
