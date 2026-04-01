using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;


public class StatHandler : MonoBehaviour
{
    [HideInInspector]
    public CharCreationManager master;

    [Header("UI Pips")]
    [Tooltip("Drag all 14 images for THIS attribute here")]
    public List<Image> pips = new List<Image>();
    
    public Color defaultActiveColor = Color.red;
    public Color activeColor = Color.green;
    public Color inactiveColor = Color.gray;

    [Header("Values")]
    public int currentStatValue = 1;
    private int maxStatValue = 4;

    private void Start()
    {
        master = GetComponentInParent<CharCreationManager>();
        if (master == null)
        {
            Debug.LogError("StatHandler on " + gameObject.name + " couldn't find a CharCreationManager on any parent!");
        }

       pips[0].color = defaultActiveColor;
        UpdateUI();
    }

    public void IncreaseStat()
    {

        if (master.pointsRemaining > 0 && currentStatValue < maxStatValue)
        {
            currentStatValue++;
            master.SpendPoint();
            UpdateUI();
        }
    }

    public void DecreaseStat()
    {

        if (currentStatValue > 1)
        {
            currentStatValue--;
            master.RefundPoint();
            UpdateUI();
        }
    }

    private void UpdateUI()
    {
        for (int i = 1; i < pips.Count; i++)
        {
            if (i < currentStatValue)
            {
                pips[i].color = activeColor;
            }
            else
            {
                pips[i].color = inactiveColor;
            }
        }
    }

    public void ResetStat()
{
    currentStatValue = 1; 
    UpdateUI();
}
public void SaveToData(PlayerStats dataTable)
{
    string myName = gameObject.name.ToLower();

    if (myName.Contains("communication")) dataTable.communication = currentStatValue;
    else if (myName.Contains("critical")) dataTable.criticalThinking = currentStatValue;
    else if (myName.Contains("adaptability")) dataTable.adaptability = currentStatValue;
    else if (myName.Contains("emotional")) dataTable.emotionalIntelligence = currentStatValue;
    else if (myName.Contains("sustainability")) dataTable.sustainability = currentStatValue;
    else if (myName.Contains("leadership")) dataTable.leadership = currentStatValue;
}
}