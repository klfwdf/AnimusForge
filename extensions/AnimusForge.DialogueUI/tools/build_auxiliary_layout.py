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

def input_box(parent, binding, x, y, w, h=32, maxlen=80, align='Left', color=DARK):
    c = children(box(parent, x,y,w,h, Sprite='afdui_input_panel'))
    return el(c, 'AnimusForgeNativeConversationEditableTextWidget',
        WidthSizePolicy='StretchToParent', HeightSizePolicy='StretchToParent', MarginLeft='8',MarginRight='8',
        Brush='Popup.Button.Text', RealText='@'+binding, MaxLength=maxlen, AutoFocus='false',
        ClipContents='true', **{'Brush.FontSize':'15', 'Brush.FontColor':color,
        'Brush.TextHorizontalAlignment':align, 'Brush.TextVerticalAlignment':'Center',
        'Command.FocusGained':'StartTyping', 'Command.FocusLost':'StopTyping'})

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
text(hv,'搜索记录',564,160,78,36,14,MUTED)
input_box(hv,'SearchText',650,162,542)
hist=children(box(hv,296,204,896,332,DataSource='{History}'))
items=scroll(hist,'AFAuxHistory','Items',0,0,896,280,True)
row=el(items,'ListPanel',WidthSizePolicy='StretchToParent',HeightSizePolicy='CoverChildren',
       MarginBottom='10',**{'StackLayout.LayoutMethod':'VerticalTopToBottom'})
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
text(tv,'资源清单',88,204,108,32,16)
text(tv,'名称 / 类型 / 可用 / 单价',204,204,220,32,13,MUTED)
input_box(tv,'SearchText',450,204,382)
ri=scroll(tv,'AFAuxResources','ResourceItems',88,244,752,240)
r=children(box(ri,0,0,734,48))
button(r,'@SelectText','Toggle',0,7,58,34,'IsSelected','CanSelect')
text(r,'@Name',68,0,365,27,16)
text(r,'@Category',68,26,365,20,13,MUTED)
text(r,'@AvailableText',442,0,128,48,15,MUTED,'Center')
text(r,'@ValueText',574,0,158,48,15,MUTED,'Right')
text(tv,'没有符合条件的资源',120,302,670,52,16,MUTED,'Center',IsVisible='@IsResourceEmpty')
text(tv,'@SelectionTitle',868,204,216,30,17)
button(tv,'清空','ClearSelection',1124,204,68,30,enabled='CanInteract')
si=scroll(tv,'AFAuxSelection','SelectedItems',868,238,324,206)
s=children(box(si,0,0,306,76))
text(s,'@Name',0,0,264,27,15)
button(s,'×','Remove',274,0,30,28)
button(s,'−','Decrement',0,33,32,32)
input_box(s,'Quantity',38,33,130,32,10,'Center','@QuantityColor')
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
