// Сборка пояснительной записки: node main.js meta.json out.docx headings.json
const fs = require('fs');
const L = require('./lib');
const { D } = L;
const META = JSON.parse(fs.readFileSync(process.argv[2], 'utf8'));
const OUT = process.argv[3];

require('./front')(META);
require('./ch2_3')();
require('./ch4')();
require('./ch5').build();
const ch6 = require('./ch6');
ch6.conclusion(META);
const nSources = ch6.sources();
ch6.appendix();

const FONT = L.FONT;
const hstyle = (id, name, lvl) => ({
  id, name, basedOn: 'Normal', next: 'Normal', quickFormat: true,
  run: { font: FONT, size: 28, bold: true, color: '000000' },
  paragraph: { outlineLevel: lvl, spacing: { line: 360 } },
});
const tocStyle = (id, name, left) => ({
  id, name, basedOn: 'Normal', next: 'Normal',
  run: { font: FONT, size: 28 },
  paragraph: { spacing: { before: 0, after: 0, line: 360 }, indent: { left },
               tabStops: [{ type: D.TabStopType.RIGHT, position: 9354, leader: D.LeaderType.DOT }] },
});

const footerP = () => new D.Paragraph({ alignment: D.AlignmentType.CENTER,
  children: [new D.TextRun({ children: [D.PageNumber.CURRENT], font: FONT, size: 24 })] });

const doc = new D.Document({
  creator: 'Курсовая работа', title: 'Приложение для ремонтной мастерской электронного оборудования',
  styles: {
    default: { document: { run: { font: FONT, size: 28 } } },
    paragraphStyles: [hstyle('Heading1', 'heading 1', 0), hstyle('Heading2', 'heading 2', 1), hstyle('Heading3', 'heading 3', 2),
      tocStyle('TOC1', 'toc 1', 0), tocStyle('TOC2', 'toc 2', 284), tocStyle('TOC3', 'toc 3', 568)],
  },
  sections: [{
    properties: {
      titlePage: true,
      page: { size: { width: 11906, height: 16838 }, margin: { top: 851, bottom: 1417, left: 1701, right: 851, footer: 567, header: 567 } },
    },
    footers: { default: new D.Footer({ children: [footerP()] }), first: new D.Footer({ children: [new D.Paragraph('')] }) },
    children: L.out,
  }],
});

D.Packer.toBuffer(doc).then(buf => {
  fs.writeFileSync(OUT, buf);
  fs.writeFileSync(process.argv[4], JSON.stringify({
    headings: L.state.headings, figs: L.state.figs.length, tabs: L.state.tabs.length, sources: nSources,
  }));
});
