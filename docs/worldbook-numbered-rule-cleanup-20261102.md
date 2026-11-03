# 燃烬帝国编号知识卡整理（2026-11-02）

状态：SOURCE_ORGANIZED / OFFLINE_VERIFIED / CONTENT_DEPLOYED / LIVE_NOT_RUN。源码在 main 分支；整理切片 3634343e53。意图检查点 4c9afac227。


## 处理

- 处理燃烬帝国中英两包中 19 组、187 对带编号的知识卡文件名。旧名反复使用同一主题标题；卡片正文、关键词与短摘要实际各不相同，因此保留全部卡片，不误删、不合并。
- 中英文文件名分别改为该卡片关键词生成的主题名，例如 rule_边疆马与骆驼.json / rule_frontier_horses_and_camels.json；目标文件名不再带 _vN 或重复家族标题。
- JSON 内部 RuleId、正文、关键词、RAG 短摘要、条件和来源均保持原字节。RuleId 仍保留 _vN，因为它是中英文身份映射与导入去重使用的稳定键。
- 同步更新 content/content-map.json 的 187 条中文 source/target，以及 english-worldbook-identity-map.json 的 187 个中文路径键和英文文件名。运行时加载器按 JSON 内 RuleId 扫描兼容文件名，未改加载逻辑。

## 验证

- 专项逐卡检查：187 对、374 个文件均存在；RuleId 和中英身份映射相符；文件 SHA-256 与改名前记录一致；内容清单目标路径存在；中英文各 414 张规则文件名唯一；全部 414 对英文身份映射仍有效。
- PlayerExportsContractTests.ps1 已运行，但其硬编码期望 3,139 条，而当前基线提交在改名前已是 8,540 条；同一检查中的映射 owner/source-target 条件在当前树均为零差异。该固定数量门禁为既有失配，未改测试绕过。
- 未构建、Stage、打包、部署、push 或操作游戏安装目录；安装游戏仍需另行授权更新。未做 187 张卡的逐条史实语义审校；本次只整理文件名与映射。

## 回滚

优先使用聚焦逆提交 git revert 3634343e53；意图提交 4c9afac227 可作为整理边界。不要 reset 共享 main。 .tmp/worldbook-numbered-cleanup-20261102/rename-plan.json 保存改名前后路径与哈希；同目录 before-ledger.md、before-HANDOFF.md 保存本次记录修改前的既有脏文档状态。

## 部署补记（2026-11-02）

用户明确要求部署后，仅在 Steam 安装目录 Modules/AnimusForge/PlayerExports/燃烬帝国编年史/knowledge/rules 将 187 个已校验文件改为提交中的新文件名；每个旧/新文件 SHA-256 一一相同。部署前确认 Bannerlord 未运行、目标模块身份为 AnimusForge、目标路径无 reparse point、无文件冲突。部署后 414 条规则文件名与提交源包一致，缺失/多余/哈希差异均为 0。

本次未更改 DLL 或其他 PlayerExports 数据，未启动游戏、未发起模型请求；新战役导入可验，旧存档内已导入知识未做实机确认。现有项目 Stage 仍为旧文件名投影（414条中187个旧名），不作为本次内容来源；后续正式 Stage/打包前需刷新干净 Stage。未创建游戏目录备份。回滚按提交 3634343e53 的旧/新路径映射，将 187 个新文件名改回对应旧名。
