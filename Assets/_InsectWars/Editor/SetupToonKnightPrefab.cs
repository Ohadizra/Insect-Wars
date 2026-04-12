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

        // --- Knight Fighter paths ---
        const string KnightModelPath = "Assets/Toon_RTS_demo/models/ToonRTS_demo_Knight.FBX";
        const string KnightAnimIdlePath = "Assets/Toon_RTS_demo/animations/WK_heavy_infantry_05_combat_idle.FBX";
        const string KnightAnimWalkPath = "Assets/Toon_RTS_demo/animations/WK_heavy_infantry_06_combat_walk.FBX";
        const string KnightAnimAttackPath = "Assets/Toon_RTS_demo/animations/WK_heavy_infantry_08_attack_B.FBX";
        const string KnightControllerPath = ControllerDir + "/KnightFighter.controller";
        const string KnightPrefabPath = PrefabDir + "/KnightFighter.prefab";

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

        // --- Ant Nest (Hive) paths ---
        const string NestModelPath = "Assets/Insects Art/Stractures/Ants nest/Meshy_AI_ant_s_nest_rts_buildi_0404152548_texture_fbx/Meshy_AI_ant_s_nest_rts_buildi_0404152548_texture.fbx";
        const string NestTexDir = "Assets/Insects Art/Stractures/Ants nest/Meshy_AI_ant_s_nest_rts_buildi_0404152548_texture_fbx";
        const string NestBaseTexPath = NestTexDir + "/Meshy_AI_ant_s_nest_rts_buildi_0404152548_texture.png";
        const string NestNormalTexPath = NestTexDir + "/Meshy_AI_ant_s_nest_rts_buildi_0404152548_texture_normal.png";
        const string NestMetallicTexPath = NestTexDir + "/Meshy_AI_ant_s_nest_rts_buildi_0404152548_texture_metallic.png";
        const string NestRoughnessTexPath = NestTexDir + "/Meshy_AI_ant_s_nest_rts_buildi_0404152548_texture_roughness.png";
        const string NestMaterialPath = PrefabDir + "/AntNestMat.mat";
        const string NestPrefabPath = PrefabDir + "/AntNest.prefab";

        // ----------------------------------------------------------------
        // Menu items
        // ----------------------------------------------------------------

        [MenuItem("Insect Wars/Setup All Units")]
        public static void SetupAll()
        {
            SetupMantisFighter();
            SetupAntWorker();
            SetupBombardierBeetle();
            SetupAntNest();
        }

        [MenuItem("Insect Wars/Setup Toon Knight Fighter")]
        public static void SetupKnightFighter()
        {
            if (!ValidateAssets(KnightModelPath, KnightAnimIdlePath, KnightAnimWalkPath, KnightAnimAttackPath))
                return;

            var controller = BuildKnightController();
            var prefab = BuildPrefab(KnightModelPath, KnightPrefabPath, "KnightFighter",
                controller, Vector3.one * 0.55f,
                agentHeight: 1.0f, agentRadius: 0.42f, agentSpeed: 5.4f,
                colCenter: new Vector3(0f, 0.5f, 0f), colRadius: 0.4f, colHeight: 1.0f);

            UpdateLibrary(meleePrefab: prefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Insect Wars] Knight fighter setup complete! " +
                      "Make sure DefaultVisualLibrary is assigned on MapDirector.");
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
        // Knight animator
        // ----------------------------------------------------------------

        static AnimatorController BuildKnightController()
        {
            EnsureDirectory(ControllerDir);
            DeleteIfExists(KnightControllerPath);

            var c = AnimatorController.CreateAnimatorControllerAtPath(KnightControllerPath);
            AddStandardParameters(c);

            var sm = c.layers[0].stateMachine;

            var idleState = sm.AddState("Idle");
            idleState.motion = ExtractClip(KnightAnimIdlePath);
            sm.defaultState = idleState;

            var walkState = sm.AddState("Walk");
            walkState.motion = ExtractClip(KnightAnimWalkPath);

            var attackState = sm.AddState("Attack");
            attackState.motion = ExtractClip(KnightAnimAttackPath);

            AddLocomotionTransitions(idleState, walkState);
            AddAttackTransitions(sm, idleState, attackState);

            EditorUtility.SetDirty(c);
            return c;
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
            Material overrideMaterial = null)
        {
            EnsureDirectory(PrefabDir);

            var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            var root = new GameObject(rootName);

            var visualGo = Object.Instantiate(modelAsset);
            visualGo.name = "Visual";
            visualGo.transform.SetParent(root.transform, false);
            visualGo.transform.localScale = visualScale;

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
            GameObject rangedPrefab = null, GameObject hivePrefab = null)
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
