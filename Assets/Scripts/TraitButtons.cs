using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Outline))]
public class TraitButton : MonoBehaviour
{
    public TraitData traitData;
    public TextMeshProUGUI nameText;
    public Image iconDisplay;

    private Outline outlineEffect;
    private UIManager uiManager;
    private CharCreationManager charMaster;
    private bool isSelected = false;

    [Header("Outline Settings")]
    public Color selectedColor = Color.green;
    public Color defaultColor = new Color(0, 0, 0, 0.5f);

    void Awake()
    {
        outlineEffect = GetComponent<Outline>();
        outlineEffect.effectColor = defaultColor;
        
        RefreshReferences();

        if (traitData != null)
        {
            if (nameText != null) nameText.text = traitData.traitName;
            if (iconDisplay != null) iconDisplay.sprite = traitData.traitIcon;
        }
    }

    // New Helper function to find managers even if they were inactive earlier
    private void RefreshReferences()
    {
        if (uiManager == null) uiManager = Object.FindFirstObjectByType<UIManager>(FindObjectsInactive.Include);
        if (charMaster == null) charMaster = Object.FindFirstObjectByType<CharCreationManager>(FindObjectsInactive.Include);
    }

    public void OnTraitButtonClick()
    {
        // One last check before running logic to prevent the crash
        RefreshReferences();

        if (charMaster == null)
        {
            Debug.LogError($"CharCreationManager is missing in the scene! Cannot process button click on {gameObject.name}");
            return;
        }

        if (isSelected) Deselect();
        else if (charMaster.traitPointsRemaining > 0) Select();
    }

    private void Select()
    {
        isSelected = true;
        outlineEffect.effectColor = selectedColor;
        
        charMaster.SpendTraitPoint();
        if (uiManager != null) uiManager.AddTraitToStats(traitData);
    }

    private void Deselect()
    {
        isSelected = false;
        outlineEffect.effectColor = defaultColor;
        
        charMaster.RefundTraitPoint();
        if (uiManager != null) uiManager.RemoveTraitFromStats(traitData);
    }
}