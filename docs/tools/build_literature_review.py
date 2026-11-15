from pathlib import Path
import json
import re
import html
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.lib import colors
from reportlab.lib.pagesizes import A4
from reportlab.lib.styles import getSampleStyleSheet, ParagraphStyle
from reportlab.lib.enums import TA_CENTER, TA_JUSTIFY, TA_LEFT
from reportlab.platypus import (
    BaseDocTemplate, PageTemplate, Frame, Paragraph, Spacer, PageBreak,
    CondPageBreak, Table, TableStyle, KeepTogether, Image,
)
from reportlab.platypus.tableofcontents import TableOfContents
from reportlab.graphics.shapes import Drawing, Rect, String, Line, Polygon

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / 'docs/LITERATURE_AND_TECHNOLOGY_REVIEW.md'
OUTPUT = ROOT / 'docs/LITERATURE_AND_TECHNOLOGY_REVIEW.pdf'
META = json.loads((ROOT / 'docs/report-metadata.json').read_text(encoding='utf-8'))

for name, font in [('TimesNew', 'times.ttf'), ('TimesNew-Bold', 'timesbd.ttf'),
                   ('TimesNew-Italic', 'timesi.ttf'), ('TimesNew-BoldItalic', 'timesbi.ttf'),
                   ('Calibri', 'calibri.ttf'), ('Calibri-Bold', 'calibrib.ttf')]:
    pdfmetrics.registerFont(TTFont(name, str(Path('C:/Windows/Fonts') / font)))
pdfmetrics.registerFontFamily('TimesNew', normal='TimesNew', bold='TimesNew-Bold',
                             italic='TimesNew-Italic', boldItalic='TimesNew-BoldItalic')
pdfmetrics.registerFontFamily('Calibri', normal='Calibri', bold='Calibri-Bold',
                             italic='Calibri', boldItalic='Calibri-Bold')

INK = colors.HexColor('#18212d')
BLUE = colors.HexColor('#244a71')
MUTED = colors.HexColor('#596675')
GRID = colors.HexColor('#c5ccd4')
PAGE_W, PAGE_H = A4
MARGIN = 52
WIDTH = PAGE_W - 2 * MARGIN
styles = getSampleStyleSheet()
styles.add(ParagraphStyle('ReviewBody', fontName='TimesNew', fontSize=11.5, leading=16,
    alignment=TA_JUSTIFY, textColor=colors.black, spaceAfter=8,
    allowWidows=0, allowOrphans=0, splitLongWords=1))
styles.add(ParagraphStyle('ReviewH1', fontName='TimesNew-Bold', fontSize=16, leading=20,
    spaceBefore=17, spaceAfter=10, keepWithNext=True, textColor=colors.black))
styles.add(ParagraphStyle('ReviewH2', fontName='TimesNew-Bold', fontSize=12.5, leading=17,
    spaceBefore=12, spaceAfter=7, keepWithNext=True, textColor=colors.black))
styles.add(ParagraphStyle('ReviewCell', fontName='TimesNew', fontSize=9.1, leading=12,
    spaceAfter=0, splitLongWords=1, textColor=colors.black))
styles.add(ParagraphStyle('ReviewCellHead', parent=styles['ReviewCell'], fontName='Calibri-Bold',
    fontSize=9.2, leading=12, textColor=INK))
styles.add(ParagraphStyle('ReviewCaption', fontName='TimesNew-Italic', fontSize=10,
    leading=13, spaceBefore=8, spaceAfter=7, keepWithNext=True))
styles.add(ParagraphStyle('ReviewFigureCaption', parent=styles['ReviewCaption'],
    keepWithNext=False, spaceAfter=10))
styles.add(ParagraphStyle('ReviewRef', fontName='TimesNew', fontSize=10, leading=13.5,
    spaceAfter=9, allowWidows=0, allowOrphans=0, splitLongWords=1))
styles.add(ParagraphStyle('ReviewBullet', parent=styles['ReviewBody'], leftIndent=14,
    firstLineIndent=0, bulletIndent=0, spaceAfter=5))
styles.add(ParagraphStyle('ReviewCoverTitle', fontName='TimesNew-Bold', fontSize=28,
    leading=35, alignment=TA_CENTER, spaceAfter=24))
styles.add(ParagraphStyle('ReviewCoverSub', fontName='TimesNew', fontSize=16,
    leading=23, alignment=TA_CENTER, spaceAfter=17))
styles.add(ParagraphStyle('ReviewCoverInfo', fontName='TimesNew', fontSize=12,
    leading=19, alignment=TA_CENTER, spaceAfter=12))
