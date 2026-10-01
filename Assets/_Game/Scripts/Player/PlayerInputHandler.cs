using System;
using UnityEngine;

namespace Momentum.PlayerSystems
{
    /// <summary>
    /// Reads keyboard/mouse input once per frame (legacy Input Manager) and exposes it to the player systems.
    /// Edge-triggered actions needed by FixedUpdate (jump, crouch) are buffered/latched so they are never lost.
    /// </summary>
    public class PlayerInputHandler : MonoBehaviour
    {
        [SerializeField] KeyBindings bindings = new KeyBindings();

        public KeyBindings Bindings => bindings;

        /// <summary>When false all input reads as neutral (paused, dead, cutscenes).</summary>
        public bool InputEnabled { get; set; } = true;

        public Vector2 Move { get; private set; }
        public Vector2 LookDelta { get; private set; }

        public bool JumpHeld { get; private set; }
        public bool JumpPressed { get; private set; }
        public bool CrouchHeld { get; private set; }
        public bool SprintHeld { get; private set; }
        public bool FireHeld { get; private set; }
        public bool FirePressed { get; private set; }
        public bool AltFireHeld { get; private set; }
        public bool AltFirePressed { get; private set; }
        public bool ReloadPressed { get; private set; }
        public bool InteractPressed { get; private set; }
        public bool GrappleHeld { get; private set; }
        public bool GrapplePressed { get; private set; }
        public bool GrappleReleased { get; private set; }
        public bool NextWeapon { get; private set; }
        public bool PreviousWeapon { get; private set; }
        /// <summary>Weapon slot (0-8) pressed this frame, or -1.</summary>
        public int WeaponSlotPressed { get; private set; } = -1;

        float lastJumpPressTime = -999f;
        bool crouchPressedLatch;

        static bool mouseAxesChecked;
        static bool mouseAxesAvailable = true;

        void Update()
        {
            bool paused = GameManager.Instance != null && GameManager.Instance.IsPaused;
            if (!InputEnabled || paused)
            {
                ClearFrameState(true);
                return;
            }

            float x = (Input.GetKey(bindings.right) ? 1f : 0f) - (Input.GetKey(bindings.left) ? 1f : 0f);
            float y = (Input.GetKey(bindings.forward) ? 1f : 0f) - (Input.GetKey(bindings.back) ? 1f : 0f);
            Move = new Vector2(x, y);

            LookDelta = Cursor.lockState == CursorLockMode.Locked ? ReadMouseDelta() : Vector2.zero;

            JumpHeld = Input.GetKey(bindings.jump);
            JumpPressed = Input.GetKeyDown(bindings.jump);
            if (JumpPressed) lastJumpPressTime = Time.time;

            CrouchHeld = Input.GetKey(bindings.crouch) || Input.GetKey(bindings.crouchAlt);
            if (Input.GetKeyDown(bindings.crouch) || Input.GetKeyDown(bindings.crouchAlt)) crouchPressedLatch = true;

            SprintHeld = Input.GetKey(bindings.sprint);
            FireHeld = Input.GetKey(bindings.fire);
            FirePressed = Input.GetKeyDown(bindings.fire);
            AltFireHeld = Input.GetKey(bindings.altFire);
            AltFirePressed = Input.GetKeyDown(bindings.altFire);
            ReloadPressed = Input.GetKeyDown(bindings.reload);
            InteractPressed = Input.GetKeyDown(bindings.interact);
            GrappleHeld = Input.GetKey(bindings.grapple);
            GrapplePressed = Input.GetKeyDown(bindings.grapple);
            GrappleReleased = Input.GetKeyUp(bindings.grapple);

            float scroll = Input.mouseScrollDelta.y;
            NextWeapon = scroll < -0.01f;
            PreviousWeapon = scroll > 0.01f;

            WeaponSlotPressed = -1;
            for (int i = 0; i < 9; i++)
            {
                if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1 + i)))
                {
                    WeaponSlotPressed = i;
                    break;
                }
            }
        }

        static Vector2 ReadMouseDelta()
        {
            if (!mouseAxesAvailable) return Vector2.zero;
            try
            {
                var delta = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
                mouseAxesChecked = true;
                return delta;
            }
            catch (ArgumentException)
            {
                if (!mouseAxesChecked)
                {
                    Debug.LogError("[Momentum] Input axes 'Mouse X'/'Mouse Y' are missing. Run Tools > Parkour FPS > Setup Complete Game.");
                }
                mouseAxesChecked = true;
                mouseAxesAvailable = false;
                return Vector2.zero;
            }
        }

        void ClearFrameState(bool clearHeld)
        {
            Move = Vector2.zero;
            LookDelta = Vector2.zero;
            JumpPressed = FirePressed = AltFirePressed = ReloadPressed = InteractPressed = false;
            GrapplePressed = GrappleReleased = NextWeapon = PreviousWeapon = false;
            WeaponSlotPressed = -1;
            if (clearHeld)
            {
                JumpHeld = CrouchHeld = SprintHeld = FireHeld = AltFireHeld = GrappleHeld = false;
                lastJumpPressTime = -999f;
                crouchPressedLatch = false;
            }
        }

        /// <summary>True when jump was pressed within the buffer window and not yet consumed.</summary>
        public bool HasBufferedJump(float window)
        {
            return Time.time - lastJumpPressTime <= window;
        }

        public void ConsumeJump()
        {
            lastJumpPressTime = -999f;
        }

        public bool ConsumeCrouchPressed()
        {
            bool value = crouchPressedLatch;
            crouchPressedLatch = false;
            return value;
        }

        public void ResetState()
        {
            ClearFrameState(true);
        }
    }
}
