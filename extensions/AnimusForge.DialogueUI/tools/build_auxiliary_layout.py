"""Build the confirmed 1280x610 auxiliary panel; preserve the dialogue console."""
from pathlib import Path
import xml.etree.ElementTree as E

ROOT = Path(__file__).resolve().parents[1]
PATH = ROOT / 'GUI/Prefabs/AFDialogueNativeOverlay.xml'
DARK = '#3B281BFF'
MUTED = '#765639FF'

def el(parent, tag, **attrs):
    return E.SubElement(parent, tag, {k: str(v) for k, v in attrs.items()})

def children(parent):
    return el(parent, 'Children')

def box(parent, x, y, w, h, **attrs):
    return el(parent, 'Widget', WidthSizePolicy='Fixed', HeightSizePolicy='Fixed',
              SuggestedWidth=w, SuggestedHeight=h, HorizontalAlignment='Left',
              VerticalAlignment='Top', MarginLeft=x, MarginTop=y,
              DoNotAcceptEvents='true', **attrs)

def text(parent, value, x, y, w, h, size=16, color=DARK, align='Left', **attrs):
    return el(parent, 'TextWidget', WidthSizePolicy='Fixed', HeightSizePolicy='Fixed',
              SuggestedWidth=w, SuggestedHeight=h, MarginLeft=x, MarginTop=y,
              HorizontalAlignment='Left', VerticalAlignment='Top', Text=value,
              Brush='Popup.Button.Text', DoNotAcceptEvents='true',
              **{'Brush.FontSize': size, 'Brush.FontColor': color,
                 'Brush.TextHorizontalAlignment': align, 'Brush.TextVerticalAlignment': 'Center'}, **attrs)

def button(parent, label, command, x, y, w, h=36, selected=None, enabled=None, **attrs):
    b = el(parent, 'ButtonWidget', WidthSizePolicy='Fixed', HeightSizePolicy='Fixed',
           SuggestedWidth=w, SuggestedHeight=h, HorizontalAlignment='Left', VerticalAlignment='Top',
           MarginLeft=x, MarginTop=y, Brush='Popup.Cancel.Button',
           DoNotAcceptEvents='false', DoNotPassEventsToChildren='true', UpdateChildrenStates='true',
           **{'Command.Click': command}, **attrs)
    if selected: b.set('IsSelected', '@'+selected)
    if enabled: b.set('IsEnabled', '@'+enabled)
    c = children(b)
    el(c, 'TextWidget', WidthSizePolicy='StretchToParent', HeightSizePolicy='StretchToParent',
       Text=label, Brush='Popup.Button.Text', DoNotAcceptEvents='true',
       **{'Brush.FontSize':'15', 'Brush.FontColor':DARK, 'Brush.TextHorizontalAlignment':'Center',
          'Brush.TextVerticalAlignment':'Center'})
    return b

def editable(c, binding, maxlen, align, color, left, right, size):
    return el(c, 'AnimusForgeNativeConversationEditableTextWidget',
        WidthSizePolicy='StretchToParent', HeightSizePolicy='StretchToParent', MarginLeft=left, MarginRight=right,
        Brush='Popup.Button.Text', RealText='@'+binding, MaxLength=maxlen, AutoFocus='false',
        ClipContents='true', **{'Brush.FontSize':size, 'Brush.FontColor':color,
        'Brush.TextHorizontalAlignment':align, 'Brush.TextVerticalAlignment':'Center',
        'Command.FocusGained':'StartTyping', 'Command.FocusLost':'StopTyping'})

SOLID = 'BlankWhiteSquare_9'

def rect(parent, x, y, w, h, color, **attrs):
    return box(parent, x, y, w, h, Sprite=SOLID, Color=color, **attrs)

def framed(parent, x, y, w, h, fill, stroke):
    # Translucent Pen fill + 1px stroke; edges drawn separately so the stroke never shows through.
    c = children(box(parent, x, y, w, h))
    rect(c, 1, 1, w-2, h-2, fill)
    for ex, ey, ew, eh in ((0,0,w,1), (0,h-1,w,1), (0,1,1,h-2), (w-1,1,1,h-2)):
        rect(c, ex, ey, ew, eh, stroke)
    return c

