# 快报 / 王国档案回归

从仓库根目录运行，`--out` 必须指定尚不存在的仓库内目录：

```powershell
python -B tests/modules/AF.Module.Weekly/BulletinArchiveTests/run.py --out artifacts/bulletin-archive-<new-run>
```

- 直接链接生产档案策略、导入/规范化、完整终端 VM、持久化 adapter 和 UTF-8 分块存储。
- 从实际文件抽取原样 DTO、Upsert、旧布局关联快照、浏览器 projection、世界消息去重与过滤合并方法；时间线常量也从源码读取。记录源文件 SHA-256。
- 检验单份快报在多个王国可见、超过三个国家及小消息国家、缓存裁剪、旧字段缺失/null、存档读回与 JSON 导入、期数/排序、默认/显式选择、国家点击切换、空档案及时间线去重。
- XML 检验仅为已有绑定和标签可用宽度契约，不是视觉验收。真实 controller 接线另外由 `EditorLifecycleTests` 检验。
- 原样抽取请求完成/主线程发布、NPC 快照和外交历史投影；验证同一期正文/`BulletinAnecdote`/本地事实摘要分流、缺少 `[SHORT]`、请求失败/异常、旧字段缺失、多块重载与归档导入。提示词 JSON/layered reader 为实际生产方法，检验旧默认精确迁移、自定义/清空、损坏保留与资源默认一致。
- `Stubs.cs` 替代游戏对象、日志、渲染/设置、文字清理、无关外交迁移，以及本轮未变的区域聚合和插图布局边界；HTTP 为合成返回。发布、NPC 快照、外交投影及保存/读取为生产源行为回放。未运行真实 Campaign、网络、Gauntlet 加载、NPC 模型转述或玩家旧存档。
