using UnityEngine;

namespace Momentum.PlayerSystems
{
    /// <summary>Looks for IInteractable objects under the crosshair, shows a prompt and handles the interact key.</summary>
    public class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] PlayerController player;
        [SerializeField] float range = 3.2f;
        [SerializeField] float radius = 0.25f;

        IInteractable current;
        string currentPrompt;

        void Awake()
        {
            if (player == null) player = GetComponent<PlayerController>();
        }

        void OnDisable()
        {
            SetPrompt(null);
            current = null;
        }

        void Update()
        {
            if (player == null || player.IsDead || player.PlayerCamera == null)
            {
                SetPrompt(null);
                return;
            }

            var cam = player.PlayerCamera.CameraTransform;
            IInteractable found = null;
            if (Physics.SphereCast(cam.position, radius, cam.forward, out RaycastHit hit, range, Layers.InteractMask, QueryTriggerInteraction.Collide))
            {
                found = hit.collider.GetComponentInParent<IInteractable>();
                if (found != null && !found.CanInteract(player)) found = null;
            }

            current = found;
            SetPrompt(found != null ? found.GetInteractPrompt(player) : null);

            if (found != null && player.InputHandler != null && player.InputHandler.InteractPressed)
            {
                found.Interact(player);
            }
        }

        void SetPrompt(string prompt)
        {
            if (prompt == currentPrompt) return;
            currentPrompt = prompt;
            GameEvents.RaiseInteractPrompt(prompt);
        }
    }
}
