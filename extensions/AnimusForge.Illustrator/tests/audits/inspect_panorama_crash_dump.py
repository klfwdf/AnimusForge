"""Read-only AMD64 minidump summary. Prints addresses, not arbitrary process memory text.

Requires minidump in a task-local dependency directory. Stack slots are candidates,
not a symbolized/unwound call stack; validate return instructions separately.
"""
import argparse
import json
import logging
import struct
import sys
from pathlib import Path

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("dump", type=Path)
parser.add_argument("--dependency-dir", required=True)
args = parser.parse_args()
sys.path.insert(0, args.dependency_dir)
logging.disable(logging.CRITICAL)
from minidump.minidumpfile import MinidumpFile

dump = MinidumpFile.parse(str(args.dump))
exception = dump.exception.exception_records[0]
dump.file_handle.seek(exception.ThreadContext.Rva)
context = dump.file_handle.read(exception.ThreadContext.DataSize)
if len(context) < 256:
    raise SystemExit("No complete AMD64 register context")
names = ["Rax", "Rcx", "Rdx", "Rbx", "Rsp", "Rbp", "Rsi", "Rdi", "R8", "R9", "R10", "R11", "R12", "R13", "R14", "R15", "Rip"]
registers = dict(zip(names, struct.unpack_from("<17Q", context, 120)))
modules = dump.modules.modules
owner = next((m for m in modules if m.baseaddress <= registers["Rip"] < m.baseaddress + m.size), None)
reader = dump.get_reader()
candidates = []
for slot in range(256):
    try:
        address = struct.unpack("<Q", reader.read(registers["Rsp"] + slot * 8, 8))[0]
    except Exception:
        break
    module = next((m for m in modules if m.baseaddress <= address < m.baseaddress + m.size), None)
    if module:
        candidates.append({"stackOffset": hex(slot * 8), "module": Path(module.name).name, "rva": hex(address - module.baseaddress)})
print(json.dumps({"dump": str(args.dump), "thread": exception.ThreadId,
    "exceptionCode": hex(exception.ExceptionRecord.ExceptionCode_raw),
    "exceptionInformation": [hex(x) for x in exception.ExceptionRecord.ExceptionInformation],
    "faultModule": Path(owner.name).name if owner else None,
    "faultRva": hex(registers["Rip"] - owner.baseaddress) if owner else None,
    "registers": {k: hex(v) for k, v in registers.items()}, "stackCandidates": candidates,
    "boundary": "Read-only dump metadata/registers. Candidate stack addresses are not an unwound call stack."}, indent=2))
