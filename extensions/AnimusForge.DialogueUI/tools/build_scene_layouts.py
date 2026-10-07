"""Build the scene action wheel and the two persistent-session panels from Pen coordinates.

Pen frames (1920x1080): NPU6i wheel, WmTFL + UJB2B scroll style, Ej55d side folio, gJg8O collapsed.
"""
from pathlib import Path
import xml.etree.ElementTree as E
from prepare_wheel_wedges import wedge_box
from wheel_geometry import ART_SIZE

ROOT = Path(__file__).resolve().parents[1]
PREFABS = ROOT / 'GUI/Prefabs'
SOLID = 'BlankWhiteSquare_9'
INK = '#29180CFF'
MUTED = '#6E5033FF'
CREAM = '#F0D9AEFF'


def el(parent, tag, **attrs):
    return E.SubElement(parent, tag, {k.replace('__', '.'): str(v) for k, v in attrs.items()})


def children(parent):
    return el(parent, 'Children')


def box(parent, x, y, w, h, **attrs):
    attrs.setdefault('DoNotAcceptEvents', 'true')
    return el(parent, 'Widget', WidthSizePolicy='Fixed', HeightSizePolicy='Fixed', SuggestedWidth=w, SuggestedHeight=h,
              HorizontalAlignment='Left', VerticalAlignment='Top', MarginLeft=x, MarginTop=y, **attrs)


def rect(parent, x, y, w, h, color, **attrs):
    return box(parent, x, y, w, h, Sprite=SOLID, Color=color, **attrs)


def framed(parent, x, y, w, h, fill, stroke, **attrs):
    c = children(box(parent, x, y, w, h, **attrs))
    rect(c, 1, 1, w - 2, h - 2, fill)
    for ex, ey, ew, eh in ((0, 0, w, 1), (0, h - 1, w, 1), (0, 1, 1, h - 2), (w - 1, 1, 1, h - 2)):
        rect(c, ex, ey, ew, eh, stroke)
    return c


def text(parent, value, x, y, w, h, size=14, color=INK, align='Left', valign='Center', **attrs):
    return el(parent, 'TextWidget', WidthSizePolicy='Fixed', HeightSizePolicy='Fixed', SuggestedWidth=w, SuggestedHeight=h,
              HorizontalAlignment='Left', VerticalAlignment='Top', MarginLeft=x, MarginTop=y, Text=value,
              Brush='Popup.Button.Text', DoNotAcceptEvents='true', Brush__FontSize=size, Brush__FontColor=color,
              Brush__TextHorizontalAlignment=align, Brush__TextVerticalAlignment=valign, **attrs)


def button(parent, bid, label, command, x, y, w, h, size=14, color=INK, **attrs):
    b = el(parent, 'ButtonWidget', Id=bid, WidthSizePolicy='Fixed', HeightSizePolicy='Fixed', SuggestedWidth=w, SuggestedHeight=h,
           HorizontalAlignment='Left', VerticalAlignment='Top', MarginLeft=x, MarginTop=y, Brush='Popup.Cancel.Button',
           DoNotAcceptEvents='false', DoNotPassEventsToChildren='true', UpdateChildrenStates='true', Command__Click=command, **attrs)
    if label is not None:
        el(children(b), 'TextWidget', WidthSizePolicy='StretchToParent', HeightSizePolicy='StretchToParent', Text=label,
           Brush='Popup.Button.Text', DoNotAcceptEvents='true', Brush__FontSize=size, Brush__FontColor=color,
           Brush__TextHorizontalAlignment='Center', Brush__TextVerticalAlignment='Center')
    return b


