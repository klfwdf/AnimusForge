using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using MCM.Abstractions;
using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Base.Global;
using MCM.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TaleWorlds.Library;
using AnimusForge.Illustrator.Core;
using AnimusForge.Illustrator.UI.Patches;

namespace AnimusForge.Illustrator
{
    public class IllustratorSettings : AttributeGlobalSettings<IllustratorSettings>
    {
        public override string Id => "AnimusForge_Illustrator_v1";
        public override string DisplayName => "AnimusForge - AI 画卷生图系统 (Illustrator)";
        public override string FolderName => "AnimusForge";
        public override string FormatType => "json";

        private static List<string> _modelOptions = new List<string> { "*手动输入*" };
        private static Dropdown<string> _modelDropdown;
        private static readonly object _modelLock = new object();
        private static string _cachedModelsFilePath;

        private static List<string> _directorModelOptions = new List<string> { "*手动输入*", "*默认(复用正文API)*" };
        private static Dropdown<string> _directorModelDropdown;
        private static readonly object _directorModelLock = new object();
        private static string _cachedDirectorModelsFilePath;

        public IllustratorSettings()
        {
            FetchModelList = RequestModelListFetch;
            FetchDirectorModelList = RequestDirectorModelListFetch;
            EditCustomStylePrompt = OpenCustomStylePromptEditor;
            EditNegativePrompt = OpenNegativePromptEditor;
            EditCustomDirectorPrompt = OpenCustomDirectorPromptEditor;
        }

        private bool _enableImageGeneration = true;

        [SettingPropertyBool("生图开启", HintText = "所有生图链路的总开关。关闭后百科、对话、快报、画廊等入口都不会发起生图请求。", Order = 0, RequireRestart = false)]
        [SettingPropertyGroup("1. 基础设置", GroupOrder = 1)]
        public bool EnableImageGeneration
        {
            get => _enableImageGeneration;
            set
            {
                if (value == _enableImageGeneration) return;
                _enableImageGeneration = value;
                QueueInjectedButtonRefresh();
            }
        }

        // Not exposed in MCM: always on. Models without vision support already degrade to text-only directing.
        public bool EnableMultimodalVision => true;

        // Character portraits are always rendered offscreen (required references); this switch only covers
        // the scene panorama of Mission conversations. Field (map) conversations never use a panorama.
        [SettingPropertyBool("启用场景离屏渲染 (环境全景)", HintText = "开启后，会话插画会在后台离屏渲染玩家附近 30 米的场景全景，作为环境参考还原建筑、陈设与材质；采集需要数秒并占用少量 GPU。关闭后不渲染全景，环境仅依据当前画面截图与文字事实。人物立绘始终离屏渲染，不受此开关影响。野外（大地图）会话本来就不使用全景。", Order = 1, RequireRestart = false)]
        [SettingPropertyGroup("1. 基础设置", GroupOrder = 1)]
        public bool EnableSceneOffscreenRendering { get; set; } = true;

        [SettingPropertyBool("生成完成后自动清理临时文件", HintText = "开启后，各提取任务结束时仅清理自己产生的离屏导出文件，不清理其他请求或历史调试文件。关闭时保留纹章导出便于排查；已读取的立绘临时文件仍按原有流程释放。不影响画廊缓存与默认插图。", Order = 3, RequireRestart = false)]
        [SettingPropertyGroup("1. 基础设置", GroupOrder = 1)]
        public bool AutoCleanTempFiles { get; set; } = false;

        [SettingPropertyText("生图 API 端点地址 (Base URL)", HintText = "填写服务根地址或 /v1。默认有参考图时优先 /images/edits（真实上传参考图），无参考图才用 /images/generations；完整 edits 地址也可识别。模型必须支持所选通道；不支持 edits 不会静默丢图转文生图。", Order = 1, RequireRestart = false)]
        [SettingPropertyGroup("2. 生图 API 配置 (OpenAI 兼容)", GroupOrder = 2)]
        public string ApiBaseUrl { get; set; } = "https://api.siliconflow.cn/v1";

        [SettingPropertyBool("使用完整调用 URL (不自动拼接后缀)", HintText = "开启后，系统将直接使用填写的端点地址发起请求，不自动追加后缀。适合自定义特殊反代或中转路径。", Order = 2, RequireRestart = false)]
        [SettingPropertyGroup("2. 生图 API 配置 (OpenAI 兼容)", GroupOrder = 2)]
        public bool UseExactEndpointUrl { get; set; } = false;

        [SettingPropertyText("生图 API 密钥 (API Key)", HintText = "用于鉴权的 API Key。若端点无需鉴权或为本地反代可留空。", Order = 3, RequireRestart = false)]
        [SettingPropertyGroup("2. 生图 API 配置 (OpenAI 兼容)", GroupOrder = 2)]
        public string ApiKey { get; set; } = "";

        [SettingPropertyButton("拉取生图模型列表", Content = "点击拉取", Order = 4, RequireRestart = false, HintText = "向配置的 Base URL (GET /models) 发起查询，自动拉取服务端支持的模型列表，并优先筛选出图像与绘画模型。")]
        [SettingPropertyGroup("2. 生图 API 配置 (OpenAI 兼容)", GroupOrder = 2)]
        public Action FetchModelList { get; set; }

        private string _modelName = "black-forest-labs/FLUX.1-schnell";

        [SettingPropertyText("生图模型名称 (Model)", HintText = "生图模型名称。支持常规生图模型（如 FLUX.1-schnell、dall-e-3、gpt-image-1.5 等）以及对话原生多模态出图模型（如 gemini-3.1-flash-image 等，系统会自动识别并走对话图生图通道）。", Order = 5, RequireRestart = false)]
        [SettingPropertyGroup("2. 生图 API 配置 (OpenAI 兼容)", GroupOrder = 2)]
        public string ModelName
        {
            get => _modelName;
            set
            {
                string trimmed = (value ?? string.Empty).Trim();
                if (_modelName == trimmed) return;
                _modelName = trimmed;
                SyncModelDropdownWithModelName(_modelName);
            }
        }

        [SettingPropertyDropdown("选择生图模型 (下拉选单)", Order = 6, RequireRestart = false, HintText = "点击上方“拉取生图模型列表”后，可从本下拉菜单中直接快速点选可用模型。若选“*手动输入*”，则使用上方文本框中输入的模型名称。")]
        [SettingPropertyGroup("2. 生图 API 配置 (OpenAI 兼容)", GroupOrder = 2)]
        public Dropdown<string> ModelDropdown
        {
            get
            {
                EnsureModelDropdown();
                return _modelDropdown;
            }
            set
            {
                lock (_modelLock)
                {
                    _modelDropdown = value;
                    if (value != null && _modelOptions != null && value.SelectedIndex >= 0 && value.SelectedIndex < _modelOptions.Count)
                    {
                        string selected = _modelOptions[value.SelectedIndex];
                        if (!string.IsNullOrWhiteSpace(selected) && selected != "*手动输入*")
                        {
                            _modelName = selected;
                            if (Instance != null && !ReferenceEquals(Instance, this))
                            {
                                Instance.ModelName = selected;
                            }
                        }
                    }
                }
            }
        }

