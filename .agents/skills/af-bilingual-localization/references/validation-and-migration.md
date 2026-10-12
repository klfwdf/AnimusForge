# 验证与接管

## 按变更选择证据

| 变更面 | 必要验证 |
| --- | --- |
| UI/设置/prefab | 两语言真实 consumer/native 解析、英文 fallback、变量字面性；核对 DataSource 作用域、宽度和最终可见按钮/行 |
| Prompt/世界书 | 实际包 selector/loader，源资产未改写，旧默认/用户覆盖优先级、真实发送 messages；UI 切换不改变包语料 |
| 兼容词/事实/历史 | 旧值、顺序、字段与失败状态守恒；UI/Prompt 投影执行，保存对象及协议身份不变 |
| C#/嵌入/API/构建闭包 | 原统一入口的两 API 与 Bootstrap、实际引用来源/marker/DLL hashes；编译符号不证明引用版本 |
| 纯非嵌入 XML/资产 | 解析、键/变量、原 ID/非文本结构与实际 consumer；只有未变编译闭包和 DLL 的哈希证据才可复用旧构建 |
| 正式双包/部署 | 完整 Stage CN/EN 投影，全部资产、路径、DLL 闭包、UI 两表；正式 ZIP 命名及每包单模块、失败回滚、源/Stage 保全 |
| skill/纯说明 | frontmatter、链接/入口、指南一致性与实际使用场景；不启动无关产品构建 |

复用有效既有测试和冻结输入，失败或新的具体风险才补测试/重跑。保留业务条件、拒绝路径、数值、目标、权限、执行及回滚断言；旧中文断言可精确迁移为资源/选版断言，但不能删业务覆盖或虚构 public API。

验证语言切换用当前支持的 native 流程，例如 EN→CN→无该翻译的语言 fallback→EN；验证局部桩时明确它没覆盖实际 Campaign、Gauntlet 渲染或 provider。静态语法、管道最后退出 0、键存在或测试数量不能代替真实消费结果。

使用实际现存工具和参数，不编造新统一入口：`tools/localization/inventory_runtime_literals.py` 负责盘点，`scripts/build/package_localization.ps1` 提供源码/DLL/内容投影门禁，`scripts/build/build_single_module.ps1` 为统一构建，`LocalizedPackageContractTests.ps1` 与 `PackageVersionContractTests.ps1` 分别覆盖双包和版本事务。先读当前参数/调用者，再调用；专项通过不等于完整发行或实机通过。

## 全量盘点

盘点冻结实际 Git 根、分支、HEAD、dirty、扫描脚本版本、输入哈希和报告路径。保护旧报告，不把旧条数当实时数量；扫描汉字只是分类输入。

| 状态 | 含义 |
| --- | --- |
| `verified` | reader、真实消费者和资源/选版已核对，有专项证据；不重派同一范围 |
| `open_gap` | 已定位生产符号、消费者和缺失行为，可形成精确修复任务 |
| `triage_candidate` | 词法/资源命中，未证明进入运行输出；只读追踪 |
| `mainline_only` / `unknown` | 共享接线待主线，或没有可信交付；不宣称外包已开工 |

`Assert-AnimusForgeEnglishRuntimeSource` 的截断阻断/`at least N` 不是全仓剩余数。兼容词、标签、正则、原事实允许保值，但必须有用途、reader、消费者与守恒证据；不能把活跃 Prompt 放入兼容目录就称已译。dormant helper 只列候选。

按真实生产分支与语义模板计事实/Prompt，不按编号区间或资源键数量虚增工作量。记录写入→读取组装→角色/状态→包语言；已覆盖 Courier/Gathering/Memory 投影不重复列缺口。映射、词法、结构、语义完成度分别报告。

## 接管与并行

- 接管 stash/他人代码先核对基准、当前 HEAD 和真实 hunk，增量合入；不整体 checkout/pop 或把一处坏修改当成整份成果无效。保原值逆恢复证明仅适用于迁移，不能证明 EN 翻译质量。
- 先检查交付目录、delivery/coverage、before/changed-files 和当前 diff，再比较写入路径与 owner。Agent 自报、任务单和聊天摘要不是交付证据；没有明确 open_gap 不整模块重翻。
- 若发现既有交付残留，登记 `file:line:symbol → consumer → missing projection`；新任务先列已经完成且禁止重做的范围，再列逐项缺口。共享映射、主 csproj、语言注册、包 selector 由主线统一合并。
- 只有用户或适用指令授权才并行；该 skill 的内部子任务使用 `gpt-6.1-sol`，不可用时如实说明，不静默换模型/启用另一会话模式。明确独占文件，真实交叉 owner 协调；外部仍施工时按已确认的只读/写入边界处理。

## 完成与保全

正式打包使用原一键入口/`package_mod.ps1`：两包同版本、同时间戳、同 DLL，完整双包通过后才发布；名称遵循 SKILL/AGENTS。本机 `MMdd_HHmm` 精度为分钟，同名尾号两包一致，旧文件不可覆盖。保原版本递增/FileVersion/失败恢复事务、Bootstrap-only XML、DLL allowlist、无 ONNX 与单模块约束。

不使用 Unicode/Base64 隐藏汉字，不删规则、吞异常、扩大标签权限、关闭门禁或豁免整文件制造通过。发现 EN 包加载旧中文覆盖、AFEF 原值进入 UI、文字绑定丢失等实际运行证据时，继续追 consumer 并修，不以源码清零结案。

最终记录改动、owner/消费者、命令结果、源码/产物哈希、已验和未验、focused 回滚位置；实际状态登记唯一主台账和 HANDOFF。区分离线、正式双包、部署、实机、旧档与真实 provider，维护规范不冒充产品完成。部署/发布/镜像仍按当前授权，不从历史 HANDOFF 获取新授权。
