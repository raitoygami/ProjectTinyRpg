using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PrimaryStatPanel : MonoBehaviour
{

    [SerializeField] private TMP_Text statTextLevel;
    [SerializeField] private TMP_Text statTextStatPoint;
    [SerializeField] private TMP_Text statTextExp;
    
    [SerializeField] private TMP_Text statTextStr; // 德
    [SerializeField] private TMP_Text statTextInt; // 智
    [SerializeField] private TMP_Text statTextVit; // 体
    [SerializeField] private TMP_Text statTextDex; // 美

    [SerializeField] private Button m_BtnAddStr;
    [SerializeField] private Button m_BtnAddInt;
    [SerializeField] private Button m_BtnAddVit;
    [SerializeField] private Button m_BtnAddDex;

    private void Awake()
    {
        var stats = PlayerManager.Instance.GetStats();
        statTextLevel.text = stats.Level.ToString();
        statTextStatPoint.text = stats.StatPoints.ToString();
        var config = ConfigManager.Instance.Tables.DataLevelUp[stats.Level];
        var nextConfig = ConfigManager.Instance.Tables.DataLevelUp[stats.Level + 1];
        // 满级了
        if (nextConfig == null)
        {
            statTextExp.gameObject.SetActive(false);
        }
        else
        {
            statTextExp.text = $"{stats.Experience}/{config.Exp}";
        }

    }
    
}
