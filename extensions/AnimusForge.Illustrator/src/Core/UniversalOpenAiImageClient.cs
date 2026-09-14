using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TaleWorlds.Library;

namespace AnimusForge.Illustrator.Core
{
    public sealed class ImageGenerationResult
    {
        public bool Success { get; set; }
        public byte[] ImageBytes { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public string ResolvedPrompt { get; set; } = string.Empty;
        public long ElapsedMilliseconds { get; set; }
    }

    public static class UniversalOpenAiImageClient
    {
        private static readonly HttpClient HttpClient;

        static UniversalOpenAiImageClient()
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            HttpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(120)
            };
        }

        public static Task<ImageGenerationResult> GenerateImageAsync(
            string prompt,
            string inputBase64Image,
            IllustrationOptions options,
            CancellationToken cancellationToken = default)
        {
            System.Collections.Generic.IReadOnlyList<IllustrationReferenceImage> single = null;
            if (!string.IsNullOrWhiteSpace(inputBase64Image))
            {
                single = new[] { new IllustrationReferenceImage(inputBase64Image, "游戏内真实画面参考") };
            }
            return GenerateImageAsync(prompt, single, options, cancellationToken);
        }

        public static async Task<ImageGenerationResult> GenerateImageAsync(
            string prompt,
            System.Collections.Generic.IReadOnlyList<IllustrationReferenceImage> referenceImages = null,
            IllustrationOptions options = null,
            CancellationToken cancellationToken = default)
        {
            var result = new ImageGenerationResult
            {
                ResolvedPrompt = prompt ?? string.Empty
            };

            var settings = options;
            if (settings == null || !settings.EnableImageGeneration)
            {
                result.ErrorMessage = "AI 生图系统未启用";
                return result;
            }

            string baseUrl = (settings.ApiBaseUrl ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                result.ErrorMessage = "未配置生图 API 端点 (Base URL)";
                return result;
            }

            string apiKey = (settings.ApiKey ?? string.Empty).Trim();
            string model = (settings.ModelName ?? "black-forest-labs/FLUX.1-schnell").Trim();
            string size = (settings.ImageSize ?? "1024x1024").Trim();
            string quality = settings.SelectedQuality ?? "";
            string style = settings.SelectedStyle ?? "";
            // style 仅 vivid/natural 是 API 合法枚举；custom/暗黑史诗/电影级 等走提示词注入，避免非法枚举 400
            string customStyleHint;
            switch (style)
            {
                case "custom":
                    customStyleHint = (settings.CustomStylePrompt ?? string.Empty).Trim();
                    style = null;
                    break;
                case "dark-epic":
                    customStyleHint = "暗黑史诗写实, dark epic realism, grim medieval war chronicle, dramatic chiaroscuro, painterly oil texture";
                    style = null;
                    break;
                case "cinematic":
                    customStyleHint = "电影级光影, cinematic film still, anamorphic composition, movie-grade dramatic lighting and color grading";
                    style = null;
                    break;
                case "classic-oil":
                    customStyleHint = "古典写实历史油画巨作, 伦勃朗与克雷格·穆林斯(Craig Mullins)式明暗对照法(Chiaroscuro), 戏剧性光影微光, 细腻富有体积感的笔触肌理, classical oil painting masterpiece, dramatic chiaroscuro lighting, painterly brushwork, 8k fine detail";
                    style = null;
                    break;
                default:
                    customStyleHint = string.Empty;
                    break;
            }
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // 1. 智能协议探测：判断是标准生图端点(/images/generations)还是对话多模态生图(/chat/completions，如 gemini-3.1-flash-image)
                if (settings.EnableReferenceImageForGeneration == false)
                {
                    referenceImages = null;
                }
                string negativePrompt = settings.NegativePrompt ?? string.Empty;
                int requestedRefImages = referenceImages?.Count ?? 0;

                bool isChatProtocol = IsChatCompletionProtocol(model, baseUrl, settings.UseExactEndpointUrl);
                string endpointUrl = ResolveEndpointUrl(baseUrl, isChatProtocol, settings.UseExactEndpointUrl);
                string effectivePrompt = BuildEffectivePrompt(prompt, size, quality, style, customStyleHint, negativePrompt, isChatProtocol);

                bool success = false;
                byte[] imageBytes = null;
                string imageUrl = null;
                string errorMessage = null;

                // 2. Images 协议 + 有参考图 → 先试 /images/edits（multipart 真正携带参考图）。
                //    generations 端点没有参考图字段，之前日志打 refImages=N 但实际从未发送。
                if (!isChatProtocol && requestedRefImages > 0 && !settings.UseExactEndpointUrl)
                {
                    var edit = await AttemptImagesEditsAsync(baseUrl, model, effectivePrompt, size, referenceImages, apiKey, cancellationToken).ConfigureAwait(false);
                    if (edit.Success)
                    {
                        success = true;
                        imageBytes = edit.ImageBytes;
                        imageUrl = edit.ImageUrl;
                        result.ResolvedPrompt = effectivePrompt;
                    }
                    else
                    {
                        Log($"[Illustrator] /images/edits 不可用（{edit.ErrorMessage}），参考图仅供导演识图，回退纯文本 /images/generations。");
                    }
                }
                else if (!isChatProtocol && requestedRefImages > 0 && settings.UseExactEndpointUrl)
                {
                    Log("[Illustrator] 启用了精确端点地址，/images/generations 无法携带参考图，参考图仅供导演识图 (ActualRefImages=0)。");
                }

                if (!success)
                {
                    var attempt = await AttemptGenerateOnceAsync(endpointUrl, model, effectivePrompt, size, quality, style, referenceImages, apiKey, isChatProtocol, cancellationToken).ConfigureAwait(false);
                    success = attempt.Success;
                    imageBytes = attempt.ImageBytes;
                    imageUrl = attempt.ImageUrl;
                    errorMessage = attempt.ErrorMessage;
                    if (success) result.ResolvedPrompt = effectivePrompt;

                    // 3. 自动弹性降级：若发往 /images/generations 被网关拒绝(提示不支持生图或需要 messages)，自动重试 /chat/completions
                    if (!success && attempt.ShouldFallbackToChat && !isChatProtocol && !settings.UseExactEndpointUrl)
                    {
                        Log($"[Illustrator] 检测到生图端点不支持该模型({model})，自动尝试回退至 /chat/completions 多模态生图通道...");
                        string chatEffectivePrompt = BuildEffectivePrompt(prompt, size, quality, style, customStyleHint, negativePrompt, true);
                        string chatEndpointUrl = ResolveEndpointUrl(baseUrl, true, false);
                        var chatRetry = await AttemptGenerateOnceAsync(chatEndpointUrl, model, chatEffectivePrompt, size, quality, style, referenceImages, apiKey, true, cancellationToken).ConfigureAwait(false);
                        if (chatRetry.Success)
                        {
                            success = true;
                            imageBytes = chatRetry.ImageBytes;
                            imageUrl = chatRetry.ImageUrl;
                            errorMessage = null;
                            result.ResolvedPrompt = chatEffectivePrompt;
                        }
                        else
                        {
                            errorMessage = chatRetry.ErrorMessage;
                        }
                    }
                }

                if (success && imageBytes != null && imageBytes.Length > 0)
                {
                    result.Success = true;
                    result.ImageBytes = imageBytes;
                    result.ImageUrl = imageUrl ?? string.Empty;
                }
                else
                {
                    result.ErrorMessage = errorMessage ?? "未能从服务端响应中提取到有效图像数据";
                }
            }
            catch (OperationCanceledException)
            {
                result.ErrorMessage = "生图请求已超时或被取消";
            }
            catch (Exception ex)
            {
                result.ErrorMessage = "生图通信异常: " + ex.Message;
                Log($"[Illustrator] Exception during generation: {ex}");
            }
            finally
            {
                stopwatch.Stop();
                result.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
                Log($"[Illustrator] Generation completed in {result.ElapsedMilliseconds}ms. Success={result.Success}");
            }

