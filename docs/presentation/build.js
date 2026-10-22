const pptxgen = require('pptxgenjs');
const { applyTheme } = require('/mnt/skills/public/pptx/scripts/apply_theme.js');
const fs = require('fs');
const IMG = '/home/claude/RepairShop/docs/';
const sizes = JSON.parse(fs.readFileSync('/home/claude/RepairShop/docs/build/imgsizes.json', 'utf8'));

const THEME = {
  name: 'Repair Shop', headFontFace: 'Cambria', bodyFontFace: 'Calibri',
  colors: { dk1: '17252C', lt1: 'FFFFFF', dk2: '0F3D4A', lt2: 'EEF3F4', accent1: 'F2A33A', accent2: '2A7F8E',
            accent3: '5C7C89', accent4: 'C8553D', accent5: '7FB77E', accent6: '8A6FB3', hlink: '2A7F8E', folHlink: '5C7C89' },
};
const pres = new pptxgen();
pres.layout = 'LAYOUT_16x9';
pres.theme = { headFontFace: THEME.headFontFace, bodyFontFace: THEME.bodyFontFace };
pres.title = 'Приложение для ремонтной мастерской электронного оборудования';
const C = pres.SchemeColor;

pres.defineSlideMaster({
  title: 'DARK', background: { color: '0F3D4A' },
  objects: [],
  slideNumber: { x: 9.2, y: 5.25, w: 0.5, h: 0.25, fontSize: 10, color: 'FFFFFF' },
});
pres.defineSlideMaster({
  title: 'CONTENT', background: { color: 'FFFFFF' },
  objects: [{ placeholder: { options: { name: 'title', type: 'title', x: 0.5, y: 0.3, w: 9, h: 0.8, fontSize: 34, bold: true,
    color: C.text2, fontFace: 'Cambria', margin: 0, valign: 'middle' }, text: '' } }],
  slideNumber: { x: 9.2, y: 5.25, w: 0.5, h: 0.25, fontSize: 10, color: '5C7C89' },
});

function fit(file, x, y, maxW, maxH, o) {          // рисунок с сохранением пропорций, по центру области
  const full = IMG + file; const [w, h] = sizes[full];
  const k = Math.min(maxW / w, maxH / h);
  const W = w * k, H = h * k;
  return { path: full, x: x + (maxW - W) / 2, y: y + (maxH - H) / 2, w: W, h: H };
}
function card(s, x, y, w, h, name) {
  s.addShape(pres.ShapeType.roundRect, { x, y, w, h, rectRadius: 0.08, fill: { color: C.background2 }, line: { color: C.background2 }, objectName: name });
}
function dot(s, n, x, y, name, fill) {
  s.addShape(pres.ShapeType.ellipse, { x, y, w: 0.42, h: 0.42, fill: { color: fill || C.accent1 }, line: { color: fill || C.accent1 }, objectName: name });
  s.addText(String(n), { x, y, w: 0.42, h: 0.42, align: 'center', valign: 'middle', fontSize: 14, bold: true, color: C.text1, margin: 0, isTextBox: true });
}
const T = (s, t, x, y, w, h, o) => s.addText(t, Object.assign({ x, y, w, h, fontSize: 14, color: C.text1, margin: 0, valign: 'top', isTextBox: true }, o || {}));
const bullets = (arr, o) => arr.map((t, i) => ({ text: t, options: Object.assign({ bullet: true, breakLine: i < arr.length - 1, paraSpaceAfter: 6 }, o || {}) }));
function notes(s, t) { s.addNotes(t); }

// ---------- 1. Проект, автор ----------
pres.addSection({ title: 'Проект, автор' });
let s = pres.addSlide({ masterName: 'DARK', sectionTitle: 'Проект, автор' });
s.addShape(pres.ShapeType.roundRect, { x: 0.5, y: 0.55, w: 2.5, h: 0.42, rectRadius: 0.21, fill: { color: C.accent1 }, line: { color: C.accent1 }, objectName: 'Метка' });
T(s, 'КУРСОВАЯ РАБОТА', 0.5, 0.55, 2.5, 0.42, { fontSize: 14, bold: true, align: 'center', valign: 'middle', color: C.text1 });
T(s, 'Приложение для ремонтной мастерской электронного оборудования', 0.5, 1.2, 5.6, 2.3,
  { fontSize: 30, bold: true, fontFace: 'Cambria', color: C.background1 });
