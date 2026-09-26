# Skill 主源、仓库采用与宿主发现

## 主源与采用关系

主源由本次用户明确指定，不内置某台机器的绝对路径。Skill 标识保持 `animusforge-maintainer`；`metadata.version` 是维护规则版本，不是 AF 产品/API/存档版本。

本项目通过根 AGENTS 明确读取仓库内维护副本；单独修改外部主源不会自动更新该副本。同步时逐文件比较版本与内容，保留仓库定制协调说明和其他作者差异；有冲突先合并，不整目录覆盖。框架 Skill 仅补内部/外部边界，不复制整套维护规范。

`agents/openai.yaml` 是展示、调用提示和发现策略元数据，不是任务调度配置。保留当前 `allow_implicit_invocation: true`；宿主真实加载结果必须独立观察，文件校验通过不证明会话已经重新发现。

## 校验

在选定主源或仓库副本中运行：

```bash
bash scripts/verify-af-skill.sh .
python3 scripts/test-af-skill.py --skill .
```

需要 Bash、Python 3 和 PyYAML；缺少必要解析器明确报 NOT-RUN 并以非零退出，不安装依赖。可用时另运行宿主的 `quick_validate.py`。验证当前 Markdown/YAML、模板、路由和失败反例；`references/history/*.txt` 是原文档快照，不按当前指令或当前相对链接解释。

## 可选安装（另需明确授权）

保留现有 helper 接口与拒绝覆盖行为，从所选源运行；先 dry-run：

```bash
bash scripts/install-af-skill.sh --host codex --mode copy --source . --dry-run
```

helper 支持 `claude|codex|both`、`symlink|copy`，默认 source 为脚本父目录；目的位置由 `CLAUDE_CONFIG_DIR` / `CODEX_HOME` 或各自 HOME 默认确定。dry-run 仍会拒绝已有非同源链接目的地，不清理旧安装。实际安装不是 Skill 编辑的隐含步骤；不修改宿主全局配置或覆盖其他副本。

安装后按实际宿主环境检查发现与作用域：AF 请求、非 AF Bannerlord、Minecraft 和显式调用分别验证。未做宿主发现测试记 NOT-RUN，不由静态校验推断兼容成功。
