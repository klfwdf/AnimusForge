"""Offline production host/worker integration; model/game inputs are read-only."""
import argparse
import collections
import hashlib
import json
import math
import os
import pathlib
import shutil
import statistics
import struct
import subprocess

REPO = pathlib.Path(__file__).resolve().parents[4]


def sha(path):
    h = hashlib.sha256()
    with path.open('rb') as f:
        for block in iter(lambda: f.read(4 * 1024 * 1024), b''):
            h.update(block)
    return h.hexdigest()


def link_readonly_fixture(source, target):
    target.parent.mkdir(parents=True, exist_ok=True)
    try:
        os.link(source, target)
    except OSError:
        shutil.copy2(source, target)


def write_string(value):
    value = value.encode('utf8')
    n = len(value)
    prefix = bytearray()
    while n >= 128:
        prefix.append((n & 127) | 128)
        n >>= 7
    return bytes(prefix) + bytes([n]) + value


def main():
    p = argparse.ArgumentParser()
    p.add_argument('--module-root', required=True)
    p.add_argument('--pack', required=True, help='Directory containing worker exe')
    p.add_argument('--host', required=True, help='Built RerankerDeviceTests.exe with CPU native dependencies')
    p.add_argument('--output', required=True)
    args = p.parse_args()
    game, pack, host, output = [pathlib.Path(x).resolve() for x in (args.module_root, args.pack, args.host, args.output)]
    if not output.is_relative_to(REPO / 'artifacts') or output.exists():
        raise ValueError('Use a fresh repository artifacts output directory.')
    output.mkdir(parents=True)
    protected = list((game / 'ONNX/reranker').glob('*')) + list((game / 'bin/Win64_Shipping_Client').glob('*onnx*.dll'))
    protected = [path for path in protected if path.is_file()]
    before = {str(path): sha(path) for path in protected}
    for name in ['complete', 'missing']:
        for source in (game / 'ONNX/reranker').iterdir():
            if source.is_file():
                link_readonly_fixture(source, output / name / 'ONNX/reranker' / source.name)
    for source in pack.rglob('*'):
        if source.is_file():
            link_readonly_fixture(source, output / 'complete/OptionalRuntimes/RerankerCuda' / source.relative_to(pack))
    flags = {'creationflags': subprocess.CREATE_NO_WINDOW} if os.name == 'nt' else {}
    faults = subprocess.run([str(host), 'faults'], capture_output=True, text=True, timeout=60, **flags)
    (output / 'faults.log').write_text(faults.stdout + faults.stderr, encoding='utf8')
    faults.check_returncode()
    results = {}
    for mode in ['cpu', 'invalid', 'missing', 'gpu', 'kill']:
        root = output / ('missing' if mode == 'missing' else 'complete')
        result = subprocess.run([str(host), mode, str(root), str(output / (mode + '.json'))], capture_output=True, text=True, timeout=180, **flags)
        (output / (mode + '.log')).write_text(result.stdout + result.stderr, encoding='utf8')
        result.check_returncode()
        results[mode] = json.loads((output / (mode + '.json')).read_text(encoding='utf8'))
        print(result.stdout.strip(), flush=True)
    comparisons = {}
    for mode in ['invalid', 'missing', 'gpu', 'kill']:
        comparisons[mode] = {}
        for key in ['scores', 'single', 'longScores']:
            cpu, actual = results['cpu'][key], results[mode][key]
            error = max(abs(a-b) for a,b in zip(cpu, actual))
            order = lambda scores: sorted(range(len(scores)), key=lambda i: -scores[i])
            comparisons[mode][key] = {'maxAbsError': error, 'sameOrder': order(cpu) == order(actual)}
            assert len(cpu) == len(actual) and all(math.isfinite(v) for v in actual) and error <= 1e-5
    # Graceful EOF allows ORT to flush a real kernel profile. Three actual production-tokenized requests.
    encoded = json.loads((output / 'gpu.json.encoded.json').read_text(encoding='utf8'))
    root = output / 'complete'
    wire = struct.pack('<i', 1) + write_string(str(root))
    request = struct.pack('<i', len(encoded['rows']))
    for row, mask in zip(encoded['rows'], encoded['masks']):
        request += struct.pack('<i', len(row))
        request += b''.join(struct.pack('<qi', token, attention) for token, attention in zip(row, mask))
    env = os.environ.copy()
    env['AF_RERANKER_WORKER_PROFILE'] = str(output / 'worker-profile')
    worker = subprocess.run([str(pack / 'AnimusForge.RerankerCuda.exe'), str(os.getpid())],
                            input=wire + request * 3, capture_output=True, timeout=120, env=env, cwd=output, **flags)
    (output / 'profile-stderr.log').write_bytes(worker.stderr)
    worker.check_returncode()
    assert worker.stdout[:4] == struct.pack('<i', 1) and b'CUDA_FP32' in worker.stdout[:20]
    profiles = list(output.glob('worker-profile*.json')) + list(output.glob('onnxruntime_profile*.json'))
    assert len(profiles) == 1
    events = json.loads(profiles[0].read_text(encoding='utf8'))
    providers = collections.Counter(e.get('args', {}).get('provider') for e in events if e.get('args', {}).get('provider'))
    assert providers['CUDAExecutionProvider'] > 0, 'No actual CUDA kernels executed'
    after = {str(path): sha(path) for path in protected}
    assert before == after, 'Game input changed during offline run'
    summary = {'comparisons': comparisons, 'providers': providers, 'protectedInputsUnchanged': True,
               'protectedHashes': after,
               'warmUncachedMedianMs': {k: statistics.median(v['warmMs']) for k,v in results.items()},
               'notRun': ['game load and frame-time', 'MCM UI', 'RTX 5060', 'real-world-book corpus']}
    (output / 'summary.json').write_text(json.dumps(summary, ensure_ascii=False, indent=2), encoding='utf8')
    print(json.dumps({k: v for k,v in summary.items() if k != 'protectedHashes'}, ensure_ascii=False, indent=2))


if __name__ == '__main__':
    main()
