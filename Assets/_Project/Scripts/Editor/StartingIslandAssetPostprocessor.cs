using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Hollowwest.EditorTools
{
    /// <summary>
    /// Applies stable import rules to the Map 3.0 render source and its separate,
    /// simplified runtime collision mesh.
    /// </summary>
    internal sealed class StartingIslandAssetPostprocessor : AssetPostprocessor
    {
        private const string Map3SourceRoot =
            "Assets/_Project/Resources/Models/Custom/Environment/StartingIsland/Map3/Source/";
        private const string Map3CollisionRoot =
            "Assets/_Project/Resources/Models/Custom/Environment/StartingIsland/Map3/Collision/";
        private const string Map3ModelFileName =
            "Meshy_AI_Floating_Island_Grass_0717131253_texture.fbx";
        private const string Map3CollisionFileName = "StartingMap3_Collision.fbx";

        public override uint GetVersion()
        {
            return 1;
        }

        private void OnPreprocessModel()
        {
            bool isMap3Model =
                assetPath.StartsWith(Map3SourceRoot, StringComparison.OrdinalIgnoreCase) &&
                Path.GetFileName(assetPath).Equals(
                    Map3ModelFileName,
                    StringComparison.OrdinalIgnoreCase);
            bool isMap3Collision =
                assetPath.StartsWith(Map3CollisionRoot, StringComparison.OrdinalIgnoreCase) &&
                Path.GetFileName(assetPath).Equals(
                    Map3CollisionFileName,
                    StringComparison.OrdinalIgnoreCase);
            if (!isMap3Model && !isMap3Collision)
            {
                return;
            }

            ModelImporter importer = (ModelImporter)assetImporter;
            importer.globalScale = 1f;
            importer.useFileScale = true;
            importer.bakeAxisConversion = false;

            importer.animationType = ModelImporterAnimationType.None;
            importer.importAnimation = false;
            importer.importAnimatedCustomProperties = false;
            importer.importBlendShapes = false;
            importer.importCameras = false;
            importer.importConstraints = false;
            importer.importLights = false;
            importer.importVisibility = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.addCollider = false;

            importer.isReadable = isMap3Collision;
            importer.keepQuads = false;
            importer.weldVertices = true;
            importer.optimizeMeshPolygons = true;
            importer.optimizeMeshVertices = true;
            importer.meshCompression = ModelImporterMeshCompression.Off;
            importer.importNormals = isMap3Collision
                ? ModelImporterNormals.None
                : ModelImporterNormals.Import;
            importer.importTangents = isMap3Collision
                ? ModelImporterTangents.None
                : ModelImporterTangents.CalculateMikk;
        }

        private void OnPreprocessTexture()
        {
            bool isMap3Texture =
                assetPath.StartsWith(Map3SourceRoot, StringComparison.OrdinalIgnoreCase) &&
                !Path.GetExtension(assetPath).Equals(".fbx", StringComparison.OrdinalIgnoreCase);
            if (!isMap3Texture)
            {
                return;
            }

            TextureImporter importer = (TextureImporter)assetImporter;
            importer.textureShape = TextureImporterShape.Texture2D;
            importer.mipmapEnabled = true;
            importer.streamingMipmaps = true;
            importer.streamingMipmapsPriority = 0;
            importer.filterMode = FilterMode.Trilinear;
            importer.anisoLevel = 8;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.compressionQuality = 100;
            importer.crunchedCompression = false;
            importer.alphaIsTransparency = false;
            importer.alphaSource = TextureImporterAlphaSource.None;

            string textureName = Path.GetFileNameWithoutExtension(assetPath);
            if (textureName.EndsWith("_normal", StringComparison.OrdinalIgnoreCase) ||
                textureName.EndsWith("_NormalGL", StringComparison.OrdinalIgnoreCase))
            {
                importer.textureType = TextureImporterType.NormalMap;
                importer.sRGBTexture = false;
                importer.maxTextureSize = 4096;
                return;
            }

            importer.textureType = TextureImporterType.Default;
            bool isDataMap =
                textureName.EndsWith("_metallic", StringComparison.OrdinalIgnoreCase) ||
                textureName.EndsWith("_roughness", StringComparison.OrdinalIgnoreCase) ||
                textureName.EndsWith("_AO", StringComparison.OrdinalIgnoreCase) ||
                textureName.EndsWith("_AmbientOcclusion", StringComparison.OrdinalIgnoreCase) ||
                textureName.EndsWith("_Displacement", StringComparison.OrdinalIgnoreCase);
            importer.sRGBTexture = !isDataMap;
            importer.maxTextureSize = isDataMap ? 4096 : 8192;
        }
    }
}
