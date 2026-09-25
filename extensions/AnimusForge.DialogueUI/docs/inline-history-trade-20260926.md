# DialogueUI 内联历史与给予界面 — 2026-09-26

## 范围与实现

- 工作目录：F:/AnimusForge-main/extensions/AnimusForge.DialogueUI。基础 revision 为 1a5f8625；仅提交本轮扩展文件，保留主项目、Illustrator 与场景喊话的已有改动。
- AFDialogueNativeOverlay.xml 中接入已确认的 1280×610 辅助面板（屏幕顶部126、水平居中），使用 afdui_aux_panel.png；外围 alpha 透明，内部纹理保留。
- 顶部切换历史／给予展示、右上收起。打开时隐藏底部重复入口与输入，保留对白和肖像；收起不执行交易。
- 历史按目标读取宿主持久／会话记录，最多260条；沿用宿主每页50条与 RichText 链接，支持日期、对话／行动过滤、搜索与前后翻页。关闭释放宿主显示缓存。
- 资源按宿主菜单规则提供物品／第纳尔、部队、俘虏、固定资产；选择与数量直接在面板内完成。每页20项；右侧有数量输入、加减、全部、移除及估值合计。切换资源类别会清空本次选择。
- InlineTradeBridge 只在同步打开本次菜单时捕获精确宿主回调；资格、实际转移、展示事实与历史写入仍由 ShoutBehavior 执行。确认校验回调所有权、原资源对象、数量、重复项和 pending 身份顺序，防止过期提交。
- 输入适配覆盖普通对话模式、搜索／数量输入、百科暂挂恢复与对话结束清理；不再把焦点抢回已隐藏的对白编辑器。

## 性能

- 资源只在打开、切换类别、提交后重建；不逐帧扫描背包。搜索150ms合并输入。
- 反射成员只缓存一次；面板关闭时 Tick 立即返回，打开时只校验会话身份并处理待执行搜索。
- 动态按钮样式仅在列表结构变化后执行两次延后遍历，新按钮单次创建 brush；不逐帧扫描控件。
- 原10Hz肖像外观检查和相机取景逻辑保持。

## 已验证

- 现有 build.ps1：BannerlordApi=1.3／1.4 编译，零错误、零警告；宿主AF哈希保持。
- verify_auxiliary_contract.ps1：两个安装宿主的反射字段／方法，以及95处数据和命令绑定、109个控件与笔刷属性。
- auxiliary-tests：55项真实桥代码＋真实Harmony的.NET Framework 4.7.2托管测试；包含五种资源模式、取消、重复确认、数量边界、过期快照、回调更换、pending顺序变化、异常清理、无关弹窗和重入。夹具不是游戏资源转移验收。
- 现有 verify_presentation.py 与249项肖像断言通过；已安装1.4相机和地图肖像私有接口契约通过。
- 构建、部署脚本未修改。部署结果见 artifacts/deployment-clean-rebuild.json；逐文件哈希核对见 artifacts/auxiliary-deployment-verification.json。

## act_none 调查

- Illustrator 旧请求 20260925T210321_f95f040ac94d416aa9c4e2dc493063fe 的 trace.json 事件34–37，在保存与退休时实际 actionChannel0=act_none、configuredIdleAction为空、actionProgress0=0。北京时间约2026-09-26 05:03。
- 已存在提交379d9d51（05:28）在 NativeCharacterExportWidget 构造函数显式设置 act_inventory_idle_start，并关闭装备／自定义动画；安装 DLL 时间05:29:59。旧日志早于该修复。
- DialogueUI 的 LiveSpeakerPortrait 本来就显式使用该待机动作，本轮未改肖像动画。不能将旧导出日志归因于尚未部署的新辅助界面；也不能据此宣称新导出实机已修好。

## 尚需实机验收

- 两版本的真实 Gauntlet 渲染、分辨率缩放、字体、鼠标命中、滚动、普通／AI模式与百科返回焦点。
- 五种资源模式的实际资格与转移／展示及历史写入；关闭与取消不动资源、会话目标变化后旧选择失效。
- 安装待机初始化修复之后的新 Illustrator 导出截图及诊断；本轮不改 Illustrator。

## 回滚

- 改动前源码副本：artifacts/source-backups/aux-panel-20260926-055842（含当时已有修改，勿整目录覆盖其他人的变更）。
- 本轮独立提交可用 focused revert 撤销；不要 hard reset。
- 部署脚本自动保留 old-module，准确位置记录在 artifacts/deployment-clean-rebuild.json 的 Backup。
