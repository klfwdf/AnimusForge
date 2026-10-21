# AF-GPTSOVITS-U 0.1.1 — 音质优先版安装与语音包教程

[English](AF-GPTSOVITS-U.en.md)

更新：2026-10-21。**本地评估候选，不是公开发行；未安装到游戏。** L 保持独立，游戏/GCCZ/存档不随此附加包修改。本文是 U 的独立教程，不套用 L 的 2GiB 目标。

0.1.1 补齐参考音频重采样依赖 `resampy`；原 0.1.0 自带的 16kHz 录音通常不会触发缺失，但 44.1/48kHz 等自定义参考可能失败。本修订不改变 V5 模型、权重或 32 步采样档。

## U 教程 1 — 版本与音质

AF-GPTSOVITS-U 0.1.1 是本地评估附加包，尚未发布公开下载。采用官方 GPT-SoVITS V5 Dev、原生 48kHz 输出和固定 32 步音质优先配置，不是给 L 包改名。更多步数不能保证每个角色都完美，也不代表所有情况下音质最强。

需要明确支持 U 的 gpt-sovits-v5 引擎的 AF 新版本；旧版即使版本号看似足够，也会拒绝这个引擎。不要改 runtime.json 绕过检查。

解压后约 7.86GiB，下载、解压与缓存建议预留至少 20GB 硬盘空间；硬盘、运行内存和显存是三回事。目前没有 U 的公开下载链接，现有角色录音仍仅限本地评估，公开分发需另核实授权。

## U 教程 2 — 安装与 L 共存

先退出游戏和启动器，解压到临时目录。将 AF-GPTSOVITS-U 和需要的 VoicePacks 内容合并到现有 Modules/AnimusForge 下；应能看到 AnimusForge/AF-GPTSOVITS-U/runtime.json，不要套两层 AnimusForge。

附加包不含 AF DLL 或 SubModule.xml，不能升级旧 AF。保留游戏文件、设置、GCCZ 与自定义音色改动；包内 server.py、ultra_engine.py、app 和 runtime 不要缺失。无需另装 Python，也不必开旧 9880 服务。

同时装有效 L 和 U 时，自动识别仍优先 L。要测试 U，请在退出游戏后把 L 移到模组目录外保留备份；不要把 L 改名成 U，也不要同时开启两套服务。点击教程只显示说明，不启动或下载模型。

## U 教程 3 — 开启与角色选声

在 TTS 设置选择 AF-GPTSOVITS-L / U（自动），开启语音并等待就绪。确认识别的是 AF-GPTSOVITS-U，再点本地语音试听；冷启动可能约一分钟，不要反复刷新。

角色第一次交谈前后都可在百科选声音。默认五男五女沿用既有 ID，有名角色保留已分配声音；花火是 Custom 下一个角色加多种语气，不是四个角色。Custom 只手选，不进默认随机池。

仍仅主对话对象出声，旁人保留文字。和平会面允许，真正战斗禁用；先校验完整回复，再按句排队朗读，不读未经校验的逐字流。关闭语音或退出 AF 只释放它自己启动的服务。新增语音包后刷新状态。

## U 教程 4 — 硬件与实测边界

U 优先音质，不沿用 L 的 2GiB 目标。本包面向 Windows/CUDA 和兼容的 NVIDIA 显卡；CPU、AMD、Intel 尚未验证。启动时可用显存不足 6GiB 会拒绝启动；这是保守余量检查，不代表推理占用 6GiB。

首轮冒烟实测：RTX 5070 Ti 16GB，未开游戏，按进程每 100ms 采样 WDDM。专用显存峰值约 2292MiB，运行内存约 4730MiB；生成 14.4 秒中文音频用 21.33 秒，冷启动就绪 63.34 秒。采样可能遗漏短峰值，速度还受文本、参考录音和其他负载影响。

游戏同开建议以 12GB/16GB 显卡、16GB 以上内存配置开始验证，不保证每套机器可用，也不建议仅据此购买硬件。8GB 显卡同开未验证；本音质优先配置可能慢于实时。多例句结果见 U 独立文档。

