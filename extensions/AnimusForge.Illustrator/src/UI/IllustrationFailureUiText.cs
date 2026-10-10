using System;

namespace AnimusForge.Illustrator.UI
{
    // Display-only projection. Keep worker errors, diagnostic records and provider details intact.
    internal static class IllustrationFailureUiText
    {
        internal static string Project(string raw)
            => Project(raw, unwrapCommunication: true);

        private static string Project(string raw, bool unwrapCommunication)
        {
            switch (raw)
            {
                case "百科完整装备离屏立绘未取得，已停止生成；请查看本次 portrait_stage_result 的 failureCode。":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_PORTRAIT_REFERENCE", "The encyclopedia's complete equipped portrait could not be captured off-screen. Generation stopped; check failureCode in this request's portrait_stage_result.");
                case "玩家完整装备离屏立绘失败，已停止生成。":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_PLAYER_REFERENCE", "The player's complete equipped portrait could not be captured off-screen. Generation stopped.");
                case "对方完整装备离屏立绘失败，已停止生成。":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_PARTNER_REFERENCE", "The other character's complete equipped portrait could not be captured off-screen. Generation stopped.");
                case "独立场景未返回完整的全景参考图。":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_PROBE_MISSING", "The isolated scene capture did not return a complete panorama reference.");
                case "独立场景返回了空的参考拼图。":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_PROBE_EMPTY", "The isolated scene capture returned an empty reference composite.");
                case "游戏线程任务队列已满，无法执行插画采集操作。":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_CAPTURE_QUEUE", "The game-thread task queue is full. Illustration capture could not run.");
                case "AI 生图系统未启用":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_DISABLED", "AI image generation is disabled.");
                case "未配置生图 API 端点 (Base URL)":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_ENDPOINT_MISSING", "No image API endpoint (Base URL) is configured.");
                case "当前没有可供截图生图的玩家场景，或生图尚未启用。":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_MISSION_UNAVAILABLE", "No player scene is available for screenshot illustration, or image generation is disabled.");
                case "请先配置生图 API。":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_CONFIGURE_API", "Configure the image API first.");
                case "基于本图重绘需要一张有效的当前成图，未发送请求。":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_REDRAW_SOURCE", "Redrawing from this image requires a valid current image. No request was sent.");
                case "images/edits 端点需要可用的参考图；请开启参考图并取得人物或场景参考后再生成。未发送请求。":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_EDITS_REFERENCE", "The images/edits endpoint requires a usable reference image. Enable references and capture a character or scene reference before generating. No request was sent.");
                case "精确 images/generations 端点不能携带参考图；请改用 /images/edits 或开启对话多模态生图通道。未发送请求。":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_GENERATIONS_REFERENCE", "The exact images/generations endpoint cannot accept reference images. Use /images/edits or enable the conversation multimodal image channel. No request was sent.");
                case "未能从服务端响应中提取到有效图像数据":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_RESPONSE_IMAGE", "No valid image data could be extracted from the server response.");
                case "生图请求已取消":
                    return IllustratorUiText.Resolve("AF_ILL_ERROR_CANCELLED", "The image generation request was cancelled.");
                case "未能保存图像":
                case "未能保存图片":
                case "无法保存":
                    return IllustratorUiText.WeeklyNotSaved;
                case "未能保存或显示图片":
                    return IllustratorUiText.CardNotSavedOrDisplayed;
            }

            const string timeoutPrefix = "生图请求超过";
            const string timeoutSuffix = "秒总预算";
            if (raw != null && raw.StartsWith(timeoutPrefix, StringComparison.Ordinal)
                && raw.EndsWith(timeoutSuffix, StringComparison.Ordinal))
            {
                string seconds = raw.Substring(timeoutPrefix.Length, raw.Length - timeoutPrefix.Length - timeoutSuffix.Length);
                if (IsBudget(seconds))
                    return IllustratorUiText.Format("AF_ILL_ERROR_TIMEOUT", "The image generation request exceeded its total budget of {SECONDS} seconds.", "SECONDS", seconds);
            }
            const string communicationPrefix = "生图通信异常: ";
            if (unwrapCommunication && raw != null && raw.StartsWith(communicationPrefix, StringComparison.Ordinal))
                return IllustratorUiText.Format("AF_ILL_ERROR_COMMUNICATION", "Image generation communication error: {DETAIL}", "DETAIL", Project(raw.Substring(communicationPrefix.Length), unwrapCommunication: false));
            return raw;
        }

        private static bool IsBudget(string value)
        {
            if (string.IsNullOrEmpty(value)) return false;
            for (int i = 0; i < value.Length; i++)
                if (value[i] < '0' || value[i] > '9') return false;
            return true;
        }
    }
}
