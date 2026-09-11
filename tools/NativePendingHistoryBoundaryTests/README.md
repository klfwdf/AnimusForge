# Native 前置历史 / 拒绝清理回归

```powershell
# 在 G:\AFMOD\AF-REFACTOR 中运行
$env:PYTHONIOENCODING = 'utf-8'
python -B tools/NativePendingHistoryBoundaryTests/run.py --original
python -B tools/NativePendingHistoryBoundaryTests/run.py
python -B tools/NativePendingHistoryBoundaryTests/run_mutations.py
```

使用本地 SDK 8、项目 .tmp/dotnet-cli 和 .tmp/nuget-packages；生成物在本工具 .generated，不部署游戏。

## 实际执行

提取真实 Native 的前置历史 block 和全部五个拒绝分支，链接实际 pending history 队列边界；执行真实 append、snapshot、clone、message renderer、pending AFEF consume、Native/Scene rollback 及 admission/context 检查。数据模型使用原 AnimusForgeDialogueHistoryEntry、ConversationMessage。

游戏对象、key/显示名/距离 provider、场景 append bridge 和窗口配置来源为 fixture；实际集合操作、窗口取样、角色/目标字段投影与清理代码不复制替代实现。Fixture 将 Native 返回空文本的“未捕获”分支映射为 null capture 对象，保留同一判定，不把该空值当作真实最终回复验收。

## 原反例

5847a195 的实际代码已重现：
1. 后台读取显示名/距离、写 tentative history。
2. party key 改变后，旧 cleanup 没删原 Native 行，却删了场景镜像。
3. 读档切换 owner、复用 event sequence 后，旧清理误删新存档的 Native/Scene 行。

## 当前覆盖

111 检查：direct/queued、普通/主动无玩家输入、AFEF 一次消费、真实 user/assistant/system 映射、默认玩家名、原窗口与事实保留、固定 key、五个拒绝分支、换代/换 owner/会话/revision 的清理拒绝、事实保护、重复 callback、未开始超时不晚执行、发布失败、异常/诊断隔离、真实线程中已开始操作跨期限仍等待真实结果。

12 行为变异：丢准入、忽略 append/read 捕获键、丢清理 context、重算 cleanup key、删除 player/user 过滤、丢 claim、过期仍执行、丢弃已开始结果、失败发布仍执行、诊断异常逃逸。必须有运行时 FAIL，编译错误不算被捕获。

## 保留与限制

三个私有 helper 新增 capturedHistoryKey 可选参数；逆变换这一个参数和传递/默认表达式后，完整声明须等于旧版本。已有调用不传此参数时规则完全保留；新 Native 路径捕获一次并明确传入。

只验证本段前置历史与指定拒绝/丢弃清理；更早人设/规则/持久记忆准备、通用 bool timeout helper 的其他调用、TTS 直接回调、实际场景 mirror、真实存读档都仍需后续证据。不是整体 Native 全链游戏验收，也不是持久化 receipt。
