using Momentum.Levels;
using Momentum.PlayerSystems;
using UnityEngine;

namespace Momentum.Weapons
{
    /// <summary>Ammo box: refills a fraction of every owned weapon's reserve ammo.</summary>
    public class AmmoPickup : PickupBase
    {
        [Tooltip("Fraction of each weapon's max reserve granted.")]
        [SerializeField] float fraction = 0.4f;

        protected override string PromptText => "Take ammo";

        protected override bool Apply(PlayerController player)
        {
            if (player.Weapons == null) return false;
            bool added = player.Weapons.AddAmmoToAll(fraction);
            if (added) GameEvents.RaiseNotification("+AMMO", new Color(1f, 0.85f, 0.3f));
            return added;
        }

        protected override void ApplyCustomProperties(LevelObjectData data)
        {
            fraction = Mathf.Clamp(data.GetFloat("amount", fraction), 0.05f, 1f);
        }
    }
}