        private void SyncModelDropdownWithModelName(string modelName)
        {
            lock (_modelLock)
            {
                EnsureModelDropdown();
                if (_modelOptions == null || _modelOptions.Count == 0 || _modelDropdown == null) return;
                int idx = _modelOptions.IndexOf(modelName);
                _modelDropdown.SelectedIndex = idx >= 0 ? idx : 0;
            }
        }

        // ImageSize remains a public runtime/config compatibility property for the image client.
        // The MCM text editor was intentionally removed: the preset dropdown below is the only
        // user-facing source of image dimensions, so unsupported/custom dimensions cannot leak
        // into a request. The setter still accepts an old persisted value and normalizes it.
        private string _imageSize = "1024x1024";
        public string ImageSize
        {
            get
            {
                if (_sizePresetDropdown != null && _sizePresetDropdown.SelectedIndex >= 0
                    && _sizePresetDropdown.SelectedIndex < _sizePresetOptions.Count)
                {
                    return ExtractSizeToken(_sizePresetOptions[_sizePresetDropdown.SelectedIndex]);
                }

                return NormalizeImageSize(_imageSize);
            }
            set
            {
                _imageSize = NormalizeImageSize(value);
                if (_sizePresetDropdown == null) return;

                int index = FindSizePresetIndex(_imageSize);
                if (index >= 0) _sizePresetDropdown.SelectedIndex = index;
            }
        }

        private static readonly List<string> _sizePresetOptions = new List<string>
        {
            "1024x1024 (1:1 方形)",
            "1280x720 (16:9 宽屏横幅)",
            "720x1280 (9:16 竖幅立绘)",
            "1344x768 (7:4 史诗宽画幅)",
            "1024x1536 (2:3 竖版海报)",
            "2048x2048 (1:1 超高清方形)"
        };
        private Dropdown<string> _sizePresetDropdown;

        private static string ExtractSizeToken(string preset)
        {
            if (string.IsNullOrWhiteSpace(preset)) return "1024x1024";
            int separator = preset.IndexOf(' ');
            return (separator > 0 ? preset.Substring(0, separator) : preset).Trim();
        }

        private static int FindSizePresetIndex(string size)
        {
            string token = NormalizeImageSize(size);
            for (int i = 0; i < _sizePresetOptions.Count; i++)
            {
                if (string.Equals(ExtractSizeToken(_sizePresetOptions[i]), token, StringComparison.OrdinalIgnoreCase))
                    return i;
            }

            return -1;
        }

        private static string NormalizeImageSize(string size)
        {
            int index = FindSizePresetIndexWithoutNormalization(size);
            return index >= 0 ? ExtractSizeToken(_sizePresetOptions[index]) : "1024x1024";
        }

        private static int FindSizePresetIndexWithoutNormalization(string size)
        {
            string token = (size ?? string.Empty).Trim();
            for (int i = 0; i < _sizePresetOptions.Count; i++)
            {
                if (string.Equals(ExtractSizeToken(_sizePresetOptions[i]), token, StringComparison.OrdinalIgnoreCase))
                    return i;
            }

            return -1;
        }

        [SettingPropertyDropdown("生图分辨率 (Size)", Order = 7, RequireRestart = false, HintText = "快报与“全屏覆盖”场景插画按预设转为16:9：1024方形及1280/720档→1280×720；1344/1536档→1536×864；2048方形→2048×1152。百科与“独立面板”场景插画使用所选尺寸。需生图服务支持对应尺寸。")]
        [SettingPropertyGroup("2. 生图 API 配置 (OpenAI 兼容)", GroupOrder = 2)]
        public Dropdown<string> SizePresetDropdown
        {
            get
            {
                if (_sizePresetDropdown == null)
                {
                    int idx = FindSizePresetIndex(_imageSize);
                    _sizePresetDropdown = new Dropdown<string>(_sizePresetOptions, idx >= 0 ? idx : 0);
                }
                return _sizePresetDropdown;
            }
            set
            {
                _sizePresetDropdown = value;
                int idx = value?.SelectedIndex ?? 0;
                if (idx > 0 && idx < _sizePresetOptions.Count)
                {
                    string size = ExtractSizeToken(_sizePresetOptions[idx]);
                    if (!string.IsNullOrWhiteSpace(size))
                    {
                        _imageSize = NormalizeImageSize(size);
                        if (Instance != null && !ReferenceEquals(Instance, this))
                        {
                            Instance.ImageSize = _imageSize;
                        }
                    }
                }
                else if (idx == 0)
                {
                    _imageSize = ExtractSizeToken(_sizePresetOptions[0]);
                }
            }
        }

        private static readonly List<string> _qualityOptions = new List<string>
        {
            "默认 (不传)",
            "standard (标准)",
            "hd (高清细节)",
            "low (低-最快出图)",
            "medium (中等)",
            "high (高-精细渲染)",
            "auto (交由服务端自动)"
        };
        private Dropdown<string> _qualityDropdown;

        private static readonly string[] _qualityTokens = { "", "standard", "hd", "low", "medium", "high", "auto" };

        [SettingPropertyDropdown("生成画质预设 (Quality)", Order = 8, RequireRestart = false, HintText = "standard/hd 对应 DALL-E-3 系；low/medium/high/auto 对应 gpt-image-1 系及支持这些枚举的中转模型。选“默认”时不发送该参数，避免不支持的模型报错。Gemini 等对话生图通道自动换算为画质提示词注入。")]
        [SettingPropertyGroup("2. 生图 API 配置 (OpenAI 兼容)", GroupOrder = 2)]
        public Dropdown<string> QualityDropdown
        {
            get
            {
                if (_qualityDropdown == null)
                    _qualityDropdown = new Dropdown<string>(_qualityOptions, 0);
                return _qualityDropdown;
            }
            set => _qualityDropdown = value;
        }

        public string SelectedQuality
        {
            get
            {
                int idx = _qualityDropdown?.SelectedIndex ?? 0;
                return (idx > 0 && idx < _qualityTokens.Length) ? _qualityTokens[idx] : "";
            }
        }

