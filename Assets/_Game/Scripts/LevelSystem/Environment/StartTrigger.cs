using System.Collections.Generic;
using Momentum.PlayerSystems;
using UnityEngine;

namespace Momentum.Levels
{
    /// <summary>Start gate: the run timer starts when the player passes through it.</summary>
    public class StartTrigger : MonoBehaviour
    {
        public static readonly List<StartTrigger> All = new List<StartTrigger>();

        [SerializeField] GameObject editorVisual;

        void OnEnable()
        {
            if (!All.Contains(this)) All.Add(this);
        }

        void OnDisable()
        {
            All.Remove(this);
        }

        void Start()
        {
            if (editorVisual != null) editorVisual.SetActive(false);
        }

        void OnTriggerEnter(Collider other)
        {
            var player = other.GetComponentInParent<PlayerController>();
            if (player != null && !player.IsDead) GameEvents.RaiseStartLineCrossed();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            All.Clear();
        }
    }
}
