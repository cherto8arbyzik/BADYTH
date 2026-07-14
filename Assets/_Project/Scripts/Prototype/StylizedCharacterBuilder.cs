using UnityEngine;

namespace Hollowwest.Prototype
{

public static class StylizedCharacterBuilder
{
    public static void BuildHuman(
        Transform root,
        Material clothMaterial,
        Material skinMaterial,
        Material hairMaterial,
        Material accentMaterial,
        bool isHero,
        int variant)
    {
        float bodyScale = isHero ? 1.08f : 0.92f;

        CreatePart(
            root,
            "Torso",
            PrimitiveType.Capsule,
            new Vector3(0f, 1.03f * bodyScale, 0f),
            new Vector3(0.40f, 0.48f, 0.32f) * bodyScale,
            Quaternion.identity,
            clothMaterial);

        CreatePart(
            root,
            "Head",
            PrimitiveType.Sphere,
            new Vector3(0f, 1.76f * bodyScale, 0f),
            Vector3.one * (0.38f * bodyScale),
            Quaternion.identity,
            skinMaterial);

        CreatePart(
            root,
            "Hair",
            PrimitiveType.Sphere,
            new Vector3(0f, 1.91f * bodyScale, -0.025f),
            new Vector3(0.40f, 0.20f, 0.40f) * bodyScale,
            Quaternion.identity,
            hairMaterial);

        CreateLimb(root, "Left Arm", new Vector3(-0.37f, 1.10f, 0f) * bodyScale, 12f, clothMaterial, bodyScale);
        CreateLimb(root, "Right Arm", new Vector3(0.37f, 1.10f, 0f) * bodyScale, -12f, clothMaterial, bodyScale);
        CreateLeg(root, "Left Leg", new Vector3(-0.16f, 0.36f, 0f) * bodyScale, clothMaterial, bodyScale);
        CreateLeg(root, "Right Leg", new Vector3(0.16f, 0.36f, 0f) * bodyScale, clothMaterial, bodyScale);

        if (isHero)
        {
            CreatePart(
                root,
                "Shoulder Mantle",
                PrimitiveType.Cylinder,
                new Vector3(0f, 1.37f * bodyScale, 0f),
                new Vector3(0.50f, 0.06f, 0.40f) * bodyScale,
                Quaternion.identity,
                accentMaterial);

            CreatePart(
                root,
                "Back Shield",
                PrimitiveType.Cylinder,
                new Vector3(0f, 1.02f * bodyScale, -0.29f * bodyScale),
                new Vector3(0.34f, 0.07f, 0.34f) * bodyScale,
                Quaternion.Euler(90f, 0f, 0f),
                accentMaterial);
        }
        else if (variant % 3 == 0)
        {
            CreatePart(
                root,
                "Cap",
                PrimitiveType.Cylinder,
                new Vector3(0f, 1.95f * bodyScale, 0f),
                new Vector3(0.29f, 0.05f, 0.29f) * bodyScale,
                Quaternion.identity,
                accentMaterial);
        }
    }

    public static void BuildEnemy(Transform root, Material bodyMaterial, Material eyeMaterial)
    {
        CreatePart(
            root,
            "Fiend Body",
            PrimitiveType.Capsule,
            new Vector3(0f, 0.78f, 0f),
            new Vector3(0.58f, 0.68f, 0.52f),
            Quaternion.identity,
            bodyMaterial);

        CreatePart(
            root,
            "Fiend Head",
            PrimitiveType.Sphere,
            new Vector3(0f, 1.55f, 0.08f),
            new Vector3(0.55f, 0.42f, 0.48f),
            Quaternion.identity,
            bodyMaterial);

        CreatePart(
            root,
            "Left Eye",
            PrimitiveType.Sphere,
            new Vector3(-0.14f, 1.60f, 0.46f),
            Vector3.one * 0.09f,
            Quaternion.identity,
            eyeMaterial);

        CreatePart(
            root,
            "Right Eye",
            PrimitiveType.Sphere,
            new Vector3(0.14f, 1.60f, 0.46f),
            Vector3.one * 0.09f,
            Quaternion.identity,
            eyeMaterial);

        CreatePart(
            root,
            "Left Horn",
            PrimitiveType.Cube,
            new Vector3(-0.34f, 1.88f, 0f),
            new Vector3(0.13f, 0.42f, 0.13f),
            Quaternion.Euler(0f, 0f, -28f),
            bodyMaterial);

        CreatePart(
            root,
            "Right Horn",
            PrimitiveType.Cube,
            new Vector3(0.34f, 1.88f, 0f),
            new Vector3(0.13f, 0.42f, 0.13f),
            Quaternion.Euler(0f, 0f, 28f),
            bodyMaterial);
    }

    private static void CreateLimb(
        Transform root,
        string name,
        Vector3 position,
        float zRotation,
        Material material,
        float scale)
    {
        CreatePart(
            root,
            name,
            PrimitiveType.Capsule,
            position,
            new Vector3(0.16f, 0.35f, 0.16f) * scale,
            Quaternion.Euler(0f, 0f, zRotation),
            material);
    }

    private static void CreateLeg(
        Transform root,
        string name,
        Vector3 position,
        Material material,
        float scale)
    {
        CreatePart(
            root,
            name,
            PrimitiveType.Capsule,
            position,
            new Vector3(0.17f, 0.34f, 0.18f) * scale,
            Quaternion.identity,
            material);
    }

    private static GameObject CreatePart(
        Transform parent,
        string name,
        PrimitiveType primitive,
        Vector3 localPosition,
        Vector3 localScale,
        Quaternion localRotation,
        Material material)
    {
        GameObject part = GameObject.CreatePrimitive(primitive);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localRotation = localRotation;
        part.transform.localScale = localScale;

        Collider collider = part.GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = false;
        }

        Renderer renderer = part.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = material;
        }

        return part;
    }
}
}
