using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    private string currentFloor;

    [SerializeField] private UIManager uiManager;

    public void GoToFloor(string nextFloor)
    {
        StartCoroutine(TransitionFloor(nextFloor));
    }

    public void GoToMainMenu()
    {
        if (BattleManager.Instance != null)
        BattleManager.Instance.ForceEndBattle();
        
        if (currentFloor != null)
            SceneManager.UnloadSceneAsync(currentFloor);

        SceneManager.LoadScene("MainMenu");
        
    }

    public void StartGame()
    {
        StartCoroutine(StartGameRoutine());
        uiManager.TogglePlayerMovement(true);
    }

    IEnumerator StartGameRoutine()
    {
        AsyncOperation load = SceneManager.LoadSceneAsync("Floor_Sec0", LoadSceneMode.Additive);
        yield return load;

        SceneManager.SetActiveScene(SceneManager.GetSceneByName("Floor_Sec0"));
        SceneManager.UnloadSceneAsync("MainMenu");
        currentFloor = "Floor_Sec0";

        ResetPlayerTransform();
    }

    private void ResetPlayerTransform()
    {
        GameObject player = GameObject.Find("Player_Character");
        if (player != null)
        {
            // If you use CharacterController, it must be disabled to teleport
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            player.transform.position = Vector3.zero;
            player.transform.rotation = Quaternion.identity;

            if (cc != null) cc.enabled = true;
            
            Debug.Log("Player reset to origin for new game.");
        }
        else
        {
            Debug.LogWarning("Player_Character not found to reset position!");
        }
    }

    IEnumerator TransitionFloor(string nextFloor)
    {
        AsyncOperation load = SceneManager.LoadSceneAsync(nextFloor, LoadSceneMode.Additive);
        yield return load;

        SceneManager.SetActiveScene(SceneManager.GetSceneByName(nextFloor));

        if (currentFloor != null)
            SceneManager.UnloadSceneAsync(currentFloor);

        currentFloor = nextFloor;
    }

    public void QuitGame()
    {
        Application.Quit();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}