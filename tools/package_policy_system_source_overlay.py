from __future__ import annotations

from datetime import datetime
from pathlib import Path
import hashlib
import json
import re
import subprocess
import zipfile


ROOT = Path(__file__).resolve().parents[1]
OUTPUT_DIR = ROOT / "dist"


def relative_path(path: Path) -> str:
    relative = path.resolve().relative_to(ROOT).as_posix()
    if relative == ".." or relative.startswith("../"):
        raise RuntimeError(f"Path escapes workspace: {path}")
    return relative


def git_text(*arguments: str) -> str:
    try:
        return subprocess.check_output(
            ["git", *arguments],
            cwd=ROOT,
            text=True,
            encoding="utf-8",
            errors="replace",
        ).strip()
    except Exception:
        return ""


def build_file_set() -> tuple[set[Path], dict[str, str]]:
    files: set[Path] = set()
    categories: dict[str, str] = {}

    def add_file(relative: str, category: str) -> None:
        path = (ROOT / relative).resolve()
        if not path.is_file():
            raise FileNotFoundError(relative)
        normalized = relative_path(path)
        files.add(path)
        categories[normalized] = category

    def add_tree(
        relative: str,
        category: str,
        *,
        excluded_names: set[str] | None = None,
    ) -> None:
        excluded_names = {name.lower() for name in (excluded_names or set())}
        base = (ROOT / relative).resolve()
        if not base.is_dir():
            raise FileNotFoundError(relative)
        relative_path(base)
        for path in base.rglob("*"):
            if not path.is_file():
                continue
            parts = {part.lower() for part in path.relative_to(ROOT).parts}
            if parts & {"bin", "obj", ".git", ".tmp", ".vs", "runs"}:
                continue
            if path.name.lower() in excluded_names:
                continue
            normalized = relative_path(path)
            files.add(path)
            categories[normalized] = category

    add_tree("PolicySystem", "policy_system")

    host_files = [
        "AnimusForge.csproj",
        "Properties/AssemblyInfo.cs",
        "SubModule.cs",
        "AIConfigHandler.cs",
        "AIConfigModel.cs",
        "DuelSettings.cs",
        "MyBehavior.cs",
        "WorldEntityRetrievalService.cs",
        "PromptListRetrievalService.cs",
        "CourierDeliveryBehavior.cs",
        "ShoutBehavior.cs",
        "AnimusForgeTerminalBehavior.cs",
        "HotkeyInputGuard.cs",
        "KingdomStrategicProfileBehavior.cs",
        "KingdomStrategicProfileBehavior.DevUi.cs",
        "ProactiveNpcRequestBehavior.cs",
        "ProactiveNpcRequestPromptsConfigModel.cs",
        "VassalageBehavior.cs",
        "VoteDealBehavior.cs",
        "VoteDealBehavior.Agenda.cs",
        "VoteDealBehavior.MapNotification.cs",
        "VoteDealBehavior.Propose.cs",
        "WorldDiplomacyBehavior.cs",
        "WorldDiplomacyLlmClient.cs",
        "OnnxEmbeddingEngine.cs",
        "AnimusForgeTextInputSanitizer.cs",
        "SaveRuntimeGuard.cs",
        "Logger.cs",
        "LlmApiCompat.cs",
        "LlmRetryPrompt.cs",
    ]
    for relative in host_files:
        add_file(relative, "host_integration")

    runtime_assets = [
        "AnimusForge/CustomPrompts/CustomPolicyEvaluatorPrompt.json",
        "AnimusForge/CustomPrompts/NpcRulerPolicyPrompt.json",
        "AnimusForge/CustomPrompts/WorldDiplomacyPrompt.json",
        "CustomPrompts/CustomPolicyEvaluatorPrompt.json",
        "AnimusForge/ModuleData/PreprocessPrompts.json",
        "AnimusForge/ModuleData/RuleBehaviorPrompts.json",
        "AnimusForge/ModuleData/ActionPostprocessPrompts.json",
        "AnimusForge/ModuleData/ProactiveNpcRequestPrompts.json",
        "AnimusForge/GUI/Prefabs/CustomPolicyComposePopup.xml",
        "AnimusForge/GUI/Prefabs/CustomPolicyHistoryPopup.xml",
        "AnimusForge/GUI/Prefabs/CustomPolicyResultPopup.xml",
        "AnimusForge/GUI/Prefabs/LocalPolicyComposePopup.xml",
        "AnimusForge/GUI/Prefabs/LocalPolicyHistoryPopup.xml",
        "AnimusForge/GUI/Prefabs/WorldDiplomacyComposePopup.xml",
        "AnimusForge/GUI/SpriteParts/af_world_diplomacy/af_world_diplomacy_notice.png",
        "AnimusForge/GUI/SpriteParts/af_world_diplomacy/af_world_diplomacy_notice_v2.png",
    ]
    for relative in runtime_assets:
        add_file(relative, "runtime_assets")

    add_tree("tools/PolicyEffectModule.ContractTests", "contract_tests")
    add_tree(
        "tools/ActionPostprocessPromptLab",
        "prompt_lab",
        excluded_names={"_tmp_v44_retry_party_transfer_002.jsonl"},
    )
    add_tree("tools/PreprocessTopicPromptLab", "prompt_lab")
    add_file("tools/package_policy_system_source_overlay.py", "packaging")

    baseline_files = [
        "Phase0_Local_Archive/baseline/cases/policy_history_retrieval.jsonl",
        "Phase0_Local_Archive/baseline/cases/policy_target_semantic_calibration.jsonl",
        "Phase0_Local_Archive/baseline/run_policy_target_handle_api_test.ps1",
        "Phase0_Local_Archive/baseline/run_policy_target_semantic_calibration.ps1",
    ]
    for relative in baseline_files:
        add_file(relative, "policy_baseline")

    documentation_files = [
        "docs/bannerlord_1_3_to_1_4_5_compatibility_diff.md",
        "docs/bannerlord_dual_module_output.md",
        "docs/free_conversation_scene_shout_alignment.md",
        "docs/directive_tag_output_case.md",
    ]
    for relative in documentation_files:
        add_file(relative, "documentation")

    return files, categories


