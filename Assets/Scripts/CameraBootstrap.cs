using System.Diagnostics;
using UnityEngine;

public class GameBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void InitializeGameSystem()
    {
        // 1. Setup Camera System (Your existing logic)
        GameObject boom = new GameObject("CameraBoom_System");
        CameraMover mover = boom.AddComponent<CameraMover>();

        GameObject camObj = new GameObject("Main Camera");
        camObj.transform.SetParent(boom.transform);
        camObj.tag = "MainCamera";
    
        Camera cam = camObj.AddComponent<Camera>();
        cam.orthographic = false;
        //cam.fieldOfView = 5f; 
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 1000f;
        camObj.transform.localPosition = new Vector3(0, 0, 0);

        Object.DontDestroyOnLoad(boom);

        // 2. Setup Persistent Player
        // Note: Put your Player prefab in Assets/Resources/Prefabs/Player
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

        Debug.Log("<color=cyan>Game Systems:</color> Camera and Player Persistent.");
    }
}