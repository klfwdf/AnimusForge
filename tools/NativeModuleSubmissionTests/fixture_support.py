"""Share the actual operation dependency with prior Native owner fixtures; no method substitutions."""
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
def include_operation_sources(out):
    for name in ['CoreDialogueContracts.cs','CoreDialogueOperation.cs']:
        (out/name).write_text((ROOT/'Refactor/Modules'/name).read_text(encoding='utf-8-sig'),encoding='utf-8')
    (out/'CoreDialogueUsing.cs').write_text('global using AnimusForge.Refactor.Modules;\n',encoding='utf-8')

ADMISSION_OWNER = 'src/modules/AF.Module.Conversation/Channels/Native/NativeConversationAdmissionOwner.cs'
def include_admission_owner(out):
    (out/'NativeConversationAdmissionOwner.cs').write_text((ROOT/ADMISSION_OWNER).read_text(encoding='utf-8-sig'), encoding='utf-8')

def migrate_admission_fixture(code):
    """Translate fixture setup, not assertions; historical runs retain the original setup."""
    fields = 'private NativeConversationAdmission _nativeConversationAdmission;\n private long _nativeConversationAdmissionEpoch=1,_nativeConversationPresentationRevision=1;'
    if fields in code:
        code = code.replace(fields, 'private readonly NativeConversationAdmissionOwner<NativeConversationAdmission> _nativeAdmissionOwner = CreateAdmissionOwner();\n private static NativeConversationAdmissionOwner<NativeConversationAdmission> CreateAdmissionOwner(){var owner=new NativeConversationAdmissionOwner<NativeConversationAdmission>();owner.EndConversation();owner.BeginPresentation();return owner;}', 1)
    code = code.replace('_nativeConversationAdmissionEpoch++;_nativeConversationAdmission=null;', '_nativeAdmissionOwner.EndConversation();')
    code = code.replace('_nativeConversationAdmissionEpoch++', '_nativeAdmissionOwner.EndConversation()')
    code = code.replace('_nativeConversationPresentationRevision++', '_nativeAdmissionOwner.BeginPresentation()')
    code = code.replace('_nativeConversationAdmission=null;', '_nativeAdmissionOwner.Release(_nativeAdmissionOwner.Current);')
    code = code.replace('_nativeConversationAdmission=admission;', '_nativeAdmissionOwner.ReserveCaptured(admission);')
    import re
    code = re.sub(r'_nativeConversationAdmission=(new NativeConversationAdmission\{[^}]+\});', r'_nativeAdmissionOwner.ReserveCaptured(\1);', code)
    for old,new in [('_nativeConversationAdmissionEpoch','_nativeAdmissionOwner.ConversationEpoch'),('_nativeConversationPresentationRevision','_nativeAdmissionOwner.PresentationRevision'),('_nativeConversationAdmission','_nativeAdmissionOwner.Current')]:
        code = re.sub(r'\b'+old+r'\b',new,code)
    # Pending-history rejection runs with the original captured request after ConversationEnded
    # clears the real slot. Do not recapture a new ticket (or dereference the empty slot).
    code = code.replace('internal sealed class Captured{internal string Key,Player;', 'internal sealed class Captured{internal NativeConversationAdmission Admission;internal string Key,Player;')
    code = code.replace('return new Captured{Key=', 'return new Captured{Admission=admission,Key=')
    code = code.replace('Reject(Captured captured,int stage=0){var admission=_nativeAdmissionOwner.Current;', 'Reject(Captured captured,int stage=0){var admission=captured.Admission;')
    return code