T(s, 'по дисциплине «Программная инженерия»', 0.5, 3.65, 5.6, 0.4, { fontSize: 16, color: C.background2 });
T(s, [{ text: 'Автор: [Фамилия Имя Отчество], гр. [____]', options: { breakLine: true } },
      { text: 'Руководитель: [должность, И.О. Фамилия]', options: { breakLine: true } },
      { text: 'КубГТУ, Краснодар, 2026' }], 0.5, 4.2, 5.8, 1.0, { fontSize: 14, color: C.background2, paraSpaceAfter: 4 });
s.addImage(fit('screenshots/02_main_orders.png', 6.3, 1.5, 3.4, 2.6));
notes(s, 'Здравствуйте. Тема моей курсовой работы — приложение для ремонтной мастерской электронного оборудования, язык C#.');

// ---------- 2. Цель, задачи ----------
pres.addSection({ title: 'Цель, задачи' });
s = pres.addSlide({ masterName: 'CONTENT', sectionTitle: 'Цель, задачи' });
s.addText('Цель и задачи', { placeholder: 'title' });
card(s, 0.5, 1.3, 3.2, 3.7, 'Карточка цели');
T(s, 'ЦЕЛЬ', 0.75, 1.5, 2.7, 0.3, { fontSize: 14, bold: true, color: C.accent2 });
T(s, 'Разработать приложение на C#, которое автоматизирует приём, ремонт и выдачу устройств в мастерской и снижает число ошибок в учёте.',
  0.75, 1.9, 2.7, 2.9, { fontSize: 16 });
const tasks = ['Изучить предметную область и аналоги', 'Составить техническое задание (ГОСТ 19.201-78)',
  'Спроектировать приложение: UML-диаграммы, модель данных', 'Реализовать программу на C# (Windows Forms)',
  'Протестировать и разместить код на GitHub'];
tasks.forEach((t, i) => { dot(s, i + 1, 4.1, 1.35 + i * 0.75, 'Номер ' + (i + 1)); T(s, t, 4.7, 1.35 + i * 0.75, 4.8, 0.55, { fontSize: 16, valign: 'middle' }); });
notes(s, 'Цель — автоматизировать учёт заказов в мастерской. Для этого решены пять задач: от анализа предметной области до тестирования и публикации кода.');

// ---------- 3. Предметная область ----------
pres.addSection({ title: 'Основная часть' });
s = pres.addSlide({ masterName: 'CONTENT', sectionTitle: 'Основная часть' });
s.addText('Предметная область', { placeholder: 'title' });
card(s, 0.5, 1.3, 4.3, 3.4, 'Проблемы');
T(s, 'Как работают сейчас', 0.75, 1.5, 3.8, 0.35, { fontSize: 18, bold: true, color: C.accent4, fontFace: 'Cambria' });
T(s, bullets(['Бумажные журналы и таблицы', 'Теряются заявки, забываются сроки', 'Запчасти списываются «на глаз»', 'Стоимость считают вручную', 'О гарантии узнают со слов клиента']),
  0.75, 2.0, 3.8, 2.9, { fontSize: 15 });
card(s, 5.2, 1.3, 4.3, 3.4, 'Решение');
T(s, 'Что даёт программа', 5.45, 1.5, 3.8, 0.35, { fontSize: 18, bold: true, color: C.accent2, fontFace: 'Cambria' });
T(s, bullets(['Единая база клиентов и заказов', 'Контроль статусов и сроков', 'Автоучёт запчастей на складе', 'Автоматический расчёт стоимости', 'Гарантия определяется по серийному номеру']),
  5.45, 2.0, 3.8, 2.9, { fontSize: 15 });
notes(s, 'Небольшие мастерские ведут учёт на бумаге. Это приводит к потерям заявок и ошибкам в расчётах. Универсальные CRM избыточны, поэтому нужна специализированная программа.');

