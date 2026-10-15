using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using MCM.Abstractions;
using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Base.Global;
using TaleWorlds.Localization;
using MCM.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TaleWorlds.Library;
using AnimusForge.Illustrator.Core;
using AnimusForge.Illustrator.Engine;
using AnimusForge.Illustrator.UI;
using AnimusForge.Illustrator.UI.Patches;

namespace AnimusForge.Illustrator
{
    public class IllustratorSettings : AttributeGlobalSettings<IllustratorSettings>
    {
        public override string Id => "AnimusForge_Illustrator_v1";
        public override string DisplayName => new TextObject("{=AF_ILL_DISPLAY}AnimusForge - AI Illustrator").ToString();
        public override string FolderName => IllustratorSettingsStorage.DirectoryKey;
        public override string FormatType => "json";

        private const string ManualModelOption = "{=AF_ILL_UI_OPTION_MANUAL_MODEL}*Manual input*";
        private const string Player2ModelOption = "{=AF_ILL_UI_OPTION_PLAYER2_MODEL}Use the model selected in the Player2 app";

        private static bool IsManualModelOption(string value) => value == ManualModelOption || value == IllustratorText.Raw("Residual_IllustratorSettings_5aa017883627");
        private static bool IsPlayer2ModelOption(string value) => value == Player2ModelOption || value == IllustratorText.Raw("Residual_IllustratorSettings_ca5c63c47021");

        // Normalize only when MCM loads a setting/preset; API model names remain raw values.
        private static Dropdown<string> NormalizeModelOptions(Dropdown<string> value)
        {
            if (value == null) return null;
            var options = new List<string>(value.Count);
            foreach (string option in value)
                options.Add(IsManualModelOption(option) ? ManualModelOption
                    : IsPlayer2ModelOption(option) ? Player2ModelOption : option);
            return new Dropdown<string>(options, value.SelectedIndex);
        }

        private List<string> _modelOptions = new List<string> { ManualModelOption };
        private Dropdown<string> _modelDropdown;
        private static readonly object _modelLock = new object();
        private static string _cachedModelsFilePath;


        public IllustratorSettings()
        {
            IllustratorSettingsStorage.EnsureReady();
            FetchModelList = RequestModelListFetch;
            TestImageApi = () => IllustratorApiTest.StartOrCancel(this);
            BindPromptEditors();
        }

        internal void BindPromptEditors()
        {
            EditCustomStylePrompt = OpenCustomStylePromptEditor;
            EditNegativePrompt = OpenNegativePromptEditor;
            EditCustomDirectorPrompt = OpenCustomDirectorPromptEditor;
        }

        private bool _enableImageGeneration = true;

        [SettingPropertyBool("{=AF_ILL_ENABLED}Enable image generation", HintText = "{=AF_ILL_ENABLED_HINT}Master switch for all image-generation paths. When disabled, encyclopedia, conversation, bulletin, and gallery entries will not send image requests.", Order = 0, RequireRestart = false)]
        [SettingPropertyGroup("{=AF_ILL_GROUP_BASIC}1. Basic Settings", GroupOrder = 1)]
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
        [SettingPropertyBool("{=AF_ILL_OFFSCREEN_ENV}Enable off-screen scene rendering (environment panorama)", HintText = "{=AF_ILL_OFFSCREEN_ENV_HINT}When enabled, conversation illustrations render a panorama within 30 meters of the player in the background for buildings, furnishings, and materials. Capture takes a few seconds and uses some GPU. When disabled, the environment uses the current screenshot and textual facts. Character portraits always render off-screen. World-map conversations do not use panoramas.", Order = 1, RequireRestart = false)]
        [SettingPropertyGroup("{=AF_ILL_GROUP_BASIC}1. Basic Settings", GroupOrder = 1)]
        public bool EnableSceneOffscreenRendering { get; set; } = true;

        [SettingPropertyBool("{=AF_ILL_CLEAN_TEMP}Clean temporary files after generation", HintText = "{=AF_ILL_CLEAN_TEMP_HINT}Each extraction task cleans only its own off-screen exports when it finishes. When disabled, heraldry exports are kept for troubleshooting; loaded portrait files still follow the normal release flow. Gallery caches and default illustrations are unaffected.", Order = 3, RequireRestart = false)]
        [SettingPropertyGroup("{=AF_ILL_GROUP_BASIC}1. Basic Settings", GroupOrder = 1)]
        public bool AutoCleanTempFiles { get; set; } = true;

        [SettingPropertyText("{=AF_ILL_IMAGE_BASE_URL}Image API endpoint (Base URL)", HintText = "{=AF_ILL_IMAGE_BASE_URL_HINT}For compatible APIs, enter the service root or /v1. Google native APIs accept the v1beta root or a complete /models/name:generateContent URL. Player2 is detected automatically at http://127.0.0.1:4315 and chooses its model in the Player2 app. With a reference image, /images/edits is preferred; without one, /images/generations is used. The model must support the selected channel.", Order = 1, RequireRestart = false)]
        [SettingPropertyGroup("{=AF_ILL_GROUP_IMAGE_API}2. Image API (OpenAI-compatible)", GroupOrder = 2)]
        public string ApiBaseUrl { get; set; } = "https://yjapi.shenlanqaq.com/v1";

