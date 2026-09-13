using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Base.Global;
using MCM.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TaleWorlds.Library;

namespace AnimusForge.Illustrator
{
    public class IllustratorSettings : AttributeGlobalSettings<IllustratorSettings>
    {
        public override string Id => "AnimusForge_Illustrator_v1";
        public override string DisplayName => "AnimusForge - AI 画卷生图系统 (Illustrator)";
        public override string FolderName => "AnimusForge";
        public override string FormatType => "json2";

        private static List<string> _modelOptions = new List<string> { "*手动输入*" };
        private static Dropdown<string> _modelDropdown;
        private static readonly object _modelLock = new object();
        private static string _cachedModelsFilePath;

        public IllustratorSettings()
        {
            FetchModelList = () =>
            {
                Task.Run(async () =>
                {
                    await FetchModelsAsync().ConfigureAwait(false);
                });
            };
        }

        [SettingPropertyBool("启用 AI 画卷生图系统", HintText = "全局总开关。开启后将在周报、画廊等界面提供 AI 图像生成与插画展示。", Order = 0, RequireRestart = false)]
        [SettingPropertyGroup("1. 基础设置", GroupOrder = 1)]
        public bool EnableImageGeneration { get; set; } = true;

        [SettingPropertyBool("启用多模态视觉提词 (方案 A)", HintText = "开启后自动抓取游戏内 3D 角色模型与会面实景画面，喂给视觉大模型 (如 GPT-4o / Qwen-VL) 提炼超精准提示词。若配置的模型不支持视觉参数，系统会自动平滑降级为高精度文本提词。", Order = 1, RequireRestart = false)]
        [SettingPropertyGroup("1. 基础设置", GroupOrder = 1)]
        public bool EnableMultimodalVision { get; set; } = true;

        [SettingPropertyBool("启用原生 3D 模型离屏渲染 (TableauView 异步导出)", HintText = "开启后，通过骑马与砍杀2引擎底层的 TableauView 异步渲染落盘管线，直接从 GPU 渲染通道提取 100% 纯净且无任何背景杂质的 3D 角色模型立绘作为 AI 垫图。若超时未落盘会自动平滑回退，安全无崩。", Order = 2, RequireRestart = false)]
        [SettingPropertyGroup("1. 基础设置", GroupOrder = 1)]
        public bool EnableOffscreenRendering { get; set; } = true;

        [SettingPropertyText("生图 API 端点地址 (Base URL)", HintText = "兼容 OpenAI 格式的生图端点。例如官方端点 https://api.openai.com/v1、硅基流动 https://api.siliconflow.cn/v1 或各种中转站(如 https://yjapi.manqiaotechnology.com/v1)。", Order = 1, RequireRestart = false)]
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

