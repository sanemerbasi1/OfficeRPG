using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class BattleUI : MonoBehaviour
{
    [Header("Player Visuals")]
    public TextMeshProUGUI playerNameText;
    public Slider playerMHBar;
    public TextMeshProUGUI playerMHValueText; 
    public TextMeshProUGUI playerShieldText; 
    public Slider playerShieldBar;

    [Header("Enemy Visuals")]
    public TextMeshProUGUI enemyNameText;
    public Image enemyImage;
    public Slider enemyMHBar;
    public TextMeshProUGUI enemyMHValueText;
    public TextMeshProUGUI enemyShieldText;
    public Slider enemyShieldBar;

    [Header("Skill Menu System")]
    public GameObject actionButtonGroup; 
    public GameObject skillButtonPrefab; 
    public Transform skillContainer;     
    
    [Header("Status Text References")]
    public TextMeshProUGUI turnIndicatorText; 
    public TextMeshProUGUI contextLogText;

    public void SetupBattleUI(string pName, string eName, Sprite eSprite)
    {
        playerNameText.text = pName;
        enemyNameText.text = eName;
        enemyImage.sprite = eSprite;
    }

    public void GenerateSkillButtons(List<SkillData> playerSkills)
    {
        foreach (Transform child in skillContainer) Destroy(child.gameObject);

        foreach (SkillData skill in playerSkills)
        {
            GameObject btnObj = Instantiate(skillButtonPrefab, skillContainer);
            SkillButton btnScript = btnObj.GetComponent<SkillButton>();
            if (btnScript != null) btnScript.Setup(skill);
        }
    }

    public void UpdateStats(int pMH, int pMax, int pShield, int pArmor, 
                       int eMH, int eMax, int eShield, int eArmor)
{
    playerMHBar.maxValue = pMax;
    playerMHBar.value = pMH;

    playerShieldBar.maxValue = pMax; 
    playerShieldBar.value = pShield; 
    playerShieldBar.gameObject.SetActive(pShield > 0); 

    playerMHValueText.text = $"{pMH} / {pMax} MH";

    playerShieldText.text = $"ARM: {pShield}"; 

    enemyMHBar.maxValue = eMax;
    enemyMHBar.value = eMH;

    enemyShieldBar.maxValue = eMax;
    enemyShieldBar.value = eShield;
    enemyShieldBar.gameObject.SetActive(eShield > 0);

    enemyMHValueText.text = $"{eMH} / {eMax} MH";
    enemyShieldText.text = $"ARM: {eShield}";
}

    public void ToggleActionButtons(bool state)
    {
        if(actionButtonGroup != null) actionButtonGroup.SetActive(state);
    }

    public void UpdateTurnDisplay(string message, Color color)
    {
        if (turnIndicatorText != null)
        {
            turnIndicatorText.text = message;
            turnIndicatorText.color = color;
        }
    }

    public void UpdateLog(string message)
    {
        if (contextLogText != null) contextLogText.text = message;
    }
}