# Отчёт задачи T-14 — Reports, fog of war and recon

- **T-XX:** T-14
- **Волна / ветка волны:** Wave 1 / `wave/1-core-mechanics`
- **Ветка задачи:** `task/T-14-reports-fog-and-recon`
- **Целевая ветка MR:** `wave/1-core-mechanics`
- **MR:** локальный merge (remote/MR отсутствует)
- **Раунды:** 1 (реализация в task-ветке) + верификация/merge в ветку волны
- **Финализировал и закоммитил отчёт (verifier, после проверок):** агент (полный цикл), 2026-11-20

## Выполнено (по пунктам «Что сделать»)
- Реализован `KnowledgeUpdater` (контракт `IKnowledgeUpdater` из T-01) и `ReconResolver` в `Arbitration`.
- Обновлён `Kriegsspiel.Core/Reports/IReportNarrator.cs`/нарратор в рамках ownership для работы с структурированными фактами/шаблонами без выдумывания фактов.
- Добавлены тесты `FogOfWarTests` и `ReportTests` (fact key/конфликты, no-data vs empty, recon, отсутствие утечки истины — по фильтру/реализации).

## Изменённые/созданные файлы (только ownership)
- `Kriegsspiel.Core/Arbitration/KnowledgeUpdater.cs` (новый)
- `Kriegsspiel.Core/Arbitration/ReconResolver.cs` (новый)
- `Kriegsspiel.Core/Reports/IReportNarrator.cs`
- `Kriegsspiel.Core.Tests/FogOfWarTests.cs` (новый)
- `Kriegsspiel.Core.Tests/ReportTests.cs` (новый)

## Проверки (из «Как проверить», выполнены implementer в ветке)
- `dotnet build Kriegsspiel.slnx` (worktree `/tmp/opencode/wave1/T-14`) → успех (0/0).
- `dotnet test Kriegsspiel.Core.Tests/Kriegsspiel.Core.Tests.csproj --filter "FogOfWarTests|ReportTests"` → Passed 19, Failed 0.

## Верификация (verifier)
- Вердикт: verified
- Замечания/раунды: критичных замечаний нет; есть нюансы интерпретации §6 и ограничения интеграции.
- Merge в ветку волны: выполнен (merge `task/T-14-reports-fog-and-recon` → `wave/1-core-mechanics`).
- Post-merge в контексте ветки волны (сборка+старт): `dotnet build Kriegsspiel.slnx` → успех (0/0); `dotnet test Kriegsspiel.Core.Tests/Kriegsspiel.Core.Tests.csproj --filter "FogOfWarTests|ReportTests"` → Passed 19, Failed 0.

## Процесс (трение)
- Старт/конец или отметки раундов: реализация в task-ветке, затем верификация/merge в ветку волны.
- Неоднозначности (ссылки на карточку/`docs/gameDocs/rules.md §…`/док/файл): формула recon/случай `obs=3` и профильные параметры (`§6`), интеграция report/knowledge pipeline в `Arbiter` (T-20), вне ownership.
- Лишние поиски и причина (чего не хватило в `structure.md`/ownership/шаблоне/требованиях): согласование с контрактами `IKnowledgeUpdater/IReportFactory` и ограничениями без правок домена/контента.
- Оценка доп. затрат: средне.
- Предложения по улучшению: уточнять в карточках границы ответственности factory/narrator/updater и точки интеграции.

## Заметки / tech-debt
- Ограничения implementer: нет интеграции в `Arbiter`/CLI/persistence; `docs/projectDocs/structure.md` вне ownership; для случая `obs=3`/ошибочной интерпретации требуются профильные параметры вне меняемых файлов. Отдельный `TD-*` не оформлялся в этой сессии.
