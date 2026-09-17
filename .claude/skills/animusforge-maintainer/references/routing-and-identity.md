# AF 身份、工作区与引用路由

## 身份证据

用户明确指定 AnimusForge/AFmod 的 Bannerlord 项目，或源码中 `AnimusForge.csproj` + `AnimusForge/SubModule.xml`（旧布局可在 ModuleData 下）+ Bootstrap 的组合是强证据。只有 AF/Forge 字样、泛 C#/Harmony/LLM 问题、其他骑砍 Mod 或 Minecraft Forge 不足以触发本 Skill。

显式 `$animusforge-maintainer` 可以选择规则，但不能确定未知副本的写入权限。独立维护 Skill 本身时核对用户选定的 Skill 路径即可，不要求它是一份 AF 源码仓库。

## 确认当前工作区

优先在用户指定的当前目录核实 Git 根、分支、HEAD、dirty 与 AF 标识，再读当前交接链接。路径/版本没有变化时复用本轮结果，不在每次编辑前扫描所有磁盘和 Git 根。

只有发现多个可能写入目标、发布目录/备份混淆或台账冲突时，才对相关候选比较 remote、修订、内容角色及用户请求。未确定目标保持只读，不擅自初始化仓库、切换分支或选择 ZIP 副本。其他机器的盘符和历史路径只作线索。

## 状态与权限

系统/宿主安全边界和当前授权优先。实际源码/Git/构建证据用于核实项目状态；当前台账记录决定，历史审查只作线索。矛盾时说明并更新当前入口，不能让 Skill 快照覆盖新实现或把旧交接当作发布授权。

## 路由工具

[suggest-reference-route.sh](../scripts/suggest-reference-route.sh) 接收项目目录和任务摘要，只输出阅读建议，不写项目。无身份时只建议核实；普通修复进入日常开发，内部抽取进入架构/工作包，历史债务仅在明确审计时读取。最终选择以真实影响面为准。
