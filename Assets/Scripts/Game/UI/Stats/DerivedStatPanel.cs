using System;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

public class DerivedStatPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text StatHealthMax;
    [SerializeField] private TMP_Text StatManaMax;
    
    [SerializeField] private TMP_Text StatPhysicsAttack;
    [SerializeField] private TMP_Text StatMagicAttack;
    [SerializeField] private TMP_Text StatCritRate;
    [SerializeField] private TMP_Text StatCritMulti;
    
    [SerializeField] private TMP_Text StatArmorPen;
    [SerializeField] private TMP_Text StatMagicPen;
    [SerializeField] private TMP_Text StatArmorResist;
    [SerializeField] private TMP_Text StatMagicResist;

    [SerializeField] private TMP_Text StatEvade;

    private void Awake()
    {
        this.SubscribeGlobal<Context.PlayerStatsChangeEvt>(OnPlayerStatsChanged);
    }

    private void Start()
    {
        RefreshStats();
    }

    private UniTask OnPlayerStatsChanged(Context.PlayerStatsChangeEvt arg)
    {
        RefreshStats();
        return UniTask.CompletedTask;    
    }

    private void RefreshStats()
    {
        if (!Context.HasInstance())
            return;
        var player = Context.Instance.PlayerInst;
        if (player == null)
            return;
        var stats = player.GetComponent<AgentStats>();
        if (stats == null)
            return;

        StatHealthMax.text = stats.MaxHealth.ToString();
        StatManaMax.text = stats.MaxMana.ToString();
        
        StatPhysicsAttack.text = stats.PhysicalAttack.ToString();
        StatMagicAttack.text = stats.MagicalAttack.ToString();
        StatCritRate.text = stats.CritChance.ToString();
        StatCritMulti.text = stats.CritMultiplier.ToString();
            
        StatArmorPen.text = stats.ArmorPenetration.ToString();
        StatMagicPen.text = stats.MagicPenetration.ToString();
        
        StatArmorResist.text = stats.ArmorResist.ToString();
        StatMagicResist.text = stats.MagicResist.ToString();
        StatEvade.text = stats.Evade.ToString();
    }
}
