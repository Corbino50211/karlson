using Momentum.PlayerSystems;
using Momentum.Weapons;
using UnityEditor;
using UnityEngine;

namespace Momentum.EditorTools
{
    /// <summary>
    /// Builds the Rigidbody first-person player prefab with every component wired:
    /// Player (movement, abilities, health, weapons, combat, interaction, audio, shake)
    ///   CameraRig (yaw, PlayerCamera) > CameraPivot (eye height, pitch, tilt) > MainCamera
    ///     > WeaponCamera (view model layer only) + WeaponHolder (sway) + GrappleOrigin
    ///   GrappleRope (LineRenderer), GrappleIndicator, shadow-only body.
    /// </summary>
    public static class PlayerPrefabGenerator
    {
        public const string PrefabPath = GamePaths.PlayerPrefabs + "/Player.prefab";
        const string FrictionlessPath = GamePaths.Materials + "/Physics_PlayerFrictionless.physicMaterial";

        public static GameObject Generate(PrefabRegistry registry, MaterialLibrary lib, MovementSettings movementSettings, CameraFeelSettings cameraSettings)
        {
            EditorUtil.Require(registry, "PrefabRegistry");
            EditorUtil.Require(lib, "MaterialLibrary");
            EditorUtil.EnsureFolder(GamePaths.PlayerPrefabs);
            var frictionless = FrictionlessMaterial();

            var root = EditorUtil.Create("Player", null, Layers.Player);
            root.tag = Tags.Player;

            var rb = root.AddComponent<Rigidbody>();
            rb.mass = 1f;
            rb.useGravity = false;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.drag = 0f;
            rb.angularDrag = 0f;

            float standHeight = movementSettings != null ? movementSettings.standHeight : 2f;
            float radius = movementSettings != null ? movementSettings.radius : 0.4f;
            var capsule = root.AddComponent<CapsuleCollider>();
            capsule.direction = 1;
            capsule.radius = radius;
            capsule.height = standHeight;
            capsule.center = Vector3.zero;
            capsule.sharedMaterial = frictionless;

            var input = root.AddComponent<PlayerInputHandler>();
            var movement = root.AddComponent<PlayerMovement>();
            var wallRun = root.AddComponent<WallRunAbility>();
            var ledge = root.AddComponent<LedgeAbility>();
            var grapple = root.AddComponent<GrappleHook>();
            var health = root.AddComponent<PlayerHealth>();
            var weapons = root.AddComponent<WeaponManager>();
            var combat = root.AddComponent<PlayerCombat>();
            var interactor = root.AddComponent<PlayerInteractor>();
            var audio = root.AddComponent<PlayerAudio>();
            var shaker = root.AddComponent<CameraShaker>();
            var controller = root.AddComponent<PlayerController>();

            // Shadow-only body so the player casts a shadow without blocking the view.
            var body = EditorUtil.Visual("BodyShadow", root.transform, EditorUtil.CapsuleMesh, lib.Get("Dark"), Vector3.zero,
                new Vector3(radius * 2f, standHeight * 0.5f, radius * 2f));
            var bodyRenderer = body.GetComponent<MeshRenderer>();
            bodyRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
            bodyRenderer.receiveShadows = false;

            // Camera hierarchy.
            var rig = EditorUtil.Create("CameraRig", root.transform, Layers.Player);
            var playerCamera = rig.AddComponent<PlayerCamera>();
            var pivot = EditorUtil.Create("CameraPivot", rig.transform, Layers.Player);
            float eyeLocal = (movementSettings != null ? movementSettings.eyeHeightStanding : 1.7f) - standHeight * 0.5f;
            pivot.transform.localPosition = new Vector3(0f, eyeLocal, 0f);

            var camGo = EditorUtil.Create("MainCamera", pivot.transform, Layers.Player);
            camGo.tag = Tags.MainCamera;
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 2000f;
            cam.fieldOfView = 90f;
            cam.depth = 0f;
            cam.cullingMask = ~((1 << Layers.ViewModel) | (1 << Layers.EditorGizmo));
            cam.allowHDR = true;
            cam.allowMSAA = true;
            camGo.AddComponent<AudioListener>();

            var weaponCamGo = EditorUtil.Create("WeaponCamera", camGo.transform, Layers.Player);
            var weaponCam = weaponCamGo.AddComponent<Camera>();
            weaponCam.clearFlags = CameraClearFlags.Depth;
            weaponCam.cullingMask = 1 << Layers.ViewModel;
            weaponCam.depth = 1f;
            weaponCam.nearClipPlane = 0.01f;
            weaponCam.farClipPlane = 20f;
            weaponCam.fieldOfView = cameraSettings != null ? cameraSettings.viewModelFov : 62f;
            weaponCam.allowHDR = true;
            weaponCam.allowMSAA = true;

            var holder = EditorUtil.Create("WeaponHolder", camGo.transform, Layers.ViewModel);
            var sway = holder.AddComponent<WeaponSway>();

            var grappleOrigin = EditorUtil.Create("GrappleOrigin", camGo.transform, Layers.Player);
            grappleOrigin.transform.localPosition = new Vector3(-0.32f, -0.3f, 0.45f);

            // Rope.
            var ropeGo = EditorUtil.Create("GrappleRope", root.transform, Layers.Player);
            var line = ropeGo.AddComponent<LineRenderer>();
            line.sharedMaterial = lib.rope;
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.widthMultiplier = 0.045f;
            line.numCapVertices = 2;
            line.startColor = new Color(0.25f, 0.9f, 1f);
            line.endColor = new Color(0.9f, 0.95f, 1f);
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.enabled = false;
            var rope = ropeGo.AddComponent<GrappleRope>();

            // Grapple target indicator (ring facing the camera).
            var indicator = EditorUtil.Create("GrappleIndicator", root.transform, Layers.IgnoreRaycast);
            var ring = EditorUtil.Visual("Ring", indicator.transform, MeshAssetGenerator.Torus, lib.Get("GrappleOrb"), Vector3.zero, Vector3.one, new Vector3(90f, 0f, 0f));
            EditorUtil.DisableShadows(ring);
            for (int i = 0; i < 4; i++)
            {
                var tick = EditorUtil.Box("Tick" + i, indicator.transform, lib.Get("GrappleOrb"), Quaternion.Euler(0f, 0f, i * 90f) * new Vector3(0f, 1.25f, 0f), new Vector3(0.08f, 0.35f, 0.02f), new Vector3(0f, 0f, i * 90f));
                EditorUtil.DisableShadows(tick);
            }
            indicator.SetActive(false);

            // Wiring.
            EditorUtil.Set(movement, "settings", movementSettings);
            EditorUtil.Set(movement, "inputHandler", input);
            EditorUtil.Set(movement, "playerCamera", playerCamera);
            EditorUtil.Set(movement, "wallRun", wallRun);
            EditorUtil.Set(movement, "ledge", ledge);
            EditorUtil.Set(movement, "grapple", grapple);
            EditorUtil.Set(movement, "frictionlessMaterial", frictionless);
            EditorUtil.Set(wallRun, "movement", movement);
            EditorUtil.Set(ledge, "movement", movement);

            EditorUtil.Set(grapple, "movement", movement);
            EditorUtil.Set(grapple, "aimTransform", camGo.transform);
            EditorUtil.Set(grapple, "ropeOrigin", grappleOrigin.transform);
            EditorUtil.Set(grapple, "rope", rope);
            EditorUtil.Set(grapple, "targetIndicator", indicator.transform);
            EditorUtil.Set(rope, "hook", grapple);

            EditorUtil.Set(health, "maxHealth", 100f);
            EditorUtil.Set(weapons, "player", controller);
            EditorUtil.Set(weapons, "weaponHolder", holder.transform);
            EditorUtil.Set(weapons, "sway", sway);
            EditorUtil.Set(sway, "movement", movement);
            EditorUtil.Set(combat, "player", controller);
            EditorUtil.Set(combat, "weapons", weapons);
            EditorUtil.Set(interactor, "player", controller);
            EditorUtil.Set(audio, "movement", movement);

            EditorUtil.Set(playerCamera, "feel", cameraSettings);
            EditorUtil.Set(playerCamera, "movement", movement);
            EditorUtil.Set(playerCamera, "cameraRig", rig.transform);
            EditorUtil.Set(playerCamera, "cameraPivot", pivot.transform);
            EditorUtil.Set(playerCamera, "mainCamera", cam);
            EditorUtil.Set(playerCamera, "viewModelCamera", weaponCam);
            EditorUtil.Set(playerCamera, "shaker", shaker);

            EditorUtil.Set(controller, "inputHandler", input);
            EditorUtil.Set(controller, "movement", movement);
            EditorUtil.Set(controller, "playerCamera", playerCamera);
            EditorUtil.Set(controller, "health", health);
            EditorUtil.Set(controller, "weapons", weapons);
            EditorUtil.Set(controller, "combat", combat);
            EditorUtil.Set(controller, "grapple", grapple);
            EditorUtil.Set(controller, "interactor", interactor);
            EditorUtil.Set(controller, "shaker", shaker);

            var prefab = EditorUtil.SavePrefab(root, PrefabPath);
            registry.player = prefab;
            return prefab;
        }

        static PhysicMaterial FrictionlessMaterial()
        {
            EditorUtil.EnsureFolder(GamePaths.Materials);
            var mat = AssetDatabase.LoadAssetAtPath<PhysicMaterial>(FrictionlessPath);
            if (mat == null)
            {
                mat = new PhysicMaterial("PlayerFrictionless");
                AssetDatabase.CreateAsset(mat, FrictionlessPath);
            }
            mat.dynamicFriction = 0f;
            mat.staticFriction = 0f;
            mat.bounciness = 0f;
            mat.frictionCombine = PhysicMaterialCombine.Minimum;
            mat.bounceCombine = PhysicMaterialCombine.Minimum;
            EditorUtility.SetDirty(mat);
            return mat;
        }
    }
}
