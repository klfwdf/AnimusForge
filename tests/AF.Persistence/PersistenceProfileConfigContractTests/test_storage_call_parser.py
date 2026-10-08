import unittest
import copy
from unittest.mock import patch
import validate_persistence_profile_config as validator

from validate_persistence_profile_config import extract_call_arguments, split_call_arguments
from pathlib import Path
import importlib.util
import json
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / "tools"))
import PersistenceIdentityAudit as identity
from PersistenceTypedRef import resolve_snapshot_bindings


class StorageCallParserTests(unittest.TestCase):
    def test_civil_war_v2_catalog_and_v1_negative_control(self):
        catalog=validator.load_json(validator.FIXTURE_DIR/'persistence-catalog.json')
        bindings=validator.load_json(validator.FIXTURE_DIR/'syncdata-binding-catalog.json')
        actual=validator.discover_typed_bindings()
        rows=[row for row in actual if row['key'].startswith('_af_kingdom_civil_war_')]
        self.assertEqual(len(rows),2)
        self.assertTrue(all(row['key']=='_af_kingdom_civil_war_v2' and row['type']=='string' and row['ref']=='_civilWarJsonStorage' for row in rows))
        with patch.object(validator,'discover_typed_bindings',return_value=actual):
            validator.validate_typed_bindings(bindings,catalog)
        for bad_key in ['_af_kingdom_civil_war_v1','_af_kingdom_civil_war_v3']:
            changed=copy.deepcopy(actual)
            for row in changed:
                if row['key']=='_af_kingdom_civil_war_v2':row['key']=bad_key
            with patch.object(validator,'discover_typed_bindings',return_value=changed):
                with self.assertRaisesRegex(AssertionError,'binding catalog drifted'):
                    validator.validate_typed_bindings(bindings,catalog)

    def test_typed_ref_partial_same_authority(self):
        snapshot = [
            ("Host.cs", 'namespace A; class Host { State store; void Save() { SyncData("key", ref store.Text); } }'),
            ("State.cs", 'namespace A; partial class State { internal string Text; }'),
            ("State.Extra.cs", 'namespace A; partial class State { internal int Extra; }'),
        ]
        self.assertEqual(resolve_snapshot_bindings(ROOT, snapshot, identity.sync_bindings), {("key", "string")})

    def test_typed_ref_qualified_names_are_not_ambiguous(self):
        snapshot = [
            ("Host.cs", 'namespace A; class Host { State store; void Save() { SyncData("key", ref store.Text); } }'),
            ("State.cs", 'namespace A; class State { internal string Text; }'),
            ("Other.cs", 'namespace B; class State { internal int Text; }'),
        ]
        self.assertEqual(resolve_snapshot_bindings(ROOT, snapshot, identity.sync_bindings), {("key", "string")})

    def test_typed_ref_true_duplicate_type_is_rejected(self):
        snapshot = [
            ("Host.cs", 'namespace A; class Host { State store; void Save() { SyncData("key", ref store.Text); } }'),
            ("State.cs", 'namespace A; class State { internal string Text; }'),
            ("Duplicate.cs", 'namespace A; class State { internal string Text; }'),
        ]
        with self.assertRaisesRegex((ValueError, RuntimeError), "ambiguous"):
            resolve_snapshot_bindings(ROOT, snapshot, identity.sync_bindings)

    def test_typed_ref_property_keeps_declared_storage_type(self):
        snapshot = [("Host.cs", 'namespace A; class Host { string text; ref string Alias => ref text; void Save() { SyncData("key", ref Alias); } }')]
        self.assertEqual(resolve_snapshot_bindings(ROOT, snapshot, identity.sync_bindings), {("key", "string")})

    def test_explicit_nested_generic_sync_is_not_silently_omitted(self):
        source = 'using System.Collections.Generic; namespace A; class MobileParty {} class Host { Dictionary<MobileParty, string> parties; void Save() { data.SyncData<Dictionary<MobileParty, string>>("_af_wildernessNonHeroPartyMemoryIds_v1", ref parties); } }'
        expected = {("_af_wildernessNonHeroPartyMemoryIds_v1", "Dictionary<MobileParty, string>")}
        self.assertEqual(resolve_snapshot_bindings(ROOT, [("Host.cs", source)], identity.sync_bindings), expected)
        # Unrelated migrated ref aliases must not change existing key discovery.
        migrated = source.replace('void Save()', 'string text; ref string Alias => ref text; void Save()').replace('ref parties);', 'ref parties); data.SyncData("other", ref Alias);')
        self.assertEqual(resolve_snapshot_bindings(ROOT, [("Host.cs", migrated)], identity.sync_bindings), expected | {("other", "string")})

    def test_production_sources_use_evaluated_compile_not_test_shadow(self):
        # Only evaluation is controlled; production_sources must consume Compile metadata.
        result = subprocess.CompletedProcess([], 0, json.dumps({"Items": {"Compile": [
            {"FullPath": str(ROOT / "src/AF.Persistence/OwnerJsonStorageCodec.cs")}
        ]}}), "")
        with patch.object(identity.subprocess, "run", return_value=result):
            self.assertEqual(identity.production_sources(), [ROOT / "src/AF.Persistence/OwnerJsonStorageCodec.cs"])

    def test_historical_compile_uses_own_inventory_and_ordered_links(self):
        project = r'''<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup>
          <TargetFramework>net472</TargetFramework>
          <DefaultItemExcludes>$(DefaultItemExcludes);$(MSBuildProjectDirectory)\local\**</DefaultItemExcludes>
        </PropertyGroup><ItemGroup>
          <Compile Remove="tests\**\*.cs;extensions\**\*.cs" />
          <Compile Include="extensions\Linked\src\**\*.cs" Exclude="extensions\Linked\src\obj\**\*.cs" />
          <Compile Remove="extensions\Linked\src\Removed.cs" />
        </ItemGroup></Project>'''
        inventory = ['Host.cs', 'tests/Fake.cs', 'extensions/Unlinked/Fake.cs',
                     'extensions/Linked/src/Owner.cs', 'extensions/Linked/src/Removed.cs',
                     'extensions/Linked/src/obj/Fake.cs', 'local/Fake.cs', 'obj/Fake.cs']
        self.assertEqual(identity.historical_compile_paths(project, inventory),
                         ['Host.cs', 'extensions/Linked/src/Owner.cs'])
        # A genuine same-qualified production conflict remains visible to semantic resolution.
        duplicate = [('Host.cs', 'namespace A; class Host { State store; void Save() { SyncData("k", ref store.Text); } }'),
                     ('Owner.cs', 'namespace A; class State { internal string Text; }'),
                     ('OtherOwner.cs', 'namespace A; class State { internal string Text; }')]
        with self.assertRaisesRegex((ValueError, RuntimeError), 'ambiguous'):
            resolve_snapshot_bindings(ROOT, duplicate, identity.sync_bindings)

    def test_symbolic_inventory_distinguishes_lifecycle_and_calls_from_other_declarations(self):
        self.assertTrue(validator.SYMBOLIC_PATTERN.search('dataStore.SyncData(StorageKey, ref storage);'))
        self.assertTrue(validator.SYMBOLIC_PATTERN.search('public override void SyncData(IDataStore dataStore) {}'))
        self.assertTrue(validator.SYMBOLIC_PATTERN.search('internal static void SyncData(IDataStore dataStore) {}'))
        self.assertFalse(validator.SYMBOLIC_PATTERN.search('public void SyncData(bool isSaving, bool isLoading, Action save) {}'))
        self.assertFalse(validator.SYMBOLIC_PATTERN.search('void SyncData(bool isSaving, bool isLoading);'))
        self.assertFalse(validator.SYMBOLIC_PATTERN.search('dataStore.SyncData( "literal", ref storage);'))

    def test_current_symbolic_inventory_does_not_double_count_historical_projection(self):
        retired = Path("retired-helper.cs")
        thin = b"void SyncRecovery(IDataStore dataStore) { adapter.Sync(dataStore); }"
        prior = "void SyncRecovery(IDataStore dataStore) { dataStore.SyncData(StorageKey, ref storage); }"
        # The ambient historical hook restores the old call, while the current
        # physical helper has none. It must not duplicate the sole adapter.
        with patch.object(Path, "read_text", return_value=prior), patch.object(Path, "read_bytes", return_value=thin):
            self.assertFalse(validator.current_symbolic_persistence_source(retired))
        # Genuine current calls remain visible even when historical text omits them.
        with patch.object(Path, "read_text", return_value=thin.decode()), patch.object(Path, "read_bytes", return_value=prior.encode()):
            self.assertTrue(validator.current_symbolic_persistence_source(Path("sole-adapter.cs")))

    def test_nested_source_expression_preserves_storage_key(self):
        source = 'FlattenStringDictionary(_inbox.ExportRecords(), SaveKeyRecords, "WorldEventInbox")'
        calls = list(extract_call_arguments(source, "FlattenStringDictionary"))
        self.assertEqual(len(calls), 1)
        self.assertEqual(split_call_arguments(calls[0])[1], "SaveKeyRecords")

    def test_quoted_parenthesis_does_not_truncate_call(self):
        source = 'SaveChunkedString(value, "key(x)", "label")'
        calls = list(extract_call_arguments(source, "SaveChunkedString"))
        self.assertEqual(split_call_arguments(calls[0])[1], '"key(x)"')


if __name__ == "__main__":
    unittest.main()
