namespace AnimusForge.Illustrator.Core
{
    public enum IllustrationReferenceKind
    {
        Unspecified,
        Character,
        Emblem,
        Scene,
        CharacterDetail,
        ScenePanorama,
        SceneViews,
        MapConversationScene,
        EventCharacter,
        EventEmblem,
        ScenePerspective,
        PairedScenePerspective,
        GeneratedImage,
        MissionScreenshot
    }

    /// <summary>
    /// 一张发送给视觉模型/生图模型的参考图，附带中文标签说明其内容角色
    /// （例如：人物真实3D形象、家族纹章、现场实景），使多图混传时模型不会混淆。
    /// </summary>
    public sealed class IllustrationReferenceImage
    {
        public string Base64Image { get; }
        public string Label { get; }
        public IllustrationReferenceKind Kind { get; }
        // Native full-body tableau render in the fixed idle stance. The image client sends
        // these last so the endpoint does not adopt the idle silhouette as its canvas.
        internal bool IsIdleStanceFullBody { get; set; }

        public IllustrationReferenceImage(string base64Image, string label)
            : this(base64Image, label, IllustrationReferenceKind.Unspecified)
        {
        }

        public IllustrationReferenceImage(string base64Image, string label, IllustrationReferenceKind kind)
        {
            Base64Image = base64Image;
            Label = label ?? string.Empty;
            Kind = kind;
        }
    }
}
