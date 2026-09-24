using System;
using System.Collections.Generic;
using System.IO;
using AnimusForge.Illustrator.Engine;
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
        public string DiagnosticId { get; set; } = string.Empty;
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
                single = new[] { new IllustrationReferenceImage(inputBase64Image, "游戏内真实画面参考", IllustrationReferenceKind.Scene) };
            }
            return GenerateImageAsync(prompt, single, options, cancellationToken);
        }

        public static async Task<ImageGenerationResult> GenerateImageAsync(
            string prompt,
            System.Collections.Generic.IReadOnlyList<IllustrationReferenceImage> referenceImages = null,
            IllustrationOptions options = null,
            CancellationToken cancellationToken = default)
        {
            using (var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                deadline.CancelAfter(TimeSpan.FromSeconds(120));
                cancellationToken = deadline.Token;
            var result = new ImageGenerationResult
            {
                ResolvedPrompt = prompt ?? string.Empty,
                DiagnosticId = GenerationDiagnostics.Current?.Id ?? string.Empty
            };

            var settings = options;
            if (settings == null || !settings.EnableImageGeneration)
            {
                result.ErrorMessage = "AI 生图系统未启用";
                GenerationDiagnostics.Current?.RecordImageResult(result);
                return result;
            }

            string baseUrl = (settings.ApiBaseUrl ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                result.ErrorMessage = "未配置生图 API 端点 (Base URL)";
                GenerationDiagnostics.Current?.RecordImageResult(result);
                return result;
            }

            string apiKey = (settings.ApiKey ?? string.Empty).Trim();
            GenerationDiagnostics.Current?.RegisterSecret(apiKey);
            string model = (settings.ModelName ?? "black-forest-labs/FLUX.1-schnell").Trim();
            string size = (settings.ImageSize ?? "1024x1024").Trim();
            string quality = settings.SelectedQuality ?? "";
            // style 仅 vivid/natural 是 API 合法枚举；custom/暗黑史诗/电影级/古典油画 等走提示词注入，避免非法枚举 400
            // 每个提示词注入预设携带专属负面词；自定义画风/负面词两个文本框仅在选“提示词(自定义画风)”预设时生效
            var stylePreset = IllustrationStylePresets.Resolve(settings.SelectedStyle, settings.CustomStylePrompt);
            string style = stylePreset.ApiStyle;
            string customStyleHint = stylePreset.ImagePrompt;
            string presetNegative = stylePreset.NegativePrompt;
            bool isCustomPreset = stylePreset.IsCustom;
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // 1. 智能协议探测：判断是标准生图端点(/images/generations)还是对话多模态生图(/chat/completions，如 gemini-3.1-flash-image)
                if (settings.EnableReferenceImageForGeneration == false)
                {
                    referenceImages = null;
                }
                // 用户自定义负面词仅在选“提示词(自定义画风)”预设时生效，追加在预设负面词之后
                string userNegative = isCustomPreset ? (settings.NegativePrompt ?? string.Empty).Trim() : string.Empty;
                string negativePrompt;
                if (string.IsNullOrWhiteSpace(presetNegative)) negativePrompt = userNegative;
                else if (string.IsNullOrWhiteSpace(userNegative)) negativePrompt = presetNegative;
                else negativePrompt = presetNegative + ", " + userNegative;
                int requestedRefImages = referenceImages?.Count ?? 0;

                bool exactEditsEndpoint = settings.UseExactEndpointUrl && IsImagesEditsEndpointUrl(baseUrl);
                bool isChatProtocol = !exactEditsEndpoint && (IsChatCompletionProtocol(model, baseUrl, settings.UseExactEndpointUrl)
                    || (settings.PreferChatImageProtocol && !settings.UseExactEndpointUrl));
                string endpointUrl = ResolveEndpointUrl(baseUrl, isChatProtocol, settings.UseExactEndpointUrl);
                string effectivePrompt = BuildEffectivePrompt(prompt, size, quality, style, customStyleHint, negativePrompt, isChatProtocol, settings.Randomness);
                if (exactEditsEndpoint && requestedRefImages == 0)
                {
                    result.ErrorMessage = "精确 images/edits 端点需要可用的参考图；请开启参考图并取得人物或场景参考后再生成。未发送请求。";
                    return result;
                }
                if (settings.UseExactEndpointUrl && !exactEditsEndpoint && !isChatProtocol && requestedRefImages > 0)
                {
                    // /images/generations has no image field. Do not report a successful
                    // text-only generation as if the caller's identity/scene references were
                    // honored when an exact images/generations URL explicitly selected that endpoint.
                    result.ErrorMessage = "精确 images/generations 端点不能携带参考图；请改用 /images/edits 或开启对话多模态生图通道。未发送请求。";
                    return result;
                }

                bool success = false;
                byte[] imageBytes = null;
                string imageUrl = null;
                string errorMessage = null;
                bool stopAfterEditFailure = false;

                // 2. Images 协议 + 有参考图 → 先试 /images/edits（multipart 真正携带参考图）。
                //    generations 端点没有参考图字段，之前日志打 refImages=N 但实际从未发送。
                if (!isChatProtocol && requestedRefImages > 0 && (!settings.UseExactEndpointUrl || exactEditsEndpoint))
                {
                    var edit = await AttemptImagesEditsAsync(baseUrl, model, effectivePrompt, size, quality, style, referenceImages, apiKey, cancellationToken, customStyleHint).ConfigureAwait(false);
                    result.ResolvedPrompt = edit.ResolvedPrompt;
                    if (edit.Success)
                    {
                        success = true;
                        imageBytes = edit.ImageBytes;
                        imageUrl = edit.ImageUrl;
                    }
                    else if (settings.UseExactEndpointUrl || !edit.ShouldFallbackToText)
                    {
                        errorMessage = edit.ErrorMessage;
                        stopAfterEditFailure = true;
                    }
                    else
                    {
                        // A generations request cannot carry the references. Stopping here
                        // avoids a visually valid but identity/scene-invalid image. The user
                        // can opt into the chat multimodal route when the provider supports it.
                        errorMessage = "images/edits 不支持当前参考图请求；为避免静默丢失人物/场景参考，已停止生成。请开启“优先对话多模态生图通道”或配置可用的 /images/edits 端点。";
                        stopAfterEditFailure = true;
                    }
                }
                else if (!isChatProtocol && requestedRefImages > 0 && settings.UseExactEndpointUrl)
                {
                    Log("[Illustrator] 启用了精确端点地址，/images/generations 无法携带参考图，参考图仅供导演识图 (ActualRefImages=0)。");
                }

                if (!success && !stopAfterEditFailure)
                {
                    var attempt = await AttemptGenerateOnceAsync(endpointUrl, model, effectivePrompt, size, quality, style, referenceImages, apiKey, isChatProtocol, cancellationToken, customStyleHint).ConfigureAwait(false);
                    success = attempt.Success;
                    imageBytes = attempt.ImageBytes;
                    imageUrl = attempt.ImageUrl;
                    errorMessage = attempt.ErrorMessage;
                    result.ResolvedPrompt = attempt.ResolvedPrompt;

                    // 3. 自动弹性降级：若发往 /images/generations 被网关拒绝(提示不支持生图或需要 messages)，自动重试 /chat/completions
                    if (!success && attempt.ShouldFallbackToChat && !isChatProtocol && !settings.UseExactEndpointUrl)
                    {
                        Log($"[Illustrator] 检测到生图端点不支持该模型({model})，自动尝试回退至 /chat/completions 多模态生图通道...");
                        string chatEffectivePrompt = BuildEffectivePrompt(prompt, size, quality, style, customStyleHint, negativePrompt, true, settings.Randomness);
                        string chatEndpointUrl = ResolveEndpointUrl(baseUrl, true, false);
                        var chatRetry = await AttemptGenerateOnceAsync(chatEndpointUrl, model, chatEffectivePrompt, size, quality, style, referenceImages, apiKey, true, cancellationToken, customStyleHint).ConfigureAwait(false);
                        result.ResolvedPrompt = chatRetry.ResolvedPrompt;
                        if (chatRetry.Success)
                        {
                            success = true;
                            imageBytes = chatRetry.ImageBytes;
                            imageUrl = chatRetry.ImageUrl;
                            errorMessage = null;
                        }
                        else
                        {
                            errorMessage = chatRetry.ErrorMessage;
                        }
                    }
                }

                if (success && imageBytes != null && imageBytes.Length > 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    result.ImageBytes = imageBytes;
                    result.Success = true;
                    result.ImageUrl = imageUrl ?? string.Empty;
                }
                else
                {
                    result.ErrorMessage = errorMessage ?? "未能从服务端响应中提取到有效图像数据";
                    Log("[Illustrator] Generation failed; details in request diagnostics.");
                }
            }
            catch (OperationCanceledException)
            {
                result.ErrorMessage = "生图请求已超时或被取消";
            }
            catch (InvalidDataException ex)
            {
                result.ErrorMessage = ex.Message;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = "生图通信异常: " + ex.Message;
                Log($"[Illustrator] Exception during generation: {ex.GetType().Name}");
            }
            finally
            {
                stopwatch.Stop();
                result.ErrorMessage = SensitiveLogText.Redact(result.ErrorMessage, apiKey);
                result.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
                GenerationDiagnostics.Current?.RecordImageResult(result);
                Log($"[Illustrator] Generation completed in {result.ElapsedMilliseconds}ms. Success={result.Success}");

                // 临时产物由各提取任务在消费完毕后按自身路径清理，不能在此全局扫描删除。
            }

            return result;

            }
        }

        public static bool IsChatCompletionProtocol(string model, string baseUrl, bool useExactUrl)
        {
            // An explicit standard endpoint wins over model-name heuristics. Parse
            // only the path so query parameters cannot change the request schema.
            if (Uri.TryCreate(baseUrl, UriKind.Absolute, out var endpoint))
            {
                string path = endpoint.AbsolutePath.TrimEnd('/');
                if (path.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase)) return true;
                if (useExactUrl && (path.EndsWith("/images/generations", StringComparison.OrdinalIgnoreCase) ||
                    path.EndsWith("/images/edits", StringComparison.OrdinalIgnoreCase))) return false;
            }
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
            string url = (baseUrl ?? string.Empty).Trim();
            if (useExactUrl)
            {
                return url;
            }
            url = url.TrimEnd('/');

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
        private const string BuiltinNegativePrompt = "extra limbs, malformed hands, bad anatomy, unwanted text, watermark, UI overlay, reference image collage";

        private static string BuildImageStyleAnchor(string customStyleHint, string style)
        {
            string resolved = !string.IsNullOrWhiteSpace(customStyleHint)
                ? customStyleHint.Trim()
                : (style ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(resolved)) return string.Empty;

            var anchor = new StringBuilder();
            anchor.Append("【最高优先级画风锚点】整幅成图必须统一采用指定画风：")
                .Append(resolved)
                .Append("。参考图只提供人物身份、装备、纹章载体和已确认场景事实，不决定最终媒介、渲染质感或摄影风格；从零重绘人物与环境，保持指定画风的笔触、材质和光影一致。");
            if (resolved.IndexOf("油画", StringComparison.OrdinalIgnoreCase) >= 0 ||
                resolved.IndexOf("oil painting", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                anchor.Append("必须看得见有层次的油彩笔触、画布质感和概括性笔触，禁止输出照片、3D渲染、游戏截图或平滑的数字插画质感。");
            }
            return anchor.ToString();
        }

        private const int MaxReferencePayloadBytes = 48 * 1024 * 1024;

        private static IReadOnlyList<IllustrationReferenceImage> PrepareReferenceImages(IReadOnlyList<IllustrationReferenceImage> references, CancellationToken cancellationToken)
        {
            if (references == null) return null;
            var prepared = new List<IllustrationReferenceImage>();
            int totalBytes = 0;
            for (int index = 0; index < references.Count; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var reference = references[index];
                string failure = "图片解码或格式转换失败";
                try
                {
                    if (reference == null || string.IsNullOrWhiteSpace(reference.Base64Image))
                    {
                        failure = "图片数据为空";
                        throw new InvalidDataException();
                    }
                    string encoded = reference.Base64Image.Trim();
                    if (encoded.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                    {
                        int comma = encoded.IndexOf(',');
                        int metadataEnd = comma < 0 ? -1 : encoded.IndexOf(';');
                        if (comma < 0 || metadataEnd < 0 ||
                            encoded.IndexOf(";base64", metadataEnd, StringComparison.OrdinalIgnoreCase) < 0)
                        {
                            failure = "data URI 格式错误";
                            throw new InvalidDataException();
                        }
                        encoded = encoded.Substring(comma + 1);
                    }
                    if (encoded.Length > ImagePayload.MaxBytes * 4L / 3L + 8)
                    {
                        failure = "超过单图24 MiB限制";
                        throw new InvalidDataException();
                    }
                    byte[] normalized = ImagePayload.Normalize(Convert.FromBase64String(encoded));
                    if (totalBytes > MaxReferencePayloadBytes - normalized.Length)
                    {
                        failure = "参考图累计超过48 MiB限制";
                        throw new InvalidDataException();
                    }
                    totalBytes += normalized.Length;
                    prepared.Add(new IllustrationReferenceImage(Convert.ToBase64String(normalized), reference.Label, reference.Kind));
                }
                catch (Exception ex)
                {
                    // Once selected, every reference must survive preparation. Optional
                    // captures are omitted by their producer, never silently dropped here.
                    throw new InvalidDataException($"参考图 {index + 1}（{reference?.Kind.ToString() ?? "未知用途"}）无效：{failure}；已停止生成，未发送请求。", ex);
                }
            }
            return prepared;
        }

        public static string BuildEffectivePrompt(string prompt, string size, string quality, string style, string customStyleHint = null, string negativePrompt = null, bool chatProtocol = false, int randomness = 0)
        {
            string effectivePrompt = chatProtocol
                ? BuildChatImagePrompt(prompt, size, quality, style, customStyleHint)
                : (prompt ?? string.Empty);
            if (!chatProtocol && !string.IsNullOrWhiteSpace(customStyleHint))
            {
                effectivePrompt += "\n[画风指令: " + customStyleHint.Trim() + "]";
            }
            // 通用负面词只覆盖成图缺陷；遮面/装备按人物事实与参考图处理。
            string mergedNegative = string.IsNullOrWhiteSpace(negativePrompt)
                ? BuiltinNegativePrompt
                : BuiltinNegativePrompt + ", " + negativePrompt.Trim();
            effectivePrompt += "\n[画面中严禁出现的元素/Negative]: " + mergedNegative;
            // 0 完全沿用旧版，不追加本段；正数只增加艺术表现变化，不放松硬事实。
            if (randomness > 0)
            {
                int strength = Math.Min(100, randomness);
                string clause = strength >= 100
                    ? "在事实允许的范围内，最大程度探索不同取景、留白、景深与光影表现"
                    : $"艺术表现随机强度为 {strength}/100；数值越高，越主动探索不同取景、留白、景深与光影表现。低值仅作轻微变化";
                effectivePrompt += "\n[艺术表现随机指导]: 人物五官、肤色、发型、体型、装备、家族纹章及所有已确认游戏事实始终保持一致。人物身份立绘只用于身份与装备，纹章标准图只用于徽记；不得把身份图的姿势、背景、构图或光影用作画面模板。场景按导演正文组织：已确认的现场空间关系严格保留，明确标记的非具名艺术布景可以围绕导演主题和空间设计丰富发挥，补充与时代文化一致的材质、装饰与光影细节。保留导演选择的环境内容，不以人物为主为由清空背景；真实现场不补造未知陈设，人物数量与事件结果不改写。取景与绘画表现可大胆变化，同时保持本次行动及空间关系成立。" + clause + "。";
            }
            return effectivePrompt.Trim();
        }

        /// <summary>
        /// 由基础地址推导 /images/edits 端点（标准 OpenAI 图生图/多参考图编辑端点）。
        /// </summary>
        private static string ResolveEditsEndpointUrl(string baseUrl)
        {
            string url = (baseUrl ?? string.Empty).Trim();
            // A complete edit URL may carry routing/authentication query parameters. Preserve it verbatim.
            if (IsImagesEditsEndpointUrl(url)) return url;
            url = url.TrimEnd('/');
            if (url.EndsWith("/images/generations", StringComparison.OrdinalIgnoreCase))
            {
                return url.Substring(0, url.Length - "/images/generations".Length).TrimEnd('/') + "/images/edits";
            }
            if (url.EndsWith("/v1", StringComparison.OrdinalIgnoreCase)) return url + "/images/edits";
            return url + "/v1/images/edits";
        }

        private static bool IsImagesEditsEndpointUrl(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
                uri.AbsolutePath.TrimEnd('/').EndsWith("/images/edits", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// OpenAI /images/edits multipart 请求：参考图以 image[] 文件流真正上传。
        /// 与 /images/generations 的 JSON 不同，这是 Images 协议族里唯一能携带参考图的标准通道。
        /// </summary>
        private static async Task<(bool Success, byte[] ImageBytes, string ImageUrl, string ErrorMessage, bool ShouldFallbackToText, string ResolvedPrompt)> AttemptImagesEditsAsync(
            string baseUrl,
            string model,
            string effectivePrompt,
            string size,
            string quality,
            string style,
            System.Collections.Generic.IReadOnlyList<IllustrationReferenceImage> referenceImages,
            string apiKey,
            CancellationToken cancellationToken,
            string customStyleHint = null)
        {
            string editsUrl = ResolveEditsEndpointUrl(baseUrl);
            bool hadReferences = referenceImages != null && referenceImages.Count > 0;
            referenceImages = PrepareReferenceImages(referenceImages, cancellationToken);
            if (hadReferences && (referenceImages == null || referenceImages.Count == 0))
                return (false, null, null, "没有可用的参考图；请求未发送。", false, effectivePrompt ?? string.Empty);
            string sentPrompt = effectivePrompt ?? string.Empty;
            try
            {
                using (var form = new MultipartFormDataContent())
                {
                    form.Add(new StringContent(model ?? string.Empty, Encoding.UTF8), "model");
                    var labels = new StringBuilder();
                    string styleAnchor = BuildImageStyleAnchor(customStyleHint, style);
                    if (!string.IsNullOrWhiteSpace(styleAnchor)) labels.AppendLine(styleAnchor);
                    labels.AppendLine(VisualFidelityRules.ReferenceRepaint);
                    labels.AppendLine().AppendLine(effectivePrompt ?? string.Empty);
                    if (!string.IsNullOrWhiteSpace(quality)) form.Add(new StringContent(quality, Encoding.UTF8), "quality");
                    if (string.IsNullOrWhiteSpace(customStyleHint) && !string.IsNullOrWhiteSpace(style)) labels.Append("\n画风要求：").Append(style);
                    if (!string.IsNullOrWhiteSpace(size)) form.Add(new StringContent(size, Encoding.UTF8), "size");
                    form.Add(new StringContent("1"), "n");

                    int sent = 0;
                    foreach (var reference in referenceImages)
                    {
                        // PrepareReferenceImages already normalized and validated every image.
                        byte[] bytes = Convert.FromBase64String(reference.Base64Image);
                        labels.Append("\n参考图 ").Append(sent + 1).Append("（reference_").Append(sent).Append(".png）：").Append(VisualFidelityRules.ReferenceRoleInstruction(reference.Kind)).Append(" ").Append(reference.Label);
                        var imageContent = new ByteArrayContent(bytes);
                        imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
                        form.Add(imageContent, "image[]", $"reference_{sent}.png");
                        sent++;
                    }
                    if (sent == 0) return (false, null, null, "no usable reference images", false, sentPrompt);
                    // Identity-reference redraw is not a masked local repair. Do not synthesize a mask:
                    // an all-transparent mask does not provide identity-only conditioning.
                    sentPrompt = labels.ToString();
                    form.Add(new StringContent(sentPrompt, Encoding.UTF8), "prompt");

                    using (var request = new HttpRequestMessage(HttpMethod.Post, editsUrl) { Content = form })
                    {
                        if (!string.IsNullOrWhiteSpace(apiKey))
                        {
                            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                        }
                        Log($"[Illustrator] Requesting image edit from {SensitiveLogText.SafeUrl(editsUrl)} (model={model}, protocol=ImagesEdits, ActualRefImages={sent})...", apiKey);
                        if (GenerationDiagnostics.Current != null) await GenerationDiagnostics.Current.RecordImageRequestAsync(request, "ImagesEdits").ConfigureAwait(false);
                        using (var response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false))
                        {
                            string responseText = Encoding.UTF8.GetString(await ImagePayload.ReadBoundedAsync(response.Content, ImagePayload.MaxResponseBytes, cancellationToken).ConfigureAwait(false));
                            GenerationDiagnostics.Current?.RecordImageResponse(responseText, (int)response.StatusCode);
                            if (!response.IsSuccessStatusCode)
                            {
                                return (false, null, null, ExtractErrorMessage(responseText, (int)response.StatusCode), IsUnsupportedEditEndpoint((int)response.StatusCode, responseText), sentPrompt);
                            }
                            var extracted = await ExtractImageAsync(responseText, cancellationToken).ConfigureAwait(false);
                            if (extracted != null && extracted.Bytes != null && extracted.Bytes.Length > 0)
                            {
                                return (true, extracted.Bytes, extracted.Url, null, false, sentPrompt);
                            }
                            return (false, null, null, DescribeMissingImageResponse(responseText), false, sentPrompt);
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
                return (false, null, null, "images/edits request failed: " + ex.Message, false, sentPrompt);
            }
        }

        private static async Task<(bool Success, byte[] ImageBytes, string ImageUrl, string ErrorMessage, bool ShouldFallbackToChat, string ResolvedPrompt)> AttemptGenerateOnceAsync(
            string endpointUrl,
            string model,
            string effectivePrompt,
            string size,
            string quality,
            string style,
            System.Collections.Generic.IReadOnlyList<IllustrationReferenceImage> referenceImages,
            string apiKey,
            bool isChatProtocol,
            CancellationToken cancellationToken,
            string customStyleHint = null)
        {
            JObject payload;
            int actualRefImages = 0;
            bool hadReferences = referenceImages != null && referenceImages.Count > 0;
            referenceImages = PrepareReferenceImages(referenceImages, cancellationToken);
            if (hadReferences && (referenceImages == null || referenceImages.Count == 0))
                return (false, null, null, "没有可用的参考图；请求未发送。", false, effectivePrompt ?? string.Empty);
            string sentPrompt = effectivePrompt ?? string.Empty;
            if (isChatProtocol)
            {
                JToken messageContent;
                bool hasRefs = referenceImages != null && referenceImages.Count > 0;
                if (hasRefs)
                {
                    var content = new JArray
                    {
                        new JObject { ["type"] = "text", ["text"] = BuildImageStyleAnchor(customStyleHint, style) },
                        new JObject { ["type"] = "text", ["text"] = VisualFidelityRules.ReferenceRepaint }
                    };

                    if (string.IsNullOrWhiteSpace((string)content[0]["text"])) content.RemoveAt(0);

                    // 1. 统计真实人物参考图数量
                    int heroCount = 0;
                    foreach (var r in referenceImages)
                    {
                        if (r == null || string.IsNullOrWhiteSpace(r.Base64Image)) continue;
                        if (r.Kind == IllustrationReferenceKind.Character)
                            heroCount++;
                    }

                    // 2. 将视觉参考图放在最前（多模态视觉模型优先感知）
                    int heroIndex = 0;
                    foreach (var reference in referenceImages)
                    {
                        if (reference == null || string.IsNullOrWhiteSpace(reference.Base64Image)) continue;
                        string label = reference.Label ?? string.Empty;
                        bool isEmblem = reference.Kind == IllustrationReferenceKind.Emblem;
                        bool isScene = reference.Kind == IllustrationReferenceKind.Scene || reference.Kind == IllustrationReferenceKind.ScenePanorama ||
                            reference.Kind == IllustrationReferenceKind.SceneViews || reference.Kind == IllustrationReferenceKind.MapConversationScene ||
                            reference.Kind == IllustrationReferenceKind.ScenePerspective;
                        bool isDetail = reference.Kind == IllustrationReferenceKind.CharacterDetail;
                        bool isHero = reference.Kind == IllustrationReferenceKind.Character || isDetail;

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

                        if (!string.IsNullOrWhiteSpace(label))
                        {
                            content.Add(new JObject
                            {
                                ["type"] = "text",
                                ["text"] = "【上图专属约束】" + label
                            });
                        }

                        if (reference.Kind == IllustrationReferenceKind.EventCharacter || reference.Kind == IllustrationReferenceKind.EventEmblem)
                        {
                            content.Add(new JObject
                            {
                                ["type"] = "text",
                                ["text"] = "【所选事件的可选参考，不指定画面主角】" + VisualFidelityRules.ReferenceRoleInstruction(reference.Kind)
                            });
                        }
                        else if (isHero)
                        {
                            if (!isDetail) heroIndex++;
                            string roleHint = isDetail ? "【同名人物头肩细节补充，不增加人物数量】" : (heroCount > 1)
                                ? $"【人物身份参考图 {heroIndex}】" : "【核心人物官方真实视觉基准图】";

                            string fidelityMandate = roleHint + "：" + VisualFidelityRules.CharacterAppearancePriority + "参考图不是画中画或额外人物。";
                            content.Add(new JObject
                            {
                                ["type"] = "text",
                                ["text"] = fidelityMandate
                            });
                        }
                        else if (isEmblem)
                        {
                            content.Add(new JObject
                            {
                                ["type"] = "text",
                                ["text"] = "【家族纹章图案样板】：上图仅为家族纹章图案标准样板；仅当画面中已有明确纹章载体（盾牌、纹章罩袍、背景旗帜或其他已确认的纹章穿戴物）时参照此图案绘制，绝不是人物相貌，也不可凭样图新增载体或在普通金属板甲胸甲表面硬印该图案！"
                            });
                        }
                        else if (isScene)
                        {
                            content.Add(new JObject
                            {
                                ["type"] = "text",
                                ["text"] = (reference.Kind == IllustrationReferenceKind.MapConversationScene ? "【地图对话单视角布景参考】" :
                                    reference.Kind == IllustrationReferenceKind.ScenePerspective ? "【场景普通透视结构参考】" :
                                    reference.Kind == IllustrationReferenceKind.SceneViews ? "【场景前后双视角参考】" : reference.Kind == IllustrationReferenceKind.ScenePanorama ? "【场景预制体全景参考】" : "【当前位置与环境定位参考】") +
                                    VisualFidelityRules.ReferenceRoleInstruction(reference.Kind) + " 按导演画风重绘，不复制UI或游戏渲染质感。"
                            });
                        }
                        actualRefImages++;
                    }

                    // 3. 追加详细场景与构图描述
                    content.Add(new JObject
                    {
                        ["type"] = "text",
                        ["text"] = effectivePrompt
                    });

                    // 4. 追加艺术重绘与画风铁律
                    string resolvedStyle = !string.IsNullOrWhiteSpace(customStyleHint) ? customStyleHint.Trim() : (!string.IsNullOrWhiteSpace(style) ? style.Trim() : null);
                    string styleClause = !string.IsNullOrWhiteSpace(resolvedStyle)
                        ? $"1. 严格遵循指定的画风要求（{resolvedStyle}），从零完整重绘整幅画面，统一处理人物、环境、材质、光照与透视；不要复制参考图像素、UI或游戏截图痕迹。\n"
                        : "1. 从零完整重绘整幅画面，统一处理人物、环境、材质、光照与透视；不要复制参考图像素、UI或游戏截图痕迹。\n";

                    content.Add(new JObject
                    {
                        ["type"] = "text",
                        ["text"] = "【最终呈现规范/Artistic Redraw & Fidelity Mandate】：\n" +
                                   styleClause +
                                   "2. 人物容貌与实际衣着装备以对应身份参考图为准，人物数量、动作、站位和环境按导演描述呈现。\n" +
                                   "3. 单幅完整艺术画卷（Single Unified Canvas）：整幅画面为单一完整画面，画面无画中画（No picture-in-picture）、无贴片小图或缩略图框（No inset reference boxes or thumbnails）、无角色设定立绘板（No character concept sheets or turnarounds）。\n" +
                                   "4. 姿态与互动遵循导演描述，与支撑物和空间关系保持物理一致。\n" +
                                   "5. 落实导演选定的场所、空间关系与构图；非具名艺术布景可补充与主题一致的材质、装饰和光影细节，真实现场仅保留有依据的内容。背景须保留导演设计的空间与环境细节。"
                    });

                    messageContent = content;
                }
                else
                {
                    string styleAnchor = BuildImageStyleAnchor(customStyleHint, style);
                    messageContent = string.IsNullOrWhiteSpace(styleAnchor)
                        ? (JToken)effectivePrompt
                        : (JToken)(styleAnchor + "\n" + effectivePrompt);
                }
                sentPrompt = ExtractChatPromptText(messageContent);

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

                string styleAnchor = BuildImageStyleAnchor(customStyleHint, style);
                if (!string.IsNullOrWhiteSpace(styleAnchor))
                {
                    payload["prompt"] = styleAnchor + "\n" + payload["prompt"];
                    sentPrompt = (string)payload["prompt"];
                }
            }

            using (var request = new HttpRequestMessage(HttpMethod.Post, endpointUrl))
            {
                request.Content = new StringContent(payload.ToString(Formatting.None), Encoding.UTF8, "application/json");

                if (!string.IsNullOrWhiteSpace(apiKey))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                }

                Log($"[Illustrator] Requesting image generation from {SensitiveLogText.SafeUrl(endpointUrl)} (model={model}, protocol={(isChatProtocol ? "Chat" : "Images")}, refImages={referenceImages?.Count ?? 0}, ActualRefImages={actualRefImages})...", apiKey);
                if (GenerationDiagnostics.Current != null) await GenerationDiagnostics.Current.RecordImageRequestAsync(request, isChatProtocol ? "Chat" : "Images").ConfigureAwait(false);

                using (var response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false))
                {
                        string responseText = Encoding.UTF8.GetString(await ImagePayload.ReadBoundedAsync(response.Content, ImagePayload.MaxResponseBytes, cancellationToken).ConfigureAwait(false));
                        GenerationDiagnostics.Current?.RecordImageResponse(responseText, (int)response.StatusCode);

                        if (!response.IsSuccessStatusCode)
                        {
                            string errorMsg = ExtractErrorMessage(responseText, (int)response.StatusCode);
                            Log($"[Illustrator] Request failed: HTTP {(int)response.StatusCode}; details in request diagnostics.");

                            bool fallback = false;
                            if (!isChatProtocol && (
                                errorMsg.IndexOf("not supported on /v1/images/generations", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                errorMsg.IndexOf("field messages is required", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                errorMsg.IndexOf("not support", StringComparison.OrdinalIgnoreCase) >= 0))
                            {
                                fallback = true;
                            }

                            return (false, null, null, errorMsg, fallback, sentPrompt);
                        }

                        var extracted = await ExtractImageAsync(responseText, cancellationToken).ConfigureAwait(false);
                        if (extracted != null && extracted.Bytes != null && extracted.Bytes.Length > 0)
                        {
                            return (true, extracted.Bytes, extracted.Url, null, false, sentPrompt);
                        }

                        return (false, null, null, DescribeMissingImageResponse(responseText), false, sentPrompt);
                    }
            }
        }

        private static string ExtractChatPromptText(JToken messageContent)
        {
            if (messageContent == null) return string.Empty;
            if (messageContent.Type == JTokenType.String) return messageContent.Value<string>() ?? string.Empty;
            var content = messageContent as JArray;
            if (content == null) return string.Empty;
            var text = new StringBuilder();
            foreach (var part in content)
            {
                if (part?["type"]?.Value<string>() != "text") continue;
                string value = part["text"]?.Value<string>();
                if (value == null) continue;
                if (text.Length > 0) text.AppendLine();
                text.Append(value);
            }
            return text.ToString();
        }

        private sealed class ExtractedImage
        {
            public byte[] Bytes { get; set; }
            public string Url { get; set; }
        }

        private static bool IsUnsupportedEditEndpoint(int status, string body)
        {
            if (status == 404 || status == 405 || status == 501) return true;
            string message = ExtractErrorMessage(body, status);
            return status == 400 && (message.IndexOf("not support", StringComparison.OrdinalIgnoreCase) >= 0 ||
                message.IndexOf("unsupported", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static string DescribeMissingImageResponse(string responseText)
        {
            if (string.IsNullOrWhiteSpace(responseText)) return FormatMissingImageError("服务端返回空响应体");
            string reason = "响应未包含可识别的图片数据";
            try
            {
                var parsed = JObject.Parse(responseText);
                var choice = (parsed["choices"] as JArray)?.First;
                var message = choice?["message"];
                if (!string.IsNullOrWhiteSpace(message?["refusal"]?.ToString()) || choice?["finish_reason"]?.ToString() == "content_filter")
                    reason = "服务端拒绝生成图片或触发内容过滤";
                else if (choice?["finish_reason"]?.ToString() == "length")
                    reason = "服务端输出达到长度上限，未取得图片";
                else if (message != null && string.IsNullOrWhiteSpace(message["content"]?.ToString()) &&
                    !(message["images"] is JArray images && images.Count > 0))
                    reason = "服务端返回空回复，未提供文字或图片";
                else if (!string.IsNullOrWhiteSpace(message?["content"]?.ToString()))
                    reason = "服务端返回了内容，但未能提取图片；请检查响应格式或模型输出";
            }
            catch (JsonException) { }
            // The complete sanitized response was recorded before parsing. Keep provider
            // JSON out of the in-game status overlay; use the diagnostic suffix to find it.
            return FormatMissingImageError(reason);
        }

        private static string FormatMissingImageError(string reason)
        {
            string id = GenerationDiagnostics.Current?.Id;
            string diagnostic = string.IsNullOrEmpty(id) ? string.Empty :
                " 诊断：" + id.Substring(Math.Max(0, id.Length - 8));
            return reason + "；未自动重试，可稍后手动重绘。" + diagnostic;
        }

        private static async Task<ExtractedImage> ExtractImageAsync(string responseText, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(responseText)) return null;
            JObject parsed = JObject.Parse(responseText);
            if (parsed["data"] is JArray dataArray)
                foreach (var item in dataArray)
                {
                    var image = await ExtractImageTokenAsync(item, cancellationToken).ConfigureAwait(false);
                    if (image?.Bytes?.Length > 0) return image;
                }
            if (parsed["choices"] is JArray choices)
                foreach (var choice in choices)
                {
                    var message = choice["message"];
                    if (message == null) continue;
                    if (message["images"] is JArray images)
                        foreach (var item in images)
                        {
                            var image = await ExtractImageTokenAsync(item, cancellationToken).ConfigureAwait(false);
                            if (image?.Bytes?.Length > 0) return image;
                        }
                    if (message["content"] is JArray blocks)
                    {
                        foreach (var block in blocks)
                        {
                            var image = await ExtractImageTokenAsync(block, cancellationToken).ConfigureAwait(false);
                            if (image?.Bytes?.Length > 0) return image;
                        }
                    }
                    else
                    {
                        var image = await ExtractImageTextAsync(message["content"]?.ToString(), cancellationToken).ConfigureAwait(false);
                        if (image?.Bytes?.Length > 0) return image;
                    }
                }
            return null;
        }

        private static async Task<ExtractedImage> ExtractImageTokenAsync(JToken token, CancellationToken cancellationToken)
        {
            if (token is JObject obj)
            {
                var imageUrl = obj["image_url"];
                string[] candidates = {
                    obj["b64_json"]?.ToString(), obj["base64"]?.ToString(),
                    imageUrl is JObject imageObject ? imageObject["url"]?.ToString() : imageUrl?.ToString(),
                    obj["url"]?.ToString()
                };
                foreach (string candidate in candidates)
                {
                    try
                    {
                        var image = await ParseUriOrBase64Async(candidate, cancellationToken).ConfigureAwait(false);
                        if (image?.Bytes?.Length > 0) return image;
                    }
                    catch (InvalidDataException) { }
                    catch (ArgumentException) { }
                    catch (FormatException) { } // 一个坏候选不能遮住同响应的有效图。
                }
                return await ExtractImageTextAsync(obj["text"]?.ToString(), cancellationToken).ConfigureAwait(false);
            }
            if (token is JValue)
                return await ExtractImageTextAsync(token.ToString(), cancellationToken, true).ConfigureAwait(false);
            return null;
        }

        private static async Task<ExtractedImage> ExtractImageTextAsync(string content, CancellationToken cancellationToken, bool allowBase64 = false)
        {
            if (string.IsNullOrWhiteSpace(content)) return null;
            content = content.Trim();
            foreach (Match match in Regex.Matches(content, @"!\[.*?\]\((https?://[^\s\)]+|data:image/[^;]+;base64,[^\s\)]+)\)"))
            {
                try
                {
                    var image = await ParseUriOrBase64Async(match.Groups[1].Value, cancellationToken).ConfigureAwait(false);
                    if (image?.Bytes?.Length > 0) return image;
                }
                catch (InvalidDataException) { }
                    catch (ArgumentException) { }
                    catch (FormatException) { }
            }
            if (allowBase64 || content.StartsWith("data:image", StringComparison.OrdinalIgnoreCase) ||
                content.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || content.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                try { return await ParseUriOrBase64Async(content, cancellationToken).ConfigureAwait(false); }
                catch (InvalidDataException) { }
                    catch (ArgumentException) { }
                    catch (FormatException) { }
            return null;
        }

        private static async Task<ExtractedImage> ParseUriOrBase64Async(string uriOrBase64, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(uriOrBase64)) return null;

            cancellationToken.ThrowIfCancellationRequested();
            if (uriOrBase64.Length > ImagePayload.MaxResponseBytes) throw new InvalidDataException("图片编码超过限制。");
            uriOrBase64 = uriOrBase64.Trim();
            if (uriOrBase64.StartsWith("data:image", StringComparison.OrdinalIgnoreCase))
            {
                int commaIdx = uriOrBase64.IndexOf(',');
                string b64 = commaIdx >= 0 ? uriOrBase64.Substring(commaIdx + 1) : uriOrBase64;
                return new ExtractedImage { Bytes = ImagePayload.Normalize(Convert.FromBase64String(b64.Trim())) };
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
                    return new ExtractedImage { Bytes = ImagePayload.Normalize(bytes) };
                }
            }
            catch
            {
            }

            return null;
        }

        private const int MaxImageDownloadRedirects = 3;

        private static async Task<byte[]> DownloadImageBytesAsync(string url, CancellationToken cancellationToken)
        {
            try
            {
                if (!Uri.TryCreate(url, UriKind.Absolute, out Uri current) ||
                    !await IsSafeImageDownloadUriAsync(current, cancellationToken).ConfigureAwait(false))
                {
                    Log($"[Illustrator] Refused unsafe generated-image URL: {SensitiveLogText.SafeUrl(url)}");
                    return null;
                }

                // The URL comes from a provider response rather than the user's configured API
                // endpoint. Disable the process proxy and automatic redirects so a provider cannot
                // turn an apparently public image URL into a request to a local or private target.
                using (var handler = new HttpClientHandler { AllowAutoRedirect = false, UseProxy = false })
                using (var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(60) })
                {
                    for (int redirect = 0; redirect <= MaxImageDownloadRedirects; redirect++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        if (!await IsSafeImageDownloadUriAsync(current, cancellationToken).ConfigureAwait(false))
                        {
                            Log($"[Illustrator] Refused unsafe generated-image redirect: {SensitiveLogText.SafeUrl(current.ToString())}");
                            return null;
                        }

                        using (var request = new HttpRequestMessage(HttpMethod.Get, current))
                        using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false))
                        {
                            if (IsRedirectStatus(response.StatusCode))
                            {
                                if (redirect >= MaxImageDownloadRedirects || response.Headers.Location == null)
                                {
                                    Log($"[Illustrator] Refused generated-image redirect chain from {SensitiveLogText.SafeUrl(url)}");
                                    return null;
                                }

                                Uri next = response.Headers.Location.IsAbsoluteUri
                                    ? response.Headers.Location
                                    : new Uri(current, response.Headers.Location);
                                if (!await IsSafeImageDownloadUriAsync(next, cancellationToken).ConfigureAwait(false))
                                {
                                    Log($"[Illustrator] Refused unsafe generated-image redirect: {SensitiveLogText.SafeUrl(next.ToString())}");
                                    return null;
                                }
                                current = next;
                                continue;
                            }

                            response.EnsureSuccessStatusCode();
                            return ImagePayload.Normalize(await ImagePayload.ReadBoundedAsync(response.Content, ImagePayload.MaxBytes, cancellationToken).ConfigureAwait(false));
                        }
                    }
                    return null;
                }
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                Log($"[Illustrator] Failed to download generated image from {SensitiveLogText.SafeUrl(url)}: {ex.GetType().Name}");
                return null;
            }
        }

        private static bool IsRedirectStatus(HttpStatusCode statusCode)
        {
            return statusCode == HttpStatusCode.MovedPermanently || statusCode == HttpStatusCode.Found ||
                statusCode == HttpStatusCode.SeeOther || statusCode == HttpStatusCode.TemporaryRedirect ||
                (int)statusCode == 308;
        }

        private static async Task<bool> IsSafeImageDownloadUriAsync(Uri uri, CancellationToken cancellationToken)
        {
            if (uri == null || !uri.IsAbsoluteUri ||
                (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
                 !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)) ||
                string.IsNullOrWhiteSpace(uri.Host) || !string.IsNullOrEmpty(uri.UserInfo))
            {
                return false;
            }

            string host = uri.DnsSafeHost?.TrimEnd('.');
            if (string.IsNullOrWhiteSpace(host) || IsLocalImageHostName(host)) return false;
            cancellationToken.ThrowIfCancellationRequested();

            if (IPAddress.TryParse(host, out IPAddress literal))
                return !IsDisallowedImageAddress(literal);

            IPAddress[] addresses;
            try
            {
                addresses = await Dns.GetHostAddressesAsync(host).ConfigureAwait(false);
            }
            catch
            {
                return false;
            }

            cancellationToken.ThrowIfCancellationRequested();
            if (addresses == null || addresses.Length == 0) return false;
            foreach (IPAddress address in addresses)
                if (IsDisallowedImageAddress(address)) return false;
            return true;
        }

        private static bool IsLocalImageHostName(string host)
        {
            string normalized = (host ?? string.Empty).TrimEnd('.').ToLowerInvariant();
            return normalized == "localhost" || normalized.EndsWith(".localhost", StringComparison.Ordinal) ||
                normalized.EndsWith(".local", StringComparison.Ordinal) || normalized.EndsWith(".internal", StringComparison.Ordinal) ||
                normalized.EndsWith(".lan", StringComparison.Ordinal) || normalized == "metadata.google.internal" ||
                normalized == "metadata";
        }

        private static bool IsDisallowedImageAddress(IPAddress address)
        {
            if (address == null) return true;
            if (address.IsIPv4MappedToIPv6) address = address.MapToIPv4();
            if (IPAddress.IsLoopback(address) || address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any) ||
                address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || address.IsIPv6Multicast)
                return true;

            byte[] bytes = address.GetAddressBytes();
            if (bytes.Length == 16)
                return (bytes[0] & 0xFE) == 0xFC; // IPv6 unique-local fc00::/7.
            if (bytes.Length != 4) return true;

            int first = bytes[0], second = bytes[1], third = bytes[2];
            return first == 0 || first == 10 || (first == 100 && second >= 64 && second <= 127) ||
                (first == 169 && second == 254) || (first == 172 && second >= 16 && second <= 31) ||
                (first == 192 && second == 168) || (first == 192 && second == 0 && third == 0) ||
                (first == 198 && second >= 18 && second <= 19) || (first == 198 && second == 51 && third == 100) ||
                (first == 203 && second == 0 && third == 113) || first >= 224;
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
                    directives.Add("high rendering quality, coherent detail appropriate to the selected artistic medium");
                }
                else if (quality.Equals("standard", StringComparison.OrdinalIgnoreCase) || quality.Equals("auto", StringComparison.OrdinalIgnoreCase))
                {
                    directives.Add("standard definition quality");
                }
                else if (quality.Equals("medium", StringComparison.OrdinalIgnoreCase))
                {
                    directives.Add("balanced rendering quality appropriate to the selected artistic medium");
                }
                else if (quality.Equals("low", StringComparison.OrdinalIgnoreCase))
                {
                    directives.Add("fast draft quality");
                }
            }

            if (string.IsNullOrWhiteSpace(customStyleHint) && !string.IsNullOrWhiteSpace(style))
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

        private static void Log(string message, string secret = null)
        {
            TaleWorlds.Library.Debug.Print(SensitiveLogText.Redact(message, secret));
        }
    }
}
