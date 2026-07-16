#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Hollowwest.EditorTools
{
    /// <summary>
    /// One-click builder for the Izba_Rowanhearth static-mesh prefab.
    /// Creates the 3 runtime materials (palette Atlas / Iron / emissive Window),
    /// configures the FBX + palette import settings, and assembles a 3-LOD
    /// LODGroup prefab (PF_Izba_Rowanhearth) matching the project's asset layout.
    ///
    /// Run from the menu:  Tools > Badyth > Build Izba_Rowanhearth Prefab
    ///
    /// Gameplay exposes this prefab as one of the visual variants of the single
    /// "izba" building definition.
    /// </summary>
    public static class BuildIzbaRowanhearthPrefab
    {
        const string Root    = "Assets/_Project/Resources/Models/Custom/Buildings/Izba_Rowanhearth";
        const string GeoDir  = Root + "/Geometry";
        const string MatDir  = Root + "/Materials";
        const string TexPath = Root + "/Textures/TEX_Izba_Rowanhearth_Palette.png";
        const string Prefab  = Root + "/PF_Izba_Rowanhearth.prefab";

        // Screen-relative LOD transition heights (match the project's LOD strategy).
        static readonly float[] LodHeights = { 0.58f, 0.27f, 0.085f };

        [MenuItem("Tools/Badyth/Build Izba_Rowanhearth Prefab")]
        public static void Build()
        {
            if (!AssetDatabase.IsValidFolder(Root))
            {
                EditorUtility.DisplayDialog("Izba builder",
                    "Folder not found:\n" + Root +
                    "\n\nImport the exported FBX/PNG first (focus Unity to trigger the import).", "OK");
                return;
            }

            ConfigurePalette();
            var mats = BuildMaterials();
            BuildPrefab(mats);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        static void ConfigurePalette()
        {
            var imp = AssetImporter.GetAtPath(TexPath) as TextureImporter;
            if (imp == null) return;
            imp.textureType   = TextureImporterType.Default;
            imp.sRGBTexture   = true;
            imp.mipmapEnabled = false;                 // tiny LUT strip — no mips, no swatch bleed
            imp.filterMode    = FilterMode.Point;      // crisp swatches at any distance
            imp.wrapMode      = TextureWrapMode.Clamp;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.SaveAndReimport();
        }

        static Material[] BuildMaterials()
        {
            if (!AssetDatabase.IsValidFolder(MatDir))
                AssetDatabase.CreateFolder(Root, "Materials");

            var palette = AssetDatabase.LoadAssetAtPath<Texture2D>(TexPath);

            var atlas = Ensure("MAT_Izba_Rowanhearth_Atlas");
            atlas.SetColor("_Color", Color.white);
            atlas.SetTexture("_MainTex", palette);
            atlas.SetFloat("_Glossiness", 0.14f);
            atlas.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(atlas);

            var iron = Ensure("MAT_Izba_Rowanhearth_Iron");
            iron.SetColor("_Color", new Color(0.025f, 0.03f, 0.032f, 1f));
            iron.SetFloat("_Metallic", 0.72f);
            iron.SetFloat("_Glossiness", 0.42f);
            EditorUtility.SetDirty(iron);

            var window = Ensure("MAT_Izba_Rowanhearth_Window");
            window.SetColor("_Color", new Color(0.08f, 0.035f, 0.015f, 1f));
            window.SetFloat("_Glossiness", 0.30f);
            window.SetFloat("_Metallic", 0f);
            window.EnableKeyword("_EMISSION");
            window.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            window.SetColor("_EmissionColor", new Color(2.6f, 0.676f, 0.117f, 1f)); // warm amber HDR
            EditorUtility.SetDirty(window);

            return new[] { atlas, iron, window }; // slot order matches FBX submeshes (Atlas/Iron/Window)
        }

        static Material Ensure(string name)
        {
            string path = MatDir + "/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(Shader.Find("Standard")) { name = name };
                AssetDatabase.CreateAsset(m, path);
            }
            return m;
        }

        static void BuildPrefab(Material[] mats)
        {
            var root = new GameObject("PF_Izba_Rowanhearth");
            try
            {
                var group = root.AddComponent<LODGroup>();
                group.fadeMode = LODFadeMode.CrossFade;
                group.animateCrossFading = true;

                var lods = new List<LOD>(3);
                Bounds lod0Bounds = default;
                bool hasLod0Bounds = false;

                for (int i = 0; i < 3; i++)
                {
                    string fbx = GeoDir + "/SM_Izba_Rowanhearth_LOD" + i + ".fbx";
                    ConfigureModel(fbx);
                    var source = AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
                    if (source == null)
                    {
                        Debug.LogError("[Izba] Model not found in " + fbx + " — did Unity import it yet?");
                        continue;
                    }

                    var go = PrefabUtility.InstantiatePrefab(source) as GameObject;
                    if (go == null)
                    {
                        Debug.LogError("[Izba] Could not instantiate " + fbx);
                        continue;
                    }

                    go.name = "SM_Izba_Rowanhearth_LOD" + i;
                    go.transform.SetParent(root.transform, false);
                    go.transform.localPosition = source.transform.localPosition;
                    go.transform.localRotation = source.transform.localRotation;
                    go.transform.localScale = source.transform.localScale;

                    Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
                    foreach (Renderer renderer in renderers)
                    {
                        renderer.sharedMaterials = mats;
                        renderer.shadowCastingMode = ShadowCastingMode.On;
                        renderer.receiveShadows = true;
                    }

                    if (i == 0)
                    {
                        foreach (Renderer renderer in renderers)
                        {
                            if (!hasLod0Bounds)
                            {
                                lod0Bounds = renderer.bounds;
                                hasLod0Bounds = true;
                            }
                            else
                            {
                                lod0Bounds.Encapsulate(renderer.bounds);
                            }
                        }
                    }

                    lods.Add(new LOD(LodHeights[i], renderers));
                }

                group.SetLODs(lods.ToArray());
                group.RecalculateBounds();

                var bc = root.AddComponent<BoxCollider>();
                bc.center = hasLod0Bounds ? root.transform.InverseTransformPoint(lod0Bounds.center) : Vector3.up;
                bc.size   = hasLod0Bounds ? lod0Bounds.size : Vector3.one * 2f;

                var asset = PrefabUtility.SaveAsPrefabAsset(root, Prefab);
                Debug.Log("[Izba] Built prefab: " + Prefab + "  (LODs: " + lods.Count + ")");
                Selection.activeObject = asset;
                EditorGUIUtility.PingObject(asset);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        static void ConfigureModel(string fbxPath)
        {
            var mi = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (mi == null) return;
            mi.globalScale   = 1f;
            mi.useFileScale  = true;
            mi.importCameras = false;
            mi.importLights  = false;
            mi.addCollider   = false;
            mi.materialImportMode = ModelImporterMaterialImportMode.None; // we assign our own 3 mats
            mi.animationType = ModelImporterAnimationType.None;
            mi.SaveAndReimport();
        }

    }
}
#endif
