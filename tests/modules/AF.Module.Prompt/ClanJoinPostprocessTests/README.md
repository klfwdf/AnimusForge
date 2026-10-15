# 家族归附后处理 CN/EN 回归

```powershell
python tests/modules/AF.Module.Prompt/ClanJoinPostprocessTests/run.py --out artifacts/clan-join-postprocess/replay
```

可选 `--token-log <本地日志路径>` 回放 2026-10-15 的 16:50:23、16:51:38 两次已确认失败输入。原始日志及生成的上下文仅留在忽略的 artifacts 目录，不提交玩家材料。

从当前生产源码原样提取王国识别、资格判断、规则构建、标签表渲染和标签归一化方法，链接生产 `PostprocessRuleEntry`，读取真实 CN/EN 规则及实体文案资源。游戏对象与配置载入边界使用替身，不调用远程模型或执行游戏动作。

覆盖中英文及混合标题、LF/CRLF、末尾无换行、相邻实体区隔离、旧 typed ID、去重、现有王国/灭亡/未知目标、族长/玩家家族/玩家队伍限制、个人入队独立开关和具体标签白名单。旧正则负控必须复现英文清单缺少归附规则。

通过仅代表“实体清单→可用规则→标签表→具体标签归一化”在上述条件下正确，不代替真实模型输出、主线程执行、存档或三渠道完整实机验收。生产资源改动另由原统一入口验证双版本 DLL 与 Bootstrap。
