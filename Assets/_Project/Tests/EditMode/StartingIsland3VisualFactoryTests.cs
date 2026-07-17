using System.Collections.Generic;
using System.Linq;
using Hollowwest.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Hollowwest.Tests
{
    public sealed class StartingIsland3VisualFactoryTests
    {
        private const string AssetRoot =
            "Assets/_Project/Resources/Models/Custom/Environment/StartingIsland/Map3/Source/";
        private const string CollisionAssetPath =
            "Assets/_Project/Resources/Models/Custom/Environment/StartingIsland/Map3/Collision/StartingMap3_Collision.fbx";

        [Test]
        public void TryCreate_WhenRequiredTextureIsMissing_LeavesNoPartialHierarchy()
        {
            GameObject owner = new("Starting Map 3 Missing Texture Test");
            GameObject model = CreateModelFixture();
            Dictionary<string, Texture2D> textures = CreateTextureFixtures();
            Object.DestroyImmediate(textures[StartingIsland3VisualFactory.RoughnessPath]);
            textures.Remove(StartingIsland3VisualFactory.RoughnessPath);

            try
            {
                bool created = StartingIsland3VisualFactory.TryCreate(
                    owner.transform,
                    path => path == StartingIsland3VisualFactory.ModelPath ? model : null,
                    path => textures.TryGetValue(path, out Texture2D texture) ? texture : null,
                    LoadShader,
                    out GameObject visual);

                Assert.That(created, Is.False);
                Assert.That(visual, Is.Null);
                Assert.That(owner.transform.childCount, Is.Zero);
            }
            finally
            {
                DestroyTextureFixtures(textures);
                Object.DestroyImmediate(model);
                Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void TryCreate_WithRequiredAssets_BuildsPbrRenderOnlyVisualAtContractScale()
        {
            GameObject owner = new("Starting Map 3 Factory Test");
            GameObject model = CreateModelFixture();
            Dictionary<string, Texture2D> textures = CreateTextureFixtures();
            Material runtimeMaterial = null;

            try
            {
                Assert.That(
                    StartingIsland3VisualFactory.TryCreate(
                        owner.transform,
                        path => path == StartingIsland3VisualFactory.ModelPath ? model : null,
                        path => textures.TryGetValue(path, out Texture2D texture)
                            ? texture
                            : null,
                        LoadShader,
                        out GameObject visual),
                    Is.True);

                Assert.That(visual.transform.parent, Is.EqualTo(owner.transform));
                Assert.That(visual.transform.localRotation, Is.EqualTo(Quaternion.identity));

                Renderer renderer = visual.GetComponentsInChildren<Renderer>(true).Single();
                Assert.That(renderer.enabled, Is.True);
                Assert.That(renderer.sharedMaterials, Has.Length.EqualTo(2));
                Assert.That(
                    renderer.sharedMaterials.Select(material => material.shader.name),
                    Is.All.EqualTo("Hollowwest/Starting Island 3"));

                Material material = renderer.sharedMaterial;
                runtimeMaterial = material;
                Assert.That(
                    material.GetTexture("_BaseColorMap"),
                    Is.SameAs(textures[StartingIsland3VisualFactory.BaseColorPath]));
                Assert.That(
                    material.GetTexture("_NormalMap"),
                    Is.SameAs(textures[StartingIsland3VisualFactory.NormalPath]));
                Assert.That(
                    material.GetTexture("_MetallicMap"),
                    Is.SameAs(textures[StartingIsland3VisualFactory.MetallicPath]));
                Assert.That(
                    material.GetTexture("_RoughnessMap"),
                    Is.SameAs(textures[StartingIsland3VisualFactory.RoughnessPath]));

                Assert.That(
                    visual.GetComponentsInChildren<Collider>(true),
                    Is.Not.Empty,
                    "The fixture must exercise imported-collider disabling.");
                Assert.That(
                    visual.GetComponentsInChildren<Collider>(true)
                        .Any(collider => collider.enabled),
                    Is.False,
                    "The 810k-vertex visual must never become a MeshCollider.");

                Bounds bounds = renderer.bounds;
                Assert.That(
                    Mathf.Max(bounds.size.x, bounds.size.z),
                    Is.EqualTo(StartingIsland3VisualFactory.TargetLongestHorizontalSize)
                        .Within(0.01f));
                float groundHeight = Mathf.Lerp(
                    bounds.min.y,
                    bounds.max.y,
                    StartingIsland3VisualFactory.GroundHeightFromBottom);
                Assert.That(groundHeight, Is.Zero.Within(0.01f));
            }
            finally
            {
                Object.DestroyImmediate(owner);
                if (runtimeMaterial != null)
                {
                    Object.DestroyImmediate(runtimeMaterial);
                }

                Object.DestroyImmediate(model);
                DestroyTextureFixtures(textures);
            }
        }

        [Test]
        public void TryCreate_WithImportedMap3_UsesDetailedSourceAndAuthoredPbrMaps()
        {
            GameObject owner = new("Starting Map 3 Imported Asset Test");
            Material runtimeMaterial = null;

            try
            {
                Assert.That(
                    StartingIsland3VisualFactory.TryCreate(
                        owner.transform,
                        out GameObject visual),
                    Is.True,
                    "The FBX, four Meshy PBR maps and Map3 shader must all load from Resources.");

                Renderer renderer = visual.GetComponentsInChildren<Renderer>(true).Single();
                runtimeMaterial = renderer.sharedMaterial;
                MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
                Assert.That(meshFilter, Is.Not.Null);
                Assert.That(meshFilter.sharedMesh.vertexCount, Is.GreaterThan(750000));
                Assert.That(
                    runtimeMaterial.shader.name,
                    Is.EqualTo("Hollowwest/Starting Island 3"));
                Assert.That(runtimeMaterial.GetTexture("_BaseColorMap"), Is.Not.Null);
                Assert.That(runtimeMaterial.GetTexture("_NormalMap"), Is.Not.Null);
                Assert.That(runtimeMaterial.GetTexture("_MetallicMap"), Is.Not.Null);
                Assert.That(runtimeMaterial.GetTexture("_RoughnessMap"), Is.Not.Null);

                Bounds bounds = renderer.bounds;
                Assert.That(bounds.size.x, Is.InRange(279.5f, 280.5f));
                Assert.That(bounds.size.z, Is.InRange(262.5f, 264f));
                Assert.That(bounds.size.y, Is.InRange(144.5f, 146f));
                float groundHeight = Mathf.Lerp(
                    bounds.min.y,
                    bounds.max.y,
                    StartingIsland3VisualFactory.GroundHeightFromBottom);
                Assert.That(groundHeight, Is.Zero.Within(0.02f));
                Assert.That(
                    visual.GetComponentsInChildren<Collider>(true)
                        .Any(collider => collider.enabled),
                    Is.False);
            }
            finally
            {
                Object.DestroyImmediate(owner);
                if (runtimeMaterial != null)
                {
                    Object.DestroyImmediate(runtimeMaterial);
                }
            }
        }

        [Test]
        public void Map3Importers_KeepVisualDetailedAndDisableImportedCollision()
        {
            ModelImporter modelImporter = AssetImporter.GetAtPath(
                AssetRoot + StartingIsland3VisualFactory.AssetName + ".fbx") as ModelImporter;
            Assert.That(modelImporter, Is.Not.Null);
            Assert.That(modelImporter.addCollider, Is.False);
            Assert.That(modelImporter.isReadable, Is.False);
            Assert.That(modelImporter.importAnimation, Is.False);
            Assert.That(modelImporter.animationType, Is.EqualTo(ModelImporterAnimationType.None));
            Assert.That(
                modelImporter.materialImportMode,
                Is.EqualTo(ModelImporterMaterialImportMode.None));
            Assert.That(
                modelImporter.meshCompression,
                Is.EqualTo(ModelImporterMeshCompression.Off));
            Assert.That(modelImporter.importNormals, Is.EqualTo(ModelImporterNormals.Import));
            Assert.That(
                modelImporter.importTangents,
                Is.EqualTo(ModelImporterTangents.CalculateMikk));

            ModelImporter collisionImporter =
                AssetImporter.GetAtPath(CollisionAssetPath) as ModelImporter;
            Assert.That(collisionImporter, Is.Not.Null);
            Assert.That(collisionImporter.addCollider, Is.False,
                "The collision factory must own the one runtime MeshCollider.");
            Assert.That(collisionImporter.isReadable, Is.True,
                "Runtime MeshCollider cooking requires the simplified mesh to stay readable.");
            Assert.That(collisionImporter.importAnimation, Is.False);
            Assert.That(
                collisionImporter.materialImportMode,
                Is.EqualTo(ModelImporterMaterialImportMode.None));
            Assert.That(
                collisionImporter.meshCompression,
                Is.EqualTo(ModelImporterMeshCompression.Off));
            Assert.That(
                collisionImporter.importNormals,
                Is.EqualTo(ModelImporterNormals.None));
            Assert.That(
                collisionImporter.importTangents,
                Is.EqualTo(ModelImporterTangents.None));

            AssertTextureImporter(
                StartingIsland3VisualFactory.AssetName + ".png",
                TextureImporterType.Default,
                true,
                8192);
            AssertTextureImporter(
                StartingIsland3VisualFactory.AssetName + "_normal.png",
                TextureImporterType.NormalMap,
                false,
                4096);
            AssertTextureImporter(
                StartingIsland3VisualFactory.AssetName + "_metallic.png",
                TextureImporterType.Default,
                false,
                4096);
            AssertTextureImporter(
                StartingIsland3VisualFactory.AssetName + "_roughness.png",
                TextureImporterType.Default,
                false,
                4096);
        }

        private static Shader LoadShader(string path)
        {
            return path == StartingIsland3VisualFactory.ShaderPath
                ? Resources.Load<Shader>(path)
                : null;
        }

        private static GameObject CreateModelFixture()
        {
            GameObject root = new("Starting Map 3 Model Fixture");
            GameObject mesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mesh.name = "Mesh_0";
            mesh.transform.SetParent(root.transform, false);
            mesh.transform.localScale = new Vector3(1f, 0.5f, 0.8f);
            mesh.GetComponent<Renderer>().sharedMaterials = new Material[2];
            return root;
        }

        private static Dictionary<string, Texture2D> CreateTextureFixtures()
        {
            return new Dictionary<string, Texture2D>
            {
                [StartingIsland3VisualFactory.BaseColorPath] = CreateTexture("Base Color"),
                [StartingIsland3VisualFactory.NormalPath] = CreateTexture("Normal"),
                [StartingIsland3VisualFactory.MetallicPath] = CreateTexture("Metallic"),
                [StartingIsland3VisualFactory.RoughnessPath] = CreateTexture("Roughness")
            };
        }

        private static Texture2D CreateTexture(string name)
        {
            return new Texture2D(2, 2) { name = name };
        }

        private static void DestroyTextureFixtures(
            Dictionary<string, Texture2D> textures)
        {
            foreach (Texture2D texture in textures.Values)
            {
                if (texture != null)
                {
                    Object.DestroyImmediate(texture);
                }
            }
        }

        private static void AssertTextureImporter(
            string fileName,
            TextureImporterType expectedType,
            bool expectedSrgb,
            int expectedMaxSize)
        {
            TextureImporter importer = AssetImporter.GetAtPath(
                AssetRoot + fileName) as TextureImporter;
            Assert.That(importer, Is.Not.Null, $"Missing TextureImporter for {fileName}");
            Assert.That(importer.textureType, Is.EqualTo(expectedType));
            Assert.That(importer.sRGBTexture, Is.EqualTo(expectedSrgb));
            Assert.That(importer.maxTextureSize, Is.EqualTo(expectedMaxSize));
            Assert.That(importer.mipmapEnabled, Is.True);
            Assert.That(importer.streamingMipmaps, Is.True);
            Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Trilinear));
            Assert.That(importer.anisoLevel, Is.EqualTo(8));
        }
    }
}
