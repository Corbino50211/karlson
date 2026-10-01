using System;
using UnityEngine;

namespace Momentum.PlayerSystems
{
    /// <summary>Keyboard / mouse bindings for the player. Defaults follow the game's documented controls.</summary>
    [Serializable]
    public class KeyBindings
    {
        public KeyCode forward = KeyCode.W;
        public KeyCode back = KeyCode.S;
        public KeyCode left = KeyCode.A;
        public KeyCode right = KeyCode.D;
        public KeyCode jump = KeyCode.Space;
        public KeyCode crouch = KeyCode.LeftControl;
        public KeyCode crouchAlt = KeyCode.C;
        public KeyCode sprint = KeyCode.LeftShift;
        public KeyCode fire = KeyCode.Mouse0;
        public KeyCode altFire = KeyCode.Mouse1;
        public KeyCode reload = KeyCode.R;
        public KeyCode interact = KeyCode.E;
        public KeyCode grapple = KeyCode.Q;
        public KeyCode pause = KeyCode.Escape;
        public KeyCode debugOverlay = KeyCode.F3;
        public KeyCode restartCheckpoint = KeyCode.T;
    }
}
