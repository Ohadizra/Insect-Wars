using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using System.IO;
using InsectWars.Data;

namespace InsectWars.Editor
{
    public static class SetupUnitPrefabs
    {
        const string ControllerDir = "Assets/_InsectWars/Animations";
        const string PrefabDir = "Assets/_InsectWars/Prefabs";
        const string LibraryDir = "Assets/_InsectWars/Data";
        const string LibraryPath = LibraryDir + "/DefaultVisualLibrary.asset";

        // --- Ant Worker paths ---
        const string AntModelPath = "Assets/Insects Art/Meshy_AI_Iron_Ant_Vanguard_0404124845_texture_fbx 1/Meshy_AI_Iron_Ant_Vanguard_quadruped/Meshy_AI_Iron_Ant_Vanguard_quadruped_model_Animation_Walking_withSkin.fbx";
        const string AntTexDir = "Assets/Insects Art/Meshy_AI_Iron_Ant_Vanguard_0404124845_texture_fbx 1/Meshy_AI_Iron_Ant_Vanguard_quadruped";
        const string AntBaseTexPath = AntTexDir + "/Meshy_AI_Iron_Ant_Vanguard_quadruped_texture_0.png";
        const string AntMetallicTexPath = AntTexDir + "/Meshy_AI_Iron_Ant_Vanguard_quadruped_texture_0_metallic.png";
        const string AntRoughnessTexPath = AntTexDir + "/Meshy_AI_Iron_Ant_Vanguard_quadruped_texture_0_roughness.png";
        const string AntMaterialPath = PrefabDir + "/AntWorkerMat.mat";
        const string AntControllerPath = ControllerDir + "/AntWorker.controller";
        const string AntPrefabPath = PrefabDir + "/AntWorker.prefab";

        // --- Mantis Fighter paths ---
        const string MantisModelPath = "Assets/Insects Art/Fighter-Mantis/Meshy_AI_Ironclad_Mantis_Autom_quadruped/Meshy_AI_Ironclad_Mantis_Autom_quadruped_model_Animation_Walking_withSkin.fbx";
        const string MantisTexDir = "Assets/Insects Art/Fighter-Mantis/Meshy_AI_Ironclad_Mantis_Autom_quadruped";
        const string MantisBaseTexPath = MantisTexDir + "/Meshy_AI_Ironclad_Mantis_Autom_quadruped_texture_0.png";
        const string MantisMetallicTexPath = MantisTexDir + "/Meshy_AI_Ironclad_Mantis_Autom_quadruped_texture_0_metallic.png";
        const string MantisRoughnessTexPath = MantisTexDir + "/Meshy_AI_Ironclad_Mantis_Autom_quadruped_texture_0_roughness.png";
        const string MantisMaterialPath = PrefabDir + "/MantisFighterMat.mat";
        const string MantisControllerPath = ControllerDir + "/MantisFighter.controller";
        const string MantisPrefabPath = PrefabDir + "/MantisFighter.prefab";

        // --- Bombardier Beetle (Ranged) paths ---
        const string BeetleModelPath = "Assets/Insects Art/Bombardier-Beetle/Meshy_AI_Bombardier_Beetles_quadruped/Meshy_AI_Bombardier_Beetles_quadruped_model_Animation_Walking_withSkin.fbx";
        const string BeetleTexDir = "Assets/Insects Art/Bombardier-Beetle/Meshy_AI_Bombardier_Beetles_quadruped";
        const string BeetleBaseTexPath = BeetleTexDir + "/Meshy_AI_Bombardier_Beetles_quadruped_texture_0.png";
        const string BeetleNormalTexPath = BeetleTexDir + "/Meshy_AI_Bombardier_Beetles_quadruped_texture_0_normal.png";
        const string BeetleMetallicTexPath = BeetleTexDir + "/Meshy_AI_Bombardier_Beetles_quadruped_texture_0_metallic.png";
        const string BeetleRoughnessTexPath = BeetleTexDir + "/Meshy_AI_Bombardier_Beetles_quadruped_texture_0_roughness.png";
        const string BeetleMaterialPath = PrefabDir + "/BombardierBeetleMat.mat";
        const string BeetleControllerPath = ControllerDir + "/BombardierBeetle.controller";
        const string BeetlePrefabPath = PrefabDir + "/BombardierBeetle.prefab";

        // --- Black Widow paths ---
        const string WidowModelPath = "Assets/_InsectWars/Units/Black Widow/Meshy_AI_Crimson_Widow_0415065353_texture_fbx/Meshy_AI_Crimson_Widow_0415065353_texture.fbx";
        const string WidowTexDir = "Assets/_InsectWars/Units/Black Widow/Meshy_AI_Crimson_Widow_0415065353_texture_fbx";
        const string WidowBaseTexPath = WidowTexDir + "/Meshy_AI_Crimson_Widow_0415065353_texture.png";
        const string WidowNormalTexPath = WidowTexDir + "/Meshy_AI_Crimson_Widow_0415065353_texture_normal.png";
        const string WidowEmissionTexPath = WidowTexDir + "/Meshy_AI_Crimson_Widow_0415065353_texture_emission.png";
        const string WidowRoughnessTexPath = WidowTexDir + "/Meshy_AI_Crimson_Widow_0415065353_texture_roughness.png";
        const string WidowMaterialPath = PrefabDir + "/BlackWidowMat.mat";
        const string WidowControllerPath = ControllerDir + "/BlackWidow.controller";
        const string WidowPrefabPath = PrefabDir + "/BlackWidow.prefab";

        // --- Hawk Moth paths ---
        const string MothModelPath = "Assets/_InsectWars/Units/Hawk Moth/Meshy_AI_Nocturnal_Sentinel_0415071225_texture_fbx/Meshy_AI_Nocturnal_Sentinel_0415071225_texture.fbx";
        const string MothTexDir = "Assets/_InsectWars/Units/Hawk Moth/Meshy_AI_Nocturnal_Sentinel_0415071225_texture_fbx";
        const string MothBaseTexPath = MothTexDir + "/Meshy_AI_Nocturnal_Sentinel_0415071225_texture.png";
        const string MothNormalTexPath = MothTexDir + "/Meshy_AI_Nocturnal_Sentinel_0415071225_texture_normal.png";
        const string MothEmissionTexPath = MothTexDir + "/Meshy_AI_Nocturnal_Sentinel_0415071225_texture_emission.png";
        const string MothRoughnessTexPath = MothTexDir + "/Meshy_AI_Nocturnal_Sentinel_0415071225_texture_roughness.png";
        const string MothMetallicTexPath = MothTexDir + "/Meshy_AI_Nocturnal_Sentinel_0415071225_texture_metallic.png";
        const string MothMaterialPath = PrefabDir + "/HawkMothMat.mat";
        const string MothControllerPath = ControllerDir + "/HawkMoth.controller";
        const string MothPrefabPath = PrefabDir + "/HawkMoth.prefab";