def scroll(parent, name, source, x, y, w, h, auto=False):
    host = children(box(parent, x, y, w, h))
    attrs = {'AutoScrollRequestVersion': '@HistoryScrollVersion'} if auto else {}
    s = el(host, 'AnimusForgeConversationHistoryAutoScrollPanel' if auto else 'ScrollablePanel', WidthSizePolicy='StretchToParent',
           HeightSizePolicy='StretchToParent', MarginRight=12, ClipRect=name + 'Clip', InnerPanel=name + 'Clip\\' + name + 'List',
           MouseScrollAxis='Vertical', VerticalScrollbar='..\\' + name + 'Bar', AutoHideScrollBars='true', **attrs)
    clip = el(children(s), 'Widget', Id=name + 'Clip', WidthSizePolicy='StretchToParent', HeightSizePolicy='StretchToParent', ClipContents='true')
    lst = el(children(clip), 'ListPanel', Id=name + 'List', DataSource='{' + source + '}', WidthSizePolicy='StretchToParent',
             HeightSizePolicy='CoverChildren', StackLayout__LayoutMethod='VerticalTopToBottom')
    bar = el(host, 'ScrollbarWidget', Id=name + 'Bar', WidthSizePolicy='Fixed', SuggestedWidth=5, HeightSizePolicy='StretchToParent',
             HorizontalAlignment='Right', AlignmentAxis='Vertical', Handle=name + 'Handle', MinValue=0, MaxValue=100)
    el(children(bar), 'Widget', Id=name + 'Handle', WidthSizePolicy='StretchToParent', HeightSizePolicy='Fixed', SuggestedHeight=26, Sprite='afdui_scroll_handle')
    return el(lst, 'ItemTemplate')


def editor(parent, x, y, w, h, color, hint, hint_color):
    c = children(box(parent, x, y, w, h))
    el(c, 'TextWidget', WidthSizePolicy='StretchToParent', HeightSizePolicy='StretchToParent', MarginLeft=12, MarginRight=12, MarginTop=10,
       Text=hint, Brush='ConversationItem.Text', DoNotAcceptEvents='true', IsVisible='@IsInputEmpty', Brush__FontSize=15,
       Brush__FontColor=hint_color, Brush__TextHorizontalAlignment='Left', Brush__TextVerticalAlignment='Top')
    el(c, 'DevMultilineEditableTextWidget', Id='AFSessionInput', WidthSizePolicy='StretchToParent', HeightSizePolicy='StretchToParent',
       MarginLeft=12, MarginRight=12, MarginTop=10, MarginBottom=10, VerticalAlignment='Top', Command__TextEntered='ExecuteSubmit',
       Brush='ConversationItem.Text', Brush__FontSize=16, Brush__FontColor=color, Brush__TextHorizontalAlignment='Left',
       Brush__TextVerticalAlignment='Top', EditorFontSize=16, RealText='@InputText', AutoFocus='false', SubmitOnEnter='true',
       UseBlackCaret='false', MaxLength=6000)


def root(name, **attrs):
    prefab = E.Element('Prefab')
    r = el(el(prefab, 'Window'), 'Widget', WidthSizePolicy='StretchToParent', HeightSizePolicy='StretchToParent',
           DoNotAcceptEvents='true', DoNotPassEventsToChildren='false', **attrs)
    return prefab, children(r), name


def catch_all(parent, visible=None, color=None):
    # Swallows clicks on empty screen while the panel owns input, so the mission never sees them
    # (no attack swing / camera drag while typing). First child, so every real control wins hits.
    attrs = {'IsVisible': visible} if visible else {}
    if color:
        attrs.update(Sprite=SOLID, Color=color)
    return el(parent, 'Widget', WidthSizePolicy='StretchToParent', HeightSizePolicy='StretchToParent', DoNotAcceptEvents='false', **attrs)


def anchored(parent, w, h, horizontal, vertical, **margins):
    return el(parent, 'Widget', WidthSizePolicy='Fixed', HeightSizePolicy='Fixed', SuggestedWidth=w, SuggestedHeight=h,
              HorizontalAlignment=horizontal, VerticalAlignment=vertical, DoNotAcceptEvents='true', **margins)


def dark_button(parent, bid, label, command, x, y, w, h, fill='#2A1E14E0', stroke='#9E7A46FF', color='#E8D5B5FF', size=12, **attrs):
    # A conditional button hides its frame too, so an overlaid variant never shows through.
    frame_attrs = {'IsVisible': attrs['IsVisible']} if 'IsVisible' in attrs else {}
    framed(parent, x, y, w, h, fill, stroke, **frame_attrs)
    return button(parent, bid, label, command, x, y, w, h, size, color, **attrs)


