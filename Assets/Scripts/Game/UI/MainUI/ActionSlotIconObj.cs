using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ActionSlotIconObj : MonoBehaviour
{
    [SerializeField] private Image _Icon;
    [SerializeField] private GameObject _nodeCooldown;
    [SerializeField] private TMP_Text _textCooldown;
    private int _abilityReference;
    private AbilityStat _abilityStat;
    public void UpdateAbilityInfo(int abilityID, AbilityStat abilityStat)
    {
        if (_abilityStat != null)
            _abilityStat.OnCooldownChanged -= OnCooldownChanged;
        _abilityReference = abilityID;
        _abilityStat = abilityStat;
        _abilityStat.OnCooldownChanged += OnCooldownChanged;
        _nodeCooldown.gameObject.SetActive(_abilityStat is { Cooldown: > 0 });
        _textCooldown.text = _abilityStat != null ? _abilityStat.Cooldown.ToString() : "";
    }

    private void OnCooldownChanged()
    {
        Debug.Log($"Ability {_abilityReference} changed {_abilityStat is { Cooldown: > 0 }}");
        _nodeCooldown.gameObject.SetActive(_abilityStat is { Cooldown: > 0 });
        _textCooldown.text = _abilityStat != null ? _abilityStat.Cooldown.ToString() : "";
    }

    public void UpdateIcon(Sprite sprite)
    {
        _Icon.sprite = sprite;
    }
    
    
    
}
