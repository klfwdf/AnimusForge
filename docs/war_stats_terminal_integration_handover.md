# AnimusForge 终端内嵌战争统计（WarStats）工作交接文档

本文档供后续接手此项目的 Agent 或开发者参考，详细说明了战争统计（WarStats）内嵌到 AnimusForge 终端主面板所做的全部架构设计、源码修改、UI 适配、配置解耦以及双版本构建与部署细节。

---

## 1. 任务背景与核心目标

1. **功能内嵌**：将独立战役战争统计（原位于 `C:\Users\29310\Documents\骑砍2mod`）整合至 AnimusForge 终端面板（Terminal），作为终端第一页的第一个按钮 **【战争】** 的内嵌子面板，取代原有的二次跳转。
2. **快捷入口统一**：终端快捷键设为 `U` 键，同时保留大地图快捷按钮入口。
3. **MCM 选项解耦**：移除原战争统计在 MCM（Mod Configuration Menu）中的独立设置页，所有显示模式与排序直接在终端 UI 内部即时调整。
4. **布局美化与高度扩展**：
   - 彻底解决 Gauntlet XML 数据源与可见性绑定导致的面板黑屏问题；
   - 将子标签切换栏（当前战争 / 历史战争 / 清除所有）上移至原面包屑导航位置，替代冗余的 `终端 / 战争` 文本；
   - 整体提升战争数据面板高度（增加约 80px 纵向可视空间）；
   - 修复排序下拉菜单宽度不足导致选项文字截断（`最新战...`）的 Bug；
   - 在子标签栏右侧（用户标注红框区域）新增 **【单页滚动 / 多页分页】** 即时切换按钮，点击即生效。
5. **双版本兼容发布**：遵循双版本规范（Bannerlord 1.3.x 与 1.4.x），全过程通过单模块一键构建脚本发布至游戏目录。

---

## 2. 涉及的关键代码文件清单

