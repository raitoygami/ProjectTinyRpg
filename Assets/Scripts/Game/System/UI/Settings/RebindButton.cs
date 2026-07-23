using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RebindButton : MonoBehaviour
{
    public string actionName;           // 在Inspector中填写 "Movement"、"Jump" 等
    [SerializeField] private Button button;
    [SerializeField] private TextMeshProUGUI bindingText;

    private void Awake()
    {
        button.onClick.AddListener(StartRebind);
        RefreshDisplay();
    }

    private void StartRebind()
    {
        InputSystem.Instance.StartRebinding(actionName, 0, RefreshDisplay);
    }

    private void RefreshDisplay()
    {
        bindingText.text = InputSystem.Instance.GetBindingDisplay(actionName);;
    }
}