styles.add(ParagraphStyle('UniversityHeading', fontName='TimesNew-Bold', fontSize=14,
    leading=20, alignment=TA_CENTER, spaceAfter=4))
styles.add(ParagraphStyle('CoverDetail', fontName='TimesNew', fontSize=11.5,
    leading=16, alignment=TA_CENTER, spaceAfter=5))
styles.add(ParagraphStyle('CoverProject', fontName='TimesNew-Bold', fontSize=18,
    leading=24, alignment=TA_CENTER, spaceAfter=4))
styles.add(ParagraphStyle('CoverReport', fontName='TimesNew-Bold', fontSize=20,
    leading=27, alignment=TA_CENTER, spaceBefore=9, spaceAfter=9))
styles.add(ParagraphStyle('CoverMember', fontName='TimesNew', fontSize=11.5,
    leading=17, spaceAfter=0))
styles.add(ParagraphStyle('CoverRepository', fontName='TimesNew', fontSize=8.7,
    leading=12, alignment=TA_CENTER, spaceAfter=0))
styles.add(ParagraphStyle('ReviewTOC0', fontName='TimesNew-Bold', fontSize=10.3,
    leading=14, leftIndent=0, firstLineIndent=0, spaceBefore=5))
styles.add(ParagraphStyle('ReviewTOC1', fontName='TimesNew', fontSize=9.7,
    leading=13, leftIndent=14, firstLineIndent=0, spaceBefore=1))

def normalize(s):
    return s.translate(str.maketrans({'—':'-', '–':'-', '‑':'-', '\u00a0':' ',
        '“':'"', '”':'"', '’':"'", '‘':"'"}))

def inline(s):
    s = normalize(s)
    saved = []
    def protect(value):
        saved.append(value)
        return f'ZLINKTOKEN{len(saved)-1}Z'
    s = re.sub(r'\[\[(\d+)\]\]\(#ref-\d+\)',
        lambda m: protect(f'<link href="#ref-{m[1]}" color="#244a71">[{m[1]}]</link>'), s)
    s = re.sub(r'\[([^\]]+)\]\(([^)]+)\)',
        lambda m: protect('<link href="'+html.escape(m[2], quote=True)+'" color="#244a71">'+html.escape(m[1])+'</link>'), s)
    s = html.escape(s)
    s = re.sub(r'\*\*([^*]+)\*\*', r'<b>\1</b>', s)
    s = re.sub(r'\*([^*]+)\*', r'<i>\1</i>', s)
    s = re.sub(r'`([^`]+)`', r'<font name="Calibri">\1</font>', s)
    for i, v in enumerate(saved):
        s = s.replace(f'ZLINKTOKEN{i}Z', v)
    return s

def slug(s):
    return re.sub(r'\s+', '-', re.sub(r'[^\w\- ]', '', s.lower()))

class ReviewDoc(BaseDocTemplate):
    def __init__(self, filename):
        super().__init__(filename, pagesize=A4, leftMargin=MARGIN, rightMargin=MARGIN,
            topMargin=46, bottomMargin=46, title='Interactive VR Simulation - Literature and Technology Review',
            author='; '.join(m['name'] for m in META['members']), subject='CENG 407 Literature and Technology Review')
        frame = Frame(MARGIN, 46, WIDTH, PAGE_H-92, leftPadding=0, rightPadding=0,
                      topPadding=0, bottomPadding=0, id='body')
        self.addPageTemplates(PageTemplate(id='Review', frames=[frame], onPage=self.decorate))
    def decorate(self, canvas, doc):
        if doc.page > 1:
            canvas.saveState()
            canvas.setFont('Calibri', 8)
            canvas.setFillColor(MUTED)
            canvas.drawString(MARGIN, PAGE_H-28, 'WORKPLACE STUDIO  |  LITERATURE AND TECHNOLOGY REVIEW')
            canvas.drawRightString(PAGE_W-MARGIN, 25, str(doc.page))
            canvas.restoreState()
    def afterFlowable(self, flowable):
        if hasattr(flowable, 'toc_key'):
            self.canv.bookmarkPage(flowable.toc_key)
            self.canv.addOutlineEntry(flowable.getPlainText(), flowable.toc_key,
                                     level=flowable.toc_level, closed=False)
            self.notify('TOCEntry',(flowable.toc_level,flowable.getPlainText(),self.page,flowable.toc_key))

def heading(text, level=0):
    p=Paragraph(inline(text), styles['ReviewH1' if level==0 else 'ReviewH2'])
    p.toc_key=slug(text); p.toc_level=level
    return p