def history_row(item):
    row = children(el(item, 'ListPanel', WidthSizePolicy='StretchToParent', HeightSizePolicy='CoverChildren', MarginBottom=8,
                      DoNotAcceptEvents='true', StackLayout__LayoutMethod='VerticalTopToBottom'))
    speaker = children(el(row, 'Widget', WidthSizePolicy='StretchToParent', HeightSizePolicy='Fixed', SuggestedHeight=22, DoNotAcceptEvents='true'))
    for visible, color in (('@IsPlayer', '#8A6220FF'), ('@IsNpc', '#4A321EFF'), ('@IsFact', '#2F5E78FF')):
        el(speaker, 'TextWidget', WidthSizePolicy='StretchToParent', HeightSizePolicy='StretchToParent', Text='@Speaker', IsVisible=visible,
           Brush='Popup.Button.Text', DoNotAcceptEvents='true', Brush__FontSize=13, Brush__FontColor=color,
           Brush__TextHorizontalAlignment='Left', Brush__TextVerticalAlignment='Center')
    el(row, 'TextWidget', WidthSizePolicy='StretchToParent', HeightSizePolicy='CoverChildren', Text='@Text', Brush='Popup.Button.Text',
       DoNotAcceptEvents='true', Brush__FontSize=16, Brush__FontColor=INK, Brush__TextHorizontalAlignment='Left', Brush__TextVerticalAlignment='Top')


def history_drawer(parent, **placement):
    d = children(anchored(parent, 960, 520, placement.pop('horizontal'), 'Top', IsVisible='@IsHistoryOpen', Sprite='afdui_parchment_panel', **placement))
    text(d, '场景对话记录', 44, 26, 400, 30, 19)
    button(d, 'AFTabCloseHistory', '关闭', 'ExecuteToggleHistory', 816, 26, 100, 32, 14)
    history_row(scroll(d, 'AFHistoryDrawer', 'HistoryLines', 44, 70, 872, 420, auto=True))