        private static readonly List<string> _styleOptions = new List<string>
        {
            "古典油画（默认）",
            "vivid (鲜艳生动·API枚举)",
            "natural (自然真实·API枚举)",
            "暗黑史诗写实 (提示词注入)",
            "电影级光影 (提示词注入)",
            "提示词 (自定义画风)",
            "莫桑艺术·默兹河珐琅彩饰 (提示词注入)"
        };
        private Dropdown<string> _styleDropdown;

        [SettingPropertyDropdown("生成风格画风 (Style)", Order = 9, RequireRestart = false, HintText = "vivid/natural 为 OpenAI 官方 style 枚举参数；其余预设不作为 style 参数发送（避免非法枚举报错），而是把画风指令与预设负面词注入提示词，对任何模型生效。选“提示词”时使用下方自定义文本。")]
        [SettingPropertyGroup("2. 生图 API 配置 (OpenAI 兼容)", GroupOrder = 2)]
        public Dropdown<string> StyleDropdown
        {
            get
            {
                if (_styleDropdown == null)
                    _styleDropdown = new Dropdown<string>(_styleOptions, 0); // 默认 古典油画
                return _styleDropdown;
            }
            set => _styleDropdown = value;
        }

        public string SelectedStyle
        {
            get
            {
                int idx = _styleDropdown?.SelectedIndex ?? 0;
                switch (idx)
                {
                    case 1: return "vivid";
                    case 2: return "natural";
                    case 3: return "dark-epic";
                    case 4: return "cinematic";
                    case 5: return "custom";
                    case 6: return "mosan-art";
                    default: return "classic-oil";
                }
            }
        }

        [SettingPropertyButton("自定义画风提示词 (Style=提示词 时生效)", Content = "打开编辑器", Order = 10, RequireRestart = false, HintText = "点击打开大文本编辑器，自由编辑画风指令。仅当上方画风预设选“提示词”时生效，作为【画风指令】注入所有生图通道的提示词，对任何模型生效。默认填入古典油画预设内容供参考。")]
        [SettingPropertyGroup("2. 生图 API 配置 (OpenAI 兼容)", GroupOrder = 2)]
        public Action EditCustomStylePrompt { get; set; }

        // 预填古典油画预设内容，供玩家查看/改写；仅在画风预设选“提示词”时生效
        public string CustomStylePrompt { get; set; } = "古典写实历史油画巨作, 伦勃朗与克雷格·穆林斯(Craig Mullins)式明暗对照法(Chiaroscuro), 戏剧性光影微光, 细腻富有体积感的笔触肌理, classical oil painting masterpiece, dramatic chiaroscuro lighting, painterly brushwork, 8k fine detail";

        [SettingPropertyBool("向生图模型附带参考图 (垫图/图生图)", HintText = "开启后，截取的人物3D立绘参考图将一并发送给生图模型（支持对话多模态及兼容的 ImagesEdits 通道）。关闭则仅把参考图用于提示词导演扩写。", Order = 10, RequireRestart = false)]
        [SettingPropertyGroup("2. 生图 API 配置 (OpenAI 兼容)", GroupOrder = 2)]
        public bool EnableReferenceImageForGeneration { get; set; } = true;

        [SettingPropertyButton("负面提示词 (Negative Prompt)", Content = "打开编辑器", Order = 11, RequireRestart = false, HintText = "点击打开大文本编辑器，填写画面中不希望出现的元素，例如：模糊, 变形, 多余手指, 现代物品, 水印文字。仅在画风预设选“提示词(自定义画风)”时生效，作为禁止指令追加在预设负面词之后。默认填入古典油画预设内容供参考。")]
        [SettingPropertyGroup("2. 生图 API 配置 (OpenAI 兼容)", GroupOrder = 2)]
        public Action EditNegativePrompt { get; set; }

        // 预填古典油画预设负面词，供玩家查看/改写；仅在画风预设选“提示词(自定义画风)”时生效
        public string NegativePrompt { get; set; } = "cartoon, anime, cel shading, flat colors, plastic skin, 3d render, oversaturated, modern objects, 卡通, 动漫风, 塑料质感, 现代物品";

        [SettingPropertyInteger("随机", 0, 100, "0", Order = 12, RequireRestart = false, HintText = "0 沿用旧版，不追加随机提示词。数值越高，越鼓励取景、留白、景深与光影表现的变化；人物外貌、装备、纹章和已确认游戏事实仍须保持一致。这是提示词指导强度，不是模型采样参数。")]
        [SettingPropertyGroup("2. 生图 API 配置 (OpenAI 兼容)", GroupOrder = 2)]
        public int Randomness { get; set; } = 50;

        private int _imageGenerationTimeoutSeconds = 240;

        [SettingPropertyInteger("生图请求总超时", 60, 600, "0 秒", Order = 13, RequireRestart = false, HintText = "单次生图的总等待上限，默认240秒；包含 Edits 尝试、对话通道回退和结果图下载。高画质多参考图重绘或中转较慢时可调大。下一次生图生效；不影响视觉导演请求与离屏采集超时。")]
        [SettingPropertyGroup("2. 生图 API 配置 (OpenAI 兼容)", GroupOrder = 2)]
        public int ImageGenerationTimeoutSeconds
        {
            get => _imageGenerationTimeoutSeconds;
            set => _imageGenerationTimeoutSeconds = Math.Max(60, Math.Min(600, value));
        }

        // Retained for old config/code compatibility; protocol selection is automatic and this
        // preference is intentionally no longer exposed in MCM.
        public bool PreferChatImageProtocol { get; set; } = false;

        private string _directorModelName = "";

        [SettingPropertyText("导演 API 端点地址 (Base URL)", HintText = "视觉导演 API 端点地址。留空时自动复用主模块正文对话 API 端点；若需指定独立的 LLM 模型或第三方中转作为提示词扩写导演，请在此填写 Base URL（如 https://api.openai.com/v1）。", Order = 1, RequireRestart = false)]
        [SettingPropertyGroup("3. 视觉导演 API 配置 (OpenAI 兼容 · 留空使用正文API)", GroupOrder = 3)]
        public string DirectorApiBaseUrl { get; set; } = "";

        [SettingPropertyText("导演 API 密钥 (API Key)", HintText = "视觉导演 API 密钥 (Key)。留空时自动复用主模块正文对话 API Key。", Order = 2, RequireRestart = false)]
        [SettingPropertyGroup("3. 视觉导演 API 配置 (OpenAI 兼容 · 留空使用正文API)", GroupOrder = 3)]
        public string DirectorApiKey { get; set; } = "";

