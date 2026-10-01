using Momentum.Audio;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Momentum.UI
{
    /// <summary>Plays hover/click sounds for a selectable (works while paused).</summary>
    public class UIButtonSounds : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
    {
        [SerializeField] bool isBackButton;

        Selectable selectable;

        void Awake()
        {
            selectable = GetComponent<Selectable>();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (selectable != null && !selectable.interactable) return;
            AudioManager.PlayUI(SoundId.UIHover, 0.5f);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (selectable != null && !selectable.interactable) return;
            AudioManager.PlayUI(isBackButton ? SoundId.UIBack : SoundId.UIClick, 0.8f);
        }

        public void SetBackButton(bool value)
        {
            isBackButton = value;
        }
    }
}
