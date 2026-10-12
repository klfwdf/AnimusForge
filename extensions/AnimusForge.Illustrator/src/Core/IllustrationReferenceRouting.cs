using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using AnimusForge.Illustrator.Engine;
using Newtonsoft.Json.Linq;

namespace AnimusForge.Illustrator.Core
{
    internal static class IllustrationReferenceRouting
    {
        // Every full-body capture uses the same native idle stance (StanceIndex 0). Name it,
        // so the image model treats the pose as a render artefact rather than a composition.
        internal static string IdleStanceFullBodyNote => IllustratorText.Get("Ref_IdleStanceFullBodyNote");

        // Stable reorder for the image endpoint only: idle-stance full-body renders go last,
        // so /images/edits and chat image models do not take the first image's silhouette as
        // the canvas. Director payloads keep the original order. Runs once per request.
        internal static IReadOnlyList<IllustrationReferenceImage> SendIdleStanceFullBodyLast(IReadOnlyList<IllustrationReferenceImage> references)
        {
            if (references == null || !references.Any(r => r != null && r.IsIdleStanceFullBody)) return references;
            var ordered = new List<IllustrationReferenceImage>(references.Count);
            ordered.AddRange(references.Where(r => r == null || !r.IsIdleStanceFullBody));
            ordered.AddRange(references.Where(r => r != null && r.IsIdleStanceFullBody));
            return ordered;
        }

        internal static void AddCharacter(List<IllustrationReferenceImage> director, List<IllustrationReferenceImage> image,
            CharacterPortraitReferences portraits, string name, string fullBodyLabel, bool eventReference = false)
        {
            if (portraits == null || string.IsNullOrWhiteSpace(portraits.FullBody)) return;
            var full = new IllustrationReferenceImage(portraits.FullBody, (fullBodyLabel ?? string.Empty) + IdleStanceFullBodyNote,
                eventReference ? IllustrationReferenceKind.EventCharacter : IllustrationReferenceKind.Character)
            { IsIdleStanceFullBody = true };
            director.Add(full);
            if (image != null && !ReferenceEquals(director, image)) image.Add(full);
            // Ignore legacy HeadDetail values as well: both model stages now receive one
            // complete portrait per character, with no redundant close-up image.
        }

        // Session snapshots may contain older head-detail references; do not re-upload them on redraw.
        internal static IEnumerable<IllustrationReferenceImage> SinglePortraitSessionReferences(IReadOnlyList<IllustrationReferenceImage> references)
        {
            return references == null ? Enumerable.Empty<IllustrationReferenceImage>()
                : references.Where(r => r != null && r.Kind != IllustrationReferenceKind.CharacterDetail);
        }

        internal static IllustrationReferenceImage SelectSceneAnchor(IReadOnlyList<IllustrationReferenceImage> scenes)
        {
            var scene = scenes?.FirstOrDefault(x => x != null &&
                (x.Kind == IllustrationReferenceKind.Scene || x.Kind == IllustrationReferenceKind.MapConversationScene) && !string.IsNullOrWhiteSpace(x.Base64Image));
            return scene == null ? null : new IllustrationReferenceImage(scene.Base64Image,
                scene.Label + (scene.Kind == IllustrationReferenceKind.MapConversationScene ? IllustratorText.Get("Ref_MapConversationSceneAnchor") : IllustratorText.Get("Ref_SceneAnchor")), scene.Kind);
        }

