# B站弹幕输入桥

独立 Windows 桌面工具：连接直播间弹幕，支持关键词筛选；双击一条弹幕会将正文复制到剪贴板，再由玩家在游戏输入框按 `Ctrl+V` 粘贴。它不修改游戏文件，也不自动操作游戏窗口。

## 运行

双击 `dist/BilibiliDanmakuBridge.exe` 即可运行，无需单独安装 .NET。请保留 `dist/` 中的全部文件；需要 Windows 10/11 x64。

从源码运行：

```powershell
dotnet run --project .\BilibiliDanmakuBridge.csproj
```

重新生成自包含软件目录：

```powershell
dotnet publish .\BilibiliDanmakuBridge.csproj -c Release -r win-x64 --self-contained true -o .\dist
```

输入直播间号并连接。双击弹幕后切回已打开并聚焦的游戏输入框，按 `Ctrl+V`。

## 连接与边界

弹幕接收使用直播间 WebSocket 消息流。连接前会解析短房间号，并按当前接口要求获取 WBI 签名和 `buvid3` Cookie。连接失败会弹出服务端错误信息，便于诊断。B站接口和消息格式可能变化，直播间权限、平台策略或网络限制也可能导致连接失败。

UI 线程不承担网络读取；接收采用异步 WebSocket。消息列表最多保留 1000 条、界面最多显示 300 条，防止高流量时无界增长。