def participant_card(item, w, h, image, name, detail, seal, gap, fill, stroke, sizes=(14, 12, 12)):
    card = children(box(item, 0, 0, w, h, MarginBottom=gap))
    rect(card, 0, 0, w, h, stroke)
    rect(card, 1, 1, w - 2, h - 2, fill)
    rect(card, 1, 1, w - 2, h - 2, '#E8C77A55', IsVisible='@IsAddressee')
    pick = children(button(card, 'AFRowSelect', None, 'ExecuteSelect', 0, 0, seal[0] - 4, h))
    ix, iy, isz = image
    el(pick, 'ImageIdentifierWidget', WidthSizePolicy='Fixed', HeightSizePolicy='Fixed', SuggestedWidth=isz, SuggestedHeight=isz,
       HorizontalAlignment='Left', VerticalAlignment='Top', MarginLeft=ix, MarginTop=iy, DoNotAcceptEvents='true', IsVisible='@HasImage',
       ImageId='@ImageId', AdditionalArgs='@ImageArgs', TextureProviderName='@ImageProvider',
       CircularClipEnabled='true', CircularClipRadius=isz // 2, CircularClipSmoothingRadius=2)
    text(pick, '@Name', *name, sizes[0], INK)
    text(pick, '@Detail', *detail, sizes[1], '#5E432AFF', valign='Top')
    sx, sy, sw, sh = seal
    sc = children(button(card, 'AFRowSeal', None, 'ExecuteCycle', sx, sy, sw, sh))
    for visible, sprite in (('@IsParticipating', 'afdui_seal_base_green'), ('@IsExcluded', 'afdui_seal_base_red'), ('@IsLocked', 'afdui_seal_base_gold')):
        box(sc, 6, (sh - 22) // 2, 22, 22, Sprite=sprite, IsVisible=visible)
    text(sc, '@StateText', 32, 0, sw - 32, sh, sizes[2], '#5C3E14FF')
    # Visual dim only: DoNotAcceptEvents keeps both buttons clickable underneath.
    rect(card, 0, 0, w, h, '#F5EDDE88', IsVisible='@IsDimmed')


def save(tree):
    prefab, _, name = tree
    E.indent(prefab, space='  ')
    E.ElementTree(prefab).write(PREFABS / (name + '.xml'), encoding='utf-8', xml_declaration=False)
    print('written', name)


def editable(parent, binding, maxlen, align, color, left, right, size):
    return el(parent, 'AnimusForgeNativeConversationEditableTextWidget', WidthSizePolicy='StretchToParent', HeightSizePolicy='StretchToParent',
              MarginLeft=left, MarginRight=right, Brush='Popup.Button.Text', RealText='@' + binding, MaxLength=maxlen, AutoFocus='false',
              ClipContents='true', Brush__FontSize=size, Brush__FontColor=color, Brush__TextHorizontalAlignment=align,
              Brush__TextVerticalAlignment='Center', Command__FocusGained='StartTyping', Command__FocusLost='StopTyping')


def tab(parent, bid, label, command, x, y, w, h, selected=None, enabled=None, size=15, **attrs):
    if selected:
        attrs['IsSelected'] = '@' + selected
    if enabled:
        attrs['IsEnabled'] = '@' + enabled
    return button(parent, bid, label, command, x, y, w, h, size, '#3B281BFF', **attrs)


def staged_trade(parent, x, y, w, size):
    text(parent, '@StagedTradeText', x, y, w - 114, 24, size, '#8A4A12FF', IsVisible='@HasStagedTrade')
    tab(parent, 'AFTabCancelTrade', '取消给予', 'ExecuteCancelStagedTrade', x + w - 108, y, 108, 24, size=12, IsVisible='@HasStagedTrade')


def trade_panel(parent):
    # Pen CxuqP "02 给予与展示" (1280x610): the dialogue give panel's layout; children in panel coordinates.
    t = children(anchored(parent, 1280, 610, 'Center', 'Top', MarginTop=150, DataSource='{Trade}', IsVisible='@IsOpen',
                          Sprite='afdui_aux_panel'))
    text(t, '@Title', 88, 100, 300, 30, 22)
    tab(t, 'AFTabTradeClose', '返回对话', 'Close', 1056, 104, 136, 40)
    for x, label, cmd, sel in ((88, '物品 / 第纳尔', 'SelectItems', 'IsItems'), (276, '部队', 'SelectTroops', 'IsTroops'),
                               (464, '俘虏', 'SelectPrisoners', 'IsPrisoners'), (652, '固定资产', 'SelectAssets', 'IsAssets')):
        tab(t, 'AFTabTradeMode', label, cmd, x, 160, 180, 36, sel, 'CanInteract')
    tab(t, 'AFTabTradeGive', '给予', 'SelectGive', 868, 160, 158, 36, 'IsGive', 'CanInteract')
    tab(t, 'AFTabTradeShow', '展示', 'SelectShow', 1034, 160, 158, 36, 'IsShow', 'CanShow')
    s = framed(t, 88, 204, 752, 34, '#FFF7E24D', '#AC895566')
    box(s, 10, 9, 16, 16, Sprite='afdui_icon_search')
    text(s, '搜索物品或第纳尔…', 36, 0, 576, 34, 14, '#947451FF', IsVisible='@IsSearchEmpty')
    text(s, '@ResourceCountText', 622, 0, 120, 34, 13, '#8A6944FF', 'Right')
    editable(s, 'SearchText', 80, 'Left', '#3B281BFF', 36, 140, 15)
    for label, x, w, align in (('资源', 100, 460, 'Left'), ('持有', 618, 120, 'Center'), ('估值', 738, 95, 'Center')):
        text(t, label, x, 238, w, 26, 13, '#8B693FFF', align)
    ri = scroll(t, 'AFTradeResources', 'ResourceItems', 88, 264, 766, 220)
    row = children(button(ri, 'AFRowResource', None, 'Toggle', 0, 0, 752, 44, IsSelected='@IsSelected', IsEnabled='@CanSelect'))
    rect(row, 0, 43, 752, 1, '#98764633')
    el(row, 'ImageIdentifierWidget', WidthSizePolicy='Fixed', HeightSizePolicy='Fixed', SuggestedWidth=34, SuggestedHeight=26,
       HorizontalAlignment='Left', VerticalAlignment='Top', MarginLeft=8, MarginTop=9, DoNotAcceptEvents='true', IsVisible='@HasImage',
       ImageId='@ImageId', AdditionalArgs='@ImageArgs', TextureProviderName='@ImageProvider')
    box(row, 12, 10, 24, 24, Sprite='afdui_icon_coin', IsVisible='@IsGold')
    text(row, '@Name', 48, 4, 472, 20, 15, '#412D1DFF')
    text(row, '@Category', 48, 24, 472, 16, 11, '#987750FF')
    text(row, '@AvailableText', 530, 0, 120, 44, 14, '#64482DFF', 'Center')
    text(row, '@ValueText', 650, 0, 95, 44, 14, '#64482DFF', 'Center')
    text(t, '没有符合条件的资源', 88, 330, 752, 52, 16, MUTED, 'Center', IsVisible='@IsResourceEmpty')
    text(t, '@SelectionTitle', 868, 204, 216, 30, 17)
    tab(t, 'AFTabTradeClear', '清空', 'ClearSelection', 1124, 204, 68, 30, enabled='CanInteract')
    si = children(box(scroll(t, 'AFTradeSelection', 'SelectedItems', 868, 238, 324, 206), 0, 0, 306, 76))
    text(si, '@Name', 0, 0, 264, 27, 15)
    tab(si, 'AFTabQty', '×', 'Remove', 274, 0, 30, 28)
    tab(si, 'AFTabQty', '−', 'Decrement', 0, 33, 32, 32)
    q = children(rect(si, 38, 33, 130, 32, '#A78145FF'))
    editable(children(rect(q, 1, 1, 128, 30, '#25190FFF')), 'Quantity', 10, 'Center', '@QuantityColor', 6, 6, 16)
    tab(si, 'AFTabQty', '+', 'Increment', 174, 33, 32, 32)
    tab(si, 'AFTabQty', '全部', 'SelectAll', 214, 33, 90, 32)
    text(t, '从左侧选择资源', 884, 294, 276, 52, 15, MUTED, 'Center', IsVisible='@IsSelectionEmpty')
    text(t, '估值合计', 868, 452, 94, 32, 14, MUTED)
    text(t, '@TotalValue', 968, 452, 224, 32, 16, '#3B281BFF', 'Right')
    tab(t, 'AFTabTradePage', '上一页', 'PreviousResources', 88, 498, 90, 38, enabled='CanResourcePrevious')
    text(t, '@ResourcePageText', 184, 498, 188, 38, 13, MUTED, 'Center')
    tab(t, 'AFTabTradePage', '下一页', 'NextResources', 378, 498, 90, 38, enabled='CanResourceNext')
    text(t, '@TradeNote', 486, 493, 354, 46, 13, MUTED)
    tab(t, 'AFTabTradeConfirm', '@ConfirmText', 'Confirm', 868, 498, 324, 38, enabled='CanSubmit')
    text(t, '@StatusText', 88, 542, 1104, 26, 13, '#7B3D2FFF', 'Center')


def build_wheel():
    # Pen NPU6i after the 50% shrink: wheel 480x480 at (720, 300) on the 1920x1080 canvas.
    tree = root('AFSceneWheel')
    r = tree[1]
    catch_all(r, color='#00000059')
    c = children(anchored(r, 1920, 1080, 'Center', 'Center'))
    wx, wy, ws = 720, 300, 480
    box(c, wx, wy, ws, ws, Sprite='afdui_wheel_chassis_symmetric')
    k = ws / ART_SIZE
    for sector, flag in (('talk', 'IsHoverTalk'), ('actions', 'IsHoverActions'), ('leave', 'IsHoverLeave'), ('give', 'IsHoverGive')):
        x0, y0, x1, y1 = wedge_box(sector)
        box(c, round(wx + x0 * k, 2), round(wy + y0 * k, 2), round((x1 - x0) * k, 2), round((y1 - y0) * k, 2),
            Sprite='afdui_wheel_wedge_' + sector, IsVisible='@' + flag)
    # Sector labels only (no buttons); the rumor sector stays blank until the host has that action.
    for label, x, y, extra in (('对 话', 766, 399, {}), ('@ActionsText', 1011, 400, {'IsVisible': '@HasActions'}),
                               ('离 开', 905, 630, {}), ('给 予', 1061, 586, {})):
        text(c, label, x, y, 110, 28, 16, '#3B2A1AFF', 'Center', **extra)
    view = children(box(c, 917.75, 497.75, 84.5, 84.5, CircularClipEnabled='true', CircularClipRadius=42, CircularClipSmoothingRadius=1))
    el(view, 'Widget', WidthSizePolicy='StretchToParent', HeightSizePolicy='StretchToParent', Sprite=SOLID, Color='#1E1510FF', DoNotAcceptEvents='true')
    # Half of the conversation portrait geometry (same crop ratios); the layer re-applies the half offsets.
    el(view, 'CharacterTableauWidget', Id='AFWheelPortrait', WidthSizePolicy='Fixed', HeightSizePolicy='Fixed', SuggestedWidth=253.5, SuggestedHeight=315,
       HorizontalAlignment='Left', VerticalAlignment='Top', PositionXOffset=-84.5, PositionYOffset=-67, StanceIndex=1,
       CustomRenderScale=2.7, IsVisible='false', DoNotAcceptEvents='true')
    # One invisible hit area for the whole ring; the layer resolves the sector from the cursor angle.
    button(c, 'AFBareWheelHit', None, 'ExecuteSector', wx, wy, ws, ws)
    # After the hit area so a click on the nameplate is taken by it (inert) and never reaches the ring.
    plate = children(box(c, 860, 596, 200, 32, Id='AFWheelPlate', Sprite='afdui_plaque_nameplate', DoNotAcceptEvents='false'))
    text(plate, '@TargetName', 16, 0, 168, 32, 13, '#FCE5B8FF', 'Center')
    d = framed(c, 1215, 320, 200, 440, '#16110DEE', '#C8A050FF', IsVisible='@IsDrawerOpen')
    head = children(box(d, 10, 10, 180, 34, Sprite='afdui_plaque_nameplate'))
    text(head, '@DrawerHeader', 0, 0, 180, 34, 13, '#FFDF95FF', 'Center')
    item = scroll(d, 'AFWheelDrawer', 'DrawerItems', 10, 54, 188, 380)
    button(item, 'AFCapItem', '@Label', 'ExecuteSelect', 0, 0, 180, 44, 14, '#FCE5B8FF', IsEnabled='@IsEnabled', MarginBottom=8)
    save(tree)


def build_scroll():
    tree = root('AFSceneSessionScroll', IsVisible='@IsVisible')
    r = tree[1]
    catch_all(r, visible='@IsExpanded')
    expanded = children(el(r, 'Widget', WidthSizePolicy='StretchToParent', HeightSizePolicy='StretchToParent', DoNotAcceptEvents='true', IsVisible='@IsExpanded'))
    chrome = children(el(expanded, 'Widget', WidthSizePolicy='StretchToParent', HeightSizePolicy='StretchToParent', DoNotAcceptEvents='true', IsVisible='@IsChromeVisible'))
    # Pen WmTFL after the 75% shrink: bottom scroll 1380x285 at (270, 785); children in scroll coordinates.
    s = children(anchored(chrome, 1380, 285, 'Center', 'Bottom', MarginBottom=10))
    box(s, 0, 0, 1380, 285, Sprite='afdui_scroll_chassis_clean')
    button(s, 'AFRowCollapse', '收起界面', 'ExecuteCollapse', 572, 28, 120, 30, 13, '#3B1802FF')
    text(s, '@AddresseeText', 275, 80, 400, 20, 14, '#E2C38AFF')
    text(s, '@StatusText', 680, 80, 270, 20, 13, '#FFB08AFF', 'Right')
    editor(s, 263, 100, 698, 98, '#F0DFBEFF', '输入你想对周围人说的话…… (Enter 发送 · Shift+Enter 换行)', '#E2C38AAA')
    staged_trade(s, 275, 200, 680, 13)
    button(s, 'AFPlateHistory', '历史', 'ExecuteToggleHistory', 991, 82, 70, 32, 13, '#361F0EFF')
    button(s, 'AFPlateGift', '赠送', 'ExecuteGift', 1067, 82, 70, 32, 13, '#361F0EFF', IsEnabled='@CanSend')
    button(s, 'AFPlateCodex', '图鉴', 'ExecuteEncyclopedia', 1143, 82, 70, 32, 13, '#361F0EFF')
    button(s, 'AFPlateIllustrate', '@IllustrationButtonText', 'ExecuteIllustrate', 1219, 82, 70, 32, 13, '#361F0EFF', IsEnabled='@CanIllustrate', IsVisible='@IsIllustrationVisible')
    button(s, 'AFPlaqueSend', '@SendText', 'ExecuteSubmit', 991, 125, 188, 41, 15, '#FFDF95FF', IsEnabled='@CanSend')
    # While a round runs the send plaque is disabled; 打断 sits on top of it.
    button(s, 'AFPlaqueInterrupt', '打断 · 插话', 'ExecuteInterrupt', 991, 125, 188, 41, 15, '#FFB08AFF', IsVisible='@CanInterrupt')
    button(s, 'AFPlateLeave', '退出交谈', 'ExecuteLeave', 1189, 125, 102, 41, 14, '#361F0EFF')
    # Pen UJB2B right docket 460x640 at (1410, 35) with larger type; rolls up into the tag below.
    d = children(anchored(chrome, 460, 640, 'Right', 'Top', MarginRight=50, MarginTop=35, IsVisible='@IsDocketOpen'))
    box(d, 0, 0, 460, 640, Sprite='afdui_audience_docket_pure_clean')
    text(d, '@AudienceTitle', 70, 110, 320, 24, 16, '#2E1A0CFF', 'Center')
    text(d, '@AudienceStats', 70, 136, 320, 20, 13, '#6E4A2AFF', 'Center')
    text(d, '点名片 = 选为对话对象 · 点印章 = 参与 / 屏蔽 / 锁定', 70, 158, 320, 36, 12, '#8C6540FF', 'Center', 'Top')
    tab(d, 'AFTabIncludeAll', '全部参与', 'ExecuteIncludeAll', 110, 196, 110, 30, size=13)
    tab(d, 'AFTabKeepLocked', '只留锁定', 'ExecuteKeepLocked', 240, 196, 110, 30, size=13)
    card = scroll(d, 'AFSessionDocket', 'Participants', 75, 234, 322, 326)
    participant_card(card, 310, 72, (6, 14, 44), (58, 6, 158, 22), (58, 28, 160, 40), (222, 19, 86, 34), 6, '#FAF3E3D9', '#B88E4FFF', (15, 12, 12))
    text(d, '滚轮查看更多', 75, 566, 310, 18, 12, '#5C3A1EFF', 'Center')
    tab(d, 'AFTabRollDocket', '卷起名录', 'ExecuteToggleDocket', 186, 588, 88, 24, size=12)
    # Pen gJg8O r77nV6: rolled docket tag at the docket's top.
    g = children(anchored(chrome, 330, 36, 'Right', 'Top', MarginRight=115, MarginTop=35, IsVisible='@IsDocketRolled'))
    button(g, 'AFCapDocketTag', '@DocketTagText', 'ExecuteToggleDocket', 0, 0, 330, 36, 14, CREAM)
    history_drawer(chrome, horizontal='Center', MarginTop=130)
    trade_panel(expanded)
    # Pen gJg8O scheme 1: floating expand handle 300x44.
    h = children(anchored(r, 300, 44, 'Center', 'Bottom', MarginBottom=26, IsVisible='@IsCollapsed'))
    button(h, 'AFCapExpand', '@CollapsedText', 'ExecuteExpand', 0, 0, 300, 44, 13, CREAM)
    save(tree)


def build_folio():
    tree = root('AFSceneSessionFolio', IsVisible='@IsVisible')
    r = tree[1]
    catch_all(r, visible='@IsExpanded')
    expanded = children(el(r, 'Widget', WidthSizePolicy='StretchToParent', HeightSizePolicy='StretchToParent', DoNotAcceptEvents='true', IsVisible='@IsExpanded'))
    chrome = children(el(expanded, 'Widget', WidthSizePolicy='StretchToParent', HeightSizePolicy='StretchToParent', DoNotAcceptEvents='true', IsVisible='@IsChromeVisible'))
    # Pen Ej55d: right console 577x1060 at (1335, 10) with larger type; children in console coordinates.
    f = children(anchored(chrome, 577, 1060, 'Right', 'Top', MarginRight=8, MarginTop=10))
    box(f, 0, 0, 577, 1060, Sprite='afdui_3tier_console_chassis_clean')
    text(f, '场景对谈手札', 25, 16, 300, 34, 18, '#EBD4A8FF')
    dark_button(f, 'AFRowFold', '收起界面', 'ExecuteCollapse', 400, 20, 112, 30, fill='#231912D9', stroke='#B88E4FFF', color='#F5E2BDFF', size=13)
    a = children(box(f, 59, 131, 460, 236))
    text(a, '@AudienceTitle', 10, 2, 230, 22, 15, '#382313FF')
    text(a, '@AudienceStats', 10, 24, 230, 18, 12, '#6E5033FF')
    tab(a, 'AFTabIncludeAll', '全部参与', 'ExecuteIncludeAll', 250, 2, 96, 26, size=12)
    tab(a, 'AFTabKeepLocked', '只留锁定', 'ExecuteKeepLocked', 354, 2, 96, 26, size=12)
    card = scroll(a, 'AFSessionAudience', 'Participants', 8, 42, 454, 194)
    participant_card(card, 444, 46, (6, 6, 34), (46, 3, 280, 22), (46, 25, 280, 18), (335, 8, 100, 30), 3, '#F5EDDEBF', '#B49164B3', (14, 12, 12))
    hs = children(box(f, 59, 400, 460, 375))
    text(hs, '—— 场景对话记录 ——', 6, 2, 448, 18, 12, '#8C6E4AFF', 'Center')
    history_row(scroll(hs, 'AFSessionStream', 'HistoryLines', 6, 24, 454, 320, auto=True))
    staged_trade(hs, 6, 348, 448, 12)
    text(f, '@AddresseeText', 73, 781, 440, 22, 14, '#E2C38AFF')
    editor(f, 59, 803, 460, 136, '#F0DFBEFF', '输入你要对周围人说的话，或点名片向特定 NPC 交谈……', '#D9C3A0AA')
    text(f, '[Enter] 发送 · [Shift+Enter] 换行 · 按受众名单同步分发', 73, 941, 440, 16, 12, '#B09572FF')
    text(f, '@StatusText', 73, 959, 440, 22, 13, '#FFB08AFF')
    for bid, label, command, x, w, extra in (('AFRowHistory', '历史', 'ExecuteToggleHistory', 39, 70, {}),
                                             ('AFRowGift', '赠送', 'ExecuteGift', 115, 70, {'IsEnabled': '@CanSend'}),
                                             ('AFRowCodex', '图鉴', 'ExecuteEncyclopedia', 191, 70, {}),
                                             ('AFRowIllustrate', '@IllustrationButtonText', 'ExecuteIllustrate', 267, 70, {'IsEnabled': '@CanIllustrate', 'IsVisible': '@IsIllustrationVisible'}),
                                             ('AFRowLeave', '离开', 'ExecuteLeave', 459, 80, {})):
        dark_button(f, bid, label, command, x, 992, w, 32, size=13, **extra)
    dark_button(f, 'AFRowSend', '@SendText', 'ExecuteSubmit', 343, 992, 110, 32, fill='#644420F2', stroke='#E5B865FF', color='#FFF0C8FF', size=13, IsEnabled='@CanSend')
    dark_button(f, 'AFRowInterrupt', '打断 · 插话', 'ExecuteInterrupt', 343, 992, 110, 32, fill='#6A2A1CF2', stroke='#E58A65FF', color='#FFE0D0FF', size=13, IsVisible='@CanInterrupt')
    history_drawer(chrome, horizontal='Left', MarginLeft=180, MarginTop=150)
    trade_panel(expanded)
    t = children(anchored(r, 90, 180, 'Right', 'Center', IsVisible='@IsCollapsed'))
    framed(t, 0, 0, 90, 180, '#2A2016EB', '#C8A365FF')
    button(t, 'AFRowExpand', None, 'ExecuteExpand', 0, 0, 90, 180)
    text(t, '展开\n对谈手札', 6, 36, 78, 60, 13, '#F5E2BDFF', 'Center')
    text(t, '(按 T)', 6, 100, 78, 20, 11, '#E5C895FF', 'Center')
    save(tree)


build_wheel()
build_scroll()
build_folio()
