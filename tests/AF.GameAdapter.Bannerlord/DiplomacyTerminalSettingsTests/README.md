# 外交终端设置清理回放

运行 `python tests/AF.GameAdapter.Bannerlord/DiplomacyTerminalSettingsTests/run.py`。

直接使用本地引用中的真实 MCM `JsonSettingsFormat`、属性发现器和设置 JSON converter，链接当前生产源码的两个篇幅属性及默认常量。验证旧 JSON 含六个已删除字段仍可加载，既有篇幅和其他有效配置保持，保存不再写废弃项，缺失值采用100—300默认。检查终端实际注册、setter/range、英文fallback与中英文资源一致，并通过真实 TaleWorlds `TextObject` 加载与切换两种 UI 语言，解析本次相关文案。

测试设置宿主为脱离游戏的最小替身，不启动完整 MCM provider、游戏或写玩家配置。输出在新的仓内目录；需要已有 MCM/SDK引用，不下载依赖。
