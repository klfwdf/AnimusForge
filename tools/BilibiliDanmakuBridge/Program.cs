using System.IO.Compression;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Windows.Forms;

namespace BilibiliDanmakuBridge;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}

internal sealed record Danmaku(string User, string Text, DateTime ReceivedAt)
{
    public override string ToString() => $"[{ReceivedAt:HH:mm:ss}] {User}: {Text}";
}

internal sealed class MainForm : Form
{
    private readonly TextBox _room = new() { PlaceholderText = "直播间号", Width = 120 };
    private readonly TextBox _filter = new() { PlaceholderText = "关键词过滤（可空）", Width = 190 };
    private readonly Button _connect = new() { Text = "连接", AutoSize = true };
    private readonly Button _pause = new() { Text = "暂停显示", AutoSize = true, Enabled = false };
    private readonly ListBox _messages = new() { Dock = DockStyle.Fill, IntegralHeight = false };
    private readonly Label _status = new() { Text = "未连接", AutoSize = true, Padding = new Padding(8, 7, 0, 0) };
    private readonly System.Windows.Forms.Timer _filterTimer = new() { Interval = 250 };
    private readonly List<Danmaku> _allMessages = new();
    private CancellationTokenSource? _connectionCts;
    private bool _paused;

    public MainForm()
    {
        Text = "B站弹幕输入桥";
        Width = 780;
        Height = 560;
        MinimumSize = new System.Drawing.Size(560, 360);
        StartPosition = FormStartPosition.CenterScreen;

        var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 42, Padding = new Padding(8, 6, 8, 4), WrapContents = false };
        top.Controls.AddRange([_room, _connect, _filter, _pause, _status]);
        Controls.Add(_messages);
        Controls.Add(top);
        _connect.Click += async (_, _) => await ToggleConnectionAsync();
        _pause.Click += (_, _) => { _paused = !_paused; _pause.Text = _paused ? "继续显示" : "暂停显示"; };
        _filter.TextChanged += (_, _) => RenderMessages();
        _filterTimer.Tick += (_, _) => RenderMessages();
        _filterTimer.Start();
        _messages.DoubleClick += (_, _) => PasteSelected();
        _messages.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { PasteSelected(); e.Handled = true; } };
        FormClosing += (_, _) => _connectionCts?.Cancel();
    }

    private async Task ToggleConnectionAsync()
    {
        if (_connectionCts is not null)
        {
            _connectionCts.Cancel();
            return;
        }
        if (!int.TryParse(_room.Text.Trim(), out int roomId) || roomId <= 0)
        {
            MessageBox.Show(this, "请输入有效的直播间号。", "输入有误", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _connectionCts = new CancellationTokenSource();
        _connect.Text = "断开";
        _pause.Enabled = true;
        SetStatus("正在连接…");
        bool cancelled = false;
        try
        {
            await BilibiliLiveClient.RunAsync(roomId, AddDanmaku, SetStatus, _connectionCts.Token);
        }
        catch (OperationCanceledException) { cancelled = true; }
        catch (Exception ex)
        {
            SetStatus("连接失败");
            MessageBox.Show(this, ex.Message, "B站连接失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _connectionCts.Dispose();
            _connectionCts = null;
            _connect.Text = "连接";
            _pause.Enabled = false;
            if (!IsDisposed && cancelled) SetStatus("已断开");
        }
    }

    private void AddDanmaku(Danmaku message)
    {
        if (IsDisposed || _paused) return;
        if (InvokeRequired) { BeginInvoke(() => AddDanmaku(message)); return; }
        _allMessages.Add(message);
        if (_allMessages.Count > 1000) _allMessages.RemoveRange(0, _allMessages.Count - 1000);
        RenderMessages();
    }

    private void RenderMessages()
    {
        if (IsDisposed || _paused) return;
        string filter = _filter.Text.Trim();
        var visible = _allMessages.Where(x => filter.Length == 0 || x.Text.Contains(filter, StringComparison.OrdinalIgnoreCase)).TakeLast(300).ToArray();
        _messages.BeginUpdate();
        _messages.Items.Clear();
        _messages.Items.AddRange(visible.Cast<object>().ToArray());
        if (_messages.Items.Count > 0) _messages.TopIndex = _messages.Items.Count - 1;
        _messages.EndUpdate();
    }

    private void PasteSelected()
    {
        if (_messages.SelectedItem is not Danmaku item) return;
        try
        {
            Clipboard.SetText(item.Text);
            _status.Text = "已复制；切回游戏输入框后按 Ctrl+V";
            _status.ForeColor = System.Drawing.Color.DarkGreen;
        }
        catch (Exception ex) { SetStatus("剪贴板失败：" + ex.Message); }
    }

    private void SetStatus(string text)
    {
        if (IsDisposed) return;
        if (InvokeRequired) { BeginInvoke(() => SetStatus(text)); return; }
        _status.Text = text;
    }
}

internal static class BilibiliLiveClient
{
    private static readonly Uri DanmakuInfoUri = new("https://api.live.bilibili.com/xlive/web-room/v1/index/getDanmuInfo");
    private static readonly Uri RoomInitUri = new("https://api.live.bilibili.com/room/v1/Room/room_init");
    private static readonly Uri NavUri = new("https://api.bilibili.com/x/web-interface/nav");
    private static readonly int[] MixinKeyEncTab = [46, 47, 18, 2, 53, 8, 23, 32, 15, 50, 10, 31, 58, 3, 45, 35, 27, 43, 5, 49, 33, 9, 42, 19, 29, 28, 14, 39, 12, 38, 41, 13, 37, 48, 7, 16, 24, 55, 40, 61, 26, 17, 0, 1, 60, 51, 30, 4, 22, 25, 54, 21, 56, 59, 6, 63, 57, 62, 11, 36, 20, 34, 44, 52];

    public static async Task RunAsync(int roomId, Action<Danmaku> onDanmaku, Action<string> onStatus, CancellationToken cancellationToken)
    {
        onStatus("解析直播间…");
        var cookies = new System.Net.CookieContainer();
        using var handler = new HttpClientHandler { CookieContainer = cookies, AutomaticDecompression = System.Net.DecompressionMethods.All };
        using var http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(15) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36");
        http.DefaultRequestHeaders.Referrer = new Uri("https://www.bilibili.com/");

        using JsonDocument fingerprint = JsonDocument.Parse(await http.GetStringAsync("https://api.bilibili.com/x/frontend/finger/spi", cancellationToken));
        JsonElement fingerprintData = fingerprint.RootElement.GetProperty("data");
        string buvid3 = fingerprintData.GetProperty("b_3").GetString() ?? throw new InvalidDataException("B站没有返回设备标识。");
        string buvid4 = fingerprintData.GetProperty("b_4").GetString() ?? "";
        cookies.Add(new System.Net.Cookie("buvid3", buvid3, "/", ".bilibili.com"));
        if (buvid4.Length > 0) cookies.Add(new System.Net.Cookie("buvid4", buvid4, "/", ".bilibili.com"));

        using JsonDocument roomInfo = JsonDocument.Parse(await http.GetStringAsync($"{RoomInitUri}?id={roomId}", cancellationToken));
        int realRoomId = roomInfo.RootElement.GetProperty("data").GetProperty("room_id").GetInt32();
        if (realRoomId <= 0) throw new InvalidDataException("B站没有返回有效房间号，请检查房间号是否正确。");

        onStatus("获取弹幕连接授权…");
        using JsonDocument nav = JsonDocument.Parse(await http.GetStringAsync(NavUri, cancellationToken));
        JsonElement wbi = nav.RootElement.GetProperty("data").GetProperty("wbi_img");
        string imgKey = KeyFromUrl(wbi.GetProperty("img_url").GetString());
        string subKey = KeyFromUrl(wbi.GetProperty("sub_url").GetString());
        string mixinKey = string.Concat(MixinKeyEncTab.Select(i => (imgKey + subKey)[i]));
        long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var signedParams = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["id"] = realRoomId.ToString(), ["type"] = "0", ["web_location"] = "444.8", ["wts"] = timestamp.ToString()
        };
        string query = string.Join("&", signedParams.Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(Regex.Replace(kv.Value, "[!'()*]", ""))}"));
        string signature = Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(query + mixinKey))).ToLowerInvariant();
        string infoUrl = $"{DanmakuInfoUri}?{query}&w_rid={signature}";
        using JsonDocument info = JsonDocument.Parse(await http.GetStringAsync(infoUrl, cancellationToken));
        JsonElement root = info.RootElement;
        int apiCode = root.TryGetProperty("code", out JsonElement code) ? code.GetInt32() : -1;
        if (apiCode != 0)
        {
            string message = root.TryGetProperty("message", out JsonElement apiMessage) ? apiMessage.ToString() : "未知错误";
            if (apiCode == -352)
                throw new InvalidDataException("B站风控拒绝了弹幕授权请求（-352）。请等待几分钟后再试；连续快速重试可能延长限制。若持续出现，请检查本机网络环境或登录 B站后重试。");
            throw new InvalidDataException($"B站弹幕授权失败（{apiCode}）：{message}。请确认房间号，并检查网络或稍后重试。");
        }
        JsonElement data = root.GetProperty("data");
        string token = data.GetProperty("token").GetString() ?? throw new InvalidDataException("接口未返回连接令牌");
        JsonElement hosts = data.GetProperty("host_list");
        Exception? lastError = null;
        foreach (JsonElement host in hosts.EnumerateArray())
        {
            cancellationToken.ThrowIfCancellationRequested();
            string? hostname = host.GetProperty("host").GetString();
            if (string.IsNullOrWhiteSpace(hostname)) continue;
            int port = host.GetProperty("wss_port").GetInt32();
            try
            {
                await ConnectHostAsync(hostname, port, realRoomId, token, onDanmaku, onStatus, cancellationToken);
                return;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) { lastError = ex; }
        }
        throw new IOException("无法连接弹幕服务器：" + (lastError?.Message ?? "接口没有返回可用服务器"), lastError);
    }

    private static string KeyFromUrl(string? url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri)) throw new InvalidDataException("无法读取 B站 WBI 签名密钥。");
        string filename = Path.GetFileNameWithoutExtension(uri.AbsolutePath);
        if (filename.Length < 32) throw new InvalidDataException("B站返回的 WBI 签名密钥格式无效。");
        return filename;
    }

    private static async Task ConnectHostAsync(string host, int port, int roomId, string token, Action<Danmaku> onDanmaku, Action<string> onStatus, CancellationToken ct)
    {
        using var socket = new ClientWebSocket();
        socket.Options.KeepAliveInterval = TimeSpan.FromSeconds(20);
        await socket.ConnectAsync(new Uri($"wss://{host}:{port}/sub"), ct);
        byte[] join = JsonSerializer.SerializeToUtf8Bytes(new { uid = 0, roomid = roomId, protover = 3, platform = "web", type = 2, key = token });
        await socket.SendAsync(Packet(join, 7), WebSocketMessageType.Binary, true, ct);
        onStatus("已连接，等待弹幕…");
        using var heartbeat = new PeriodicTimer(TimeSpan.FromSeconds(30));
        Task receive = ReceiveLoopAsync(socket, onDanmaku, ct);
        while (!receive.IsCompleted)
        {
            Task tick = heartbeat.WaitForNextTickAsync(ct).AsTask();
            if (await Task.WhenAny(receive, tick) == receive) break;
            await socket.SendAsync(Packet([], 2), WebSocketMessageType.Binary, true, ct);
        }
        await receive;
    }

    private static async Task ReceiveLoopAsync(ClientWebSocket socket, Action<Danmaku> onDanmaku, CancellationToken ct)
    {
        byte[] buffer = new byte[64 * 1024];
        using var stream = new MemoryStream();
        while (socket.State == WebSocketState.Open)
        {
            WebSocketReceiveResult result = await socket.ReceiveAsync(buffer, ct);
            if (result.MessageType == WebSocketMessageType.Close) break;
            stream.Write(buffer, 0, result.Count);
            if (!result.EndOfMessage) continue;
            ParsePacketBuffer(stream.GetBuffer(), (int)stream.Length, onDanmaku);
            stream.SetLength(0);
        }
    }

    private static byte[] Packet(byte[] body, int operation)
    {
        byte[] packet = new byte[16 + body.Length];
        WriteInt(packet, 0, packet.Length); WriteShort(packet, 4, 16); WriteShort(packet, 6, 1);
        WriteInt(packet, 8, operation); WriteInt(packet, 12, 1); body.CopyTo(packet, 16);
        return packet;
    }

    private static void ParsePacketBuffer(byte[] bytes, int length, Action<Danmaku> onDanmaku)
    {
        int offset = 0;
        while (offset + 16 <= length)
        {
            int packetLength = ReadInt(bytes, offset);
            if (packetLength < 16 || offset + packetLength > length) return;
            int headerLength = ReadShort(bytes, offset + 4);
            int version = ReadShort(bytes, offset + 6);
            int operation = ReadInt(bytes, offset + 8);
            int bodyOffset = offset + headerLength;
            int bodyLength = packetLength - headerLength;
            if (operation == 5 && bodyLength > 0)
            {
                if (version == 2) ParseCompressed(bytes, bodyOffset, bodyLength, onDanmaku, false);
                else if (version == 3) ParseCompressed(bytes, bodyOffset, bodyLength, onDanmaku, true);
                else ParseJson(bytes, bodyOffset, bodyLength, onDanmaku);
            }
            offset += packetLength;
        }
    }

    private static void ParseCompressed(byte[] data, int offset, int count, Action<Danmaku> sink, bool brotli)
    {
        using var input = new MemoryStream(data, offset, count);
        using Stream decoded = brotli ? new BrotliStream(input, CompressionMode.Decompress) : new ZLibStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream(); decoded.CopyTo(output);
        byte[] bytes = output.ToArray(); ParsePacketBuffer(bytes, bytes.Length, sink);
    }

    private static void ParseJson(byte[] bytes, int offset, int count, Action<Danmaku> sink)
    {
        try
        {
            using JsonDocument json = JsonDocument.Parse(bytes.AsMemory(offset, count));
            JsonElement root = json.RootElement;
            if (!root.TryGetProperty("cmd", out JsonElement cmd) || !cmd.GetString()!.StartsWith("DANMU_MSG", StringComparison.Ordinal)) return;
            JsonElement info = root.GetProperty("info");
            string text = info[1].GetString() ?? "";
            string user = info[2][1].GetString() ?? "观众";
            if (text.Length > 0) sink(new Danmaku(user, text, DateTime.Now));
        }
        catch (JsonException) { }
        catch (KeyNotFoundException) { }
        catch (InvalidOperationException) { }
    }

    private static int ReadInt(byte[] b, int o) => (b[o] << 24) | (b[o + 1] << 16) | (b[o + 2] << 8) | b[o + 3];
    private static int ReadShort(byte[] b, int o) => (b[o] << 8) | b[o + 1];
    private static void WriteInt(byte[] b, int o, int v) { b[o] = (byte)(v >> 24); b[o + 1] = (byte)(v >> 16); b[o + 2] = (byte)(v >> 8); b[o + 3] = (byte)v; }
    private static void WriteShort(byte[] b, int o, int v) { b[o] = (byte)(v >> 8); b[o + 1] = (byte)v; }
}
