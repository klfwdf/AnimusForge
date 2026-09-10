# AF 主体 / 内部模块 / 子 MOD：初版框架

> 2026-09-11。这是已经开始接线的初版，不是整个重构项目 DONE，也不是完整 SDK 发布。

## 一张图看懂

```text
AnimusForge.dll（Bootstrap 按游戏版本只加载一份）
│
├─ AF 主体
│  └─ Native / Scene / Courier → 原有 Prompt、LLM、权威后处理、动作、记忆、展示
│
├─ Refactor/Modules：制作组内部边界
│  ├─ TeamModuleServices → IPolicyModulePort   → 既有政策业务
│  ├─ TeamModuleServices → IGatheringModulePort → 既有宴会业务
│  ├─ TeamModuleServices → ISiegeModulePort     → AfGcczShoutBridge → 既有 GCCZ 业务
│  └─ ModuleFrameworkRuntime + InternalModuleDirectory：显式装配及状态目录
│
└─ Api/V1：子 MOD 的公开边界
   └─ AfApi.GetSnapshot / GetCapability → 同一个内部目录的只读投影
      ↑
   独立子 MOD DLL
```

**不需要子 MOD 依赖所有制作组模块。** 子 MOD 依赖 AF 的公共 API；未来需要某模块能力时，再经公共 API 的受控转接访问。制作组模块在同 DLL 内用 internal 契约，不需要绕公共 SDK 调自己。两层最终共享主体 owner，不能各造一条 LLM 或动作提交路径。

## 这次具体改变

- 新建有版本的内部定义、能力目录及专用 typed ports；模块显式登记，不反射扫描 DLL。
- 在主体的四个 owner 文件中，把选定政策/宴会/GCCZ 的直接调用改为薄桥调用；参数、返回值、ref/out、调用顺序和既有 gate 原样保留。
- 同类型场景 partial 接缝同步接入，不只改 Native 或 Courier 一条链。
- 新建 public V1，只发布版本、框架状态、公共能力探测和内部接缝只读目录。
- 在 SubModule 加载/卸载中更新框架目录状态；不新增 Tick/后台服务。

## 谁负责什么

| 区域 | 责任 | 不应该做 |
|---|---|---|
| AF 主体 | 捕获请求、选择话题、生成、后处理、调度、动作/记忆提交、展示 | 不复制政策/宴会/GCCZ 业务 |
| internal ports/adapters | 把主体参数准确送到既有 owner | 不吞参数、不补发一次动作/记忆、不另写规则 |
| 制作组模块 | 自身资格、业务状态、数值和结果 | 不绕主体生成第二条同类 LLM 流程 |
| 公共 V1 API | 稳定、有限、可探测的外部边界 | 不暴露 Hero/Agent、可变 owner、执行器、密钥或任意函数调度 |
| 独立子 MOD | 只依赖文档明确承诺的公共契约 | 不把 internal 类型或旧 ForExternal 方法当稳定 SDK |

## 容易误解的四件事

1. `FrameworkState.Ready` = 首版目录和适配器已装配，**不是** Campaign 正在运行、NPC 有资格、业务完整迁移或实机测试 PASS。
2. 内部 capability `Available` = 当前目录与已声明 FeatureBridge 门禁允许该接缝；仍须原请求 owner 检查会话、目标、generation 和线程。`IsExternallyCallable` 在首版恒为 false。
3. 目录不是强制拦截全部历史调用的防火墙。原有兼容入口依然存在；本轮不为它们增加一个会改变玩法的新总开关。
4. 当前公共 `CatalogRead` 可用；三渠道程序化提交、动作执行、记忆写入、第三方注册都返回 `NotSupported`。目录可查询不等于这些功能已开放。

## 初版没有做

- 不改额外模块业务、数值、玩法状态机和业务存档。
- 不切 Native 默认、不删仍有调用/存档/外部兼容责任的旧入口。
- 不改 FeatureBridges.json 默认值，不把 `policy-world-diplomacy` 当整个政策模块开关。
- 不新建 Contracts DLL，不改变程序集名、Bootstrap 或单模块双实现布局。
- 不覆盖游戏、不读写用户真实存档、不安装 SDK、不恢复自动化、不推送。

## 下一步顺序

1. 补 Native admission：与现有 UI 共用排他、主线程绑定会话，明确请求取消/完成语义后再开放普通文本提交。
2. 修 Courier 双向早期准备线程边界；先捕获游戏状态，后台完成网络/纯计算，再由主线程 owner 完成。
3. 按功能对照验证 Scene 接力、旁听、记忆/事实唯一提交；逐步把完整主体入口收敛，而不是只换类型名。
4. 增加经过批准的公共结果/生命周期通知、模块能力转接与扩展注册；每个能力单独定义权限和失败语义。
5. 新接口在 1.3/1.4 游戏内及旧存档验证后，再评估默认迁移与删除可证明无用的旧路径。

技术说明：`docs/architecture/af-internal-module-guide-v1.md`、`docs/architecture/af-public-api-guide-v1.md`。
本轮状态与验证入口：根目录 `HANDOFF.md`。