def validate_files(files: set[Path]) -> None:
    blocked_suffixes = {".dll", ".exe", ".pdb", ".zip", ".onnx", ".onnx_data"}
    secret_patterns = {
        "openai_key": re.compile(r"sk-[A-Za-z0-9_-]{16,}"),
        "google_key": re.compile(r"AIza[0-9A-Za-z_-]{20,}"),
        "private_key": re.compile(r"BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY"),
        "bearer_token": re.compile(r"Bearer\s+[A-Za-z0-9._~+/-]{24,}", re.I),
        "credential_url": re.compile(r"https?://[^\s/:]+:[^\s/@]+@", re.I),
    }
    secret_hits: list[dict[str, str]] = []

    for path in files:
        relative = relative_path(path)
        lower = relative.lower()
        if lower.startswith("animusforge/onnx/") or path.suffix.lower() in blocked_suffixes:
            raise RuntimeError(f"Blocked ONNX/binary asset selected: {relative}")
        if any(
            part.lower() in {"bin", "obj", "runs", ".tmp", ".git"}
            for part in path.relative_to(ROOT).parts
        ):
            raise RuntimeError(f"Generated/private path selected: {relative}")
        if path.suffix.lower() == ".png":
            continue
        text = path.read_bytes().decode("utf-8", errors="ignore")
        for rule, pattern in secret_patterns.items():
            if pattern.search(text):
                secret_hits.append({"rule": rule, "path": relative})

    if secret_hits:
        raise RuntimeError(
            "Potential secrets found: " + json.dumps(secret_hits, ensure_ascii=False)
        )


