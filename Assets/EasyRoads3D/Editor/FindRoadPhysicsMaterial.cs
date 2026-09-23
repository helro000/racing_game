using UnityEditor;
using UnityEngine;
using System.IO;

public class FindRoadPhysicsMaterial
{
    [MenuItem("Tools/Road Relay/Find roadPhysicsMaterial")]
    public static void Find()
    {
        Debug.Log("===== Searching for roadPhysicsMaterial =====");

        int found = 0;

        string[] allAssets = AssetDatabase.GetAllAssetPaths();

        foreach (string path in allAssets)
        {
            if (
                !path.EndsWith(".prefab") &&
                !path.EndsWith(".unity") &&
                !path.EndsWith(".asset")
            )
            {
                continue;
            }

            try
            {
                string fullPath =
                    Path.GetFullPath(path);

                if (!File.Exists(fullPath))
                    continue;

                string content =
                    File.ReadAllText(fullPath);

                if (
                    content.Contains(
                        "roadPhysicsMaterial"
                    )
                )
                {
                    Debug.Log(
                        "FOUND roadPhysicsMaterial in: "
                        + path
                    );

                    found++;
                }
            }
            catch
            {
                // Ignore assets that cannot be read as text
            }
        }


        if (found == 0)
        {
            Debug.LogWarning(
                "No serialized roadPhysicsMaterial field was found in text assets."
            );
        }
        else
        {
            Debug.Log(
                "Finished. Found "
                + found
                + " asset(s)."
            );
        }
    }
}