using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillButton : MonoBehaviour
{
    public TextMeshProUGUI skillNameText;
    public GameObject cooldownPanel; 
    private TextMeshProUGUI cooldownText; 
    public Image iconImage;

    private SkillData assignedSkill;
    private Button btn;
    private int remainingCooldown = 0;

    // Expose skill for BattleUI to find via Find()
    public SkillData skill => assignedSkill;

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
            if (skill.skillIcon != null)
            {
                iconImage.sprite = skill.skillIcon;
                iconImage.gameObject.SetActive(true);
            }
            else
            {
                iconImage.gameObject.SetActive(false);
            }
        }

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
        if (btn != null) btn.interactable = !onCooldown;

        if (cooldownText != null)
        {
            cooldownText.gameObject.SetActive(onCooldown);
            if (onCooldown) cooldownText.text = remainingCooldown.ToString();
        }
    }

    private void OnButtonClick()
    {
        if (assignedSkill != null && BattleManager.Instance != null)
        {
            BattleManager.Instance.ExecutePlayerAction(assignedSkill);
        }
        else
        {
            Debug.LogWarning("SkillButton: Assigned skill or BattleManager.Instance is missing!");
        }
    }
}