using UnityEngine;
using UnityEngine.UI;

public class ActionSlotIconObj : MonoBehaviour
{
    [SerializeField] private Image _Icon;

    private Ability _AbilityReference;

    public void UpdateAbility(Ability ability)
    {
        _AbilityReference = ability;
        _Icon.sprite = _AbilityReference.Icon;
    }

    public void UpdateIcon(Sprite sprite)
    {
        _Icon.sprite = sprite;
    }
    
}
