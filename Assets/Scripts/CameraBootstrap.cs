using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI; 

public class GameBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void InitializeGameSystem()
    {
        // --- CAMERA ---
        GameObject boom = new GameObject("CameraBoom_System");
        CameraMover mover = boom.AddComponent<CameraMover>();

        GameObject camObj = new GameObject("Main Camera");
        camObj.transform.SetParent(boom.transform);
        camObj.tag = "MainCamera";

        Camera cam = camObj.AddComponent<Camera>();
        cam.orthographic = false;
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 1000f;
        camObj.transform.localPosition = Vector3.zero;
        
        // Ensure we don't have multiple audio listeners
        if (Object.FindAnyObjectByType<AudioListener>() == null)
        {
            camObj.AddComponent<AudioListener>();
        }

        Object.DontDestroyOnLoad(boom);

        // --- EVENT SYSTEM ---
        // Check if an EventSystem already exists in the scene
        if (Object.FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            
            // 2. FIX: Add InputSystemUIInputModule instead of StandaloneInputModule
            eventSystem.AddComponent<InputSystemUIInputModule>();
            
            Object.DontDestroyOnLoad(eventSystem);
        }
GameObject musicPrefab = Resources.Load<GameObject>("Prefabs/MusicController");
    if (musicPrefab != null)
    {
        GameObject musicObj = Object.Instantiate(musicPrefab);
        musicObj.name = "MusicController_System";
        Object.DontDestroyOnLoad(musicObj);
    }
    else
    {
        Debug.LogWarning("<color=yellow>Bootstrap:</color> MusicController prefab not found in Resources/Prefabs/. No background music will play.");
    }
        // --- PLAYER ---
        GameObject playerPrefab = Resources.Load<GameObject>("Prefabs/Player");

        if (playerPrefab != null)
        {
            GameObject player = Object.Instantiate(playerPrefab);
            player.name = "Player_Character";
            Object.DontDestroyOnLoad(player);

            mover.playerTransform = player.transform;
        }
        else
        {
            Debug.LogError("<color=red>Bootstrap Error:</color> Player Prefab not found in Resources!");
        }

        Debug.Log("<color=cyan>Game Systems:</color> Camera and Player Persistent (Input System Ready).");
    }
}