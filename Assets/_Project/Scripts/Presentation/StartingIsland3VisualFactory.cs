using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Hollowwest.Presentation
{
    /// <summary>
    /// Creates the render-only Starting Map 3.0 visual. Gameplay collision stays
    /// on the dedicated low-poly ground owned by the prototype bootstrap.
    /// </summary>
    public static class StartingIsland3VisualFactory
    {
        public const string AssetName =
            "Meshy_AI_Floating_Island_Grass_0717131253_texture";
        public const string ResourceRoot =
            "Models/Custom/Environment/StartingIsland/Map3/Source/";
        public const string ModelPath = ResourceRoot + AssetName;
        public const string BaseColorPath = ResourceRoot + AssetName;
        public const string NormalPath = ResourceRoot + AssetName + "_normal";
        public const string MetallicPath = ResourceRoot + AssetName + "_metallic";
        public const string RoughnessPath = ResourceRoot + AssetName + "_roughness";
        public const string ShaderPath = "Shaders/StartingIsland3";

        // The source was measured in Blender before integration. Matching the
        // previous visual size keeps the 3.0 island inside the current 310x280
        // gameplay/navigation footprint.
        public const float TargetLongestHorizontalSize = 280f;
        public const float GroundHeightFromBottom = 0.926231f;

        public static bool TryCreate(Transform parent, out GameObject root)
        {
            return TryCreate(
                parent,
                path => Resources.Load<GameObject>(path),
                path => Resources.Load<Texture2D>(path),
                path => Resources.Load<Shader>(path),
                out root);
        }

        public static bool TryCreate(
            Transform parent,
            Func<string, GameObject> modelLoader,
            Func<string, Texture2D> textureLoader,
            Func<string, Shader> shaderLoader,
            out GameObject root)
        {
            root = null;
            if (modelLoader == null || textureLoader == null || shaderLoader == null)
            {
                return false;
            }

            GameObject sourcePrefab = modelLoader(ModelPath);
            Texture2D baseColor = textureLoader(BaseColorPath);
            Texture2D normal = textureLoader(NormalPath);
            Texture2D metallic = textureLoader(MetallicPath);
            Texture2D roughness = textureLoader(RoughnessPath);
            Shader shader = shaderLoader(ShaderPath);
            if (sourcePrefab == null || baseColor == null || normal == null ||
                metallic == null || roughness == null || shader == null)
            {
                return false;
            }

            GameObject visualRoot = new("StartingIsland3Visual");
            if (parent != null)
            {
                visualRoot.transform.SetParent(parent, false);
            }

            GameObject source = UnityEngine.Object.Instantiate(
                sourcePrefab,
                visualRoot.transform,
                false);
            source.name = "StartingMap3_Source";
            DisableColliders(source);

            Renderer[] renderers = source.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0 ||
                !TryGetLocalMeshBounds(visualRoot.transform, source, out Bounds sourceBounds))
            {
                DestroyObject(visualRoot);
                return false;
            }

            Material material = CreateMaterial(
                shader,
                baseColor,
                normal,
                metallic,
                roughness);
            foreach (Renderer renderer in renderers)
            {
                AssignMaterial(renderer, material);
                renderer.enabled = true;
                renderer.shadowCastingMode =
                    UnityEngine.Rendering.ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }

            float longestHorizontalSize = Mathf.Max(
                sourceBounds.size.x,
                sourceBounds.size.z);
            if (longestHorizontalSize <= 0.0001f)
            {
                DestroyObject(visualRoot);
                DestroyObject(material);
                return false;
            }

            float runtimeScale = TargetLongestHorizontalSize / longestHorizontalSize;
            float sourceGroundHeight = Mathf.Lerp(
                sourceBounds.min.y,
                sourceBounds.max.y,
                GroundHeightFromBottom);
            Vector3 groundCenter = new(
                sourceBounds.center.x,
                sourceGroundHeight,
                sourceBounds.center.z);

            visualRoot.transform.localScale = Vector3.one * runtimeScale;
            visualRoot.transform.localPosition = -groundCenter * runtimeScale;

            root = visualRoot;
            return true;
        }

        private static Material CreateMaterial(
            Shader shader,
            Texture2D baseColor,
            Texture2D normal,
            Texture2D metallic,
            Texture2D roughness)
        {
            Material material = new(shader)
            {
                name = "MAT_StartingIsland3_Runtime",
                enableInstancing = true,
                hideFlags = HideFlags.HideAndDontSave
            };
            material.SetTexture("_BaseColorMap", baseColor);
            material.SetTexture("_NormalMap", normal);
            material.SetTexture("_MetallicMap", metallic);
            material.SetTexture("_RoughnessMap", roughness);
            material.SetColor("_Tint", Color.white);
            material.SetFloat("_NormalStrength", 1f);
            return material;
        }

        private static bool TryGetLocalMeshBounds(
            Transform visualRoot,
            GameObject source,
            out Bounds bounds)
        {
            bounds = default;
            bool initialized = false;
            foreach (MeshFilter meshFilter in source.GetComponentsInChildren<MeshFilter>(true))
            {
                Mesh mesh = meshFilter.sharedMesh;
                if (mesh == null)
                {
                    continue;
                }

                Matrix4x4 toRoot = visualRoot.worldToLocalMatrix *
                    meshFilter.transform.localToWorldMatrix;
                foreach (Vector3 corner in GetCorners(mesh.bounds))
                {
                    Vector3 point = toRoot.MultiplyPoint3x4(corner);
                    if (!initialized)
                    {
                        bounds = new Bounds(point, Vector3.zero);
                        initialized = true;
                    }
                    else
                    {
                        bounds.Encapsulate(point);
                    }
                }
            }

            return initialized;
        }

        private static IEnumerable<Vector3> GetCorners(Bounds bounds)
        {
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;
            yield return new Vector3(min.x, min.y, min.z);
            yield return new Vector3(min.x, min.y, max.z);
            yield return new Vector3(min.x, max.y, min.z);
            yield return new Vector3(min.x, max.y, max.z);
            yield return new Vector3(max.x, min.y, min.z);
            yield return new Vector3(max.x, min.y, max.z);
            yield return new Vector3(max.x, max.y, min.z);
            yield return new Vector3(max.x, max.y, max.z);
        }

        private static void DisableColliders(GameObject hierarchyRoot)
        {
            foreach (Collider collider in
                     hierarchyRoot.GetComponentsInChildren<Collider>(true))
            {
                collider.enabled = false;
            }
        }

        private static void AssignMaterial(Renderer renderer, Material material)
        {
            int materialCount = Mathf.Max(1, renderer.sharedMaterials.Length);
            renderer.sharedMaterials = Enumerable
                .Repeat(material, materialCount)
                .ToArray();
        }

        private static void DestroyObject(UnityEngine.Object target)
        {
            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(target);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(target);
            }
        }
    }
}
