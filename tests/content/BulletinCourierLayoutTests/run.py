"""Layout/XML contracts, not a live Gauntlet mouse/drag acceptance test."""
from copy import deepcopy
import hashlib
from io import BytesIO
import json
from pathlib import Path
import re
import subprocess
import unittest
import xml.etree.ElementTree as ET
from PIL import Image, ImageChops
from repair_parchment import ROI, repair

ROOT = Path(__file__).resolve().parents[3]
BASE = 'ab4b4b7f'
BULLETIN = 'content/modules/AF.Module.Weekly/GUI/Prefabs/WorldBulletinPanel.xml'
COURIER = 'content/modules/AF.Module.Conversation/GUI/Prefabs/CourierLetterReplyPopup.xml'
PARCHMENT = 'content/modules/AF.Module.Weekly/GUI/SpriteParts/af_world_bulletin/af_world_bulletin_parchment.png'


def original(path):
    return subprocess.check_output(['git', 'show', BASE + ':' + path], cwd=ROOT)


def canonical(node):
    return (node.tag, tuple(sorted(node.attrib.items())), (node.text or '').strip(),
            tuple(canonical(child) for child in node))


def by_id(root, value):
    return next(node for node in root.iter() if node.get('Id') == value)


def parent(root, node):
    return next(p for p in root.iter() if node in list(p))


def children(node):
    container = node.find('Children')
    return list(container) if container is not None else []


def resolve(root, origin, path):
    nodes = {child: owner for owner in root.iter() for child in children(owner)}
    current = origin
    for part in path.split('\\'):
        current = nodes[current] if part == '..' else next(x for x in children(current) if x.get('Id') == part)
    return current


