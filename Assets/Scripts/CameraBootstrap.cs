using UnityEngine;

public class CameraBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void InitializeCameraSystem()
    {
        GameObject boom = new GameObject("CameraBoom_System");
        boom.AddComponent<CameraMover>();

        GameObject camObj = new GameObject("Main Camera");
        camObj.transform.SetParent(boom.transform);
        camObj.tag = "MainCamera";
        
        Camera cam = camObj.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5f; 
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 1000f;

        camObj.transform.localPosition = new Vector3(0, 0, -10);

        Object.DontDestroyOnLoad(boom);
        
        Debug.Log("<color=cyan>Camera System:</color> Auto-Spawned and ready for action.");
    }
}