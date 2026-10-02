# 政变胜利交互离线回归

运行：`python extensions/AnimusForge.Coup/tests/Coup.VictoryFlowTests/run.py`。

从当前生产源码提取 Campaign/Mission 的真实处置、离场、Tick、动画和战报回调，并直接编译当前 CoupSession、CoupOutcomeReport 与原版通知子类。测试输入均为确定性的引擎/UI替身，不改产品源码，不复制/逆变换旧版产品。

覆盖取消/Tab重开、代次与Mission重验、禁用/失效拘押、重复点击、异常关闭与地图恢复、动画先于战报、原版通知关闭回调、动画提交失败/通知UI缺失、已提交动画的恢复、旧完成存档、战报代次/重复确认以及不劫持其他城镇。

边界：政治动作提交由计数替身替代，不能当作真实王权/领地/拘押动作验收；只证明生产交互与生命周期调用顺序。真实政治提交实现保持既有幂等标记，由源码门禁及双API实际构建验证。真实原版动画素材、Gauntlet暂停/输入与实机遭遇返回须进游戏另验。

输出在项目 `artifacts/coup-victory-feedback-20261002/flow-harness`，不覆盖游戏、不启动部署。