## U 教程 5 — 自定义语音包要求

目录使用 VoicePacks/Male/<包>、Female/<包> 或 Custom/<包>；manifest.json 就放在这一层包内，不再套下载目录。保持唯一稳定 ID、英文译名＋永久代号、性别/分类、相对 reference_file、真实 SHA-256、prompt_lang 与准确 prompt_text。参考 manifest.example.json，必须替换示例哈希。

U 强制参考音频为 3–10 秒单声道 WAV，非空且不超过 8MiB，推荐 PCM16。保留真实录音采样质量，不靠升采样伪造细节；不要音乐、混声、爆音或纯静音。prompt_text 必须逐字对应该片段，最多 1000 字符；prompt_lang 是录音本身的 zh/en/ja/yue。

同一角色的 neutral/happy/angry/sad 放在 emotion_variants，各有自己的文件、哈希、文本。缺少语气时用基础参考；任意 .pth/.ckpt 权重不能当角色包插入。使用有权使用的录音，代码开源不等于角色素材获准再分发。

## U 教程 6 — 语言、语气与排错

四种服务共用语音输出语言：空白/auto 跟随正文；输入中文、粤语、英语、日语等名称选择目标语言。同语种不翻译，不同语种由后处理只翻译朗读副本，正文和历史不变；翻译失败只显示文字。

沿用已接受的 MOOD：JOY/DELIGHTED → happy，ANNOYED → angry，其余 neutral；sad 可试听。不新增游戏动作标签。V5 也不能保证从一段平淡录音得到所有生气、可爱等表演，适合的参考语气仍很重要。

不出声时检查服务、U 兼容、就绪、主对象、战斗状态、音频哈希/路径与翻译。日志在 Logs/TTS，缓存在 Cache/TTS/U。空闲显存不足或 OOM 不会偷偷切付费 API 或 L；关闭其他 GPU 程序，或主动选择 L/远端服务。勿禁用哈希检查，勿公开令牌、密钥、私聊；更新时退出游戏，只替换目标包。

## 准确安装结构

```text
Modules/AnimusForge/
├─ SubModule.xml                    # existing AF; do not replace
├─ bin/                             # existing AF; separate compatible update
├─ AF-GPTSOVITS-U/
│  ├─ runtime.json
│  ├─ server.py
│  ├─ ultra_engine.py
│  ├─ app/                          # V5 Dev + 48kHz vocoder + frontend
│  ├─ runtime/                      # portable Windows Python/CUDA libraries
│  └─ Documentation/
└─ VoicePacks/
   ├─ Male/<pack>/manifest.json
   ├─ Female/<pack>/manifest.json
   └─ Custom/MyVoice-AF-C100/
      ├─ manifest.json
      ├─ reference.wav
      └─ tones/angry.wav
```

**兼容提示：**仅有原先 L 的 AF 安装不代表已支持本次 V5 U；必须使用通过本轮 `gpt-sovits-v5` 引擎校验的 AF 更新。附加包本身不含该主程序更新，也不能覆盖旧包来绕过。MCM 与终端使用同一原设置 owner：自动本地服务下新增“U 版教程”，原 L 的八章仍保留。

## 九组真实合成测量（未同时开游戏）

机器：RTX 5070 Ti 16GB（报告 16303MiB），驱动 617.42。Windows WDDM 按工作进程 PID 每 100ms 采样专用/共享显存；RSS 记录进程内存工作集。不是 `torch allocated`，不是全卡显存，也不是峰值硬限制。未控制其他后台应用，不能用于显卡购买承诺。

- Peak dedicated VRAM: **2303.5MiB (2.25GiB)**.
- Peak shared GPU memory: **82.0MiB**.
- Peak process RAM (RSS): **5176.8MiB (5.06GiB)**.
- Ready: **20.84s** (earlier cold smoke: **63.34s**).
- Output: **48000Hz / mono / PCM16 WAV**, batch 1; nine successes, owned process exited.