        // --- Ant Nest (Hive) paths ---
        const string NestModelPath = "Assets/Insects Art/Stractures/Ants nest/Meshy_AI_ant_s_nest_rts_buildi_0404152548_texture_fbx/Meshy_AI_ant_s_nest_rts_buildi_0404152548_texture.fbx";
        const string NestTexDir = "Assets/Insects Art/Stractures/Ants nest/Meshy_AI_ant_s_nest_rts_buildi_0404152548_texture_fbx";
        const string NestBaseTexPath = NestTexDir + "/Meshy_AI_ant_s_nest_rts_buildi_0404152548_texture.png";
        const string NestNormalTexPath = NestTexDir + "/Meshy_AI_ant_s_nest_rts_buildi_0404152548_texture_normal.png";
        const string NestMetallicTexPath = NestTexDir + "/Meshy_AI_ant_s_nest_rts_buildi_0404152548_texture_metallic.png";
        const string NestRoughnessTexPath = NestTexDir + "/Meshy_AI_ant_s_nest_rts_buildi_0404152548_texture_roughness.png";
        const string NestMaterialPath = PrefabDir + "/AntNestMat.mat";
        const string NestPrefabPath = PrefabDir + "/AntNest.prefab";

        // --- Sky Tower paths ---
        const string SkyTowerModelPath = "Assets/_InsectWars/Buildings/SkyTree/Meshy_AI_Citadel_of_the_Hexed__0409195524_texture.fbx";
        const string SkyTowerTexDir = "Assets/_InsectWars/Buildings/SkyTree";
        const string SkyTowerBaseTexPath = SkyTowerTexDir + "/Meshy_AI_Citadel_of_the_Hexed__0409195524_texture.png";
        const string SkyTowerNormalTexPath = SkyTowerTexDir + "/Meshy_AI_Citadel_of_the_Hexed__0409195524_texture_normal.png";
        const string SkyTowerEmissionTexPath = SkyTowerTexDir + "/Meshy_AI_Citadel_of_the_Hexed__0409195524_texture_emission.png";
        const string SkyTowerMetallicTexPath = SkyTowerTexDir + "/Meshy_AI_Citadel_of_the_Hexed__0409195524_texture_metallic.png";
        const string SkyTowerRoughnessTexPath = SkyTowerTexDir + "/Meshy_AI_Citadel_of_the_Hexed__0409195524_texture_roughness.png";
        const string SkyTowerMaterialPath = PrefabDir + "/SkyTowerMat.mat";
        const string SkyTowerPrefabPath = PrefabDir + "/SkyTower.prefab";

        // ----------------------------------------------------------------
        // Menu items
        // ----------------------------------------------------------------

        [MenuItem("Insect Wars/Setup All Units")]
        public static void SetupAll()
        {
            SetupMantisFighter();
            SetupAntWorker();
            SetupBombardierBeetle();
            SetupBlackWidow();
            SetupHawkMoth();
            SetupAntNest();
            SetupSkyTower();
        }

        [MenuItem("Insect Wars/Setup Ant Worker")]
        public static void SetupAntWorker()
        {
            if (!ValidateAssets(AntModelPath))
                return;

            FixAntImportSettings();

            var controller = BuildAntWorkerController();
            var antMaterial = BuildAntMaterial();
            var prefab = BuildPrefab(AntModelPath, AntPrefabPath, "AntWorker",
                controller, Vector3.one,
                agentHeight: 0.6f, agentRadius: 0.4f, agentSpeed: 4.0f,
                colCenter: new Vector3(0f, 0.3f, 0f), colRadius: 0.4f, colHeight: 0.6f,
                overrideMaterial: antMaterial);

            UpdateLibrary(workerPrefab: prefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Insect Wars] Ant worker setup complete! " +
                      "Make sure DefaultVisualLibrary is assigned on MapDirector.");
        }

