"""AF V2Pro inference profile. Official source remains separate and unchanged."""
from pathlib import Path
from collections import OrderedDict
import hashlib
import os
import sys


def prepare(app, cache):
    app, cache = Path(app).resolve(), Path(cache).resolve()
    cache.mkdir(parents=True, exist_ok=True)
    os.environ.setdefault("PYTHONDONTWRITEBYTECODE", "1")
    os.environ["HF_HUB_OFFLINE"] = "1"
    os.environ["HF_HUB_DISABLE_TELEMETRY"] = "1"
    os.environ["HF_HOME"] = str(cache / "huggingface")
    os.environ["NLTK_DATA"] = str(app / "offline-data" / "nltk" if (app / "offline-data" / "nltk").exists() else app.parent / "cache" / "nltk")
    for key, leaf in (("MPLCONFIGDIR", "matplotlib"), ("NUMBA_CACHE_DIR", "numba"),
                      ("CUDA_CACHE_PATH", "cuda"), ("TORCHINDUCTOR_CACHE_DIR", "inductor"),
                      ("TORCH_HOME", "torch"), ("XDG_CACHE_HOME", "xdg")):
        folder = cache / leaf
        folder.mkdir(parents=True, exist_ok=True)
        os.environ[key] = str(folder)
    os.environ["OMP_NUM_THREADS"] = "4"
    os.environ["MKL_NUM_THREADS"] = "4"
    os.environ["language"] = "en_US"
    os.chdir(app)
    sys.path[:0] = [str(app), str(app / "GPT_SoVITS")]


def load(app, cache, optimized=True):
    prepare(app, cache)
    import torch
    torch.set_num_threads(4)
    import torch.nn.functional as F
    from transformers import AutoModelForMaskedLM, AutoTokenizer
    from TTS_infer_pack.TTS import TTS, TTS_Config, Text2SemanticLightningModule, CNHubert, SV
    if not torch.cuda.is_available():
        raise RuntimeError("cuda_unavailable")

    class TargetBert(torch.nn.Module):
        def __init__(self, masked):
            super().__init__()
            self.encoder = masked.bert
            # hidden_states includes embeddings: [-3] is the output of layer N-2.
            layers = getattr(masked.config, "af_target_hidden_layer", len(self.encoder.encoder.layer) - 2)
            self.encoder.encoder.layer = torch.nn.ModuleList(list(self.encoder.encoder.layer)[:layers])
            self.encoder.config.num_hidden_layers = layers

        def forward(self, **inputs):
            inputs.pop("output_hidden_states", None)
            last = self.encoder(**inputs, output_hidden_states=False, return_dict=True).last_hidden_state
            # Keep the upstream TextPreprocessor contract without retaining all hidden states.
            return {"hidden_states": (last, last, last)}

    class ReferenceEncoder(torch.nn.Module):
        def __init__(self, inner):
            super().__init__()
            self.inner = inner.eval().half().cpu()

        def forward(self, audio):
            self.inner.to(audio.device)
            try:
                return {"last_hidden_state": self.inner(audio)["last_hidden_state"]}
            finally:
                self.inner.cpu()
                torch.cuda.empty_cache()

    class ReferenceSpeaker:
        def __init__(self):
            self.inner = SV(torch.device("cpu"), True)
            self.key = None
            self.embeddings = OrderedDict()

        def compute_embedding3(self, audio):
            if self.key in self.embeddings:
                value = self.embeddings.pop(self.key)
                self.embeddings[self.key] = value
                return value.to(audio.device)
            self.inner.embedding_model.to(audio.device)
            try:
                value = self.inner.compute_embedding3(audio)
                self.embeddings[self.key] = value.detach().cpu()
                while len(self.embeddings) > 44:
                    self.embeddings.popitem(last=False)
                return value
            finally:
                self.inner.embedding_model.cpu()
                torch.cuda.empty_cache()

    class LiteTTS(TTS):
        def init_sv_model(self):
            if self.sv_model is None:
                self.sv_model = ReferenceSpeaker()

        def init_cnhuhbert_weights(self, base_path):
            model = CNHubert(base_path).eval()
            model.model = ReferenceEncoder(model.model)
            self.cnhuhbert_model = model

        def init_bert_weights(self, base_path):
            self.bert_tokenizer = AutoTokenizer.from_pretrained(base_path, local_files_only=True)
            masked = AutoModelForMaskedLM.from_pretrained(base_path, local_files_only=True, torch_dtype=torch.float16)
            self.bert_model = TargetBert(masked).eval().to(self.configs.device)

        def init_t2s_weights(self, weights_path):
            self.configs.t2s_weights_path = weights_path
            self.configs.save_configs()
            self.configs.hz = 50
            checkpoint = torch.load(weights_path, map_location="cpu", weights_only=False)
            config = checkpoint["config"]
            self.configs.max_sec = config["data"]["max_sec"]
            model = Text2SemanticLightningModule(config, "unused", is_train=False)
            model.load_state_dict(checkpoint["weight"])
            # Convert before GPU placement; do not retain a second CUDA checkpoint copy.
            self.t2s_model = model.eval().half().to(self.configs.device)
            codebook = self.t2s_model.model.ar_audio_embedding.weight.detach()
            mute = codebook[self.configs.mute_tokens[self.configs.version]].unsqueeze(0)
            self.configs.mute_emb_sim_matrix = F.cosine_similarity(mute.float(), codebook.float(), dim=-1)

    base = Path(app) / "GPT_SoVITS" / "pretrained_models"
    config = TTS_Config({"custom": {
        "device": "cuda", "is_half": True, "version": "v2Pro",
        "t2s_weights_path": str(base / "s1v3.ckpt"),
        "vits_weights_path": str(base / "v2Pro" / "s2Gv2Pro.pth"),
        "bert_base_path": str(base / "chinese-roberta-wwm-ext-large"),
        "cnhuhbert_base_path": str(base / "chinese-hubert-base"),
    }})
    config.configs_path = str(Path(cache) / "tts-infer.yaml")
    pipeline = (LiteTTS if optimized else TTS)(config)
    if str(pipeline.configs.device) != "cuda":
        raise RuntimeError("cuda_required_for_this_profile")
    torch.cuda.empty_cache()
    return Engine(pipeline, optimized)


