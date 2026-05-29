using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillButton : MonoBehaviour
{
    public TextMeshProUGUI skillNameText;
    public GameObject cooldownPanel; 
    public Image iconImage;

    private TextMeshProUGUI cooldownText; 
    private SkillData assignedSkill;
    private Button btn;
    private int remainingCooldown = 0;

    public SkillData skill => assignedSkill;
    
    // ADDED: Allows BattleUI to check if this specific button is currently locked out
    public bool IsOnCooldown => remainingCooldown > 0;

    private void Awake()
    {
        btn = GetComponent<Button>();
        if (cooldownPanel != null)
            cooldownText = cooldownPanel.GetComponentInChildren<TextMeshProUGUI>();
    }

    public void Setup(SkillData skill)
    {
        assignedSkill = skill;
        remainingCooldown = 0;

        if (skillNameText != null)
            skillNameText.text = skill.skillName;

        if (iconImage != null)
        {
            iconImage.sprite = skill.skillIcon;
            iconImage.gameObject.SetActive(skill.skillIcon != null);
        }

        // Lazy initialization safeguard
        if (btn == null) btn = GetComponent<Button>();

        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(OnButtonClick);
        }

        RefreshUI();
    }

    public void OnSkillUsed()
    {
        remainingCooldown = assignedSkill.cooldownTurns;
        RefreshUI();
    }

    public void TickCooldown()
    {
        if (remainingCooldown > 0)
        {
            remainingCooldown--;
            RefreshUI();
        }
    }

    private void RefreshUI()
    {
        bool onCooldown = remainingCooldown > 0;
        
        if (btn != null) 
            btn.interactable = !onCooldown;

        // Optimization: Toggles the whole panel background, not just text mesh
        if (cooldownPanel != null) 
            cooldownPanel.SetActive(onCooldown);

        if (cooldownText != null && onCooldown) 
            cooldownText.text = remainingCooldown.ToString();
    }

    private void OnButtonClick()
    {
        // Safe, clean execution statement without debugging bloat
        if (assignedSkill != null && BattleManager.Instance != null)
        {
            BattleManager.Instance.ExecutePlayerAction(assignedSkill);
        }
    }
}