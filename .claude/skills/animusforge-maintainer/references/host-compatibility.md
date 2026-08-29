# Claude Code and Codex compatibility

`af-skill/` is one shared source directory for two hosts. `SKILL.md` is the only instruction authority; both hosts discover skills from a directory containing that file and YAML frontmatter with the same `name`.

## Shared contract

| Item | Shared rule |
| --- | --- |
| Directory name | Source directory may be `af-skill`; installed directory is `animusforge-maintainer`, matching frontmatter `name`. |
| Entrypoint | `SKILL.md` must start with valid YAML frontmatter. Keep `name` lowercase hyphen-case and quote descriptions containing punctuation. |
| Discovery metadata | `name`, `description`, and the optional short `metadata` map must remain portable YAML. Do not add host-exclusive frontmatter fields without checking both hosts. |
| Instruction semantics | `SKILL.md` and `references/` contain host-neutral Markdown instructions. Never assume a proprietary tool exists when a standard file/shell/Git check can decide the issue. |
| Manual invocation | Use `$animusforge-maintainer` where the host supports skill invocation. A host may additionally offer a slash/UI invocation, but it is not part of the shared contract. |
| Automatic selection | It is intentionally allowed only for positively identified AnimusForge work. This is a routing boundary, not permission to edit an ambiguous source copy. |
| UI metadata | `agents/openai.yaml` is optional host UI metadata. It may improve Codex UI presentation; the skill remains valid if Claude Code ignores it. |

## Installation layouts

The source remains at:

```text
/Volumes/工作区/MyBannerlordMods/AFmod/af-skill
```

Run installation from that source directory, or pass it explicitly with `--source`; do not infer a source from an AF code copy.

Install the source under the common skill identifier for each host:

```text
Claude Code: ${CLAUDE_CONFIG_DIR:-$HOME/.claude}/skills/animusforge-maintainer
Codex:       ${CODEX_HOME:-$HOME/.codex}/skills/animusforge-maintainer
```

Use the maintained helper from the source directory:

```bash
./scripts/install-af-skill.sh --host both --mode symlink --dry-run
./scripts/install-af-skill.sh --host both --mode symlink
```

`symlink` is preferred so both hosts load the same source and future source changes remain synchronized. `copy` is available only when symlinks are unsuitable; copied installations must then be refreshed deliberately.

The helper:

- validates the source before installation;
- refuses to overwrite a real directory, an unrelated link, or another skill installation;
- can target one host with `--host claude` or `--host codex`;
- never touches an AF worktree, source, Git index, build, asset, save, or package;
- requires a new turn/session after installation before discovery is tested.

Do not use the helper to replace a pre-existing installation without first inspecting that installation and explicitly deciding its disposition.

## Validation in both hosts

Run the portable source validator first:

```bash
./scripts/verify-af-skill.sh
```

When Codex tooling is available, additionally run Codex's structural validator:

```bash
python3 "${CODEX_HOME:-$HOME/.codex}/skills/.system/skill-creator/scripts/quick_validate.py" .
```

After installation and a new host turn/session, test both hosts with:

1. a verified AnimusForge task, such as a Bootstrap 1.3/1.4 packaging or courier action pipeline request;
2. a generic Bannerlord task, which must not automatically select the AF skill;
3. a Minecraft Forge task, which must not select the AF skill;
4. an explicit `$animusforge-maintainer` invocation on a known AF copy, which must still require canonical-worktree confirmation before edits.

Record actual host/version/discovery observations in the AF execution ledger. Do not claim host discovery passed merely because source validation passed.

## Host-specific boundaries

- **Claude Code:** this source is compatible with standard `SKILL.md` discovery. Whether the current Claude Code environment scans global skills, project skills, or a plugin-provided directory depends on that installation; verify after linking.
- **Codex:** Codex discovers user skills from `${CODEX_HOME:-$HOME/.codex}/skills`. Its `agents/openai.yaml` metadata and `quick_validate.py` are supported aids, not a separate instruction source.
- Do not put Claude-specific slash-command, plugin-manifest, hook, or model-routing syntax into portable frontmatter.
- Do not put Codex-specific configuration in `SKILL.md` that would alter AF maintenance semantics in Claude Code.

## Updating the shared source

1. Edit only the source `af-skill/` directory after the ledger protocol permits the documentation change.
2. Run `verify-af-skill.sh` and, when available, Codex `quick_validate.py`.
3. If installed via symlink, verify both destinations still resolve to the source.
4. If installed via copy, refresh each copied destination through an explicit, reviewed update; do not silently overwrite it.
5. Record changed paths, validators, host discovery results or `NOT-RUN` reasons, and rollback in the execution ledger.