// ---------- 4. Архитектура ----------
s = pres.addSlide({ masterName: 'CONTENT', sectionTitle: 'Основная часть' });
s.addText('Архитектура', { placeholder: 'title' });
s.addImage(fit('diagrams/arch.png', 0.5, 1.4, 5.7, 3.4));
const layers = [['Интерфейс', 'Windows Forms, без бизнес-правил'], ['Бизнес-логика', 'Сервисы и чистые правила'], ['Данные', 'IDataStore: XML-файл']];
layers.forEach((l, i) => { dot(s, i + 1, 6.5, 1.5 + i * 1.1, 'Слой ' + (i + 1), C.accent2);
  T(s, l[0], 7.05, 1.45 + i * 1.1, 2.5, 0.35, { fontSize: 18, bold: true, fontFace: 'Cambria' });
  T(s, l[1], 7.05, 1.82 + i * 1.1, 2.5, 0.6, { fontSize: 14, color: C.accent3 }); });
notes(s, 'Приложение построено по трём слоям. Зависимости направлены только вниз, поэтому бизнес-логику можно тестировать без запуска окон.');

// ---------- 5. Прецеденты ----------
s = pres.addSlide({ masterName: 'CONTENT', sectionTitle: 'Основная часть' });
s.addText('Диаграмма прецедентов', { placeholder: 'title' });
s.addImage(fit('diagrams/usecase.png', 0.5, 1.25, 4.2, 3.9));
card(s, 5.1, 1.3, 4.4, 1.7, 'Администратор');
T(s, 'Администратор', 5.3, 1.4, 4, 0.35, { fontSize: 18, bold: true, fontFace: 'Cambria', color: C.accent2 });
T(s, 'Приём и выдача устройств, назначение мастера, склад, прайс-лист, сотрудники, отчёты', 5.3, 1.8, 4.0, 1.1, { fontSize: 14 });
card(s, 5.1, 3.3, 4.4, 1.7, 'Мастер');
T(s, 'Мастер', 5.3, 3.4, 4, 0.35, { fontSize: 18, bold: true, fontFace: 'Cambria', color: C.accent2 });
T(s, 'Свои заказы, смена статусов, добавление работ и запчастей, просмотр склада', 5.3, 3.8, 4.0, 1.1, { fontSize: 14 });
notes(s, 'В системе две роли. Администратор имеет полный доступ, мастер видит только назначенные ему заказы.');

// ---------- 6. Жизненный цикл ----------
s = pres.addSlide({ masterName: 'CONTENT', sectionTitle: 'Основная часть' });
s.addText('Жизненный цикл заказа', { placeholder: 'title' });
T(s, bullets(['Принят → Диагностика → Ремонт → Готов → Выдан', 'Отмена возможна до готовности', 'Диагностика и ремонт — только с назначенным мастером', '«Готов» — только если указаны работы', 'Недопустимый статус выбрать нельзя']),
  0.5, 1.4, 5.3, 3.5, { fontSize: 16 });
s.addImage(fit('diagrams/states.png', 6.2, 1.2, 3.3, 3.95));
notes(s, 'Переходы описаны конечным автоматом в одном месте — классе OrderWorkflow. Интерфейс предлагает только допустимые следующие статусы.');

// ---------- 7. Модель данных ----------
s = pres.addSlide({ masterName: 'CONTENT', sectionTitle: 'Основная часть' });
s.addText('Модель данных', { placeholder: 'title' });
s.addImage(fit('diagrams/er.png', 0.5, 1.3, 5.6, 3.8));
T(s, bullets(['Клиент → устройства → заказы', 'Заказ содержит работы, запчасти и историю статусов', 'Цена копируется в заказ при добавлении', 'Хранение в XML, доступ через IDataStore — можно перейти на SQLite']),
  6.4, 1.5, 3.1, 3.4, { fontSize: 15 });
notes(s, 'Модель данных реляционная, хотя физически сериализуется в XML. При необходимости достаточно написать новый класс хранилища.');

// ---------- 8. Интерфейс ----------
s = pres.addSlide({ masterName: 'CONTENT', sectionTitle: 'Основная часть' });
s.addText('Интерфейс программы', { placeholder: 'title' });
s.addImage(fit('screenshots/02_main_orders.png', 0.5, 1.3, 6.0, 3.8));
T(s, bullets(['Поиск и фильтры по статусу и мастеру', 'Просроченные и готовые заказы выделены цветом', 'Вкладки: клиенты, склад, прайс, отчёты, сотрудники', 'Экспорт заказов в CSV']),
  6.8, 1.5, 2.7, 3.4, { fontSize: 14 });
