# 压缩输入切片验证（2026-09-12）

状态：`OFFLINE_COMPLETE / LIVE_SAVE_PENDING`。工作区为 `F:\AnimusForge-main`，基线生产源码 `9040d184`，本轮 intent/checkpoint `15c58d59`。精确生产提交和后续台账见根 HANDOFF。

| 验证层 | 本轮结果 |
|---|---|
| 旧源码反例 | 精确读取 `9040d184` 的三种 live 输入及缺失来源接受检查，按预期 exit 1；静态证据 |
| 新输入边界 | net8 / net472 各 137 项 PASS；后者使用项目 `_deps_auto` 原 Json.NET |
| 六个行为变异 | 首版 121 项 fixture 中分别触发 13 / 4 / 2 / 10 / 7 / 13 项失败；之后增加三类实际 worker 覆盖至 137，生产代码不变 |
| 全文件源码对照 | 12 个具名声明定向反转后与 `9040d184:MyBehavior.cs` 完全一致；prompt builders、Apply/Mark 和其他 owner 源码未变 |
| 历史召回 / Native | 852 / 27 PASS |
| 失败提示 / 主线程完成 | 85 / 17 PASS |
| 记忆恢复 / 周报结果回执 | 原合同程序 PASS |
| 同 DLL 制作组端口 | 308 PASS；3 个旧变异按预期拒绝 |
| 存档身份 | 相对 `e40c92d7`，SyncData 146/146、CampaignBehavior 36/36，模块 ID/Name 均 AnimusForge、只加载 Bootstrap |
| 现有单模块流程 | Debug/Release × 1.3/1.4/Bootstrap 六构建及两套项目内 Stage PASS，0 warning / 0 error |
| 实际 DLL | 四实现共 532 项元数据断言 PASS；外部引用 internal 按预期 CS0122；公开 V1/旧 memory 签名不变 |
| LIVE / provider / SAVE | 未运行；尚无实机帧耗时、真实网络和旧存档接受证据 |

四份实现 DLL SHA256：

- Debug 1.3：`53a74ee51d835cd970fd2850c491269b77bb075c0d2eb03e81629966756af1e8`
- Debug 1.4：`9f9fdc80929b7534069793af43dcabfce2c1b2fe133f06198c368c05651d786d`
- Release 1.3：`dffcb134ab9f37a75887ec0d40cc92245d2135d124fd038d1182697cf7ea67fe`
- Release 1.4：`aaf538621cb8459d0bcfe1340487054cbd6005be8203b7da2639bc52d31268e7`

本地原始日志：`.tmp/goal-20260912/memory-*.log`、`memory-persistence.json`。当前 reference 文件实际标记为游戏 1.3.15 / 1.4.6；构建沿用仓库 `BannerlordApi=1.3/1.4` 的双实现流程。

环境说明：系统默认 SDK 10.0.400；测试单独使用已安装的 8.0.421 和现有 net8 pack。net472 fixture 只读定位已还原引用程序集，不改变全局 SDK、不安装依赖或覆盖游戏。初次编译的 StringReader/Writer 命名冲突已明确限定 .NET 类型修复。net8 不直接加载依赖 System.Security.Permissions 的旧 Framework Json.NET，另用 net472 + 原 DLL 验证了真实目标运行时。

边界：网络、游戏对象和写入副作用为 fixture；本片不声称全项目线程安全。EngineTick 保留两个排队动作上限，但没有实测单动作最坏耗时。Native 其他读取、TTS、Courier、其他 memory writer 和实机验收仍按本轮目标继续。
