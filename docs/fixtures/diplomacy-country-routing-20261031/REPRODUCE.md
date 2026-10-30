# 按国家分流与说客登记提示：复跑入口

本次新增 `--country-routing` 专项使用真实共享标签分发器、规范化解析器、说客登记及直接外交 owner。游戏身份、时间、LLM 回答、原生效果叶端口为受控替身；没有启动游戏、调用付费模型或覆盖安装文件。UI 校验加载真正 TaleWorlds TextObject 和现有双语 XML。

## 生成隔离测试项目

沿用 `tests/modules/AF.Module.Diplomacy/WorldDiplomacyRoundLifecycle.SmokeTests/run_response_recovery.py`，新建目录，分别选择 `--package cn` / `--package en`，`--mode=--country-routing`；本机 `--game-bin` 为 `E:/SteamLibrary/steamapps/common/Mount & Blade II Bannerlord/bin/Win64_Shipping_Client`。完整用法沿用 `docs/fixtures/lobbyist-diplomacy-20261030/REPRODUCE.md`。

生成器会将选定包复制到测试 DLL 旁的 `ModuleData`；手动重建不能省略此步骤。其余回归前复制原 Fixtures 的 JSON，并设置 `AF_SMOKE_UI_MODULE=content/modules/AF.Module.Diplomacy` 的绝对路径、`AF_SMOKE_UI_LANGUAGE=简体中文/English`、`AF_SMOKE_PACKAGE_ROOT` 和 `AF_SMOKE_PACKAGE_LANGUAGE`，运行：

`--country-routing`、`--lobbyist`、`--bilateral-direct`、`--speech-review`、`--sovereign-control`、`--oral-declaration`、`--original-oral-vassalage`。

## 关键覆盖

- 原始失败标签 `DECLARE_WAR:khuzait:empire_s`，玩家实际统治 `empire_w`：登记一个说客事件，经原固定发文链路发布并执行一次宣战。
- 六类位置参数标签、六类 COMMIT、玩家君主/非君主/仅加入某国、三渠道、同轮混合事项；原生关闭 AI 的入口保持。
- 未授权发起国、非当前君主、不存在国家、缺贡金/割地条款、非法期限、未确认玩家同意均不因转换而执行；完整贡金方向、割地和原提案来源保留。
- 只有登记成功通知；真实 UI 语言显示君主/国家/承诺。待澄清、拒绝不报成功，通知异常不回滚登记；重复格式、读档、完成后跨日重放及字段顺序/默认零值不同均不重复同一交互的工作或通知。
- 使用同一事件/发文/效果 owner；无新增旧复核、第二执行器、存档字段或 Tick 扫描。输入至多 1,800 字符，规范化只在外交标签边界；语义去重只解析同君主、渠道、会话、交互的候选。

## 构建

使用原 `scripts/build/build_single_module.ps1` 纯构建两个实现和 Bootstrap；参数与上述原复跑文档相同，不传 Stage/Deploy，不改构建脚本。验证信息与产物哈希见 `verification.json`，定向测试结果见 `checks.txt`。实机、安装、ZIP、push 均未执行。
