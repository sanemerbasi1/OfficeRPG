using UnityEngine;
using UnityEngine.UI;
using System.Collections;
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
    public TMP_Text fullLogText; 
    public GameObject fullLogPanel;
    [SerializeField] private ScrollRect logScrollRect;
    [SerializeField] private RectTransform logContent;

    [Header("Turn Order Display")]
    [SerializeField] private Image slotPrev2;
    [SerializeField] private Image slotPrev1;
    [SerializeField] private Image slotCurrent;
    [SerializeField] private Image slotNext1;
    [SerializeField] private Image slotNext2;

    private List<Sprite> turnSequence = new List<Sprite>();
    private int turnIndex = 0;

    // Tracks all active skill buttons for cooldown management
    private List<SkillButton> activeSkillButtons = new List<SkillButton>();

    public void SetupBattleUI(string pName, string eName, Sprite eSprite)
    {
        playerNameText.text = pName;
        enemyNameText.text = eName;
        enemyImage.sprite = eSprite;
    }

    public void GenerateSkillButtons(List<SkillData> playerSkills)
    {
        foreach (Transform child in skillContainer) Destroy(child.gameObject);
        activeSkillButtons.Clear();

        foreach (SkillData skill in playerSkills)
        {
            GameObject btnObj = Instantiate(skillButtonPrefab, skillContainer);
            SkillButton btnScript = btnObj.GetComponent<SkillButton>();
            if (btnScript != null)
            {
                btnScript.Setup(skill);
                activeSkillButtons.Add(btnScript);
            }
        }
    }

    public void NotifySkillUsed(SkillData skill)
    {
        SkillButton btn = activeSkillButtons.Find(b => b.skill == skill);
        if (btn != null) btn.OnSkillUsed();
    }

    public void TickAllCooldowns()
    {
        foreach (SkillButton btn in activeSkillButtons)
            btn.TickCooldown();
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
    public void ToggleLogPanel()
{
    if (fullLogPanel != null)
        fullLogPanel.SetActive(!fullLogPanel.activeSelf);
}
public void ClearLog()
{
    if (contextLogText != null) contextLogText.text = "";
    if (fullLogText != null) fullLogText.text = "";
}

   public void UpdateLog(string message)
{
    if (contextLogText != null)
        contextLogText.text = message;

    if (fullLogText != null)
       {
        fullLogText.text += "\n\n" + message;
        StartCoroutine(ScrollToBottom());
    }
    
}
IEnumerator ScrollToBottom()
{
    yield return new WaitForEndOfFrame();
    LayoutRebuilder.ForceRebuildLayoutImmediate(logContent);
    logScrollRect.verticalNormalizedPosition = 0f;
}

public void InitializeTurnDisplay(Sprite player, Sprite enemy, bool playerFirst)
{
    turnSequence.Clear();
    Sprite enemySprite = playerFirst ? player : enemy;
    Sprite playerSprite = playerFirst ? enemy : player;
    for (int i = 0; i < 20; i++)
        turnSequence.Add(i % 2 == 0 ? playerSprite : enemySprite);

    turnIndex = 2;
    RefreshTurnSlots();
}

public void AdvanceTurnDisplay()
{
    turnIndex++;
    if (turnIndex + 3 >= turnSequence.Count)
    {
        Sprite last = turnSequence[turnSequence.Count - 1];
        Sprite next = last == turnSequence[0] ? turnSequence[1] : turnSequence[0];
        for (int i = 0; i < 10; i++)
            turnSequence.Add(i % 2 == 0 ? next : last);
    }
    RefreshTurnSlots();
}
private void RefreshTurnSlots()
{
    SetSlot(slotPrev2,   turnIndex - 2, 0.35f);
    SetSlot(slotPrev1,   turnIndex - 1, 0.5f);
    SetSlot(slotCurrent, turnIndex,     1f);
    SetSlot(slotNext1,   turnIndex + 1, 0.5f);
    SetSlot(slotNext2,   turnIndex + 2, 0.35f);
}

private void SetSlot(Image slot, int index, float alpha)
{
    if (slot == null) return;
    bool valid = index >= 0 && index < turnSequence.Count;
    slot.gameObject.SetActive(valid);
    if (!valid) return;

    slot.sprite = turnSequence[index];
    Color c = slot.color; c.a = alpha; slot.color = c;
}
}