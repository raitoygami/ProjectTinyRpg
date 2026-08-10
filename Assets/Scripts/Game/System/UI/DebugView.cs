using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

public class DebugView : PubSubActor
{
    [SerializeField] private TMP_Text m_TextTurn;
    private void Awake()
    {
        //GetComponent<TileSelector>().Setup(_material);
        this.SubscribeGlobal<TurnManager.NewLoopEvt>(OnNewCycle);
    }
    private UniTask OnNewCycle(TurnManager.NewLoopEvt arg)
    {
        m_TextTurn.text = arg.LoopCount.ToString();
        return UniTask.CompletedTask;
    }

}
