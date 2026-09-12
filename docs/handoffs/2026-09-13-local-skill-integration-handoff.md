# AF 两套 Skill 本地融合与冲突核对（2026-09-13）

## 本轮范围 / ACTIVE 检查点

用户要求列表介绍 ZIP Skill、与已有框架 Skill 核对冲突并融入本地。本轮只更新仓库内 `.claude/skills/animusforge-maintainer/` 的维护 Skill、`.agents/skills/af-core-framework/` 的协调条款，以及根 AGENTS/HANDOFF 入口；不安装全局、不改生产、不推送、不恢复自动化。

- 本地起点：`e40c92d72524b7ea80a5dc0e36dc996963a62e66`；工作区 `G:\AFMOD\AF-REFACTOR`，分支 `codex/af-framework-skill-delivery-20260911`。
- 已抓取远端：`bd2ed35fef0b68eb9c9b56e6376d1594ecf6f0a8`。前轮只 fetch，未合并这 4 个远端提交；本次不顺带更新运行源码。
- 输入：`D:\qq\af-skill (1).zip`，上游 Skill `animusforge-maintainer` 版本 `0.1.1`，ZIP SHA256 `ac342dc52157bd3c9402dcd0e1967d75174a2a3e30edd20998e19a1b1040d163`。原 ZIP 只读保留，不执行安装脚本。
- 原本地维护 Skill 已受 Git 跟踪；本检查点保存融合前状态。两份 2026-09-06 用户草稿和仅本地简明 HANDOFF 保持原样。
- 验证：Skill YAML/格式、链接、附带脚本语法与只读校验、路由场景、协调条款审读，以及生产源码和用户文件未变化。宿主新会话实际发现不作未执行的保证。