notes(s, 'Главное окно — список заказов. Двойной щелчок открывает карточку заказа.');

// ---------- 9. Карточка заказа и правила ----------
s = pres.addSlide({ masterName: 'CONTENT', sectionTitle: 'Основная часть' });
s.addText('Карточка заказа', { placeholder: 'title' });
s.addImage(fit('screenshots/03_order_details.png', 0.5, 1.3, 4.9, 3.8));
const rules = [['5–10 %', 'скидка постоянному клиенту на работы'], ['30 дней', 'гарантия: повторный ремонт бесплатно'], ['500 ₽', 'диагностика при отказе от ремонта'], ['авто', 'списание и возврат запчастей']];
rules.forEach((r, i) => { const y = 1.35 + i * 0.95; card(s, 5.7, y, 3.8, 0.8, 'Правило ' + (i + 1));
  T(s, r[0], 5.85, y, 1.3, 0.8, { fontSize: 20, bold: true, fontFace: 'Cambria', color: C.accent2, valign: 'middle' });
  T(s, r[1], 7.15, y, 2.25, 0.8, { fontSize: 13, valign: 'middle' }); });
notes(s, 'В карточке мастер ведёт ремонт. Стоимость пересчитывается автоматически с учётом скидки, гарантии и отказа от ремонта.');

// ---------- 10. Тестирование ----------
s = pres.addSlide({ masterName: 'CONTENT', sectionTitle: 'Основная часть' });
s.addText('Тестирование', { placeholder: 'title' });
const stats = [['40', 'модульных тестов'], ['40', 'из 40 пройдено'], ['8', 'диаграмм и схем'], ['~2800', 'строк кода C#']];
stats.forEach((st, i) => { const x = 0.5 + i * 2.28; card(s, x, 1.4, 2.1, 1.9, 'Показатель ' + (i + 1));
  T(s, st[0], x, 1.55, 2.1, 0.95, { fontSize: 40, bold: true, fontFace: 'Cambria', color: C.accent2, align: 'center', valign: 'middle' });
  T(s, st[1], x + 0.1, 2.6, 1.9, 0.5, { fontSize: 14, align: 'center' }); });
T(s, bullets(['Тесты проверяют правила без запуска окон: FixedClock и MemoryDataStore', 'Покрыты авторизация, статусы, склад, стоимость, гарантия, поиск, хранилище']),
  0.5, 3.7, 9, 1.3, { fontSize: 15 });
notes(s, 'Бизнес-логика проверена сорока модульными тестами, все они пройдены.');

// ---------- 11. Заключение ----------
pres.addSection({ title: 'Заключение, выводы' });
s = pres.addSlide({ masterName: 'DARK', sectionTitle: 'Заключение, выводы' });
T(s, 'Заключение', 0.5, 0.35, 9, 0.8, { fontSize: 34, bold: true, fontFace: 'Cambria', color: C.background1, valign: 'middle' });
T(s, 'Выводы', 0.5, 1.4, 4.2, 0.35, { fontSize: 18, bold: true, color: C.accent1, fontFace: 'Cambria' });
T(s, bullets(['Разработано приложение для ремонтной мастерской на C# и Windows Forms', 'Правила мастерской вынесены в тестируемую бизнес-логику', 'Заказы, склад и стоимость ведутся автоматически']),
  0.5, 1.85, 4.2, 3.0, { fontSize: 15, color: C.background1 });
T(s, 'Развитие', 5.3, 1.4, 4.2, 0.35, { fontSize: 18, bold: true, color: C.accent1, fontFace: 'Cambria' });
T(s, bullets(['SQLite или SQL Server для нескольких рабочих мест', 'SMS-уведомления клиентам о готовности', 'Печать актов и QR-код на квитанции']),
  5.3, 1.85, 4.2, 3.0, { fontSize: 15, color: C.background1 });
T(s, 'Исходный код: github.com/[логин]/RepairShop', 0.5, 4.9, 6, 0.3, { fontSize: 12, color: C.background2 });
notes(s, 'Все задачи выполнены. Спасибо за внимание, готов ответить на вопросы.');

(async () => {
  await pres.writeFile({ fileName: '/home/claude/RepairShop/docs/presentation/Презентация.pptx' });
  await applyTheme('/home/claude/RepairShop/docs/presentation/Презентация.pptx', THEME);
  console.log('ok');
})();
