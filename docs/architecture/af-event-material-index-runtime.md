# 事件素材索引：真实职责提取

## 所有权

- `MyBehavior`：实际事件来源、人物/王国/日历、正文处理、记录列表与存档、权威追加/更新和发布。
- `EventSourceMaterialIndex<T>`：派生索引构建、原重复键策略、source/map/count/List 结构绑定。没有 TaleWorlds、网络、存档或跨模块副作用。
- 私有 `EventSourceMaterialEntry` 保持原嵌套类型和字段。没有为了可见性移动持久模型或开放 public API。

这不是把同一个 MyBehavior partial 换文件名：新运行时类掌握派生状态与规则；MyBehavior 通过窄方法组合实际调用。它也不是完整 MemoryService，原记录和存档责任未声称已迁移。

## 真实顺序

```text
RecordEventSourceMaterial
  → component.IsCurrent(source, map)
  → 必要时 component.Build(source)，得到未发布的完整 map
  → MyBehavior 再验 source 引用，发布 map，然后 component.Bind
  → 按原索引键更新记录，或向权威 list 追加并插入 map
  → list + map 两步成功后才重绑，失败不冒充成功
```

原 load 和派生索引重建仍调用 MyBehavior 的窄 Rebuild 入口；不存在第二份记录库或第二个事实提交者。原 foreach 对列表结构改动仍能抛错；Build 抛错时旧 map 保留，已实际 append 的记录也不假装回滚。

## 保持的规则与限制

- 命名键大小写不敏感、相同键最后一条胜出。
- 非负日期空键沿用原 fallback 的第一条匹配；负日期命名键仍用原 key builder 的日期归一规则。
- payload 更新的日期、次序、world/kingdom 累积标志不变；人物识别/文本规则仍归主体 owner。
- 同数替换、Clear 后重填、索引替换、列表替换以及枚举探针耗尽后再变动，均使索引失效。
- 一次有效结构绑定后，新增不存在的键不再全历史 fallback。2000 条历史 + 50 个新键的真实谓词访问保持 0。
- 仅承诺现有已审计串行 writers；不承诺任意后台并发或对所有嵌套 Day/StableKey 任意写入的监控。
- API 不向外暴露派生状态，不改变原 SyncData/程序集身份；读档/实机仍须独立验收。

## 验证与清理

23 个实际生产方法场景与 c21523f8 提取前实现相同；62abfdb3 原实现的 8 个缺陷/成本反例仍有效，七种同义 fault 未减少。测试编译采用显式输入列表，避免旧生成 Index.cs 混入。

删除旧 MyBehavior 索引 partial、主类的四个派生绑定字段及两个旧 private 绑定/检查方法；派生字段进入新组件。保留原 Record/Rebuild 门面是因为 load、正常写入仍真实调用它们。

完整验收、源码提交与一基坐标见[本轮 HANDOFF](../handoffs/2026-09-14-b1-index-owner-integration-handoff.md)。本页不代表主体大类已全部拆薄。