| 文件路径 | 修改类型 | 核心改动说明 |
| :--- | :--- | :--- |
| [`AnimusForge/GUI/Prefabs/AnimusForgeTerminalPopup.xml`](file:///f:/AnimusForge-main/AnimusForge/GUI/Prefabs/AnimusForgeTerminalPopup.xml) | 核心修改 | 1. 拆分数据源与可见性容器；<br>2. 顶栏增设 `IsVisible="@IsBreadcrumbVisible"`；<br>3. 在原面包屑高度构建战争控制栏（居中显示子标签，右侧放置显示模式切换按钮）；<br>4. 战争卡片与主列表大幅上移（MarginTop 116 -> 72），主列表留 36px 避让翻页栏；<br>5. 排序下拉框宽度由 180px 扩至 230px。 |
| [`WarStats/AfWarStatsSettings.cs`](file:///f:/AnimusForge-main/WarStats/AfWarStatsSettings.cs) | 重构解耦 | 移除对 MCM `AttributeGlobalSettings` 的继承与所有 MCM 属性注解，改造为内存静态配置类，默认显示模式置为 `1`（单页滚动）。 |
| [`WarStats/AfWarStatsPopupVM.cs`](file:///f:/AnimusForge-main/WarStats/AfWarStatsPopupVM.cs) | 功能扩充 | 1. 增加 `DisplayModeButtonText` 状态属性；<br>2. 增加 `ExecuteToggleDisplayMode()` 交互方法；<br>3. 在 `RefreshContent()` 中自动驱动模式文本与名册刷新。 |
| [`WarStats/AfWarStatsTexts.cs`](file:///f:/AnimusForge-main/WarStats/AfWarStatsTexts.cs) | 本地化扩充 | 新增 `ModeScrollable` 与 `ModePaged` 本地化静态属性。 |
| [`AnimusForge/ModuleData/Languages/afwarstats_strings.xml`](file:///f:/AnimusForge-main/AnimusForge/ModuleData/Languages/afwarstats_strings.xml) | 语言资源 | 新增英文字符串 `AFWST_ModeScrollable` 与 `AFWST_ModePaged`。 |
| [`AnimusForge/ModuleData/Languages/CNs/afwarstats_strings-zh-CN.xml`](file:///f:/AnimusForge-main/AnimusForge/ModuleData/Languages/CNs/afwarstats_strings-zh-CN.xml) | 语言资源 | 新增中文字符串 `模式：单页滚动` 与 `模式：多页分页`。 |
| [`AnimusForgeTerminalUiModels.cs`](file:///f:/AnimusForge-main/AnimusForgeTerminalUiModels.cs) | 视图控制 | 增加 `IsBreadcrumbVisible` 属性，在处于 `TerminalViewMode.WarStats` 时返回 `false`，其他视图返回 `true`。 |

---

## 3. 核心机制与改动深度解析

### 3.1 解决 Gauntlet XML 黑屏根本原因（容器上下文隔离）
- **现象**：点击终端顶栏【战争】按钮后，中央主视窗区域纯黑无内容。
- **原因**：Gauntlet UI 遇到同一节点同时含有 `IsVisible="@IsWarStatsVisible"` 和 `DataSource="{WarStatsVm}"` 时，会将上下文直接下推至 `WarStatsVm`，导致父级发出的可见性属性变更无法被正确监听到。
- **解法**：分层嵌套：
  ```xml
  <Widget IsVisible="@IsWarStatsVisible" WidthSizePolicy="StretchToParent" HeightSizePolicy="StretchToParent">
    <Children>
      <Widget DataSource="{WarStatsVm}" WidthSizePolicy="StretchToParent" HeightSizePolicy="StretchToParent">
        ...
      </Widget>
    </Children>
  </Widget>
  ```

### 3.2 子标签上移与空间提拉（提升 80px 空间）
- 原结构中，子标签栏占据中央主视窗内部 44px 高度，且面包屑文字 `终端 / 战争` 冗余占用顶部空间。
- **优化后结构**：
  1. 在 `AnimusForgeTerminalUiModels.cs` 中添加 `IsBreadcrumbVisible`，战争视图下隐藏面包屑；
  2. 将 `[ 当前战争 ] [ 历史战争 ] [ 清除所有 ]` 与 `[ 模式切换按钮 ]` 提升到外层 `MarginTop="150"` 位置；
  3. 中央窗口内：3 张概览卡片移至 `MarginTop="2"`，主内容面板起始位置由 `116` 调整为 `72`；
  4. 列表与历史归档面板纵向可用空间扩充了约 80px，彻底摆脱拥挤感。

### 3.3 下拉菜单宽度修复
- 原 `Standard.DropdownWithHorizontalControl` 的 `SuggestedWidth` 和 `Parameter.CustomWidth` 为 180px，扣除内边距与下箭头后，仅剩约 145px，中文 6 个字（如“最新战争优先”）会被截断为“最新战...”。
- 调整为 `230px`，选项完整显示，中英文均无溢出截断。

### 3.4 彻底剥离 MCM
- MCM 的扫描机制是基于反射搜索 `BaseSettings` / `AttributeGlobalSettings<T>`。
- 将 `AfWarStatsSettings` 转为普通静态类：
  ```csharp
  public static class AfWarStatsSettings
  {
      private static int _currentSortMode = 0;
      private static int _historySortMode = 0;
      private static int _currentWarsDisplayMode = 1; // 默认单页滚动
      private static int _historyWarsDisplayMode = 1;

      internal static int GetCurrentSortMode() => _currentSortMode;
      internal static void SetCurrentSortMode(int selectedIndex) => _currentSortMode = selectedIndex;
      internal static int GetCurrentWarsDisplayMode() => _currentWarsDisplayMode;
      internal static void SetCurrentWarsDisplayMode(int selectedIndex) => _currentWarsDisplayMode = selectedIndex;
      internal static int GetHistorySortMode() => _historySortMode;
      internal static void SetHistorySortMode(int selectedIndex) => _historySortMode = selectedIndex;
      internal static int GetHistoryWarsDisplayMode() => _historyWarsDisplayMode;
      internal static void SetHistoryWarsDisplayMode(int selectedIndex) => _historyWarsDisplayMode = selectedIndex;
  }
  ```
- 结果：MCM 设置列表不再出现“战争统计”条目，纯净无干扰。

### 3.5 界面内即时【单页 / 多页模式】切换按钮
- **排版位置**：在子标签行右侧对齐系统 Tab（`HorizontalAlignment="Right" MarginRight="134"`），宽度 200px，完美匹配用户在截图中圈出的红框位置，同时左侧的 `[ 当前战争 ] [ 历史战争 ] [ 清除所有 ]` 依然保持居中。
- **状态联动**：
  ```csharp
  public void ExecuteToggleDisplayMode()
  {
      bool currentlyScrollable = CurrentTabSelected
          ? (AfWarStatsSettings.GetCurrentWarsDisplayMode() == 1)
          : (AfWarStatsSettings.GetHistoryWarsDisplayMode() == 1);

      int newMode = currentlyScrollable ? 0 : 1;
      AfWarStatsSettings.SetCurrentWarsDisplayMode(newMode);
      AfWarStatsSettings.SetHistoryWarsDisplayMode(newMode);

      RefreshContent();
  }
  ```
- **分页栏避让**：主内容面板设置 `MarginBottom="36"`，当处于多页模式（`DisplayMode == 0`）时，底部的 `ShowPaginationControls`（高度 32px）正好坐落在此避让区内，不会遮挡战争名册卡片。

---

## 4. Git 提交记录参考

当前工作位于分支：`refactor/prepare-af-restructure`

最近与此任务直接相关的提交：
1. `57f10cec`: `fix(terminal): remove war stats mcm, default to single page scroll, elevate subtabs and fix dropdown width`
2. `770067a6`: `feat(terminal): add display mode toggle button in war subtabs row`

---

## 5. 编译与部署指令

接手 Agent 如需再次修改代码并验证构建，**必须严格按照以下步骤执行**：

### 5.1 验证双版本 API 编译通过
```powershell
# 1. 验证 Bannerlord 1.4.x 编译
dotnet build AnimusForge.csproj -c Debug /p:BannerlordApi=1.4

# 2. 验证 Bannerlord 1.3.x 编译
dotnet build AnimusForge.csproj -c Debug /p:BannerlordApi=1.3 /p:Bannerlord13ReferencesVerified=true
```
*要求：必须 0 错误、0 警告。*

### 5.2 统一单模块一键部署至游戏
```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\一键编译覆盖推送\build_single_module.ps1 -ProjectRoot "F:\AnimusForge-main" -BannerlordRoot "F:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord" -Configuration "Debug" -Deploy
```
*该脚本会自动编译 1.3 DLL、1.4 DLL 与 Bootstrap，并将 Prefab、Languages、Data 及 DLL 统一热替换至 `Modules/AnimusForge`。*

---

## 6. 后续可能的扩展建议

1. **历史战争领主阵亡跳转**：
   - 目前点击历史战争右侧的阵亡领主名（`HistoryDeathsA` / `HistoryDeathsB`）已挂接原版百科跳转，若未来领主被斩首有特殊记忆交互，可由此处切入。
2. **快捷键自定义**：
   - 终端入口目前固定为 `U` 键（`InputKey.U`），如果后续用户希望在 MCM 中自定义终端唤起按键，可在终端全局设置中暴露按键绑定。