def transfer(value, device):
    import torch
    if torch.is_tensor(value):
        return value.detach().to(device).clone()
    if isinstance(value, dict):
        return {key: transfer(item, device) for key, item in value.items()}
    if isinstance(value, list):
        return [transfer(item, device) for item in value]
    if isinstance(value, tuple):
        return tuple(transfer(item, device) for item in value)
    return value


def tensor_bytes(value):
    import torch
    if torch.is_tensor(value):
        return value.numel() * value.element_size()
    if isinstance(value, dict):
        return sum(tensor_bytes(v) for v in value.values())
    if isinstance(value, (list, tuple)):
        return sum(tensor_bytes(v) for v in value)
    return 0


class Engine:
    def __init__(self, pipeline, optimized):
        self.pipeline, self.optimized = pipeline, optimized
        self.cache = OrderedDict()
        self.cache_bytes = 0
        self.max_cache_bytes = 64 * 1024 * 1024
        self.active_key = None

    def synthesize(self, request):
        import numpy as np
        import torch
        path = Path(request["ref_audio_path"]).resolve()
        key = (hashlib.sha256(path.read_bytes()).hexdigest(), request["prompt_lang"], request["prompt_text"])
        if self.optimized:
            self.pipeline.sv_model.key = key
        if self.optimized and key in self.cache and key != self.active_key:
            packet = self.cache.pop(key)
            self.cache[key] = packet
            self.pipeline.prompt_cache = transfer(packet, self.pipeline.configs.device)
            # Preserve the live relative/absolute spelling used by the upstream cache check.
            self.pipeline.prompt_cache["ref_audio_path"] = str(path)
        values = dict(request)
        values.update(ref_audio_path=str(path), media_type="wav", streaming_mode=False,
                      batch_size=1, parallel_infer=True, use_cuda_graph=False, super_sampling=False)
        with torch.inference_mode():
            rate, audio = next(self.pipeline.run(values))
        if self.optimized:
            packet = transfer(self.pipeline.prompt_cache, "cpu")
            size = tensor_bytes(packet)
            if key in self.cache:
                self.cache_bytes -= tensor_bytes(self.cache.pop(key))
            if size <= self.max_cache_bytes:
                self.cache[key] = packet
                self.cache_bytes += size
                while self.cache_bytes > self.max_cache_bytes or len(self.cache) > 44:
                    _, removed = self.cache.popitem(last=False)
                    self.cache_bytes -= tensor_bytes(removed)
        self.active_key = key
        return int(rate), np.asarray(audio, dtype=np.int16)


def verify_bert(app, cache):
    prepare(app, cache)
    import torch
    from transformers import AutoTokenizer, AutoModelForMaskedLM
    path = Path(app) / "GPT_SoVITS/pretrained_models/chinese-roberta-wwm-ext-large"
    model = AutoModelForMaskedLM.from_pretrained(path, local_files_only=True).eval()
    inputs = AutoTokenizer.from_pretrained(path, local_files_only=True)("你答应过要听我说完，怎么又装作什么都不知道呢？", return_tensors="pt")
    with torch.inference_mode():
        expected = model(**inputs, output_hidden_states=True).hidden_states[-3].clone()
        encoder = model.bert
        encoder.encoder.layer = torch.nn.ModuleList(list(encoder.encoder.layer)[:-2])
        encoder.config.num_hidden_layers -= 2
        actual = encoder(**inputs, output_hidden_states=False, return_dict=True).last_hidden_state
    torch.testing.assert_close(actual, expected, rtol=0, atol=0)
    return {"target_layer": encoder.config.num_hidden_layers, "max_abs_error": float((actual-expected).abs().max()), "device": "cpu", "dtype": str(actual.dtype)}
