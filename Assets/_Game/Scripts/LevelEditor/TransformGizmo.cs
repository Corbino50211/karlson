using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Momentum.LevelEditor
{
    public enum GizmoMode
    {
        Move,
        Rotate,
        Scale
    }

    /// <summary>
    /// Runtime transform gizmo for the level editor: axis arrows + XZ plane handle (move), rings (rotate)
    /// and axis/center cubes (scale). Built procedurally, drawn on top of geometry, constant screen size.
    /// Drags are forwarded to LevelEditorManager which applies snapping and records undo.
    /// </summary>
    public class TransformGizmo : MonoBehaviour
    {
        enum HandleKind { Axis, Plane, Center }

        struct Handle
        {
            public int axis;
            public HandleKind kind;
            public GizmoMode mode;
            public Renderer[] renderers;
            public Material material;
        }

        static readonly Vector3[] Axes = { Vector3.right, Vector3.up, Vector3.forward };

        readonly Dictionary<Collider, Handle> handles = new Dictionary<Collider, Handle>();
        Transform moveRoot;
        Transform rotateRoot;
        Transform scaleRoot;
        LevelEditorManager manager;
        Camera cam;
        Material highlight;
        GizmoMode mode = GizmoMode.Move;
        bool visible;

        bool dragging;
        Handle dragHandle;
        Vector3 dragPivot;
        Vector3 dragAxis;
        float startParam;
        Vector3 startPoint;
        Vector3 startVector;
        Vector2 startMouse;
        float handleLength = 1f;
        Collider hovered;

        public bool IsDragging => dragging;
        public GizmoMode Mode => mode;

        public void Initialize(LevelEditorManager owner, Camera editorCamera)
        {
            manager = owner;
            cam = editorCamera;
            Build();
            SetVisible(false);
        }

        // ------------------------------------------------------------------ Construction

        void Build()
        {
            var lib = MaterialLibrary.Instance;
            var mats = new Material[3];
            mats[0] = lib != null && lib.gizmoX != null ? lib.gizmoX : FallbackMaterial(Color.red);
            mats[1] = lib != null && lib.gizmoY != null ? lib.gizmoY : FallbackMaterial(Color.green);
            mats[2] = lib != null && lib.gizmoZ != null ? lib.gizmoZ : FallbackMaterial(Color.blue);
            var center = lib != null && lib.gizmoCenter != null ? lib.gizmoCenter : FallbackMaterial(Color.yellow);
            highlight = lib != null && lib.gizmoHighlight != null ? lib.gizmoHighlight : FallbackMaterial(Color.white);

            Mesh cylinder = PrimitiveMesh(PrimitiveType.Cylinder);
            Mesh cube = PrimitiveMesh(PrimitiveType.Cube);
            Mesh cone = MeshGenerator.Cone;
            Mesh torus = MeshGenerator.CreateTorus(1f, 0.025f, 64, 6);
            Mesh torusCollider = MeshGenerator.CreateTorus(1f, 0.1f, 32, 6);

            moveRoot = Child("Move");
            rotateRoot = Child("Rotate");
            scaleRoot = Child("Scale");

            for (int i = 0; i < 3; i++)
            {
                Quaternion toAxis = Quaternion.FromToRotation(Vector3.up, Axes[i]);

                // Move arrow
                var arrow = Child("MoveAxis" + i, moveRoot);
                var shaft = Visual("Shaft", arrow, cylinder, mats[i], Axes[i] * 0.5f, toAxis, new Vector3(0.035f, 0.5f, 0.035f));
                var tip = Visual("Tip", arrow, cone, mats[i], Axes[i] * 1f, toAxis, new Vector3(0.24f, 0.28f, 0.24f));
                AddBox(arrow, Axes[i] * 0.62f, toAxis, new Vector3(0.18f, 1.25f, 0.18f), new Handle { axis = i, kind = HandleKind.Axis, mode = GizmoMode.Move, renderers = new[] { shaft, tip }, material = mats[i] });

                // Rotate ring
                var ring = Child("RotateAxis" + i, rotateRoot);
                var ringRenderer = Visual("Ring", ring, torus, mats[i], Vector3.zero, toAxis, Vector3.one);
                var colHolder = new GameObject("RingCollider");
                colHolder.layer = Layers.EditorGizmo;
                colHolder.transform.SetParent(ring, false);
                colHolder.transform.localRotation = toAxis;
                var mc = colHolder.AddComponent<MeshCollider>();
                mc.sharedMesh = torusCollider;
                handles[mc] = new Handle { axis = i, kind = HandleKind.Axis, mode = GizmoMode.Rotate, renderers = new[] { ringRenderer }, material = mats[i] };

                // Scale handle
                var scaleAxis = Child("ScaleAxis" + i, scaleRoot);
                var sShaft = Visual("Shaft", scaleAxis, cylinder, mats[i], Axes[i] * 0.5f, toAxis, new Vector3(0.035f, 0.5f, 0.035f));
                var sTip = Visual("Tip", scaleAxis, cube, mats[i], Axes[i] * 1f, toAxis, Vector3.one * 0.16f);
                AddBox(scaleAxis, Axes[i] * 0.6f, toAxis, new Vector3(0.2f, 1.2f, 0.2f), new Handle { axis = i, kind = HandleKind.Axis, mode = GizmoMode.Scale, renderers = new[] { sShaft, sTip }, material = mats[i] });
            }

            var plane = Child("MovePlane", moveRoot);
            var planeVis = Visual("Quad", plane, cube, center, new Vector3(0.28f, 0f, 0.28f), Quaternion.identity, new Vector3(0.3f, 0.02f, 0.3f));
            AddBox(plane, new Vector3(0.28f, 0f, 0.28f), Quaternion.identity, new Vector3(0.32f, 0.08f, 0.32f), new Handle { axis = 1, kind = HandleKind.Plane, mode = GizmoMode.Move, renderers = new[] { planeVis }, material = center });

            var uniform = Child("ScaleCenter", scaleRoot);
            var uniVis = Visual("Cube", uniform, cube, center, Vector3.zero, Quaternion.identity, Vector3.one * 0.2f);
            AddBox(uniform, Vector3.zero, Quaternion.identity, Vector3.one * 0.26f, new Handle { axis = -1, kind = HandleKind.Center, mode = GizmoMode.Scale, renderers = new[] { uniVis }, material = center });
        }

        static Mesh PrimitiveMesh(PrimitiveType type)
        {
            var temp = GameObject.CreatePrimitive(type);
            var mesh = temp.GetComponent<MeshFilter>().sharedMesh;
            Destroy(temp);
            return mesh;
        }

        static Material FallbackMaterial(Color c)
        {
            var shader = Shader.Find("Momentum/GizmoOverlay");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            var m = new Material(shader);
            m.color = c;
            return m;
        }

        Transform Child(string childName, Transform parent = null)
        {
            var go = new GameObject(childName);
            go.layer = Layers.EditorGizmo;
            go.transform.SetParent(parent != null ? parent : transform, false);
            return go.transform;
        }

        Renderer Visual(string visualName, Transform parent, Mesh mesh, Material mat, Vector3 pos, Quaternion rot, Vector3 scale)
        {
            var go = new GameObject(visualName);
            go.layer = Layers.EditorGizmo;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = rot;
            go.transform.localScale = scale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return r;
        }

        void AddBox(Transform parent, Vector3 center, Quaternion rot, Vector3 size, Handle handle)
        {
            var go = new GameObject("Collider");
            go.layer = Layers.EditorGizmo;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            go.transform.localRotation = rot;
            var box = go.AddComponent<BoxCollider>();
            box.size = size;
            handles[box] = handle;
        }

        // ------------------------------------------------------------------ Per frame

        public void SetMode(GizmoMode newMode)
        {
            if (dragging) return;
            mode = newMode;
            ApplyVisibility();
        }

        public void SetVisible(bool value)
        {
            visible = value;
            if (!value && dragging) EndDrag();
            ApplyVisibility();
        }

        void ApplyVisibility()
        {
            if (moveRoot == null) return;
            moveRoot.gameObject.SetActive(visible && mode == GizmoMode.Move);
            rotateRoot.gameObject.SetActive(visible && mode == GizmoMode.Rotate);
            scaleRoot.gameObject.SetActive(visible && mode == GizmoMode.Scale);
        }

        /// <summary>Positions the gizmo on the current selection.</summary>
        public void UpdatePlacement(Vector3 pivot, Quaternion primaryRotation)
        {
            if (!dragging || dragHandle.mode == GizmoMode.Move) transform.position = pivot;
            transform.rotation = mode == GizmoMode.Scale ? primaryRotation : Quaternion.identity;
            if (cam != null)
            {
                float d = Vector3.Distance(cam.transform.position, transform.position);
                float s = Mathf.Max(0.3f, d * 0.12f);
                transform.localScale = Vector3.one * s;
                handleLength = s;
            }
        }

        /// <summary>Handles hover/drag. Returns true when the mouse interaction belongs to the gizmo.</summary>
        public bool ProcessInput(Ray ray, bool pointerOverUI)
        {
            if (!visible) return false;
            if (dragging)
            {
                if (Input.GetMouseButton(0)) Drag(ray);
                else EndDrag();
                return true;
            }

            Collider newHover = null;
            if (!pointerOverUI && Physics.Raycast(ray, out RaycastHit hit, 5000f, 1 << Layers.EditorGizmo, QueryTriggerInteraction.Collide) &&
                handles.ContainsKey(hit.collider) && handles[hit.collider].mode == mode)
            {
                newHover = hit.collider;
            }
            SetHover(newHover);

            if (hovered != null && Input.GetMouseButtonDown(0))
            {
                BeginDrag(handles[hovered], ray);
                return true;
            }
            return hovered != null;
        }

        void SetHover(Collider c)
        {
            if (hovered == c) return;
            if (hovered != null && handles.TryGetValue(hovered, out var old)) Paint(old, false);
            hovered = c;
            if (hovered != null && handles.TryGetValue(hovered, out var h)) Paint(h, true);
        }

        void Paint(Handle h, bool highlighted)
        {
            foreach (var r in h.renderers)
            {
                if (r != null) r.sharedMaterial = highlighted ? highlight : h.material;
            }
        }

        void BeginDrag(Handle handle, Ray ray)
        {
            dragging = true;
            dragHandle = handle;
            dragPivot = transform.position;
            startMouse = Input.mousePosition;
            manager.BeginTransformDrag();

            switch (handle.mode)
            {
                case GizmoMode.Move:
                    if (handle.kind == HandleKind.Plane)
                    {
                        startPoint = PlanePoint(ray, Vector3.up, dragPivot);
                    }
                    else
                    {
                        dragAxis = Axes[handle.axis];
                        startParam = MathUtil.ClosestParameterOnLineToRay(dragPivot, dragAxis, ray);
                    }
                    break;
                case GizmoMode.Rotate:
                    dragAxis = Axes[handle.axis];
                    startVector = PlanePoint(ray, dragAxis, dragPivot) - dragPivot;
                    break;
                case GizmoMode.Scale:
                    if (handle.kind == HandleKind.Axis)
                    {
                        dragAxis = transform.rotation * Axes[handle.axis];
                        startParam = MathUtil.ClosestParameterOnLineToRay(dragPivot, dragAxis, ray);
                    }
                    break;
            }
        }

        void Drag(Ray ray)
        {
            switch (dragHandle.mode)
            {
                case GizmoMode.Move:
                    if (dragHandle.kind == HandleKind.Plane)
                    {
                        Vector3 p = PlanePoint(ray, Vector3.up, dragPivot);
                        Vector3 delta = p - startPoint;
                        delta.y = 0f;
                        manager.DragMove(delta);
                    }
                    else
                    {
                        float t = MathUtil.ClosestParameterOnLineToRay(dragPivot, dragAxis, ray);
                        manager.DragMove(dragAxis * (t - startParam));
                    }
                    break;
                case GizmoMode.Rotate:
                    {
                        Vector3 v = PlanePoint(ray, dragAxis, dragPivot) - dragPivot;
                        if (v.sqrMagnitude < 0.0001f || startVector.sqrMagnitude < 0.0001f) return;
                        float angle = Vector3.SignedAngle(startVector, v, dragAxis);
                        manager.DragRotate(dragAxis, angle, dragPivot);
                    }
                    break;
                case GizmoMode.Scale:
                    if (dragHandle.kind == HandleKind.Center)
                    {
                        float factor = 1f + ((Vector2)Input.mousePosition - startMouse).y / 200f;
                        manager.DragScale(-1, Mathf.Max(0.05f, factor));
                    }
                    else
                    {
                        float t = MathUtil.ClosestParameterOnLineToRay(dragPivot, dragAxis, ray);
                        float factor = 1f + (t - startParam) / Mathf.Max(0.1f, handleLength);
                        manager.DragScale(dragHandle.axis, Mathf.Max(0.05f, factor));
                    }
                    break;
            }
        }

        void EndDrag()
        {
            dragging = false;
            manager.EndTransformDrag();
        }

        static Vector3 PlanePoint(Ray ray, Vector3 normal, Vector3 point)
        {
            var plane = new Plane(normal, point);
            if (plane.Raycast(ray, out float enter)) return ray.GetPoint(enter);
            // Ray parallel to the plane: project the far point onto it.
            return plane.ClosestPointOnPlane(ray.GetPoint(50f));
        }

        public static bool PointerOverUI()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }
    }
}
