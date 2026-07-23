# 任务系统（Quest）使用说明

**代码目录**：**`Scripts/Quest/`** — **`Quest`**、**`QuestPhase`**、**`QuestManager`**、**`QuestDeepClone`**、**`QuestTemplateAsset`**、**`QuestGoal/`**、**Editor**（**`QuestPhasePropertyDrawer`**、**`QuestTemplateAssetEditor`**）。

## 1. 架构概览

| 类型 | 作用 |
|------|------|
| **`Quest`** | **具体**任务类型（**`sealed`**）；阶段与目标在 **编辑器**（**`QuestTemplateAsset`** 等）组装，一般不再写 **`Quest`** 派生类。 |
| **`QuestPhase`** | 一个阶段：`phaseId` + **`List<QuestGoal> goals`**（**`[SerializeReference]`**，Inspector 由自定义 Drawer 绘制）。 |
| **`QuestGoal`** | 目标抽象基类；子类放在 **`Scripts/Quest/QuestGoal/`**；可重写 **`BindListening` / `UnbindListening`**（见 §4）。 |
| **`QuestManager`** | 单例：注册、查询、接受/推进/完成等入口；数据只存在 **`GameData.quests`**。 |
| **`GameData.quests`** | `Quest[]`，与 **`PersistenceModule`** 存档（Newtonsoft UTF-8 JSON）同一份引用。 |
| **`QuestType`** | `Main` / `Side` / `Daily` / `Hidden` 等分类。 |
| **`QuestTemplateAsset`** | ScriptableObject：Inspector 编辑任务；**`InstantiateRuntimeQuest()`** 经 **`QuestDeepClone`** 克隆后注册（§10）。 |
| **`QuestDeepClone`** | 模板运行时深拷贝 **`Quest` / `QuestGoal`**（反射字段拷贝，无任务侧 JSON）。 |
| **`QuestConfigBootstrap`** | 运行时把 Inspector 中拖入的 **`QuestTemplateAsset[]`** 克隆并 **`Register`**（§10）。 |

**前置条件**：场景中必须有 **`PersistenceModule`**（提供运行时 **`GameData`**）。否则 **`QuestManager.Register`** 会抛 **`InvalidOperationException`**。

---

## 2. 创建任务并注册

推荐用 **`QuestTemplateAsset`** 在 Inspector 里组装；代码侧示例：

---

## 3. 玩家接受任务

```csharp
bool ok = QuestManager.Instance.TryAcceptQuest("main_intro");

if (QuestManager.Instance.TryGetQuest("main_intro", out var q))
    q.TryAccept();
```

- **`TryAccept`**：成功则写入 **`acceptedMinutes`**、**`currentPhaseIndex = 0`**。若勾选 **`Quest.autoBindPubSubGoalListeners`**，会在接取后对全部 **`QuestGoal`** 调用 **`BindListening`**（见 §4.2）。
- 已在进行中且未完成 → 失败；已完成且 **`repeatable == false`** → 失败；**可重复**任务会先 **`ResetProgress`**（若开启自动绑定会先 **Unbind**）再接受。

---

## 4. 更新目标进度（业务侧）

### 4.0 目标完成通知（`OnComplete`）

任务已 **接取** 且未 **整任务完成** 时，派生类在 **`IsCompleted` 为 true** 后应调用 **`OnComplete()`**（先更新自身字段再调用）。  
基类会通知 **`QuestPhase`**：若当前阶段全部 **`QuestGoal`** 已完成，且该阶段正是任务的 **`CurrentPhase`**，则调用 **`Quest.TryAdvanceOrComplete()`**（推进到下一阶段或标记整任务完成）。

**上下文绑定**：**`Register` / 读档 `NormalizeAfterDeserialize` / `TryAccept`** 后会 **`AttachGoalContexts`**，为每个 Goal 写入所属 **`Quest`** 与 **`QuestPhase`**；未接取时 **`OnComplete`** 不会推进任务。

### 4.1 计数目标（`QuestGoalCounter`）

优先使用 **`AddProgress`**（内部会在达成时 **`OnComplete()`**）：

```csharp
if (g is QuestGoalCounter kill && kill.goalId == "kill")
    kill.AddProgress(1);
```

若直接改 **`current`**，外部代码无法调用 **`protected OnComplete()`**；请改用 **`AddProgress`**，或在派生类中提供在改写字段后调用 **`OnComplete()`** 的公共方法。

### 4.2 通过 PubSub 监听事件（解耦）

**`QuestGoal`** 实现 **`BindListening` / `UnbindListening`**。任务侧可选：

- 在 **`Quest`** / **`QuestTemplateAsset`** 上勾选 **`autoBindPubSubGoalListeners`**：接取时自动 **`BindListening`**，完成或重置进度时自动 **`UnbindListening`**。
- 不勾选时由业务自行遍历 **`phases` → `goals`** 调用上述方法。

读档后订阅句柄丢失时，对进行中任务调用 **`quest.RebindPubSubGoalListeners()`**；**`Unregister` 前**可调用 **`quest.ReleasePubSubGoalListeners()`**。

全局总线一般为 **`Context.Instance.Messager`**；**`PubSub.Subscribe<T>`** 返回的 **`Action`** 在 **`UnbindListening`** 里调用。

**玩法侧发布示例**：

```csharp
await Context.Instance.Messager.Publish(new QuestEnemyKilledSampleEvent { EnemyTypeId = 1 });
```

