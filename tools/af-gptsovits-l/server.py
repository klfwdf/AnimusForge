"""Managed loopback-only AF L server. No training or weight-switch API."""
import argparse
import ctypes
import io
import json
import logging
from logging.handlers import RotatingFileHandler
import os
from pathlib import Path
import threading
import time


def normalize_output_language(value):
    # AF sends canonical engine codes. Default remains legacy auto; never guess an unknown choice.
    aliases = {"auto": "auto", "zh": "all_zh", "all_zh": "all_zh",
               "yue": "all_yue", "all_yue": "all_yue", "ja": "all_ja", "all_ja": "all_ja", "en": "en"}
    if not isinstance(value, str) or value not in aliases:
        raise ValueError("text_language_invalid")
    return aliases[value]


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--port", type=int, required=True)
    parser.add_argument("--module-root", required=True)
    parser.add_argument("--parent-pid", type=int, required=True)
    args = parser.parse_args()
    module = Path(args.module_root).resolve()
    root = Path(__file__).resolve().parent
    instance = os.environ.get("AF_TTS_INSTANCE", "")
    if len(instance) != 32 or not instance.isalnum() or not 1024 <= args.port <= 65535:
        raise RuntimeError("invalid_managed_launch")
    cache = module / "Cache/TTS"
    logs = module / "Logs/TTS"
    for folder in (cache, logs):
        folder.mkdir(parents=True, exist_ok=True)
    handler = RotatingFileHandler(logs / "af-gptsovits-l.log", maxBytes=2*1024*1024, backupCount=2, encoding="utf-8")
    logger = logging.getLogger("af_l")
    logger.setLevel(logging.INFO)
    logger.addHandler(handler)
    os.environ["TEMP"] = os.environ["TMP"] = str(cache)
    import lite_engine
    lite_engine.prepare(root / "app", cache)
    from fastapi import FastAPI, Request, HTTPException
    from fastapi.responses import Response
    from starlette.concurrency import run_in_threadpool
    import uvicorn
    import soundfile as sf
    app = FastAPI(docs_url=None, redoc_url=None, openapi_url=None)
    state = {"state": "loading", "engine": None, "error": None}
    gate = threading.Lock()

    def authenticate(request):
        if request.headers.get("X-AF-Instance") != instance:
            raise HTTPException(403, "instance_mismatch")

    @app.get("/af/health")
    async def health(request: Request):
        authenticate(request)
        return {"instance": instance, "package_id": "AF-GPTSOVITS-L", "af_runtime_api": 1,
                "engine": "gpt-sovits-v2pro", "state": state["state"],
                "capabilities": {"audio": "pcm16_mono_wav", "batch_size": 1, "weight_switch": False, "text_language_selection": True, "languages": ["zh", "en", "ja", "yue"]}, "busy": gate.locked()}

    @app.post("/tts")
    async def tts(request: Request):
        authenticate(request)
        if state["state"] != "ready":
            raise HTTPException(503, "runtime_not_ready")
        raw = await request.body()
        if len(raw) > 32768:
            raise HTTPException(413, "request_too_large")
        try:
            data = json.loads(raw)
            text = data["text"]
            reference = Path(data["ref_audio_path"]).resolve()
            voice_root = (module / "VoicePacks").resolve()
            reference.relative_to(voice_root)
            if isinstance(text, str) and any("\uac00" <= ch <= "\ud7af" for ch in text):
                raise HTTPException(400, "korean_frontend_unavailable")
            if not isinstance(text, str) or not text.strip() or len(text) > 500:
                raise ValueError("text_invalid")
            if not reference.is_file() or reference.stat().st_size > 8*1024*1024 or reference.suffix.lower() != ".wav":
                raise ValueError("reference_invalid")
            if data.get("prompt_lang") not in ("zh", "en", "ja", "yue"):
                raise ValueError("reference_language_invalid")
            if not isinstance(data.get("prompt_text"), str) or len(data["prompt_text"]) > 1000:
                raise ValueError("reference_text_invalid")
            speed = float(data.get("speed_factor", 1))
            if not 0.5 <= speed <= 2:
                raise ValueError("speed_invalid")
            # The server accepts only the bounded AF synthesis shape.
            data = {"text": text, "text_lang": normalize_output_language(data.get("text_lang", "auto")), "ref_audio_path": str(reference),
                    "prompt_lang": data["prompt_lang"], "prompt_text": data["prompt_text"],
                    "speed_factor": speed, "text_split_method": "cut5", "seed": -1}
        except (KeyError, TypeError, ValueError, OSError):
            raise HTTPException(400, "invalid_synthesis_request")
        if not gate.acquire(blocking=False):
            raise HTTPException(429, "runtime_busy")
        started = time.monotonic()
        try:
            rate, audio = await run_in_threadpool(state["engine"].synthesize, data)
            output = io.BytesIO()
            sf.write(output, audio, rate, format="WAV", subtype="PCM_16")
            if output.tell() > 32*1024*1024:
                raise HTTPException(413, "audio_too_large")
            logger.info("synthesis_ok chars=%d seconds=%.3f", len(text), time.monotonic()-started)
            return Response(output.getvalue(), media_type="audio/wav")
        except HTTPException:
            raise
        except Exception as error:
            logger.exception("synthesis_failed type=%s", type(error).__name__)
            raise HTTPException(500, "synthesis_failed")
        finally:
            gate.release()

    def initialize():
        try:
            state["engine"] = lite_engine.load(root / "app", cache)
            state["state"] = "ready"
            logger.info("runtime_ready af_runtime_api=1")
        except Exception as error:
            state["state"] = "failed"
            state["error"] = type(error).__name__
            logger.exception("initialization_failed")

    # An open handle identifies the original parent even if its PID is later reused.
    kernel = ctypes.WinDLL("kernel32", use_last_error=True)
    kernel.OpenProcess.restype = ctypes.c_void_p
    parent = kernel.OpenProcess(0x1000, False, args.parent_pid)
    if not parent:
        raise RuntimeError("parent_unavailable")
    kernel.GetExitCodeProcess.argtypes = [ctypes.c_void_p, ctypes.POINTER(ctypes.c_ulong)]
    kernel.CloseHandle.argtypes = [ctypes.c_void_p]
    def watch_parent():
        try:
            while True:
                code = ctypes.c_ulong()
                if not kernel.GetExitCodeProcess(parent, ctypes.byref(code)) or code.value != 259:
                    os._exit(0)
                time.sleep(1)
        finally:
            kernel.CloseHandle(parent)
    threading.Thread(target=watch_parent, daemon=True).start()
    threading.Thread(target=initialize, daemon=True).start()
    uvicorn.run(app, host="127.0.0.1", port=args.port, access_log=False, log_level="warning", http="h11", ws="none", loop="asyncio", lifespan="off")


if __name__ == "__main__":
    main()
