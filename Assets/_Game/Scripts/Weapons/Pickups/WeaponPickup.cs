using Momentum.Audio;
using Momentum.Levels;
using Momentum.PlayerSystems;
using UnityEngine;

namespace Momentum.Weapons
{
    /// <summary>
    /// A physical weapon in the level. Walk into it (or press E) to equip it. If the weapon is already
    /// owned it gives ammo instead.
    /// </summary>
    public class WeaponPickup : PickupBase
    {
        const string DisplayName = "WeaponDisplay";

        [SerializeField] WeaponDefinition weapon;
        [SerializeField] Transform displayRoot;

        public WeaponDefinition Weapon => weapon;

        void Reset()
        {
            collectSound = SoundId.WeaponPickup;
        }

        void Start()
        {
            RefreshDisplay();
        }

        public void SetWeapon(WeaponDefinition def)
        {
            weapon = def;
            RefreshDisplay();
        }

        protected override string PromptText => weapon != null ? "Pick up " + weapon.displayName : null;

        protected override bool Apply(PlayerController player)
        {
            if (weapon == null || player.Weapons == null) return false;
            bool isNew = player.Weapons.GiveWeapon(weapon, true, out bool ammoAdded);
            if (isNew)
            {
                GameEvents.RaiseNotification(weapon.displayName.ToUpperInvariant(), weapon.accentColor);
                AudioManager.Play2D(SoundId.WeaponPickup, 0.8f);
                return true;
            }
            if (ammoAdded)
            {
                GameEvents.RaiseNotification("+" + weapon.displayName + " AMMO", new Color(0.8f, 0.8f, 0.8f));
                return true;
            }
            return false;
        }

        protected override void ApplyCustomProperties(LevelObjectData data)
        {
            var registry = PrefabRegistry.Instance;
            string id = data.GetString("weapon", weapon != null ? weapon.id : "shotgun");
            if (registry != null)
            {
                var def = registry.GetWeapon(id);
                if (def != null) weapon = def;
            }
            RefreshDisplay();
        }

        /// <summary>Spawns the weapon's world model on the pedestal.</summary>
        public void RefreshDisplay()
        {
            var root = displayRoot != null ? displayRoot : transform;
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                var child = root.GetChild(i);
                if (child.name != DisplayName) continue;
                if (Application.isPlaying) Destroy(child.gameObject);
                else DestroyImmediate(child.gameObject);
            }
            if (weapon == null || weapon.worldModelPrefab == null) return;
            var model = Instantiate(weapon.worldModelPrefab, root, false);
            model.name = DisplayName;
            foreach (var c in model.GetComponentsInChildren<Collider>(true))
            {
                if (Application.isPlaying) Destroy(c);
                else DestroyImmediate(c);
            }
        }
    }
}