        [SettingPropertyButton("拉取导演模型列表", Content = "点击拉取", Order = 3, RequireRestart = false, HintText = "向填写的导演 Base URL 发起查询拉取可用模型列表。若未填写导演 Base URL，则向主模块正文 API 发起拉取。")]
        [SettingPropertyGroup("3. 视觉导演 API 配置 (OpenAI 兼容 · 留空使用正文API)", GroupOrder = 3)]
        public Action FetchDirectorModelList { get; set; }

        [SettingPropertyText("导演模型名称 (Model)", HintText = "视觉导演模型名称（如 gpt-4o、qwen-plus、deepseek-chat 等）。留空时自动复用主模块正文对话模型。", Order = 4, RequireRestart = false)]
        [SettingPropertyGroup("3. 视觉导演 API 配置 (OpenAI 兼容 · 留空使用正文API)", GroupOrder = 3)]
        public string DirectorModelName
        {
            get => _directorModelName;
            set
            {
                string trimmed = (value ?? string.Empty).Trim();
                if (_directorModelName == trimmed) return;
                _directorModelName = trimmed;
                SyncDirectorModelDropdownWithModelName(_directorModelName);
            }
        }

        [SettingPropertyDropdown("选择导演模型 (下拉选单)", Order = 5, RequireRestart = false, HintText = "点击上方“拉取导演模型列表”后可从下拉菜单点选。若选“*手动输入*”，则使用上方文本框输入的模型名；若选“*默认(复用正文API)*”，则留空复用主模块。")]
        [SettingPropertyGroup("3. 视觉导演 API 配置 (OpenAI 兼容 · 留空使用正文API)", GroupOrder = 3)]
        public Dropdown<string> DirectorModelDropdown
        {
            get
            {
                EnsureDirectorModelDropdown();
                return _directorModelDropdown;
            }
            set
            {
                lock (_directorModelLock)
                {
                    _directorModelDropdown = value;
                    if (value != null && _directorModelOptions != null && value.SelectedIndex >= 0 && value.SelectedIndex < _directorModelOptions.Count)
                    {
                        string selected = _directorModelOptions[value.SelectedIndex];
                        if (selected == "*默认(复用正文API)*")
                        {
                            _directorModelName = "";
                            if (Instance != null && !ReferenceEquals(Instance, this))
                            {
                                Instance.DirectorModelName = "";
                            }
                        }
                        else if (!string.IsNullOrWhiteSpace(selected) && selected != "*手动输入*")
                        {
                            _directorModelName = selected;
                            if (Instance != null && !ReferenceEquals(Instance, this))
                            {
                                Instance.DirectorModelName = selected;
                            }
                        }
                    }
                }
            }
        }

        private void SyncDirectorModelDropdownWithModelName(string modelName)
        {
            lock (_directorModelLock)
            {
                EnsureDirectorModelDropdown();
                if (_directorModelOptions == null || _directorModelOptions.Count == 0 || _directorModelDropdown == null) return;
                if (string.IsNullOrWhiteSpace(modelName))
                {
                    int defaultIdx = _directorModelOptions.IndexOf("*默认(复用正文API)*");
                    _directorModelDropdown.SelectedIndex = defaultIdx >= 0 ? defaultIdx : 0;
                }
                else
                {
                    int idx = _directorModelOptions.IndexOf(modelName);
                    _directorModelDropdown.SelectedIndex = idx >= 0 ? idx : 0;
                }
            }
        }

        [SettingPropertyInteger("导演提词参考篇幅（约 Token）", 600, 4000, "0 Token", HintText = "视觉导演完整输出的软性篇幅参考，不会作为 API 硬上限；模型可为保证四段完整而上下浮动。数值越大，等待时间和费用可能越高。", Order = 6, RequireRestart = false)]
        [SettingPropertyGroup("3. 视觉导演 API 配置 (OpenAI 兼容 · 留空使用正文API)", GroupOrder = 3)]
        public int DirectorMaxTokens { get; set; } = 2000;

        [SettingPropertyButton("自定义导演提示词", Content = "打开编辑器", Order = 7, RequireRestart = false,
            HintText = "填写构图、动作、景别与叙事偏好，适用于百科、会晤和周报/快报。留空保持默认规则；只发给视觉导演，不直接追加到生图端。不能覆盖已知人物、装备、场景与事件事实或改变输出格式。导演关闭/未配置时不生效；修改后从下一次生成开始生效，正在生成的任务不变。")]
        [SettingPropertyGroup("3. 视觉导演 API 配置 (OpenAI 兼容 · 留空使用正文API)", GroupOrder = 3)]
        public Action EditCustomDirectorPrompt { get; set; }

        // Persist with the existing MCM settings identity, like CustomStylePrompt; old configs default to empty.
        public string CustomDirectorPrompt { get; set; } = "";

        [SettingPropertyBool("自动生图", HintText = "仅在“生图开启”开启时生效。开启：快报生成时提前生成插画；关闭：快报右侧面板打开且缓存未命中时再自动生成。", Order = 1, RequireRestart = false)]
        [SettingPropertyGroup("4. 周报与展示场景", GroupOrder = 4)]
        public bool AutoGenerateWeeklyReportIllustration { get; set; } = true;

        private bool _enableEncyclopediaIllustration = true;
        private bool _enableConversationIllustration = true;

        // Kept as non-MCM compatibility properties so old settings can still deserialize.
        // Runtime gating is intentionally owned by EnableImageGeneration.
        public bool EnableEncyclopediaIllustration
        {
            get => _enableEncyclopediaIllustration;
            set
            {
                if (value == _enableEncyclopediaIllustration) return;
                _enableEncyclopediaIllustration = value;
                QueueInjectedButtonRefresh();
            }
        }

        public bool EnableConversationIllustration
        {
            get => _enableConversationIllustration;
            set
            {
                if (value == _enableConversationIllustration) return;
                _enableConversationIllustration = value;
                QueueInjectedButtonRefresh();
            }
        }

        [SettingPropertyBool("NPC回复后自动重绘场景插画", HintText = "开启后，玩家先手动生成本次对话的第一张插画；之后每次 NPC 回复自动重绘一张。后续请求复用首次采集的全景、人物参考和稳定硬事实，只更新最近2条对话与动作。", Order = 4, RequireRestart = false)]
        [SettingPropertyGroup("4. 周报与展示场景", GroupOrder = 4)]
        public bool AutoGenerateConversationIllustrationFullscreen { get; set; } = false;

        private static readonly List<string> _conversationDisplayOptions = new List<string>
        {
            "独立面板（默认）",
            "全屏覆盖"
        };
        private Dropdown<string> _conversationDisplayDropdown;

