using UnityEngine;

namespace Momentum.LevelEditor
{
    /// <summary>Large translucent grid plane at y = 0 that follows the camera and matches the snap size.</summary>
    public class EditorGrid : MonoBehaviour
    {
        [SerializeField] float size = 400f;

        MeshRenderer meshRenderer;
        Material instance;
        Transform followTarget;
        float cellSize = 1f;

        void Awake()
        {
            meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer == null)
            {
                gameObject.AddComponent<MeshFilter>().sharedMesh = BuildQuad();
                meshRenderer = gameObject.AddComponent<MeshRenderer>();
            }
            var library = MaterialLibrary.Instance;
            var source = library != null ? library.editorGrid : null;
            if (source != null)
            {
                instance = new Material(source);
                meshRenderer.sharedMaterial = instance;
            }
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            gameObject.layer = Layers.EditorGizmo;
            transform.localScale = Vector3.one;
            SetCellSize(1f);
        }

        void OnDestroy()
        {
            if (instance != null) Destroy(instance);
        }

        static Mesh BuildQuad()
        {
            var mesh = new Mesh { name = "GridQuad" };
            mesh.vertices = new[]
            {
                new Vector3(-0.5f, 0f, -0.5f), new Vector3(-0.5f, 0f, 0.5f), new Vector3(0.5f, 0f, 0.5f), new Vector3(0.5f, 0f, -0.5f)
            };
            mesh.uv = new[] { new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f) };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        public void SetFollowTarget(Transform target)
        {
            followTarget = target;
        }

        public void SetCellSize(float cell)
        {
            cellSize = Mathf.Max(0.05f, cell);
            transform.localScale = new Vector3(size, 1f, size);
            if (instance != null) instance.mainTextureScale = new Vector2(size / cellSize, size / cellSize);
        }

        void LateUpdate()
        {
            if (followTarget == null) return;
            float snap = cellSize * 4f;
            Vector3 p = followTarget.position;
            transform.position = new Vector3(Mathf.Round(p.x / snap) * snap, 0.01f, Mathf.Round(p.z / snap) * snap);
        }
    }
}