| Voice | Tone | Language | Synthesis s | Audio s | RTF |
| --- | --- | --- | ---: | ---: | ---: |
| female-sparkle | neutral | all_zh | 36.47 | 16.40 | 2.22 |
| female-sparkle | happy | all_zh | 12.81 | 14.84 | 0.86 |
| female-sparkle | angry | all_zh | 12.59 | 15.48 | 0.81 |
| female-sparkle | sad | all_zh | 12.70 | 13.00 | 0.98 |
| male-clear | angry | all_zh | 10.53 | 12.96 | 0.81 |
| female-gentle | happy | all_zh | 9.19 | 11.40 | 0.81 |
| female-sparkle | neutral | en | 10.95 | 8.28 | 1.32 |
| female-sparkle | happy | all_ja | 7.94 | 8.72 | 0.91 |
| female-sparkle | neutral | all_yue | 8.06 | 8.56 | 0.94 |

RTF = 生成耗时 / 音频时长；大于 1 表示慢于实时。本轮第一句 36.47 秒，后续 7.94–12.81 秒，不能保证每句实时。四段花火故意使用相同长句、不同语气参考，便于人工比较；生成成功不等于已经证明情绪准确、无漏字或原生外语口音。**玩家试听、长时间游戏并行、旧存档及不同显卡仍待验收。**

## 制作自己的角色包

使用 [manifest.example.json](manifest.example.json)，另存到自己的分类/包文件夹，替换 ID、名称、文字、路径和所有占位哈希；不要覆盖根清单。label 使用英文译名，style_target 使用永久代号（如 AF-C100），显示可识别、内部 ID 不随改名漂移。默认分类须与 gender 相同；自定义分类不参加随机。每个清单 ≤2MiB，最多扫描 128 清单/8MiB 元数据/512 个音色。prompt_text ≤1000 字符；输入正文按现有 AF 分句，服务单次 ≤500 字符。

```powershell
# Run in your own pack folder / 在自己的语音包文件夹执行
(Get-FileHash -LiteralPath .\reference.wav -Algorithm SHA256).Hash.ToLowerInvariant()
(Get-FileHash -LiteralPath .\tones\angry.wav -Algorithm SHA256).Hash.ToLowerInvariant()
```

每一种语气单独录制，但保持一个角色 ID。没有对应录音就删除示例变体，不要伪造文件或语气。保留同一说话人、正确切分、准确文本；不同情绪不要混入另一角色。32 步与 48kHz 不能修复低质量素材。当前 U 支持 zh/en/ja/yue；不支持韩语前端，不加载任意角色微调权重或训练数据。

## 来源与许可

- [Official GPT-SoVITS source](https://github.com/RVC-Boss/GPT-SoVITS), pinned source: `a3035089c65ca63eb7777a4b0dafd198ebe36cda`.
- [Official model repository](https://huggingface.co/lj1995/GPT-SoVITS/tree/8f8857563c040d542de108a2c18b0425abd6c9c5/gsv-v5-pretrained), pinned model revision: `8f8857563c040d542de108a2c18b0425abd6c9c5`.
- `s2Gv5dev.pth`: 766385517 bytes; SHA256 `16c11fc588a9229a479316a11fc0f518087e79773f5271666a90b39d50460407`.
- `vocoder.pth`: 57761163 bytes; SHA256 `b7543c83222aa2105bb4efb56845086bc5f2eeba61243d723ffa062052b3221f`.

模型下载已逐项核验长度和 SHA-256。代码、模型、便携依赖和角色录音各有许可边界；本地候选标记 redistribution_cleared=false。未公开上传，不代表许可最终禁止或已经获准；正式分享前另行核实。U 不使用量化、L 的 BERT 裁剪或自动切换 Turbo；不提供训练 UI、权重切换、公网服务、任意参考路径或云端兜底。保留原来手动 API 链路供高级用户使用。
