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
        internal static void AddCharacter(List<IllustrationReferenceImage> director, List<IllustrationReferenceImage> image,
            CharacterPortraitReferences portraits, string name, string fullBodyLabel, bool eventReference = false)
        {
            if (portraits == null || string.IsNullOrWhiteSpace(portraits.FullBody)) return;
            var full = new IllustrationReferenceImage(portraits.FullBody, fullBodyLabel,
                eventReference ? IllustrationReferenceKind.EventCharacter : IllustrationReferenceKind.Character);
            director.Add(full);
            if (image != null && !ReferenceEquals(director, image)) image.Add(full);
            if (string.IsNullOrWhiteSpace(portraits.HeadDetail)) return;
            var head = new IllustrationReferenceImage(portraits.HeadDetail,
                "人物【" + name + "】头肩细节：与前一张全身图是同一个人，不是新增人物。" +
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
                    ? " 生图环境参考：仅保留这张对话布景可见的地貌、植被、材质与采光，屏幕人物位置不作为野外实际站位证据。"
                    : " 生图环境参考：保持实际建筑布局、墙面材质与固有色、门窗楼梯及人物高低关系；") +
                "依导演选择的机位和画风重新绘制，参考图不作为必须保留的像素底图，不复制UI或截图渲染质感。", scene.Kind);
        }

        internal static void AddSceneReferences(List<IllustrationReferenceImage> image, IReadOnlyList<IllustrationReferenceImage> scenes,
            IllustrationDirection direction, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            var current = SelectSceneAnchor(scenes);
            bool sendCurrent = current != null && (current.Kind == IllustrationReferenceKind.MapConversationScene || direction?.UsedLocalFallback == true);
            var panorama = scenes?.FirstOrDefault(x => x != null && x.Kind == IllustrationReferenceKind.ScenePanorama && !string.IsNullOrWhiteSpace(x.Base64Image));
            if (panorama != null)
            {
                var watch = Stopwatch.StartNew();
                bool selected = direction?.UsedLocalFallback == false && direction.SceneYawDegrees.HasValue &&
                    direction.ScenePitchDegrees.HasValue && direction.SceneHorizontalFovDegrees.HasValue;
                double yaw = selected ? direction.SceneYawDegrees.Value : 0;
                double pitch = selected ? direction.ScenePitchDegrees.Value : 0;
                // 35 mm-equivalent default on a full-frame camera is about 54-55°
                // horizontal FOV. Keep the director-selected value when present.
                double fov = selected ? direction.SceneHorizontalFovDegrees.Value : 55;
                // Internally produced base64 only. Bound allocation before decode and do
                // not retry with the distorted panorama if projection fails.
                if (panorama.Base64Image.Length > 24 * 1024 * 1024)
                    throw new InvalidOperationException("场景全景超过投影预算，已停止生图。");
                byte[] perspective = ScenePerspectiveProjection.Project(Convert.FromBase64String(panorama.Base64Image), yaw, pitch, fov, token);
                image.Insert(0, new IllustrationReferenceImage(Convert.ToBase64String(perspective),
                    "同一现场独立静态副本的普通透视环境参考：" +
                    (selected ? "依据导演选择的方向从完整全景重新投影。" : "从完整全景取默认前向，仅补充可见环境资料，最终取景由正文决定。") +
                    "保留可见建筑、家具、门窗与材质的空间关系，按正文统一绘制人物和环境；不把参考取景作为必须复制的画面。" +
                    "此参考图主动省略了现场所有人物与动态实体，空桌椅或空地不代表现场无人；在场人物及有证据的背景人群按正文绘制，不照搬副本的无人状态。" +
                    "这张图只约束场景环境，不用于推断人物脸部、发型、年龄、服装或姿态；人物外观以对应身份参考图为准。" +
                    "这是中性观察补光，不代表现场光源；光源方向、时段与氛围按正文，人物位置按本次事实。缺失区域不作为开放天空或新增物体的证据。",
                    IllustrationReferenceKind.ScenePerspective));
                GenerationDiagnostics.Current?.RecordStage("scene_perspective_reference", new JObject
                {
                    ["selection"] = selected ? "director" : "default_front", ["yawDegrees"] = yaw,
                    ["pitchDegrees"] = pitch, ["horizontalFovDegrees"] = fov, ["width"] = 768, ["height"] = 768,
                    ["elapsedMs"] = watch.ElapsedMilliseconds, ["panoramaSentToImage"] = false,
                    ["currentScreenshotSentToImage"] = sendCurrent
                });
            }
            // MapConversation has no panorama: its only environment reference remains
            // the passive tabletop view. Mission screenshots calibrate the director;
            // only local composition needs that calibration on the image side too.
            if (sendCurrent)
                image.Insert(panorama != null ? 1 : 0, current);
        }
    }
}