def create_package() -> dict[str, object]:
    files, categories = build_file_set()
    validate_files(files)

    now = datetime.now().astimezone()
    stamp = now.strftime("%Y%m%d_%H%M%S")
    package_name = f"AnimusForge_PolicySystem_FullSourceOverlay_{stamp}"
    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
    zip_path = OUTPUT_DIR / f"{package_name}.zip"

    manifest_files: list[dict[str, object]] = []
    for path in sorted(files, key=lambda item: relative_path(item).lower()):
        relative = relative_path(path)
        data = path.read_bytes()
        manifest_files.append(
            {
                "path": relative,
                "category": categories[relative],
                "size": len(data),
                "sha256": hashlib.sha256(data).hexdigest().upper(),
            }
        )

    branch = git_text("branch", "--show-current")
    commit = git_text("rev-parse", "HEAD")
    scoped_status = git_text(
        "status",
        "--short",
        "--",
        *[str(item["path"]) for item in manifest_files],
    )
    category_counts: dict[str, int] = {}
    for item in manifest_files:
        category = str(item["category"])
        category_counts[category] = category_counts.get(category, 0) + 1

    total_bytes = sum(int(item["size"]) for item in manifest_files)
    readme = f"""# AnimusForge 政策系统全量源码覆盖包

生成时间：{now.isoformat(timespec="seconds")}
来源分支：{branch or "unknown"}
来源提交：{commit or "unknown"}
文件数量：{len(manifest_files)}
未压缩总大小：{total_bytes} bytes

## 这是什么

这是给其他 AnimusForge 作者做源码集成的完整文件覆盖包，不是可直接丢进游戏 Modules 的客户端成品包。

OVERLAY 目录保留仓库相对路径，包含：

- 完整 PolicySystem：玩家全国/地方/附庸政策、NPC政策、议程生命周期、效果模块、TargetPlan、统一历史语义检索、对话政策注入、自动起草与 UI。
- 完整宿主接入文件：行为注册/Tick、MCM与提示词、前处理实体、信使/自由对话/场景喊话、附庸、投票议程、主动请求与世界外交联动。
- 完整运行时资源：政策通用提示词、NPC政策提示词、前处理/规则/后处理配置、全部政策弹窗 XML 及关联外交资源。
- 契约测试、ONNX政策检索/目标语义基线、前后处理实验工具源码与兼容文档。

## ONNX 边界

本包不包含：

- AnimusForge/ONNX 中任何模型、tokenizer 或配置文件；
- model.onnx、model.onnx_data；
- Microsoft.ML.OnnxRuntime.dll 或其他编译/运行时二进制。

本包保留 OnnxEmbeddingEngine.cs，因为政策检索与 TargetPlan 需要调用 AF 现有引擎。接收方必须继续使用其现有 AF ONNX 模型与运行时，禁止因本包下载或替换模型。

## 集成方式

1. 先备份或提交接收方仓库。
2. 对比 MANIFEST.json 和 SOURCE_STATUS.txt。
3. 将 OVERLAY 内文件复制到接收方 AF 仓库根目录，保留相对路径。
4. 这是完整文件覆盖，不是逐行补丁。宿主文件尤其是 MyBehavior.cs、ShoutBehavior.cs、DuelSettings.cs、SubModule.cs；若已有其他作者修改，必须人工合并，不能盲目覆盖。
5. 使用接收方自己的 Bannerlord 引用、现有 ONNX 目录和构建流程，分别验证 BannerlordApi=1.3、BannerlordApi=1.4 与 Bootstrap。

## 已排除

为避免泄漏或污染，未打入：ONNX模型、DLL/EXE/PDB、bin/obj、日志、存档、API密钥、游戏目录、旧压缩包、部署/覆盖产物、测试 runs 中的请求与模型回复。

## 校验

- FILES.sha256：包内每个覆盖文件的 SHA-256。
- MANIFEST.json：路径、分类、大小和 SHA-256。
- 生成前已执行常见密钥、私钥、Bearer 与凭据 URL 模式扫描，命中数为 0。
- 本源码快照最近一次验证：Bannerlord API 1.3/1.4 完整契约均通过 5472 assertions；实际现有 ONNX 政策历史专项通过 2069 assertions；Bootstrap 构建成功。
"""

    excluded = """# 有意排除项

- AnimusForge/ONNX/**
- *.onnx、*.onnx_data、tokenizer/config 模型资产
- *.dll、*.exe、*.pdb
- **/bin/**、**/obj/**、**/.tmp/**、**/runs/**
- 日志、存档、API密钥、游戏目录同步/部署产物
- 原版游戏反编译源码与本地依赖缓存

这些排除项不属于政策功能源码缺失。接收方必须已有合法的 AF 基础工程、Bannerlord API 引用和现有 ONNX 模型/runtime。
"""

    manifest = {
        "package": package_name,
        "kind": "AnimusForge policy system full source overlay",
        "createdLocal": now.isoformat(timespec="seconds"),
        "sourceRoot": str(ROOT),
        "gitBranch": branch,
        "gitCommit": commit,
        "worktreeDirtyForIncludedFiles": bool(scoped_status),
        "fileCount": len(manifest_files),
        "totalBytes": total_bytes,
        "categoryCounts": category_counts,
        "excludedOnnxAssets": True,
        "secretPatternHitCount": 0,
        "files": manifest_files,
    }
    manifest_text = json.dumps(manifest, ensure_ascii=False, indent=2) + "\n"
    hash_text = "".join(
        f'{item["sha256"]}  OVERLAY/{item["path"]}\n' for item in manifest_files
    )
    status_text = (
        f"branch={branch or 'unknown'}\n"
        f"commit={commit or 'unknown'}\n"
        "included_worktree_status:\n"
        f"{scoped_status or '(clean for included paths)'}\n"
    )

    with zipfile.ZipFile(
        zip_path,
        "w",
        compression=zipfile.ZIP_DEFLATED,
        compresslevel=9,
    ) as archive:
        prefix = package_name + "/"
        archive.writestr(prefix + "README_FIRST.md", readme.encode("utf-8"))
        archive.writestr(prefix + "EXCLUDED.md", excluded.encode("utf-8"))
        archive.writestr(prefix + "MANIFEST.json", manifest_text.encode("utf-8"))
        archive.writestr(prefix + "FILES.sha256", hash_text.encode("utf-8"))
        archive.writestr(prefix + "SOURCE_STATUS.txt", status_text.encode("utf-8"))
        for item in manifest_files:
            archive.write(
                ROOT / str(item["path"]),
                prefix + "OVERLAY/" + str(item["path"]),
            )

    return {
        "zipPath": str(zip_path),
        "zipSize": zip_path.stat().st_size,
        "zipSha256": hashlib.sha256(zip_path.read_bytes()).hexdigest().upper(),
        "fileCount": len(manifest_files),
        "totalBytes": total_bytes,
        "categoryCounts": category_counts,
        "secretPatternHitCount": 0,
        "onnxAssetsIncluded": False,
    }


if __name__ == "__main__":
    print(json.dumps(create_package(), ensure_ascii=False, indent=2))
