using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UI_SkillSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private UI ui;
    private Image skillIcon;
    private RectTransform rect;
    private Button button;
    private Skill_DataSO skillData;
    public SkillType skillType;
    [SerializeField] private Image cooldownOverlay;
    [SerializeField] private string inputKeyName;
    [SerializeField] private TextMeshProUGUI inputKeyText;
    [SerializeField] private GameObject conflictSlot;

    private void Awake()
    {
        ui = GetComponentInParent<UI>();
        button = GetComponent<Button>();
        skillIcon = GetComponent<Image>();
        rect = GetComponent<RectTransform>();
    }

    private void OnValidate()
    {
        gameObject.name = "UI_SkillSlot - " + skillType.ToString();
    }

    public void SetUpSkillSlot(Skill_DataSO selectedSkill)
    {
        this.skillData = selectedSkill;

        Color color = Color.black;
        color.a = 0.6f;
        cooldownOverlay.color = color;

        inputKeyText.text = inputKeyName;
        skillIcon.sprite = selectedSkill.icon;

        if(conflictSlot != null)
            conflictSlot.SetActive(false);
    }

    public void StartCooldown(float cooldown)
    {
        cooldownOverlay.fillAmount = 1f;

        StartCoroutine(CooldownCo(cooldown));
    }

    public void ResetCooldown() => cooldownOverlay.fillAmount = 0f;

    private IEnumerator CooldownCo(float duration)
    {
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            cooldownOverlay.fillAmount = 1f - (elapsedTime / duration);
            yield return null;
        }

        cooldownOverlay.fillAmount = 0f;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if(skillData == null)
            return;
        
        ui.skillToolTip.ShowToolTip(true, rect, skillData, null);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ui.skillToolTip.ShowToolTip(false, null);
    }
}
