using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using Newtonsoft.Json.Linq;

namespace AnimusForge.Illustrator.Core
{
    // Pure per-request translation; no game objects, configuration reads or network probing.
    internal static class GeminiNativeImageProtocol
    {
        private static readonly HashSet<string> SupportedRatios = new HashSet<string>(StringComparer.Ordinal)
        { "1:1", "2:3", "3:2", "3:4", "4:3", "4:5", "5:4", "9:16", "16:9", "21:9" };

        internal static bool IsNativeUrl(string url)
        {
            if (!Uri.TryCreate((url ?? string.Empty).Trim(), UriKind.Absolute, out var uri)
                || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)) return false;
            string path = uri.AbsolutePath.TrimEnd('/');
            if (path.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith("/images/generations", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith("/images/edits", StringComparison.OrdinalIgnoreCase)
                || path.IndexOf("/openai", StringComparison.OrdinalIgnoreCase) >= 0) return false;
            return path.EndsWith(":generateContent", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith(":streamGenerateContent", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith("/v1beta", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith("/v1beta/models", StringComparison.OrdinalIgnoreCase)
                || (string.Equals(uri.Host, "generativelanguage.googleapis.com", StringComparison.OrdinalIgnoreCase)
                    && (path.Length == 0 || path == "/v1" || path == "/v1/models"));
        }

        internal static string ResolveEndpoint(string url, string model)
        {
            var uri = new Uri((url ?? string.Empty).Trim(), UriKind.Absolute);
            string path = uri.AbsolutePath.TrimEnd('/');
            if (path.EndsWith(":streamGenerateContent", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Native Gemini image generation requires the non-streaming :generateContent endpoint. No request was sent.");
            if (path.EndsWith(":generateContent", StringComparison.OrdinalIgnoreCase)) return uri.ToString();
            model = (model ?? string.Empty).Trim();
            if (model.StartsWith("models/", StringComparison.Ordinal)) model = model.Substring(7);
            if (model.Length == 0) throw new InvalidOperationException("Native Gemini image generation requires a model name. No request was sent.");
            foreach (char c in model)
                if (!(char.IsLetterOrDigit(c) || c == '-' || c == '_' || c == '.'))
                    throw new InvalidOperationException("The native Gemini model name is invalid. No request was sent.");
            if (path.Length == 0) path = "/v1beta";
            if (!path.EndsWith("/models", StringComparison.OrdinalIgnoreCase)) path += "/models";
            return new UriBuilder(uri) { Path = path + "/" + model + ":generateContent", Fragment = "" }.Uri.ToString();
        }

        internal static HttpRequestMessage CreateModelListRequest(string url, string apiKey)
        {
            var uri = new Uri(url, UriKind.Absolute);
            string path = uri.AbsolutePath.TrimEnd('/');
            int models = path.LastIndexOf("/models/", StringComparison.OrdinalIgnoreCase);
            if (models >= 0) path = path.Substring(0, models) + "/models";
            else
            {
                if (path.Length == 0) path = "/v1beta";
                if (!path.EndsWith("/models", StringComparison.OrdinalIgnoreCase)) path += "/models";
            }
            var request = new HttpRequestMessage(HttpMethod.Get, new UriBuilder(uri) { Path = path, Fragment = "" }.Uri);
            try
            {
                if (!string.IsNullOrWhiteSpace(apiKey)) request.Headers.Add("x-goog-api-key", apiKey);
                return request;
            }
            catch { request.Dispose(); throw; }
        }

        internal static List<string> ReadModelIds(JObject response)
        {
            var result = new List<string>();
            if (!(response["models"] is JArray models)) return result;
            foreach (var model in models)
            {
                string name = (string)model["name"];
                if (string.IsNullOrWhiteSpace(name)) continue;
                if (name.StartsWith("models/", StringComparison.Ordinal)) name = name.Substring(7);
                result.Add(name);
            }
            return result;
        }

        internal static string EndpointModel(string endpoint)
        {
            string path = new Uri(endpoint, UriKind.Absolute).AbsolutePath;
            string last = path.Substring(path.LastIndexOf('/') + 1);
            return Uri.UnescapeDataString(last.Substring(0, last.Length - ":generateContent".Length));
        }

        internal static string ExactAspectRatio(string size)
        {
            string[] dimensions = (size ?? string.Empty).Trim().ToLowerInvariant().Split(new[] { 'x', '*', ':' });
            if (dimensions.Length != 2 || !int.TryParse(dimensions[0], out int width)
                || !int.TryParse(dimensions[1], out int height) || width <= 0 || height <= 0) return null;
            int a = width, b = height;
            while (b != 0) { int remainder = a % b; a = b; b = remainder; }
            return (width / a) + ":" + (height / a);
        }

        internal static JObject BuildImageConfig(string model, string size, bool chatCompatible)
        {
            string name = (model ?? string.Empty).ToLowerInvariant();
            if (chatCompatible && name.IndexOf("gemini", StringComparison.Ordinal) < 0) return null;
            var config = new JObject();
            string ratio = ExactAspectRatio(size);
            if (ratio != null && SupportedRatios.Contains(ratio))
                config[chatCompatible ? "aspect_ratio" : "aspectRatio"] = ratio;
            // 2.5 image and 3.1 Flash Lite image do not offer higher output tiers.
            bool variableResolution = (name.Contains("gemini-3") || name.Contains("gemini-nano-banana")) && !name.Contains("flash-lite");
            string[] dimensions = (size ?? string.Empty).Split('x');
            if (variableResolution && dimensions.Length == 2 && int.TryParse(dimensions[0], out int width)
                && int.TryParse(dimensions[1], out int height) && width > 0 && height > 0)
            {
                int longest = Math.Max(width, height);
                config[chatCompatible ? "image_size" : "imageSize"] = longest <= 1440 ? "1K" : longest <= 2560 ? "2K" : "4K";
            }
            return config.Count > 0 ? config : null;
        }

        internal static JObject ConvertPayload(JObject chatPayload, string size)
        {
            var messages = chatPayload["messages"] as JArray;
            if (messages?.Count != 1 || (string)messages[0]["role"] != "user")
                throw new InvalidOperationException("The native Gemini image request is missing complete user content. No request was sent.");
            var parts = new JArray();
            JToken content = messages[0]["content"];
            if (content?.Type == JTokenType.String) parts.Add(new JObject { ["text"] = content.Value<string>() });
            else if (content is JArray blocks)
            {
                foreach (var block in blocks)
                {
                    if ((string)block["type"] == "text") parts.Add(new JObject { ["text"] = (string)block["text"] ?? string.Empty });
                    else if ((string)block["type"] == "image_url")
                    {
                        string image = (string)block["image_url"]?["url"];
                        const string prefix = "data:image/png;base64,";
                        if (image == null || !image.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                            throw new InvalidDataException("Native Gemini reference images must be validated PNG data. No request was sent.");
                        parts.Add(new JObject { ["inlineData"] = new JObject { ["mimeType"] = "image/png", ["data"] = image.Substring(prefix.Length) } });
                    }
                    else throw new InvalidOperationException("The native Gemini image request contains an unsupported content block. No request was sent.");
                }
            }
            if (parts.Count == 0) throw new InvalidOperationException("The native Gemini image request is empty. No request was sent.");
            var config = new JObject { ["responseModalities"] = new JArray("TEXT", "IMAGE") };
            var imageConfig = BuildImageConfig((string)chatPayload["model"], size, false);
            if (imageConfig != null) config["imageConfig"] = imageConfig;
            return new JObject { ["contents"] = new JArray(new JObject { ["role"] = "user", ["parts"] = parts }), ["generationConfig"] = config };
        }

        internal static IEnumerable<string> ImageData(JObject response)
        {
            if (response["promptFeedback"]?["blockReason"] != null || !(response["candidates"] is JArray candidates)) yield break;
            foreach (var candidate in candidates)
            {
                string finish = (string)candidate["finishReason"];
                if (!string.IsNullOrEmpty(finish) && finish != "STOP") continue;
                if (!(candidate["content"]?["parts"] is JArray parts)) continue;
                foreach (var part in parts)
                {
                    if (part["thought"]?.Value<bool>() == true) continue;
                    var inline = part["inlineData"] as JObject ?? part["inline_data"] as JObject;
                    string mime = (string)(inline?["mimeType"] ?? inline?["mime_type"]);
                    if (!string.Equals(mime, "image/png", StringComparison.OrdinalIgnoreCase)
                        && !string.Equals(mime, "image/jpeg", StringComparison.OrdinalIgnoreCase)) continue;
                    string data = (string)inline?["data"];
                    if (!string.IsNullOrWhiteSpace(data)) yield return data;
                }
            }
        }
    }
}
