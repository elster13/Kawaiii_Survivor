using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Editor utility to create a fireball prefab with required components and wire up CottonCandyBullet fields.
public static class CreateFireballPrefab
{
    [MenuItem("Tools/Create Fireball Prefab")]
    public static void Create()
    {
        string spritePath = "Assets/Art/Sprites/CottonCandyBullet.svg";
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);

        // If SVG not imported as a sprite (or package not available), create a simple fallback PNG sprite
        if (sprite == null)
        {
            try
            {
                string pngPath = "Assets/Art/Sprites/CottonCandyFireball.png";
                int size = 256;
                Texture2D tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
                Color32[] cols = new Color32[size * size];
                Vector2 center = new Vector2(size / 2f, size / 2f);
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float d = Vector2.Distance(new Vector2(x, y), center) / (size * 0.5f);
                        d = Mathf.Clamp01(d);
                        Color c = Color.Lerp(new Color(1f, 0.6f, 0.2f), new Color(0.5f, 0.05f, 0f), d);
                        float alpha = 1f - d;
                        cols[y * size + x] = new Color(c.r, c.g, c.b, alpha);
                    }
                }
                tex.SetPixels32(cols);
                tex.Apply();
                byte[] png = tex.EncodeToPNG();
                System.IO.File.WriteAllBytes(pngPath, png);
                AssetDatabase.ImportAsset(pngPath);
                sprite = AssetDatabase.LoadAssetAtPath<Sprite>(pngPath);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("Failed to create fallback PNG sprite: " + ex.Message);
            }
        }

        GameObject root = new GameObject("CottonCandyFireball");
        // Sprite
        SpriteRenderer sr = root.AddComponent<SpriteRenderer>();
        if (sprite != null)
            sr.sprite = sprite;
        else
            Debug.LogWarning("Fireball sprite not found at: " + spritePath);

        // Collider used for damage trigger
        CircleCollider2D col = root.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.35f;

        // Trail
        TrailRenderer trail = root.AddComponent<TrailRenderer>();
        trail.time = 0.35f;
        trail.startWidth = 0.3f;
        trail.endWidth = 0.05f;
        trail.material = new Material(Shader.Find("Sprites/Default"));
        trail.startColor = new Color(1f, 0.6f, 0.2f, 0.9f);
        trail.endColor = new Color(1f, 0.2f, 0.05f, 0f);

        // Flight VFX (simple particle system)
        GameObject flight = new GameObject("FlightVfx");
        flight.transform.SetParent(root.transform, false);
        var flightPS = flight.AddComponent<ParticleSystem>();
        var mainF = flightPS.main;
        mainF.loop = true;
        mainF.startLifetime = 0.4f;
        mainF.startSpeed = 0.1f;
        mainF.startSize = 0.25f;
        mainF.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.7f, 0.3f));
        var emissionF = flightPS.emission;
        emissionF.rateOverTime = 40f;
        var shapeF = flightPS.shape;
        shapeF.shapeType = ParticleSystemShapeType.Cone;
        shapeF.angle = 15f;
        shapeF.radius = 0.05f;

        // Impact VFX
        GameObject impact = new GameObject("ImpactVfx");
        impact.transform.SetParent(root.transform, false);
        var impactPS = impact.AddComponent<ParticleSystem>();
        var mainI = impactPS.main;
        mainI.loop = false;
        mainI.startLifetime = 0.6f;
        mainI.startSpeed = 1.2f;
        mainI.startSize = 0.6f;
        mainI.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.5f, 0.2f));
        var emissionI = impactPS.emission;
        emissionI.rateOverTime = 0f;
        emissionI.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 30) });
        var shapeI = impactPS.shape;
        shapeI.shapeType = ParticleSystemShapeType.Sphere;
        shapeI.radius = 0.3f;

        // Try to find the CottonCandyBullet type via reflection in loaded assemblies
        Type bulletType = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a =>
            {
                try { return a.GetTypes(); } catch { return new Type[0]; }
            })
            .FirstOrDefault(t => t.Name == "CottonCandyBullet");

        if (bulletType != null && typeof(MonoBehaviour).IsAssignableFrom(bulletType))
        {
            root.AddComponent(bulletType);
        }
        else
        {
            Debug.LogWarning("Could not find type 'CottonCandyBullet' in assemblies. Please ensure the script compiles and try again.");
        }

        var prefabPath = "Assets/Prefabs/CottonCandyFireball.prefab";

        // Create Prefab folder if not exists
        string folder = "Assets/Prefabs";
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets", "Prefabs");

        // Save the prefab asset
        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);

        // After saving, load the prefab asset and set serialized fields on the asset instance (more robust)
        GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefabAsset != null)
        {
            var compOnPrefab = prefabAsset.GetComponent("CottonCandyBullet");
            if (compOnPrefab != null)
            {
                SerializedObject so = new SerializedObject((UnityEngine.Object)compOnPrefab);
                var p_damage = so.FindProperty("damageCollider");
                if (p_damage != null)
                    p_damage.objectReferenceValue = prefabAsset.GetComponent<CircleCollider2D>();

                var p_flight = so.FindProperty("flightVfx");
                if (p_flight != null)
                    p_flight.objectReferenceValue = prefabAsset.transform.Find("FlightVfx")?.GetComponent<ParticleSystem>();

                var p_impact = so.FindProperty("impactVfx");
                if (p_impact != null)
                    p_impact.objectReferenceValue = prefabAsset.transform.Find("ImpactVfx")?.GetComponent<ParticleSystem>();

                var p_trail = so.FindProperty("trail");
                if (p_trail != null)
                    p_trail.objectReferenceValue = prefabAsset.GetComponent<TrailRenderer>();

                var p_sprite = so.FindProperty("spriteRenderer");
                if (p_sprite != null)
                    p_sprite.objectReferenceValue = prefabAsset.GetComponent<SpriteRenderer>();

                so.ApplyModifiedProperties();
            }
            else
            {
                Debug.LogWarning("CottonCandyBullet component not found on prefab asset; serialized fields were not assigned. Add the script (compile errors may prevent reflection).", prefabAsset);
            }
        }

        UnityEngine.Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();

        Debug.Log("CottonCandyFireball prefab created at: " + prefabPath + ". Please open it and fine-tune particle settings / layer masks.");
    }
}