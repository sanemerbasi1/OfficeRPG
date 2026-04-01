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
    private int maxStatValue = 5;

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
        // 1. Check if we have points left in the bank
        // 2. Check if we are already at the max 
        if (master.pointsRemaining > 0 && currentStatValue < maxStatValue)
        {
            currentStatValue++;
            master.SpendPoint(); // Tell the master to subtract 1
            UpdateUI();
        }
    }

    public void DecreaseStat()
    {
        // Check if we have at least 1 point spent here
        if (currentStatValue > 1)
        {
            currentStatValue--;
            master.RefundPoint(); // Tell the master to add 1 back
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
}