using System.Collections.Generic;
using UnityEngine;

namespace Momentum.Levels
{
    /// <summary>
    /// Dedicated grapple anchor. Grapple points are magnetically targeted within a small cone, so they
    /// are easy to hit at speed. Any Environment surface is also grappleable.
    /// </summary>
    public class GrapplePoint : MonoBehaviour
    {
        public static readonly List<GrapplePoint> All = new List<GrapplePoint>();

        [SerializeField] Renderer highlightRenderer;
        [SerializeField] Color idleColor = new Color(0.1f, 0.8f, 1f);
        [SerializeField] Color targetedColor = new Color(1f, 0.9f, 0.2f);
        [SerializeField] Vector3 anchorOffset = Vector3.zero;

        MaterialPropertyBlock block;
        bool targeted;

        public Vector3 AnchorPosition => transform.TransformPoint(anchorOffset);

        void OnEnable()
        {
            if (!All.Contains(this)) All.Add(this);
            ApplyColor(idleColor);
        }

        void OnDisable()
        {
            All.Remove(this);
        }

        public void SetTargeted(bool value)
        {
            if (targeted == value) return;
            targeted = value;
            ApplyColor(value ? targetedColor : idleColor);
        }

        void ApplyColor(Color c)
        {
            if (highlightRenderer == null) return;
            if (block == null) block = new MaterialPropertyBlock();
            highlightRenderer.GetPropertyBlock(block);
            block.SetColor("_Color", c);
            block.SetColor("_EmissionColor", c * 2f);
            highlightRenderer.SetPropertyBlock(block);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            All.Clear();
        }
    }
}
