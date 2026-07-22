using UnityEditor;
using UnityEngine;

public sealed class JendelaTerrainEditor : EditorWindow
{
    private KonfigurasiTerrain konfigurasi;
    private Terrain terrainTarget;

    [MenuItem("Tools/Terrain Prosedural/Generator")]
    private static void BukaJendela()
    {
        GetWindow<JendelaTerrainEditor>("Generator Terrain Prosedural");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Generator Terrain Prosedural", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        konfigurasi = (KonfigurasiTerrain)EditorGUILayout.ObjectField(
            "Konfigurasi",
            konfigurasi,
            typeof(KonfigurasiTerrain),
            false);

        terrainTarget = (Terrain)EditorGUILayout.ObjectField(
            "Terrain Target",
            terrainTarget,
            typeof(Terrain),
            true);

        EditorGUILayout.Space();

        using (new EditorGUI.DisabledScope(konfigurasi == null || terrainTarget == null))
        {
            if (GUILayout.Button("Generate Ulang", GUILayout.Height(32)))
            {
                JalankanGenerasi();
            }
        }

        if (konfigurasi == null)
        {
            EditorGUILayout.HelpBox("Pilih asset Konfigurasi Terrain terlebih dahulu.", MessageType.Info);
        }

        if (terrainTarget == null)
        {
            EditorGUILayout.HelpBox("Pilih Terrain target di scene terlebih dahulu.", MessageType.Info);
        }
    }

    private void JalankanGenerasi()
    {
        try
        {
            EditorUtility.DisplayProgressBar("Generator Terrain Prosedural", "Membangun terrain...", 0.1f);
            PembangkitTerrain pembangkit = new PembangkitTerrain(konfigurasi);
            pembangkit.Bangun(terrainTarget);
            EditorUtility.SetDirty(terrainTarget.terrainData);
            AssetDatabase.SaveAssets();
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }
}
