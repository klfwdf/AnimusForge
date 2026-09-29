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
        internal const string IdleStanceFullBodyNote =
            "【全身图用途】此图是游戏引擎统一的待机展示站姿（正面直立、双臂自然下垂），仅作服装装备形制、配色、覆盖范围与体型的对照；" +
            "人物的身体朝向、重心、四肢位置、手势与取景按正文【此刻动作】（如有）与【人物与镜头】绘制。";

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
            if (string.IsNullOrWhiteSpace(portraits.HeadDetail)) return;
            var head = new IllustrationReferenceImage(portraits.HeadDetail,
                "人物【" + name + "】头肩细节：与同名全身图是同一个人，不是新增人物。" +
                "仅补充可见五官、须发与头部装备，保留装备遮挡；不指定姿态、取景或照明。" +
                (eventReference ? "仅在所选事件涉及此人时使用。" : string.Empty),
                eventReference ? IllustrationReferenceKind.EventCharacter : IllustrationReferenceKind.CharacterDetail);
            director.Add(head);
            if (image != null && !ReferenceEquals(director, image)) image.Add(head);
        }

        internal static IllustrationReferenceImage SelectSceneAnchor(IReadOnlyList<IllustrationReferenceImage> scenes)
        {
            var scene = scenes?.FirstOrDefault(x => x != null &&
                (x.Kind == IllustrationReferenceKind.Scene || x.Kind == IllustrationReferenceKind.MapConversationScene) && !string.IsNullOrWhiteSpace(x.Base64Image));
            return scene == null ? null : new IllustrationReferenceImage(scene.Base64Image,
                scene.Label + (scene.Kind == IllustrationReferenceKind.MapConversationScene
                    ? " 生图环境参考：仅用于当前位置与环境定位，保留这张对话布景可见的地貌、植被与材质，屏幕人物位置不作为野外实际站位证据。"
                    : " 生图环境参考：仅用于当前位置与环境定位，保持实际建筑布局、墙面材质与固有色、门窗楼梯及环境高低关系；") +
                "人物动作优先采用对话中的已发生叙事，不以截图待机姿势覆盖导演动作。" +
                "依导演选择的机位和画风重新绘制，参考图不作为必须保留的像素底图，不复制UI或截图渲染质感。", scene.Kind);
        }

        internal static void AddSceneReferences(List<IllustrationReferenceImage> image, IReadOnlyList<IllustrationReferenceImage> scenes,
            IllustrationDirection direction, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            var current = SelectSceneAnchor(scenes);
            bool sendCurrent = current != null && (current.Kind == IllustrationReferenceKind.MapConversationScene ||
                direction?.UsedLocalFallback == true || direction?.VisionUnsupported == true || direction?.UsedTextOnlyDirector == true);
            var panorama = scenes?.FirstOrDefault(x => x != null && x.Kind == IllustrationReferenceKind.ScenePanorama && !string.IsNullOrWhiteSpace(x.Base64Image));
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
                    throw new InvalidOperationException("场景全景超过投影预算，已停止生图。");
                string auxiliarySelection = AuxiliarySelection(direction, selected);
                bool sendAuxiliary = auxiliarySelection == "director";
                byte[] panoramaBytes = Convert.FromBase64String(panorama.Base64Image);
                byte[][] perspectives = sendAuxiliary
                    ? ScenePerspectiveProjection.ProjectPair(panoramaBytes, yaw, pitch, fov,
                        direction.AuxiliarySceneYawDegrees.Value, direction.AuxiliaryScenePitchDegrees.Value,
                        direction.AuxiliarySceneHorizontalFovDegrees.Value, token)
                    : new[] { ScenePerspectiveProjection.Project(panoramaBytes, yaw, pitch, fov, token) };
                const string sceneRole =
                    "此参考图来自同一现场独立静态副本，主动省略了现场所有人物与动态实体，空桌椅或空地不代表现场无人；在场人物及有证据的背景人群按正文绘制，不照搬副本的无人状态。" +
                    "这张图只约束场景环境，不用于推断人物脸部、发型、年龄、服装或姿态；人物外观以对应身份参考图为准。" +
                    "这是中性观察补光，不代表现场光源；光源方向、时段与氛围按正文，人物位置按本次事实。缺失区域不作为开放天空或新增物体的证据。";
                image.Insert(sceneReferenceCount++, new IllustrationReferenceImage(Convert.ToBase64String(perspectives[0]),
                    "普通透视环境主视角：" +
                    (selected ? "依据导演选择的方向从完整全景重新投影。" : "导演未提供可用的视觉选景，从完整全景取默认前向。") +
                    "最终画面的方向与可见建筑关系以此图为准，不能把画外的王座、门洞或其他方向的陈设搬进此视角。" + sceneRole,
                    IllustrationReferenceKind.ScenePerspective));
                if (sendAuxiliary)
                    image.Insert(sceneReferenceCount++, new IllustrationReferenceImage(Convert.ToBase64String(perspectives[1]),
                        "普通透视环境辅助视角：与前一张主图来自同一全景，仅补充相邻结构、重叠地标和材质。" +
                        "最终构图仍采用主视角，不把辅助图独有的设施移到主背景，不拼接背景或重复绘制共同地标。" + sceneRole,
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
