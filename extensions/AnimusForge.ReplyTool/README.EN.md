# AnimusForge.ReplyTool 0.1.1

An independent Bannerlord submodule. Harmony wraps AF's two HTTP sender delegates in memory, moves the current user text into an ID-paired, completed `read_user_input` call/result, forces `output_reply(content)`, and restores that reply tool's arguments as ordinary AF text. AF retains its endpoint, authentication, sender, cancellation, timeout, admission, action and memory owners. No AF source, DLL, settings or saves are changed. No proxy or additional model requests are used.

## Use

Run `python extensions/AnimusForge.ReplyTool/build.py --game-root "your Bannerlord directory"`. It builds, tests and stages only this submodule; it never builds or deploys AF. `artifacts/latest-stage.txt` identifies the new stage.

Copy the `AnimusForge.ReplyTool` folder from either CN or EN to the game's `Modules/`. Enable it after `Bannerlord.Harmony` and `AnimusForge`. CN/EN selects the fixed tool prompt resource, not the reply language; match the AF content package where possible. Disable this module in the launcher, or move its folder away, to roll back.

Startup diagnostics and one `request_prepared;input_wrapped=True/False;stream=True/False` line per transformed request are in this module's `Logs/ReplyTool.log`; no request bodies, credentials or endpoints are logged. There are no MCM/terminal settings; the launcher controls activation. Restart the game process after a DLL update; an already loaded DLL is not hot-reloaded.

## Scope and behavior

- Applies to model names containing `gemini` at endpoints ending in `/chat/completions`. Aliases without that substring, native Gemini/Anthropic, Responses, TTS, image and embedding endpoints pass through.
- Applies equally to eligible requests using AF's shared streaming/non-streaming transports, including native dialogue, couriers and scene shouts. Existing stages, prompts, JSON, tags and consumers remain in place.
- Existing `tools/functions/tool_choice/function_call/response_format` and multiple-candidate requests pass through. The internal tool carries text; no model tool action is executed.
- Input wrapping changes only the latest user turn in the outgoing copy. Earlier history, system messages and saves remain unchanged. The result is `{"source":"user_input","text":"original text"}` with matching call/result IDs. A complete current-turn pair is not nested again, while an older pair does not exempt a new turn. For multimodal input, only text parts move; images stay in the user message. Image-only and unknown content types do not produce fabricated text. A malformed current input pair is an explicit error.
- Complete arguments are validated for non-streaming replies. Streaming decodes fragmented JSON escapes and surrogate pairs with independent per-response state. Ordinary text is a fallback. Tool text wins if both forms occur together; a tool arriving after ordinary text was already published causes an error to prevent duplication.
- Incomplete JSON, multiple/unknown tools, output length limits and filtering are errors. AF retains its existing policy for already displayed partial text. HTTP failures retain their status and body. No automatic retries or continuations. Providers without tool support may reject requests; disable the module to restore normal requests.
- One request transformation and one transformation per SSE event; argument decoding is O(n) overall. No Tick IO, polling, background game-object access or per-fragment reflection.

## Validation

`artifacts/verification.json` records both reference API lines, identical shared DLLs, actual installed AF assembly patch/transport tests, source hashes and unchanged AF DLL hashes. Deterministic local senders are used; no real provider is called.

Offline tests do not establish live-game or provider acceptance, model completion rates, or actual channel actions. This mechanism does not raise the model's output limit or guarantee every reply completes. The build command never deploys or pushes; consult the corresponding receipts for installation and live-provider validation status.

## References

The user's analysis supplied the text-in-tool protocol concept. This is an independent C# implementation, without copying source from the referenced projects: [RP-Hub](https://github.com/STA1N156/RP-Hub/tree/cd8f2a6d599230e8788a1888a478912c6e83d7fe), [gcli2api](https://github.com/STA1N156/gcli2api/tree/af32928885c96d6d68656a3f6d3df4ee173b920f), [AIStudio2API](https://github.com/STA1N156/AIStudio2API/tree/69398d8bd8219a0c0ef9208021c49f25e42a76d6).
