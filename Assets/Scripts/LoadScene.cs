using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    private string currentFloor;

    public void GoToFloor(string nextFloor)
    {
        StartCoroutine(TransitionFloor(nextFloor));
    }

    public void GoToMainMenu()
    {
        if (currentFloor != null)
            SceneManager.UnloadSceneAsync(currentFloor);

        SceneManager.LoadScene("MainMenu");
    }

    public void StartGame()
    {
        StartCoroutine(TransitionFloor("Floor_01"));
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
}