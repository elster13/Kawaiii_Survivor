using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System.Collections.Generic;

public static class FindCandySpriteUsage
{
    [MenuItem("Tools/Diagnostics/Find Candy Sprite Usage")]
    public static void FindUsage()
    {
        Debug.Log("Starting Candy sprite usage scan...");

        // Find all Image components (includes scene objects and prefab instances)
        Image[] images = Resources.FindObjectsOfTypeAll<Image>();

        int found = 0;
        foreach (var img in images)
        {
            if (img == null) continue;
            if (img.sprite == null) continue;
            if (img.sprite.name.ToLower().Contains("candy"))
            {
                Debug.Log($"Image component using Candy sprite found on GameObject: '{img.gameObject.name}' (in {GetObjectLocation(img.gameObject)})", img.gameObject);
                found++;
            }
        }

        // Additionally search project assets for Sprite assets with 'Candy' in name
        string[] guids = AssetDatabase.FindAssets("t:Sprite");
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) continue;
            if (sprite.name.ToLower().Contains("candy"))
            {
                // find references by searching for serialized references in assets (prefabs, scenes)
                string[] deps = AssetDatabase.FindAssets("t:Prefab t:Scene");
                foreach (var d in deps)
                {
                    string p = AssetDatabase.GUIDToAssetPath(d);
                    string text = System.IO.File.ReadAllText(p);
                    if (text.Contains(path) || text.Contains(sprite.name))
                    {
                        Debug.Log($"Sprite asset '{sprite.name}' referenced in asset: {p}");
                        found++;
                    }
                }
            }
        }

        if (found == 0)
            Debug.Log("No Image components or asset references with 'Candy' found.");
        else
            Debug.Log($"Candy usage scan complete. Matches found: {found}");
    }

    private static string GetObjectLocation(GameObject go)
    {
        if (EditorUtility.IsPersistent(go))
            return "Project Asset (Prefab)";
        if (go.scene.IsValid())
            return $"Scene: {go.scene.name}";
        return "Unknown";
    }
}
