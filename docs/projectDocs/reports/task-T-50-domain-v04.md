# Отчёт задачи T-50 — домен v0.4 (Unit/Order/Report/GameState/Knowledge)

- **T-50:** домен v0.4: `Unit`/`Order`/`EffectiveOrder`/`Report`/`GameState`/`Zone`/`CommanderTraits`/`Courier`/`PlayerKnowledge`, `IGameRules`, append-only knowledge.
- **Волна / ветка волны:** Wave 5, `wave/5-v04-foundation` @ `90acbe6` (база — `master@90acbe6`).
- **Ветка задачи / MR:** `task/T-50-domain-v04` @ `55bf554` (код-коммит `16d1eda`), worktree implementer `/tmp/opencode/ks-implementer-T-50-r1`; MR не создавался (передача по refs ветки).
- **Раунды:** r1 — `docs/projectDocs/reports/handoffs/implementer-T-50-r1.md` (`55bf554`).
- **Финализировал и закоммитил отчёт (verifier, после проверок):** этот коммит в `verifier/T-50-r1`.

## Выполнено (по пунктам «Что сделать»)
1. `Unit`: edge-локация (`IsOnEdge`, `EdgeFromZoneId`, `EdgeToZoneId`, `RemainingMinutes`, `EntryEdgeType`), единичный `PendingOrder`, `LastMovedMinute`/`LastFoughtMinute`; `IsCombatEffective = Strength >= 0.30 * (InitialStrengthSnapshot > 0 ? InitialStrengthSnapshot : Strength)`; `Posture.Defend` по умолчанию.
2. `Order`/`EffectiveOrder`: move/hold/recon, `TargetZoneId`, `Deadline` (алиас `NotLaterThan`), `ForbiddenZoneId`, строковый `Outcome`, `CreatedMinute`/`SentMinute`/`DeliverAt`; `EffectiveOrder.From` переносит поля и сохраняет связь через `SourceOrderId`.
3. `Report`: `ObservedZoneId`, `ReportedOwnStrength` (`int?`), `ReportedEnemyStrength`, confidence 1..3 (сеттер и `HasRequiredFields`); recon допускает `ReportedOwnStrength == null`.
4. `PlayerKnowledge`/`KnowledgeUpdater`: append-only, без слияния версий и без изменения confidence; no-data/empty/occupied по факту донесения (приоритет `ObservedZoneId`); скрытая истина не хранится.
5. `GameState`: `HqZoneId`; `EndMinute`/`Seed`/`RngState` используются как v0.4-поля детерминизма; существующие списки — append-only лог.
6. `GameEvent`: новых типов не добавлено, изменений не потребовалось.
7. `Zone`/`CommanderTraits`/`IGameRules`: `CourierMinutes`; алиасы `Init`/`Pedantry`/`Honesty`; v0.4-поля курьера; порог 0.30 и MVP-константы в `IGameRules`.
8. `DomainInvariantsTests` переписан под v0.4; knowledge-часть `FogOfWarTests` — append-only (recon-часть оставлена для `T-58`); `TestContent` дополнен. Проверки extended-инвариантов (branches/cargo/contact geometry/casualty/enums) удалены.

## Изменённые/созданные файлы (только ownership)
- `Kriegsspiel.Core/Domain/{Unit,Order,EffectiveOrder,Report,GameState,Zone,CommanderTraits,Courier,PlayerKnowledge}.cs`
- `Kriegsspiel.Core/Contracts/IGameRules.cs` (`IKnowledgeUpdater.cs`, `GameEvent.cs` — без изменений)
- `Kriegsspiel.Core/Arbitration/KnowledgeUpdater.cs`
- `Kriegsspiel.Core.Tests/{DomainInvariantsTests,FogOfWarTests}.cs`, `Kriegsspiel.Core.Tests/Fixtures/TestContent.cs`
- `docs/projectDocs/reports/handoffs/implementer-T-50-r1.md` (process-артефакт)
- Вне ownership: `AGENTS.md`, `docs/gameDocs/rules.md`, `todo.md`, чужие файлы/проекты — не менялись (проверено `git diff --name-only 90acbe6..16d1eda`).

## Проверки (evidence refs)
- Pre-merge (detached @ `55bf554`, worktree `/tmp/opencode/ks-verifier-T-50-r1`):
  - `dotnet build Kriegsspiel.slnx` → 0 Warning(s), 0 Error(s).
  - `dotnet test ... --filter "FullyQualifiedName~DomainInvariantsTests|FullyQualifiedName~FogOfWarTests"` → 34/34 passed.
  - `dotnet test Kriegsspiel.slnx` → 151/151 passed.
- Post-merge (ветка `verifier/T-50-r1`, контекст волны, merge `2c0ee5e`):
  - `dotnet build Kriegsspiel.slnx` → 0 Warning(s), 0 Error(s).
  - T-50 filter → 34/34 passed.
  - `dotnet test Kriegsspiel.slnx` → 151/151 passed.

## Верификация (verifier)
- **Вердикт: verified.**
- Соответствие `rules.md` v0.4 (§1/§3/§4/§6/§7): подтверждено по коду и тестам; активных механик сверх v0.4 нет (extended-члены сохранены только для промежуточной зелёной сборки, MVP-путём не читаются).
- Ownership соблюдён; несанкционированных изменений нет.
- Новых проектов/объявлений `class|record|struct|interface|enum` в diff нет; `*/Mvp/*` нет.
- Тесты: старые extended-инварианты удалены/переписаны; «пакетного» сохранения старых тестов нет; новых тестовых классов не создано.
- Техдолга перехода нет (новых `TD-*`; `tech-debt.md` не менялся).
- Процесс: работа в task-ветке, коммиты с атрибуцией `implementer`; в чужие ветки/`master` коммитов нет (`16d1eda` содержит только `task/T-50-domain-v04`).
- **Merge в ветку волны:** выполнен per-task (без batch): `task/T-50-domain-v04` → `verifier/T-50-r1` (`--no-ff`, merge `2c0ee5e`); `wave/5-v04-foundation` переведена fast-forward на `2c0ee5e`.
- **Post-merge в контексте волны:** сборка/фильтр T-50/full suite → PASS (см. выше).

## Процесс (трение)
- Неоднозначности: `todo.md:56` фиксирует «статус — строковый `Outcome`», при этом карточка (`todo.md:165`) перечисляет `Executed/Distorted/FailedRejected`; реализация строкой корректна (новый enum запрещён).
- Лишние поиски: gate-отчёт `verifier-gate-mvp-transition-r3.md` лежит в `master`, но не в базе ветки волны (`90acbe6`) — пришлось читать из primary checkout; на проверку не влияет.
- Оценка доп. затрат: низко (диff небольшой, проверки воспроизводимы).
- Предложения: при фиксации MVP-констант в `IGameRules` сверять имена констант с каноном, чтобы исключить «мёртвые»/двусмысленные имена.

## Заметки / tech-debt
- TD нет.
- Наблюдение (не блокирует, вне активного пути): `IGameRules.MinTravelTimeMinutes = 5` добавлен, но не используется и не соответствует `rules.md` (минимум 5 задан только для `T_leg`/`T_leg_back` — там отдельный `MinCourierLegMinutes`). На поведение v0.4 не влияет; учесть при `T-53`, чтобы не применить минимум к фактическому времени ребра.
