using System.Collections.Generic;
using System.Linq;
using AnimusForge.Illustrator.Engine;

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
                "人物【" + name + "】的头肩细节参考：与该人物全身身份图是同一个人，不是新增人物或另一套服装。" +
                "辨认可见的须发有无、发际线、分缝、长短与盘束形状、耳颈轮廓及头盔护具；保留装备遮挡，不补造看不见的头发。" +
                "仅补充身份细节，不要求采用本图取景、姿态或照明。" +
                (eventReference ? "这是本期候选人物资料，只有导演选中的事件涉及此人且需要他入画时才使用，不强制该人物成为画面主角。" : string.Empty),
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

        internal static void AddSceneReferences(List<IllustrationReferenceImage> image, IReadOnlyList<IllustrationReferenceImage> scenes)
        {
            var panorama = scenes?.FirstOrDefault(x => x != null && (x.Kind == IllustrationReferenceKind.SceneViews || x.Kind == IllustrationReferenceKind.ScenePanorama) && !string.IsNullOrWhiteSpace(x.Base64Image));
            if (panorama != null) image.Add(panorama);
            var current = SelectSceneAnchor(scenes);
            if (current != null) image.Add(current);
        }
    }
}
