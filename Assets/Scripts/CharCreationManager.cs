using UnityEngine;
using TMPro;

public class CharCreationManager : MonoBehaviour
{
    public int pointsRemaining = 6;
    private int startingPoints;
    public TextMeshProUGUI pointsText;

    private void Start() 
    {
        startingPoints = pointsRemaining;
        UpdatePointsUI();
    }

    public bool CanSpendPoint() => pointsRemaining > 0;

    public void SpendPoint()
    {
        pointsRemaining--;
        UpdatePointsUI();
    }

    public void RefundPoint()
    {
        pointsRemaining++;
        UpdatePointsUI();
    }

    private void UpdatePointsUI()
    {
        pointsText.text = "Points Left: " + pointsRemaining;
    }
    public void ResetPoints()
{
    pointsRemaining = startingPoints;
    UpdatePointsUI();
}
}