        [SettingPropertyText("{=AF_ILL_IMAGE_KEY}Image API key (API Key)", HintText = "{=AF_ILL_IMAGE_KEY_HINT}API key for authentication. Leave blank for endpoints that need no authentication or local proxies.", Order = 3, RequireRestart = false)]
        [SettingPropertyGroup("{=AF_ILL_GROUP_IMAGE_API}2. Image API (OpenAI-compatible)", GroupOrder = 2)]
        public string ApiKey { get; set; } = "";

        [SettingPropertyButton("{=AF_ILL_FETCH_IMAGE_MODELS}Fetch image model list", Content = "{=AF_ILL_FETCH}Fetch", Order = 4, RequireRestart = false, HintText = "{=AF_ILL_FETCH_IMAGE_MODELS_HINT}Queries the configured endpoint for models. Google native mode uses /models and native authentication. Only the current response page is read; enter a model manually if it is not listed. Image and art models are prioritized.")]
        [SettingPropertyGroup("{=AF_ILL_GROUP_IMAGE_API}2. Image API (OpenAI-compatible)", GroupOrder = 2)]
        public Action FetchModelList { get; set; }

        private string _modelName = "black-forest-labs/FLUX.1-schnell";

        [SettingPropertyText("{=AF_ILL_IMAGE_MODEL}Image model name (Model)", HintText = "{=AF_ILL_IMAGE_MODEL_HINT}Image model name. Supports standard image models and native multimodal conversation image models; the channel is detected automatically.", Order = 5, RequireRestart = false)]
        [SettingPropertyGroup("{=AF_ILL_GROUP_IMAGE_API}2. Image API (OpenAI-compatible)", GroupOrder = 2)]
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

        [SettingPropertyDropdown("{=AF_ILL_SELECT_IMAGE_MODEL}Select image model (dropdown)", Order = 6, RequireRestart = false, HintText = "{=AF_ILL_SELECT_IMAGE_MODEL_HINT}After fetching models above, select one here. Choosing “*manual input*” uses the model name entered above.")]
        [SettingPropertyGroup("{=AF_ILL_GROUP_IMAGE_API}2. Image API (OpenAI-compatible)", GroupOrder = 2)]
        public Dropdown<string> ModelDropdown
        {
            get { EnsureModelDropdown(); return _modelDropdown; }
            set
            {
                lock (_modelLock)
                {
                    // Isolate active settings and preset selections.
                    ReplaceModelDropdown(NormalizeModelOptions(value));
                    ModelSelectionChanged(_modelDropdown, new PropertyChangedEventArgs(nameof(Dropdown<string>.SelectedIndex)));
                }
            }
        }

        private void ReplaceModelDropdown(Dropdown<string> value)
        {
            if (_modelDropdown != null) _modelDropdown.PropertyChanged -= ModelSelectionChanged;
            _modelDropdown = value;
            if (_modelDropdown != null) _modelDropdown.PropertyChanged += ModelSelectionChanged;
        }

        private void ModelSelectionChanged(object sender, PropertyChangedEventArgs args)
        {
            if (!ReferenceEquals(sender, _modelDropdown) || args.PropertyName != nameof(Dropdown<string>.SelectedIndex)) return;
            var dropdown = _modelDropdown;
            if (dropdown == null || dropdown.SelectedIndex < 0 || dropdown.SelectedIndex >= dropdown.Count) return;
            string selected = dropdown[dropdown.SelectedIndex];
            if (string.IsNullOrWhiteSpace(selected) || IsManualModelOption(selected) || IsPlayer2ModelOption(selected) || _modelName == selected) return;
            _modelName = selected;
            OnPropertyChanged(nameof(ModelName));
        }


