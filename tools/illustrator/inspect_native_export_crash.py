"""Read-only PE analysis of a reported native crash RVA; requires pefile/capstone.

No process attachment, native invocation, memory patch or engine file modification.
Pass --dependency-dir when dependencies were installed in a local task directory.
"""
import argparse
import hashlib
import pathlib
import sys

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("native_dll", type=pathlib.Path)
parser.add_argument("--rva", type=lambda value: int(value, 0), required=True)
parser.add_argument("--dependency-dir")
args = parser.parse_args()
if args.dependency_dir:
    sys.path.insert(0, args.dependency_dir)
import pefile
import capstone

pe = pefile.PE(str(args.native_dll))
owners = [entry.struct for entry in pe.DIRECTORY_ENTRY_EXCEPTION
          if entry.struct.BeginAddress <= args.rva < entry.struct.EndAddress]
if len(owners) != 1:
    raise SystemExit("RVA did not resolve to one PE runtime function")
owner = owners[0]
base = pe.OPTIONAL_HEADER.ImageBase
disassembler = capstone.Cs(capstone.CS_ARCH_X86, capstone.CS_MODE_64)
disassembler.detail = True
instructions = list(disassembler.disasm(
    pe.get_data(owner.BeginAddress, owner.EndAddress - owner.BeginAddress), base + owner.BeginAddress))
faults = [index for index, ins in enumerate(instructions) if ins.address - base == args.rva]
if len(faults) != 1:
    raise SystemExit("RVA is not an instruction boundary in the runtime function")
fault_index = faults[0]
print("DLL:", args.native_dll)
print("SHA256:", hashlib.sha256(args.native_dll.read_bytes()).hexdigest().upper())
print("Runtime function:", hex(owner.BeginAddress), hex(owner.EndAddress))
print("Reported crash RVA:", hex(args.rva))
print("Function string references and instructions near the reported fault:")
for index, ins in enumerate(instructions):
    literals = []
    for operand in ins.operands:
        if operand.type == capstone.x86.X86_OP_MEM and operand.mem.base == capstone.x86.X86_REG_RIP:
            target = ins.address + ins.size + operand.mem.disp - base
            raw = pe.get_data(target, 80).split(b"\0")[0]
            if len(raw) > 2 and all(32 <= item < 127 for item in raw):
                literals.append(repr(raw.decode("ascii")))
    if literals or fault_index - 8 <= index <= fault_index + 8:
        print(hex(ins.address - base), ins.mnemonic, ins.op_str, " ".join(literals))
print("Boundary: static bytes only; no crash registers, stack, or live render acceptance.")
