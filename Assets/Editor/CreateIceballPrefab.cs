using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class CreateIceballPrefab
{
    [MenuItem("Tools/Create Iceball Prefab")]
    public static void Create()
    {
        string spritePath = "Assets/Art/Sprites/IceCandyBullet.svg";
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);

        GameObject root = new GameObject("IceCandyFireball");
        SpriteRenderer sr = root.AddComponent<SpriteRenderer>();
        if (sprite != null) sr.sprite = sprite; else Debug.LogWarning("Ice sprite not found: " + spritePath);

        CircleCollider2D col = root.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.35f;

        TrailRenderer trail = root.AddComponent<TrailRenderer>();
        trail.time = 0.45f;
        trail.startWidth = 0.28f;
        trail.endWidth = 0.04f;
        trail.material = new Material(Shader.Find("Sprites/Default"));
        trail.startColor = new Color(0.7f, 0.95f, 1f, 0.9f);
        trail.endColor = new Color(0.4f, 0.8f, 1f, 0f);

        GameObject flight = new GameObject("FlightVfx");
        flight.transform.SetParent(root.transform, false);
        var flightPS = flight.AddComponent<ParticleSystem>();
        var mainF = flightPS.main;
        mainF.loop = true;
        mainF.startLifetime = 0.45f;
        mainF.startSpeed = 0.06f;
        mainF.startSize = 0.24f;
        mainF.startColor = new ParticleSystem.MinMaxGradient(new Color(0.7f, 0.95f, 1f));
        var emissionF = flightPS.emission; emissionF.rateOverTime = 32f;
        var shapeF = flightPS.shape; shapeF.shapeType = ParticleSystemShapeType.Cone; shapeF.angle = 12f; shapeF.radius = 0.04f;

        GameObject impact = new GameObject("ImpactVfx");
        impact.transform.SetParent(root.transform, false);
        var impactPS = impact.AddComponent<ParticleSystem>();
        var mainI = impactPS.main;
        mainI.loop = false; mainI.startLifetime = 0.6f; mainI.startSpeed = 1.0f; mainI.startSize = 0.6f; mainI.startColor = new ParticleSystem.MinMaxGradient(new Color(0.6f,0.9f,1f));
        var emissionI = impactPS.emission; emissionI.rateOverTime = 0f; emissionI.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 22) });
        var shapeI = impactPS.shape; shapeI.shapeType = ParticleSystemShapeType.Sphere; shapeI.radius = 0.28f;

        // Try to find IceCandyBullet
        Type bulletType = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => { try { return a.GetTypes(); } catch { return new Type[0]; } })
            .FirstOrDefault(t => t.Name == "IceCandyBullet");

        if (bulletType != null && typeof(MonoBehaviour).IsAssignableFrom(bulletType))
            root.AddComponent(bulletType);
        else
            Debug.LogWarning("Could not find type 'IceCandyBullet'. Ensure script compiles.");

        string prefabPath = "Assets/Prefabs/IceCandyFireball.prefab";
        string folder = "Assets/Prefabs";
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets","Prefabs");

        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);

        GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefabAsset != null)
        {
            var compOnPrefab = prefabAsset.GetComponent("IceCandyBullet");
            if (compOnPrefab != null)
            {
                SerializedObject so = new SerializedObject((UnityEngine.Object)compOnPrefab);
                var p_damage = so.FindProperty("damageCollider");
                if (p_damage != null) p_damage.objectReferenceValue = prefabAsset.GetComponent<CircleCollider2D>();
                var p_flight = so.FindProperty("flightVfx"); if (p_flight != null) p_flight.objectReferenceValue = prefabAsset.transform.Find("FlightVfx")?.GetComponent<ParticleSystem>();
                var p_impact = so.FindProperty("impactVfx"); if (p_impact != null) p_impact.objectReferenceValue = prefabAsset.transform.Find("ImpactVfx")?.GetComponent<ParticleSystem>();
                var p_trail = so.FindProperty("trail"); if (p_trail != null) p_trail.objectReferenceValue = prefabAsset.GetComponent<TrailRenderer>();
                var p_sprite = so.FindProperty("spriteRenderer"); if (p_sprite != null) p_sprite.objectReferenceValue = prefabAsset.GetComponent<SpriteRenderer>();
                so.ApplyModifiedProperties();
            }
            else Debug.LogWarning("IceCandyBullet script missing on prefab asset; cannot assign serialized fields.", prefabAsset);
        }

        UnityEngine.Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        Debug.Log("IceCandyFireball prefab created at: " + prefabPath);
    }
}
