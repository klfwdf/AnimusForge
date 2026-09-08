# 给你的 GitHub 交付说明

## 这次交付了什么

代码提交 `9a4a26dc`，原验证/交接文档提交 `8fe2b40d`，加上本次发布说明。源代码、正式测试、HANDOFF 和制作组文案一起交付；本轮没有再改生产逻辑。

- 仓库：https://github.com/klfwdf/AnimusForge
- 独立重构分支：`codex/af-main-refactor-continuation-20260831`
- [打开此次交付](https://github.com/klfwdf/AnimusForge/tree/codex/af-main-refactor-continuation-20260831)
- 没有推送 `main`，没有覆盖游戏、实际存档、ONNX 或其他工作树，也没有恢复自动化。

## 为什么没有覆盖原共享分支

fetch 发现 `refactor/prepare-af-restructure` 被别人改写过：原 `aefa02ad` 变为 `03eb33f1`，历史不再是快进。最新那项改动已在我们的版本内，但对方删除其他历史的意图不能擅自猜测。因此先把完整、已验证的成果保存在独立重构分支，不 force-push，也不偷偷合回被移除的历史。

以后要合回原共享分支时，需要先确认制作组要保留的历史与功能；此次上传不是合并到原共享分支，也不是正式 Release。

## 本地文件在哪里

- 技术交接：`G:\AFMOD\AF-REFACTOR\docs\handoffs\2026-09-09-recovery-fixes-handoff.md`
- 可直接转发制作组：`G:\AFMOD\AF-REFACTOR\docs\handoffs\2026-09-09-recovery-fixes-team-brief.md`
- 本说明：`G:\AFMOD\AF-REFACTOR\docs\handoffs\2026-09-09-github-delivery-notes.md`

前两份文档可以从 GitHub 查看，也在本地保留。没有代你发送 QQ；需要你自行将制作组文案转发给成员。两份 2026-09-06 原草稿未修改、未混入本次提交。

## 已验证与不能误解的地方

- 1.3、1.4、Bootstrap 的 Debug/Release 六项构建通过。
- 37 个完整 C# 测试项目及新增专项回归通过；Policy 只执行四个安全子集。
- Python 26 个检查通过，缺实机证据样例按预期阻塞；三个 SDK 10 工具仍未通过当前环境编译。
- **整个项目尚未最终收尾。** Courier 前置准备线程边界、Native 完整等价接入、其余领域对照/旧代码清理，以及本候选真实游戏/旧档/经济/AFEF/语音验收还要继续。
- 真实日志、生成测试文件和构建 DLL 属本地忽略产物，没有随源码推送，不把源码提交冒充已经发布可玩的安装包。

旧交接中“全部切换/收尾/通过”的表述只代表当时记录；本次必须以最新 09-09 HANDOFF 的限制为准，不能沿用成当前验收结论。

## 可以直接告诉制作组的话

AF 重构修复候选已整理到独立分支 `codex/af-main-refactor-continuation-20260831`，代码提交为 `9a4a26dc`。信使、经济资产范围、场景输入与语音竞态等已修复，并通过双版本构建和离线回归。HANDOFF 与后续任务清单在分支内。本次是可追溯的开发成果交付，不是全项目最终发布；请按同一候选继续补真实游戏、旧档和实际副作用测试。原共享分支历史有变动，因此本次没有强推覆盖，请勿误从旧分支取版本。
