using UnityEngine;
using TMPro;

public class CharCreationManager : MonoBehaviour
{
    public int statPointsRemaining = 6;
    public int traitPointsRemaining = 2;
    private int startingPoints;
    private int startingTraitPoints;
    public TextMeshProUGUI statPointsText;
    public TextMeshProUGUI traitPointsText;

    private void Start() 
    {
        startingPoints = statPointsRemaining;
        startingTraitPoints = traitPointsRemaining;
        UpdatePointsUI();
    }

    public bool CanSpendPoint() => statPointsRemaining > 0;

    public void SpendPoint()
    {
        statPointsRemaining--;
        UpdatePointsUI();
    }

    public void RefundPoint()
    {
        statPointsRemaining++;
        UpdatePointsUI();
    }

    private void UpdatePointsUI()
    {
        statPointsText.text = "Points Left: " + statPointsRemaining;
        traitPointsText.text = "Trait Points Left: " + traitPointsRemaining;
    }
    public void ResetPoints()
{
    statPointsRemaining = startingPoints;
    traitPointsRemaining = startingTraitPoints;
    UpdatePointsUI();
}

public void SpendTraitPoint()
{
    traitPointsRemaining--;
    UpdatePointsUI();
}

public void RefundTraitPoint()
{
    traitPointsRemaining++;
    UpdatePointsUI();
}
}
