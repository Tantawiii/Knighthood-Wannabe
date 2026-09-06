using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_InGame : MonoBehaviour
{
    private Player player;
    private UI_SkillSlot[] skillSlots;
    
    [SerializeField] private RectTransform healthRect;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TextMeshProUGUI healthText;

    private void Awake()
    {
        // Cached in Awake so skill slots are ready before any Start() (e.g. Skill_Base
        // unlocking default skills) queries them.
        skillSlots = GetComponentsInChildren<UI_SkillSlot>(true);
    }

    private void Start()
    {
        player = FindFirstObjectByType<Player>();
        player.health.OnHealthUpdate += UpdateHealthBar;
    }

    public UI_SkillSlot GetSkillSlot(SkillType skillType)
    {
        foreach (var slot in skillSlots)
        {
            if (slot.skillType == skillType)
            {
                slot.gameObject.SetActive(true);
                return slot;
            }
        }
        return null;
    }

    private void UpdateHealthBar()
    {
        float currentHealth = Mathf.Min(Mathf.RoundToInt(player.health.GetCurrentHealthValue()), player.entityStats.GetMaxHealth());
        float maxHealth = player.entityStats.GetMaxHealth();
        float sizeDifference = Mathf.Abs(maxHealth - healthRect.sizeDelta.x);

        if(sizeDifference > 0.1f)
        {
            healthRect.sizeDelta = new Vector2(maxHealth * .2f, healthRect.sizeDelta.y);
        }

        healthText.text = currentHealth + " / " + maxHealth;
        healthSlider.value = player.health.GetHealthPercent();
    }
}
