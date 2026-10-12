using System;
using System.Collections.Generic;

namespace AnimusForge.Illustrator.Core
{
    // This contract belongs only to the explicit player-directed screen-capture mode. No panorama/portrait routing.
    internal static class MissionScreenshotRules
    {
        internal static readonly string ShoutMode = IllustratorText.Raw("ScreenshotRules_836");
        internal static readonly string BattleMode = IllustratorText.Raw("ScreenshotRules_837");
        internal static readonly string SpatialContract =
            IllustratorText.Get("ScreenshotRules_838") +
            IllustratorText.Get("ScreenshotRules_839") +
            IllustratorText.Get("ScreenshotRules_840") +
            IllustratorText.Get("ScreenshotRules_841") +
            IllustratorText.Get("ScreenshotRules_842") +
            IllustratorText.Get("ScreenshotRules_843") +
            IllustratorText.Get("ScreenshotRules_844");
        internal static readonly string ShoutAction =
            IllustratorText.Get("ScreenshotRules_845") +
            IllustratorText.Get("ScreenshotRules_846") +
            IllustratorText.Get("ScreenshotRules_847");
        internal static readonly string BattleAction =
            IllustratorText.Get("ScreenshotRules_848") +
            IllustratorText.Get("ScreenshotRules_849");
        internal static readonly string DirectorSystem =
            IllustratorText.Get("ScreenshotRules_850") +
            SpatialContract + "\n" +
            IllustratorText.Get("ScreenshotRules_851") +
            IllustratorText.Get("ScreenshotRules_852") +
            IllustratorText.Get("ScreenshotRules_853") +
            IllustratorText.Get("ScreenshotRules_854");

        internal static bool IsMode(string mode) => mode == ShoutMode || mode == BattleMode;
        internal static string Contract(bool battle) => SpatialContract + "\n" + (battle ? BattleAction : ShoutAction);
        internal static void RequireReferences(IReadOnlyList<IllustrationReferenceImage> references)
        {
            if (references == null || references.Count < 1 || references.Count > 2)
                throw new InvalidOperationException("Free-camera generation requires one or two scene captures. No request was sent.");
            foreach (var reference in references)
                if (reference == null || reference.Kind != IllustrationReferenceKind.MissionScreenshot || string.IsNullOrWhiteSpace(reference.Base64Image))
                    throw new InvalidOperationException("Scene capture references are missing or have the wrong type. No request was sent.");
        }
    }
}