**示例类型**（**`Scripts/Quest/QuestGoal/`**）：**`QuestEnemyKilledSampleEvent`**、**`QuestGoalKillByPubSubSample`**。

自定义 **`QuestGoal`** 需实现 **`IsCompleted`** 与 **`Reset()`**；新增派生类后，编辑器 Goals 列表的 **+** 会通过 **`TypeCache.GetTypesDerivedFrom<QuestGoal>()`** 自动出现（需无参可构造）。

---

## 5. 推进阶段与完成任务

**阶段推进广播**：**`TryAdvanceOrComplete()`** 在非最后一阶段成功 **`currentPhaseIndex++`** 后，会通过 **`Context.Instance.Messager.Publish(new QuestPhaseAdvancedEvent { ... })`** 广播（**`Forget` 异步**）。订阅方可监听 **`QuestPhaseAdvancedEvent`** 获取 **`QuestId`**、前后阶段索引与 **`phaseId`**。

```csharp
quest.TryAdvancePhase();
quest.TryComplete();
quest.TryAdvanceOrComplete();
```

```csharp
QuestManager.Instance.TryAdvanceOrCompleteQuest("main_intro");
QuestManager.Instance.TryCompleteQuest("main_intro");
```

---

## 6. 查询与列表

```csharp
QuestManager.Instance.TryGetQuest(id, out var quest);
foreach (var q in QuestManager.Instance.AllQuests) { }
foreach (var q in QuestManager.Instance.GetQuestsByType(QuestType.Side)) { }
foreach (var q in QuestManager.Instance.GetActiveQuests()) { }
```

**状态**：**`IsAccepted`**、**`IsCompleted`**、**`CurrentPhase`**。

---

## 7. 时间与回合

- **`TurnManager.CurrentGameTime`**、**`GameTimeConverter.TurnRoundToGameMinutes`** 与任务 **`acceptedMinutes` / `completedMinutes`** 的关系见前文。

---

## 8. 存档与读档

- 任务在 **`PersistenceModule`** 的 **`GameData.quests`** 中；多态 **`QuestGoal`** 依赖 Newtonsoft **`TypeNameHandling.Auto`**（IL2CPP 注意裁剪与 **link.xml**）。
- 使用 PubSub 且 **`autoBindPubSubGoalListeners`** 时，读档后请在 **`Messager`** 就绪后对进行中任务调用 **`RebindPubSubGoalListeners()`**。

---

## 9. 扩展：自定义目标

**`Quest`** 为 **`sealed`**，流程扩展通过 **新 `QuestGoal` 派生类** + 编辑器组装即可。

```csharp
[Serializable]
public class QuestGoalTalkToNpc : QuestGoal
{
    public int npcInstanceId;
    public bool talked;

    public override bool IsCompleted => talked;
    public override void Reset() { talked = false; }
}
```

将脚本放在 **`Scripts/Quest/QuestGoal/`**（或任意运行时程序集）后，Inspector 中该阶段 **Goals** 的 **+** 菜单会出现新类型。

---

## 10. 任务模板（QuestTemplate）与运行时注册

### 10.1 编辑器：创建模板

1. **Create → Quest → Quest Template** → **`QuestTemplateAsset`**。
2. 将 **`quest`** 设为 **`Quest`**（**SerializeReference**），展开 **`phases`**；各 Phase 的 **Goals** 用 **+** 添加 **`QuestGoal`** 派生类型。
3. Inspector 底部可用 **新建默认 Quest**、**仅清除接取/完成状态（模板化）**。

### 10.2 运行时：从模板注册

1. 场景中挂 **`QuestConfigBootstrap`**，勾选 **`Register On Start`**。
2. 将 **`QuestTemplateAsset`** 拖入 **`Quest Templates`** 数组。
3. 启动时对每个模板调用 **`InstantiateRuntimeQuest()`**：内部 **`QuestDeepClone.CloneQuest`** 复制 **`Quest` / `QuestPhase` / 多态 `QuestGoal`**（按类型 **`Activator.CreateInstance`** + 反射拷贝非 **`[NonSerialized]`** 字段），再 **`QuestTemplateRuntime.ApplyTemplateRuntimeDefaults`**，最后 **`QuestManager.Register`**；同 **`questId`** 已存在则跳过。

```csharp
QuestConfigBootstrap.TryRegisterFromTemplate(myTemplateAsset, skipIfQuestIdExists: true);
```

**注意**：**`QuestGoal`** 派生类须有**公共无参构造函数**；引用类型字段为**浅拷贝**。任务配置勿依赖 **`UnityEngine.Object`** 引用（克隆不保证有意义）。

---

## 11. 注销任务

```csharp
if (QuestManager.Instance.TryGetQuest("main_intro", out var q))
    q.ReleasePubSubGoalListeners();

QuestManager.Instance.Unregister("main_intro");
```

---

## 12. 常见问题

1. **`Register` 抛错“需要 PersistenceModule”** — 场景中先有 **`PersistenceModule`**。
2. **`TryAdvancePhase` 一直 false** — 检查当前阶段所有 **`QuestGoal.IsCompleted`**。
3. **`TryComplete` 失败** — 必须最后一阶段且目标全完成。
4. **Goals 的 + 没有新类型** — 确认派生类 **`[Serializable]`**、**非抽象**、**公共无参构造函数**，且在 **`TypeCache`** 可见的程序集中。
5. **模板拖进 Bootstrap 仍不注册** — 检查 **`PersistenceModule`**、**`QuestManager`**、模板 **`questId`** 是否为空或与已有任务重复。
