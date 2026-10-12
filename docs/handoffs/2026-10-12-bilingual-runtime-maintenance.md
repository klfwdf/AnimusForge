# 双语回归修复、维护规范与远端 main 融合准备

阶段：总结。实施切片 `1ffb30f620594bebfdc1955f59fe86df97e89487`，仅保存在任务本地ref；本地主分支仍 `main` / `a911d4d0de8964a393b6bdc6eaa83705b5218b02`，原119个暂存文件及index SHA不变。远端仅fetch/read main，不pull/merge/push。旧10/12 EN部署报告是此前交付，本条明确接续本轮修正版。

本轮修复真实报告的问题：EN输入收到中文新回复、附加规则协议被翻译导致识别不一致、AFEF已知标签、历史编辑/删除及翻页、终端筛选/分页、MCM下拉和终端翻译缓存。所有确认的自然语言writer在最终system按包语言收口，含三渠道、周报、公报、外交/拟稿、政策、人物、记忆、叛乱命名、RP简介、GCCZ城镇记忆与插画导演；clone/角色/原始输入、共同合同hash、协议、数字和专名保持。英文lords误命中Dakhila内部ID的共享检索缺陷已用实际DLL正负例修复。未知旧记录、玩家内容和已有生成文本未自动翻译或改存档。

AF数据按用途位于当前模组：CustomPrompts、Logs、Configs、PlayerExports、Cache和Recovery。31个不可变默认映射到ModuleData/Defaults/CustomPrompts，精确CN/EN哈希清册区分旧发布值与用户编辑；旧用户迁移与已有模块优先，保持原字节。MCM只路由AF自己的3个ID；Illustrator原独立路由保留。新增MCM helper的junction缺口已用真实两API复现后修复并阻断。原C目录保留作兼容来源，不自动删除；真实用户C迁移及权限失败未实机验。

长期仓库skill、两份维护参考及AGENTS已同步后续内容必须双语。打包必须同时CN/EN，名称AnimusForge_v1.X.X_CN_MMdd_HHmm.zip / EN，1007_0802是本机10月7日08:02，同分钟使用相同递增尾号，禁止PackageLabel改名。原版本递增/双包事务/门禁仍保持，本轮没有官方ZIP或版本递增。

验证：两API Release各0错误/363既有警告（343 CS8632、15 CS0649、3 CS0105、2 CS0162），Bootstrap0/0；双Coup门禁通过，8664个产品源码/资源输入SHA0漂移。初次PowerShell缺Get-FileHash在任务wrapper预载Utility后重跑；旧Stage因资源映射变动被原白名单拒绝，完整旧项目输出移到task superseded-stage且逐SHA保持，再通过原StageOnly入口装配，未放宽未知文件门禁。

最终DLL回放：1.3/1.4各CN、EN两次，真实原生UI英语→中文→英语；每次169语言/UI/真实实体匹配检查及170消息/历史检查PASS。另Prompt两语各228、Entities39、ContentLayout真实3503映射+8无效负控、StageWhitelist正负控、打包79、版本事务62、skill格式PASS。43个prefab/2019绑定每API最终编译闭包0失败/0未决；1.3 Core.ViewModelCollection沿原csproj来自当前BannerlordBinDir，混合来源明确记录，不能称纯1.3完整运行验收。存储专项：两版MCM各31、4个junction阻断、TTS43及8有效业务负控、存储消费者125、ModelPreset57、PlayerExports76、DataPaths63、纯IO28、编辑器原9步骤与12备份断言通过。

完整CN/EN Stage投影各3519文件PASS，实际source/DLL英语门禁通过。修正版EN已部署到 `F:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord\Modules\AnimusForge`，3519标准目标验证，2608未列文件保持、0用户改动世界书保留；按此前明确要求无新安装备份、未启动游戏。没有覆盖原版TaleWorlds DLL。

远端main新增64提交/104路径。共同基线83ba387；最终main `a9013b705e320bdafbe9ffa5e69fbb0d80d054ac`。当前双语修正版工作区的三方只读预览：7净可合、77文本冲突（含换行）、20远端单侧。融合执行清单和逐路径归属见 artifacts/bilingual-maintenance-skill-20261012/remote-main-current/integration-plan.md 与 integration-queue.json。f8口头外交融合前必须解决：R01仅双方文本非空即授玩家同意并即时兑现（P1）；R02取消移除job后仍可发旧请求（completion阻断effect，P2）；R03新中文拟文和旧规则须配对本地双语及同意合同。其余提交按owner分类，存储窄安全改进已在当前修复内采纳，未把64提交当全验收通过。远端原快照和98+8输入SHA核实，未执行远端代码或测试。

未验证：Gauntlet实际绘制/点击、当前campaign/provider真正新回复、新旧存档、真实C迁移权限、WinForms交互、全部世界书逐条语义、正式CN/EN ZIP；J17历史inverse受未审Policy Lifecycle阻断，当前TTS真实13断言和负控替代有明确边界。部署完成后需重启游戏加载新DLL及Lazy资源，旧中文正文不会自动改成英文。

回滚：源码focused inverse `artifacts/bilingual-maintenance-skill-20261012/verified-implementation-slice-inverse.patch`只读git apply --check通过；本地ref可作focused inverse commit，不能reset主线。它不恢复安装模块；本轮依要求没有安装旧文件备份。证据：同目录full-projection-result/deployment-verified/unified-build-r3、tests/bilingual-actual-runtime-api{13,14}-r3；原agent storage-implementation/compiled-proof和remote-main-incoming-a9013b7报告保留。源锚点详表见verified-code-map.json和主职责图。
