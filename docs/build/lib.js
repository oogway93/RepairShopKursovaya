// Вспомогательная библиотека построения пояснительной записки по памятке оформления КР
const fs = require('fs');
const D = require('docx');
const {
  Paragraph, TextRun, ImageRun, Table, TableRow, TableCell, AlignmentType, WidthType, BorderStyle,
  HeadingLevel, PageBreak, TabStopType, VerticalAlign, ShadingType, LineRuleType
} = D;

const FONT = 'Times New Roman';
const GAP = 420;           // одна пустая строка (14 пт * 1,5 = 21 пт)
const INDENT = 850;        // 1,5 см
const TEXT_W = 9354;       // ширина текста: 210-30-15 мм
const sizes = JSON.parse(fs.readFileSync('/home/claude/RepairShop/docs/build/imgsizes.json', 'utf8'));

const state = { prevBottom: false, figs: [], tabs: [], figCount: 0, tabCount: 0, headings: [], pending: null };
const out = [];            // накопленные блоки

function gapFor(top) {
  const g = (state.prevBottom || top) ? GAP : 0;
  return g;
}
function push(p, bottom) { out.push(p); state.prevBottom = !!bottom; }

function runs(text, opts) {
  opts = opts || {};
  // поддержка **жирного** внутри текста
  const parts = String(text).split(/(\*\*[^*]+\*\*)/g).filter(s => s.length);
  return parts.map(s => {
    const bold = s.startsWith('**');
    return new TextRun({ text: bold ? s.slice(2, -2) : s, bold: bold || opts.bold, font: opts.font || FONT,
                         size: opts.size || 28, italics: opts.italics });
  });
}

// ---------- заголовки ----------
function H(level, text, o) {
  o = o || {};
  const numbered = o.numbered !== false;
  const pb = o.pageBreak !== undefined ? o.pageBreak : !numbered;
  const lvl = [HeadingLevel.HEADING_1, HeadingLevel.HEADING_2, HeadingLevel.HEADING_3][level - 1];
  state.headings.push({ level, text });
  const para = new Paragraph({
    heading: lvl,
    pageBreakBefore: level === 1 && pb,
    alignment: numbered ? AlignmentType.JUSTIFIED : AlignmentType.CENTER,
    indent: { firstLine: numbered ? INDENT : 0 },
    keepNext: true,
    spacing: { before: level === 1 && pb ? 0 : gapFor(true), after: 0, line: 360 },
    children: [new TextRun({ text, bold: true, font: FONT, size: 28 })],
  });
  push(para, true);
}
const H1 = (t, o) => H(1, t, o);
const H2 = (t) => H(2, t);
const H3 = (t) => H(3, t);

// ---------- текст ----------
function P(text, o) {
  o = o || {};
  push(new Paragraph({
    alignment: AlignmentType.JUSTIFIED,
    indent: { firstLine: o.noIndent ? 0 : INDENT },
    spacing: { before: gapFor(false), after: 0, line: 360 },
    keepNext: o.keepNext,
    children: runs(text),
  }), false);
}
function DASH(items) {
  items.forEach(t => P('– ' + t));
}
function NUM(items) {
  items.forEach((t, i) => P((i + 1) + '. ' + t));
}
function CODE(text) {
  const lines = text.replace(/\n$/, '').split('\n');
  lines.forEach((ln, i) => {
    out.push(new Paragraph({
      alignment: AlignmentType.LEFT,
      spacing: { before: i === 0 ? gapFor(true) : 0, after: 0, line: 240 },
      indent: { left: 0, firstLine: 0 },
      keepLines: true,
      children: [new TextRun({ text: ln === '' ? ' ' : ln, font: 'Courier New', size: 18 })],
    }));
  });
  state.prevBottom = true;
}

