#!/usr/bin/env python3
"""Сборка пояснительной записки: node -> docx, подстановка оглавления, подсчёт страниц через LibreOffice."""
import json, os, re, shutil, subprocess, sys, zipfile, tempfile
from xml.sax.saxutils import escape

HERE = os.path.dirname(os.path.abspath(__file__))
OUT_DOCX = sys.argv[1] if len(sys.argv) > 1 else '/tmp/kr.docx'
WORK = tempfile.mkdtemp(prefix='krbuild_')
FONT = 'Times New Roman'

def run(cmd, **kw):
    return subprocess.run(cmd, check=True, capture_output=True, text=True, **kw)

def toc_xml(entries):
    """Поле TOC с готовыми строками (Word обновит его по F9)."""
    paras = []
    n = len(entries)
    for i, (lvl, text, page) in enumerate(entries):
        pre = ''
        if i == 0:
            pre = ('<w:r><w:fldChar w:fldCharType="begin"/></w:r>'
                   '<w:r><w:instrText xml:space="preserve"> TOC \\o "1-3" \\h \\z \\u </w:instrText></w:r>'
                   '<w:r><w:fldChar w:fldCharType="separate"/></w:r>')
        post = '<w:r><w:fldChar w:fldCharType="end"/></w:r>' if i == n - 1 else ''
        rpr = '<w:rPr><w:rFonts w:ascii="%s" w:hAnsi="%s" w:cs="%s"/><w:sz w:val="28"/></w:rPr>' % (FONT, FONT, FONT)
        paras.append(
            '<w:p><w:pPr><w:pStyle w:val="TOC%d"/></w:pPr>%s'
            '<w:r>%s<w:t xml:space="preserve">%s</w:t></w:r>'
            '<w:r>%s<w:tab/></w:r><w:r>%s<w:t>%s</w:t></w:r>%s</w:p>'
            % (lvl, pre, rpr, escape(text), rpr, rpr, page, post))
    return ''.join(paras)

def inject(src, dst, entries):
    with zipfile.ZipFile(src) as z:
        items = {n: z.read(n) for n in z.namelist()}
    xml = items['word/document.xml'].decode('utf8')
    m = re.search(r'<w:p>(?:(?!</w:p>).)*?@@TOC@@.*?</w:p>', xml, re.S)
    assert m, 'нет метки оглавления'
    xml = xml[:m.start()] + toc_xml(entries) + xml[m.end():]
    items['word/document.xml'] = xml.encode('utf8')
    with zipfile.ZipFile(dst, 'w', zipfile.ZIP_DEFLATED) as z:
        for n, b in items.items():
            z.writestr(n, b)

def pdf_pages(docx):
    outdir = os.path.join(WORK, 'pdf'); os.makedirs(outdir, exist_ok=True)
    run(['soffice', '--headless', '--convert-to', 'pdf', '--outdir', outdir, docx])
    pdf = os.path.join(outdir, os.path.splitext(os.path.basename(docx))[0] + '.pdf')
    n = int(re.search(r'Pages:\s+(\d+)', run(['pdfinfo', pdf]).stdout).group(1))
    pages = []
    for i in range(1, n + 1):
        t = run(['pdftotext', '-f', str(i), '-l', str(i), '-layout', pdf, '-']).stdout
        pages.append([re.sub(r'\s+', ' ', l).strip() for l in t.split('\n') if l.strip()])
    return pdf, pages

def locate(headings, pages):
    res, cur = [], 0
    for h in headings:
        key = re.sub(r'\s+', ' ', h.get('search', h['text']))[:32]
        found = None
        for p in range(cur, len(pages)):
            for l in pages[p]:
                if l.startswith(key) and not re.search(r'\.{3,}\s*\d+$', l):
                    found = p; break
            if found is not None: break
        if found is None:
            raise Exception('заголовок не найден: ' + h['text'])
        cur = found
        res.append(found + 1)
    return res

meta = {'main': 34, 'app': 2, 'pages': 36, 'figs': 16, 'tabs': 14, 'sources': 7, 'tests': 40}
tests_txt = open(os.path.join(HERE, '..', 'test-output.txt'), encoding='utf8').read()
meta['tests'] = len([l for l in tests_txt.split('\n') if l.startswith('[ OK ]') or l.startswith('[FAIL]')])
numbers = None
for attempt in range(6):
    mpath = os.path.join(WORK, 'meta.json'); json.dump(meta, open(mpath, 'w'))
    raw_docx = os.path.join(WORK, 'raw.docx'); hpath = os.path.join(WORK, 'h.json')
    run(['node', os.path.join(HERE, 'main.js'), mpath, raw_docx, hpath], cwd=HERE)
    info = json.load(open(hpath))
    heads = info['headings']
    start = [i for i, h in enumerate(heads) if h['text'] == 'Содержание'][0] + 1
    toc_heads = heads[start:]
    entries = [(h['level'], h.get('text'), (numbers[i] if numbers else 0)) for i, h in enumerate(toc_heads)]
    cand = os.path.join(WORK, 'cand.docx'); inject(raw_docx, cand, entries)
    pdf, pages = pdf_pages(cand)
    # страницы для основного текста ищем после оглавления
    toc_end = max(i for i, p in enumerate(pages[:8]) if any('.....' in l for l in p))
    body_pages = [[] for _ in range(toc_end + 1)] + pages[toc_end + 1:]
    new_numbers = locate(toc_heads, body_pages)
    new_meta = dict(meta, pages=len(pages), main=new_numbers[-1] - 1, app=len(pages) - new_numbers[-1] + 1, figs=info['figs'], tabs=info['tabs'], sources=info['sources'])
    # число страниц ПЗ без приложения
    print('проход', attempt, 'страниц', len(pages), 'рис', info['figs'], 'табл', info['tabs'], flush=True)
    if new_numbers == numbers and new_meta == meta:
        shutil.copy(cand, OUT_DOCX); shutil.copy(pdf, os.path.splitext(OUT_DOCX)[0] + '.pdf')
        json.dump({'pages': len(pages), 'numbers': new_numbers, 'heads': toc_heads}, open(os.path.join(WORK, 'final.json'), 'w'))
        print('готово:', OUT_DOCX); break
    numbers, meta = new_numbers, new_meta
else:
    raise SystemExit('не сошлось')
