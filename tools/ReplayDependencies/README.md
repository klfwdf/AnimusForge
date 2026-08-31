# Bannerlord 1.4 replay dependency boundary

用于 `PolicyGatewayReplayTests`、`WorldDiplomacyGatewayReplayTests`、
`TtsGatewayReplayTests`、`ProductionOptInEntryReplayTests`。四者共用此 targets，
不修改 `Program.cs`、生产代码、默认入口、官方构建/部署流程或游戏目录。
项目仍为 `net8.0`；metadata helper 使用系统 Windows PowerShell 5.1。

## 来源必须显式指定

| MSBuild property | 用途 |
| --- | --- |
| `GameRoot` | 实际游戏根，仅校验/记录；**不是**固定引用缺失时的 fallback |
| `Bannerlord14ReferencePath` | 已确认的固定 1.4 引用目录；只选其顶层 `TaleWorlds.*`、`SandBox*`、Newtonsoft/平台辅助 DLL |
| `ReplayHarmonyModulePath` | 精确 Harmony 模块根；验证 `SubModule.xml` ID，只选 `0Harmony`、`MonoMod.*`、`Mono.Cecil*` |
| `ReplayMcmModulePath` | 精确 MCM 模块根；验证模块 ID，只选 `MCMv5.dll` |
| `ReplayUiExtenderModulePath` | 精确 UIExtenderEx 模块根；验证模块 ID，只选 `Bannerlord.UIExtenderEx.dll` |
| `ReplayPrivateRuntimePath` | 明确的 AF 私有依赖目录；只选 managed `Microsoft.ML.OnnxRuntime.dll` |

模块路径可以位于 Workshop 或本机 Modules，但不接受“扫所有第三方模块再覆盖
同名 DLL”的策略。固定引用目录中即使有旧 MCM/Harmony，也不参与这些模块的选择。
不会复制任意 `AnimusForge.dll`、`System.*`、游戏 native DLL、ONNX 模型到输出。

`Program.cs` 仍加载项目本地固定的
`bin/Debug/single_module_stage/AnimusForge/bin/Win64_Shipping_Client/versions/1.4/AnimusForge.dll`。
先用既有官方流程单独准备 Stage；此框架只读取它，不构建、不修改 Stage。
自定义 runner 输出层级/RuntimeIdentifier 会被拒绝，避免验证与实际加载不同的 Stage。

## Fail-closed 与证据

- 缺参数、目录、模块 manifest、明确依赖或 Stage：报错，不跳过复制继续“成功”。
- metadata-only 检查 Stage 的直接非 Framework 引用；来源不明或完整程序集身份不符即失败。
- 复制前校验整个计划；输出已有不同 SHA256 同名 DLL 或递归子目录同名候选则失败。
  不自动覆盖或清理旧输出。检查报错中的具体文件后，另行审核清理该 runner 的本地 `bin`。
- 输出严格限制为当前 runner 的 `bin` 子目录；不允许与输入重叠或经过 reparse point。
- 成功输出 `af-replay-dependencies.json`，记录 Stage SHA256、引用路径、每项 owner、
  完整程序集身份和 SHA256。相同输入重复运行不重复复制，manifest 稳定。
- `System.*` 仍由 SDK/NuGet/.NET runtime 负责。这里证明的是这四个 replay 所需的
  managed 加载边界，**不是**全量游戏/native/ONNX 推理依赖闭包或真实游戏验收。

## 本机复现（2026-09-01）

在 `G:\AFMOD\AF-REFACTOR` 执行。路径是本机证据，不是机器无关默认值。
固定引用与当前安装版本可能不同；不得用二者相同的 `AssemblyVersion=1.0.0.0`
推断其字节或 API 相同。

```powershell
$dotnet = 'C:\Users\28358\AppData\Local\Microsoft\dotnet\dotnet.exe'
$env:DOTNET_CLI_HOME = 'G:\AFMOD\.build-cache\af-refactor-cli'
$env:NUGET_PACKAGES = 'G:\AFMOD\NEW-10\.dotnet_cli\.nuget\packages'
$properties = @(
    '-p:GameRoot=E:\steam\steamapps\common\Mount & Blade II Bannerlord'
    '-p:Bannerlord14ReferencePath=G:\AFMOD\NEW-10\.tmp\build_check\1.4'
    '-p:ReplayHarmonyModulePath=E:\steam\steamapps\workshop\content\261550\2859188632'
    '-p:ReplayMcmModulePath=E:\steam\steamapps\workshop\content\261550\2859238197'
    '-p:ReplayUiExtenderModulePath=E:\steam\steamapps\workshop\content\261550\2859222409'
    '-p:ReplayPrivateRuntimePath=G:\AFMOD\NEW-10\AnimusForge\bin\Win64_Shipping_Client'
)
foreach ($name in @('PolicyGatewayReplayTests', 'WorldDiplomacyGatewayReplayTests',
                    'TtsGatewayReplayTests', 'ProductionOptInEntryReplayTests')) {
    & $dotnet run --project ".\tools\$name\$name.csproj" @properties
    if ($LASTEXITCODE -ne 0) { throw "$name failed: $LASTEXITCODE" }
}
```

不要用 `--no-build` 绕过当前依赖验证后据此宣布新输入通过。需要改变引用/模块版本时，
先检查当前输出的 manifest 与报错，不要以扫描其他目录、降 target framework、
生成 stub DLL 或恢复旧的通配复制来消除错误。

## 框架自测

自测只向显式 ScratchDirectory 的唯一子目录写入，保留日志/fixture，不删除内容。
使用真实 DLL 副本；负向身份测试需要不同版本的真实 MCMv5，默认从固定引用目录取，
也可显式传 `-ConflictingMcmAssemblyPath`。不会生成伪造依赖。

```powershell
.\tools\ReplayDependencies\Test-ReplayDependencies.ps1 `
  -GameRoot 'E:\steam\steamapps\common\Mount & Blade II Bannerlord' `
  -ReferencePath 'G:\AFMOD\NEW-10\.tmp\build_check\1.4' `
  -HarmonyModulePath 'E:\steam\steamapps\workshop\content\261550\2859188632' `
  -McmModulePath 'E:\steam\steamapps\workshop\content\261550\2859238197' `
  -UiExtenderModulePath 'E:\steam\steamapps\workshop\content\261550\2859222409' `
  -PrivateRuntimePath 'G:\AFMOD\NEW-10\AnimusForge\bin\Win64_Shipping_Client' `
  -ScratchDirectory 'G:\AFMOD\.build-cache\af-framework-20260901'
```

覆盖：缺引用、错误模块 ID、输出越界、缺 DLL、程序集身份不符、空格/非 ASCII
路径、幂等复制、同名输出冲突不覆盖、递归同名候选拒绝。
各 replay 的测试断言由原 runner 负责；此框架不修改或掩盖它们的失败。
