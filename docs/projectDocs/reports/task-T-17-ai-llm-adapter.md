# Отчёт задачи T-17 — AI/LLM adapter

- **T-XX:** T-17
- **Волна / ветка волны:** Wave 1 / `wave/1-core-mechanics`
- **Ветка задачи:** `task/T-17-ai-llm-adapter`
- **Целевая ветка MR:** `wave/1-core-mechanics`
- **MR:** локальный merge (remote/MR отсутствует)
- **Раунды:** 1 (реализация в task-ветке) + верификация/merge в ветку волны
- **Финализировал и закоммитил отчёт (verifier, после проверок):** агент (полный цикл), 2026-11-20

## Выполнено (по пунктам «Что сделать»)
- Создан проект `Kriegsspiel.AI` с адаптерами (`LlmOrderCompiler`, `LlmReportNarrator`, `LlmAmbiguityInterpreter`, `StateQueryTools`) и промптами/схемами в `Kriegsspiel.AI/Prompts/*`.
- Добавлена логика валидации/проекции контекста/детерминированного fallback и replay/log в новых файлах `Kriegsspiel.Core/Ai/*` (в пределах ownership), нарратор ограничен структурированными фактами.
- Добавлены тесты `Kriegsspiel.Core.Tests/AIAdapterTests.cs`.

## Изменённые/созданные файлы (только ownership)
- `Kriegsspiel.AI/Kriegsspiel.AI.csproj` и `Kriegsspiel.AI/*.cs`, `Kriegsspiel.AI/Prompts/*` (новые)
- `Kriegsspiel.Core/Ai/*` (новые файлы)
- `Kriegsspiel.Core.Tests/AIAdapterTests.cs` (новый)

## Проверки (из «Как проверить», выполнены implementer в ветке)
- `dotnet build Kriegsspiel.slnx` (worktree `/tmp/opencode/wave1/T-17`) → успех (0/0).
- `dotnet build Kriegsspiel.AI/Kriegsspiel.AI.csproj` → успех (0/0).
- `dotnet test Kriegsspiel.Core.Tests/Kriegsspiel.Core.Tests.csproj --filter AIAdapterTests` → Passed 10, Failed 0.

## Верификация (verifier)
- Вердикт: verified (с зафиксированными ограничениями интеграции вне ownership)
- Замечания/раунды: `Kriegsspiel.AI` не добавлен в `.slnx` и нет project reference из `Kriegsspiel.Core.Tests.csproj` (владение T-22); `AIAdapterTests` в ветке волны проверяют логику через `Core/Ai` и проходят, но полная интеграция проекта/DI — вне scope T-17.
- Merge в ветку волны: выполнен (merge `task/T-17-ai-llm-adapter` → `wave/1-core-mechanics`).
- Post-merge в контексте ветки волны (сборка+старт): `dotnet build Kriegsspiel.slnx` → успех (0/0); `dotnet build Kriegsspiel.AI/Kriegsspiel.AI.csproj` → успех (0/0); `dotnet test Kriegsspiel.Core.Tests/Kriegsspiel.Core.Tests.csproj --filter AIAdapterTests` → Passed 10, Failed 0.

## Процесс (трение)
- Старт/конец или отметки раундов: реализация в task-ветке, затем верификация/merge в ветку волны.
- Неоднозначности (ссылки на карточку/`docs/gameDocs/rules.md §…`/док/файл): невозможность подключить проект/тесты в solution и DI из-за ownership (`§12`, T-17 vs T-22); зависимость fallback от `EnemyDoctrine`/интеграции в других ветках.
- Лишние поиски и причина (чего не хватило в `structure.md`/ownership/шаблоне/требованиях): проверка границ правок csproj/slnx/конфигов.
- Оценка доп. затрат: средне/высоко (+ обходы ограничений ownership).
- Предложения по улучшению: явно описывать стратегию компоновки/проверок, когда task создаёт новый проект, но не владеет solution/DI.

## Заметки / tech-debt
- Интеграция `Kriegsspiel.AI` в `.slnx`/DI/конфиг — T-22. `docs/projectDocs/structure.md` не обновлялся (вне ownership). Семантическая гарантия нарратора ограничена структурой/контрактом (см. реализацию). Отдельный `TD-*` не оформлялся в этой сессии.
