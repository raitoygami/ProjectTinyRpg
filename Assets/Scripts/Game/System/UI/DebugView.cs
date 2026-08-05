using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

public class DebugView : PubSubActor
{
    [SerializeField] private Material _material;
    [SerializeField] private TMP_Text m_TextTurn;
    [SerializeField] private Ability TestAbility;
    [SerializeField] private Ability TestAbility2;

    private Ability _AbilityRef;
    private Ability _AbilityRef2;
    
    private void Awake()
    {
        _AbilityRef = Instantiate(TestAbility);
        _AbilityRef2 =  Instantiate(TestAbility2);
        //GetComponent<TileSelector>().Setup(_material);
        this.SubscribeGlobal<TurnManager.NewLoopEvt>(OnNewCycle);
    }
    
    private UniTask OnNewCycle(TurnManager.NewLoopEvt arg)
    {
        m_TextTurn.text = arg.LoopCount.ToString();
        return UniTask.CompletedTask;
    }

    public void TestSkill()
    {
        if (_AbilityRef.IsSelecting())
        {
            _AbilityRef.Cancel();            
        }
        else
        {
            var player = Context.Instance.PlayerInst;
            _AbilityRef.SetOwner(player);
            _ = player.PrepareAbility(_AbilityRef);
        }
    }
    
    public void TestSkill2()
    {
        if (TestAbility2.IsSelecting())
        {
            TestAbility2.Cancel();            
        }
        else
        {
            var player = Context.Instance.PlayerInst;
            TestAbility2.SetOwner(player);
            _ = player.PrepareAbility(TestAbility2);
        }
    }
}
