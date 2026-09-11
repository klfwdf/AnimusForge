# AF 框架 Skill 与重构分支交付（2026-09-11）

## 实施前检查点

用户授权本轮编写框架/要求 Skill、在 HANDOFF 标注代码位置、明确主体功能可演进，并推送 GitHub 重构分支；指定 Native history 简明 HANDOFF 只留本地。自动化不恢复，游戏不部署。

起点 `37c43417`，生产代码 `8f1cd479`；fetch 后 origin 重构分支为 `a58c2191`，本地领先 38 / 落后 0。被排除文档仅在未推送的 `37c43417` 中新增，因此不能直接推本地 HEAD，也不能用先删后推泄露历史。

计划：仓库内 `.agents/skills/af-core-framework/` 存可复用要求，根 AGENTS 路由；总 HANDOFF 提供符号/行号/源码版本与未覆盖边界。运行生产代码不改、不整片搬旧文件。保留本地来源分支历史，以 `8f1cd479` 为父提交创建不含指定文档的干净交付分支，普通快进推送到既有远端重构分支，不改主分支、不强推。远端变化或测试所需历史缺失时停止发布。

## 已完成的本地验证

- 仓库 Skill 位于 `.agents/skills/af-core-framework/`，根 AGENTS 路由；未全局安装。Skill 明确允许批准的主体功能演进，保持分层/兼容，不把 Api.V1 当前只读或旧测试行为写成永久限制。
- 新可上传交接：`docs/handoffs/2026-09-11-framework-skill-github-handoff.md`；总 HANDOFF 已更新；25 个代码坐标在 `docs/architecture/af-framework-code-map.json` 与范围图。
- bundled quick_validate 通过；坐标校验对记录提交和当前工作树均通过；3 个错误坐标/符号/摘要反例被拒绝；文档链接/UTF-8/diff 检查通过。
- C#、项目/解决方案、ModuleData 和一键脚本相对 `8f1cd479` 没有变化，不重复运行全部游戏构建。之前六项 Stage 是上一批运行代码的证据，不是本轮新增实机验证。
- 本地验证日志：`.tmp/framework-skill-delivery-20260911/validation-results.json`。两份用户草稿哈希保持；指定简明 HANDOFF 留本地，交付时同时排除其文件和引入它的本地祖先。
- 干净交付分支拟为 `codex/af-framework-skill-delivery-20260911`，远端仍是原 GitHub 重构分支；确切推送结果必须以远端 ref 核实后记录。自动化仍 PAUSED，不部署游戏。

## GitHub 交付已核实

- 框架/代码/Skill/可上传文档已普通快进推到 `origin/codex/af-main-refactor-continuation-20260831`：`a58c2191 → 38c003ab`，`git ls-remote` 确認目标提交。此后若补交接记录，分支末端为该记录提交，以实际 ref 为准。
- `38c003ab` 父提交为已验证源码 `8f1cd479`；保留此前全部测试对照祖先。与本地来源 `669fbedd` 的完整树差异仅为排除指定简明 HANDOFF；源分支历史原样保留。
- 指定文档在交付树和新增远端提交历史均不存在，对应 blob 也不在待推对象可达集合中；原文件本地仍在且字节未变。两份 2026-09-06 草稿字节未变且未加入提交。
- 在干净交付分支再次运行 Skill 格式、25 点当前坐标、Team ports 308/3、Memory snapshot 852、Native history 27 检查均通过。原源码 `8f1cd479` 没有改变，未新增游戏部署/实机测试。
- 当前本地分支是 `codex/af-framework-skill-delivery-20260911`，跟踪原远端重构分支；后续推送须显式指定获准 ref，不从原本地来源分支推送或合并排除文档历史。
- 自动化 `af-7-8` 仍 PAUSED。交付回执留本地 `.tmp/framework-skill-delivery-20260911/first-push-receipt.json`。