class LayoutContracts(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.b = ET.parse(ROOT / BULLETIN).getroot()
        cls.c = ET.parse(ROOT / COURIER).getroot()
        cls.old_b = ET.fromstring(original(BULLETIN))
        cls.old_c = ET.fromstring(original(COURIER))

    def test_historical_baselines_are_preserved(self):
        expected = {BULLETIN:'9F6B9902AFE5BA4AFF33762F440E4E167F6FF74AC94B3E27814BC93E9C33F9B6',
                    COURIER:'EEECF1C2DC8262836469C091F564642EC38C32199E8C5F5821047EF25D14EB56'}
        for path, digest in expected.items():
            self.assertEqual(hashlib.sha256(original(path).replace(b'\r\n',b'\n')).hexdigest().upper(), digest)

    def test_bulletin_has_only_approved_layout_changes(self):
        actual = deepcopy(self.b)
        section = next(n for n in actual.iter('Widget') if n.get('IsVisible') == '@HasMinors')
        section.attrib.update(SuggestedWidth='914', MarginLeft='98')
        del section.attrib['HorizontalAlignment']
        title = next(n for n in section.iter('TextWidget') if n.get('Text') == '其 余 消 息')
        del title.attrib['HorizontalAlignment']; title.set('MarginLeft','425')
        for column in section.iter('ListPanel'):
            if column.get('DataSource') not in ['{LeftMinors}','{RightMinors}']: continue
            del column.attrib['HorizontalAlignment']; column.set('SuggestedWidth','447')
            if column.get('DataSource') == '{RightMinors}': column.set('MarginLeft','467')
        archive = by_id(actual,'ArchiveBulletinButton')
        for key in ['HorizontalAlignment','VerticalAlignment','MarginBottom']: del archive.attrib[key]
        archive.attrib.update(MarginLeft='1022',MarginTop='700')
        footer = next(n for n in self.old_b.iter('TextWidget') if n.get('Text') == '@FooterHintText')
        owner = parent(actual,archive); owner.insert(list(owner).index(archive),deepcopy(footer))
        self.assertEqual(canonical(actual),canonical(self.old_b))

    def test_courier_has_only_approved_layout_changes(self):
        actual = deepcopy(self.c)
        impact = by_id(actual,'ImpactSummary'); owner = parent(actual,impact); index = list(owner).index(impact)
        owner.remove(impact)
        old = next(n for n in self.old_c.iter('RichTextWidget') if n.get('Text') == '@ImpactText')
        owner.insert(index,deepcopy(old))
        for button in actual.iter('ButtonWidget'):
            if button.get('Brush') != 'AFCourierLetter.Band.Button':continue
            button.set('SuggestedHeight','44')
            for text in button.iter('TextWidget'):del text.attrib['Brush.FontSize']
        gap = next(n for n in actual.iter('Widget') if n.get('IsVisible') == '@CanReply')
        gap.set('SuggestedHeight','10')
        self.assertEqual(canonical(actual),canonical(self.old_c))

    def test_footer_removed_and_archive_centered(self):
        self.assertFalse(any(n.get('Text')=='@FooterHintText' for n in self.b.iter()))
        button = by_id(self.b,'ArchiveBulletinButton')
        self.assertEqual(button.get('HorizontalAlignment'),'Center')
        self.assertEqual(button.get('VerticalAlignment'),'Bottom')
        self.assertEqual(button.get('MarginBottom'),'76')
        self.assertIsNone(button.get('MarginLeft'))
        self.assertEqual(button.get('Command.Click'),'ExecuteClose')
        self.assertEqual(button.get('IsVisible'),'@ShowCloseButton')
        self.assertEqual(button.get('DoNotPassEventsToChildren'),'true')

    def test_minor_columns_are_symmetric_and_keep_links(self):
        section=next(n for n in self.b.iter('Widget') if n.get('IsVisible')=='@HasMinors')
        self.assertEqual(section.get('HorizontalAlignment'),'Center')
        self.assertEqual(section.get('SuggestedWidth'),'1078')
        left=next(n for n in section.iter('ListPanel') if n.get('DataSource')=='{LeftMinors}')
        right=next(n for n in section.iter('ListPanel') if n.get('DataSource')=='{RightMinors}')
        self.assertEqual(left.get('HorizontalAlignment'),'Left');self.assertEqual(right.get('HorizontalAlignment'),'Right')
        self.assertEqual(left.get('SuggestedWidth'),right.get('SuggestedWidth'))
        self.assertEqual(1078-int(left.get('SuggestedWidth'))*2,20)
        for text in section.iter('RichTextWidget'):
            self.assertEqual(text.get('Command.LinkClick'),'ExecuteOpenEncyclopediaLink')
            self.assertEqual(text.get('Command.LinkAlternateClick'),'ExecuteOpenEncyclopediaLink')
            self.assertEqual(text.get('DoNotAcceptEvents'),'false')

    def test_compact_buttons_fit_and_keep_reply_gate(self):
        buttons=[n for n in self.c.iter('ButtonWidget') if n.get('Brush')=='AFCourierLetter.Band.Button']
        self.assertEqual(len(buttons),2)
        for n in buttons:
            self.assertEqual(n.get('SuggestedHeight'),'32');self.assertEqual(n.get('SuggestedWidth'),'380')
            self.assertEqual(n.get('DoNotPassEventsToChildren'),'true')
            text=n.find('Children/TextWidget')
            self.assertEqual(text.get('Brush.FontSize'),'19');self.assertEqual(text.get('DoNotAcceptEvents'),'true')
            self.assertLessEqual(int(text.get('SuggestedHeight')),int(n.get('SuggestedHeight')))
        reply=next(n for n in buttons if n.get('Command.Click')=='ExecuteReply')
        self.assertEqual(reply.get('IsVisible'),'@CanReply')
        self.assertEqual({n.get('Command.Click') for n in buttons},{'ExecuteReply','ExecuteClose'})

    def test_impact_has_complete_native_scroll_binding(self):
        region=by_id(self.c,'ImpactSummary'); panel=by_id(region,'ImpactScrollPanel')
        self.assertEqual(region.get('IsVisible'),'@HasImpact')
        clip=resolve(self.c,panel,panel.get('ClipRect'))
        inner=resolve(self.c,panel,panel.get('InnerPanel'))
        scrollbar=resolve(self.c,panel,panel.get('VerticalScrollbar'))
        handle=resolve(self.c,scrollbar,scrollbar.get('Handle'))
        self.assertEqual(clip.get('ClipContents'),'true')
        self.assertEqual(inner.get('HeightSizePolicy'),'CoverChildren')
        self.assertEqual(scrollbar.tag,'ScrollbarWidget')
        self.assertEqual(scrollbar.get('AlignmentAxis'),'Vertical')
        self.assertEqual(handle.get('Id'),'ImpactHandle')
        self.assertEqual(panel.get('AutoHideScrollBars'),'true')
        self.assertEqual(panel.get('DoNotAcceptEvents'),'false')
        for n in [scrollbar,handle]:
            self.assertNotEqual(n.get('IsVisible'),'false');self.assertEqual(n.get('DoNotAcceptEvents'),'false')
        self.assertGreater(float(scrollbar.get('SuggestedWidth')),0)

    def test_impact_grows_instead_of_fixed_text_clipping(self):
        text=next(n for n in self.c.iter('RichTextWidget') if n.get('Text')=='@ImpactText')
        self.assertEqual(text.get('HeightSizePolicy'),'CoverChildren')
        self.assertEqual(text.get('CanBreakWords'),'true')
        self.assertIsNone(text.get('SuggestedHeight'));self.assertIsNone(text.get('ClipContents'))
        self.assertEqual(text.get('Command.LinkClick'),'ExecuteOpenEncyclopediaLink')
        self.assertEqual(text.get('Command.LinkAlternateClick'),'ExecuteOpenEncyclopediaLink')
        self.assertEqual(text.get('DoNotAcceptEvents'),'false')

    def test_body_scroll_and_theme_are_unchanged(self):
        for a,b in [(by_id(self.c,'BodyClipRect'),by_id(self.old_c,'BodyClipRect')),
                    (by_id(self.c,'BodyScrollbar'),by_id(self.old_c,'BodyScrollbar'))]:
            self.assertEqual(canonical(a),canonical(b))
        new=next(n for n in self.c.iter('Widget') if n.get('DataSource')=='{Theme}')
        old=next(n for n in self.old_c.iter('Widget') if n.get('DataSource')=='{Theme}')
        self.assertEqual(canonical(new),canonical(old))

    def test_scaled_geometry_keeps_action_and_impact_separate(self):
        region=by_id(self.c,'ImpactSummary')
        footer=next(n for n in self.c.iter('ListPanel') if n.get('VerticalAlignment')=='Bottom')
        close_y=740-int(footer.get('MarginBottom'))-32
        reply_y=close_y-8-32
        for scale in [.75,1,1.25,1.5]:
            self.assertGreater((reply_y-570)*scale,0)
            self.assertGreaterEqual((780-(1180-380)/2-380)*scale,0)
            self.assertLessEqual((int(region.get('MarginTop'))+int(region.get('SuggestedHeight')))*scale,686*scale)
            archive=by_id(self.b,'ArchiveBulletinButton')
            x=(1280-int(archive.get('SuggestedWidth')))/2
            self.assertEqual((x+int(archive.get('SuggestedWidth'))/2)*scale,640*scale)

    def test_map_still_uses_one_source_and_all_xml_is_valid(self):
        entries=json.loads((ROOT/'content/content-map.json').read_text(encoding='utf-8'))['entries']
        for source in [BULLETIN,COURIER]:
            matches=[e for e in entries if e['source']==source];self.assertEqual(len(matches),1)
            self.assertEqual(matches[0]['target'],'GUI/Prefabs/'+Path(source).name)
        for file in (ROOT/'content').rglob('*.xml'):ET.parse(file)

    def test_scroll_contract_exists_in_both_native_api_sources(self):
        for version in ['1.3.x','1.4.5']:
            path=ROOT/f'原版游戏本体代码{version}/TaleWorlds.GauntletUI/TaleWorlds/GauntletUI/BaseTypes/ScrollablePanel.cs'
            source=path.read_text(encoding='utf-8-sig').replace('this.','')
            for term in ['OnMouseScroll()', 'VerticalScrollbar != null', '_canScrollVertical = true',
                         'VerticalScrollbar.MaxValue', 'InnerPanel.Size.Y', 'ClipRect.Size.Y',
                         'AutoHideScrollBars','VerticalScrollbar.Handle']:
                self.assertIn(term,source)

    def test_corner_repair_is_local_reproducible_and_preserves_transparency(self):
        baseline = original(PARCHMENT)
        self.assertEqual(hashlib.sha256(baseline).hexdigest(),
                         'bbee0ac2454e6ef10b45f20d363556602ea2169a7d980de0715ea92c7dae020f')
        old = Image.open(BytesIO(baseline))
        new = Image.open(ROOT / PARCHMENT)
        self.assertEqual((new.size, new.mode), (old.size, old.mode))
        self.assertEqual(new.getchannel('A').tobytes(), old.getchannel('A').tobytes())
        self.assertEqual(new.tobytes(), repair(old).tobytes())
        bbox = ImageChops.difference(old.convert('RGB'), new.convert('RGB')).getbbox()
        self.assertIsNotNone(bbox)
        self.assertGreaterEqual(bbox[0], ROI[0]); self.assertGreaterEqual(bbox[1], ROI[1])
        self.assertLessEqual(bbox[2], ROI[2]); self.assertLessEqual(bbox[3], ROI[3])
        # Actual missing connector gained ink, not merely a metadata/save change.
        patch = ImageChops.difference(old.convert('RGB'), new.convert('RGB')).crop((1430,1010,1595,1050))
        self.assertIsNotNone(patch.getbbox())


if __name__=='__main__':unittest.main(verbosity=2)
