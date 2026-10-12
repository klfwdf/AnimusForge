# EN 版部署验收（2026-10-12）

阶段：总结。**EN已部署，实机未验证。** 用户授权部署并选择EN，后明确“不用备份”；当前源码root/main/HEAD保持 `F:/AnimusForge-main` / `main` / `a911d4d0de8964a393b6bdc6eaa83705b5218b02`。部署目标 `F:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord\Modules\AnimusForge`，单模块 `v1.5.7`。Prompt和世界书EN，界面继续按游戏语言选中英，不改游戏界面设置。

## 实际变更与验证

- 已验Release产物复用：双API各0错误/363警告，Bootstrap0/0，当前9个产物SHA匹配原r4；原源码/DLL门禁通过，未屏蔽警告。十二资源不参与编译/嵌入，C#/项目/构建入口不变。
- 原完整EN投影第一轮失败10文件：9个GUI文件只中文注释，实际固定中文漏口是物品名。123注释英译后，所有非注释字节和XML树相同；物品仅name改Native TextObject键，ID `af_xihai_daimao_head`、mesh、0.8重量、100头甲和flags完整保留。既有sceneactions_strings增1个双语键，原全部键值保留；EN `Daimao`取自稳定资产ID/mesh的命名，CN `呆喵`保持。
- 两真实TW本地化库各632解析（English/中文/空French回退/English反切）PASS，原完整EN投影 3518 文件PASS。EN替换 3203（世界书 3131 / 其他 72）；80 语言XML留双语，标准文件名与canonical身份保留。旧extension/items.xml不在当前内容映射，是未部署的旧副本，未扩大本轮写入范围。
- 按用户新指令停止原备份准备，游戏尚未写入；旧Stage已在该指令前本地移位保留，后续不新增备份。任务内直接同卷原子替换 3353 文件，原入口与保护校验保留；未新增游戏Recovery。本地实测普通PowerShell null造成空路径，修为NullString.Value，两个宿主均通过；错误原日志保留。
- 安装 3518 个标准文件逐SHA一致，2561 个原未列入文件hash无变更；不删除未知/模型/用户文件。原119暂存、真实索引、main与一键/构建/部署/打包脚本均保持；无ZIP、push、外仓镜像或游戏启动。

实际二进制SHA：1.3 `5df6e1eb9e3b47f25b5832a664a2e918ff7f07a881d3870ecc1778bb0a2fb749`；1.4 `a9d3c07bf85c54d1950322a8629260ae15bce5a9941b32f99f099385e14fd2e6`；Bootstrap `547c77c84d79d9f0bfe434b5d419640911e2747d2d1f7d10a4404a7d7364f7ea`。单模块XML只宣告Bootstrap，两实现保原程序集名及标准版本路径。

## 证据与回滚

证据根 `artifacts/en-deploy-20261012/`：`preflight.json`、`resource-gap-repair.json`、`native-item-name-receipt.json`、`prepared.json`、`projection.json`、`direct-plan.json`、`direct-complete.json`、`deployment.json`、`verification.json`。命令r1由用户新指令停止，r2真实EN阻断保留，r3成功exit0。主台账[本条](../animusforge-refactoring-and-repository-reorganization-plan.md#en-deployment-20261012)；[三锚点职责](../architecture/af-framework-code-scope.md#en-deployment-20261012)。

源码checkpoint `5d5fbd53e0d2e7439fcc11121a3da0faa5176f46`，验证资源切片 `bb386814eea315adba5bd39c986f007e9064cb41`，本地分支 `codex/en-deploy-20261012`；十二文件focused [源码inverse](../../artifacts/en-deploy-20261012/resource-source-rollback.diff)已通过apply-check。**用户拒绝游戏部署备份，故本轮没有旧安装恢复副本**；direct-plan中的old hash不能恢复旧字节。只做源码聚焦逆提交，不hard reset或覆盖其他作者交付。完整实机UI/战役/LLM/旧档/全量语义及正式CN/EN分发ZIP仍未验。
