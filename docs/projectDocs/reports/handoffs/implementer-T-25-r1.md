# Handoff implementer T-25 — r1

- **ROLE/TARGET:** implementer / T-25 (FS-005 enlarged-scale demo scenario and scale/content checks)
- **Ветка задачи:** `task/T-25-fs005-scale-demo` (создана от `wave/4-large-maps-console-display` @ `1156fba`)
- **MR:** не создавался; передача по ref ветки и commit (мержит `verifier`)
- **Раунд:** 1
- **Статус:** ok, готово к per-task верификации

## Что сделано

- Добавлен `Content/scenarios/fs005_scale_demo.json`: демо-сценарий 6×6 — 36 зон и 36 подразделений (18 Blue / 18 Red), оба ориентира масштаба FS-005 выполнены одновременно.
- Топология: граф с дорогами/бездорожьем, двумя переправами `Crossing` (C2↔D2, C5↔D5), симметричными `travelMinutes`/`edgeTypes`, объектами `Bridge`/`ObservationPost`/`Fortification`/`Depot` и грузом. Ссылки зон/юнитов/целей валидны; веса целей суммируются в 1.0; `startMinute=360`, `endMinute=720`, `seed=505`.
- Добавлен `Kriegsspiel.Core.Tests/Fs005ScaleContentTests.cs`: 3 теста — загрузка через `ContentLoader` с каноническими `Content/friction.json` и `Content/reports.json` (+ `GameStateFactory`), пороги масштаба и консистентность графа, повторяемость загрузки.
- Обновлён `docs/projectDocs/structure.md`: одна запись о новом сценарии в разделе `Content/scenarios` (п. 4 карточки — остальные разделы не трогались).

## Изменённые файлы (ownership)

- `Content/scenarios/fs005_scale_demo.json` (новый) — в ownership T-25.
- `Kriegsspiel.Core.Tests/Fs005ScaleContentTests.cs` (новый) — в ownership T-25.
- `docs/projectDocs/structure.md` — в ownership T-25, только строка нового сценария.
- `docs/projectDocs/reports/handoffs/implementer-T-25-r1.md` — требуемый пользователем handoff-артефакт, канонические отчёты не трогались.

Файлы вне ownership (включая `docs/projectDocs/todo.md`, `docs/gameDocs/rules.md`, `AGENTS.md`, `Content/scenarios/river_line_10x10.json`, файлы T-24, отчёты verifier) не изменены. Проверено через `git status --short`: только перечисленные выше файлы.

## Проверки (команда → результат)

Все команды выполнялись в `/tmp/opencode/task-T-25` до передачи.

- `grep -n "fs005_scale_demo" docs/projectDocs/structure.md` → строка 16, запись о сценарии присутствует.
- `python3 -m json.tool Content/scenarios/fs005_scale_demo.json > /dev/null` → `JSON_OK`.
- `dotnet build Kriegsspiel.slnx` → `Build succeeded`, 0 Warning(s), 0 Error(s).
- `dotnet test Kriegsspiel.Core.Tests/Kriegsspiel.Core.Tests.csproj --filter Fs005ScaleContentTests` → Passed 3 / Failed 0 / Skipped 0.
- `dotnet test Kriegsspiel.slnx` → Passed 148 / Failed 0 / Skipped 0 (регрессий нет).

Окружение: `dotnet 10.0.404`. Ограничений при запуске проверок нет.

## Риски и находки

- Интерактивный smoke `КАРТА` на `fs005_scale_demo` не выполнялся в T-25: `GameLoop` использует `SelectionPrompt` и не автоматизируется через pipe; эта проверка описана в сценарии волны и относится к wave-gate после merge T-24+T-25. Контент для неё подготовлен (36 зон, рёбра трёх типов, мосты, объекты, 36 юнитов). Для `verifier` достаточно загрузки сценария автотестами.
- Карточка (п. 4) явно ограничивает правку `structure.md` только записью о сценарии, поэтому новый тестовый файл не добавлен в раздел тестов структуры. Если нужна запись о `Fs005ScaleContentTests.cs` — это отдельное решение `docs-maintainer`/`verifier` без изменения кода. Затраты низкие.
- Противоречий `docs/gameDocs/rules.md`, неоднозначностей `FS-005` и предложений `TD-*` нет.

## Процесс (трение)

- Неоднозначности: только ограничение п. 4 карточки по объёму правки `structure.md` (см. выше).
- Лишние поиски: низкие — уточнение схемы по `ContentLoader`/`Zone`/`Unit` и формату `river_line_10x10.json` потребовалось для корректной симметрии рёбер и валидации.
- Оценка доп. затрат: низко.
- Предложения: none.
