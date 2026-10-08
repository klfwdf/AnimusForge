# AF-ENGLISH 本轮验证

状态：`INVENTORY_VERIFIED / TRANSLATION_NOT_STARTED / LIVE_NOT_RUN`。

## 已执行

- 建分支时的 GitHub main：`a7d8421425cf96a52208c5019ecdbeb311ad8e48`。官方快照 32,124 个 blob 与 GitHub tree 一致；整树 `04689e8a3823ea67f89dae557dc48390e26679a1`、浅克隆提交 SHA 完全匹配。
- 提取工具 .NET 8 构建：0 警告、0 错误；没有新增 NuGet 依赖。6 项 unittest 通过，覆盖 Unicode/重复 JSON 值、Json.NET 字面换行、XML 实体、C# 字符/插值/注释/两 API 分支，以及精确的世界书路径。
- 对同一源基线完整提取两次，7 个被跟踪清单/压缩流的 SHA-256 逐字节相同。
- 3,144 个选中文件和 1,877 个延期文件的源 SHA-256 在提取后保持；译文状态全部未开始。
- 196,820 个 occurrence ID 不重复；45,826 个含汉字记录与独立解码筛选一致；26,469 个去重原文保留全部引用；原始汉字行 38,410 条。
- 所有 occurrence 均属于源清单，不含延期世界书路径；4 个 gzip 流完整解压与普通原件 SHA 相同。
- MSBuild 生产 Compile 输入不包含新增离线提取器，产品代码/资源、官方构建入口未修改。
- 定向废代码/冲突搜索只命中测试临时目录和说明次级 legacy 加载资源的有效注释；无冲突、弃用的新运行时桥或平行实现。最终 `git diff --check` 通过。

## 产物哈希

- `excluded-worldbook.json`：`b8e89befb99a6d238639ad146808b46e4c2ee9cfab628d96e3a93222d1080803`
- `han-review.jsonl.gz`：`574db3961db658e9f5ab7394cc7b9af07f1149ba422b6ed83f5a35751057296e`
- `han-source-lines.tsv.gz`：`378e047a2a07ca9df5536f7e2da87b69df3a7299c14ba679b28cd0d94ec313b6`
- `manifest.json`：`a1cea4edd3826a4169cde26ba28d7e0fc1211ace00fff93d0c53723ea95f3aa3`
- `occurrences.jsonl.gz`：`792da9c3d6a66ff898b17d10847ae26de5bd6dde76add91e83ae90d96fbb543d`
- `summary.json`：`c2cd9a408a71ce77f8f1913019e15973c8c9d34beb6513fe0dfa604ac59c9ac9`
- `unique-han-texts.jsonl.gz`：`35a9a1bcf418ebb6fd30ae06861e16cca314bb53092b1eef8544315e7d203424`

## 复跑

```powershell
python tools/LocalizationInventory/extract_text.py --dotnet G:\AFMOD\.dotnet-sdk\dotnet.exe
$env:AF_INVENTORY_DOTNET='G:\AFMOD\.dotnet-sdk\dotnet.exe'
python -m unittest discover -s tools/LocalizationInventory -p test_extract_text.py -v
git diff --check
```

本轮新增离线工具，没有被替换的产品路径或需要删除的旧实现。bin/obj/缓存与普通大清单不跟踪；完整压缩清单保留所有文本，并不是删减抽样。

## 未验证

- 尚无英化译文；未构建英化产品 DLL、未部署/发布 MOD 包。
- 未进行游戏内布局/菜单/存档、真实 LLM/provider、三渠道英文输出验收。
- 静态字符串清单不等于所有文本运行可达，也不等于每条含汉字文本都应翻译。
- 未 OCR 图片，未提取原生二进制或玩家个人运行资料；XML 提取是词法盘点，JSON 读取允许现有 Json.NET 风格控制符，不是严格资源语法认证。