        private void SyncModelDropdownWithModelName(string modelName)
        {
            lock (_modelLock)
            {
                EnsureModelDropdown();
                if (_modelOptions == null || _modelOptions.Count == 0 || _modelDropdown == null) return;
                int idx = _modelDropdown.IndexOf(modelName);
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
                    return _sizePresetTokens[_sizePresetDropdown.SelectedIndex];
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
            "{=AF_ILL_UI_OPTION_SIZE_SQUARE}1024x1024 (1:1 square)",
            "{=AF_ILL_UI_OPTION_SIZE_WIDE}1280x720 (16:9 widescreen)",
            "{=AF_ILL_UI_OPTION_SIZE_PORTRAIT}720x1280 (9:16 character portrait)",
            "{=AF_ILL_UI_OPTION_SIZE_EPIC}1344x768 (7:4 epic widescreen)",
            "{=AF_ILL_UI_OPTION_SIZE_POSTER}1024x1536 (2:3 portrait poster)",
            "{=AF_ILL_UI_OPTION_SIZE_UHD}2048x2048 (1:1 ultra-high-definition square)"
        };
        private static readonly string[] _sizePresetTokens = { "1024x1024", "1280x720", "720x1280", "1344x768", "1024x1536", "2048x2048" };
        private Dropdown<string> _sizePresetDropdown;

        private static int FindSizePresetIndex(string size)
        {
            string token = NormalizeImageSize(size);
            for (int i = 0; i < _sizePresetOptions.Count; i++)
            {
                if (string.Equals(_sizePresetTokens[i], token, StringComparison.OrdinalIgnoreCase))
                    return i;
            }

            return -1;
        }

        private static string NormalizeImageSize(string size)
        {
            int index = FindSizePresetIndexWithoutNormalization(size);
            return index >= 0 ? _sizePresetTokens[index] : "1024x1024";
        }

        private static int FindSizePresetIndexWithoutNormalization(string size)
        {
            string token = (size ?? string.Empty).Trim();
            for (int i = 0; i < _sizePresetOptions.Count; i++)
            {
                if (string.Equals(_sizePresetTokens[i], token, StringComparison.OrdinalIgnoreCase))
                    return i;
            }

            return -1;
        }

        [SettingPropertyDropdown("{=AF_ILL_IMAGE_SIZE}Image resolution (Size)", Order = 7, RequireRestart = false, HintText = "{=AF_ILL_IMAGE_SIZE_HINT}Bulletin and fullscreen scene illustrations convert presets to 16:9: the 1024 square and 1280/720 presets become 1280×720; the 1344/1536 presets become 1536×864; the 2048 square becomes 2048×1152. Encyclopedia and standalone-panel scene illustrations use the selected size. The image service must support the corresponding size.")]
        [SettingPropertyGroup("{=AF_ILL_GROUP_IMAGE_API}2. Image API (OpenAI-compatible)", GroupOrder = 2)]
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
                _sizePresetDropdown = value == null ? null : new Dropdown<string>(_sizePresetOptions, value.SelectedIndex);
                int idx = value?.SelectedIndex ?? 0;
                if (idx > 0 && idx < _sizePresetOptions.Count)
                {
                    string size = _sizePresetTokens[idx];
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
                    _imageSize = _sizePresetTokens[0];
                }
            }
        }

        private static readonly List<string> _qualityOptions = new List<string>
        {
            "{=AF_ILL_UI_OPTION_QUALITY_DEFAULT}Default (omit parameter)",
            "{=AF_ILL_UI_OPTION_QUALITY_STANDARD}standard (standard)",
            "{=AF_ILL_UI_OPTION_QUALITY_HD}hd (high detail)",
            "{=AF_ILL_UI_OPTION_QUALITY_LOW}low (fastest generation)",
            "{=AF_ILL_UI_OPTION_QUALITY_MEDIUM}medium (balanced)",
            "{=AF_ILL_UI_OPTION_QUALITY_HIGH}high (fine rendering)",
            "{=AF_ILL_UI_OPTION_QUALITY_AUTO}auto (server decides)"
        };
        private Dropdown<string> _qualityDropdown;

        private static readonly string[] _qualityTokens = { "", "standard", "hd", "low", "medium", "high", "auto" };

        [SettingPropertyDropdown("{=AF_ILL_QUALITY}Image quality preset (Quality)", Order = 8, RequireRestart = false, HintText = "{=AF_ILL_QUALITY_HINT}standard/hd target DALL-E-3; low/medium/high/auto target gpt-image-1 and compatible proxies. Default omits the parameter to avoid unsupported-model errors. Conversation image channels convert this to a quality prompt.")]
        [SettingPropertyGroup("{=AF_ILL_GROUP_IMAGE_API}2. Image API (OpenAI-compatible)", GroupOrder = 2)]
        public Dropdown<string> QualityDropdown
        {
            get
            {
                if (_qualityDropdown == null)
                    _qualityDropdown = new Dropdown<string>(_qualityOptions, 0);
                return _qualityDropdown;
            }
            set => _qualityDropdown = value == null ? null : new Dropdown<string>(_qualityOptions, value.SelectedIndex);
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
            "{=AF_ILL_UI_OPTION_STYLE_CLASSIC}Classical oil painting (default)",
            "{=AF_ILL_UI_OPTION_STYLE_VIVID}vivid (vivid colors; API enum)",
            "{=AF_ILL_UI_OPTION_STYLE_NATURAL}natural (natural realism; API enum)",
            "{=AF_ILL_UI_OPTION_STYLE_DARK}Dark epic realism (prompt instructions)",
            "{=AF_ILL_UI_OPTION_STYLE_CINEMATIC}Cinematic lighting (prompt instructions)",
            "{=AF_ILL_UI_OPTION_STYLE_CUSTOM}Prompt (custom style)",
            "{=AF_ILL_UI_OPTION_STYLE_MOSAN}Mosan art: Meuse enamel decoration (prompt instructions)",
            "{=AF_ILL_UI_OPTION_STYLE_ANIME}Japanese semi-realistic narrative illustration (prompt instructions)"
        };
        private Dropdown<string> _styleDropdown;

