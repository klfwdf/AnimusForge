# AnimusForge 羊皮纸对话界面（独立子模组）

模块 ID `AnimusForge_DialogueUI`，程序集 `AnimusForge.DialogueUI.dll`。依赖原 AnimusForge 和 Harmony；启动器将本模块排在 AF 后。可选 Illustrator 继续负责绘图。这里不包含、不重编、不替换 AF 主 DLL。

## 功能

- 场景喊话：羊皮卷多行输入、目标百科链接、交易摘要、按当前目标可见范围读取的记录。保留 Enter 发送、Shift+Enter 换行和6000字限制。发送后恢复场景，NPC继续头顶气泡和语音；“离开”取消输入。给予仍通过原 Y 菜单，首版不显示建议回复和喊话绘图。
- 场景一对一：人物真实肖像/姓名/可用纹章、NPC正文、右侧AI输入的三分区羊皮纸。新会话默认进入AI输入；可手动切换到全部原版选项。记录、给予/展示、可选插画、更多、发送和离开均调用已有机制。
- 大地图对话、信使、业务提示词、标签、动作和记忆结构不属于本模块。

## 构建与产物

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\extensions\AnimusForge.DialogueUI\build.ps1 -BannerlordRoot '<游戏目录>'
```

该命令只编译并暂存本模块，不运行离线测试、不部署。需要已安装1.4游戏和两份匹配的AF实现；1.3使用固定完整引用包 `Bannerlord.ReferenceAssemblies 1.3.15.110062`。两个目标顺序构建、分别保存，避免对象目录冲突。

- `artifacts/1.3/`、`artifacts/1.4/`：对应 DLL 与构建记录。
- `artifacts/stage/<API>/AnimusForge_DialogueUI/`：可安装目录。只选与游戏版本匹配的一份，不能同时加载两份。
- `artifacts/packages/`：本次独立模块 ZIP。包内不带 AF/TaleWorlds DLL、模型凭据、生成原图或测试工具。
- `work/`：静态设计预览与资源清单；`outputs/`：本地生图原始结果。这些预览不是游戏截图。

当前用户要求不运行离线测试；保留的 `tools/ContractProbe` 仅供之后另行授权的维护使用，不由构建脚本调用。实际界面布局、输入法/焦点、NPC语音、资源刷新与退出效果尚需实机验收。

## 之后整合

主模块未修改。UI资源、包装显示与兼容适配分开；接入位置、实际源码提交和未覆盖责任见本目录 `HANDOFF.md`。主模块重构完成后可由原 owner 加载资源/调用展示适配，移除独立加载补丁；不需要迁移存档或复制另一条AI链。

独立版与整合版只启用一个。未来整合方应先停用独立版入口，再接管相同资源，避免双重加载和重复按钮。

## 回滚

未部署时无需恢复游戏文件。安装后可退出游戏，在启动器取消勾选本子模组，原界面恢复；不新增存档状态。源码按本模块提交定向 revert，不能 hard reset 或带入其他作者改动。具体提交在本模块 HANDOFF 中列出。