def search_box(parent, x, y, w, h, fill, stroke, icon, icon_x, hint, hint_color, hint_size, right_text=None, right_w=0):
    # Pen search bar: icon, placeholder shown only while empty, optional right-aligned count.
    c = framed(parent, x, y, w, h, fill, stroke)
    box(c, icon_x, (h-icon)//2, icon, icon, Sprite='afdui_icon_search')
    right = 10 + right_w + (10 if right_w else 0)
    text(c, hint, 36, 0, w-36-right, h, hint_size, hint_color, IsVisible='@IsSearchEmpty')
    if right_text: text(c, right_text, w-10-right_w, 0, right_w, h, 13, '#8A6944FF', 'Right')
    editable(c, 'SearchText', 80, 'Left', DARK, 36, right, 15)
    return c

def scroll(parent, name, source, x,y,w,h, history=False):
    host=children(box(parent,x,y,w,h))
    attrs={'AutoScrollRequestVersion':'@AutoScrollRequestVersion',
           'AutoScrollTopRequestVersion':'@AutoScrollTopRequestVersion'} if history else {}
    s=el(host, 'AnimusForgeConversationHistoryAutoScrollPanel' if history else 'ScrollablePanel',
         WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',MarginRight='12',
         ClipRect=name+'Clip',InnerPanel=name+'Clip'+chr(92)+name+'List',MouseScrollAxis='Vertical',
         VerticalScrollbar='..'+chr(92)+name+'Bar',AutoHideScrollBars='true', **attrs)
    clip=el(children(s),'Widget',Id=name+'Clip',WidthSizePolicy='StretchToParent',
            HeightSizePolicy='StretchToParent',ClipContents='true')
    lst=el(children(clip),'ListPanel',Id=name+'List',DataSource='{'+source+'}',
           WidthSizePolicy='StretchToParent',HeightSizePolicy='CoverChildren',
           **{'StackLayout.LayoutMethod':'VerticalTopToBottom'})
    bar=el(host,'ScrollbarWidget',Id=name+'Bar',WidthSizePolicy='Fixed',SuggestedWidth='5',
           HeightSizePolicy='StretchToParent',HorizontalAlignment='Right',AlignmentAxis='Vertical',
           Handle=name+'Handle',MinValue='0',MaxValue='100')
    el(children(bar),'Widget',Id=name+'Handle',WidthSizePolicy='StretchToParent',
       HeightSizePolicy='Fixed',SuggestedHeight='26',Sprite='afdui_scroll_handle')
    return el(lst,'ItemTemplate')

tree=E.parse(PATH)
root=tree.getroot().find('./Window/Widget/Children')
for node in list(root):
    if node.get('Id') == 'AFDialogueAuxiliaryPanel': root.remove(node)
toolbar=tree.getroot().find('.//*[@Id="AFDialogueToolbar"]')
toolbar.set('IsVisible','@IsToolbarVisible')
tree.getroot().find('.//DevMultilineEditableTextWidget').set('Id','AFDialogueInputEditor')
panel=el(root,'Widget',Id='AFDialogueAuxiliaryPanel',DataSource='{Auxiliary}',
         WidthSizePolicy='Fixed',SuggestedWidth='1280',HeightSizePolicy='Fixed',SuggestedHeight='610',
         HorizontalAlignment='Center',VerticalAlignment='Top',MarginTop='126',Sprite='afdui_aux_panel',
         IsVisible='@IsVisible',DoNotAcceptEvents='false',DoNotPassEventsToChildren='false')
p=children(panel)
text(p,'@Title',88,100,230,30,22)
text(p,'@TargetName',88,130,360,22,14,MUTED)
button(p,'对话历史','ShowHistory',656,108,148,36,'IsHistory','CanInteract',Id='AFAuxHistoryTab')
button(p,'给予 / 展示','ShowTrade',814,108,172,36,'IsTrade','CanInteract',Id='AFAuxTradeTab')
button(p,'收起面板','Close',1056,104,136,40,enabled='CanInteract',Id='AFAuxClose')

hv=children(box(p,0,0,1280,610,IsVisible='@IsHistory'))
day=scroll(hv,'AFAuxDays','HistoryDays',88,160,184,324)
button(day,'@Label','ExecuteSelect',0,0,166,40,'IsSelected')
button(hv,'全部','FilterAll',296,160,76,36,'IsAllFilter','CanInteract')
button(hv,'对话','FilterDialogue',380,160,76,36,'IsDialogueFilter','CanInteract')
button(hv,'行动','FilterActions',464,160,76,36,'IsActionFilter','CanInteract')
search_box(hv,882,162,310,32,'#FFF6DE55','#94704766',15,12,'搜索这位人物的记录','#967954FF',13)
hist=children(box(hv,296,204,896,332,DataSource='{History}'))
items=scroll(hist,'AFAuxHistory','Items',0,0,896,280,True)
cell=children(el(items,'Widget',WidthSizePolicy='StretchToParent',HeightSizePolicy='CoverChildren',
                 MarginBottom='10',DoNotAcceptEvents='true'))
row=el(cell,'ListPanel',WidthSizePolicy='StretchToParent',HeightSizePolicy='CoverChildren',
       DoNotAcceptEvents='true',**{'StackLayout.LayoutMethod':'VerticalTopToBottom'})
# Two-click delete on the time row (right-aligned, same 22px band), so it never covers the message text.
db=button(cell,'@DeleteText','ExecuteDelete',0,0,84,22,'IsDeleteArmed',IsVisible='@CanDelete')
db.set('HorizontalAlignment','Right')
rc=children(row)
el(rc,'TextWidget',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='22',
   Text='@ChatItemTime',Brush='Popup.Description.Text',DoNotAcceptEvents='true',
   **{'Brush.FontSize':'13','Brush.FontColor':MUTED})
el(rc,'RichTextWidget',WidthSizePolicy='StretchToParent',HeightSizePolicy='CoverChildren',
   Brush='Conversation.ConversedPartyLine.Text',Text='@ChatText',CanBreakWords='true',
   DoNotAcceptEvents='false',**{'Brush.FontSize':'17','Brush.FontColor':DARK,
   'Brush.TextHorizontalAlignment':'Left','Brush.TextVerticalAlignment':'Top',
   'Command.LinkClick':'ExecuteOpenEncyclopediaLink','Command.LinkAlternateClick':'ExecuteOpenEncyclopediaLink'})
button(hist,'更早记录','LoadOlderPage',0,294,108,38,enabled='CanLoadOlderPage')
text(hist,'@PageStatusText',116,294,540,38,14,MUTED,'Center')
button(hist,'更新记录','LoadNewerPage',666,294,108,38,enabled='CanLoadNewerPage')
button(hv,'回到最新','LatestHistory',1080,498,112,38,enabled='CanInteract')
text(hv,'按日期查看',88,498,184,38,14,MUTED,'Center')

tv=children(box(p,0,0,1280,610,IsVisible='@IsTrade'))
for x,label,cmd,sel in [(88,'物品 / 第纳尔','SelectItems','IsItems'),(276,'部队','SelectTroops','IsTroops'),
                         (464,'俘虏','SelectPrisoners','IsPrisoners'),(652,'固定资产','SelectAssets','IsAssets')]:
    button(tv,label,cmd,x,160,180,36,sel,'CanInteract')
button(tv,'给予','SelectGive',868,160,158,36,'IsGive','CanInteract')
button(tv,'展示','SelectShow',1034,160,158,36,'IsShow','CanShow')
search_box(tv,88,204,752,34,'#FFF7E24D','#AC895566',16,10,'搜索物品或第纳尔…','#947451FF',14,'@ResourceCountText',120)
HEAD = '#8B693FFF'
# Click anywhere on a row to select it; no separate checkbox column. 持有 is widened so
# eight-digit denar totals stay on one line.
for label,x,w,align in [('资源',100,460,'Left'),('持有',618,120,'Center'),('估值',738,95,'Center')]:
    text(tv,label,x,238,w,26,13,HEAD,align)
# Scrollbar sits in the gutter between the 752px list and the selection panel, as in Pen.
ri=scroll(tv,'AFAuxResources','ResourceItems',88,264,766,220)
rb=el(ri,'ButtonWidget',Id='AFAuxRow',WidthSizePolicy='Fixed',HeightSizePolicy='Fixed',SuggestedWidth='752',SuggestedHeight='44',
      HorizontalAlignment='Left',VerticalAlignment='Top',DoNotAcceptEvents='false',DoNotPassEventsToChildren='true',
      UpdateChildrenStates='true',IsSelected='@IsSelected',IsEnabled='@CanSelect',**{'Command.Click':'Toggle'})
r=children(rb)
rect(r,0,43,752,1,'#98764633')
el(r,'ImageIdentifierWidget',WidthSizePolicy='Fixed',HeightSizePolicy='Fixed',SuggestedWidth='34',SuggestedHeight='26',
   HorizontalAlignment='Left',VerticalAlignment='Top',MarginLeft='8',MarginTop='9',DoNotAcceptEvents='true',
   ImageId='@ImageId',AdditionalArgs='@ImageArgs',TextureProviderName='@ImageProvider',IsVisible='@HasImage')
box(r,12,10,24,24,Sprite='afdui_icon_coin',IsVisible='@IsGold')
text(r,'@Name',48,4,472,20,15,'#412D1DFF')
text(r,'@Category',48,24,472,16,11,'#987750FF')
text(r,'@AvailableText',530,0,120,44,14,'#64482DFF','Center')
text(r,'@ValueText',650,0,95,44,14,'#64482DFF','Center')
text(tv,'没有符合条件的资源',88,330,752,52,16,MUTED,'Center',IsVisible='@IsResourceEmpty')
text(tv,'@SelectionTitle',868,204,216,30,17)
button(tv,'清空','ClearSelection',1124,204,68,30,enabled='CanInteract')
si=scroll(tv,'AFAuxSelection','SelectedItems',868,238,324,206)
s=children(box(si,0,0,306,76))
text(s,'@Name',0,0,264,27,15)
button(s,'×','Remove',274,0,30,28)
button(s,'−','Decrement',0,33,32,32)
# Pen quantity field: #25190F fill, 1px #A78145 stroke, centered #EBD5A7 16px value.
q=children(rect(s,38,33,130,32,'#A78145FF'))
editable(children(rect(q,1,1,128,30,'#25190FFF')),'Quantity',10,'Center','@QuantityColor',6,6,16)
button(s,'+','Increment',174,33,32,32)
button(s,'全部','SelectAll',214,33,90,32)
text(tv,'从左侧选择资源',884,294,276,52,15,MUTED,'Center',IsVisible='@IsSelectionEmpty')
text(tv,'估值合计',868,452,94,32,14,MUTED)
text(tv,'@TotalValue',968,452,224,32,16,DARK,'Right')
button(tv,'上一页','PreviousResources',88,498,90,38,enabled='CanResourcePrevious')
text(tv,'@ResourcePageText',184,498,188,38,13,MUTED,'Center')
button(tv,'下一页','NextResources',378,498,90,38,enabled='CanResourceNext')
text(tv,'@TradeNote',486,493,354,46,13,MUTED)
button(tv,'@ConfirmText','Confirm',868,498,324,38,enabled='CanSubmit',Id='AFAuxConfirm')
text(p,'@Status',88,542,1104,26,13,'#7B3D2FFF','Center')

E.indent(tree,space='  ')
tree.write(PATH,encoding='utf-8',xml_declaration=False)
print('Auxiliary panel written:',PATH)
