using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class HazardBootstrap
{
    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.BeforeSceneLoad
    )]
    private static void Initialize()
    {
        // Prevent duplicate subscriptions
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }


    private static void OnSceneLoaded(
        Scene scene,
        LoadSceneMode mode
    )
    {
        // Only add hazards to Road Relay race maps
        if (
            Array.IndexOf(
                RelaySettings.Scenes,
                scene.name
            ) < 0
        )
        {
            return;
        }


        // Don't create another one if the scene
        // already contains a spawner
        RandomRoadHazardSpawner existingSpawner =
            UnityEngine.Object.FindAnyObjectByType<
                RandomRoadHazardSpawner
            >();


        if (existingSpawner != null)
        {
            Debug.Log(
                "HAZARD BOOTSTRAP: Existing spawner found in "
                + scene.name
            );

            return;
        }


        // Try to use the existing MANAGER object
        GameObject manager =
            GameObject.Find("MANAGER");


        // Some maps may not have a MANAGER object
        if (manager == null)
        {
            manager =
                new GameObject(
                    "Road Hazard System"
                );
        }


        manager.AddComponent<
            RandomRoadHazardSpawner
        >();


        Debug.Log(
            "HAZARD BOOTSTRAP: Added hazard system to "
            + scene.name
        );
    }
}