            return result;
        }

        public static bool IsChatCompletionProtocol(string model, string baseUrl, bool useExactUrl)
        {
            if (!string.IsNullOrWhiteSpace(baseUrl) && baseUrl.IndexOf("/chat/completions", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            if (!string.IsNullOrWhiteSpace(model))
            {
                string m = model.ToLowerInvariant();
                // 常见以对话多模态形式输出图像的模型 (如 Gemini Native Image 系列)
                if (m.Contains("gemini") && m.Contains("image")) return true;
                if (m.Contains("chat") && m.Contains("image")) return true;
            }

            return false;
        }

        public static string ResolveEndpointUrl(string baseUrl, bool isChatCompletion, bool useExactUrl)
        {
            string url = (baseUrl ?? string.Empty).Trim().TrimEnd('/');
            if (useExactUrl)
            {
                return url;
            }

            if (isChatCompletion)
            {
                if (url.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase))
                {
                    return url;
                }
                if (url.EndsWith("/images/generations", StringComparison.OrdinalIgnoreCase))
                {
                    url = url.Substring(0, url.Length - "/images/generations".Length).TrimEnd('/');
                }
                if (url.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
                {
                    return url + "/chat/completions";
                }
                return url + "/v1/chat/completions";
            }
            else
            {
                if (url.EndsWith("/images/generations", StringComparison.OrdinalIgnoreCase))
                {
                    return url;
                }
                if (url.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase))
                {
                    url = url.Substring(0, url.Length - "/chat/completions".Length).TrimEnd('/');
                }
                if (url.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
                {
                    return url + "/images/generations";
                }
                return url + "/v1/images/generations";
            }
        }

        /// <summary>
        /// 拼出实际发给生图服务的有效提示词：Chat 协议附加画幅/画质格式指令，Images 协议把画风写进正文，
        /// 两种协议都追加负面提示词。缓存与"查看提示词"展示的就是这个真实发送值。
        /// </summary>
        private const string BuiltinNegativePrompt = "game screenshot, 3D game render, video game still, HUD, user interface, UI elements, dialogue box, subtitles, overlay text, watermark";

        public static string BuildEffectivePrompt(string prompt, string size, string quality, string style, string customStyleHint = null, string negativePrompt = null, bool chatProtocol = false)
        {
            string effectivePrompt = chatProtocol
                ? BuildChatImagePrompt(prompt, size, quality, style, customStyleHint)
                : (prompt ?? string.Empty);
            if (!chatProtocol && !string.IsNullOrWhiteSpace(customStyleHint))
            {
                effectivePrompt += "\n[画风指令: " + customStyleHint.Trim() + "]";
            }
            // 内置反截图负面词：无论用户负面词如何配置都生效，避免生图模型复刻游戏渲染质感与界面元素
            string mergedNegative = string.IsNullOrWhiteSpace(negativePrompt)
                ? BuiltinNegativePrompt
                : BuiltinNegativePrompt + ", " + negativePrompt.Trim();
            effectivePrompt += "\n[画面中严禁出现的元素/Negative]: " + mergedNegative;
            return effectivePrompt;
        }

        /// <summary>
        /// 由基础地址推导 /images/edits 端点（标准 OpenAI 图生图/多参考图编辑端点）。
        /// </summary>
        private static string ResolveEditsEndpointUrl(string baseUrl)
        {
            string url = (baseUrl ?? string.Empty).Trim().TrimEnd('/');
            if (url.EndsWith("/images/edits", StringComparison.OrdinalIgnoreCase)) return url;
            if (url.EndsWith("/images/generations", StringComparison.OrdinalIgnoreCase))
            {
                return url.Substring(0, url.Length - "/images/generations".Length).TrimEnd('/') + "/images/edits";
            }
            if (url.EndsWith("/v1", StringComparison.OrdinalIgnoreCase)) return url + "/images/edits";
            return url + "/v1/images/edits";
        }

        /// <summary>
        /// OpenAI /images/edits multipart 请求：参考图以 image[] 文件流真正上传。
        /// 与 /images/generations 的 JSON 不同，这是 Images 协议族里唯一能携带参考图的标准通道。
        /// </summary>
        private static async Task<(bool Success, byte[] ImageBytes, string ImageUrl, string ErrorMessage)> AttemptImagesEditsAsync(
            string baseUrl,
            string model,
            string effectivePrompt,
            string size,
            System.Collections.Generic.IReadOnlyList<IllustrationReferenceImage> referenceImages,
            string apiKey,
            CancellationToken cancellationToken)
        {
            string editsUrl = ResolveEditsEndpointUrl(baseUrl);
            try
            {
                using (var form = new MultipartFormDataContent())
                {
                    form.Add(new StringContent(model ?? string.Empty, Encoding.UTF8), "model");
                    form.Add(new StringContent(effectivePrompt ?? string.Empty, Encoding.UTF8), "prompt");
                    if (!string.IsNullOrWhiteSpace(size)) form.Add(new StringContent(size, Encoding.UTF8), "size");
                    form.Add(new StringContent("1"), "n");

                    int sent = 0;
                    foreach (var reference in referenceImages)
                    {
                        if (reference == null || string.IsNullOrWhiteSpace(reference.Base64Image)) continue;
                        string data = reference.Base64Image;
                        if (data.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                        {
                            int comma = data.IndexOf(',');
                            if (comma < 0) continue;
                            data = data.Substring(comma + 1);
                        }
                        byte[] bytes;
                        try { bytes = Convert.FromBase64String(data); }
                        catch { continue; }
                        if (bytes == null || bytes.Length < 100) continue;
                        var imageContent = new ByteArrayContent(bytes);
                        imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
                        form.Add(imageContent, "image[]", $"reference_{sent}.png");
                        sent++;
                    }
                    if (sent == 0) return (false, null, null, "no usable reference images");

                    using (var request = new HttpRequestMessage(HttpMethod.Post, editsUrl) { Content = form })
                    {
                        if (!string.IsNullOrWhiteSpace(apiKey))
                        {
                            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                        }
                        Log($"[Illustrator] Requesting image edit from {editsUrl} (model={model}, protocol=ImagesEdits, ActualRefImages={sent})...");
                        using (var response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken).ConfigureAwait(false))
                        {
                            string responseText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                            if (!response.IsSuccessStatusCode)
                            {
                                return (false, null, null, ExtractErrorMessage(responseText, (int)response.StatusCode));
                            }
                            var extracted = await ExtractImageAsync(responseText, cancellationToken).ConfigureAwait(false);
                            if (extracted != null && extracted.Bytes != null && extracted.Bytes.Length > 0)
                            {
                                return (true, extracted.Bytes, extracted.Url, null);
                            }
                            return (false, null, null, "edit response contained no image data");
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return (false, null, null, "images/edits request failed: " + ex.Message);
            }
        }

        private static async Task<(bool Success, byte[] ImageBytes, string ImageUrl, string ErrorMessage, bool ShouldFallbackToChat)> AttemptGenerateOnceAsync(
            string endpointUrl,
            string model,
            string effectivePrompt,
            string size,
            string quality,
            string style,
            System.Collections.Generic.IReadOnlyList<IllustrationReferenceImage> referenceImages,
            string apiKey,
            bool isChatProtocol,
            CancellationToken cancellationToken)
        {
            JObject payload;
            int actualRefImages = 0;
            if (isChatProtocol)
            {
                JToken messageContent;
                bool hasRefs = referenceImages != null && referenceImages.Count > 0;
                if (hasRefs)
                {
                    var content = new JArray
                    {
                        new JObject
                        {
                            ["type"] = "text",
                            ["text"] = effectivePrompt
                        }
                    };
                    foreach (var reference in referenceImages)
                    {
                        if (reference == null || string.IsNullOrWhiteSpace(reference.Base64Image)) continue;
                        if (!string.IsNullOrWhiteSpace(reference.Label))
                        {
                            content.Add(new JObject
                            {
                                ["type"] = "text",
                                ["text"] = "【参考图】" + reference.Label
                            });
                        }
                        string data = reference.Base64Image;
                        string mimeType = data.StartsWith("iVBORw0KGgo") ? "image/png" : "image/jpeg";
                        string dataUri = data.StartsWith("data:") ? data : $"data:{mimeType};base64,{data}";
                        content.Add(new JObject
                        {
                            ["type"] = "image_url",
                            ["image_url"] = new JObject
                            {
                                ["url"] = dataUri
                            }
                        });
                        actualRefImages++;
                    }
                    messageContent = content;
                }
                else
                {
                    messageContent = effectivePrompt;
                }

                payload = new JObject
                {
                    ["model"] = model,
                    ["messages"] = new JArray
                    {
                        new JObject
                        {
                            ["role"] = "user",
                            ["content"] = messageContent
                        }
                    }
                };

                // Gemini 等多模态对话生图原生支持 aspect_ratio 顶层字段
                string ar = ResolveGeminiAspectRatio(size);
                if (!string.IsNullOrWhiteSpace(ar))
                {
                    payload["aspect_ratio"] = ar;
                }
            }
            else
            {
                payload = new JObject
                {
                    ["model"] = model,
                    ["prompt"] = effectivePrompt,
                    ["n"] = 1,
                    ["size"] = size
                };

                if (!string.IsNullOrWhiteSpace(quality))
                {
                    payload["quality"] = quality;
                }

                if (!string.IsNullOrWhiteSpace(style))
                {
                    payload["style"] = style;
                }
            }

            using (var request = new HttpRequestMessage(HttpMethod.Post, endpointUrl))
            {
                request.Content = new StringContent(payload.ToString(Formatting.None), Encoding.UTF8, "application/json");

                if (!string.IsNullOrWhiteSpace(apiKey))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                }

                Log($"[Illustrator] Requesting image generation from {endpointUrl} (model={model}, protocol={(isChatProtocol ? "Chat" : "Images")}, refImages={referenceImages?.Count ?? 0}, ActualRefImages={actualRefImages})...");

                using (var response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken).ConfigureAwait(false))
                {
                        string responseText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                        if (!response.IsSuccessStatusCode)
                        {
                            string errorMsg = ExtractErrorMessage(responseText, (int)response.StatusCode);
                            Log($"[Illustrator] Request failed: {errorMsg}");

                            bool fallback = false;
                            if (!isChatProtocol && (
                                errorMsg.IndexOf("not supported on /v1/images/generations", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                errorMsg.IndexOf("field messages is required", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                errorMsg.IndexOf("not support", StringComparison.OrdinalIgnoreCase) >= 0))
                            {
                                fallback = true;
                            }

                            return (false, null, null, errorMsg, fallback);
                        }

                        var extracted = await ExtractImageAsync(responseText, cancellationToken).ConfigureAwait(false);
                        if (extracted != null && extracted.Bytes != null && extracted.Bytes.Length > 0)
                        {
                            return (true, extracted.Bytes, extracted.Url, null, false);
                        }

                        return (false, null, null, "响应中未能解析到有效的图片数据 (支持 data[] 数组、choices[].message.images 及 Markdown 图链接)", false);
                    }
            }
        }

        private sealed class ExtractedImage
        {
            public byte[] Bytes { get; set; }
            public string Url { get; set; }
        }

        private static async Task<ExtractedImage> ExtractImageAsync(string responseText, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(responseText)) return null;

            JObject parsed = JObject.Parse(responseText);

            // 1. 标准 OpenAI Images 格式: "data": [ { "b64_json": "...", "url": "..." } ]
            if (parsed["data"] is JArray dataArray && dataArray.Count > 0)
            {
                JToken firstItem = dataArray[0];
                string b64 = firstItem["b64_json"]?.ToString();
                string url = firstItem["url"]?.ToString();

                if (!string.IsNullOrWhiteSpace(b64))
                {
                    return new ExtractedImage { Bytes = Convert.FromBase64String(b64) };
                }
                if (!string.IsNullOrWhiteSpace(url))
                {
                    byte[] bytes = await DownloadImageBytesAsync(url, cancellationToken).ConfigureAwait(false);
                    return new ExtractedImage { Bytes = bytes, Url = url };
                }
            }

            // 2. Chat Completions 图像格式: "choices": [ { "message": { "images": [ ... ], "content": "..." } } ]
            if (parsed["choices"] is JArray choices && choices.Count > 0)
            {
                JToken message = choices[0]["message"];
                if (message != null)
                {
                    // 2a. 检查 message.images 数组 (Gemini/OneAPI 标准多模态出图)
                    if (message["images"] is JArray images && images.Count > 0)
                    {
                        foreach (var imgToken in images)
                        {
                            string rawUrl = null;
                            if (imgToken is JObject imgObj)
                            {
                                rawUrl = imgObj["image_url"]?["url"]?.ToString()
                                         ?? imgObj["url"]?.ToString()
                                         ?? imgObj["b64_json"]?.ToString()
                                         ?? imgObj["base64"]?.ToString();
                            }
                            else if (imgToken is JValue val)
                            {
                                rawUrl = val.ToString();
                            }

                            if (!string.IsNullOrWhiteSpace(rawUrl))
                            {
                                var extracted = await ParseUriOrBase64Async(rawUrl, cancellationToken).ConfigureAwait(false);
                                if (extracted?.Bytes != null && extracted.Bytes.Length > 0)
                                    return extracted;
                            }
                        }
                    }

                    // 2b. 检查 message.content 中的 Markdown 图片格式或直接 data:image / URL
                    string content = message["content"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(content))
                    {
                        var match = Regex.Match(
                            content,
                            @"!\[.*?\]\((https?://[^\s\)]+|data:image/[^;]+;base64,[^\s\)]+)\)");
                        if (match.Success)
                        {
                            var extracted = await ParseUriOrBase64Async(match.Groups[1].Value, cancellationToken).ConfigureAwait(false);
                            if (extracted?.Bytes != null && extracted.Bytes.Length > 0)
                                return extracted;
                        }

                        if (content.StartsWith("data:image", StringComparison.OrdinalIgnoreCase) ||
                            content.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                            content.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                        {
                            var extracted = await ParseUriOrBase64Async(content.Trim(), cancellationToken).ConfigureAwait(false);
                            if (extracted?.Bytes != null && extracted.Bytes.Length > 0)
                                return extracted;
                        }
                    }
                }
            }

            return null;
        }

        private static async Task<ExtractedImage> ParseUriOrBase64Async(string uriOrBase64, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(uriOrBase64)) return null;

            uriOrBase64 = uriOrBase64.Trim();
            if (uriOrBase64.StartsWith("data:image", StringComparison.OrdinalIgnoreCase))
            {
                int commaIdx = uriOrBase64.IndexOf(',');
                string b64 = commaIdx >= 0 ? uriOrBase64.Substring(commaIdx + 1) : uriOrBase64;
                return new ExtractedImage { Bytes = Convert.FromBase64String(b64.Trim()) };
            }

            if (uriOrBase64.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                uriOrBase64.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                byte[] bytes = await DownloadImageBytesAsync(uriOrBase64, cancellationToken).ConfigureAwait(false);
                return new ExtractedImage { Bytes = bytes, Url = uriOrBase64 };
            }

            try
            {
                byte[] bytes = Convert.FromBase64String(uriOrBase64);
                if (bytes != null && bytes.Length > 100)
                {
                    return new ExtractedImage { Bytes = bytes };
                }
            }
            catch
            {
            }

            return null;
        }

        private static async Task<byte[]> DownloadImageBytesAsync(string url, CancellationToken cancellationToken)
        {
            try
            {
                using (var client = new HttpClient { Timeout = TimeSpan.FromSeconds(60) })
                using (var response = await client.GetAsync(url, cancellationToken).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();
                    return await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Log($"[Illustrator] Failed to download generated image from {url}: {ex.Message}");
                return null;
            }
        }

        private static string ExtractErrorMessage(string responseBody, int statusCode)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(responseBody))
                {
                    JObject obj = JObject.Parse(responseBody);
                    JToken error = obj["error"];
                    if (error != null)
                    {
                        string msg = error["message"]?.ToString();
                        if (!string.IsNullOrWhiteSpace(msg))
                        {
                            return $"HTTP {statusCode}: {msg}";
                        }
                    }
                }
            }
            catch
            {
            }
            return $"HTTP {statusCode}: {responseBody}";
        }

        public static string ResolveGeminiAspectRatio(string size)
        {
            if (string.IsNullOrWhiteSpace(size)) return null;

            string s = size.Trim().ToLowerInvariant();
            string[] parts = s.Split(new[] { 'x', '*', ':' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2 && int.TryParse(parts[0], out int w) && int.TryParse(parts[1], out int h) && h > 0)
            {
                double ratio = (double)w / h;
                // 常见官方比例映射: 1:1, 16:9, 9:16, 4:3, 3:4
                if (ratio >= 0.95 && ratio <= 1.05) return "1:1";
                if (ratio >= 1.55 && ratio <= 1.95) return "16:9";
                if (ratio >= 0.50 && ratio <= 0.65) return "9:16";
                if (ratio >= 1.25 && ratio <= 1.45) return "4:3";
                if (ratio >= 0.68 && ratio <= 0.85) return "3:4";
                if (ratio > 1.05) return "16:9";
                return "9:16";
            }

            return null;
        }

        public static string BuildChatImagePrompt(string prompt, string size, string quality, string style, string customStyleHint = null)
        {
            var directives = new System.Collections.Generic.List<string>();

            string ar = ResolveGeminiAspectRatio(size);
            if (!string.IsNullOrWhiteSpace(ar))
            {
                directives.Add($"aspect ratio {ar}");
                if (ar == "16:9") directives.Add("panoramic widescreen landscape format");
                else if (ar == "9:16") directives.Add("vertical portrait format");
                else if (ar == "3:4") directives.Add("tall portrait format");
                else if (ar == "4:3") directives.Add("standard landscape format");
                else if (ar == "1:1") directives.Add("square format");
            }
            if (!string.IsNullOrWhiteSpace(size))
            {
                directives.Add($"target resolution {size}");
            }

            if (!string.IsNullOrWhiteSpace(quality))
            {
                if (quality.Equals("hd", StringComparison.OrdinalIgnoreCase) || quality.Equals("high", StringComparison.OrdinalIgnoreCase))
                {
                    directives.Add("ultra-high definition, 4k resolution, hyper-detailed, masterpiece, hd quality");
                }
                else if (quality.Equals("standard", StringComparison.OrdinalIgnoreCase) || quality.Equals("auto", StringComparison.OrdinalIgnoreCase))
                {
                    directives.Add("standard definition quality");
                }
                else if (quality.Equals("medium", StringComparison.OrdinalIgnoreCase))
                {
                    directives.Add("medium-high definition quality, detailed");
                }
                else if (quality.Equals("low", StringComparison.OrdinalIgnoreCase))
                {
                    directives.Add("fast draft quality");
                }
            }

            if (!string.IsNullOrWhiteSpace(style))
            {
                if (style.Equals("vivid", StringComparison.OrdinalIgnoreCase))
                {
                    directives.Add("vivid style, hyper-real, dramatic cinematic lighting, rich saturated colors");
                }
                else if (style.Equals("natural", StringComparison.OrdinalIgnoreCase))
                {
                    directives.Add("natural style, authentic realism, soft natural lighting, true to life");
                }
            }

            if (!string.IsNullOrWhiteSpace(customStyleHint))
            {
                directives.Add("style: " + customStyleHint.Trim());
            }

            if (directives.Count > 0)
            {
                string directiveText = string.Join(", ", directives);
                return $"{prompt}\n[Format Directive: {directiveText}]";
            }

            return prompt;
        }

        private static void Log(string message)
        {
            TaleWorlds.Library.Debug.Print(message);
        }
    }
}
