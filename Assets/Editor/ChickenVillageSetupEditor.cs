using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ChickenVillageSetupEditor
{
    private const string HenModelPath = "Assets/Art/Animals/AyamCemani/AyamCemaniHen.fbx";
    private const string RoosterModelPath = "Assets/Art/Animals/AyamCemani/AyamCemaniRooster.fbx";
    private const string EggModelPath = "Assets/Art/Animals/AyamCemani/ChickenEgg.fbx";
    private const string NestModelPath = "Assets/Art/Environment/Village/TraditionalBambooBroodingNest.fbx";
    private const string CoopWoodMaterialPath = "Assets/Materials/Village/ChickenCoop_Wood.mat";
    private const string VillageScenePath = "Assets/Scenes/VilageScene.unity";

    private const string ControllerFolder = "Assets/Animation/Animals";
    private const string ControllerPath = ControllerFolder + "/Chicken.controller";
    private const string RoosterOverridePath = ControllerFolder + "/ChickenRooster.overrideController";
    private const string PrefabFolder = "Assets/Prefabs/Village/Animals";
    private const string HenPrefabPath = PrefabFolder + "/AyamCemaniHen.prefab";
    private const string RoosterPrefabPath = PrefabFolder + "/AyamCemaniRooster.prefab";
    private const string GeneratedRootName = "Chicken Population (Generated)";

    private static readonly Vector3 PatrolCenter = new(-1.35f, 0f, -0.15f);
    private static readonly Vector2 PatrolSize = new(5f, 3.6f);
    private static readonly float[] NestPositionsX = { -3.45f, -2.35f, -1.25f, -0.15f };

    [MenuItem("Tools/Horror Project/Build Chicken Coop Population")]
    public static void BuildFromMenu()
    {
        BuildAll();
    }

    public static void BuildFromCommandLine()
    {
        try
        {
            BuildAll();
            Debug.Log("CHICKEN_VILLAGE_SETUP_OK");
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }

    public static void ValidateFromCommandLine()
    {
        try
        {
            ValidateAll();
            Debug.Log("CHICKEN_VILLAGE_VALIDATION_OK");
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }

    private static void BuildAll()
    {
        EnsureFolder(ControllerFolder);
        EnsureFolder(PrefabFolder);
        ImportRequiredAssets();

        AnimationClip henIdle = FindClip(HenModelPath, "IdlePeck");
        AnimationClip henWalk = FindClip(HenModelPath, "Walk");
        AnimationClip henIncubate = FindClip(HenModelPath, "IncubateEgg");
        AnimationClip roosterIdle = FindClip(RoosterModelPath, "IdlePeck");
        AnimationClip roosterWalk = FindClip(RoosterModelPath, "Walk");

        AnimatorController controller = CreateChickenController(henIdle, henWalk, henIncubate);
        AnimatorOverrideController roosterOverride = CreateRoosterOverride(
            controller,
            henIdle,
            henWalk,
            roosterIdle,
            roosterWalk);

        GameObject henPrefab = CreateChickenPrefab(
            HenModelPath,
            HenPrefabPath,
            "Ayam Cemani Hen",
            controller);
        GameObject roosterPrefab = CreateChickenPrefab(
            RoosterModelPath,
            RoosterPrefabPath,
            "Ayam Cemani Rooster",
            roosterOverride);

        PopulateVillageScene(henPrefab, roosterPrefab);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void ImportRequiredAssets()
    {
        string[] paths =
        {
            HenModelPath,
            RoosterModelPath,
            EggModelPath,
            NestModelPath,
            CoopWoodMaterialPath
        };
        foreach (string path in paths)
        {
            if (AssetDatabase.LoadMainAssetAtPath(path) == null)
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            }

            if (AssetDatabase.LoadMainAssetAtPath(path) == null)
            {
                throw new InvalidOperationException($"Required asset is missing: {path}");
            }
        }
    }

    private static AnimationClip FindClip(string modelPath, string clipName)
    {
        AnimationClip clip = AssetDatabase.LoadAllAssetsAtPath(modelPath)
            .OfType<AnimationClip>()
            .FirstOrDefault(candidate => string.Equals(candidate.name, clipName, StringComparison.Ordinal));
        if (clip == null)
        {
            string available = string.Join(", ", AssetDatabase.LoadAllAssetsAtPath(modelPath)
                .OfType<AnimationClip>()
                .Select(candidate => candidate.name));
            throw new InvalidOperationException(
                $"Animation clip '{clipName}' was not found in {modelPath}. Available: {available}");
        }

        return clip;
    }

    private static AnimatorController CreateChickenController(
        AnimationClip idle,
        AnimationClip walk,
        AnimationClip incubate)
    {
        AssetDatabase.DeleteAsset(ControllerPath);
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("IsIncubating", AnimatorControllerParameterType.Bool);

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        stateMachine.name = "Chicken Base Layer";
        AnimatorState locomotion = controller.CreateBlendTreeInController(
            "Locomotion",
            out BlendTree blendTree,
            0);
        blendTree.name = "Idle Walk Blend";
        blendTree.blendType = BlendTreeType.Simple1D;
        blendTree.blendParameter = "Speed";
        blendTree.useAutomaticThresholds = false;
        blendTree.AddChild(idle, 0f);
        blendTree.AddChild(walk, 1f);

        AnimatorState incubating = stateMachine.AddState("Incubating", new Vector3(430f, 80f));
        incubating.motion = incubate;
        stateMachine.defaultState = locomotion;
        SetStatePosition(stateMachine, locomotion, new Vector3(180f, 80f));

        AnimatorStateTransition enterIncubation = locomotion.AddTransition(incubating);
        ConfigureTransition(enterIncubation);
        enterIncubation.AddCondition(AnimatorConditionMode.If, 0f, "IsIncubating");

        AnimatorStateTransition leaveIncubation = incubating.AddTransition(locomotion);
        ConfigureTransition(leaveIncubation);
        leaveIncubation.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsIncubating");

        EditorUtility.SetDirty(blendTree);
        EditorUtility.SetDirty(stateMachine);
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        return controller;
    }

    private static void ConfigureTransition(AnimatorStateTransition transition)
    {
        transition.hasExitTime = false;
        transition.hasFixedDuration = true;
        transition.duration = 0.18f;
        transition.canTransitionToSelf = false;
        transition.interruptionSource = TransitionInterruptionSource.Source;
    }

    private static void SetStatePosition(
        AnimatorStateMachine stateMachine,
        AnimatorState state,
        Vector3 position)
    {
        ChildAnimatorState[] states = stateMachine.states;
        for (int index = 0; index < states.Length; index++)
        {
            if (states[index].state != state)
            {
                continue;
            }

            states[index].position = position;
            stateMachine.states = states;
            return;
        }
    }

    private static AnimatorOverrideController CreateRoosterOverride(
        RuntimeAnimatorController controller,
        AnimationClip henIdle,
        AnimationClip henWalk,
        AnimationClip roosterIdle,
        AnimationClip roosterWalk)
    {
        AssetDatabase.DeleteAsset(RoosterOverridePath);
        AnimatorOverrideController overrideController = new(controller)
        {
            name = "Chicken Rooster Override"
        };
        AssetDatabase.CreateAsset(overrideController, RoosterOverridePath);

        List<KeyValuePair<AnimationClip, AnimationClip>> overrides = new();
        overrideController.GetOverrides(overrides);
        for (int index = 0; index < overrides.Count; index++)
        {
            AnimationClip original = overrides[index].Key;
            if (original == henIdle)
            {
                overrides[index] = new KeyValuePair<AnimationClip, AnimationClip>(original, roosterIdle);
            }
            else if (original == henWalk)
            {
                overrides[index] = new KeyValuePair<AnimationClip, AnimationClip>(original, roosterWalk);
            }
        }

        overrideController.ApplyOverrides(overrides);
        EditorUtility.SetDirty(overrideController);
        AssetDatabase.SaveAssets();
        return overrideController;
    }

    private static GameObject CreateChickenPrefab(
        string modelPath,
        string prefabPath,
        string prefabName,
        RuntimeAnimatorController controller)
    {
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        if (model == null)
        {
            throw new InvalidOperationException($"Chicken model could not be loaded: {modelPath}");
        }

        AssetDatabase.DeleteAsset(prefabPath);
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
        instance.name = prefabName;
        Animator animator = instance.GetComponent<Animator>();
        if (animator == null)
        {
            animator = instance.AddComponent<Animator>();
        }

        animator.runtimeAnimatorController = controller;
        animator.avatar = AssetDatabase.LoadAllAssetsAtPath(modelPath)
            .OfType<Avatar>()
            .FirstOrDefault();
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;

        VillageChickenController chicken = instance.GetComponent<VillageChickenController>();
        if (chicken == null)
        {
            chicken = instance.AddComponent<VillageChickenController>();
        }

        chicken.Configure(null, PatrolCenter, PatrolSize, false);
        chicken.AssignAnimator(animator);
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
        UnityEngine.Object.DestroyImmediate(instance);
        if (prefab == null)
        {
            throw new InvalidOperationException($"Failed to save chicken prefab: {prefabPath}");
        }

        return prefab;
    }

    private static void PopulateVillageScene(GameObject henPrefab, GameObject roosterPrefab)
    {
        Scene scene = EditorSceneManager.OpenScene(VillageScenePath, OpenSceneMode.Single);
        VillageChickenCoopMesh coop = UnityEngine.Object.FindObjectsByType<VillageChickenCoopMesh>(
                FindObjectsInactive.Include)
            .FirstOrDefault(candidate => candidate.gameObject.scene == scene);
        if (coop == null)
        {
            throw new InvalidOperationException("VilageScene does not contain a VillageChickenCoopMesh.");
        }

        Transform existing = coop.transform.Find(GeneratedRootName);
        if (existing != null)
        {
            UnityEngine.Object.DestroyImmediate(existing.gameObject);
        }

        Transform generatedRoot = CreateGroup(GeneratedRootName, coop.transform);
        Transform nestGroup = CreateGroup("Nesting Baskets", generatedRoot);
        Transform animalGroup = CreateGroup("Wandering Chickens", generatedRoot);
        BuildBambooRack(generatedRoot);

        GameObject nestModel = AssetDatabase.LoadAssetAtPath<GameObject>(NestModelPath);
        GameObject eggModel = AssetDatabase.LoadAssetAtPath<GameObject>(EggModelPath);
        for (int index = 0; index < NestPositionsX.Length; index++)
        {
            GameObject nest = InstantiatePrefab(nestModel, nestGroup, $"Brooding Nest {index + 1:00}");
            nest.transform.localPosition = new Vector3(NestPositionsX[index], 0.12f, 2.08f);
            nest.transform.localRotation = Quaternion.identity;

            if (index >= 2)
            {
                continue;
            }

            GameObject egg = InstantiatePrefab(eggModel, nest.transform, $"Chicken Egg {index + 1:00}");
            egg.transform.localPosition = new Vector3(0f, 0.215f, -0.015f);
            egg.transform.localRotation = Quaternion.Euler(4f, index == 0 ? -6f : 7f, 0f);

            GameObject hen = InstantiatePrefab(henPrefab, nest.transform, $"Hen Incubating {index + 1:00}");
            hen.transform.localPosition = new Vector3(0f, 0.215f, 0f);
            hen.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            ConfigureChicken(hen, coop.transform, true);
        }

        Vector3[] henPositions =
        {
            new(-3.1f, 0f, -1.25f),
            new(-1.75f, 0f, -1.45f),
            new(-0.45f, 0f, -1.05f),
            new(0.65f, 0f, 0.65f)
        };
        for (int index = 0; index < henPositions.Length; index++)
        {
            GameObject hen = InstantiatePrefab(henPrefab, animalGroup, $"Hen Wandering {index + 3:00}");
            hen.transform.localPosition = henPositions[index];
            hen.transform.localRotation = Quaternion.Euler(0f, 35f + index * 73f, 0f);
            ConfigureChicken(hen, coop.transform, false);
        }

        Vector3[] roosterPositions =
        {
            new(-3.15f, 0f, 0.8f),
            new(0.45f, 0f, -0.35f)
        };
        for (int index = 0; index < roosterPositions.Length; index++)
        {
            GameObject rooster = InstantiatePrefab(
                roosterPrefab,
                animalGroup,
                $"Rooster Wandering {index + 1:00}");
            rooster.transform.localPosition = roosterPositions[index];
            rooster.transform.localRotation = Quaternion.Euler(0f, 145f + index * 110f, 0f);
            ConfigureChicken(rooster, coop.transform, false);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
        {
            throw new InvalidOperationException("Failed to save the populated VilageScene.");
        }
    }

    private static void BuildBambooRack(Transform parent)
    {
        Transform rack = CreateGroup("Bamboo Nest Rack", parent);
        Material material = AssetDatabase.LoadAssetAtPath<Material>(CoopWoodMaterialPath);
        CreateCylinder(
            "Hanging Rail",
            rack,
            new Vector3(-1.8f, 1.03f, 2.08f),
            new Vector3(0.07f, 2.1f, 0.07f),
            Quaternion.Euler(0f, 0f, 90f),
            material);
        CreateCylinder(
            "Left Support",
            rack,
            new Vector3(-3.85f, 0.515f, 2.08f),
            new Vector3(0.07f, 0.515f, 0.07f),
            Quaternion.identity,
            material);
        CreateCylinder(
            "Right Support",
            rack,
            new Vector3(0.25f, 0.515f, 2.08f),
            new Vector3(0.07f, 0.515f, 0.07f),
            Quaternion.identity,
            material);
    }

    private static void CreateCylinder(
        string name,
        Transform parent,
        Vector3 localPosition,
        Vector3 localScale,
        Quaternion localRotation,
        Material material)
    {
        GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cylinder.name = name;
        cylinder.transform.SetParent(parent, false);
        cylinder.transform.localPosition = localPosition;
        cylinder.transform.localRotation = localRotation;
        cylinder.transform.localScale = localScale;
        UnityEngine.Object.DestroyImmediate(cylinder.GetComponent<Collider>());
        cylinder.GetComponent<MeshRenderer>().sharedMaterial = material;
    }

    private static GameObject InstantiatePrefab(
        GameObject prefab,
        Transform parent,
        string instanceName)
    {
        if (prefab == null)
        {
            throw new InvalidOperationException($"Could not instantiate missing prefab '{instanceName}'.");
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = instanceName;
        instance.transform.SetParent(parent, false);
        return instance;
    }

    private static void ConfigureChicken(GameObject chickenObject, Transform coop, bool incubating)
    {
        VillageChickenController chicken = chickenObject.GetComponent<VillageChickenController>();
        if (chicken == null)
        {
            throw new InvalidOperationException($"{chickenObject.name} has no VillageChickenController.");
        }

        chicken.Configure(coop, PatrolCenter, PatrolSize, incubating);
        EditorUtility.SetDirty(chicken);
        PrefabUtility.RecordPrefabInstancePropertyModifications(chicken);
    }

    private static Transform CreateGroup(string name, Transform parent)
    {
        GameObject group = new(name);
        group.transform.SetParent(parent, false);
        return group.transform;
    }

    private static void EnsureFolder(string path)
    {
        string[] parts = path.Split('/');
        string current = parts[0];
        for (int index = 1; index < parts.Length; index++)
        {
            string next = current + "/" + parts[index];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[index]);
            }

            current = next;
        }
    }

    private static void ValidateAll()
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            throw new InvalidOperationException("Chicken Animator Controller is missing.");
        }

        Dictionary<string, AnimatorControllerParameterType> parameters = controller.parameters
            .ToDictionary(parameter => parameter.name, parameter => parameter.type);
        if (parameters.Count != 2 ||
            !parameters.TryGetValue("Speed", out AnimatorControllerParameterType speedType) ||
            speedType != AnimatorControllerParameterType.Float ||
            !parameters.TryGetValue("IsIncubating", out AnimatorControllerParameterType incubatingType) ||
            incubatingType != AnimatorControllerParameterType.Bool)
        {
            throw new InvalidOperationException("Chicken controller parameters are not configured correctly.");
        }

        AnimatorState[] states = controller.layers[0].stateMachine.states
            .Select(child => child.state)
            .ToArray();
        AnimatorState locomotion = states.Single(state => state.name == "Locomotion");
        AnimatorState incubating = states.Single(state => state.name == "Incubating");
        BlendTree blendTree = locomotion.motion as BlendTree;
        if (blendTree == null || blendTree.blendParameter != "Speed" || blendTree.children.Length != 2)
        {
            throw new InvalidOperationException("Chicken locomotion Blend Tree is not configured correctly.");
        }

        ChildMotion[] motions = blendTree.children;
        if (motions[0].motion.name != "IdlePeck" ||
            !Mathf.Approximately(motions[0].threshold, 0f) ||
            motions[1].motion.name != "Walk" ||
            !Mathf.Approximately(motions[1].threshold, 1f) ||
            incubating.motion == null ||
            incubating.motion.name != "IncubateEgg")
        {
            throw new InvalidOperationException("Chicken controller motions are not assigned correctly.");
        }

        AnimatorOverrideController roosterOverride = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(
            RoosterOverridePath);
        if (roosterOverride == null || roosterOverride.runtimeAnimatorController != controller)
        {
            throw new InvalidOperationException("Rooster Animator Override Controller is missing or invalid.");
        }

        List<KeyValuePair<AnimationClip, AnimationClip>> overrides = new();
        roosterOverride.GetOverrides(overrides);
        foreach (string clipName in new[] { "IdlePeck", "Walk" })
        {
            AnimationClip replacement = overrides
                .Single(pair => pair.Key.name == clipName)
                .Value;
            if (replacement == null || AssetDatabase.GetAssetPath(replacement) != RoosterModelPath)
            {
                throw new InvalidOperationException($"Rooster clip override is invalid: {clipName}");
            }
        }

        ValidateChickenPrefab(HenPrefabPath, controller);
        ValidateChickenPrefab(RoosterPrefabPath, roosterOverride);
        ValidateClipBindings(HenPrefabPath, FindClip(HenModelPath, "IdlePeck"));
        ValidateClipBindings(HenPrefabPath, FindClip(HenModelPath, "Walk"));
        ValidateClipBindings(HenPrefabPath, FindClip(HenModelPath, "IncubateEgg"));
        ValidateClipBindings(RoosterPrefabPath, FindClip(RoosterModelPath, "IdlePeck"));
        ValidateClipBindings(RoosterPrefabPath, FindClip(RoosterModelPath, "Walk"));

        Scene scene = EditorSceneManager.OpenScene(VillageScenePath, OpenSceneMode.Single);
        VillageChickenCoopMesh coop = UnityEngine.Object.FindObjectsByType<VillageChickenCoopMesh>(
                FindObjectsInactive.Include)
            .Single(candidate => candidate.gameObject.scene == scene);
        Transform generatedRoot = coop.transform.Find(GeneratedRootName);
        if (generatedRoot == null)
        {
            throw new InvalidOperationException("Generated chicken population is missing from the coop.");
        }

        VillageChickenController[] chickens = generatedRoot.GetComponentsInChildren<VillageChickenController>(true);
        int henCount = chickens.Count(chicken => chicken.name.StartsWith("Hen ", StringComparison.Ordinal));
        int roosterCount = chickens.Count(chicken => chicken.name.StartsWith("Rooster ", StringComparison.Ordinal));
        int incubatingCount = chickens.Count(chicken => chicken.IsIncubating);
        if (chickens.Length != 8 || henCount != 6 || roosterCount != 2 || incubatingCount != 2)
        {
            throw new InvalidOperationException(
                $"Chicken population is invalid: total={chickens.Length}, hens={henCount}, " +
                $"roosters={roosterCount}, incubating={incubatingCount}.");
        }

        Transform nestGroup = generatedRoot.Find("Nesting Baskets");
        if (nestGroup == null || nestGroup.childCount != 4)
        {
            throw new InvalidOperationException("The coop must contain exactly four arranged nesting baskets.");
        }

        int eggCount = 0;
        for (int index = 0; index < nestGroup.childCount; index++)
        {
            Transform nest = nestGroup.GetChild(index);
            if (!Mathf.Approximately(nest.localPosition.x, NestPositionsX[index]) ||
                !Mathf.Approximately(nest.localPosition.z, 2.08f))
            {
                throw new InvalidOperationException($"Brooding nest {index + 1} is not arranged correctly.");
            }

            eggCount += nest.GetComponentsInChildren<Transform>(true)
                .Count(child => child.name.StartsWith("Chicken Egg", StringComparison.Ordinal));
        }

        if (eggCount != 2 || generatedRoot.Find("Bamboo Nest Rack") == null)
        {
            throw new InvalidOperationException("The nesting rack or its two occupied eggs are missing.");
        }

        Debug.Log(
            $"Chicken validation: parameters={parameters.Count}, states={states.Length}, " +
            $"nests={nestGroup.childCount}, hens={henCount}, roosters={roosterCount}, " +
            $"incubating={incubatingCount}, eggs={eggCount}.");
    }

    private static void ValidateChickenPrefab(
        string prefabPath,
        RuntimeAnimatorController expectedController)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        Animator animator = prefab != null ? prefab.GetComponent<Animator>() : null;
        VillageChickenController chicken = prefab != null
            ? prefab.GetComponent<VillageChickenController>()
            : null;
        if (prefab == null || animator == null || chicken == null ||
            animator.runtimeAnimatorController != expectedController || animator.applyRootMotion)
        {
            throw new InvalidOperationException($"Chicken prefab is invalid: {prefabPath}");
        }
    }

    private static void ValidateClipBindings(string prefabPath, AnimationClip clip)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        string[] missingPaths = AnimationUtility.GetCurveBindings(clip)
            .Select(binding => binding.path)
            .Where(path => !string.IsNullOrEmpty(path) && prefab.transform.Find(path) == null)
            .Distinct()
            .ToArray();
        if (missingPaths.Length > 0)
        {
            throw new InvalidOperationException(
                $"Animation '{clip.name}' does not match {prefabPath}: " +
                string.Join(", ", missingPaths));
        }
    }
}