        [MenuItem("Insect Wars/Setup Mantis Fighter")]
        public static void SetupMantisFighter()
        {
            if (!ValidateAssets(MantisModelPath))
                return;

            FixMantisImportSettings();

            var controller = BuildMantisFighterController();
            var mantisMaterial = BuildMantisMaterial();
            var prefab = BuildPrefab(MantisModelPath, MantisPrefabPath, "MantisFighter",
                controller, Vector3.one * 2.5f,
                agentHeight: 2.0f, agentRadius: 1.05f, agentSpeed: 5.4f,
                colCenter: new Vector3(0f, 1.0f, 0f), colRadius: 1.05f, colHeight: 2.0f,
                overrideMaterial: mantisMaterial);

            UpdateLibrary(meleePrefab: prefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Insect Wars] Mantis fighter setup complete! " +
                      "Make sure DefaultVisualLibrary is assigned on MapDirector.");
        }

        [MenuItem("Insect Wars/Setup Bombardier Beetle")]
        public static void SetupBombardierBeetle()
        {
            if (!ValidateAssets(BeetleModelPath))
                return;

            FixBeetleImportSettings();

            var controller = BuildBeetleController();
            var beetleMaterial = BuildBeetleMaterial();
            var prefab = BuildPrefab(BeetleModelPath, BeetlePrefabPath, "BombardierBeetle",
                controller, Vector3.one * 1.8f,
                agentHeight: 0.8f, agentRadius: 0.45f, agentSpeed: 4.2f,
                colCenter: new Vector3(0f, 0.4f, 0f), colRadius: 0.45f, colHeight: 0.8f,
                overrideMaterial: beetleMaterial);

            UpdateLibrary(rangedPrefab: prefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Insect Wars] Bombardier Beetle setup complete! " +
                      "Make sure DefaultVisualLibrary is assigned on MapDirector.");
        }

        [MenuItem("Insect Wars/Setup Ant Nest")]
        public static void SetupAntNest()
        {
            if (!ValidateAssets(NestModelPath))
                return;

            FixNestImportSettings();

            var nestMaterial = BuildNestMaterial();
            var prefab = BuildHivePrefab(NestModelPath, NestPrefabPath, "AntNest",
                Vector3.one * 4f, nestMaterial,
                colCenter: new Vector3(0f, 1.5f, 0f), colSize: new Vector3(5f, 3f, 5f));

            UpdateLibrary(hivePrefab: prefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Insect Wars] Ant nest setup complete! " +
                      "Make sure DefaultVisualLibrary is assigned on MapDirector.");
        }

        [MenuItem("Insect Wars/Setup Sky Tower")]
        public static void SetupSkyTower()
        {
            if (!ValidateAssets(SkyTowerModelPath))
                return;

            FixSkyTowerImportSettings();

            var skyTowerMaterial = BuildSkyTowerMaterial();
            var prefab = BuildBuildingPrefab(SkyTowerModelPath, SkyTowerPrefabPath, "SkyTower",
                Vector3.one * 4f, skyTowerMaterial,
                colCenter: new Vector3(0f, 2.5f, 0f), colSize: new Vector3(4f, 5f, 4f));

            UpdateLibrary(skyTowerPrefab: prefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Insect Wars] Sky Tower setup complete! " +
                      "Make sure DefaultVisualLibrary is assigned on MapDirector.");
        }

        [MenuItem("Insect Wars/Setup Black Widow")]
        public static void SetupBlackWidow()
        {
            if (!ValidateAssets(WidowModelPath))
                return;

            FixWidowImportSettings();

            var controller = BuildBlackWidowController();
            var widowMaterial = BuildWidowMaterial();
            var prefab = BuildPrefab(WidowModelPath, WidowPrefabPath, "BlackWidow",
                controller, Vector3.one * 2f,
                agentHeight: 0.7f, agentRadius: 0.5f, agentSpeed: 4.8f,
                colCenter: new Vector3(0f, 0.35f, 0f), colRadius: 0.5f, colHeight: 0.7f,
                overrideMaterial: widowMaterial,
                visualRotation: new Vector3(-90f, 0f, 0f));

            UpdateLibrary(blackWidowPrefab: prefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Insect Wars] Black Widow setup complete! " +
                      "Make sure DefaultVisualLibrary is assigned on MapDirector.");
        }

        [MenuItem("Insect Wars/Setup Hawk Moth")]
        public static void SetupHawkMoth()
        {
            if (!ValidateAssets(MothModelPath))
                return;

            FixMothImportSettings();

            var controller = BuildHawkMothController();
            var mothMaterial = BuildMothMaterial();
            var prefab = BuildPrefab(MothModelPath, MothPrefabPath, "HawkMoth",
                controller, Vector3.one,
                agentHeight: 1.5f, agentRadius: 0.8f, agentSpeed: 5.0f,
                colCenter: new Vector3(0f, 0.75f, 0f), colRadius: 0.8f, colHeight: 1.5f,
                overrideMaterial: mothMaterial,
                visualRotation: new Vector3(-90f, 0f, 0f));

            UpdateLibrary(hawkMothPrefab: prefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Insect Wars] Hawk Moth setup complete! " +
                      "Make sure DefaultVisualLibrary is assigned on MapDirector.");
        }

        static void FixMothImportSettings()
        {
            var importer = AssetImporter.GetAtPath(MothModelPath) as ModelImporter;
            if (importer == null) return;

            bool dirty = false;

            if (importer.materialImportMode != ModelImporterMaterialImportMode.None)
            {
                importer.materialImportMode = ModelImporterMaterialImportMode.None;
                dirty = true;
            }
            if (!importer.isReadable)
            {
                importer.isReadable = true;
                dirty = true;
            }
            if (Mathf.Abs(importer.globalScale - 100.0f) > 0.01f)
            {
                importer.globalScale = 100.0f;
                dirty = true;
            }
            if (importer.animationType != ModelImporterAnimationType.Generic)
            {
                importer.animationType = ModelImporterAnimationType.Generic;
                dirty = true;
            }

            if (dirty)
            {
                importer.SaveAndReimport();
                Debug.Log("[Insect Wars] Fixed Hawk Moth FBX import settings (scale=100.0, generic rig).");
            }
        }

        static Material BuildMothMaterial()
        {
            EnsureDirectory(PrefabDir);

            var sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null)
            {
                Debug.LogError("[Insect Wars] URP Lit shader not found.");
                return null;
            }

            var mat = AssetDatabase.LoadAssetAtPath<Material>(MothMaterialPath);
            if (mat == null)
            {
                mat = new Material(sh);
                AssetDatabase.CreateAsset(mat, MothMaterialPath);
            }
            else
            {
                mat.shader = sh;
            }

            mat.SetColor("_BaseColor", Color.white);

            mat.SetFloat("_Surface", 0f);
            mat.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.SetFloat("_Blend", 0f);
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
            mat.SetOverrideTag("RenderType", "Opaque");

            var baseTex = AssetDatabase.LoadAssetAtPath<Texture2D>(MothBaseTexPath);
            if (baseTex != null)
                mat.SetTexture("_BaseMap", baseTex);

            var normalTex = AssetDatabase.LoadAssetAtPath<Texture2D>(MothNormalTexPath);
            if (normalTex != null)
            {
                mat.SetTexture("_BumpMap", normalTex);
                mat.EnableKeyword("_NORMALMAP");
            }

            mat.SetTexture("_EmissionMap", null);
            mat.SetColor("_EmissionColor", Color.black);
            mat.DisableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;

            var roughnessTex = AssetDatabase.LoadAssetAtPath<Texture2D>(MothRoughnessTexPath);
            if (roughnessTex != null)
                mat.SetFloat("_Smoothness", 0.45f);

            var metallicTex = AssetDatabase.LoadAssetAtPath<Texture2D>(MothMetallicTexPath);
            if (metallicTex != null)
                mat.SetTexture("_MetallicGlossMap", metallicTex);

            mat.SetFloat("_Metallic", 0.1f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static AnimatorController BuildHawkMothController()
        {
            EnsureDirectory(ControllerDir);
            DeleteIfExists(MothControllerPath);

            var c = AnimatorController.CreateAnimatorControllerAtPath(MothControllerPath);
            AddStandardParameters(c);

            var sm = c.layers[0].stateMachine;

            var walkClip = ExtractClip(MothModelPath);

            var idleState = sm.AddState("Idle");
            idleState.motion = walkClip;
            idleState.speed = 0f;
            sm.defaultState = idleState;

            var walkState = sm.AddState("Walk");
            walkState.motion = walkClip;

            AddLocomotionTransitions(idleState, walkState);

            EditorUtility.SetDirty(c);
            return c;
        }

        static void FixWidowImportSettings()
        {
            var importer = AssetImporter.GetAtPath(WidowModelPath) as ModelImporter;
            if (importer == null) return;

            bool dirty = false;

            if (importer.materialImportMode != ModelImporterMaterialImportMode.None)
            {
                importer.materialImportMode = ModelImporterMaterialImportMode.None;
                dirty = true;
            }
            if (!importer.isReadable)
            {
                importer.isReadable = true;
                dirty = true;
            }
            if (Mathf.Abs(importer.globalScale - 80f) > 0.01f)
            {
                importer.globalScale = 80f;
                dirty = true;
            }
            if (importer.animationType != ModelImporterAnimationType.Generic)
            {
                importer.animationType = ModelImporterAnimationType.Generic;
                dirty = true;
            }
            if (importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel)
            {
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                dirty = true;
            }

            var clips = importer.clipAnimations;
            if (clips == null || clips.Length == 0)
            {
                var defaultClips = importer.defaultClipAnimations;
                if (defaultClips.Length > 0)
                {
                    foreach (var c in defaultClips)
                    {
                        c.loopTime = true;
                        c.loopPose = true;
                    }
                    importer.clipAnimations = defaultClips;
                    dirty = true;
                }
            }
            else
            {
                bool clipsChanged = false;
                foreach (var c in clips)
                {
                    if (!c.loopTime) { c.loopTime = true; clipsChanged = true; }
                    if (!c.loopPose) { c.loopPose = true; clipsChanged = true; }
                }
                if (clipsChanged)
                {
                    importer.clipAnimations = clips;
                    dirty = true;
                }
            }

            if (dirty)
            {
                importer.SaveAndReimport();
                Debug.Log("[Insect Wars] Fixed Black Widow FBX import settings (scale=80, Generic rig, avatar, looping clips).");
            }
        }

        static Material BuildWidowMaterial()
        {
            EnsureDirectory(PrefabDir);

            var sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null)
            {
                Debug.LogError("[Insect Wars] URP Lit shader not found.");
                return null;
            }

            var mat = AssetDatabase.LoadAssetAtPath<Material>(WidowMaterialPath);
            if (mat == null)
            {
                mat = new Material(sh);
                AssetDatabase.CreateAsset(mat, WidowMaterialPath);
            }
            else
            {
                mat.shader = sh;
            }

            var baseTex = AssetDatabase.LoadAssetAtPath<Texture2D>(WidowBaseTexPath);
            if (baseTex != null)
                mat.SetTexture("_BaseMap", baseTex);

            var normalTex = AssetDatabase.LoadAssetAtPath<Texture2D>(WidowNormalTexPath);
            if (normalTex != null)
            {
                mat.SetTexture("_BumpMap", normalTex);
                mat.EnableKeyword("_NORMALMAP");
            }

            var emissionTex = AssetDatabase.LoadAssetAtPath<Texture2D>(WidowEmissionTexPath);
            if (emissionTex != null)
            {
                mat.SetTexture("_EmissionMap", emissionTex);
                mat.SetColor("_EmissionColor", Color.white * 0.5f);
                mat.EnableKeyword("_EMISSION");
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }

            var roughnessTex = AssetDatabase.LoadAssetAtPath<Texture2D>(WidowRoughnessTexPath);
            if (roughnessTex != null)
                mat.SetFloat("_Smoothness", 0.4f);

            mat.SetFloat("_Metallic", 0.15f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static AnimatorController BuildBlackWidowController()
        {
            EnsureDirectory(ControllerDir);
            DeleteIfExists(WidowControllerPath);

            var c = AnimatorController.CreateAnimatorControllerAtPath(WidowControllerPath);
            AddStandardParameters(c);
            c.AddParameter("WebCast", AnimatorControllerParameterType.Trigger);

            var sm = c.layers[0].stateMachine;

            var walkClip = ExtractClip(WidowModelPath);

            var idleState = sm.AddState("Idle");
            idleState.motion = walkClip;
            idleState.speed = 0f;
            sm.defaultState = idleState;

            var walkState = sm.AddState("Walk");
            walkState.motion = walkClip;

            AddLocomotionTransitions(idleState, walkState);

            EditorUtility.SetDirty(c);
            return c;
        }

        static void FixNestImportSettings()
        {
            var importer = AssetImporter.GetAtPath(NestModelPath) as ModelImporter;
            if (importer == null) return;

            bool dirty = false;

            if (importer.materialImportMode != ModelImporterMaterialImportMode.None)
            {
                importer.materialImportMode = ModelImporterMaterialImportMode.None;
                dirty = true;
            }
            if (!importer.isReadable)
            {
                importer.isReadable = true;
                dirty = true;
            }
            if (Mathf.Abs(importer.globalScale - 80f) > 0.01f)
            {
                importer.globalScale = 80f;
                dirty = true;
            }
            if (importer.animationType != ModelImporterAnimationType.None)
            {
                importer.animationType = ModelImporterAnimationType.None;
                dirty = true;
            }

            if (dirty)
            {
                importer.SaveAndReimport();
                Debug.Log("[Insect Wars] Fixed nest FBX import settings (scale=80, no rig).");
            }
        }

        static Material BuildNestMaterial()
        {
            EnsureDirectory(PrefabDir);

            var sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null)
            {
                Debug.LogError("[Insect Wars] URP Lit shader not found.");
                return null;
            }

            var mat = AssetDatabase.LoadAssetAtPath<Material>(NestMaterialPath);
            if (mat == null)
            {
                mat = new Material(sh);
                AssetDatabase.CreateAsset(mat, NestMaterialPath);
            }
            else
            {
                mat.shader = sh;
            }

            var baseTex = AssetDatabase.LoadAssetAtPath<Texture2D>(NestBaseTexPath);
            if (baseTex != null)
                mat.SetTexture("_BaseMap", baseTex);

            var normalTex = AssetDatabase.LoadAssetAtPath<Texture2D>(NestNormalTexPath);
            if (normalTex != null)
            {
                mat.SetTexture("_BumpMap", normalTex);
                mat.EnableKeyword("_NORMALMAP");
            }

            var metallicTex = AssetDatabase.LoadAssetAtPath<Texture2D>(NestMetallicTexPath);
            if (metallicTex != null)
                mat.SetTexture("_MetallicGlossMap", metallicTex);

            var roughnessTex = AssetDatabase.LoadAssetAtPath<Texture2D>(NestRoughnessTexPath);
            if (roughnessTex != null)
                mat.SetFloat("_Smoothness", 0.5f);

            mat.SetFloat("_Metallic", 0.2f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static void FixSkyTowerImportSettings()
        {
            var importer = AssetImporter.GetAtPath(SkyTowerModelPath) as ModelImporter;
            if (importer == null) return;

            bool dirty = false;

            if (importer.materialImportMode != ModelImporterMaterialImportMode.None)
            {
                importer.materialImportMode = ModelImporterMaterialImportMode.None;
                dirty = true;
            }
            if (!importer.isReadable)
            {
                importer.isReadable = true;
                dirty = true;
            }
            if (Mathf.Abs(importer.globalScale - 80f) > 0.01f)
            {
                importer.globalScale = 80f;
                dirty = true;
            }
            if (importer.animationType != ModelImporterAnimationType.None)
            {
                importer.animationType = ModelImporterAnimationType.None;
                dirty = true;
            }

            if (dirty)
            {
                importer.SaveAndReimport();
                Debug.Log("[Insect Wars] Fixed Sky Tower FBX import settings (scale=80, no rig).");
            }
        }

        static Material BuildSkyTowerMaterial()
        {
            EnsureDirectory(PrefabDir);

            var sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null)
            {
                Debug.LogError("[Insect Wars] URP Lit shader not found.");
                return null;
            }

            var mat = AssetDatabase.LoadAssetAtPath<Material>(SkyTowerMaterialPath);
            if (mat == null)
            {
                mat = new Material(sh);
                AssetDatabase.CreateAsset(mat, SkyTowerMaterialPath);
            }
            else
            {
                mat.shader = sh;
            }

            mat.SetColor("_BaseColor", Color.white);

            var baseTex = AssetDatabase.LoadAssetAtPath<Texture2D>(SkyTowerBaseTexPath);
            if (baseTex != null)
                mat.SetTexture("_BaseMap", baseTex);

            var normalTex = AssetDatabase.LoadAssetAtPath<Texture2D>(SkyTowerNormalTexPath);
            if (normalTex != null)
            {
                mat.SetTexture("_BumpMap", normalTex);
                mat.EnableKeyword("_NORMALMAP");
            }

            mat.SetTexture("_EmissionMap", null);
            mat.SetColor("_EmissionColor", Color.black);
            mat.DisableKeyword("_EMISSION");

            var metallicTex = AssetDatabase.LoadAssetAtPath<Texture2D>(SkyTowerMetallicTexPath);
            if (metallicTex != null)
                mat.SetTexture("_MetallicGlossMap", metallicTex);

            var roughnessTex = AssetDatabase.LoadAssetAtPath<Texture2D>(SkyTowerRoughnessTexPath);
            if (roughnessTex != null)
                mat.SetFloat("_Smoothness", 0.5f);

            mat.SetFloat("_Metallic", 0.2f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static GameObject BuildHivePrefab(string modelPath, string prefabPath, string rootName,
            Vector3 visualScale, Material overrideMaterial,
            Vector3 colCenter, Vector3 colSize)
        {
            EnsureDirectory(PrefabDir);

            var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            var root = new GameObject(rootName);
            root.tag = "Hive";

            var visualGo = Object.Instantiate(modelAsset);
            visualGo.name = "Visual";
            visualGo.transform.SetParent(root.transform, false);
            visualGo.transform.localScale = visualScale;

            if (overrideMaterial != null)
            {
                foreach (var smr in visualGo.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    var mats = smr.sharedMaterials;
                    for (int i = 0; i < mats.Length; i++) mats[i] = overrideMaterial;
                    smr.sharedMaterials = mats;
                }
                foreach (var mr in visualGo.GetComponentsInChildren<MeshRenderer>(true))
                {
                    var mats = mr.sharedMaterials;
                    for (int i = 0; i < mats.Length; i++) mats[i] = overrideMaterial;
                    mr.sharedMaterials = mats;
                }
            }

            var col = root.AddComponent<BoxCollider>();
            col.center = colCenter;
            col.size = colSize;
            col.isTrigger = true;

            var modifier = root.AddComponent<NavMeshModifier>();
            modifier.ignoreFromBuild = true;

            root.AddComponent<InsectWars.RTS.HiveDeposit>();
            root.AddComponent<InsectWars.RTS.HiveVisual>();

            var allRenderers = visualGo.GetComponentsInChildren<Renderer>(true);
            foreach (var r in allRenderers)
            {
                Debug.Log($"[Insect Wars] Nest Renderer '{r.name}': type={r.GetType().Name}, " +
                          $"bounds={r.bounds.size}, enabled={r.enabled}, " +
                          $"material={(r.sharedMaterial != null ? r.sharedMaterial.name : "NULL")}");
            }
            Debug.Log($"[Insect Wars] Nest Visual localScale={visualGo.transform.localScale}");

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);

            EditorUtility.SetDirty(prefab);
            return prefab;
        }

        static GameObject BuildBuildingPrefab(string modelPath, string prefabPath, string rootName,
            Vector3 visualScale, Material overrideMaterial,
            Vector3 colCenter, Vector3 colSize)
        {
            EnsureDirectory(PrefabDir);

            var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            var root = new GameObject(rootName);

            var visualGo = Object.Instantiate(modelAsset);
            visualGo.name = "Visual";
            visualGo.transform.SetParent(root.transform, false);
            visualGo.transform.localScale = visualScale;

            if (overrideMaterial != null)
            {
                foreach (var smr in visualGo.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    var mats = smr.sharedMaterials;
                    for (int i = 0; i < mats.Length; i++) mats[i] = overrideMaterial;
                    smr.sharedMaterials = mats;
                }
                foreach (var mr in visualGo.GetComponentsInChildren<MeshRenderer>(true))
                {
                    var mats = mr.sharedMaterials;
                    for (int i = 0; i < mats.Length; i++) mats[i] = overrideMaterial;
                    mr.sharedMaterials = mats;
                }
            }

            var col = root.AddComponent<BoxCollider>();
            col.center = colCenter;
            col.size = colSize;
            col.isTrigger = true;

            var modifier = root.AddComponent<NavMeshModifier>();
            modifier.ignoreFromBuild = true;

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);

            EditorUtility.SetDirty(prefab);
            return prefab;
        }

        static void FixMantisImportSettings()
        {
            var importer = AssetImporter.GetAtPath(MantisModelPath) as ModelImporter;
            if (importer == null) return;

            bool dirty = false;

            if (importer.materialImportMode != ModelImporterMaterialImportMode.None)
            {
                importer.materialImportMode = ModelImporterMaterialImportMode.None;
                dirty = true;
            }
            if (!importer.isReadable)
            {
                importer.isReadable = true;
                dirty = true;
            }
            if (Mathf.Abs(importer.globalScale - 80f) > 0.01f)
            {
                importer.globalScale = 80f;
                dirty = true;
            }
            if (importer.animationType != ModelImporterAnimationType.Generic)
            {
                importer.animationType = ModelImporterAnimationType.Generic;
                dirty = true;
            }
            if (importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel)
            {
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                dirty = true;
            }

            var clips = importer.clipAnimations;
            if (clips == null || clips.Length == 0)
            {
                var defaultClips = importer.defaultClipAnimations;
                if (defaultClips.Length > 0)
                {
                    foreach (var c in defaultClips)
                    {
                        c.loopTime = true;
                        c.loopPose = true;
                    }
                    importer.clipAnimations = defaultClips;
                    dirty = true;
                }
            }
            else
            {
                bool clipsChanged = false;
                foreach (var c in clips)
                {
                    if (!c.loopTime) { c.loopTime = true; clipsChanged = true; }
                    if (!c.loopPose) { c.loopPose = true; clipsChanged = true; }
                }
                if (clipsChanged)
                {
                    importer.clipAnimations = clips;
                    dirty = true;
                }
            }

            if (dirty)
            {
                importer.SaveAndReimport();
                Debug.Log("[Insect Wars] Fixed mantis FBX import settings (scale=80, Generic rig, avatar, looping clips).");
            }
        }

        static void FixAntImportSettings()
        {
            var importer = AssetImporter.GetAtPath(AntModelPath) as ModelImporter;
            if (importer == null) return;

            bool dirty = false;

            if (importer.materialImportMode != ModelImporterMaterialImportMode.None)
            {
                importer.materialImportMode = ModelImporterMaterialImportMode.None;
                dirty = true;
            }
            if (!importer.isReadable)
            {
                importer.isReadable = true;
                dirty = true;
            }
            if (Mathf.Abs(importer.globalScale - 80f) > 0.01f)
            {
                importer.globalScale = 80f;
                dirty = true;
            }
            if (importer.animationType != ModelImporterAnimationType.Generic)
            {
                importer.animationType = ModelImporterAnimationType.Generic;
                dirty = true;
            }
            if (importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel)
            {
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                dirty = true;
            }

            var clips = importer.clipAnimations;
            if (clips == null || clips.Length == 0)
            {
                var defaultClips = importer.defaultClipAnimations;
                if (defaultClips.Length > 0)
                {
                    foreach (var c in defaultClips)
                    {
                        c.loopTime = true;
                        c.loopPose = true;
                    }
                    importer.clipAnimations = defaultClips;
                    dirty = true;
                }
            }
            else
            {
                bool clipsChanged = false;
                foreach (var c in clips)
                {
                    if (!c.loopTime) { c.loopTime = true; clipsChanged = true; }
                    if (!c.loopPose) { c.loopPose = true; clipsChanged = true; }
                }
                if (clipsChanged)
                {
                    importer.clipAnimations = clips;
                    dirty = true;
                }
            }

            if (dirty)
            {
                importer.SaveAndReimport();
                Debug.Log("[Insect Wars] Fixed ant FBX import settings (scale=80, Generic rig, avatar, looping clips).");
            }
        }

        // ----------------------------------------------------------------
        // Ant Worker animator (walk clip is embedded in the model FBX)
        // ----------------------------------------------------------------

        static AnimatorController BuildAntWorkerController()
        {
            EnsureDirectory(ControllerDir);
            DeleteIfExists(AntControllerPath);

            var c = AnimatorController.CreateAnimatorControllerAtPath(AntControllerPath);
            AddStandardParameters(c);

            var sm = c.layers[0].stateMachine;

            var walkClip = ExtractClip(AntModelPath);

            var idleState = sm.AddState("Idle");
            idleState.motion = walkClip;
            idleState.speed = 0f;
            sm.defaultState = idleState;

            var walkState = sm.AddState("Walk");
            walkState.motion = walkClip;

            AddLocomotionTransitions(idleState, walkState);

            EditorUtility.SetDirty(c);
            return c;
        }

        // ----------------------------------------------------------------
        // Mantis Fighter animator (walk clip embedded in the model FBX)
        // ----------------------------------------------------------------

        static AnimatorController BuildMantisFighterController()
        {
            EnsureDirectory(ControllerDir);
            DeleteIfExists(MantisControllerPath);

            var c = AnimatorController.CreateAnimatorControllerAtPath(MantisControllerPath);
            AddStandardParameters(c);

            var sm = c.layers[0].stateMachine;

            var walkClip = ExtractClip(MantisModelPath);

            var idleState = sm.AddState("Idle");
            idleState.motion = walkClip;
            idleState.speed = 0f;
            sm.defaultState = idleState;

            var walkState = sm.AddState("Walk");
            walkState.motion = walkClip;

            AddLocomotionTransitions(idleState, walkState);

            EditorUtility.SetDirty(c);
            return c;
        }

        static Material BuildMantisMaterial()
        {
            EnsureDirectory(PrefabDir);

            var sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null)
            {
                Debug.LogError("[Insect Wars] URP Lit shader not found.");
                return null;
            }

            var mat = AssetDatabase.LoadAssetAtPath<Material>(MantisMaterialPath);
            if (mat == null)
            {
                mat = new Material(sh);
                AssetDatabase.CreateAsset(mat, MantisMaterialPath);
            }
            else
            {
                mat.shader = sh;
            }

            var baseTex = AssetDatabase.LoadAssetAtPath<Texture2D>(MantisBaseTexPath);
            if (baseTex != null)
                mat.SetTexture("_BaseMap", baseTex);

            var metallicTex = AssetDatabase.LoadAssetAtPath<Texture2D>(MantisMetallicTexPath);
            if (metallicTex != null)
                mat.SetTexture("_MetallicGlossMap", metallicTex);

            var roughnessTex = AssetDatabase.LoadAssetAtPath<Texture2D>(MantisRoughnessTexPath);
            if (roughnessTex != null)
            {
                mat.SetTexture("_MetallicGlossMap", metallicTex);
                mat.SetFloat("_Smoothness", 0.5f);
            }

            mat.SetFloat("_Metallic", 0.3f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // ----------------------------------------------------------------
        // Bombardier Beetle import / material / animator
        // ----------------------------------------------------------------

        static void FixBeetleImportSettings()
        {
            var importer = AssetImporter.GetAtPath(BeetleModelPath) as ModelImporter;
            if (importer == null) return;

            bool dirty = false;

            if (importer.materialImportMode != ModelImporterMaterialImportMode.None)
            {
                importer.materialImportMode = ModelImporterMaterialImportMode.None;
                dirty = true;
            }
            if (!importer.isReadable)
            {
                importer.isReadable = true;
                dirty = true;
            }
            if (Mathf.Abs(importer.globalScale - 80f) > 0.01f)
            {
                importer.globalScale = 80f;
                dirty = true;
            }
            if (importer.animationType != ModelImporterAnimationType.Generic)
            {
                importer.animationType = ModelImporterAnimationType.Generic;
                dirty = true;
            }
            if (importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel)
            {
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                dirty = true;
            }

            var clips = importer.clipAnimations;
            if (clips == null || clips.Length == 0)
            {
                var defaultClips = importer.defaultClipAnimations;
                if (defaultClips.Length > 0)
                {
                    foreach (var c in defaultClips)
                    {
                        c.loopTime = true;
                        c.loopPose = true;
                    }
                    importer.clipAnimations = defaultClips;
                    dirty = true;
                }
            }
            else
            {
                bool clipsChanged = false;
                foreach (var c in clips)
                {
                    if (!c.loopTime) { c.loopTime = true; clipsChanged = true; }
                    if (!c.loopPose) { c.loopPose = true; clipsChanged = true; }
                }
                if (clipsChanged)
                {
                    importer.clipAnimations = clips;
                    dirty = true;
                }
            }

            if (dirty)
            {
                importer.SaveAndReimport();
                Debug.Log("[Insect Wars] Fixed beetle FBX import settings (scale=80, Generic rig, avatar, looping clips).");
            }
        }

        static Material BuildBeetleMaterial()
        {
            EnsureDirectory(PrefabDir);

            var sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null)
            {
                Debug.LogError("[Insect Wars] URP Lit shader not found.");
                return null;
            }

            var mat = AssetDatabase.LoadAssetAtPath<Material>(BeetleMaterialPath);
            if (mat == null)
            {
                mat = new Material(sh);
                AssetDatabase.CreateAsset(mat, BeetleMaterialPath);
            }
            else
            {
                mat.shader = sh;
            }

            var baseTex = AssetDatabase.LoadAssetAtPath<Texture2D>(BeetleBaseTexPath);
            if (baseTex != null)
                mat.SetTexture("_BaseMap", baseTex);

            var normalTex = AssetDatabase.LoadAssetAtPath<Texture2D>(BeetleNormalTexPath);
            if (normalTex != null)
            {
                mat.SetTexture("_BumpMap", normalTex);
                mat.EnableKeyword("_NORMALMAP");
            }

            var metallicTex = AssetDatabase.LoadAssetAtPath<Texture2D>(BeetleMetallicTexPath);
            if (metallicTex != null)
                mat.SetTexture("_MetallicGlossMap", metallicTex);

            var roughnessTex = AssetDatabase.LoadAssetAtPath<Texture2D>(BeetleRoughnessTexPath);
            if (roughnessTex != null)
                mat.SetFloat("_Smoothness", 0.5f);

            mat.SetFloat("_Metallic", 0.25f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static AnimatorController BuildBeetleController()
        {
            EnsureDirectory(ControllerDir);
            DeleteIfExists(BeetleControllerPath);

            var c = AnimatorController.CreateAnimatorControllerAtPath(BeetleControllerPath);
            AddStandardParameters(c);

            var sm = c.layers[0].stateMachine;

            var walkClip = ExtractClip(BeetleModelPath);

            var idleState = sm.AddState("Idle");
            idleState.motion = walkClip;
            idleState.speed = 0f;
            sm.defaultState = idleState;

            var walkState = sm.AddState("Walk");
            walkState.motion = walkClip;

            AddLocomotionTransitions(idleState, walkState);

            EditorUtility.SetDirty(c);
            return c;
        }

        // ----------------------------------------------------------------
        // Shared helpers
        // ----------------------------------------------------------------

        static void AddStandardParameters(AnimatorController c)
        {
            c.AddParameter("Speed", AnimatorControllerParameterType.Float);
            c.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
            c.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            c.AddParameter("Gathering", AnimatorControllerParameterType.Bool);
            c.AddParameter("Death", AnimatorControllerParameterType.Trigger);
        }

        static void AddLocomotionTransitions(AnimatorState idle, AnimatorState walk)
        {
            var toWalk = idle.AddTransition(walk);
            toWalk.AddCondition(AnimatorConditionMode.If, 0, "IsMoving");
            toWalk.hasExitTime = false;
            toWalk.duration = 0.15f;

            var toIdle = walk.AddTransition(idle);
            toIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsMoving");
            toIdle.hasExitTime = false;
            toIdle.duration = 0.15f;
        }

        static void AddAttackTransitions(AnimatorStateMachine sm,
            AnimatorState idle, AnimatorState attack)
        {
            var anyToAttack = sm.AddAnyStateTransition(attack);
            anyToAttack.AddCondition(AnimatorConditionMode.If, 0, "Attack");
            anyToAttack.hasExitTime = false;
            anyToAttack.duration = 0.1f;

            var toIdle = attack.AddTransition(idle);
            toIdle.hasExitTime = true;
            toIdle.exitTime = 0.9f;
            toIdle.duration = 0.15f;
        }

        static GameObject BuildPrefab(string modelPath, string prefabPath, string rootName,
            AnimatorController controller, Vector3 visualScale,
            float agentHeight, float agentRadius, float agentSpeed,
            Vector3 colCenter, float colRadius, float colHeight,
            Material overrideMaterial = null, Vector3? visualRotation = null)
        {
            EnsureDirectory(PrefabDir);

            var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            var root = new GameObject(rootName);

            var visualGo = Object.Instantiate(modelAsset);
            visualGo.name = "Visual";
            visualGo.transform.SetParent(root.transform, false);
            visualGo.transform.localScale = visualScale;
            if (visualRotation.HasValue)
                visualGo.transform.localEulerAngles = visualRotation.Value;

            var anim = visualGo.GetComponent<Animator>();
            if (anim == null) anim = visualGo.AddComponent<Animator>();
            anim.runtimeAnimatorController = controller;
            anim.applyRootMotion = false;

            if (anim.avatar == null)
            {
                var allAssets = AssetDatabase.LoadAllAssetsAtPath(modelPath);
                foreach (var a in allAssets)
                {
                    if (a is Avatar avatar)
                    {
                        anim.avatar = avatar;
                        Debug.Log($"[Insect Wars] Assigned avatar '{avatar.name}' to Animator.");
                        break;
                    }
                }
            }

            var smrs = visualGo.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (var smr in smrs)
            {
                smr.updateWhenOffscreen = true;
                if (overrideMaterial != null)
                {
                    var mats = smr.sharedMaterials;
                    for (int i = 0; i < mats.Length; i++) mats[i] = overrideMaterial;
                    smr.sharedMaterials = mats;
                }
            }
            if (overrideMaterial != null)
            {
                var mrs = visualGo.GetComponentsInChildren<MeshRenderer>(true);
                foreach (var mr in mrs)
                {
                    var mats = mr.sharedMaterials;
                    for (int i = 0; i < mats.Length; i++) mats[i] = overrideMaterial;
                    mr.sharedMaterials = mats;
                }
                Debug.Log($"[Insect Wars] Applied material to {smrs.Length} SkinnedMeshRenderers, {mrs.Length} MeshRenderers.");
            }

            var agent = root.AddComponent<NavMeshAgent>();
            agent.height = agentHeight;
            agent.radius = agentRadius;
            agent.acceleration = 48f;
            agent.angularSpeed = 520f;
            agent.speed = agentSpeed;

            var col = root.AddComponent<CapsuleCollider>();
            col.center = colCenter;
            col.radius = colRadius;
            col.height = colHeight;

            // Log diagnostic info about the model
            var allRenderers = visualGo.GetComponentsInChildren<Renderer>(true);
            foreach (var r in allRenderers)
            {
                Debug.Log($"[Insect Wars] Renderer '{r.name}': type={r.GetType().Name}, " +
                          $"bounds={r.bounds.size}, enabled={r.enabled}, " +
                          $"material={(r.sharedMaterial != null ? r.sharedMaterial.name : "NULL")}, " +
                          $"shader={(r.sharedMaterial != null ? r.sharedMaterial.shader.name : "N/A")}");
            }
            Debug.Log($"[Insect Wars] Visual localScale={visualGo.transform.localScale}, " +
                      $"childCount={visualGo.transform.childCount}");

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);

            EditorUtility.SetDirty(prefab);
            return prefab;
        }

        static Material BuildAntMaterial()
        {
            EnsureDirectory(PrefabDir);

            var sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null)
            {
                Debug.LogError("[Insect Wars] URP Lit shader not found.");
                return null;
            }

            var mat = AssetDatabase.LoadAssetAtPath<Material>(AntMaterialPath);
            if (mat == null)
            {
                mat = new Material(sh);
                AssetDatabase.CreateAsset(mat, AntMaterialPath);
            }
            else
            {
                mat.shader = sh;
            }

            var baseTex = AssetDatabase.LoadAssetAtPath<Texture2D>(AntBaseTexPath);
            if (baseTex != null)
                mat.SetTexture("_BaseMap", baseTex);

            var metallicTex = AssetDatabase.LoadAssetAtPath<Texture2D>(AntMetallicTexPath);
            if (metallicTex != null)
                mat.SetTexture("_MetallicGlossMap", metallicTex);

            var roughnessTex = AssetDatabase.LoadAssetAtPath<Texture2D>(AntRoughnessTexPath);
            if (roughnessTex != null)
            {
                mat.SetTexture("_MetallicGlossMap", metallicTex);
                mat.SetFloat("_Smoothness", 0.5f);
            }

            mat.SetFloat("_Metallic", 0.3f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static AnimationClip ExtractClip(string fbxPath)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            foreach (var a in assets)
            {
                if (a is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                    return clip;
            }
            Debug.LogWarning($"[Insect Wars] No AnimationClip found in {fbxPath}");
            return null;
        }

        static void UpdateLibrary(GameObject workerPrefab = null, GameObject meleePrefab = null,
            GameObject rangedPrefab = null, GameObject hivePrefab = null,
            GameObject blackWidowPrefab = null, GameObject hawkMothPrefab = null,
            GameObject skyTowerPrefab = null)
        {
            EnsureDirectory(LibraryDir);

            var lib = AssetDatabase.LoadAssetAtPath<UnitVisualLibrary>(LibraryPath);
            if (lib == null)
            {
                lib = ScriptableObject.CreateInstance<UnitVisualLibrary>();
                AssetDatabase.CreateAsset(lib, LibraryPath);
            }

            if (workerPrefab != null) lib.workerPrefab = workerPrefab;
            if (meleePrefab != null) lib.meleePrefab = meleePrefab;
            if (rangedPrefab != null) lib.rangedPrefab = rangedPrefab;
            if (hivePrefab != null) lib.hivePrefab = hivePrefab;
            if (blackWidowPrefab != null) lib.blackWidowPrefab = blackWidowPrefab;
            if (hawkMothPrefab != null) lib.hawkMothPrefab = hawkMothPrefab;
            if (skyTowerPrefab != null) lib.skyTowerPrefab = skyTowerPrefab;

            EditorUtility.SetDirty(lib);
        }

        static bool ValidateAssets(params string[] paths)
        {
            foreach (var p in paths)
            {
                if (AssetDatabase.LoadMainAssetAtPath(p) == null)
                {
                    Debug.LogError($"[Insect Wars] Missing asset: {p}");
                    return false;
                }
            }
            return true;
        }

        static void DeleteIfExists(string assetPath)
        {
            if (AssetDatabase.LoadMainAssetAtPath(assetPath) != null)
                AssetDatabase.DeleteAsset(assetPath);
        }

        static void EnsureDirectory(string assetDir)
        {
            var fullPath = Path.Combine(Application.dataPath, assetDir.Substring("Assets/".Length));
            if (!Directory.Exists(fullPath))
                Directory.CreateDirectory(fullPath);
        }
    }
}
