namespace AnimusForge.Illustrator.Core
{
    /// <summary>
    /// 一张发送给视觉模型/生图模型的参考图，附带中文标签说明其内容角色
    /// （例如：人物真实3D形象、家族纹章、现场实景），使多图混传时模型不会混淆。
    /// </summary>
    public sealed class IllustrationReferenceImage
    {
        public string Base64Image { get; }
        public string Label { get; }

        public IllustrationReferenceImage(string base64Image, string label)
        {
            Base64Image = base64Image;
            Label = label ?? string.Empty;
        }
    }
}