        internal static void AddSceneReferences(List<IllustrationReferenceImage> image, IReadOnlyList<IllustrationReferenceImage> scenes,
            IllustrationDirection direction, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            var current = SelectSceneAnchor(scenes);
            var panorama = scenes?.FirstOrDefault(x => x != null && x.Kind == IllustrationReferenceKind.ScenePanorama && !string.IsNullOrWhiteSpace(x.Base64Image));
            // Without a panorama (scene offscreen switched off) the screenshot is the only environment image, so the image side needs it too.
            bool sendCurrent = current != null && (current.Kind == IllustrationReferenceKind.MapConversationScene || panorama == null ||
                direction?.UsedLocalFallback == true || direction?.VisionUnsupported == true || direction?.UsedTextOnlyDirector == true);
            int sceneReferenceCount = 0;
            if (panorama != null)
            {
                var watch = Stopwatch.StartNew();
                bool selected = direction?.UsedLocalFallback == false && !direction.VisionUnsupported && !direction.UsedTextOnlyDirector && direction.SceneYawDegrees.HasValue &&
                    direction.ScenePitchDegrees.HasValue && direction.SceneHorizontalFovDegrees.HasValue;
                double yaw = selected ? direction.SceneYawDegrees.Value : 0;
                double pitch = selected ? direction.ScenePitchDegrees.Value : 0;
                // 35 mm-equivalent default on a full-frame camera is about 54-55°
                // horizontal FOV. Keep the director-selected value when present.
                double fov = selected ? direction.SceneHorizontalFovDegrees.Value : 55;
                // Internally produced base64 only. Bound allocation before decode.
                if (panorama.Base64Image.Length > 24 * 1024 * 1024)
                    throw new InvalidOperationException("The scene panorama exceeds its projection budget. Image generation stopped.");
                string auxiliarySelection = AuxiliarySelection(direction, selected);
                bool sendAuxiliary = auxiliarySelection == "director";
                byte[] panoramaBytes = Convert.FromBase64String(panorama.Base64Image);
                byte[][] perspectives = sendAuxiliary
                    ? ScenePerspectiveProjection.ProjectPair(panoramaBytes, yaw, pitch, fov,
                        direction.AuxiliarySceneYawDegrees.Value, direction.AuxiliaryScenePitchDegrees.Value,
                        direction.AuxiliarySceneHorizontalFovDegrees.Value, token)
                    : new[] { ScenePerspectiveProjection.Project(panoramaBytes, yaw, pitch, fov, token) };
                string sceneRole = IllustratorText.Get("Ref_SceneRole");
                image.Insert(sceneReferenceCount++, new IllustrationReferenceImage(Convert.ToBase64String(perspectives[0]),
                    (selected ? IllustratorText.Get("Ref_PerspectiveMain_Selected") : IllustratorText.Get("Ref_PerspectiveMain_Default")) + sceneRole,
                    IllustrationReferenceKind.ScenePerspective));
                if (sendAuxiliary)
                    image.Insert(sceneReferenceCount++, new IllustrationReferenceImage(Convert.ToBase64String(perspectives[1]),
                        IllustratorText.Get("Ref_PerspectiveAux") + sceneRole,
                        IllustrationReferenceKind.ScenePerspective));
                GenerationDiagnostics.Current?.RecordStage("scene_perspective_reference", new JObject
                {
                    ["selection"] = selected ? "director" : "default_front", ["yawDegrees"] = yaw,
                    ["pitchDegrees"] = pitch, ["horizontalFovDegrees"] = fov, ["width"] = 768, ["height"] = 768,
                    ["elapsedMs"] = watch.ElapsedMilliseconds, ["panoramaSentToImage"] = false,
                    ["auxiliarySelection"] = auxiliarySelection, ["auxiliarySentToImage"] = sendAuxiliary,
                    ["auxiliaryYawDegrees"] = direction?.AuxiliarySceneYawDegrees,
                    ["auxiliaryPitchDegrees"] = direction?.AuxiliaryScenePitchDegrees,
                    ["auxiliaryHorizontalFovDegrees"] = direction?.AuxiliarySceneHorizontalFovDegrees,
                    ["currentScreenshotSentToImage"] = sendCurrent
                });
            }
            // MapConversation has no panorama: its only environment reference remains
            // the passive tabletop view. Mission screenshots calibrate the director;
            // local composition and text-only directors need that calibration on the image side too.
            if (sendCurrent)
                image.Insert(sceneReferenceCount, current);
        }

        private static string AuxiliarySelection(IllustrationDirection direction, bool primarySelected)
        {
            if (!primarySelected) return "no_visual_primary";
            if (!direction.AuxiliarySceneYawDegrees.HasValue || !direction.AuxiliaryScenePitchDegrees.HasValue ||
                !direction.AuxiliarySceneHorizontalFovDegrees.HasValue) return "missing_or_invalid";
            double yaw = direction.AuxiliarySceneYawDegrees.Value;
            double pitch = direction.AuxiliaryScenePitchDegrees.Value;
            double fov = direction.AuxiliarySceneHorizontalFovDegrees.Value;
            // Negated inclusive comparisons also reject NaN and infinity.
            if (!(yaw >= -180 && yaw <= 180 && pitch >= -60 && pitch <= 60 && fov >= 45 && fov <= 100))
                return "missing_or_invalid";
            double radians = Math.PI / 180;
            double primaryPitch = direction.ScenePitchDegrees.Value * radians;
            double auxiliaryPitch = pitch * radians;
            double dot = Math.Sin(primaryPitch) * Math.Sin(auxiliaryPitch) +
                Math.Cos(primaryPitch) * Math.Cos(auxiliaryPitch) * Math.Cos((yaw - direction.SceneYawDegrees.Value) * radians);
            double separation = Math.Acos(Math.Max(-1, Math.Min(1, dot))) / radians;
            if (separation < 8) return "duplicate_direction";
            // Inscribed view cones must overlap by at least 10 degrees. This is a
            // geometric guard (including the yaw seam), not semantic verification.
            if (separation > (direction.SceneHorizontalFovDegrees.Value + fov) / 2 - 10)
                return "insufficient_overlap";
            return "director";
        }
    }
}
