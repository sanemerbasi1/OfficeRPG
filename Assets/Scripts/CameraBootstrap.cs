using UnityEngine;

public class CameraBootstrap
{
    // FIX: Changed 'RuntimeInitializeLoadMethod' to 'RuntimeInitializeLoadType'
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void InitializeCameraSystem()
    {
        // 1. Create the Boom (The Parent/Handle)
        GameObject boom = new GameObject("CameraBoom_System");
        boom.AddComponent<CameraMover>();

        // 2. Create the actual Main Camera (The Child/Lens)
        GameObject camObj = new GameObject("Main Camera");
        camObj.transform.SetParent(boom.transform);
        camObj.tag = "MainCamera";
        
        // 3. Set up the Camera Component for 2D Pixel Art
        Camera cam = camObj.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5f; 
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 1000f;

        // 4. Position the camera relative to the Boom
        camObj.transform.localPosition = new Vector3(0, 0, -10);

        // 5. Keep this system alive across all scenes
        Object.DontDestroyOnLoad(boom);
        
        Debug.Log("<color=cyan>Camera System:</color> Auto-Spawned and ready for action.");
    }
}