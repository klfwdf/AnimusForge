using System;
using AnimusForge.Illustrator.Engine;

namespace AnimusForge.Illustrator.Core
{
    // One detached edit job; no Hero, Mission, director or scene capture is consulted.
    internal static class CurrentImageRedraw
    {
        internal sealed class Completion
        {
            internal ImageGenerationResult Result;
            internal CachedIllustrationItem Saved;
        }

        internal static bool Start(IllustrationScope scope, CachedIllustrationItem source,
            string instruction, IllustrationOptions options, string sessionKey,
            Action<Completion> complete, Action<string> fail)
        {
            if (source == null || scope == null || !scope.IsCurrent || source.CampaignKey != scope.CampaignKey
                || string.IsNullOrWhiteSpace(instruction) || options == null)
            { fail("当前成图或请求已失效，未发送请求。"); return false; }
            var snapshot = source.CopyMetadata();
            byte[] sourceBytes = source.ImageData;
            var editOptions = options.ForCurrentImageEdit();
            return scope.RunGeneration(snapshot.SubjectKey, sessionKey, async token =>
            {
                token.ThrowIfCancellationRequested();
                // Generated images are immutable; read at most once, from the selected image only.
                byte[] bytes = ImagePayload.Normalize(sourceBytes ?? ImagePayload.ReadEncodedFile(snapshot.FilePath, token));
                var references = new[] { new IllustrationReferenceImage(Convert.ToBase64String(bytes),
                    "待编辑的当前成图", IllustrationReferenceKind.GeneratedImage) };
                var result = await UniversalOpenAiImageClient.GenerateImageAsync(instruction, references, editOptions, token).ConfigureAwait(false);
                CachedIllustrationItem saved = null;
                if (result.Success && result.ImageBytes != null)
                {
                    token.ThrowIfCancellationRequested();
                    saved = DiskImageCacheManager.SaveImageWithEditMetadata(snapshot.SubjectKey, result.ImageBytes,
                        result.ResolvedPrompt, snapshot.Title, snapshot.Category, snapshot.CampaignKey,
                        editOptions.MaxCacheCount, makeDefault: false, allowImplicitDefault: false,
                        theme: snapshot.Theme, actionSummary: snapshot.ActionSummary, diagnosticId: result.DiagnosticId,
                        styleFingerprint: snapshot.StyleFingerprint, sourceImageKey: snapshot.Key,
                        editInstruction: instruction, generationMode: "current_image_edit");
                    if (saved != null) DiskImageCacheManager.PromoteDefaultIfNewest(saved, snapshot.CampaignKey);
                }
                return new Completion { Result = result, Saved = saved };
            }, complete, fail, value => value.Saved, value => value.Result?.ErrorMessage ?? "未能保存图片");
        }
    }
}
