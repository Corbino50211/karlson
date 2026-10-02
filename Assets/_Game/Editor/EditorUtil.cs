using System.IO;
using UnityEditor;
using UnityEngine;

namespace Momentum.EditorTools
{
    /// <summary>Shared helpers for the generators: folders, assets, prefabs, serialized field wiring.</summary>
    public static class EditorUtil
    {
        static Mesh cubeMesh, cylinderMesh, sphereMesh, capsuleMesh, quadMesh;

        // ------------------------------------------------------------------ Folders & assets

        public static void EnsureFolder(string path)
        {
            path = path.Replace('\\', '/').TrimEnd('/');
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string name = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        /// <summary>Loads a ScriptableObject asset or creates it. Existing assets keep their values.</summary>
        public static T LoadOrCreate<T>(string path, out bool created) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            created = asset == null;
            if (asset == null)
            {
                EnsureFolder(Path.GetDirectoryName(path));
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
            }
            return asset;
        }

        public static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            return LoadOrCreate<T>(path, out _);
        }

        /// <summary>Creates or overwrites an asset in place (keeps GUID so references survive).</summary>
        public static T SaveAsset<T>(T asset, string path) where T : Object
        {
            EnsureFolder(Path.GetDirectoryName(path));
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(asset, path);
                return asset;
            }
            if (existing == asset) return existing;
            EditorUtility.CopySerialized(asset, existing);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        /// <summary>Saves a hierarchy as a prefab (overwriting in place) and destroys the temporary object.</summary>
        public static GameObject SavePrefab(GameObject root, string path)
        {
            EnsureFolder(Path.GetDirectoryName(path));
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path, out bool success);
            Object.DestroyImmediate(root);
            if (!success) Debug.LogError("[Momentum Setup] Failed to save prefab " + path);
            return prefab;
        }

        public static GameObject LoadPrefab(string path)
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        /// <summary>Throws a clear error when a required asset is missing or was unloaded by Unity.</summary>
        public static T Require<T>(T asset, string description) where T : Object
        {
            if (asset == null)
            {
                throw new System.InvalidOperationException(description + " is missing or was unloaded. Run Tools > Parkour FPS > Setup Complete Game again.");
            }
            return asset;
        }

        // ------------------------------------------------------------------ Builtin meshes

        static Mesh Primitive(PrimitiveType type, ref Mesh cache)
        {
            if (cache != null) return cache;
            var temp = GameObject.CreatePrimitive(type);
            cache = temp.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(temp);
            return cache;
        }

        public static Mesh CubeMesh => Primitive(PrimitiveType.Cube, ref cubeMesh);
        public static Mesh CylinderMesh => Primitive(PrimitiveType.Cylinder, ref cylinderMesh);
        public static Mesh SphereMesh => Primitive(PrimitiveType.Sphere, ref sphereMesh);
        public static Mesh CapsuleMesh => Primitive(PrimitiveType.Capsule, ref capsuleMesh);
        public static Mesh QuadMesh => Primitive(PrimitiveType.Quad, ref quadMesh);

        // ------------------------------------------------------------------ Object building

        public static GameObject Create(string name, Transform parent = null, int layer = 0)
        {
            var go = new GameObject(name);
            go.layer = layer;
            if (parent != null) go.transform.SetParent(parent, false);
            return go;
        }

        /// <summary>Creates a visual child with a mesh and material (no collider).</summary>
        public static GameObject Visual(string name, Transform parent, Mesh mesh, Material material, Vector3 localPos, Vector3 localScale, Vector3 localEuler = default, int layer = -1)
        {
            var go = new GameObject(name);
            go.layer = layer >= 0 ? layer : (parent != null ? parent.gameObject.layer : 0);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(localEuler);
            go.transform.localScale = localScale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = material;
            return go;
        }

        public static GameObject Box(string name, Transform parent, Material material, Vector3 localPos, Vector3 localScale, Vector3 localEuler = default)
        {
            return Visual(name, parent, CubeMesh, material, localPos, localScale, localEuler);
        }

        public static GameObject Cylinder(string name, Transform parent, Material material, Vector3 localPos, Vector3 localScale, Vector3 localEuler = default)
        {
            return Visual(name, parent, CylinderMesh, material, localPos, localScale, localEuler);
        }

        public static GameObject Sphere(string name, Transform parent, Material material, Vector3 localPos, Vector3 localScale)
        {
            return Visual(name, parent, SphereMesh, material, localPos, localScale);
        }

        public static void SetLayerRecursive(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform t in go.transform) SetLayerRecursive(t.gameObject, layer);
        }

        public static void DisableShadows(GameObject go)
        {
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
            }
        }

        // ------------------------------------------------------------------ Serialized field wiring

        static SerializedProperty Prop(Object target, string field, out SerializedObject so)
        {
            so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null) Debug.LogError($"[Momentum Setup] Field '{field}' not found on {target.GetType().Name}.");
            return p;
        }

        public static void Set(Object target, string field, Object value)
        {
            var p = Prop(target, field, out var so);
            if (p == null) return;
            p.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void Set(Object target, string field, float value)
        {
            var p = Prop(target, field, out var so);
            if (p == null) return;
            p.floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void Set(Object target, string field, int value)
        {
            var p = Prop(target, field, out var so);
            if (p == null) return;
            // intValue writes the underlying value for enums too (works with non-contiguous enums).
            p.intValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void Set(Object target, string field, bool value)
        {
            var p = Prop(target, field, out var so);
            if (p == null) return;
            p.boolValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void Set(Object target, string field, string value)
        {
            var p = Prop(target, field, out var so);
            if (p == null) return;
            p.stringValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void Set(Object target, string field, Color value)
        {
            var p = Prop(target, field, out var so);
            if (p == null) return;
            p.colorValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void Set(Object target, string field, Vector3 value)
        {
            var p = Prop(target, field, out var so);
            if (p == null) return;
            p.vector3Value = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetArray(Object target, string field, Object[] values)
        {
            var p = Prop(target, field, out var so);
            if (p == null) return;
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ------------------------------------------------------------------ Textures

        /// <summary>Writes a texture to a PNG asset and configures the importer.</summary>
        public static Texture2D SaveTexture(Texture2D tex, string path, TextureWrapMode wrap, bool alphaIsTransparency, bool mipmaps = true)
        {
            EnsureFolder(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Default;
                importer.alphaIsTransparency = alphaIsTransparency;
                importer.wrapMode = wrap;
                importer.mipmapEnabled = mipmaps;
                importer.anisoLevel = 4;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}
