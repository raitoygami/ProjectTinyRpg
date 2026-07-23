using System;

/// <summary>
/// 示例：全局击杀事件。玩法在敌人死亡处调用
/// <c>Context.Instance.Messager.Publish(new QuestEnemyKilledSampleEvent { EnemyTypeId = ... })</c>
/// 或 MonoBehaviour 扩展 <c>this.PublishGlobal(...)</c>（见 <c>Extensions</c>）。
/// </summary>
public class QuestEnemyKilledSampleEvent : EventArgs
{
    public int EnemyTypeId;
}
