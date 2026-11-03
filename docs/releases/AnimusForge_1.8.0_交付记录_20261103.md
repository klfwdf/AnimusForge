# AnimusForge 1.8.0 交付记录（2026-11-03）

BUILT / PACKAGED / DEPLOYED / SOURCE_PUSHED / USER_SKIPPED_VALIDATION。

本记录替代此前人设与索引修复条目的“未构建、未部署、未推送”交付状态。旧日志和00:33旧DLL的成功建库证据仍属于旧构建，不是本修复实机验收。

## 修复与提交

- eae418f4bc：关闭扩展人设后隐藏展示与编辑入口、停止注入扩展上下文；关闭时成功重生成清理该人物旧扩展字段，保留正文、语音与经历状态；中英说明同步。
- 379d24a12c：首次正常对话即使没有实体也发布规则快照并沿原链路建立索引；未发布的null快照不能提交为同版本就绪空库。保留检索开关、按版本缓存、批次16、线程上限4及失败冷却，无新增Tick扫描/IO。
- 65bb09ee45：从仓库当前树移除旧bekko的config.json、model.onnx、tokenizer.json，并移除对应Git LFS路径和忽略例外。本地原始文件、游戏内量化模型与向量缓存保留；Git历史及服务端历史LFS对象没有清除。
- 上述提交及此前获授权的版本/资源瘦身/极限7z入口提交已推送origin/main，源代码推送返回51d5d94f43..379d24a12c。

## 构建与包体

版本保持1.8.0，AssemblyFileVersion为1.8.0.0；既有程序集身份不变。API1.3与API1.4均0错误、各406警告；Bootstrap为0错误、0警告。构建使用当时完整工作区，包括其他作者已有未提交改动；本次Git提交只收纳本任务文件，不替其他作者提交无关dirty。

编译时默认源码glob误收work下辅助Probe.cs导致重复定义，随后仅在任务目录使用BuildInputs.targets排除work/**/*.cs，重新编译成功；未修改生产项目或一键入口。

| 内容包 | 文件 | 大小 |
| --- | --- | ---: |
| CN | 一键编译覆盖推送/packages/AnimusForge_v1.8.0_CN_1103_0049.7z | 179,795,800字节，171.47MiB |
| EN | 一键编译覆盖推送/packages/AnimusForge_v1.8.0_EN_1103_0049.7z | 179,335,952字节，171.03MiB |

两包均含Bootstrap、1.3/1.4实现和124,972,807字节的量化bekko模型及配套tokenizer/config；沿用此前世界书选择、PDB/Lite世界书排除与图片瘦身范围，不包含运行时向量缓存。采用既有共享编码器：LZMA、512MiB字典、fb273、bt4、mc1000000、lc4/lp2/pb0、极限9、solid/mqs、2线程、不存时间戳。两个编码进程均退出0后发布文件，旧包保留。没有归档回读或校验。

## 游戏部署

目标：F:/SteamLibrary/steamapps/common/Mount & Blade II Bannerlord/Modules/AnimusForge。

部署进程退出0，435个受管理文件写入完成，涵盖新双实现、Bootstrap、配套标记、模块XML及CN共享运行资源。复用现有脚本路径/reparse保护，按同卷候选逐文件替换；未修改常规一键流程。第一次因PowerShell将空备份参数转换为非法空路径，在0文件替换时停止；改用NullString后完成。

不覆盖玩家配置、CustomPrompts、PlayerExports、日志、缓存、ONNX或既有FeatureBridges/TerminalSettings文件；未生成安装Recovery、备份、manifest或自动回滚。安装目录操作只发生在用户明确授权后。

## 证据、未验项与回滚

本机任务目录：.tmp/package-180-persona-index-20261103-004209/。
其中delivery.json、build-results.json、编译/编码日志、deployment-result.json保存实际结果。发行包没有上传为GitHub Release附件。

按用户会话要求未运行测试、离线模型实验、验证门禁、归档回读、哈希验证或修复后游戏接受；编译和文件部署成功不等于首次对话实机已通过。

人设修改前检查点a8f2e9d8ee，原字节快照.tmp/expanded-persona-disable-20261102/before/；索引修改前检查点4a5cde59d及.tmp/first-dialogue-knowledge-index-20261103/before/、change.patch；旧模型移除前检查点7844a6d42f。源码按本任务提交做聚焦逆向修改；不覆盖共享大文件或其他作者dirty。游戏部署没有自动回滚，需用选定的旧构建人工重新部署。
