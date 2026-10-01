using UnityEngine;
using UnityEngine.Rendering;

namespace Momentum.Levels
{
    /// <summary>Applies LevelEnvironmentSettings (ambient light, skybox, fog, sun) to the active scene.</summary>
    public static class EnvironmentApplier
    {
        public static void Apply(LevelEnvironmentSettings env, Light sun)
        {
            if (env == null) return;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = env.ambientSky;
            RenderSettings.ambientEquatorColor = env.ambientEquator;
            RenderSettings.ambientGroundColor = env.ambientGround;
            RenderSettings.fog = env.fog;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = env.fogColor;
            RenderSettings.fogDensity = env.fogDensity;

            var library = MaterialLibrary.Instance;
            var sky = library != null ? library.GetSkybox(env.skybox) : null;
            if (sky != null) RenderSettings.skybox = sky;

            if (sun == null) sun = FindSun();
            if (sun != null)
            {
                sun.color = env.sunColor;
                sun.intensity = env.sunIntensity;
                sun.transform.rotation = Quaternion.Euler(env.sunRotation);
                RenderSettings.sun = sun;
            }
            if (Application.isPlaying) DynamicGI.UpdateEnvironment();
        }

        public static Light FindSun()
        {
            if (RenderSettings.sun != null) return RenderSettings.sun;
            foreach (var l in Object.FindObjectsOfType<Light>())
            {
                if (l.type == LightType.Directional) return l;
            }
            return null;
        }

        /// <summary>Preset environments used by campaign levels and selectable in the editor.</summary>
        public static LevelEnvironmentSettings Preset(string name)
        {
            var env = new LevelEnvironmentSettings { preset = string.IsNullOrEmpty(name) ? "Day" : name };
            switch (name)
            {
                case "Dusk":
                    env.skybox = "Dusk";
                    env.ambientSky = new Color(0.85f, 0.62f, 0.5f);
                    env.ambientEquator = new Color(0.6f, 0.45f, 0.42f);
                    env.ambientGround = new Color(0.3f, 0.25f, 0.25f);
                    env.sunColor = new Color(1f, 0.72f, 0.5f);
                    env.sunIntensity = 1.1f;
                    env.sunRotation = new Vector3(18f, -60f, 0f);
                    env.fogColor = new Color(0.86f, 0.6f, 0.5f);
                    env.fogDensity = 0.006f;
                    break;
                case "Night":
                    env.skybox = "Night";
                    env.ambientSky = new Color(0.32f, 0.38f, 0.55f);
                    env.ambientEquator = new Color(0.25f, 0.28f, 0.36f);
                    env.ambientGround = new Color(0.12f, 0.12f, 0.16f);
                    env.sunColor = new Color(0.6f, 0.72f, 1f);
                    env.sunIntensity = 0.7f;
                    env.sunRotation = new Vector3(55f, 30f, 0f);
                    env.fogColor = new Color(0.12f, 0.14f, 0.22f);
                    env.fogDensity = 0.009f;
                    break;
                case "Boss":
                    env.skybox = "Boss";
                    env.ambientSky = new Color(0.62f, 0.32f, 0.35f);
                    env.ambientEquator = new Color(0.4f, 0.22f, 0.25f);
                    env.ambientGround = new Color(0.18f, 0.1f, 0.12f);
                    env.sunColor = new Color(1f, 0.55f, 0.5f);
                    env.sunIntensity = 1.0f;
                    env.sunRotation = new Vector3(40f, 120f, 0f);
                    env.fogColor = new Color(0.32f, 0.12f, 0.14f);
                    env.fogDensity = 0.008f;
                    break;
                case "Sky":
                    env.skybox = "Day";
                    env.ambientSky = new Color(0.68f, 0.8f, 0.98f);
                    env.ambientEquator = new Color(0.62f, 0.68f, 0.78f);
                    env.ambientGround = new Color(0.45f, 0.5f, 0.6f);
                    env.sunColor = new Color(1f, 0.98f, 0.92f);
                    env.sunIntensity = 1.25f;
                    env.sunRotation = new Vector3(60f, -20f, 0f);
                    env.fogColor = new Color(0.75f, 0.85f, 1f);
                    env.fogDensity = 0.003f;
                    break;
                case "Industrial":
                    env.skybox = "Dusk";
                    env.ambientSky = new Color(0.6f, 0.6f, 0.62f);
                    env.ambientEquator = new Color(0.45f, 0.44f, 0.42f);
                    env.ambientGround = new Color(0.25f, 0.24f, 0.22f);
                    env.sunColor = new Color(1f, 0.85f, 0.7f);
                    env.sunIntensity = 1.0f;
                    env.sunRotation = new Vector3(35f, 45f, 0f);
                    env.fogColor = new Color(0.55f, 0.52f, 0.5f);
                    env.fogDensity = 0.007f;
                    break;
            }
            return env;
        }

        public static readonly string[] PresetNames = { "Day", "Dusk", "Night", "Sky", "Industrial", "Boss" };
    }
}
