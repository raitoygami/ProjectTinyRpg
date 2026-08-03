/*using System;
using System.Linq;

public partial class GameData
{
    public Quest[] quests;

    public void EnsureQuestsInitialized()
    {
        if (quests == null)
            quests = Array.Empty<Quest>();
    }

    /// <summary>若不存在同 <see cref="Quest.questId" /> 则追加（同一实例即存档行）。</summary>
    public void AddQuestIfMissing(Quest quest)
    {
        if (quest == null || string.IsNullOrEmpty(quest.questId)) return;
        EnsureQuestsInitialized();
        if (quests.Any(t => t != null && t.questId == quest.questId)) return;

        var next = new Quest[quests.Length + 1];
        Array.Copy(quests, next, quests.Length);
        next[quests.Length] = quest;
        quests = next;
    }

    public bool TryGetQuest(string questId, out Quest quest)
    {
        quest = null;
        if (string.IsNullOrEmpty(questId)) return false;
        EnsureQuestsInitialized();
        foreach (var q in quests)
        {
            if (q == null || q.questId != questId) continue;
            quest = q;
            return true;
        }

        return false;
    }

    public bool RemoveQuest(string questId)
    {
        if (string.IsNullOrEmpty(questId)) return false;
        EnsureQuestsInitialized();
        var idx = -1;
        for (var i = 0; i < quests.Length; i++)
            if (quests[i] != null && quests[i].questId == questId)
            {
                idx = i;
                break;
            }

        if (idx < 0) return false;
        if (quests.Length == 1)
        {
            quests = Array.Empty<Quest>();
            return true;
        }

        var next = new Quest[quests.Length - 1];
        for (int i = 0, j = 0; i < quests.Length; i++)
            if (i != idx)
                next[j++] = quests[i];
        quests = next;
        return true;
    }

    private void NormalizeQuestsAfterDeserialize()
    {
        if (quests == null) return;
        foreach (var q in quests)
            q?.NormalizeAfterDeserialize();
    }

    private static readonly int _registerQuestSerialization = RegisterQuestSerialization();

    private static int RegisterQuestSerialization()
    {
        RegisterSerializationCallbacks(
            d => d.EnsureQuestsInitialized(),
            d =>
            {
                d.EnsureQuestsInitialized();
                d.NormalizeQuestsAfterDeserialize();
            });
        return 0;
    }
}*/