def diagram():
    d=Drawing(WIDTH, 460)
    def box(x,y,w,h,lines, fill='#f1f4f7', bold=False):
        d.add(Rect(x,y,w,h,rx=4,ry=4,strokeColor=GRID,fillColor=colors.HexColor(fill),strokeWidth=.7))
        ys=y+h/2+(len(lines)-1)*6
        for i,t in enumerate(lines):
            d.add(String(x+w/2,ys-i*12-3,t,fontName='Calibri-Bold' if bold else 'Calibri',
                         fontSize=10,textAnchor='middle',fillColor=INK))
    def arrow(x1,y1,x2,y2):
        d.add(Line(x1,y1,x2,y2,strokeColor=MUTED,strokeWidth=.8))
        import math
        a=math.atan2(y2-y1,x2-x1); l=5
        pts=[x2,y2,x2-l*math.cos(a-.45),y2-l*math.sin(a-.45),x2-l*math.cos(a+.45),y2-l*math.sin(a+.45)]
        d.add(Polygon(pts,fillColor=MUTED,strokeColor=MUTED))
    mid=WIDTH/2
    box(mid-126,421,252,34,['Instructor photographs or video'],bold=True)
    arrow(mid,421,mid,405)
    box(mid-126,371,252,34,['Capture review and processing policy'],fill='#e8eff5')
    xs=[0,169,338];w=153
    for x,txt in zip(xs,[['Local workstation AI'],['Institution-controlled','AI server'],['Approved internet','AI service']]):
        arrow(mid,371,x+w/2,347);box(x,305,w,42,txt,fill='#edf3f7')
        arrow(x+w/2,305,mid,285)
    box(mid-125,253,250,32,['Reconstruction result'])
    arrow(mid,253,mid,236)
    box(mid-125,199,250,37,['Mesh preparation and','equipment review'])
    box(0,199,103,37,['Existing licensed','GLB assets'])
    arrow(103,217,mid-125,217)
    arrow(mid,199,mid,184)
    box(mid-175,149,350,35,['Instructor authoring: hotspots, tasks, feedback'],fill='#e8eff5')
    arrow(mid,149,mid,135)
    box(mid-125,103,250,32,['Versioned training package'])
    arrow(mid-50,103,112,84);arrow(mid+50,103,379,84)
    box(32,53,160,31,['Desktop learning'])
    box(299,53,160,31,['VR learning'])
    arrow(112,53,mid,37);arrow(379,53,mid,37)
    box(mid-125,5,250,32,['Task events and evaluation'],fill='#e8eff5')
    return d

def make_table(lines):
    rows=[[x.strip() for x in line.strip().strip('|').split('|')] for line in lines]
    rows=[rows[0]]+rows[2:]
    if len(rows[0])==3: fractions=[.23,.40,.37]
    elif rows[0][0]=='Profile':fractions=[.17,.28,.24,.31]
    elif rows[0][0] in ('Approach','Option','Work'):fractions=[.21,.27,.24,.28]
    else:fractions=[.20,.29,.22,.29]
    data=[[Paragraph(inline(cell),styles['ReviewCellHead' if i==0 else 'ReviewCell'])
           for cell in row] for i,row in enumerate(rows)]
    table=Table(data,colWidths=[WIDTH*f for f in fractions],repeatRows=1,hAlign='LEFT',
                spaceBefore=3,spaceAfter=10)
    table.setStyle(TableStyle([
        ('VALIGN',(0,0),(-1,-1),'TOP'),('BACKGROUND',(0,0),(-1,0),colors.HexColor('#e8edf2')),
        ('ROWBACKGROUNDS',(0,1),(-1,-1),[colors.white,colors.HexColor('#f7f8fa')]),
        ('LINEBELOW',(0,0),(-1,0),.7,GRID),('LINEBELOW',(0,-1),(-1,-1),.5,GRID),
        ('LEFTPADDING',(0,0),(-1,-1),7),('RIGHTPADDING',(0,0),(-1,-1),7),
        ('TOPPADDING',(0,0),(-1,-1),7),('BOTTOMPADDING',(0,0),(-1,-1),7),
        ('LINEBEFORE',(1,0),(-1,-1),.25,GRID),
    ]))
    return table

text=SOURCE.read_text(encoding='utf-8')
assert META['repositoryUrl'] in text, 'Repository metadata differs from Markdown'
for member in META['members']:
    assert member['number'] in text and member['name'] in text, 'Team metadata differs from Markdown'
members=Table([[Paragraph(html.escape(m['name']),styles['CoverMember']),
                Paragraph(m['number'],styles['CoverMember'])] for m in META['members']],
              colWidths=[242,90],hAlign='CENTER')
