# AF 整个重构项目收尾执行台账

## 当前授权与基线

用户已澄清目标是整个重构项目收尾，而不是检测文档收尾。本次开始实施缺陷修复、功能对照、有证明的旧路径清理和统一验收。自动化保持 PAUSED；不把文档完成、测试数量或构建通过当作项目 DONE。

- 工作区：`G:\AFMOD\AF-REFACTOR`。
- 起点：`6c98e6fc`；已审查生产基线 `35524b04`；已 fetch，共享远端无新增需要合并的提交。
- 原两份 2026-09-06 handoff/team-brief 占位草稿保持不改、不暂存；其他作者提交保留。
- 本地修改/回归/可逆提交可执行；推送、部署/真实存档操作、全局安装、新默认切换和广泛删除按明确范围另行确认。已向用户收集当前候选实机验收材料与部署许可。
- 原检测报告和原证据包不篡改；新红绿证据与最终状态另行绑定到实际修复提交。

## 连续实施顺序

| 范围 | 交付条件 | 状态 |
|---|---|---|
| F1 Hero asset ALL | 只转移指定资产，modifier/数量/异常/重复语义保持；真实方法红绿 | ACTIVE |
| F2/F3 Courier | 完整 prepared main 保真；权威后处理 completion/领域资格恢复；不重复捕获或提前提交；失败/取消/历史 owner 一致 | ACTIVE |
| F4/F5 Scene/BattleSpeech | 首次 await 前绑定请求；旧 scene/generation 不落地；frozen 目标不丢、旧回退不覆盖新输入；waiter ownership 验证 | ACTIVE |
| F6 TTS | 请求身份和取消贯穿 job/网络/成功失败/等待回调，旧事件不消费新轮 | ACTIVE |
| Bridge/目录/三个测试 | mandatory safety 与 optional bridge 分离；真实渠道状态；保留行为断言而非恢复旧布局/死 helper | TODO |
| 其余领域/Native/旧代码 | 对照原20领域清单，已替代且无静态/反射/外部/存档职责才删除；Native 等价门槛与残余 owner 缺口明确列出 | TODO |
| 统一验收 | 所有受影响正式测试/生产回放、Debug/Release 1.3/1.4/Bootstrap；失败必须归因；实机/旧档证据不可用 stub 替代 | TODO |
| 最终交付 | 准确 HANDOFF、制作组说明、回滚与发布条件；只有所有必要门槛满足才标项目完成 | TODO |

## 协作边界

根代理负责 Courier 与集成/公共台账/最终构建。独立工作按 Economy、Scene/BattleSpeech、TTS 分文件授权；不并行写同一文件或同一 Stage。TTS 如需 ShoutBehavior 接线，先交最小补丁建议，待 Scene 写入结束后由根代理整合。不碰 GCCZ 主体规则或其他工作树。

## 当前验收结论

执行刚开始，尚无本轮修复 PASS。六个已确认功能问题以原报告为红基线，真实 Host/旧档/live Economy/AFEF/TTS 尚未验收。所有最终状态必须由新增证据更新，不能继承旧 DONE 或将缺失证据伪造为完成。
