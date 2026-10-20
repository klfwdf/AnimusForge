# AnimusForge Diplomacy Hotfix 0.1.0

为 AnimusForge v1.6.2 制作的独立外交补丁，需要已安装 AnimusForge 和 Bannerlord.Harmony。

## 修复内容

- 玩家外交事件维护时，先选取最多 8 个事件引用，再按原规则处理；事件关闭导致列表缩短时不会继续用旧长度读取列表，轮转游标也保持有界。
- 世界外交请求在主线程准备并冻结配置；异步请求及重试复用该配置，保留原 HTTP 请求预算、降级、重试和取消规则。

不替换 AnimusForge.dll，不修改存档格式，不新增配置选项。只在启动时安装 Harmony 补丁；事件快照沿用原游戏小时门控，不新增每帧反射或磁盘读取。

## 安装

1. 退出游戏，解压 ZIP，将 `AnimusForge.DiplomacyHotfix` 文件夹放进游戏的 `Modules` 目录。
2. 启动器勾选本子模组，顺序为 **Bannerlord.Harmony → AnimusForge → AnimusForge.DiplomacyHotfix**；其他模组顺序保持原设置。
3. 启动后查看 `Modules/AnimusForge.DiplomacyHotfix/Logs/DiplomacyHotfix.log`。

日志中的 `maintenance=applied;requests=applied` 表示两组补丁生效；`already_fixed` 表示检测到对应修复，跳过重复补丁；`unsupported_*` 或 `*_patch_rejected` 表示该组未应用，保留日志供排查。

CN/EN 包仅说明语言不同，DLL 完全相同，可用于 AF 的中文包或英文包；任选一份安装。

## 兼容与验证

已针对本机保留的 AF v1.6.2（文件版本 1.6.2.0）1.3/1.4 两份实现验证真实 DLL 补丁安装、重复安装及事件维护回放；已修复的两份实现会跳过。相同版本号下的其他构建按实际方法结构检测，不能仅凭版本号保证生效。

旧代码越界负例已复现，补丁后的两线各 84 项维护断言通过；旧请求源码加真实 Harmony 补丁的离线回放通过配置冻结、重试、预算、取消及配置缺失检查。游戏/MCM 和 HTTP sender 由测试替身提供，未访问真实服务。

同一 DLL 分别使用 Bannerlord 1.3.15 与 1.4.6 引用构建，产物字节一致。未完成游戏内验收、实际旧存档或 Bannerlord 1.4.8 实机验证。`build-info.json` 记录实际构建引用和哈希。该包只承载上述两项修复，不包含主项目其他外交改动。

## 卸载与回滚

退出游戏，取消勾选并删除 `Modules/AnimusForge.DiplomacyHotfix` 文件夹即可。原 AnimusForge 和存档文件保持原样；本包无需覆盖或恢复 AF 本体文件。

## 从源码构建

在仓库根目录执行（Python 3、.NET SDK；使用本机已安装的 Harmony）：

```powershell
python extensions/AnimusForge.DiplomacyHotfix/build.py --harmony "<Harmony目录>/0Harmony.dll" --package
```

可用 `--refs13` / `--refs14` 指定两线游戏引用目录。默认使用仓库 `_deps_auto` 和 `.tmp/build_check/1.4`。脚本核实引用版本，顺序构建两线并校验 DLL 一致，生成中英两份 ZIP；输出位于扩展目录的 `artifacts/packages`，不部署到游戏、不覆盖旧包。测试入口在 `tests/`；主项目原一键流程保持不变。
