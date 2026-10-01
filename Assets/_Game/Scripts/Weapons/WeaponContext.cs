using Momentum.PlayerSystems;
using UnityEngine;

namespace Momentum.Weapons
{
    /// <summary>References a weapon needs from its owner. Built once by WeaponManager.</summary>
    public class WeaponContext
    {
        public PlayerController player;
        public PlayerMovement movement;
        public PlayerCamera playerCamera;
        public WeaponSway sway;
        /// <summary>Transform shots are fired from (the main camera).</summary>
        public Transform aim;
        public GameObject owner;
        public Transform ownerRoot;
    }
}