        [SettingPropertyDropdown("{=AF_ILL_STYLE}Generation style (Style)", Order = 9, RequireRestart = false, HintText = "{=AF_ILL_STYLE_HINT}vivid/natural are official OpenAI style enums. Other presets inject style instructions and negative terms into the prompt instead of sending a style parameter. Choosing “prompt” uses the custom text below.")]
        [SettingPropertyGroup("{=AF_ILL_GROUP_IMAGE_API}2. Image API (OpenAI-compatible)", GroupOrder = 2)]
        public Dropdown<string> StyleDropdown
        {
            get
            {
                if (_styleDropdown == null)
                    _styleDropdown = new Dropdown<string>(_styleOptions, 0); // 默认 古典油画
                return _styleDropdown;
            }
            set => _styleDropdown = value == null ? null : new Dropdown<string>(_styleOptions, value.SelectedIndex);
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
                    case 7: return "narrative-anime";
                    default: return "classic-oil";
                }
            }
        }

        [SettingPropertyButton("{=AF_ILL_CUSTOM_STYLE}Custom style prompt (active when Style=prompt)", Content = "{=AF_ILL_OPEN_EDITOR}Open editor", Order = 10, RequireRestart = false, HintText = "{=AF_ILL_CUSTOM_STYLE_HINT}Open the long-text editor to edit style instructions. Active only when the style preset is “prompt”; injected into every image channel. A classical oil-painting preset is provided as a reference.")]
        [SettingPropertyGroup("{=AF_ILL_GROUP_IMAGE_API}2. Image API (OpenAI-compatible)", GroupOrder = 2)]
        public Action EditCustomStylePrompt { get; set; }

        // 预填古典油画预设内容，供玩家查看/改写；仅在画风预设选“提示词”时生效
        public string CustomStylePrompt { get; set; } = IllustratorText.Get("DefaultCustomStyle");

        [SettingPropertyBool("{=AF_ILL_REFERENCE_IMAGE}Send reference image to image model", HintText = "{=AF_ILL_REFERENCE_IMAGE_HINT}Sends captured 3D character references with the request through supported multimodal and image-edit channels. When disabled, references are used only by the prompt director.", Order = 10, RequireRestart = false)]
        [SettingPropertyGroup("{=AF_ILL_GROUP_IMAGE_API}2. Image API (OpenAI-compatible)", GroupOrder = 2)]
        public bool EnableReferenceImageForGeneration { get; set; } = true;

        [SettingPropertyBool("{=AF_ILL_GEAR_FIDELITY}Preserve AF equipment", Order = 11, RequireRestart = false, HintText = "{=AF_ILL_GEAR_FIDELITY_HINT}Preserves equipment identity while allowing narrative changes such as tears, stains, or wetness. When disabled, clothing, armor, weapons, and accessories follow custom director rules; identity and established facts remain. Affects new requests only and never changes game equipment.")]
        [SettingPropertyGroup("{=AF_ILL_GROUP_IMAGE_API}2. Image API (OpenAI-compatible)", GroupOrder = 2)]
        public bool PreserveEquipmentFidelity { get; set; } = true;


        [SettingPropertyButton("{=AF_ILL_NEGATIVE_PROMPT}Negative prompt (Negative Prompt)", Content = "{=AF_ILL_OPEN_EDITOR}Open editor", Order = 11, RequireRestart = false, HintText = "{=AF_ILL_NEGATIVE_PROMPT_HINT}Open the long-text editor and list elements to exclude from the image. Active only for the custom prompt style; appended after preset negative terms.")]
        [SettingPropertyGroup("{=AF_ILL_GROUP_IMAGE_API}2. Image API (OpenAI-compatible)", GroupOrder = 2)]
        public Action EditNegativePrompt { get; set; }

        // 预填古典油画预设负面词，供玩家查看/改写；仅在画风预设选“提示词(自定义画风)”时生效
        public string NegativePrompt { get; set; } = IllustratorText.Get("DefaultNegativePrompt");

        [SettingPropertyInteger("{=AF_ILL_RANDOMNESS}Randomness", 0, 100, "0", Order = 12, RequireRestart = false, HintText = "{=AF_ILL_RANDOMNESS_HINT}At 0, no random prompt is added. Higher values encourage variation in framing, negative space, depth of field, and lighting while preserving identity, equipment, heraldry, and established facts. This controls prompt guidance, not model sampling.")]
        [SettingPropertyGroup("{=AF_ILL_GROUP_IMAGE_API}2. Image API (OpenAI-compatible)", GroupOrder = 2)]
        public int Randomness { get; set; } = 50;

        private int _imageGenerationTimeoutSeconds = 240;

        [SettingPropertyInteger("{=AF_ILL_IMAGE_TIMEOUT}Image request timeout", 60, 600, "{=AF_ILL_UNIT_SECONDS}0 sec", Order = 13, RequireRestart = false, HintText = "{=AF_ILL_IMAGE_TIMEOUT_HINT}Maximum total wait for one image request; the default is 240 seconds. Includes Edits attempts, conversation-channel fallback, and downloading the result image. Increase it for high-quality redraws with multiple reference images or a slow relay. Applies to the next image request and does not affect visual director requests or off-screen capture timeouts.")]
        [SettingPropertyGroup("{=AF_ILL_GROUP_IMAGE_API}2. Image API (OpenAI-compatible)", GroupOrder = 2)]
        public int ImageGenerationTimeoutSeconds
        {
            get => _imageGenerationTimeoutSeconds;
            set => _imageGenerationTimeoutSeconds = Math.Max(60, Math.Min(600, value));
        }

        // Retained for old config/code compatibility; protocol selection is automatic and this
        // preference is intentionally no longer exposed in MCM.
        public bool PreferChatImageProtocol { get; set; } = false;


        [SettingPropertyInteger("{=AF_ILL_DIRECTOR_MAX_TOKENS}Director maximum output tokens", 512, 64000, "0 Token", HintText = "{=AF_ILL_DIRECTOR_MAX_TOKENS_HINT}max_tokens sent to the director API, default 25000. This is an output budget, not a target length; configure it within the API limits.", Order = 6, RequireRestart = false)]
        [SettingPropertyGroup("{=AF_ILL_GROUP_DIRECTOR_API}3. Visual Director (uses main API)", GroupOrder = 3)]
        public int DirectorApiMaxTokens { get; set; } = 25000;

        [SettingPropertyInteger("{=AF_ILL_DIRECTOR_REFERENCE_TOKENS}Director prompt reference length (approx. tokens)", 600, 4000, "0 Token", HintText = "{=AF_ILL_DIRECTOR_REFERENCE_TOKENS_HINT}A soft length reference, not the API output limit. When forced reduction is enabled, the character budget takes priority; otherwise the complete reply may contain at most 30000 characters. The full four sections are retained without padding.", Order = 9, RequireRestart = false)]
        [SettingPropertyGroup("{=AF_ILL_GROUP_DIRECTOR_API}3. Visual Director (uses main API)", GroupOrder = 3)]
        public int DirectorMaxTokens { get; set; } = 2000;

        [SettingPropertyButton("{=AF_ILL_CUSTOM_DIRECTOR}Custom director prompt", Content = "{=AF_ILL_OPEN_EDITOR}Open editor", Order = 10, RequireRestart = false,
            HintText = "{=AF_ILL_CUSTOM_DIRECTOR_HINT}Set composition, action, shot, and narrative preferences for encyclopedia, meetings, and bulletins. Blank keeps defaults. Sent only to the Visual Director and cannot override known facts or output format.")]
        [SettingPropertyGroup("{=AF_ILL_GROUP_DIRECTOR_API}3. Visual Director (uses main API)", GroupOrder = 3)]
        public Action EditCustomDirectorPrompt { get; set; }

        // Hidden editor text is persisted by IllustratorPromptSettingsPersistence in the existing MCM JSON.
        public string CustomDirectorPrompt { get; set; } = "";

        [SettingPropertyBool("{=AF_ILL_AUTO_GENERATE}Generate images automatically", HintText = "{=AF_ILL_AUTO_GENERATE_HINT}Active only when image generation is enabled. When on, illustrations are generated during bulletin creation; when off, they are generated when the panel opens and the cache misses.", Order = 1, RequireRestart = false)]
        [SettingPropertyGroup("{=AF_ILL_GROUP_DISPLAY}4. Bulletins and Display Scenes", GroupOrder = 4)]
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

        [SettingPropertyBool("{=AF_ILL_AUTO_REDRAW}Redraw scene illustration after NPC replies", HintText = "{=AF_ILL_AUTO_REDRAW_HINT}After the player generates the first illustration, each NPC reply redraws one automatically. Later requests reuse the initial panorama, references, and stable facts while updating the latest two dialogue turns and actions.", Order = 4, RequireRestart = false)]
        [SettingPropertyGroup("{=AF_ILL_GROUP_DISPLAY}4. Bulletins and Display Scenes", GroupOrder = 4)]
        public bool AutoGenerateConversationIllustrationFullscreen { get; set; } = false;

        private static readonly List<string> _conversationDisplayOptions = new List<string>
        {
            "{=AF_ILL_UI_OPTION_DISPLAY_PANEL}Standalone panel",
            "{=AF_ILL_UI_OPTION_DISPLAY_FULLSCREEN}Fullscreen overlay (default)"
        };
        private Dropdown<string> _conversationDisplayDropdown;

        [SettingPropertyDropdown("{=AF_ILL_DISPLAY_MODE}Illustration display mode", Order = 5, RequireRestart = false, HintText = "{=AF_ILL_DISPLAY_MODE_HINT}Standalone panel uses the scene illustration panel. Fullscreen overlay fills the conversation background while preserving controls and input. It hides automatically for encyclopedia and menus. Automatic redraw uses this choice.")]
        [SettingPropertyGroup("{=AF_ILL_GROUP_DISPLAY}4. Bulletins and Display Scenes", GroupOrder = 4)]
        public Dropdown<string> ConversationDisplayDropdown
        {
            get
            {
                if (_conversationDisplayDropdown == null)
                    _conversationDisplayDropdown = new Dropdown<string>(_conversationDisplayOptions, 1);
                return _conversationDisplayDropdown;
            }
            set => _conversationDisplayDropdown = value == null ? null : new Dropdown<string>(_conversationDisplayOptions, value.SelectedIndex);
        }

        public bool ConversationIllustrationUsesFullscreen
        {
            get => (_conversationDisplayDropdown?.SelectedIndex ?? 1) == 1;
        }

        private int _portraitExportTimeoutSeconds = 20;
        private int _panoramaFaceExportTimeoutSeconds = 6;
        private int _panoramaCaptureTimeoutSeconds = 40;

        [SettingPropertyInteger("{=AF_ILL_PORTRAIT_TIMEOUT}Character portrait export timeout", 10, 60, "{=AF_ILL_UNIT_SECONDS}0 sec", Order = 2, RequireRestart = false,
            HintText = "{=AF_ILL_PORTRAIT_TIMEOUT_HINT}Maximum wait for each off-screen portrait, default 20 seconds. Applies to the next extraction and does not affect AI API timeouts.")]
        [SettingPropertyGroup("{=AF_ILL_GROUP_STORAGE}5. Storage and Performance", GroupOrder = 5)]
        public int PortraitExportTimeoutSeconds
        {
            get => _portraitExportTimeoutSeconds;
            set => _portraitExportTimeoutSeconds = Math.Max(10, Math.Min(60, value));
        }

        [SettingPropertyInteger("{=AF_ILL_PANORAMA_DIRECTION_TIMEOUT}Panorama direction export timeout", 3, 15, "{=AF_ILL_UNIT_SECONDS}0 sec", Order = 3, RequireRestart = false,
            HintText = "{=AF_ILL_PANORAMA_DIRECTION_TIMEOUT_HINT}Maximum wait for each panorama direction PNG, default 6 seconds. Six directions are captured sequentially and remain subject to the total timeout.")]
        [SettingPropertyGroup("{=AF_ILL_GROUP_STORAGE}5. Storage and Performance", GroupOrder = 5)]
        public int PanoramaFaceExportTimeoutSeconds
        {
            get => _panoramaFaceExportTimeoutSeconds;
            set => _panoramaFaceExportTimeoutSeconds = Math.Max(3, Math.Min(15, value));
        }

        [SettingPropertyInteger("{=AF_ILL_PANORAMA_TOTAL_TIMEOUT}Environment panorama total timeout", 30, 120, "{=AF_ILL_UNIT_SECONDS}0 sec", Order = 4, RequireRestart = false,
            HintText = "{=AF_ILL_PANORAMA_TOTAL_TIMEOUT_HINT}Total budget for stage extraction, environment copy construction, and six-direction capture, default 40 seconds. Stops when the budget is reached and does not send missing or duplicate images.")]
        [SettingPropertyGroup("{=AF_ILL_GROUP_STORAGE}5. Storage and Performance", GroupOrder = 5)]
        public int PanoramaCaptureTimeoutSeconds
        {
            get => _panoramaCaptureTimeoutSeconds;
            set => _panoramaCaptureTimeoutSeconds = Math.Max(30, Math.Min(120, value));
        }

        [SettingPropertyInteger("{=AF_ILL_CACHE_LIMIT}Maximum local cache images", 20, 1000, "{=AF_ILL_UNIT_IMAGES}0 images", HintText = "{=AF_ILL_CACHE_LIMIT_HINT}Maximum number of generated images kept in the persistent local cache to avoid duplicate requests.", Order = 1, RequireRestart = false)]
        [SettingPropertyGroup("{=AF_ILL_GROUP_STORAGE}5. Storage and Performance", GroupOrder = 5)]
        public int MaxCacheCount { get; set; } = 200;

        // 提示词扩写：底层永久自动开启，无 MCM 开关
        public bool EnableLlmPromptExpansion { get; set; } = true;

        private void OpenCustomStylePromptEditor()
        {
            try
            {
                string initialText = CustomStylePrompt ?? "";
                DevTextEditorHelper.ShowLongTextEditor(
                    IllustratorUiText.Resolve("AF_ILL_UI_SETTINGS_STYLE_TITLE", "Edit custom style prompt"),
                    IllustratorUiText.Resolve("AF_ILL_UI_SETTINGS_STYLE_DESCRIPTION", "Active only with the custom prompt style preset. Added as style instructions to every image-generation channel."),
                    IllustratorUiText.Resolve("AF_ILL_UI_SETTINGS_STYLE_PLACEHOLDER", "For example: classical impasto oil painting, light watercolor, dark epic realism, cinematic lighting, pencil sketch. Leave blank to add no style instructions."), initialText, delegate (string input)
                {
                    CustomStylePrompt = input ?? "";
                    if (Instance != null && !ReferenceEquals(Instance, this))
                    {
                        Instance.CustomStylePrompt = CustomStylePrompt;
                    }
                    SaveCurrentSettings();
                }, null, IllustratorUiText.Save, IllustratorUiText.Resolve("AF_ILL_UI_BACK", "Back"));
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage(IllustratorUiText.Format("AF_ILL_UI_SETTINGS_STYLE_FAILED", "[Illustrator] Could not open the style prompt editor: {ERROR}", "ERROR", ex.Message), Color.FromUint(4294901760u)));
            }
        }

        private void OpenCustomDirectorPromptEditor()
        {
            try
            {
                DevTextEditorHelper.ShowLongTextEditor(IllustratorUiText.Resolve("AF_ILL_UI_SETTINGS_DIRECTOR_TITLE", "Edit custom director prompt"),
                    IllustratorUiText.Resolve("AF_ILL_UI_SETTINGS_DIRECTOR_DESCRIPTION", "For the Visual Director only: controls composition, action, shot size, and narrative without overriding actual people, equipment, events, or the four-section output format. Inactive when the director is disabled or unconfigured."),
                    IllustratorUiText.Resolve("AF_ILL_UI_SETTINGS_DIRECTOR_PLACEHOLDER", "For example: prioritize interaction between characters over static front-facing poses; choose a moment that explains the current event. Leave blank to restore default rules."),
                    CustomDirectorPrompt ?? "", delegate (string input)
                    {
                        CustomDirectorPrompt = (input ?? "").Trim();
                        if (Instance != null && !ReferenceEquals(Instance, this))
                            Instance.CustomDirectorPrompt = CustomDirectorPrompt;
                        SaveCurrentSettings();
                    }, null, IllustratorUiText.Save, IllustratorUiText.Resolve("AF_ILL_UI_BACK", "Back"));
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage(IllustratorUiText.Format("AF_ILL_UI_SETTINGS_DIRECTOR_FAILED", "[Illustrator] Could not open the director prompt editor: {ERROR}", "ERROR", ex.Message), Color.FromUint(4294901760u)));
            }
        }

        private void OpenNegativePromptEditor()
        {
            try
            {
                string initialText = NegativePrompt ?? "";
                DevTextEditorHelper.ShowLongTextEditor(
                    IllustratorUiText.Resolve("AF_ILL_UI_SETTINGS_NEGATIVE_TITLE", "Edit negative prompt"),
                    IllustratorUiText.Resolve("AF_ILL_UI_SETTINGS_NEGATIVE_DESCRIPTION", "Added to the image prompt as exclusions, telling the model what must not appear in the image."),
                    IllustratorUiText.Resolve("AF_ILL_UI_SETTINGS_NEGATIVE_PLACEHOLDER", "For example: blur, distortion, extra fingers, modern objects, watermark text, low quality. Leave blank to add no exclusions."), initialText, delegate (string input)
                {
                    NegativePrompt = input ?? "";
                    if (Instance != null && !ReferenceEquals(Instance, this))
                    {
                        Instance.NegativePrompt = NegativePrompt;
                    }
                    SaveCurrentSettings();
                }, null, IllustratorUiText.Save, IllustratorUiText.Resolve("AF_ILL_UI_BACK", "Back"));
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage(IllustratorUiText.Format("AF_ILL_UI_SETTINGS_NEGATIVE_FAILED", "[Illustrator] Could not open the negative prompt editor: {ERROR}", "ERROR", ex.Message), Color.FromUint(4294901760u)));
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
                _cachedModelsFilePath = Path.Combine(IllustratorStoragePaths.EnsureDirectory(
                    IllustratorStoragePaths.ConfigDirectory), "illustrator_models_cache.json");
            return _cachedModelsFilePath;
        }

        [SettingPropertyButton("{=AF_ILL_TEST_API}Test image API (may incur charges)", Content = "{=AF_ILL_TEST_CANCEL}Test / Cancel", Order = 14, RequireRestart = false,
            HintText = "{=AF_ILL_TEST_API_HINT}Generates one test image with the current configuration and may incur service charges. Sends one request without channel retries. Click again to cancel while running. No save or game capture is needed; the result is saved under Cache/Illustrator/ApiTest/last-result.png.")]
        [SettingPropertyGroup("{=AF_ILL_GROUP_IMAGE_API}2. Image API (OpenAI-compatible)", GroupOrder = 2)]
        public Action TestImageApi { get; set; }

        private void TryLoadCachedModels()
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
                            _modelOptions = new List<string> { ManualModelOption };
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
                        _modelOptions = new List<string> { ManualModelOption };
                    }
                    if (!string.IsNullOrWhiteSpace(ModelName) && !_modelOptions.Contains(ModelName))
                    {
                        _modelOptions.Add(ModelName);
                    }
                    int idx = _modelOptions.IndexOf(ModelName);
                    ReplaceModelDropdown(new Dropdown<string>(_modelOptions, idx >= 0 ? idx : 0));
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
                InformationManager.DisplayMessage(new InformationMessage(IllustratorUiText.Resolve("AF_ILL_UI_SETTINGS_IMAGE_FETCH_PENDING", "[AI Image] Model list is already being fetched. Please wait."), Color.FromUint(4294967040u)));
                return;
            }

            string baseUrl = (ApiBaseUrl ?? string.Empty).Trim().TrimEnd('/');
            string apiKey = (ApiKey ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                InformationManager.DisplayMessage(new InformationMessage(IllustratorUiText.Resolve("AF_ILL_UI_SETTINGS_IMAGE_FETCH_ENDPOINT", "[AI Image] Enter the image API endpoint (Base URL) first."), Color.FromUint(4294901760u)));
                return;
            }

            _modelFetchInProgress = true;
            InformationManager.DisplayMessage(new InformationMessage(IllustratorUiText.Resolve("AF_ILL_UI_SETTINGS_IMAGE_FETCH_STARTED", "[AI Image] Fetching available models..."), Color.FromUint(4294967040u)));

            bool started = IllustratorRuntime.Start(() => FetchModelListAsync(baseUrl, apiKey), (result, error) =>
            {
                _modelFetchInProgress = false;
                if (error != null)
                {
                    InformationManager.DisplayMessage(new InformationMessage(IllustratorUiText.Format("AF_ILL_UI_SETTINGS_IMAGE_FETCH_ERROR", "[AI Image] Could not fetch models: {ERROR}", "ERROR", error.Message), Color.FromUint(4294901760u)));
                    return;
                }
                ApplyFetchedModels(result);
            });

            if (!started)
            {
                _modelFetchInProgress = false;
                InformationManager.DisplayMessage(new InformationMessage(IllustratorUiText.Resolve("AF_ILL_UI_SETTINGS_IMAGE_FETCH_BUSY", "[AI Image] Background tasks are busy. Try again later."), Color.FromUint(4294901760u)));
            }
        }

        private sealed class ModelListFetchResult
        {
            public bool Player2;
            public List<string> Models;
            public string Error;
            public int HttpStatus;
            public string ResponseBody;
            public bool NoModelData;
        }

        // Only the main-thread completion resolves language resources; workers return failure data.
        private static string ModelListErrorText(ModelListFetchResult result)
        {
            if (result == null)
                return IllustratorUiText.Resolve("AF_ILL_UI_SETTINGS_FETCH_EMPTY_RESULT", "Empty result");
            if (result.HttpStatus != 0)
                return IllustratorUiText.Format2("AF_ILL_UI_SETTINGS_FETCH_HTTP_ERROR", "Fetch failed (HTTP {STATUS}): {BODY}", "STATUS", result.HttpStatus, "BODY", result.ResponseBody);
            if (result.NoModelData)
                return IllustratorUiText.Resolve("AF_ILL_UI_SETTINGS_FETCH_NO_MODELS", "The API returned success, but no usable model data could be parsed.");
            return result.Error ?? "";
        }

        private static async Task<ModelListFetchResult> FetchModelListAsync(string baseUrl, string apiKey)
        {
            try
            {
                if (await UniversalOpenAiImageClient.DetectPlayer2Async(baseUrl, System.Threading.CancellationToken.None).ConfigureAwait(false))
                    return new ModelListFetchResult { Player2 = true };
                bool nativeGemini = GeminiNativeImageProtocol.IsNativeUrl(baseUrl);
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
                using (var request = nativeGemini ? GeminiNativeImageProtocol.CreateModelListRequest(baseUrl, apiKey) : new HttpRequestMessage(HttpMethod.Get, modelsUrl))
                {
                    if (!nativeGemini && !string.IsNullOrWhiteSpace(apiKey))
                    {
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                    }

                    using (var resp = await client.SendAsync(request).ConfigureAwait(false))
                    {
                        string json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                        if (!resp.IsSuccessStatusCode)
                        {
                            return new ModelListFetchResult { HttpStatus = (int)resp.StatusCode, ResponseBody = json };
                        }

                        JObject parsed = JObject.Parse(json);
                        JArray data = parsed["data"] as JArray;
                        if (nativeGemini)
                        {
                            data = new JArray();
                            foreach (string id in GeminiNativeImageProtocol.ReadModelIds(parsed)) data.Add(new JObject { ["id"] = id });
                        }
                        if (data == null || data.Count == 0)
                        {
                            return new ModelListFetchResult { NoModelData = true };
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
                InformationManager.DisplayMessage(new InformationMessage(IllustratorUiText.Format("AF_ILL_UI_SETTINGS_IMAGE_FETCH_ERROR", "[AI Image] Could not fetch models: {ERROR}", "ERROR", ModelListErrorText(result)), Color.FromUint(4294901760u)));
                return;
            }
            if (result.HttpStatus != 0 || result.NoModelData || !string.IsNullOrWhiteSpace(result.Error))
            {
                InformationManager.DisplayMessage(new InformationMessage(IllustratorUiText.Format("AF_ILL_UI_SETTINGS_IMAGE_FETCH_ERROR", "[AI Image] Could not fetch models: {ERROR}", "ERROR", ModelListErrorText(result)), Color.FromUint(4294901760u)));
                return;
            }

            if (result.Player2)
            {
                ReplaceModelDropdown(new Dropdown<string>(new[] { Player2ModelOption }, 0));
                RequestMcmRefresh();
                InformationManager.DisplayMessage(new InformationMessage(IllustratorUiText.Resolve("AF_ILL_UI_SETTINGS_IMAGE_PLAYER2", "[AI Image] Player2 detected. Select generation/edit models in the Player2 app; the model name on this page is not sent.")));
                return;
            }
            lock (_modelLock)
            {
                _modelOptions = new List<string> { ManualModelOption };
                _modelOptions.AddRange(result.Models);

                if (!string.IsNullOrWhiteSpace(ModelName) && !_modelOptions.Contains(ModelName))
                    _modelOptions.Add(ModelName);
                int selectedIdx = _modelOptions.IndexOf(ModelName);
                ReplaceModelDropdown(new Dropdown<string>(_modelOptions, selectedIdx >= 0 ? selectedIdx : 0));
            }

            SaveCurrentSettings(this);
            RequestMcmRefresh();
            InformationManager.DisplayMessage(new InformationMessage(IllustratorUiText.Format2("AF_ILL_UI_SETTINGS_IMAGE_FETCH_SUCCESS", "[AI Image] Fetched {COUNT} available models. Current model retained: {MODEL}. The dropdown has been refreshed.", "COUNT", result.Models.Count, "MODEL", ModelName), Color.FromUint(4278255360u)));
        }

        public static void SaveCurrentSettings() => SaveCurrentSettings(null);

        private static void SaveCurrentSettings(IllustratorSettings preferred)
        {
            try
            {
                if (BaseSettingsProvider.Instance != null)
                {
                    var target = preferred ?? Instance ?? (BaseSettingsProvider.Instance.GetSettings("AnimusForge_Illustrator_v1") as IllustratorSettings);
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

    }
}
