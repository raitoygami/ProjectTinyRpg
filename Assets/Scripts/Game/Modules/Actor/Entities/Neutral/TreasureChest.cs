using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class TreasureChest : Entity
{
    [SerializeField] private Dialogue _dialogue;
    //[SerializeField] private List<QuestTemplateAsset>  _Quests = new();
    private DialogueContext _Context;
    protected void Awake()
    {
        Faction = EntityFaction.Neutral;
        _Context = new DialogueContext();
        /*foreach (var quest in _Quests)
        {
            QuestManager.Instance.TryRegisterFromTemplate(quest);
        }*/
        this.Subscribe<AgentInteractive.InteractionEvent>(OnInteraction);
    }

    private async UniTask OnInteraction(AgentInteractive.InteractionEvent arg)
    {
        await UIRoot.Instance.Dialogue.StartDialogueAsync(_dialogue, _Context, destroyCancellationToken);
    }
    
}