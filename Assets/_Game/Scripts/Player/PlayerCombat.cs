using Momentum.Weapons;
using UnityEngine;

namespace Momentum.PlayerSystems
{
    /// <summary>
    /// Bridges player input and the weapon inventory: firing, alt-fire, reloading and weapon switching
    /// (number keys 1-9 and mouse wheel).
    /// </summary>
    public class PlayerCombat : MonoBehaviour
    {
        [SerializeField] PlayerController player;
        [SerializeField] WeaponManager weapons;

        public WeaponManager Weapons => weapons;

        void Awake()
        {
            if (player == null) player = GetComponent<PlayerController>();
            if (weapons == null) weapons = GetComponent<WeaponManager>();
        }

        void Update()
        {
            if (player == null || weapons == null || player.IsDead) return;
            if (GameManager.Instance != null && GameManager.Instance.IsPaused) return;
            var input = player.InputHandler;
            if (input == null || !input.InputEnabled) return;

            if (input.WeaponSlotPressed >= 0) weapons.EquipSlot(input.WeaponSlotPressed);
            else if (input.NextWeapon) weapons.Cycle(1);
            else if (input.PreviousWeapon) weapons.Cycle(-1);

            var current = weapons.Current;
            if (current != null) current.Tick(input);
        }
    }
}
