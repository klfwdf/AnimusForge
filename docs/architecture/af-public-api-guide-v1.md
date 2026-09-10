# 独立子 MOD 公共 API V1（初版，只读）

## 当前可以做什么

只依赖 `AnimusForge.Api.V1`：

```csharp
using AnimusForge.Api.V1;

AfCapabilityInfo catalog = AfApi.GetCapability(AfCapabilityIds.CatalogRead, 1);
if (catalog.State == AfCapabilityState.Available)
{
    AfFrameworkSnapshot snapshot = AfApi.GetSnapshot();
    // snapshot.State 是框架装配状态，不是 Campaign/Mission 可提交状态。
    foreach (AfModuleInfo module in snapshot.Modules)
    {
        foreach (AfModuleCapabilityInfo capability in module.Capabilities)
        {
            // 展示接缝状态即可；IsExternallyCallable 在 V1 恒为 false。
            string line = module.Id + ": " + capability.State;
        }
    }
}

AfCapabilityInfo native = AfApi.GetCapability(AfCapabilityIds.NativeSubmit, 1);
// 初版明确 NotSupported；不存在可偷偷转发旧 ForExternal 的 Submit 方法。
```

## 能力清单

| ID 常量 | V1 状态 | 解释 |
|---|---|---|
| CatalogRead | Available | 查询版本、框架装配与只读接缝目录 |
| NativeSubmit / SceneSubmit / CourierSubmit | NotSupported | 尚未提供可靠、完整的公共请求边界 |
| ActionExecute / MemoryWrite | NotSupported | 子 MOD 不能借目录直接写动作、事实或记忆 |
| ExtensionRegister | NotSupported | 首版不允许第三方动态注册 provider/handler |

未知 ID 返回 `UnknownCapability`；版本非 1 返回 `VersionMismatch`；空白/超过 128 字符 ID 返回 `InvalidRequest`。非法 ID 校验优先；ID 精确区分大小写，不自动 trim 或别名匹配。

## 线程、生命周期与隐私

- 两个查询方法可在任意线程调用，不访问游戏实体、发起 LLM 或执行游戏动作。
- 查询本身不会启动 AF。模块加载前为 `NotInitialized`；装配完成为 `Ready`；目录初始化失败为 `Degraded`；卸载后为 `Stopped`。
- `Ready` 不代表有活动游戏，`CatalogRead` 即使启动前仍能返回一个合法的未初始化快照。
- 快照为只读拷贝；历史快照不随着后续关桥/卸载而变化，需要时重新查询。
- 不暴露配置路径、凭据、Prompt、内部 handler、Hero/Agent/Mission、存档字段或任意回调调度。
- 同进程 MOD 不是安全沙箱。public/internal 分层用于契约约束与可维护性，不承诺防御有反射能力的恶意 DLL。

## 引用和加载

- 继续用一个 `AnimusForge.dll`，API 随 1.3/1.4 两份实现同源码编译。
- 子 MOD 构建引用所支持游戏线的 AF 实现，避免把 AF 实现复制进子 MOD 的发行目录（引用设 `Private=false`）。
- 运行时必须先由 AF Bootstrap 选择并加载适配当前游戏版本的唯一实现，再调用 API。不能直接加载另一版本 DLL 来“获取接口”。
- 初版测试包含外部程序集编译和隔离 Host 契约；真实独立子 MOD 的 Bannerlord 加载顺序/二进制升级仍需实机验证。
- 现有其他 public 类型/旧 ForExternal 方法为历史兼容表面，不在本 V1 承诺范围内；本轮未一刀切改成 internal，以免破坏既有子 MOD。

## 版本规则

V1 标识的是这次小型只读表面。发布前按外部编译用例冻结；后续增加方法/字段必须保持既有签名与含义。不要给已发布接口方法加必填参数或重新解释状态；不兼容变更另开版本。能力探测不能替代对 AF 依赖版本/加载是否存在的正常检查。
