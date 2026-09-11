# AF 框架初版实施与验收（2026-09-11）

用户已授权按确认架构开始构建初版。本文是本轮实施检查点，不表示此前 21 项全部完成。

## 当前交接入口（自动化已暂停）

后续用户已授权框架 Skill/代码定位文档的单次 GitHub 交付；见根 HANDOFF 顶部和 `framework-skill-publish-progress-20260911.md`。以下“未推送”是运行代码批次当时状态，不禁止这次获准交付，也不授权恢复自动化。

Native 持久历史输入快照已提交 `8f1cd479`；新 memory 852 / Native 27、10 个新变异、既有定向回归、26 个旧变异、六项最终 Stage 与 16 组相关回归通过。详见 `docs/phase8/native-history-snapshot-progress-20260911.md` 和根 HANDOFF 最新段。

用户要求本轮完成后暂停，自动化 `af-7-8` 已确认为 PAUSED，等待新指示，不继续下一项。Api.V1 仍只读；快照只接 Native 这条历史入口，不是全局记忆线程安全，更不是实机或整个阶段 8 DONE。未推送、部署或操作真实存档。

## 范围
- 在同一个 AnimusForge.dll 中建立 internal 模块契约、显式登记和只读状态目录；不扫描 DLL、不新增全局功能开关。
- 政策、宴会、GCCZ 的选定实际接缝改走具名 typed 薄桥；业务实现、参数顺序、返回值、副作用和权威后处理责任保持不变。
- 对外建立独立 V1 API/只读 DTO，开放版本、框架装配状态与能力目录查询；不暴露游戏对象、执行器、密钥或可变内部集合。
- Native/Scene/Courier 程序化提交、动作写入、记忆写入、第三方注册本轮不开放。Native 原入口无统一排他和可靠取消；不能以缩减路径替代完整默认链。
- 补代码边界注释、内外接入技术 MD、外部编译示例、简明制作组 HANDOFF 及总 HANDOFF。

## 保持不动
默认入口、FeatureBridges.json 默认值、模块业务源码与存档身份、Bootstrap 和一键构建脚本、游戏安装、真实存档、远端分支、其他工作树及用户两份 2026-09-06 草稿。

## 验证
使用本地 SDK 与既有 Stage 构建流程；验证注册冲突/依赖/版本/未就绪/门禁、公共 DTO 隔离、真实调用接线和原回归。构建/离线 PASS 不等同于新增接口实机验收。

## 起点与回滚
实施前 HEAD: df6ab928，生产基线 9a4a26dc。本地 intent commit 后开始写代码。回滚用本轮提交的反向提交，不 reset、不清理用户改动。本轮不推送、不部署、不恢复自动化。

## 状态
初版实现与本地验证已完成；不是全部 21 项或整个重构项目 DONE。结果详见根目录 HANDOFF.md 与 docs/audits/2026-09-11-framework-v1-verification.md。

## 初版落地对应原清单

| 原编号 | 本轮状态 | 剩余 |
|---|---|---|
| A01 | 部分：31 个接缝的原新调用与参数可对照 | 主体所有功能逐项基线仍需继续 |
| A02-A09 | 沿用既有管线与修复，做相关回归；不宣称补完所有缺口 | Native admission、Courier prepare 线程、主体全面收敛等 |
| B01 | 初版：internal 显式目录、版本/依赖/冻结/状态查询 | 非动态插件注册，不承诺所有模块激活协议已完成 |
| B02-B03 | 部分：具名 typed 方法转接原规则/context/normalize/apply | 非通用贡献/Action 注册和完整新结果协议 |
| B04 | 本轮选定 13 方法、31 调用完成接线 | 模块其他旧接口仍保留，由原 owner 负责 |
| C01 | 首版只读 API 已实现 | 游戏态查询按需单独设计，不把目录 Ready 当 CampaignReady |
| C02-C05 | 未开放、不伪造成功 | 先补真实请求/结果边界，再按能力逐项扩充 |
| D01 | 本轮双实现构建、离线对照及外部契约验证 | 新接口实机、旧存档、子 MOD 加载/升级 |
| D02-D03 | 未执行 | 保留仍有责任的旧入口；默认切换单独确认 |

## 本机构建（仅 Stage）

```powershell
Set-Location -LiteralPath 'G:\AFMOD\AF-REFACTOR'
$env:DOTNET_ROOT = 'G:\AFMOD\.dotnet-sdk'
$env:PATH = $env:DOTNET_ROOT + ';' + $env:PATH
$env:DOTNET_CLI_HOME = 'G:\AFMOD\AF-REFACTOR\.tmp\dotnet-cli'
$env:NUGET_PACKAGES = 'G:\AFMOD\AF-REFACTOR\.tmp\nuget-packages'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE = 'false'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
# Debug / Release 分别执行；禁止并发构建两个实现。
powershell -NoProfile -ExecutionPolicy Bypass -File '.\一键编译覆盖推送\build_single_module.ps1' `
  -ProjectRoot 'G:\AFMOD\AF-REFACTOR' `
  -BannerlordRoot 'E:\steam\steamapps\common\Mount & Blade II Bannerlord' `
  -Bannerlord13ReferenceDir 'G:\AFMOD\NEW-10\_deps_auto' `
  -Bannerlord14ReferenceDir 'G:\AFMOD\NEW-10\.tmp\build_check\1.4' `
  -WorkshopContentDir 'E:\steam\steamapps\workshop\content\261550' `
  -RuntimeDependencyDir 'G:\AFMOD\NEW-10\AnimusForge\bin\Win64_Shipping_Client' `
  -Configuration Debug -Stage
```

SDK 8 足以完成本轮测试。历史 3 个 .NET 10 工具没有在本轮安装新 SDK 后重跑，不能写成全部工具通过。
