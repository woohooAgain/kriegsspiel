# Отчёт задачи T-25 — FS-005 enlarged-scale demo scenario and scale/content checks

- **T-XX:** T-25 — демо-контент увеличенного масштаба и проверки масштаба/контента FS-005.
- **Волна / ветка волны:** Wave 4 / `wave/4-large-maps-console-display` (база merge: `faee47a`, содержит T-24).
- **Ветка задачи / MR:** `task/T-25-fs005-scale-demo` @ `6544cbc`; merge в ветку волны `3e1ba9e` (`--no-ff`, target — ветка волны; отдельный MR не создавался, передача по ref ветки).
- **Раунды:** r1 — handoff `docs/projectDocs/reports/handoffs/implementer-T-25-r1.md` (в ветке задачи и в ветке волны).
- **Финализировал и закоммитил отчёт (verifier, после проверок):** verifier, 2026-11-22, после per-task и post-merge проверок.

## Выполнено (по пунктам «Что сделать»)
- Добавлен `Content/scenarios/fs005_scale_demo.json`: 36 зон (6×6) и 36 подразделений (18 Blue / 18 Red), оба ориентира масштаба FS-005 (>=30 зон и >=30 юнитов) выполнены; `startMinute=360`, `endMinute=720`, `seed=505`, `victoryThreshold=0.7`.
- Топология валидна: симметричные `travelMinutes`/`edgeTypes` (Road/Offroad/Crossing), ровно две переправы C2↔D2 и C5↔D5, объекты Bridge/ObservationPost/Fortification/Depot, груз; ссылки зон/юнитов/целей корректны, веса целей суммируются в 1.0.
- Добавлен `Kriegsspiel.Core.Tests/Fs005ScaleContentTests.cs`: 3 теста — загрузка через `ContentLoader` с каноническими `Content/friction.json`/`Content/reports.json` и `GameStateFactory`; пороги масштаба и инварианты графа; повторяемость загрузки.
- `docs/projectDocs/structure.md`: добавлена одна запись о новом сценарии в раздел `Content/scenarios` (строки 16), остальные разделы не изменены.

## Изменённые/созданные файлы (только ownership)
- `Content/scenarios/fs005_scale_demo.json` (новый) — ownership T-25.
- `Kriegsspiel.Core.Tests/Fs005ScaleContentTests.cs` (новый) — ownership T-25.
- `docs/projectDocs/structure.md` — ownership T-25, только одна строка записи о сценарии.
- `docs/projectDocs/reports/handoffs/implementer-T-25-r1.md` — транспортный handoff (не канонический отчёт).
- Проверено `git show --stat 6544cbc`: в коммите задачи нет других файлов; `todo.md`, `rules.md`, `AGENTS.md`, файлы T-24 (`MapCommand.cs`/`MapRenderer.cs` и отчёты), `river_line_10x10.json` в коммите задачи не изменены. Merge `3e1ba9e` внёс в ветку волны только перечисленные 4 файла.

## Проверки (evidence refs)
- В worktree `task-T-25` (чистый, `dotnet 10.0.404`), перезапуск команд из карточки: `dotnet build Kriegsspiel.slnx` → Build succeeded, 0 Warning / 0 Error.
- `dotnet test Kriegsspiel.Core.Tests/Kriegsspiel.Core.Tests.csproj --filter Fs005ScaleContentTests` → Passed 3 / Failed 0 / Skipped 0.
- `dotnet test Kriegsspiel.slnx` → Passed 148 / Failed 0 / Skipped 0.
- `python3 -m json.tool Content/scenarios/fs005_scale_demo.json > /dev/null` → JSON_OK; `grep fs005_scale_demo docs/projectDocs/structure.md` → запись на строке 16.
- Независимая проверка контента (python): zones=36, units=36 (18/18), crossing-рёбра C2↔D2/C5↔D5, асимметрий/битых ссылок 0, все локации юнитов существуют, сумма весов целей = 1.0.
- Post-merge в `/tmp/opencode/wave-4` (ветка волны с T-24): build 0 Warning / 0 Error; targeted 3/3; solution 148/148.

## Верификация (verifier)
- Вердикт: **verified** (r1, без доработок).
- Замечания/раунды (дельта): замечаний нет; handoff r1 принят целиком, все утверждения карточки подтверждены повторным прогоном.
- Merge в ветку волны: выполнен, `3e1ba9e` (`merge: T-25 FS-005 scale demo scenario (verified r1)`); target — `wave/4-large-maps-console-display`, не main.
- Post-merge в контексте ветки волны: сборка/старт и тесты успешны (build 0/0, targeted 3/3, solution 148/148); регрессий после T-24 нет.
- Интерактивный smoke `КАРТА` на масштабе не выполнялся в per-task: согласно сценарию Waves 4 (todo.md:84) он относится к wave-gate после merge T-24+T-25; autocontent для него подготовлен и загружается тестами.

## Процесс (трение)
- Неоднозначности: п. 4 карточки ограничивает правку `structure.md` только записью о сценарии — ограничение соблюдено; запись о новом тестовом файле не добавлялась (это не требовалось карточкой).
- Лишние поиски и причина: низкие — уточнение схемы `ContentLoader`/`Zone`/`Unit` и формата `river_line_10x10` для симметрии рёбер (по handoff r1).
- Оценка доп. затрат: низко.
- Предложения по улучшению: none.

## Заметки / tech-debt
- Нет (`TD-*`/противоречий правил не обнаружено).
