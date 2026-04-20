using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillButton : MonoBehaviour
{
    public TextMeshProUGUI skillNameText;
    public Image iconImage;
    private SkillData assignedSkill;
    private Button btn;

    private void Awake()
    {
        // Cache the button component once to save performance
        btn = GetComponent<Button>();
    }

    // Called by BattleUI during the skill button generation loop
    public void Setup(SkillData skill)
    {
        assignedSkill = skill;
        
        if (skillNameText != null)
            skillNameText.text = skill.skillName;
        
        // Handle the Icon
        if (iconImage != null)
        {
            if (skill.skillIcon != null)
            {
                iconImage.sprite = skill.skillIcon;
                iconImage.gameObject.SetActive(true);
            }
            else
            {
                // Hide the icon object if the SkillData doesn't have an icon assigned
                iconImage.gameObject.SetActive(false);
            }
        }

        // Clean up old listeners and add a new one for this specific skill
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(OnButtonClick);
        }
    }

    private void OnButtonClick()
    {
        // Safety check to ensure we have a skill and the BattleManager exists
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