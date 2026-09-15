"""Strict inverse of the reviewed fixed-buffer writer optimization; no source edits."""
from pathlib import Path
import hashlib
import json
import subprocess

ROOT = Path(__file__).resolve().parents[2]
HERE = Path(__file__).resolve().parent
WRITER_PATH = 'Refactor/Runtime/MemorySourceFingerprintWriter.cs'


def restore_writer(path, source):
    path = str(path).replace(chr(92), '/')
    if path != WRITER_PATH:
        return source
    review = json.loads((HERE/'writer-review.json').read_text(encoding='utf-8'))
    old = subprocess.check_output(['git', 'show', review['baseline']+':'+path], cwd=ROOT).decode('utf-8-sig').replace('\r\n', '\n')
    assert hashlib.sha256(old.encode()).hexdigest() == review['beforeSha256'], 'Unreviewed writer baseline'
    lines = old.splitlines(keepends=True)
    for change in reversed(review['paths'][path]):
        start, end = change['start'], change['end']
        assert ''.join(lines[start:end]) == change['before'], 'Writer before hunk changed'
        lines[start:end] = [change['after']]
    expected = ''.join(lines)
    assert hashlib.sha256(expected.encode()).hexdigest() == review['afterSha256'], 'Writer after hunk changed'
    assert (ROOT/path).read_text(encoding='utf-8-sig') == expected, 'Unreviewed live writer changes'
    assert source in (expected, old), 'Unreviewed input writer changes'
    return old


def main():
    from unittest.mock import patch
    path = ROOT/WRITER_PATH
    current = path.read_text(encoding='utf-8-sig')
    old = restore_writer(WRITER_PATH, current)
    assert restore_writer(WRITER_PATH.replace('/', chr(92)), current) == old
    assert restore_writer(WRITER_PATH, old) == old
    assert restore_writer('unrelated.cs', 'unchanged') == 'unchanged'
    rejected = False
    try:
        restore_writer(WRITER_PATH, current+'\n// unreviewed input\n')
    except AssertionError:
        rejected = True
    assert rejected, 'Changed input must not be normalized away'
    read_text = Path.read_text
    def changed_live(instance, *args, **kwargs):
        value = read_text(instance, *args, **kwargs)
        return value+'\n// unreviewed live\n' if instance == path else value
    rejected = False
    with patch.object(Path, 'read_text', changed_live):
        try:
            restore_writer(WRITER_PATH, current)
        except AssertionError:
            rejected = True
    assert rejected, 'Changed actual writer must fail even for old baseline input'
    print('WRITER_INVERSE_PASS paths_and_guards=6 source_files_untouched=true')


if __name__ == '__main__':
    main()