        [SettingPropertyDropdown("绘图显示效果", Order = 5, RequireRestart = false, HintText = "独立面板：使用右侧场景插画面板；全屏覆盖：插画铺满屏幕作为对话背景，按钮位于上方约四分之三区域，底部保留对话与输入区域；打开百科、菜单等界面时自动隐藏。自动重绘沿用此选择。")]
        [SettingPropertyGroup("4. 周报与展示场景", GroupOrder = 4)]
        public Dropdown<string> ConversationDisplayDropdown
        {
            get
            {
                if (_conversationDisplayDropdown == null)
                    _conversationDisplayDropdown = new Dropdown<string>(_conversationDisplayOptions, 0);
                return _conversationDisplayDropdown;
            }
            set => _conversationDisplayDropdown = value;
        }

        public bool ConversationIllustrationUsesFullscreen
        {
            get => (_conversationDisplayDropdown?.SelectedIndex ?? 0) == 1;
        }

        private int _portraitExportTimeoutSeconds = 20;
        private int _panoramaFaceExportTimeoutSeconds = 6;
        private int _panoramaCaptureTimeoutSeconds = 40;

        [SettingPropertyInteger("人物立绘导出超时", 10, 60, "0 秒", Order = 2, RequireRestart = false,
            HintText = "每个人物离屏立绘的等待上限，默认20秒。下一次提取生效；增大只帮助导出较慢的环境，不能修复原生保存未执行。不是AI接口超时。")]
        [SettingPropertyGroup("5. 存储与性能", GroupOrder = 5)]
        public int PortraitExportTimeoutSeconds
        {
            get => _portraitExportTimeoutSeconds;
            set => _portraitExportTimeoutSeconds = Math.Max(10, Math.Min(60, value));
        }

        [SettingPropertyInteger("全景单方向图片导出超时", 3, 15, "0 秒", Order = 3, RequireRestart = false,
            HintText = "每个全景方向等待原生PNG的上限，默认6秒。六个方向顺序采集，同时受全景总超时限制。下一次采集生效；不能修复原生保存未执行。")]
        [SettingPropertyGroup("5. 存储与性能", GroupOrder = 5)]
        public int PanoramaFaceExportTimeoutSeconds
        {
            get => _panoramaFaceExportTimeoutSeconds;
            set => _panoramaFaceExportTimeoutSeconds = Math.Max(3, Math.Min(15, value));
        }

        [SettingPropertyInteger("环境全景采集总超时", 30, 120, "0 秒", Order = 4, RequireRestart = false,
            HintText = "包含等待提取舞台、环境副本构建和六方向采集的总预算，默认40秒。下一次采集生效；总预算先到仍会停止，失败不发送缺面或重复图片。")]
        [SettingPropertyGroup("5. 存储与性能", GroupOrder = 5)]
        public int PanoramaCaptureTimeoutSeconds
        {
            get => _panoramaCaptureTimeoutSeconds;
            set => _panoramaCaptureTimeoutSeconds = Math.Max(30, Math.Min(120, value));
        }

        [SettingPropertyInteger("本地缓存最大保留张数", 20, 1000, "0 张", HintText = "生成的图片在本地持久化缓存的最大数量，避免重复调用消耗额度。", Order = 1, RequireRestart = false)]
        [SettingPropertyGroup("5. 存储与性能", GroupOrder = 5)]
        public int MaxCacheCount { get; set; } = 200;

        // 提示词扩写：底层永久自动开启，无 MCM 开关
        public bool EnableLlmPromptExpansion { get; set; } = true;

        private void OpenCustomStylePromptEditor()
        {
            try
            {
                string initialText = CustomStylePrompt ?? "";
                DevTextEditorHelper.ShowLongTextEditor("编辑自定义画风提示词", "仅当画风预设选“提示词”时生效，内容作为【画风指令】注入所有生图通道的提示词末尾。", "例如：古典厚涂油画, 水彩淡彩插画, 暗黑史诗写实, 电影级光影, 铅笔素描。留空则不注入。", initialText, delegate (string input)
                {
                    CustomStylePrompt = input ?? "";
                    if (Instance != null && !ReferenceEquals(Instance, this))
                    {
                        Instance.CustomStylePrompt = CustomStylePrompt;
                    }
                }, null, "保存", "返回");
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage("[Illustrator] 打开画风提示词编辑器失败: " + ex.Message, Color.FromUint(4294901760u)));
            }
        }

