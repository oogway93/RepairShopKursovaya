const fs = require('fs');
const L = require('./lib');
const { H1, P, DASH, CODE, raw, D, FONT } = L;
const { Paragraph, TextRun, AlignmentType } = D;
const ROOT = '/home/claude/RepairShop/';

function countLines(dir) {
  let n = 0;
  (function walk(d) {
    fs.readdirSync(d, { withFileTypes: true }).forEach(e => {
      const p = d + '/' + e.name;
      if (e.isDirectory()) { if (e.name !== 'obj' && e.name !== 'bin') walk(p); }
      else if (e.name.endsWith('.cs')) n += fs.readFileSync(p, 'utf8').split('\n').length;
    });
  })(dir);
  return n;
}

module.exports.conclusion = function (META) {
  const src = countLines(ROOT + 'src'), tst = countLines(ROOT + 'tests');
  H1('Заключение', { numbered: false, pageBreak: false });
  P('В ходе курсовой работы разработано настольное приложение для ремонтной мастерской электронного оборудования. ' +
    'Все поставленные задачи решены:');
  DASH(['изучена предметная область, выделены бизнес-правила мастерской и проанализированы существующие способы учёта заказов;',
        'составлено техническое задание по ГОСТ 19.201-78, включающее функциональные и эксплуатационные требования, стадии разработки и порядок приёмки;',
        'построены восемь диаграмм – архитектуры, прецедентов, деятельности, состояний, последовательности, двух диаграмм классов и модель данных «сущность – связь»;',
        'приложение реализовано на языке C# с интерфейсом Windows Forms: общий объём исходного кода – около ' + src +
        ' строк, ещё ' + tst + ' строк занимают модульные тесты;',
        'бизнес-логика проверена ' + META.tests + ' модульными тестами, все тесты пройдены;',
        'исходный код размещён в репозитории GitHub (https://github.com/[ваш-логин]/RepairShop).']);
  P('Реализованная система автоматизирует основные процессы мастерской. Приёмщик оформляет заказ за одну форму, а программа ' +
    'сама присваивает номер, проверяет гарантию по серийному номеру и формирует акт приёма. Мастер видит только свои заказы и ' +
    'ведёт ремонт, а контроль допустимых статусов исключает выдачу неготового устройства. Склад ведётся автоматически: ' +
    'запчасти списываются и возвращаются вместе с изменением заказа. Стоимость ремонта рассчитывается без участия человека с ' +
    'учётом скидки постоянного клиента, гарантии и отказа от ремонта. Руководитель получает сводный отчёт за любой период.');
  P('Главным результатом проектирования стало разделение программы на слои: интерфейс не содержит бизнес-правил, а хранилище ' +
    'скрыто за интерфейсом IDataStore. Благодаря этому правила проверяются тестами без запуска окон, а замена XML-файла на ' +
    'реляционную СУБД не потребует переписывать формы и сервисы.');
  P('Приложение можно развивать в нескольких направлениях:');
  DASH(['перейти на СУБД SQLite или SQL Server для работы нескольких рабочих мест с общей базой;',
        'добавить SMS- или e-mail-уведомления клиенту о готовности устройства;',
        'печатать акты на принтере по шаблону и формировать штрих-код или QR-код на квитанции;',
        'вести закупки запчастей у поставщиков и автоматически формировать заявки при снижении остатка;',
        'построить графические отчёты и экспорт в формат Excel.']);
};

module.exports.sources = function () {
  H1('Список использованных источников', { numbered: false, pageBreak: false });
  const list = [
    'О защите прав потребителей: Закон Российской Федерации от 07.02.1992 № 2300-1 (ред. от 08.08.2024) // КонсультантПлюс. – URL: https://www.consultant.ru/document/cons_doc_LAW_305/ (дата обращения: 20.10.2026).',
    'ГОСТ 19.201-78. Единая система программной документации. Техническое задание. Требования к содержанию и оформлению. – М.: Изд-во стандартов, 1978. – 4 с.',
    'Троелсен Э., Джепикс Ф. Язык программирования C# 7 и платформы .NET и .NET Core. – 8-е изд. – М.: Диалектика, 2018. – 1328 с.',
    'Language Integrated Query (LINQ) // Microsoft Learn. – URL: https://learn.microsoft.com/ru-ru/dotnet/csharp/linq/ (дата обращения: 20.10.2026).',
    'Документация по Windows Forms для .NET // Microsoft Learn. – URL: https://learn.microsoft.com/ru-ru/dotnet/desktop/winforms/ (дата обращения: 20.10.2026).',
    'Фаулер М. Архитектура корпоративных программных приложений. – М.: Вильямс, 2006. – 544 с.',
    'Ларман К. Применение UML 2.0 и шаблонов проектирования. – 3-е изд. – М.: Вильямс, 2013. – 736 с.',
  ];
  list.forEach((t, i) => P((i + 1) + '. ' + t));
  return list.length;
};

module.exports.appendix = function () {
  const SRC = ROOT + 'src/RepairShop.Core/';
  const file = (f) => fs.readFileSync(SRC + f, 'utf8').replace(/\r/g, '').replace(/\s+$/, '').split('\n')
    .filter(l => !/^\s*using /.test(l) && !/^\s*\/\/\//.test(l));
  L.state.headings.push({ level: 1, text: 'Приложение А Листинг программы', search: 'Приложение А' });
  raw(new Paragraph({ heading: D.HeadingLevel.HEADING_1, pageBreakBefore: true, alignment: AlignmentType.CENTER,
    spacing: { before: 0, after: 0, line: 360 }, indent: { firstLine: 0 }, keepNext: true,
    children: [new TextRun({ text: 'Приложение А', font: FONT, size: 28 })] }), true);
  raw(new Paragraph({ alignment: AlignmentType.CENTER, spacing: { before: 0, after: 0, line: 360 }, indent: { firstLine: 0 },
    keepNext: true, children: [new TextRun({ text: 'Листинг программы', font: FONT, size: 28, bold: true })] }), true);
  P('Ниже приведены файлы с основными правилами предметной области. Полный исходный код проекта размещён в репозитории GitHub.');
  const head = (t) => raw(new Paragraph({ spacing: { before: 120, after: 60, line: 240 }, indent: { firstLine: 0 }, keepNext: true,
    children: [new TextRun({ text: t, font: FONT, size: 24, bold: true })] }), false);
  head('А.1 Файл Logic/OrderWorkflow.cs');
  CODE(file('Logic/OrderWorkflow.cs').join('\n'));
  head('А.2 Файл Logic/CostCalculator.cs');
  CODE(file('Logic/CostCalculator.cs').join('\n'));
};
