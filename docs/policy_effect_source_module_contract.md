# 政策效果源码模块合同

本合同只描述随 `AnimusForge` 一起编译的源码模块。它不是独立 DLL 插件协议，也不授权第三方程序集热插拔。

## 最小目录

```text
PolicySystem/Effects/Modules/<moduleId>/
  <PascalName>EffectModule.cs
AnimusForge/CustomPrompts/Policy/Effects/
  <moduleId>.json
```

`AnimusForge.csproj` 使用 SDK 默认源码包含规则，因此在上述目录新增 `.cs` 文件无需维护 `.csproj`、Catalog 或 UI 固定名单。

## 源码注册模板

优先复制一个与新效果具有相同 `executionKind`、`hook`、target 和 host operation 的现有模块，并只替换描述符、payload 与执行适配。模块文件本身必须包含 assembly registration：

```csharp
[assembly: global::AnimusForge.PolicyEffects.PolicyEffectModuleRegistration(
    typeof(global::AnimusForge.PolicyEffects.Modules.ExampleEffectModule))]

namespace AnimusForge.PolicyEffects.Modules;

internal sealed class ExamplePayload : NumericPolicyEffectPayload
{
}

internal sealed class ExampleEffectModule
    : NumericPolicyEffectModuleBase<ExamplePayload>, IModelModifierPolicyEffectModule
{
    private static readonly PolicyEffectModuleDescriptor ModuleDescriptor =
        new PolicyEffectModuleDescriptor(
            id: "exampleEffect",
            order: 999,
            legacyIds: System.Array.Empty<string>(),
            allowedScopes: new[] { PolicyEffectScopes.Kingdom },
            allowedSelectorKinds: new[] { PolicyEffectTargetKind.Settlement },
            targetKinds: new[] { PolicyEffectTargetKind.Town },
            cueTerms: new[] { "示例" },
            retrievalText: "用于候选召回的稳定语义文本。",
            catalogSummary: "不超过 60 字的能力摘要",
            mainInstruction: "主评议技术要求。",
            postprocessRule: "后处理 payload 与单位要求。",
            payloadPromptSchema: PolicyEffectPayloadSchemas.CreateNumericValueSchema(),
            family: PolicyEffectFamily.Economy,
            executionKind: PolicyEffectExecutionKind.ModelModifier,
            hook: PolicyEffectHook.SettlementProsperityDaily,
            aggregation: PolicyEffectAggregationKind.Additive,
            valueUnit: PolicyEffectValueUnit.PointsPerDay,
            fundingMode: PolicyEffectFundingMode.InheritPolicy,
            fundingStrategy: PolicyEffectFundingStrategy.Linear,
            payloadSchemaVersion: 1,
            playerDisplayName: "示例效果",
            editableUnderstandingPrompt: "文件缺失或损坏时使用的内置理解文本。",
            editableEvaluationPrompt: "文件缺失或损坏时使用的内置判定文本。");

    public override PolicyEffectModuleDescriptor Descriptor => ModuleDescriptor;

    public System.Collections.Generic.IReadOnlyList<PolicyEffectModelContribution>
        BuildModelContributions(PolicyEffectPreparedInstance preparedInstance)
        => PolicyEffectModuleRuntimeAdapters.BuildNumericModelContributions(this, preparedInstance);
}
```

若效果不是模型修正，必须实现与现有语义匹配的执行接口，例如 `IDailyPolicyEffectModule`、`IOneShotPolicyEffectModule`、`IScheduledOncePolicyEffectModule` 或 `IPolicyEffectCompositeModule`；不得为了接入新模块改变 Candidate、TargetPlan、`SourceModuleId` 谱系、payload schema、幂等、补偿或回滚合同。

`id` 与 `order` 必须全局唯一。Catalog 在程序集首次发现时拒绝重复注册或无效描述符；`promptVisible: false` 的运行时后代不会出现在提示词文件或动态 UI 中。

## 提示词文件

`AnimusForge/CustomPrompts/Policy/Effects/<moduleId>.json`：

```json
{
  "Version": 1,
  "ModuleId": "exampleEffect",
  "UnderstandingPrompt": "模型理解该效果直接因果的可编辑文本。",
  "EvaluationPrompt": "模型判定方向、单位和强度的可编辑文本。"
}
```

文件名不构成授权。运行时只读取 Catalog 中 `PromptVisible` 模块的 canonical ID 文件；`Version` 或 `ModuleId` 不匹配、文件缺失或损坏时，只回退该描述符的内置文本。未知文件保持 inert。

## 新增模块步骤

1. 复制相同执行类型的现有模块到 `PolicySystem/Effects/Modules/<moduleId>/`。
2. 设置唯一 `id`/`order`，声明 scope、selector、target、hook、payload、单位、资金、幂等/补偿/回滚能力，并在同一文件添加 assembly registration。
3. 复用现有 target 解析与 host operation；不要扩展既有跨模块执行边界。
4. 添加同名提示词 JSON；若模块隐藏，则不添加提示词文件。
5. 扩充 `tools/PolicyEffectModule.ContractTests` 的注册、作用域、payload、执行、回滚/补偿和授权用例。
6. 使用仓库统一流程分别构建 `BannerlordApi=1.3`、`BannerlordApi=1.4` 和 Bootstrap，再检查单模块 Stage。

## 性能合同

程序集注册与提示词文件创建发生在初始化阶段。提示词服务缓存每个已注册可见模块的独立槽位，最多每 5 秒低频检查这些预期文件的指纹；不枚举目录，不在模型构建或效果执行热路径做全量文件扫描。MCM 保存只原子替换当前模块文件并直接更新当前缓存槽。
