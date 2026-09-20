# 场景一对一展示接缝

- 默认状态：同一 AF owner 首次挂载后，下一 UI tick 通过原 `SwitchTalk` 打开 AI 输入；每个原 VM 仅初始化一次。手动切回原版选项后展示全部真实选项与原继续按钮，资源刷新不会重置这次选择。原版选项状态不显示发送蜡封。
- AI 状态：Enter 与右下方“发送”蜡封都调用原 `AnimusForgeNativeConversationOverlayVM.ExecuteSubmit`。蜡封绑定原 `IsInputEnabled`，等待时变暗且不可点击；空文本检查、提交去重、输入清理、后台 owner 与返回焦点仍由 AF 处理。输入区为蜡封预留底部空间，不覆盖文本。
- “更多”仅在原权限允许编辑个性或标签测试时出现；展开后各动作继续服从原可见性。切换对话模式、记录、给予或选择更多动作后收起，不改变权限。
- 记录、给予、绘图、更多圆按钮与发送蜡封使用浅色文字；下方完整标签使用羊皮纸上的深色文字。每个按钮使用独立 brush，不修改共享原版 brush。
- 绘图复用 Illustrator 已注入按钮的原 Click handler。未安装、没有注入或关闭此功能时，绘图槽及其标签隐藏；保留其他按钮位置，不生成无效绘图按钮、不调用第二条生图链。
- 鼠标命中只检查加载时缓存的实际按钮引用与运行时矩形，包含 `AFDialogueSubmit`；隐藏或禁用按钮不参与命中，无每帧控件树扫描。
- 包装 VM 释放时解绑通知；释放后的文本写入和所有命令均被忽略，晚到的 UI 事件不会再次转发给旧 owner。
- 右栏“离开”将请求排到下一 UI tick；排队后禁止重复提交或切换。执行前核对原 AF VM、原 Native VM、Mission 与 ConversationManager，先调用 AF 原 `CloseActive` 退休展示 generation，再复核 owner 并调用原 `EndConversation`。保留原 `ConversationEndOneShot`、任务后续与说服/会话卸载生命周期，不自行结束 Mission，也不把退出界面称为取消网络请求。旧会话的排队请求不作用于新会话，异常不自动重试已可能执行过的一次性回调。

此说明描述代码接线；图像、焦点、IME、滚动和点击命中仍以两版游戏实测为准。
