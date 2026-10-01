using System.Collections.Generic;
using UnityEngine;

namespace Momentum.Levels
{
    /// <summary>Player start position. The marker visual is only visible in the editor.</summary>
    public class SpawnPoint : MonoBehaviour
    {
        public static readonly List<SpawnPoint> All = new List<SpawnPoint>();

        [SerializeField] GameObject markerVisual;

        public Vector3 FeetPosition => transform.position;
        public float Yaw => transform.eulerAngles.y;

        public static SpawnPoint First => All.Count > 0 ? All[0] : null;

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
            if (markerVisual != null) markerVisual.SetActive(false);
        }

        void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(transform.position + Vector3.up, new Vector3(0.8f, 2f, 0.8f));
            Gizmos.DrawLine(transform.position + Vector3.up, transform.position + Vector3.up + transform.forward * 1.5f);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            All.Clear();
        }
    }
}
