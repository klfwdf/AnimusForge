using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.Illustrator.Core;
using AnimusForge.Illustrator.Engine;
using TaleWorlds.Library;

namespace AnimusForge.Illustrator
{
    internal static class IllustratorApiTest
    {
        private static CancellationTokenSource _active;

        internal static void Cancel()
        {
            IllustratorRuntime.AssertMainThread();
            _active?.Cancel();
        }

        internal static void StartOrCancel(IllustratorSettings settings)
        {
            if (!IllustratorRuntime.IsMainThread)
            {
                IllustratorRuntime.Post(() => StartOrCancel(settings));
                return;
            }
            if (_active != null)
            {
                _active.Cancel();
                Show("正在取消测试…");
                return;
            }
            if (!settings.EnableImageGeneration) { Show("请先开启生图总开关。"); return; }
            if (!Uri.TryCreate((settings.ApiBaseUrl ?? "").Trim(), UriKind.Absolute, out var endpoint)
                || (endpoint.Scheme != Uri.UriSchemeHttp && endpoint.Scheme != Uri.UriSchemeHttps))
            { Show("请先填写有效的 HTTP/HTTPS 生图 API 地址。"); return; }
            if (!settings.UsePlayer2ImageApi && string.IsNullOrWhiteSpace(settings.ModelName)) { Show("请先填写生图模型名称。"); return; }

            // Capture this MCM editor object, including unsaved fields, on the UI
            // thread. Workers never read the live settings or campaign objects.
            var options = new IllustrationOptions(settings, "", "", "").ForApiTest();
            var cancellation = new CancellationTokenSource();
            _active = cancellation;
            bool started = IllustratorRuntime.Start(() => RunAsync(options, endpoint, cancellation.Token), (message, error) =>
            {
                if (ReferenceEquals(_active, cancellation)) _active = null;
                bool cancelled = cancellation.IsCancellationRequested;
                cancellation.Dispose();
                if (!IllustratorRuntime.IsHostRunning) return;
                string result = cancelled ? "测试已取消。" : error == null ? message
                    : "测试失败：" + SensitiveLogText.Redact(error.Message, options.ApiKey);
                Show(result?.Length > 420 ? result.Substring(0, 420) : result);
            });
            if (!started)
            {
                _active = null;
                cancellation.Dispose();
                Show("生图任务正在忙，请稍后重试。");
                return;
            }
            Show("开始测试，使用当前配置请求一张图片；再次点击可取消。");
        }

        private static async Task<string> RunAsync(IllustrationOptions options, Uri endpoint, CancellationToken token)
        {
            var watch = Stopwatch.StartNew();
            var directory = IllustratorStoragePaths.EnsureDirectory(IllustratorStoragePaths.ApiTestDirectory);
            IllustrationReferenceImage[] references = null;
            // Edits requires an image. Generate a local neutral reference instead
            // of reading player files or capturing a live game scene.
            if (options.UsePlayer2ImageApi || endpoint.AbsolutePath.TrimEnd('/').EndsWith("/images/edits", StringComparison.OrdinalIgnoreCase))
            {
                using (var bitmap = new Bitmap(256, 256))
                using (var graphics = Graphics.FromImage(bitmap))
                using (var output = new MemoryStream())
                {
                    graphics.Clear(System.Drawing.Color.Beige);
                    bitmap.Save(output, ImageFormat.Png);
                    references = new[] { new IllustrationReferenceImage(Convert.ToBase64String(output.ToArray()),
                        "API测试用空白画布，请在画布上绘制一只苹果。", IllustrationReferenceKind.Scene) };
                }
            }
            var result = await UniversalOpenAiImageClient.GenerateImageAsync(
                "API connection test: create one image of a red apple on a plain table. No text or lettering.",
                references, options, token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            string elapsed = (watch.ElapsedMilliseconds / 1000.0).ToString("0.0");
            if (!result.Success || result.ImageBytes == null || result.ImageBytes.Length == 0)
                return "测试失败（" + elapsed + "秒）：" + SensitiveLogText.Redact(result.ErrorMessage, options.ApiKey);
            var bytes = ImagePayload.Normalize(result.ImageBytes);
            var destination = Path.Combine(directory, "last-result.png");
            var pending = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllBytes(pending, bytes);
                token.ThrowIfCancellationRequested();
                if (File.Exists(destination)) File.Replace(pending, destination, null);
                else File.Move(pending, destination);
            }
            finally { if (File.Exists(pending)) File.Delete(pending); }
            return "测试成功（" + elapsed + "秒），图片已保存至模组 Cache/Illustrator/ApiTest/last-result.png。";
        }

        private static void Show(string text)
            => InformationManager.DisplayMessage(new InformationMessage("[生图 API 测试] " + text));
    }
}