members.setStyle(TableStyle([('TOPPADDING',(0,0),(-1,-1),1),('BOTTOMPADDING',(0,0),(-1,-1),1),
    ('LEFTPADDING',(0,0),(-1,-1),0),('RIGHTPADDING',(0,0),(-1,-1),0)]))
story=[Spacer(1,10)]
for field in ['university','faculty','department']:
    story.append(Paragraph(html.escape(META[field]),styles['UniversityHeading']))
story.extend([Spacer(1,12),Image(str(ROOT/'docs/assets/cankaya-university-logo.png'),width=82,height=82,hAlign='CENTER'),
       Spacer(1,16),Paragraph(html.escape(META['course']),styles['CoverDetail']),
       Paragraph(META['academicYear'],styles['CoverDetail']),Spacer(1,17),
       Paragraph(META['project'],styles['CoverProject']),Paragraph(META['product'],styles['CoverDetail']),
       Paragraph(META['report'],styles['CoverReport']),
       Paragraph(META['subtitle'],styles['CoverDetail']),Spacer(1,18),
       Paragraph('<b>Project Team</b>',styles['CoverDetail']),Spacer(1,5),members,Spacer(1,16),
       Paragraph('Supervisor: '+html.escape(META['supervisor']),styles['CoverDetail']),Spacer(1,9),
       Paragraph(META['date'],styles['CoverDetail']),Spacer(1,17),
       Paragraph('<link href="'+META['repositoryUrl']+'" color="#244a71">'+
                 META['repository'].replace('/',' /<br/>',1)+'</link>',styles['CoverRepository']),
       PageBreak(),Paragraph('Contents',styles['ReviewH1'])])
toc=TableOfContents();toc.levelStyles=[styles['ReviewTOC0'],styles['ReviewTOC1']]
toc.tableStyle=TableStyle([('LEFTPADDING',(0,0),(-1,-1),0),('RIGHTPADDING',(0,0),(-1,-1),0),
    ('TOPPADDING',(0,0),(-1,-1),0),('BOTTOMPADDING',(0,0),(-1,-1),0)])
toc.dotsMinLevel=0;story.extend([toc,PageBreak()])
body=text.split('## Abstract\n',1)[1]
lines=('## Abstract\n'+body).splitlines()
i=0;references=False;pending_anchor=None;tables=0
while i<len(lines):
    line=lines[i].strip()
    if not line:i+=1;continue
    if line.startswith('```mermaid'):
        while i<len(lines) and (not lines[i].startswith('```') or lines[i].startswith('```mermaid')):i+=1
        i+=1;story.extend([CondPageBreak(510),diagram()]);continue
    if line.startswith('<a id='):
        pending_anchor=re.search(r'id="([^"]+)"',line)[1];i+=1;continue
    if line.startswith('## '):
        label=line[3:]
        if label=='References':references=True;story.append(PageBreak())
        else:story.append(CondPageBreak(95))
        story.append(heading(label));i+=1;continue
    if line.startswith('### '):
        story.extend([CondPageBreak(75),heading(line[4:],1)]);i+=1;continue
    if line.startswith('**Table '):
        if not (story and hasattr(story[-1],'toc_key')):story.append(CondPageBreak(140))
        story.append(Paragraph(inline(line),styles['ReviewCaption']));i+=1;continue
    if line.startswith('|'):
        table_lines=[]
        while i<len(lines) and lines[i].strip().startswith('|'):table_lines.append(lines[i]);i+=1
        tables+=1;story.append(make_table(table_lines));continue
    if line.startswith('*Figure '):
        story.append(Paragraph(inline(line),styles['ReviewFigureCaption']));i+=1;continue
    if line.startswith('- ') or re.match(r'^\d+\.\s',line):
        if line.startswith('- '):bullet='\u2022';value=line[2:]
        else:bullet,value=line.split(' ',1)
        story.append(Paragraph(inline(value),styles['ReviewBullet'],bulletText=bullet));i+=1;continue
    paragraph=[line];i+=1
    while i<len(lines) and lines[i].strip() and not re.match(r'^(?:#|\||```|<a id=|- |\d+\. )',lines[i].strip()):
        paragraph.append(lines[i].strip());i+=1
    value=inline(' '.join(paragraph))
    if pending_anchor:value=f'<a name="{pending_anchor}"/>'+value;pending_anchor=None
    p=Paragraph(value,styles['ReviewRef' if references else 'ReviewBody'])
    if not references and value.endswith(':'):p.keepWithNext=True
    story.append(p)

OUTPUT.parent.mkdir(parents=True,exist_ok=True)
ReviewDoc(str(OUTPUT)).multiBuild(story)
print(f'PDF: {OUTPUT}')
print(f'Tables: {tables}; vector diagrams: 1; references: 31')