// ---------- рисунки ----------
function FIG(name, file, widthCm, caption) {
  const idx = state.figs.length;
  state.figs.push(name);
  const num = idx + 1;
  const sz = sizes[file];
  if (!sz) throw new Error('нет размера для ' + file);
  let wpx = Math.round(widthCm * 37.795);
  let hpx = Math.round(wpx * sz[1] / sz[0]);
  const maxH = Math.round(9 * 37.795);          // высота рисунка не более 9 см
  if (hpx > maxH) { wpx = Math.round(wpx * maxH / hpx); hpx = maxH; }
  push(new Paragraph({
    alignment: AlignmentType.CENTER, keepNext: true,
    spacing: { before: gapFor(true), after: 0 },
    indent: { firstLine: 0 },
    children: [new ImageRun({ type: 'png', data: fs.readFileSync(file), transformation: { width: wpx, height: hpx },
                              altText: { title: caption, description: caption, name } })],
  }), false);
  push(new Paragraph({
    alignment: AlignmentType.CENTER,
    spacing: { before: 0, after: 0, line: 360 },
    indent: { firstLine: 0 },
    children: runs('Рисунок ' + num + ' – ' + caption),
  }), true);
  return num;
}

// ---------- таблицы ----------
function TABLE(name, caption, widths, header, rows, o) {
  o = o || {};
  const idx = state.tabs.length;
  state.tabs.push(name);
  const num = idx + 1;
  const total = widths.reduce((a, b) => a + b, 0);
  const k = TEXT_W / total;
  const w = widths.map(x => Math.round(x * k));
  const border = { style: BorderStyle.SINGLE, size: 4, color: '000000' };
  const borders = { top: border, bottom: border, left: border, right: border };
  const fs12 = o.size || 24;
  const cell = (txt, i, isHead, align) => new TableCell({
    width: { size: w[i], type: WidthType.DXA }, borders, verticalAlign: VerticalAlign.CENTER,
    margins: { top: 40, bottom: 40, left: 90, right: 90 },
    shading: isHead ? { type: ShadingType.CLEAR, fill: 'F2F2F2', color: 'auto' } : undefined,
    children: String(txt).split('\n').map(line => new Paragraph({
      alignment: isHead ? AlignmentType.CENTER : (align || AlignmentType.LEFT),
      spacing: { before: 0, after: 0, line: 240 }, indent: { firstLine: 0 },
      children: [new TextRun({ text: line, bold: isHead, font: FONT, size: fs12 })],
    })),
  });
  push(new Paragraph({
    alignment: AlignmentType.LEFT, keepNext: true,
    spacing: { before: gapFor(true), after: 0, line: 360 }, indent: { firstLine: 0 },
    children: runs('Таблица ' + num + ' – ' + caption),
  }), false);
  const aligns = o.aligns || [];
  const trs = [new TableRow({ tableHeader: true, cantSplit: true,
                children: header.map((h, i) => cell(h, i, true)) })];
  rows.forEach(r => trs.push(new TableRow({ cantSplit: true,
                children: r.map((c, i) => cell(c, i, false, aligns[i])) })));
  out.push(new Table({ width: { size: TEXT_W, type: WidthType.DXA }, columnWidths: w, rows: trs }));
  state.prevBottom = true;
  return num;
}

const figNo = (name) => { const i = ORDER.figs.indexOf(name); if (i < 0) throw new Error('fig? ' + name); return i + 1; };
const tabNo = (name) => { const i = ORDER.tabs.indexOf(name); if (i < 0) throw new Error('tab? ' + name); return i + 1; };
const ORDER = {
  figs: ['arch', 'usecase', 'activity', 'states', 'sequence', 'classes', 'services', 'er',
         'login', 'main', 'create', 'acts', 'details', 'tabs', 'reports', 'master'],
  tabs: ['rules', 'analogs', 'roles', 'funcreq', 'stages', 'transitions', 'datadict', 'datalinks', 'forms',
         'structure', 'test1', 'test2', 'test3', 'test4'],
};
const F = (n) => 'рисунке ' + figNo(n);       // «на рисунке N»
const Fr = (n) => 'рисунок ' + figNo(n);
const T = (n) => 'таблице ' + tabNo(n);
const Tr = (n) => 'таблица ' + tabNo(n);

function raw(p, bottom) { push(p, bottom); }

module.exports = { D, H1, H2, H3, P, DASH, NUM, CODE, FIG, TABLE, F, Fr, T, Tr, figNo, tabNo, raw, out, state, ORDER,
                   FONT, GAP, INDENT, TEXT_W, runs, push };
