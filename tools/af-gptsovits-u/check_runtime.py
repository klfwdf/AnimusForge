"""Portable dependency and actual upstream resampler probe; no models or CUDA allocation."""
from pathlib import Path
import argparse
import ast
import json
import tempfile


def check(bundle):
    import numpy as np
    import torch
    import torchaudio
    import soundfile as sf
    import fastapi
    import uvicorn
    import transformers
    import resampy
    source = bundle / 'app/GPT_SoVITS/TTS_infer_pack/TTS.py'
    tree = ast.parse(source.read_text(encoding='utf-8-sig'))
    names = {'_resampy_gpu_filter', 'resample', 'load_audio_gpu'}
    functions = [node for node in tree.body if isinstance(node, ast.FunctionDef) and node.name in names]
    if {node.name for node in functions} != names:
        raise ValueError('pinned_upstream_resampler_contract_changed')
    namespace = dict(torch=torch, torchaudio=torchaudio, np=np,
                     resample_transform_dict={}, resampy_filter_cache={})
    exec(compile(ast.Module(body=functions, type_ignores=[]), str(source), 'exec'), namespace)
    checks = []
    with tempfile.TemporaryDirectory(prefix='af-u-resample-') as temp:
        for rate in (16000, 22050, 24000, 32000, 44100, 48000):
            # Three seconds is the real service minimum, rather than a zero-length stub.
            signal = (0.1 * np.sin(2 * np.pi * 220 * np.arange(rate * 3) / rate)).astype(np.float32)
            path = Path(temp) / (str(rate) + '.wav')
            sf.write(path, signal, rate, subtype='PCM_16')
            audio, result_rate = namespace['load_audio_gpu'](
                str(path), 16000, torch.device('cpu'), mono=True,
                dtype=torch.float32, match_librosa=True)
            if result_rate != 16000 or tuple(audio.shape) != (1, 48000) or not torch.isfinite(audio).all() or audio.abs().max() < 0.01:
                raise ValueError('reference_resampling_failed:' + str(rate))
            checks.append(rate)
    return {'status': 'PASS', 'reference_rates_to_16k': checks,
            'resampy': resampy.__version__, 'torch': torch.__version__,
            'scope': 'Actual upstream CPU resampler and runtime imports; no GPU/model/game test'}


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--bundle', type=Path, required=True)
    args = parser.parse_args()
    print(json.dumps(check(args.bundle.resolve(strict=True))))