        private void OpenCustomDirectorPromptEditor()
        {
            try
            {
                DevTextEditorHelper.ShowLongTextEditor("编辑自定义导演提示词",
                    "只给视觉导演：控制构图、动作、景别和叙事，不覆盖真实人物/装备/事件，不改变四段输出格式。导演关闭或未配置时不生效。",
                    "例如：优先表现人物之间的互动，避免总是正面站桩；选择能解释当前事件的瞬间。留空恢复默认规则。",
                    CustomDirectorPrompt ?? "", delegate (string input)
                    {
                        CustomDirectorPrompt = (input ?? "").Trim();
                        if (Instance != null && !ReferenceEquals(Instance, this))
                            Instance.CustomDirectorPrompt = CustomDirectorPrompt;
                        SaveCurrentSettings();
                    }, null, "保存", "返回");
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage("[AI画卷] 打开导演提示词编辑器失败: " + ex.Message, Color.FromUint(4294901760u)));
            }
        }

        private void OpenNegativePromptEditor()
        {
            try
            {
                string initialText = NegativePrompt ?? "";
                DevTextEditorHelper.ShowLongTextEditor("编辑负面提示词", "内容作为禁止指令注入生图提示词，告诉模型画面中不要出现什么。", "例如：模糊, 变形, 多余手指, 现代物品, 水印文字, 低画质。留空则不注入。", initialText, delegate (string input)
                {
                    NegativePrompt = input ?? "";
                    if (Instance != null && !ReferenceEquals(Instance, this))
                    {
                        Instance.NegativePrompt = NegativePrompt;
                    }
                }, null, "保存", "返回");
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage("[Illustrator] 打开负面提示词编辑器失败: " + ex.Message, Color.FromUint(4294901760u)));
            }
        }

        private static void QueueInjectedButtonRefresh()
        {
            IllustratorRuntime.Post(() =>
            {
                EncyclopediaHeroIllustrationPatch.RefreshInjectedButtons();
            });
        }

        private static string GetCacheFilePath()
        {
            if (string.IsNullOrEmpty(_cachedModelsFilePath))
            {
                try
                {
                    string docsDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    string configDir = Path.Combine(docsDir, "Mount and Blade II Bannerlord", "Configs", "AnimusForge");
                    if (!Directory.Exists(configDir))
                    {
                        Directory.CreateDirectory(configDir);
                    }
                    _cachedModelsFilePath = Path.Combine(configDir, "illustrator_models_cache.json");
                }
                catch
                {
                    _cachedModelsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "illustrator_models_cache.json");
                }
            }
            return _cachedModelsFilePath;
        }

        private static void TryLoadCachedModels()
        {
            try
            {
                string path = GetCacheFilePath();
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path, Encoding.UTF8);
                    var cached = JsonConvert.DeserializeObject<List<string>>(json);
                    if (cached != null && cached.Count > 0)
                    {
                        if (_modelOptions == null)
                        {
                            _modelOptions = new List<string> { "*手动输入*" };
                        }
                        foreach (var m in cached)
                        {
                            if (!string.IsNullOrWhiteSpace(m) && !_modelOptions.Contains(m))
                            {
                                _modelOptions.Add(m);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Print($"[Illustrator] Failed to load cached models: {ex.Message}");
            }
        }

        private static void SaveCachedModels(List<string> models)
        {
            try
            {
                string path = GetCacheFilePath();
                string json = JsonConvert.SerializeObject(models, Formatting.Indented);
                File.WriteAllText(path, json, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Debug.Print($"[Illustrator] Failed to save cached models: {ex.Message}");
            }
        }

        public static void RequestMcmRefresh()
        {
            try
            {
                AnimusForge.McmDropdownRuntimeRefresh.EnsurePatched();
                AnimusForge.McmDropdownRuntimeRefresh.RequestRefresh();
            }
            catch (Exception ex)
            {
                Debug.Print($"[Illustrator] McmDropdownRuntimeRefresh failed: {ex.Message}");
            }
        }

        private void EnsureModelDropdown()
        {
            lock (_modelLock)
            {
                if (_modelDropdown == null)
                {
                    TryLoadCachedModels();

                    if (_modelOptions == null || _modelOptions.Count == 0)
                    {
                        _modelOptions = new List<string> { "*手动输入*" };
                    }
                    if (!string.IsNullOrWhiteSpace(ModelName) && !_modelOptions.Contains(ModelName))
                    {
                        _modelOptions.Add(ModelName);
                    }
                    int idx = _modelOptions.IndexOf(ModelName);
                    _modelDropdown = new Dropdown<string>(_modelOptions, idx >= 0 ? idx : 0);
                }
            }
        }

        private bool _modelFetchInProgress;

        private void RequestModelListFetch()
        {
            if (!IllustratorRuntime.IsMainThread)
            {
                IllustratorRuntime.Post(RequestModelListFetch);
                return;
            }

            if (_modelFetchInProgress)
            {
                InformationManager.DisplayMessage(new InformationMessage("[AI生图] 模型列表正在拉取中，请稍候。", Color.FromUint(4294967040u)));
                return;
            }

            string baseUrl = (ApiBaseUrl ?? string.Empty).Trim().TrimEnd('/');
            string apiKey = (ApiKey ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                InformationManager.DisplayMessage(new InformationMessage("[AI生图] 请先填写生图 API 端点地址 (Base URL)！", Color.FromUint(4294901760u)));
                return;
            }

            _modelFetchInProgress = true;
            InformationManager.DisplayMessage(new InformationMessage("[AI生图] 正在拉取可用模型列表...", Color.FromUint(4294967040u)));

            bool started = IllustratorRuntime.Start(() => FetchModelListAsync(baseUrl, apiKey), (result, error) =>
            {
                _modelFetchInProgress = false;
                if (error != null)
                {
                    InformationManager.DisplayMessage(new InformationMessage($"[AI生图] 拉取模型异常: {error.Message}", Color.FromUint(4294901760u)));
                    return;
                }
                ApplyFetchedModels(result);
            });

            if (!started)
            {
                _modelFetchInProgress = false;
                InformationManager.DisplayMessage(new InformationMessage("[AI生图] 后台任务繁忙，请稍后重试。", Color.FromUint(4294901760u)));
            }
        }

        private sealed class ModelListFetchResult
        {
            public List<string> Models;
            public string Error;
        }

        private static async Task<ModelListFetchResult> FetchModelListAsync(string baseUrl, string apiKey)
        {
            try
            {
                string modelsUrl = baseUrl;
                if (modelsUrl.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase))
                    modelsUrl = modelsUrl.Substring(0, modelsUrl.Length - "/chat/completions".Length).TrimEnd('/');
                if (modelsUrl.EndsWith("/images/generations", StringComparison.OrdinalIgnoreCase))
                    modelsUrl = modelsUrl.Substring(0, modelsUrl.Length - "/images/generations".Length).TrimEnd('/');

                if (!modelsUrl.EndsWith("/models", StringComparison.OrdinalIgnoreCase))
                {
                    modelsUrl = modelsUrl.EndsWith("/v1", StringComparison.OrdinalIgnoreCase)
                        ? modelsUrl + "/models"
                        : modelsUrl + "/v1/models";
                }

                using (var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) })
                using (var request = new HttpRequestMessage(HttpMethod.Get, modelsUrl))
                {
                    if (!string.IsNullOrWhiteSpace(apiKey))
                    {
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                    }

                    using (var resp = await client.SendAsync(request).ConfigureAwait(false))
                    {
                        string json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                        if (!resp.IsSuccessStatusCode)
                        {
                            return new ModelListFetchResult { Error = $"拉取失败 (HTTP {(int)resp.StatusCode}): {json}" };
                        }

                        JObject parsed = JObject.Parse(json);
                        JArray data = parsed["data"] as JArray;
                        if (data == null || data.Count == 0)
                        {
                            return new ModelListFetchResult { Error = "接口返回成功，但未解析到可用模型数据。" };
                        }

                        var list = new List<string>();
                        foreach (var item in data)
                        {
                            string id = item["id"]?.ToString();
                            if (!string.IsNullOrWhiteSpace(id))
                            {
                                list.Add(id);
                            }
                        }

                        list.Sort((a, b) =>
                        {
                            bool aIsImg = a.IndexOf("image", StringComparison.OrdinalIgnoreCase) >= 0 || a.IndexOf("flux", StringComparison.OrdinalIgnoreCase) >= 0 || a.IndexOf("dall", StringComparison.OrdinalIgnoreCase) >= 0;
                            bool bIsImg = b.IndexOf("image", StringComparison.OrdinalIgnoreCase) >= 0 || b.IndexOf("flux", StringComparison.OrdinalIgnoreCase) >= 0 || b.IndexOf("dall", StringComparison.OrdinalIgnoreCase) >= 0;
                            if (aIsImg && !bIsImg) return -1;
                            if (!aIsImg && bIsImg) return 1;
                            return string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
                        });

                        SaveCachedModels(list);
                        return new ModelListFetchResult { Models = list };
                    }
                }
            }
            catch (Exception ex)
            {
                return new ModelListFetchResult { Error = ex.Message };
            }
        }

        private void ApplyFetchedModels(ModelListFetchResult result)
        {
            IllustratorRuntime.AssertMainThread();
            if (result == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("[AI生图] 拉取模型异常: 空结果", Color.FromUint(4294901760u)));
                return;
            }
            if (!string.IsNullOrWhiteSpace(result.Error))
            {
                InformationManager.DisplayMessage(new InformationMessage($"[AI生图] {result.Error}", Color.FromUint(4294901760u)));
                return;
            }

            lock (_modelLock)
            {
                _modelOptions = new List<string> { "*手动输入*" };
                _modelOptions.AddRange(result.Models);

                int selectedIdx = _modelOptions.IndexOf(ModelName);
                if (selectedIdx < 0) selectedIdx = _modelOptions.Count > 1 ? 1 : 0;

                _modelDropdown = new Dropdown<string>(_modelOptions, selectedIdx);
                if (selectedIdx > 0)
                {
                    string selectedModel = _modelOptions[selectedIdx];
                    ModelName = selectedModel;
                    if (Instance != null && !ReferenceEquals(Instance, this))
                    {
                        Instance.ModelName = selectedModel;
                    }
                }
            }

            SaveCurrentSettings();
            RequestMcmRefresh();
            InformationManager.DisplayMessage(new InformationMessage($"[AI生图] 成功获取 {result.Models.Count} 个可用模型！已优先选中: {ModelName}，下拉选单已即时刷新。", Color.FromUint(4278255360u)));
        }

        public static void SaveCurrentSettings()
        {
            try
            {
                if (BaseSettingsProvider.Instance != null)
                {
                    var target = Instance ?? (BaseSettingsProvider.Instance.GetSettings("AnimusForge_Illustrator_v1") as IllustratorSettings);
                    if (target != null)
                    {
                        BaseSettingsProvider.Instance.SaveSettings(target);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Print($"[Illustrator] SaveCurrentSettings failed: {ex.Message}");
            }
        }

        private void EnsureDirectorModelDropdown()
        {
            lock (_directorModelLock)
            {
                if (_directorModelDropdown == null)
                {
                    TryLoadCachedDirectorModels();

                    if (_directorModelOptions == null || _directorModelOptions.Count == 0)
                    {
                        _directorModelOptions = new List<string> { "*手动输入*", "*默认(复用正文API)*" };
                    }
                    if (!string.IsNullOrWhiteSpace(DirectorModelName) && !_directorModelOptions.Contains(DirectorModelName))
                    {
                        _directorModelOptions.Add(DirectorModelName);
                    }
                    int idx = string.IsNullOrWhiteSpace(DirectorModelName)
                        ? _directorModelOptions.IndexOf("*默认(复用正文API)*")
                        : _directorModelOptions.IndexOf(DirectorModelName);
                    _directorModelDropdown = new Dropdown<string>(_directorModelOptions, idx >= 0 ? idx : 0);
                }
            }
        }

        private bool _directorModelFetchInProgress;

        private void RequestDirectorModelListFetch()
        {
            if (!IllustratorRuntime.IsMainThread)
            {
                IllustratorRuntime.Post(RequestDirectorModelListFetch);
                return;
            }

            if (_directorModelFetchInProgress)
            {
                InformationManager.DisplayMessage(new InformationMessage("[视觉导演] 导演模型列表正在拉取中，请稍候。", Color.FromUint(4294967040u)));
                return;
            }

            string baseUrl = (DirectorApiBaseUrl ?? string.Empty).Trim().TrimEnd('/');
            string apiKey = (DirectorApiKey ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                if (TryGetHostChatEndpointForFetch(out string hostUrl, out string hostKey))
                {
                    baseUrl = hostUrl.TrimEnd('/');
                    if (string.IsNullOrWhiteSpace(apiKey))
                    {
                        apiKey = hostKey;
                    }
                    InformationManager.DisplayMessage(new InformationMessage("[视觉导演] 未配置独立端点，正在向主模块正文 API 拉取模型列表...", Color.FromUint(4294967040u)));
                }
                else
                {
                    InformationManager.DisplayMessage(new InformationMessage("[视觉导演] 请先填写导演 API 端点地址，或在主模块配置正文 API！", Color.FromUint(4294901760u)));
                    return;
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(apiKey) && TryGetHostChatEndpointForFetch(out _, out string hostKey))
                {
                    apiKey = hostKey;
                }
                InformationManager.DisplayMessage(new InformationMessage("[视觉导演] 正在向独立导演 API 端点拉取模型列表...", Color.FromUint(4294967040u)));
            }

            _directorModelFetchInProgress = true;

            bool started = IllustratorRuntime.Start(() => FetchDirectorModelListAsync(baseUrl, apiKey), (result, error) =>
            {
                _directorModelFetchInProgress = false;
                if (error != null)
                {
                    InformationManager.DisplayMessage(new InformationMessage($"[视觉导演] 拉取模型异常: {error.Message}", Color.FromUint(4294901760u)));
                    return;
                }
                ApplyFetchedDirectorModels(result);
            });

            if (!started)
            {
                _directorModelFetchInProgress = false;
                InformationManager.DisplayMessage(new InformationMessage("[视觉导演] 后台任务繁忙，请稍后重试。", Color.FromUint(4294901760u)));
            }
        }

        private static async Task<ModelListFetchResult> FetchDirectorModelListAsync(string baseUrl, string apiKey)
        {
            try
            {
                string modelsUrl = baseUrl;
                if (modelsUrl.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase))
                    modelsUrl = modelsUrl.Substring(0, modelsUrl.Length - "/chat/completions".Length).TrimEnd('/');
                if (modelsUrl.EndsWith("/images/generations", StringComparison.OrdinalIgnoreCase))
                    modelsUrl = modelsUrl.Substring(0, modelsUrl.Length - "/images/generations".Length).TrimEnd('/');

                if (!modelsUrl.EndsWith("/models", StringComparison.OrdinalIgnoreCase))
                {
                    modelsUrl = modelsUrl.EndsWith("/v1", StringComparison.OrdinalIgnoreCase)
                        ? modelsUrl + "/models"
                        : modelsUrl + "/v1/models";
                }

                using (var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) })
                using (var request = new HttpRequestMessage(HttpMethod.Get, modelsUrl))
                {
                    if (!string.IsNullOrWhiteSpace(apiKey))
                    {
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                    }

                    using (var resp = await client.SendAsync(request).ConfigureAwait(false))
                    {
                        string json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                        if (!resp.IsSuccessStatusCode)
                        {
                            return new ModelListFetchResult { Error = $"拉取失败 (HTTP {(int)resp.StatusCode}): {json}" };
                        }

                        JObject parsed = JObject.Parse(json);
                        JArray data = parsed["data"] as JArray;
                        if (data == null || data.Count == 0)
                        {
                            return new ModelListFetchResult { Error = "接口返回成功，但未解析到可用模型数据。" };
                        }

                        var list = new List<string>();
                        foreach (var item in data)
                        {
                            string id = item["id"]?.ToString();
                            if (!string.IsNullOrWhiteSpace(id))
                            {
                                list.Add(id);
                            }
                        }

                        list.Sort((a, b) =>
                        {
                            bool aIsChat = a.IndexOf("gpt", StringComparison.OrdinalIgnoreCase) >= 0 || a.IndexOf("qwen", StringComparison.OrdinalIgnoreCase) >= 0 || a.IndexOf("deepseek", StringComparison.OrdinalIgnoreCase) >= 0 || a.IndexOf("claude", StringComparison.OrdinalIgnoreCase) >= 0;
                            bool bIsChat = b.IndexOf("gpt", StringComparison.OrdinalIgnoreCase) >= 0 || b.IndexOf("qwen", StringComparison.OrdinalIgnoreCase) >= 0 || b.IndexOf("deepseek", StringComparison.OrdinalIgnoreCase) >= 0 || b.IndexOf("claude", StringComparison.OrdinalIgnoreCase) >= 0;
                            if (aIsChat && !bIsChat) return -1;
                            if (!aIsChat && bIsChat) return 1;
                            return string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
                        });

                        SaveCachedDirectorModels(list);
                        return new ModelListFetchResult { Models = list };
                    }
                }
            }
            catch (Exception ex)
            {
                return new ModelListFetchResult { Error = ex.Message };
            }
        }

        private void ApplyFetchedDirectorModels(ModelListFetchResult result)
        {
            IllustratorRuntime.AssertMainThread();
            if (result == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("[视觉导演] 拉取模型异常: 空结果", Color.FromUint(4294901760u)));
                return;
            }
            if (!string.IsNullOrWhiteSpace(result.Error))
            {
                InformationManager.DisplayMessage(new InformationMessage($"[视觉导演] {result.Error}", Color.FromUint(4294901760u)));
                return;
            }

            lock (_directorModelLock)
            {
                _directorModelOptions = new List<string> { "*手动输入*", "*默认(复用正文API)*" };
                _directorModelOptions.AddRange(result.Models);

                int selectedIdx = string.IsNullOrWhiteSpace(DirectorModelName)
                    ? 1
                    : _directorModelOptions.IndexOf(DirectorModelName);
                if (selectedIdx < 0) selectedIdx = 0;

                _directorModelDropdown = new Dropdown<string>(_directorModelOptions, selectedIdx);
            }

            SaveCurrentSettings();
            RequestMcmRefresh();
            InformationManager.DisplayMessage(new InformationMessage($"[视觉导演] 成功获取 {result.Models.Count} 个可用模型！下拉选单已即时刷新。", Color.FromUint(4278255360u)));
        }

        private static string GetDirectorCacheFilePath()
        {
            if (string.IsNullOrEmpty(_cachedDirectorModelsFilePath))
            {
                try
                {
                    string docsDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    string configDir = Path.Combine(docsDir, "Mount and Blade II Bannerlord", "Configs", "AnimusForge");
                    if (!Directory.Exists(configDir))
                    {
                        Directory.CreateDirectory(configDir);
                    }
                    _cachedDirectorModelsFilePath = Path.Combine(configDir, "illustrator_director_models_cache.json");
                }
                catch
                {
                    _cachedDirectorModelsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "illustrator_director_models_cache.json");
                }
            }
            return _cachedDirectorModelsFilePath;
        }

        private static void TryLoadCachedDirectorModels()
        {
            try
            {
                string path = GetDirectorCacheFilePath();
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path, Encoding.UTF8);
                    var cached = JsonConvert.DeserializeObject<List<string>>(json);
                    if (cached != null && cached.Count > 0)
                    {
                        if (_directorModelOptions == null)
                        {
                            _directorModelOptions = new List<string> { "*手动输入*", "*默认(复用正文API)*" };
                        }
                        foreach (var m in cached)
                        {
                            if (!string.IsNullOrWhiteSpace(m) && !_directorModelOptions.Contains(m))
                            {
                                _directorModelOptions.Add(m);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Print($"[Illustrator] Failed to load cached director models: {ex.Message}");
            }
        }

        private static void SaveCachedDirectorModels(List<string> models)
        {
            try
            {
                string path = GetDirectorCacheFilePath();
                string json = JsonConvert.SerializeObject(models, Formatting.Indented);
                File.WriteAllText(path, json, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Debug.Print($"[Illustrator] Failed to save cached director models: {ex.Message}");
            }
        }

        private static bool TryGetHostChatEndpointForFetch(out string apiUrl, out string apiKey)
        {
            apiUrl = string.Empty;
            apiKey = string.Empty;
            try
            {
                Type duelSettingsType = HarmonyLib.AccessTools.TypeByName("AnimusForge.DuelSettings");
                if (duelSettingsType != null)
                {
                    System.Reflection.MethodInfo getSettingsMethod = HarmonyLib.AccessTools.Method(duelSettingsType, "GetSettings");
                    object hostSettings = getSettingsMethod?.Invoke(null, null);
                    if (hostSettings != null)
                    {
                        System.Reflection.PropertyInfo apiKeyProp = HarmonyLib.AccessTools.Property(duelSettingsType, "ApiKey");
                        System.Reflection.PropertyInfo apiUrlProp = HarmonyLib.AccessTools.Property(duelSettingsType, "ApiUrl");
                        System.Reflection.MethodInfo getEffectiveUrlMethod = HarmonyLib.AccessTools.Method(duelSettingsType, "GetEffectiveApiUrl", new[] { typeof(string) });

                        apiKey = (apiKeyProp?.GetValue(hostSettings) as string ?? string.Empty).Trim();
                        string rawUrl = (apiUrlProp?.GetValue(hostSettings) as string ?? string.Empty).Trim();
                        if (getEffectiveUrlMethod != null)
                        {
                            apiUrl = (getEffectiveUrlMethod.Invoke(null, new object[] { rawUrl }) as string ?? rawUrl).Trim();
                        }
                        else
                        {
                            apiUrl = rawUrl;
                        }
                        return !string.IsNullOrWhiteSpace(apiUrl);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Print($"[Illustrator] TryGetHostChatEndpointForFetch failed: {ex.Message}");
            }
            return false;
        }
    }
}
