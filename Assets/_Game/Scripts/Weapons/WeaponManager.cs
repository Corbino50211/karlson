using System.Collections.Generic;
using Momentum.Audio;
using Momentum.PlayerSystems;
using UnityEngine;

namespace Momentum.Weapons
{
    /// <summary>
    /// The player's weapon inventory: nine slots (one per WeaponDefinition.slot), pickups, swapping,
    /// ammo distribution and respawn refills. Weapon view models are instantiated under the weapon holder.
    /// </summary>
    public class WeaponManager : MonoBehaviour
    {
        public const int SlotCount = 9;

        [SerializeField] PlayerController player;
        [SerializeField] Transform weaponHolder;
        [SerializeField] WeaponSway sway;

        readonly WeaponBase[] slots = new WeaponBase[SlotCount];
        WeaponContext context;
        int currentSlot = -1;

        public WeaponBase Current { get; private set; }
        public int CurrentSlot => currentSlot;
        public Transform WeaponHolder => weaponHolder;

        public IEnumerable<WeaponBase> Weapons
        {
            get
            {
                foreach (var w in slots)
                {
                    if (w != null) yield return w;
                }
            }
        }

        public int WeaponCount
        {
            get
            {
                int n = 0;
                foreach (var w in slots)
                {
                    if (w != null) n++;
                }
                return n;
            }
        }

        void Awake()
        {
            if (player == null) player = GetComponent<PlayerController>();
        }

        WeaponContext Context
        {
            get
            {
                if (context == null)
                {
                    context = new WeaponContext
                    {
                        player = player,
                        movement = player != null ? player.Movement : null,
                        playerCamera = player != null ? player.PlayerCamera : null,
                        sway = sway,
                        owner = gameObject,
                        ownerRoot = transform
                    };
                    context.aim = context.playerCamera != null ? context.playerCamera.CameraTransform : transform;
                }
                return context;
            }
        }

        public WeaponBase GetSlot(int index)
        {
            return index >= 0 && index < SlotCount ? slots[index] : null;
        }

        public bool HasWeapon(WeaponDefinition def)
        {
            if (def == null) return false;
            var w = GetSlot(def.slot - 1);
            return w != null && w.Definition == def;
        }

        /// <summary>
        /// Gives a weapon. Returns true if the weapon was new. If already owned, ammo is added instead
        /// (ammoAdded reports whether anything changed).
        /// </summary>
        public bool GiveWeapon(WeaponDefinition def, bool equip, out bool ammoAdded)
        {
            ammoAdded = false;
            if (def == null) return false;
            int index = Mathf.Clamp(def.slot - 1, 0, SlotCount - 1);
            var existing = slots[index];
            if (existing != null && existing.Definition == def)
            {
                ammoAdded = existing.AddAmmo(def.pickupAmmo) > 0;
                return false;
            }
            if (def.viewModelPrefab == null)
            {
                Debug.LogWarning($"[Momentum] Weapon '{def.displayName}' has no view model prefab. Run the setup tool.");
                return false;
            }
            if (existing != null)
            {
                if (existing == Current)
                {
                    Current = null;
                    currentSlot = -1;
                }
                Destroy(existing.gameObject);
            }

            var go = Instantiate(def.viewModelPrefab, weaponHolder != null ? weaponHolder : transform, false);
            Layers.SetLayerRecursively(go, Layers.ViewModel);
            var weapon = go.GetComponent<WeaponBase>();
            if (weapon == null)
            {
                Debug.LogWarning($"[Momentum] View model for '{def.displayName}' has no WeaponBase component.");
                Destroy(go);
                return false;
            }
            weapon.Initialize(Context, def);
            slots[index] = weapon;
            GameEvents.RaiseWeaponPickedUp(def);
            if (equip || Current == null) EquipSlot(index);
            return true;
        }

        public bool GiveWeapon(WeaponDefinition def, bool equip = true)
        {
            return GiveWeapon(def, equip, out _);
        }

        public void EquipSlot(int index)
        {
            if (index < 0 || index >= SlotCount) return;
            var weapon = slots[index];
            if (weapon == null || index == currentSlot) return;
            if (Current != null) Current.Unequip();
            currentSlot = index;
            Current = weapon;
            Current.Equip();
            AudioManager.Play2D(SoundId.WeaponSwitch, 0.5f);
        }

        public void Cycle(int direction)
        {
            if (WeaponCount == 0) return;
            int start = currentSlot < 0 ? 0 : currentSlot;
            for (int i = 1; i <= SlotCount; i++)
            {
                int index = ((start + direction * i) % SlotCount + SlotCount) % SlotCount;
                if (slots[index] != null)
                {
                    EquipSlot(index);
                    return;
                }
            }
        }

        /// <summary>Adds a fraction of each owned weapon's max reserve. Returns true if anything was added.</summary>
        public bool AddAmmoToAll(float fractionOfMax)
        {
            bool any = false;
            foreach (var w in Weapons)
            {
                if (w.HasInfiniteReserve) continue;
                int amount = Mathf.CeilToInt(w.Definition.maxReserve * fractionOfMax);
                if (w.AddAmmo(amount) > 0) any = true;
            }
            return any;
        }

        public bool AnyWeaponNeedsAmmo()
        {
            foreach (var w in Weapons)
            {
                if (w.NeedsAmmo) return true;
            }
            return false;
        }

        public void OnOwnerDied()
        {
            if (Current != null)
            {
                Current.CancelReload();
                Current.SetAiming(false);
            }
        }

        /// <summary>Respawn: keep weapons, refill magazines, cancel reloads.</summary>
        public void OnRespawn()
        {
            foreach (var w in Weapons)
            {
                w.CancelReload();
                w.RefillMagazine();
            }
            if (Current != null)
            {
                Current.SetAiming(false);
                GameEvents.RaiseWeaponEquipped(Current);
            }
        }

        public void ClearAll()
        {
            for (int i = 0; i < SlotCount; i++)
            {
                if (slots[i] != null) Destroy(slots[i].gameObject);
                slots[i] = null;
            }
            Current = null;
            currentSlot = -1;
            GameEvents.RaiseWeaponEquipped(null);
        }
    }
}