        [SettingPropertyText("生图模型名称 (Model)", HintText = "生图模型名称。支持常规生图模型（如 FLUX.1-schnell、dall-e-3、gpt-image-1.5 等）以及对话原生多模态出图模型（如 gemini-3.1-flash-image 等，系统会自动识别并走对话图生图通道）。", Order = 5, RequireRestart = false)]
        [SettingPropertyGroup("2. 生图 API 配置 (OpenAI 兼容)", GroupOrder = 2)]
        public string ModelName { get; set; } = "black-forest-labs/FLUX.1-schnell";

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
                    if (value != null && _modelOptions != null && value.SelectedIndex >= 0 && value.SelectedIndex < _modelOptions.Count)
                    {
                        string selected = _modelOptions[value.SelectedIndex];
                        if (!string.IsNullOrWhiteSpace(selected) && selected != "*手动输入*")
                        {
                            ModelName = selected;
                            if (Instance != null && !ReferenceEquals(Instance, this))
                            {
                                Instance.ModelName = selected;
                            }
                        }
                    }
                    _modelDropdown = value;
                }
            }
        }

        [SettingPropertyText("生图分辨率尺寸 (Size)", HintText = "生成的图片分辨率，如 1024x1024、1280x720 (宽屏横幅)、768x1024 等。需服务商模型支持。若为多模态模型(如 Gemini Image)，系统会自动换算为 1:1、16:9、3:4 等画幅比例注入。", Order = 7, RequireRestart = false)]
        [SettingPropertyGroup("2. 生图 API 配置 (OpenAI 兼容)", GroupOrder = 2)]
        public string ImageSize { get; set; } = "1024x1024";

        private static readonly List<string> _qualityOptions = new List<string> { "默认 (不传)", "standard (标准)", "hd (高清细节)" };
        private Dropdown<string> _qualityDropdown;

        [SettingPropertyDropdown("生成画质预设 (Quality)", Order = 8, RequireRestart = false, HintText = "仅对支持 quality 参数的服务商/模型生效（如 DALL-E-3、OpenAI 标准中转）。选“默认”时不发送该参数，避免不支持的模型报错。若使用 Gemini 则自动通过提示词与高分指令强化生效。")]
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
                if (_qualityDropdown == null || _qualityDropdown.SelectedIndex <= 0) return "";
                if (_qualityDropdown.SelectedIndex == 1) return "standard";
                if (_qualityDropdown.SelectedIndex == 2) return "hd";
                return "";
            }
        }

        private static readonly List<string> _styleOptions = new List<string> { "默认 (不传)", "vivid (鲜艳生动)", "natural (自然真实)" };
        private Dropdown<string> _styleDropdown;

        [SettingPropertyDropdown("生成风格画风 (Style)", Order = 9, RequireRestart = false, HintText = "仅对支持 style 参数的服务商/模型生效（如 DALL-E-3）。选“默认”时不发送该参数。选 vivid 为高对比度鲜艳戏剧感；选 natural 为柔和细腻的古典写实油画质感。若使用 Gemini 则自动通过提示词强化生效。")]
        [SettingPropertyGroup("2. 生图 API 配置 (OpenAI 兼容)", GroupOrder = 2)]
        public Dropdown<string> StyleDropdown
        {
            get
            {
                if (_styleDropdown == null)
                    _styleDropdown = new Dropdown<string>(_styleOptions, 0);
                return _styleDropdown;
            }
            set => _styleDropdown = value;
        }

        public string SelectedStyle
        {
            get
            {
                if (_styleDropdown == null || _styleDropdown.SelectedIndex <= 0) return "";
                if (_styleDropdown.SelectedIndex == 1) return "vivid";
                if (_styleDropdown.SelectedIndex == 2) return "natural";
                return "";
            }
        }

        [SettingPropertyBool("周报自动生成纪事插画", HintText = "开启后，每周生成国家周报时，系统将自动分析头条事件并生成一张专属的古典史诗纪事插画。", Order = 1, RequireRestart = false)]
        [SettingPropertyGroup("3. 周报与展示场景", GroupOrder = 3)]
        public bool AutoGenerateWeeklyReportIllustration { get; set; } = true;

        [SettingPropertyBool("英雄百科页注入【纪事插画】按钮", HintText = "开启后，在英雄百科页面将注入【纪事插画】按钮，可点击针对该英雄的 3D 模型与身份生平生成史诗级肖像立绘。", Order = 2, RequireRestart = false)]
        [SettingPropertyGroup("3. 周报与展示场景", GroupOrder = 3)]
        public bool EnableEncyclopediaIllustration { get; set; } = true;

        [SettingPropertyBool("现场对话界面注入【场景插画】按钮", HintText = "开启后，在地图对话与场景面对面对话时注入【场景插画】按钮，可点击根据现场双方站姿与对话语境生成生动的会晤史诗插画。", Order = 3, RequireRestart = false)]
        [SettingPropertyGroup("3. 周报与展示场景", GroupOrder = 3)]
        public bool EnableConversationIllustration { get; set; } = true;

        [SettingPropertyInteger("本地缓存最大保留张数", 20, 1000, "0 张", HintText = "生成的图片在本地持久化缓存的最大数量，避免重复调用消耗额度。", Order = 1, RequireRestart = false)]
        [SettingPropertyGroup("4. 存储与性能", GroupOrder = 4)]
        public int MaxCacheCount { get; set; } = 200;

        [SettingPropertyBool("修正 UI 显示色彩通道 (修复游戏内红蓝反色/蓝皮)", HintText = "Bannerlord 原生 Gauntlet UI 着色器在渲染内存贴图时默认红蓝通道反置。开启此项自动校正为真实肉色与服饰色彩。默认开启。", Order = 2, RequireRestart = false)]
        [SettingPropertyGroup("4. 存储与性能", GroupOrder = 4)]
        public bool FixColorChannels { get; set; } = true;

        // 提示词扩写：底层永久自动开启，并全自动复用 AnimusForge 正文对话 API 配置
        public bool EnableLlmPromptExpansion { get; set; } = true;
        public string DirectorApiBaseUrl { get; set; } = "";
        public string DirectorApiKey { get; set; } = "";
        public string DirectorModelName { get; set; } = "";

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

        public async Task FetchModelsAsync()
        {
            try
            {
                string baseUrl = (ApiBaseUrl ?? string.Empty).Trim().TrimEnd('/');
                string apiKey = (ApiKey ?? string.Empty).Trim();

                if (string.IsNullOrWhiteSpace(baseUrl))
                {
                    InformationManager.DisplayMessage(new InformationMessage("[AI生图] 请先填写生图 API 端点地址 (Base URL)！", Color.FromUint(4294901760u)));
                    return;
                }

                InformationManager.DisplayMessage(new InformationMessage("[AI生图] 正在拉取可用模型列表...", Color.FromUint(4294967040u)));

                string modelsUrl = baseUrl;
                if (modelsUrl.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase))
                    modelsUrl = modelsUrl.Substring(0, modelsUrl.Length - "/chat/completions".Length).TrimEnd('/');
                if (modelsUrl.EndsWith("/images/generations", StringComparison.OrdinalIgnoreCase))
                    modelsUrl = modelsUrl.Substring(0, modelsUrl.Length - "/images/generations".Length).TrimEnd('/');

                if (!modelsUrl.EndsWith("/models", StringComparison.OrdinalIgnoreCase))
                {
                    if (modelsUrl.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
                        modelsUrl = modelsUrl + "/models";
                    else
                        modelsUrl = modelsUrl + "/v1/models";
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
                            InformationManager.DisplayMessage(new InformationMessage($"[AI生图] 拉取失败 (HTTP {(int)resp.StatusCode}): {json}", Color.FromUint(4294901760u)));
                            return;
                        }

                        JObject parsed = JObject.Parse(json);
                        JArray data = parsed["data"] as JArray;
                        if (data == null || data.Count == 0)
                        {
                            InformationManager.DisplayMessage(new InformationMessage("[AI生图] 接口返回成功，但未解析到可用模型数据。", Color.FromUint(4294936576u)));
                            return;
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

                        // 优先排序：图像与画卷相关模型排在最前
                        list.Sort((a, b) =>
                        {
                            bool aIsImg = a.IndexOf("image", StringComparison.OrdinalIgnoreCase) >= 0 || a.IndexOf("flux", StringComparison.OrdinalIgnoreCase) >= 0 || a.IndexOf("dall", StringComparison.OrdinalIgnoreCase) >= 0;
                            bool bIsImg = b.IndexOf("image", StringComparison.OrdinalIgnoreCase) >= 0 || b.IndexOf("flux", StringComparison.OrdinalIgnoreCase) >= 0 || b.IndexOf("dall", StringComparison.OrdinalIgnoreCase) >= 0;
                            if (aIsImg && !bIsImg) return -1;
                            if (!aIsImg && bIsImg) return 1;
                            return string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
                        });

                        lock (_modelLock)
                        {
                            _modelOptions = new List<string> { "*手动输入*" };
                            _modelOptions.AddRange(list);

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

                            SaveCachedModels(list);
                        }

                        // 请求 MCM 运行时热重构刷新（主线程 Tick 将即刻重建 ModOptions 视图）
                        RequestMcmRefresh();

                        InformationManager.DisplayMessage(new InformationMessage($"[AI生图] 成功获取 {list.Count} 个可用模型！已优先选中: {ModelName}，下拉选单已即时刷新。", Color.FromUint(4278255360u)));
                    }
                }
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage($"[AI生图] 拉取模型异常: {ex.Message}", Color.FromUint(4294901760u)));
            }
        }
    }
}
