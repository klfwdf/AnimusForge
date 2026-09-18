namespace AnimusForge.Illustrator.Core
{
    internal sealed class IllustrationStylePreset
    {
        internal IllustrationStylePreset(string directorPrompt, string imagePrompt, string negativePrompt = null, string apiStyle = null, bool isCustom = false)
        {
            DirectorPrompt = directorPrompt;
            ImagePrompt = imagePrompt;
            NegativePrompt = negativePrompt;
            ApiStyle = apiStyle;
            IsCustom = isCustom;
        }

        public string DirectorPrompt { get; }
        public string ImagePrompt { get; }
        public string NegativePrompt { get; }
        public string ApiStyle { get; }
        public bool IsCustom { get; }
    }

    /// <summary>
    /// One shared definition per style. Resolve once per request; built-in presets are immutable and cached.
    /// The director gets painting decisions to apply, while the image endpoint gets a concise style anchor.
    /// </summary>
    internal static class IllustrationStylePresets
    {
        private static readonly IllustrationStylePreset ClassicOil = new IllustrationStylePreset(
            "古典写实历史油画，结合伦勃朗式明暗组织与克雷格·穆林斯式概括性绘画笔触。" +
            "以准确的人体结构、透视和大块明暗塑造体积，保留清晰可辨的油彩笔触，避免每个细节都同样锐利。" +
            "视觉焦点集中于人物面部、正在发生的动作及关键接触处；次要衣褶、装备和远景采用简练色块与松动笔触概括，形成有选择的细节层次。" +
            "轮廓随光照与空间变化，焦点处明确，背光与远处适度融入环境。" +
            "亮部可有适量厚涂，暗部保持通透与冷暖变化，避免整片死黑或全画面统一棕黄。" +
            "光源方向、昼夜、天气和物体固有色服从现场事实，明暗对照不改变真实时段。" +
            "人物、衣物、建筑与地面采用统一的绘画语言，以接触阴影、环境反光和遮挡关系自然结合。" +
            "整体厚重、克制、有历史叙事感，让绘画表现服务于人物当下的行动。",
            "古典写实历史油画，准确结构与概括性油彩笔触并重；焦点精细、次要区域简练，轮廓虚实有别，暗部通透、冷暖丰富。人物与环境统一绘制，保留现场采光及固有色，避免全局棕黄、塑料质感和均匀锐化。",
            "2d flat vector art, cheap cel-shading, lineart sketch, anime, cartoon, 卡通, 动漫风");

        private static readonly IllustrationStylePreset DarkEpic = new IllustrationStylePreset(
            "暗黑史诗写实，沉郁色调与中世纪凝重历史氛围",
            "暗黑史诗写实, dark epic realism, grim medieval war chronicle, dramatic chiaroscuro, painterly oil texture",
            "bright cheerful colors, cartoon, anime, cel shading, clean untarnished surfaces");

        private static readonly IllustrationStylePreset Cinematic = new IllustrationStylePreset(
            "电影化叙事光影与镜头语言，光照服从现场时间和环境",
            "电影级光影, cinematic film still, anamorphic composition, movie-grade dramatic lighting and color grading",
            "flat lighting, washed-out colors, cartoon, anime, cluttered composition");

        private static readonly IllustrationStylePreset MosanArt = new IllustrationStylePreset(
            "莫桑艺术（默兹河流域罗马式珐琅与手抄本彩饰）：景泰蓝式宝石级饱和平涂色块、金色勾边、装饰性边框纹样、拉长端庄的程式化人物、浓重黑色轮廓线、平面化叙事构图",
            "莫桑艺术, 默兹河流域12世纪罗马式珐琅与手抄本彩饰风格, 景泰蓝式宝石级饱和平涂色块, 金色勾边与装饰性边框纹样, 拉长端庄的程式化人物造型, 浓重黑色轮廓线, 平面化叙事构图, Mosan art, Romanesque manuscript illumination, champleve enamel, jewel-like saturated flat colors, gold outlines, decorative borders",
            "photorealism, soft gradients, photographic lighting, cartoon, anime, 摄影光影");

        private static readonly IllustrationStylePreset Vivid = new IllustrationStylePreset(
            "色彩鲜明、叙事清晰，材质与空间层次丰富可信", null, "dull colors, washed out, cartoon, anime", "vivid");

        private static readonly IllustrationStylePreset Natural = new IllustrationStylePreset(
            "自然写实、克制可信、材质与环境色彩真实", null, "oversaturated, cartoon, anime", "natural");

        internal static IllustrationStylePreset Resolve(string selectedStyle, string customStylePrompt)
        {
            switch (selectedStyle)
            {
                case "custom":
                    // Do not rewrite saved user text or substitute a built-in style for an intentionally empty prompt.
                    string custom = customStylePrompt ?? string.Empty;
                    return new IllustrationStylePreset(custom, custom, isCustom: true);
                case "dark-epic": return DarkEpic;
                case "cinematic": return Cinematic;
                case "mosan-art": return MosanArt;
                case "vivid": return Vivid;
                case "natural": return Natural;
                case "classic-oil":
                default: return ClassicOil;
            }
        }
    }
}
