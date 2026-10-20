# AF-GPTSOVITS-L 0.1.0 安装与语音包教程

适用范围：Windows 版骑马与砍杀 2 的 AnimusForge，使用内嵌 L 轻量语音附加包。此包用于本地推理，不需要玩家训练模型。教程更新：2026-10-20。

[English](AF-GPTSOVITS-L.en.md)

## 1. 下载与版本

下载文件：AF-GPTSOVITS-L_0.1.0.zip

百度网盘：[AF-GPTSOVITS-L_0.1.0.zip](https://pan.baidu.com/s/1Wsl1hXm5Mz2_7DRetX94FA?pwd=5qkj)

提取码：5qkj

这是 AF 专用轻量语音附加包，不是 AF 主模组，也不是原版 GPT-SoVITS 训练整合包。需要已支持 AF-GPTSOVITS 的 AF 版本。

本地同版本测试包约 5GB 压缩、7.3GiB 解压；为下载、解压和缓存建议预留 15GB。分享文件大小以实际下载为准。U 全量版将另有下载和教程，不能套用 L 的显存结论。

## 2. 安装位置

1. 退出游戏和启动器，先解压到临时文件夹。

2. 把 AF-GPTSOVITS-L 与 VoicePacks 放进现有 Modules/AnimusForge。若压缩包已有 AnimusForge 外层，只合并它的内容，不要多套一层。

3. 正确路径是 Modules/AnimusForge/AF-GPTSOVITS-L/runtime.json，以及 Modules/AnimusForge/VoicePacks。保留 server.py、lite_engine.py、app 与 runtime 全部文件。

4. 附加包不应覆盖 AF 的 bin、SubModule.xml、ModuleData 或其他玩法文件。不要覆盖已有自定义语音/同名清单，不要删除整个 AF。

5. 不用另外安装 Python，也不用手动开旧 9880 服务；运行环境由 AF 管理。

```text
Mount & Blade II Bannerlord/
└─ Modules/
   └─ AnimusForge/
      ├─ SubModule.xml             # existing AF / 保留原有文件
      ├─ bin/                      # existing AF / 保留原有文件
      ├─ AF-GPTSOVITS-L/
      │  ├─ runtime.json
      │  ├─ server.py
      │  ├─ lite_engine.py
      │  ├─ app/
      │  └─ runtime/
      └─ VoicePacks/
         ├─ Male/
         ├─ Female/
         └─ Custom/
            └─ MyVoice-AF-C100/
               ├─ manifest.json
               ├─ reference.wav
               └─ tones/angry.wav
```

## 3. 开启与角色选声

在 MCM 的 TTS API/语音设置里选择 AF-GPTSOVITS-L / U（自动），开启语音；自动识别兼容包，L 和 U 同在时优先 L。等待准备完成再点“测试本地语音”，冷启动时可能要等几十秒至一两分钟，不要反复点刷新。

角色百科可在第一次对话前选择声音。有名角色随机抽到后固定；Male/Female 是同性别默认池；Custom 不参加随机，只能手动选择。

L/U 本地语音只读主对话对象，旁人仍显示文字；私人对话读当前对象。和平会面可用，真正战斗禁用本地合成。读取已校验回复后分句朗读，不是逐字抢读。

添加语音后刷新本地运行包状态或重启游戏。关闭语音会释放 AF 自己启动的服务。

## 4. 显存与性能建议

建议从 8GB 显存的兼容 NVIDIA 显卡起测试，12GB/16GB 在游戏旁运行更有余量；不是保证，也不建议仅为此购买显卡。6GB 及以下不作为游戏同开推荐配置，优先手动选择远端 TTS。当前便携包是 Windows/CUDA 方案，AMD、Intel 或 CPU-only 没有同等即插即用验证。

L 的优化目标约 2GiB 显存，不是硬上限。RTX 5070 Ti 16GB 上 17 次真实合成、100ms 采样的进程专用显存峰值约 1546MiB（1.51GiB）；这不是 8GB 实卡加游戏压测，也可能漏掉瞬时峰值。

8GB 用户要给语音预留约 2–3GB 余量，降低游戏纹理/画质并关掉其他 GPU 程序。只测 MCM 试音不等于长时间稳定。观察总专用显存、卡顿和长句，紧张时关闭本地语音或自己切远端。显存、系统内存和硬盘空间是三回事。

### 配置判断

| 总显存 | 游戏同开建议 |
| --- | --- |
| 6GB 及以下 | 不作为推荐配置；优先选已配置的远端语音 |
| 8GB | 可作为测试起点，给本地语音留约 2–3GB 余量，适当降低游戏画质 |
| 12GB / 16GB 及以上 | 更宽裕，仍要检查游戏、其他 MOD 和后台应用的总占用 |

以上是基于 L 本地测试的保守使用建议，不是显卡兼容认证或最低配置承诺。显存峰值来自 2026-10-17 的 Windows 进程专用显存采样，包含冷启动；不能把 PyTorch allocated 数字当成整张显卡占用。内存不足、驱动不兼容、硬盘慢也会影响使用。

## 5. 自定义录音要求

参考音频语音包不是训练模型：准备录音、准确文字和 manifest.json 即可；不要把任意 .pth/.ckpt 当成即插即用角色包。

推荐同一说话人、清晰无混响的单声道 PCM16 WAV，单段约 3–10 秒；16kHz 可作参考输入，不必伪造更高采样率。不能有背景音乐、其他人声、剪切爆音或整段静音。每段文件必须非空且不超过 8MiB。

prompt_text 必须逐字对应这段录音（1–1000 字符）；prompt_lang 是参考录音语言，当前 L 推荐 zh/en/ja/yue，与想输出的语言不是一回事。

各语气必须是同一角色的不同录音。编辑 WAV 后重新算 SHA-256 并同步清单。只能使用有权使用的声音，公开分享须有录音/角色素材再分发授权；开源代码许可不等于录音授权。

## 6. 目录与清单字段

目录示例：VoicePacks/Custom/MyVoice-AF-C100/manifest.json；同层放 reference.wav，语气放 tones/angry.wav 等。只扫描分类下面一层包目录，不要再套下载目录。Male/Female/Custom 对应 category=male/female/custom；默认池的 category 必须与 gender 一致。

profiles 内每个声音需要稳定唯一 id（1–128 字符、无首尾空格）、label、speaker_metadata.gender（male/female）、category、reference_file、reference_sha256（64位真实 SHA-256）、prompt_lang、prompt_text。建议英文 label + style_target 专属代号，如 My Voice · AF-C100。路径相对本包，不能越界或用盘符绝对路径。

可选 emotion_variants 仅 neutral/happy/angry/sad，每项各有自己的录音路径、哈希、语言和准确文字。每个清单不超过 2MiB；总扫描最多 128 个清单、8MiB 元数据、512 个声音。JSON 示例见 GitHub 的 docs/tts/manifest.example.json；示例哈希须替换，不能原样使用。

### 可编辑清单示例

[manifest.example.json](manifest.example.json)

```json
{
  "schema_version": 1,
  "profiles": [
    {
      "id": "custom-myvoice-AF-C100",
      "label": "My Voice",
      "style_target": "AF-C100",
      "category": "custom",
      "speaker_metadata": {
        "gender": "female"
      },
      "reference_file": "reference.wav",
      "reference_sha256": "REPLACE_WITH_ACTUAL_64_HEX_SHA256",
      "prompt_lang": "zh",
      "prompt_text": "替换为录音中的准确文本。",
      "emotion_variants": {
        "angry": {
          "reference_file": "tones/angry.wav",
          "reference_sha256": "REPLACE_WITH_ACTUAL_64_HEX_SHA256",
          "prompt_lang": "zh",
          "prompt_text": "替换为生气录音中的准确文本。"
        }
      }
    }
  ]
}
```

先把模板另存为自己包内的 `manifest.json`，改成自己的 ID、名称、文字、音频路径和真实哈希。没有生气录音就删除示例中的 `angry` 项，不要引用不存在的文件。不要覆盖 `VoicePacks` 根清单。

```powershell
# Run in your own voice-pack folder / 在自己的语音包目录执行
(Get-FileHash -LiteralPath .\reference.wav -Algorithm SHA256).Hash.ToLowerInvariant()
(Get-FileHash -LiteralPath .\tones\angry.wav -Algorithm SHA256).Hash.ToLowerInvariant()
```

保存后刷新语音包，在百科选择这个自定义声音，然后分别试听平静、生气等语气。跨语言和长句也要试；哈希或格式校验通过不代表音质已经合格。建议共享统一模型和小型参考音频，而不是给每个声音重复塞整套环境。

## 7. 语言与语气

“语音输出语言”支持填写中文、粤语、英语、日语或对应英文名；留空/auto 跟随正文。四种服务共用此设置：正文同语言就不翻译，不同才由后处理私下翻译，字幕和历史不变。用中文写“日语”不会被拿去朗读；它会映射成语言代码。后处理失败/译文缺失时仅出文字，不乱读其他语言。

语气来自现有后处理的 MOOD，不把其他动作标签交给 GPT-SoVITS：JOY/DELIGHTED 对应 happy，ANNOYED 对应 angry，其余 neutral；sad 可手动试听。

一个角色一个稳定 ID，把多语气放在 emotion_variants 内，不用把花火拆成四个人。缺少该语气会用基础参考；有生气录音更易表达生气，但不保证表演效果。当前自动标签不支持自造 cute/flirty 等新类别。

## 8. 排错与更新

没声音先看：语音开关、选择的服务、运行包准备状态、主对话对象、百科音色、战斗限制，以及指定语言是否拿到有效后处理译文。参考路径/哈希错误、重复 ID、模型缺文件会拒绝使用，不会自动切到付费 API。

添加包后刷新状态或重启；文件缺失请重新完整解压，不要边运行边替换。语言提示旧包不支持时更新配套 L 包，不要只改 runtime.json 绕过校验。

报错日志在 Logs/TTS，临时缓存在 Cache/TTS。求助可给错误行、AF/包版本、显卡和复现步骤，先遮掉 API Key、令牌和私聊。

更新仅合并对应附加包，保留自定义声音、角色 ID、玩家配置和其他 MOD。网盘下载的可执行环境只用可信来源；本次链接是分享者提供的资源，不代表所有录音都已获公开分发许可。U 发布时必须另写硬件与兼容教程。

## 版本与资料说明

本教程按 AF 当前实际清单、MCM 和 L 运行包编写。网盘链接/提取码由分享者提供；网页访问未能完成独立下载核验，不为该远端 ZIP 声明本地测试包的 SHA-256。其文件名为发布名，不代表所有 0.1.0 ZIP 字节完全相同。若提示语言能力缺失，请更新对应运行包。

GPT-SoVITS 上游功能与本精简包不是同一范围：[上游项目](https://github.com/RVC-Boss/GPT-SoVITS)。本 L 包只宣称已接通的 zh/en/ja/yue；不要由上游支持推断本包已具备训练、其他平台或韩语运行环境。

后续 AF-GPTSOVITS-U 必须单独提供下载、安装与显存实测教程；本页不把 U 当成已经发布。
