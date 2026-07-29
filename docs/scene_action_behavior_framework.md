# AF 场景动作行为框架

更新日期：2026-07-29

## 本轮范围

该框架直接属于 AnimusForge，不再通过外置 Harmony 子模组反射 AF 私有方法。

当前只接入 AF **场景喊话**：

```text
玩家确认喊话
  -> AF 建立并验证框选目标快照
  -> AF 正常记录玩家消息
  -> SceneActionIntentResolver 识别精确动作指令
  -> SceneActionBehavior 保存当前 Mission 与 Agent 引用
  -> ShoutMissionBehavior 在游戏主线程按时播放动作
  -> AF 原有 NPC 思考、回复、TTS、历史和后处理继续运行
```

动作逻辑不短路 `ProcessShoutConfirmedInternal(...)`，也不替代 AF 的 LLM 对话链。

## 运行证据

参考包：

- `D:\qq\AnimusForge_XihaiAction_1_3.7z`
- SHA-256：`DE1272FF04871BEB80E602726D5E94E6F79DCFBCC588D68355B8BB6998ADFB17`

包内 2026-07-08 的实测日志确认以下调用在 Bannerlord 1.3 场景中成功：

| 语义 | 动作 | Channel | Blend-in |
| --- | --- | ---: | ---: |
| 跪下并保持 | `act_main_story_conspirator_kneel_down_1_continue` | 1 | 0.35 秒 |
| 起身 | `act_stand_up_floor_1` | 1 | 0.35 秒 |
| 西海自定义动作 | `act_af_xihai` | 1 | 0.18 秒 |

旧子模组源码还证明 `ActionIndexCache.Create(...)` + `Agent.SetActionChannel(...)` 是实际播放边界。新框架保留该边界，但移除了：

- 对多个 AF 私有异步方法的 Harmony 反射补丁。
- 播放时按 `AgentIndex` 扫描全场 Agent。
- 对普通自然语言做宽松动作关键词回退。
- 独立子模组自己的静态 mission 队列。

## 当前指令

| 输入 | 目标 | 动作 |
| --- | --- | --- |
| `跪下`、`给我跪下`、`你们都给我跪下` | 当前框选 NPC | 持续跪姿 |
| `起身`、`站起来`、`起来` | 当前框选 NPC | 起身 |
| `我跪下`、`我自己跪下` | 玩家 | 持续跪姿 |
| `我起身`、`我站起来` | 玩家 | 起身 |
| `*西海` | 当前框选 NPC | `act_af_xihai` |
| `*act_动作名` | 当前框选 NPC | 指定动作 |

只有完整、直接的命令才会触发；`我解释跪下是什么意思`、`不许跪下`、`西海是什么` 均不会触发。

`*西海` 只表示强制动作控制。当前提交没有把归档里的 `pack0.tpac` 或动作注册 XML 合入统一 AF 模块；因此实际播放 `act_af_xihai` 仍要求游戏已加载对应动作资源。原版跪下/起身不依赖该资源包。

## 调度与性能

- 队列保存 `Mission` 和 `Agent` 引用，执行时重新检查 mission 身份及 Agent 活性，避免 AgentIndex 复用和每次全场扫描。
- 待执行项按时间和序号二分插入。
- 空队列 tick 只做一次原子计数读取。
- 每 tick 最多处理 8 个到期动作，防止异常批量请求长时间占用主线程。
- 4 人及以上框选时相邻动作错峰 0.1 秒。
- 任务开始、结束或 mission 身份变化时清空队列。

## 扩展边界

后续接 NPC 回复或模糊自然语言动作判定时，应新增一个窄的严格控制协议入口：

```text
PLAY_ACTION <动作键或 act_* 动作名>
NONE
```

判定器只把这个控制结果交给 `SceneActionBehavior`；`NONE` 和普通自然语言都必须停止，不得再做关键词回退。动作目录、目标选择和主线程播放仍由现有框架统一负责。本轮没有保留尚无运行时调用者的控制结果 helper。

新增 Blender/Modding Kit 动作时，只需：

1. 在已加载模块中注册 `action_types.xml`、`action_sets.xml` 与 TPAC 动画资源。
2. 在 `SceneActionBehavior.BuildDefinitions()` 增加动作键、实际 `act_*` 名和 blend-in。
3. 在 `SceneActionIntentResolver` 增加明确别名，或由严格 `PLAY_ACTION` 结果直接选择动作。
4. 先测单人，再测 4 人错峰；动作通道、骨骼覆盖和循环属性仍以资源本身为准。
