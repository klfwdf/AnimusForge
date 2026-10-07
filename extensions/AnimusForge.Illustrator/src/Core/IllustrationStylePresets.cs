namespace AnimusForge.Illustrator.Core
{
    internal sealed class IllustrationStylePreset
    {
        internal IllustrationStylePreset(string directorPrompt, string imagePrompt, string negativePrompt = null, string apiStyle = null, bool isCustom = false)
        {
            DirectorPrompt = directorPrompt;
            // Send the full selected style independently of anything the director returns.
            // Keep endpoint-specific wording as a supplement, never as a shorter replacement.
            ImagePrompt = string.IsNullOrEmpty(imagePrompt) || imagePrompt == directorPrompt
                ? directorPrompt
                : directorPrompt + "\n" + imagePrompt;
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
    /// The image endpoint receives the full director style plus any endpoint-specific supplement.
    /// </summary>
    internal static class IllustrationStylePresets
    {
        private static readonly IllustrationStylePreset ClassicOil = new IllustrationStylePreset(
            "古典写实历史油画，结合伦勃朗式明暗组织与克雷格·穆林斯式概括性绘画笔触。" +
            "以准确的人体结构、透视和大块明暗塑造体积，保留清晰可辨的油彩笔触，避免每个细节都同样锐利。" +
            "视觉焦点集中于人物面部、正在发生的动作及关键接触处；次要衣褶、装备和远景采用简练色块与松动笔触概括，形成有选择的细节层次。" +
            "笔触概括仍保留环境的结构层次、材质分区和有叙事意义的细节，不能把已设计的建筑、陈设或自然环境抹成空色面。" +
            "轮廓随光照与空间变化，焦点处明确，背光与远处适度融入环境。" +
            "亮部可有适量厚涂，暗部保持通透与冷暖变化，避免整片死黑或全画面统一棕黄。" +
            "光源方向、昼夜、天气和物体固有色服从现场事实，明暗对照不改变真实时段。" +
            "人物、衣物、建筑与地面采用统一的绘画语言，以接触阴影、环境反光和遮挡关系自然结合。" +
            "整体厚重、克制、有历史叙事感，让绘画表现服务于人物当下的行动。",
            null,
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

        private static readonly IllustrationStylePreset NarrativeAnime = new IllustrationStylePreset(
            "采用日系动漫插画的线条、色块和半写实明暗表现，以清透色彩融合人物与精细写实环境。" +
            "人物保持原有年龄、面貌与正常人体比例；脸部轮廓、颧骨与下颌、鼻梁与鼻翼、眼窝与眼形、五官间距及肤色，以本人身份参考图为准。" +
            "风格化用于线条、色块、笔触和明暗概括，保留人物原有面部结构与个体差异，表情细腻自然。" +
            "细线描与绘画式明暗结合，轮廓线轻盈且随受光变化，头发以清楚的发束和少量细发丝塑形。" +
            "以清晰的大块明暗表现体积，局部受光过渡柔和，肤色通透，衣褶具有方向、重量与层次，材质呈现可信的反光和纹理。" +
            "色彩鲜活清透，以有层次的冷暖关系和饱和度差异组织画面，亮部明净，暗部保留色彩与细节。" +
            "环境具有准确透视、精细材质与清楚的空间层次，远景通过适量空气透视拉开距离，关键人物与动作保持清晰。" +
            "采用电影叙事式的视觉组织，人物与环境共用光源、投影和环境反光，整幅画面统一为精细数字绘画。" +
            "昼夜、天气、光源方向、人物衣着和物体固有色服从本次场景事实；地点、人数、动作与机位由本次内容决定。",
            null,
            "chibi, oversized anime eyes, thick comic outlines, flat vector art, rough sketch, plastic skin, 3d game render, photographic rendering, heavy impasto, muddy colors, excessive bloom");

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
                case "narrative-anime": return NarrativeAnime;
                case "vivid": return Vivid;
                case "natural": return Natural;
                case "classic-oil":
                default: return ClassicOil;
            }
        }
    }
}
