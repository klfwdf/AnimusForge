from pathlib import Path
import xml.etree.ElementTree as E
from PIL import Image, ImageChops

p = Path(__file__).resolve().parents[1]
r = E.parse(p/'GUI/Prefabs/AFDialogueNativeOverlay.xml').getroot()
panel = r.find('.//*[@Id="AFDialogueAuxiliaryPanel"]')
assert (panel.get('SuggestedWidth'), panel.get('SuggestedHeight'), panel.get('MarginTop')) == ('1280','610','126')
parents = {c:n for n in panel.iter() for c in n}
def resolve(n, path):
    for part in path.split(chr(92)):
        if part == '..': n = parents[parents[n]]
        else: n = next(c for c in n.find('Children') if c.get('Id') == part)
    return n
for n in panel.iter():
    if n.tag in ('ScrollablePanel','AnimusForgeConversationHistoryAutoScrollPanel'):
        for key in ('ClipRect','InnerPanel','VerticalScrollbar'): resolve(n,n.get(key))
    if n.tag == 'ScrollbarWidget': resolve(n,n.get('Handle'))
    if n.tag == 'ButtonWidget':
        assert n.get('DoNotPassEventsToChildren') == 'true'
        assert all(t.get('DoNotAcceptEvents') == 'true' for t in n.findall('.//TextWidget'))
    if n.tag not in ('Children','ItemTemplate'):
        assert all(c.tag in ('Children','ItemTemplate') for c in n), n.tag
for n in panel.findall('./Children/Widget/Children/*'):
    if n.get('HorizontalAlignment') == 'Left' and n.get('WidthSizePolicy') == 'Fixed':
        assert float(n.get('MarginLeft','0'))+float(n.get('SuggestedWidth','0')) <= 1280, n.attrib
    if n.get('VerticalAlignment') == 'Top' and n.get('HeightSizePolicy') == 'Fixed':
        assert float(n.get('MarginTop','0'))+float(n.get('SuggestedHeight','0')) <= 610, n.attrib
commands = {n.get('Command.Click') for n in panel.iter()}
expected = {'Close','Confirm','SelectItems','SelectTroops','SelectPrisoners','SelectAssets','SelectShow','SelectGive',
    'PreviousResources','NextResources','LoadOlderPage','LoadNewerPage','LatestHistory','FilterAll','FilterDialogue',
    'FilterActions','ClearSelection','SelectAll','Increment','Decrement','ShowHistory','ShowTrade'}
assert expected <= commands
im = Image.open(p/'GUI/SpriteParts/afdui_aux_panel.png')
source = Image.open(p/'design/history-gift/afdui_dialogue_aux_panel_exterior_transparent.png')
assert im.mode == 'RGBA' and ImageChops.difference(im,source).getbbox() is None
assert im.getpixel((0,0))[3] == 0 and im.getpixel((im.width//2,im.height//2))[3] == 255
print('PASS: Pen panel bounds, scroll/handle paths, 22 command routes, event routing, exact approved RGBA background. Not a rendered screenshot.')
