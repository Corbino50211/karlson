using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Momentum.EditorTools
{
    /// <summary>
    /// Generates the visual palette: world-grid architecture materials, accent/emissive materials,
    /// transparent helpers, particle/line materials, gizmo materials, skyboxes and the procedural
    /// textures they use. Fills the MaterialLibrary asset.
    /// </summary>
    public static class MaterialGenerator
    {
        public static Texture2D ParticleTexture { get; private set; }
        public static Texture2D LineTexture { get; private set; }
        public static Texture2D GridTexture { get; private set; }

        static Shader worldGrid;
        static Shader standard;

        public static MaterialLibrary Generate(MaterialLibrary library)
        {
            EditorUtil.EnsureFolder(GamePaths.Materials);
            EditorUtil.EnsureFolder(GamePaths.Textures);
            GenerateTextures();

            worldGrid = Shader.Find("Momentum/WorldGrid");
            standard = Shader.Find("Standard");
            if (worldGrid == null)
            {
                Debug.LogWarning("[Momentum Setup] Shader Momentum/WorldGrid not found (still compiling?). Using Standard for architecture.");
                worldGrid = standard;
            }

            // Architecture
            library.Set("White", Grid("Arch_White", new Color(0.92f, 0.92f, 0.94f), new Color(0.72f, 0.74f, 0.78f)));
            library.Set("LightGray", Grid("Arch_LightGray", new Color(0.78f, 0.79f, 0.82f), new Color(0.62f, 0.64f, 0.68f)));
            library.Set("Gray", Grid("Arch_Gray", new Color(0.55f, 0.56f, 0.6f), new Color(0.44f, 0.45f, 0.5f)));
            library.Set("Dark", Grid("Arch_Dark", new Color(0.22f, 0.23f, 0.26f), new Color(0.34f, 0.35f, 0.4f)));
            library.Set("Accent", Grid("Arch_Accent", new Color(1f, 0.45f, 0.1f), new Color(0.8f, 0.32f, 0.05f), new Color(1f, 0.45f, 0.1f) * 0.35f));
            library.Set("AccentBlue", Grid("Arch_AccentBlue", new Color(0.15f, 0.6f, 1f), new Color(0.1f, 0.45f, 0.8f), new Color(0.15f, 0.6f, 1f) * 0.35f));
            library.Set("AccentRed", Grid("Arch_AccentRed", new Color(0.95f, 0.15f, 0.2f), new Color(0.7f, 0.1f, 0.12f), new Color(0.95f, 0.15f, 0.2f) * 0.35f));
            library.Set("AccentGreen", Grid("Arch_AccentGreen", new Color(0.25f, 0.9f, 0.4f), new Color(0.15f, 0.65f, 0.3f), new Color(0.25f, 0.9f, 0.4f) * 0.3f));
            library.Set("AccentYellow", Grid("Arch_AccentYellow", new Color(1f, 0.85f, 0.15f), new Color(0.8f, 0.65f, 0.1f), new Color(1f, 0.85f, 0.15f) * 0.3f));
            library.Set("Hazard", Grid("Arch_Hazard", new Color(0.9f, 0.12f, 0.1f), new Color(0.15f, 0.02f, 0.02f), new Color(0.9f, 0.12f, 0.1f) * 0.8f));
            library.Set("Crate", Grid("Prop_Crate", new Color(0.78f, 0.66f, 0.48f), new Color(0.55f, 0.44f, 0.3f)));
            library.Set("Glass", Transparent("Glass", new Color(0.65f, 0.88f, 1f, 0.22f), false, 0.95f, Color.black));

            // Characters / weapons
            library.Set("EnemyBody", Std("Enemy_Body", new Color(0.93f, 0.93f, 0.95f), 0.35f));
            library.Set("EnemyDark", Std("Enemy_Dark", new Color(0.15f, 0.15f, 0.18f), 0.4f));
            library.Set("EnemyRed", Emissive("Enemy_Red", new Color(1f, 0.12f, 0.15f), 1.6f));
            library.Set("EnemyOrange", Emissive("Enemy_Orange", new Color(1f, 0.5f, 0.1f), 1.4f));
            library.Set("EnemyPurple", Emissive("Enemy_Purple", new Color(0.7f, 0.25f, 1f), 1.4f));
            library.Set("EnemyCyan", Emissive("Enemy_Cyan", new Color(0.2f, 0.85f, 1f), 1.4f));
            library.Set("EnemyYellow", Emissive("Enemy_Yellow", new Color(1f, 0.85f, 0.15f), 1.4f));
            library.Set("BossBody", Std("Boss_Body", new Color(0.82f, 0.83f, 0.86f), 0.45f, 0.2f));
            library.Set("BossDark", Std("Boss_Dark", new Color(0.12f, 0.12f, 0.15f), 0.5f, 0.3f));
            library.Set("BossCrimson", Emissive("Boss_Crimson", new Color(0.95f, 0.08f, 0.15f), 2f));
            library.Set("WeaponBody", Std("Weapon_Body", new Color(0.16f, 0.17f, 0.19f), 0.55f, 0.2f));
            library.Set("WeaponMetal", Std("Weapon_Metal", new Color(0.42f, 0.43f, 0.46f), 0.7f, 0.7f));

            // Gameplay emissives
            library.Set("GrappleOrb", Emissive("FX_GrappleOrb", new Color(0.1f, 0.8f, 1f), 2f));
            library.Set("LaunchPad", Emissive("FX_LaunchPad", new Color(0.25f, 1f, 0.45f), 2f));
            library.Set("SpeedPad", Emissive("FX_SpeedPad", new Color(1f, 0.82f, 0.1f), 1.8f));
            library.Set("Checkpoint", Emissive("FX_Checkpoint", new Color(0.9f, 0.9f, 0.9f), 1.2f));
            library.Set("Finish", Emissive("FX_Finish", new Color(1f, 0.85f, 0.2f), 1.8f));
            library.Set("PickupGlow", Emissive("FX_PickupGlow", new Color(1f, 0.6f, 0.15f), 1.8f));
            library.Set("HealthGlow", Emissive("FX_HealthGlow", new Color(0.3f, 1f, 0.45f), 1.8f));
            library.Set("HazardPanel", Emissive("FX_HazardPanel", new Color(0.25f, 0.25f, 0.3f), 0.02f));
            library.Set("LightFixture", Emissive("FX_LightFixture", new Color(1f, 0.85f, 0.6f), 2.5f));
            library.Set("Projectile", Emissive("FX_EnemyProjectile", new Color(1f, 0.2f, 0.25f), 3f));
            library.Set("Orb", Emissive("FX_EnergyOrb", new Color(0.85f, 0.2f, 1f), 3f));
            library.Set("Rock", Std("Prop_Rock", new Color(0.45f, 0.42f, 0.4f), 0.05f));

            // Transparent helpers
            library.Set("TriggerVolume", Transparent("Helper_Trigger", new Color(0.3f, 1f, 0.5f, 0.15f), true, 0f, new Color(0.1f, 0.4f, 0.2f)));
            library.Set("SecretVolume", Transparent("Helper_Secret", new Color(0.8f, 0.4f, 1f, 0.18f), true, 0f, new Color(0.3f, 0.1f, 0.4f)));
            library.Set("SpawnMarker", Transparent("Helper_Spawn", new Color(0.3f, 1f, 0.4f, 0.45f), true, 0f, new Color(0.1f, 0.5f, 0.15f)));
            library.Set("HazardVolume", Transparent("Helper_Hazard", new Color(1f, 0.15f, 0.1f, 0.45f), true, 0f, new Color(0.8f, 0.05f, 0.05f)));
            library.Set("LockedBarrier", Transparent("Helper_Locked", new Color(1f, 0.15f, 0.2f, 0.35f), true, 0f, new Color(0.7f, 0.05f, 0.1f)));
            library.Set("FinishGlow", Transparent("Helper_FinishGlow", new Color(1f, 0.85f, 0.2f, 0.25f), true, 0f, new Color(0.8f, 0.6f, 0.1f)));
            library.Set("Rage", Transparent("Helper_Rage", new Color(1f, 0.1f, 0.05f, 0.25f), true, 0f, new Color(1f, 0.1f, 0.05f)));

            library.editorGhost = Transparent("Editor_Ghost", new Color(0.3f, 0.85f, 1f, 0.35f), true, 0f, new Color(0.1f, 0.3f, 0.4f));
            library.triggerVolume = library.Get("TriggerVolume");
            library.telegraph = Transparent("FX_Telegraph", new Color(1f, 0.2f, 0.1f, 0.5f), true, 0f, new Color(1f, 0.2f, 0.1f));
            library.shield = Transparent("FX_Shield", new Color(0.3f, 0.85f, 1f, 0.22f), true, 0.9f, new Color(0.1f, 0.4f, 0.6f));

            // Particles & lines
            library.Set("ParticleAdditive", ParticleMat("FX_ParticleAdditive", true, ParticleTexture));
            library.Set("ParticleAlpha", ParticleMat("FX_ParticleAlpha", false, ParticleTexture));
            library.line = ParticleMat("FX_Line", true, LineTexture);
            library.lineAdditive = library.line;
            library.laser = ParticleMat("FX_Laser", true, LineTexture);
            library.rope = ParticleMat("FX_Rope", false, LineTexture);

            // Editor gizmos
            library.gizmoX = Gizmo("Gizmo_X", new Color(1f, 0.25f, 0.25f));
            library.gizmoY = Gizmo("Gizmo_Y", new Color(0.35f, 1f, 0.35f));
            library.gizmoZ = Gizmo("Gizmo_Z", new Color(0.3f, 0.55f, 1f));
            library.gizmoCenter = Gizmo("Gizmo_Center", new Color(1f, 0.9f, 0.3f));
            library.gizmoHighlight = Gizmo("Gizmo_Highlight", Color.white);
            library.editorSelection = library.gizmoHighlight;
            library.editorGrid = EditorGridMaterial();

            // Text
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            library.textMaterial = font != null ? font.material : null;

            // Skyboxes
            library.SetSkybox("Day", Skybox("Sky_Day", new Color(0.55f, 0.7f, 0.95f), new Color(0.42f, 0.42f, 0.45f), 1.1f, 1.3f));
            library.SetSkybox("Dusk", Skybox("Sky_Dusk", new Color(1f, 0.62f, 0.45f), new Color(0.35f, 0.25f, 0.25f), 1.6f, 1.1f));
            library.SetSkybox("Night", Skybox("Sky_Night", new Color(0.15f, 0.2f, 0.4f), new Color(0.06f, 0.06f, 0.09f), 0.6f, 0.45f));
            library.SetSkybox("Boss", Skybox("Sky_Boss", new Color(0.8f, 0.2f, 0.25f), new Color(0.12f, 0.05f, 0.06f), 1.5f, 0.7f));

            EditorUtility.SetDirty(library);
            return library;
        }

        // ------------------------------------------------------------------ Material factories

        static Material LoadOrNew(string name, Shader shader)
        {
            string path = GamePaths.Materials + "/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(mat, path);
            }
            else if (mat.shader != shader)
            {
                mat.shader = shader;
            }
            return mat;
        }

        static Material Configure(Material mat, Action<Material> configure)
        {
            configure(mat);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static Material Grid(string name, Color baseColor, Color lineColor, Color emission = default)
        {
            return Configure(LoadOrNew(name, worldGrid), m =>
            {
                m.SetColor("_Color", baseColor);
                if (m.HasProperty("_LineColor")) m.SetColor("_LineColor", lineColor);
                if (m.HasProperty("_MajorLineColor")) m.SetColor("_MajorLineColor", Color.Lerp(lineColor, Color.black, 0.2f));
                if (m.HasProperty("_GridSize")) m.SetFloat("_GridSize", 1f);
                if (m.HasProperty("_MajorEvery")) m.SetFloat("_MajorEvery", 4f);
                if (m.HasProperty("_LineWidth")) m.SetFloat("_LineWidth", 0.025f);
                m.SetFloat("_Glossiness", 0.15f);
                m.SetFloat("_Metallic", 0f);
                SetEmission(m, emission);
            });
        }

        static Material Std(string name, Color color, float smoothness, float metallic = 0f)
        {
            return Configure(LoadOrNew(name, standard), m =>
            {
                m.SetColor("_Color", color);
                m.SetFloat("_Glossiness", smoothness);
                m.SetFloat("_Metallic", metallic);
                SetEmission(m, Color.black);
            });
        }

        static Material Emissive(string name, Color color, float intensity)
        {
            return Configure(LoadOrNew(name, standard), m =>
            {
                m.SetColor("_Color", color);
                m.SetFloat("_Glossiness", 0.4f);
                m.SetFloat("_Metallic", 0f);
                SetEmission(m, color * intensity);
            });
        }

        static void SetEmission(Material m, Color emission)
        {
            m.SetColor("_EmissionColor", emission);
            if (emission.maxColorComponent > 0.001f)
            {
                m.EnableKeyword("_EMISSION");
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            else
            {
                m.DisableKeyword("_EMISSION");
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            }
        }

        /// <summary>Standard shader in Fade (fade=true) or Transparent mode.</summary>
        static Material Transparent(string name, Color color, bool fade, float smoothness, Color emission)
        {
            return Configure(LoadOrNew(name, standard), m =>
            {
                m.SetColor("_Color", color);
                m.SetFloat("_Glossiness", smoothness);
                m.SetFloat("_Metallic", 0f);
                m.SetFloat("_Mode", fade ? 2f : 3f);
                m.SetInt("_SrcBlend", fade ? (int)BlendMode.SrcAlpha : (int)BlendMode.One);
                m.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                m.SetInt("_ZWrite", 0);
                m.DisableKeyword("_ALPHATEST_ON");
                if (fade)
                {
                    m.EnableKeyword("_ALPHABLEND_ON");
                    m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                }
                else
                {
                    m.DisableKeyword("_ALPHABLEND_ON");
                    m.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                }
                m.SetOverrideTag("RenderType", "Transparent");
                m.renderQueue = (int)RenderQueue.Transparent;
                SetEmission(m, emission);
            });
        }

        static Material ParticleMat(string name, bool additive, Texture2D texture)
        {
            var shader = Shader.Find(additive ? "Legacy Shaders/Particles/Additive" : "Legacy Shaders/Particles/Alpha Blended");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            return Configure(LoadOrNew(name, shader), m =>
            {
                if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", texture);
                if (m.HasProperty("_TintColor")) m.SetColor("_TintColor", new Color(0.5f, 0.5f, 0.5f, 0.5f));
            });
        }

        static Material Gizmo(string name, Color color)
        {
            var shader = Shader.Find("Momentum/GizmoOverlay");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            return Configure(LoadOrNew(name, shader), m => m.SetColor("_Color", color));
        }

        static Material EditorGridMaterial()
        {
            var shader = Shader.Find("Unlit/Transparent");
            if (shader == null) shader = standard;
            return Configure(LoadOrNew("Editor_Grid", shader), m =>
            {
                m.SetTexture("_MainTex", GridTexture);
                m.mainTextureScale = new Vector2(400f, 400f);
            });
        }

        static Material Skybox(string name, Color skyTint, Color ground, float atmosphere, float exposure)
        {
            var shader = Shader.Find("Skybox/Procedural");
            if (shader == null) return null;
            return Configure(LoadOrNew(name, shader), m =>
            {
                m.SetFloat("_SunDisk", 2f);
                m.SetFloat("_SunSize", 0.04f);
                m.SetFloat("_SunSizeConvergence", 5f);
                m.SetFloat("_AtmosphereThickness", atmosphere);
                m.SetColor("_SkyTint", skyTint);
                m.SetColor("_GroundColor", ground);
                m.SetFloat("_Exposure", exposure);
            });
        }

        // ------------------------------------------------------------------ Textures

        static void GenerateTextures()
        {
            // Soft radial particle.
            var p = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            for (int y = 0; y < 64; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    float dx = (x - 31.5f) / 31.5f;
                    float dy = (y - 31.5f) / 31.5f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(1f - d);
                    a = a * a;
                    p.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            p.Apply();
            ParticleTexture = EditorUtil.SaveTexture(p, GamePaths.Textures + "/ParticleSoft.png", TextureWrapMode.Clamp, true);

            // Soft line (fades across its width).
            var l = new Texture2D(8, 64, TextureFormat.RGBA32, false);
            for (int y = 0; y < 64; y++)
            {
                float v = Mathf.Abs((y - 31.5f) / 31.5f);
                float a = Mathf.Clamp01(1f - v);
                a = Mathf.Pow(a, 1.5f);
                for (int x = 0; x < 8; x++) l.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
            l.Apply();
            LineTexture = EditorUtil.SaveTexture(l, GamePaths.Textures + "/LineSoft.png", TextureWrapMode.Clamp, true);

            // Editor grid cell.
            var g = new Texture2D(64, 64, TextureFormat.RGBA32, true);
            for (int y = 0; y < 64; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    bool edge = x < 2 || y < 2;
                    g.SetPixel(x, y, edge ? new Color(1f, 1f, 1f, 0.45f) : new Color(1f, 1f, 1f, 0.02f));
                }
            }
            g.Apply();
            GridTexture = EditorUtil.SaveTexture(g, GamePaths.Textures + "/EditorGrid.png", TextureWrapMode.Repeat, true);
        }
    }
}
