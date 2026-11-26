# Handoff: T-50 — домен v0.4 (Unit/Order/Report/GameState/Knowledge), round 1

## Задача и refs
- **T-50**, Wave 5, покрывает `docs/gameDocs/rules.md` §1, §3, §4, §6, §7; карточка `docs/projectDocs/todo.md:140-181`; сценарий Wave 5 `docs/projectDocs/todo.md:94-103`.
- **Ветка волны (база):** `wave/5-v04-foundation` @ `90acbe6`.
- **Ветка задачи:** `task/T-50-domain-v04` @ `16d1eda`.
- **Worktree:** `/tmp/opencode/ks-implementer-T-50-r1`.
- **Код-коммит:** `16d1eda` (`feat(core): align domain, reports and knowledge with rules v0.4`).
- **MR:** не создан; передача по ref ветки/коммита, per-task merge выполняет `verifier`. Merge не выполнялся.
- **Ownership:** изменены только файлы из карточки T-50 + этот handoff. `AGENTS.md`, `docs/gameDocs/rules.md`, `todo.md`, чужие задачи/ветки не тронуты.

## Что сделано (v0.4)
- **Unit:** добавлена edge-локация (`IsOnEdge`, `EdgeFromZoneId`, `EdgeToZoneId`, `RemainingMinutes`, `EntryEdgeType`), единичный `PendingOrder` (`Order?`), `LastMovedMinute`/`LastFoughtMinute`; `IsCombatEffective` = только `Strength >= 0.30 * (InitialStrengthSnapshot > 0 ? InitialStrengthSnapshot : Strength)`; `Posture.Defend` используется как defense; расширенные члены оставлены для промежуточной сборки и v0.4-путём не читаются.
- **Order/EffectiveOrder:** добавлены v0.4 `Deadline` (алиас существующего `NotLaterThan`), `ForbiddenZoneId`, строковый `Outcome`, `CreatedMinute`/`SentMinute`/`DeliverAt`; `EffectiveOrder.From` переносит эти поля и сохраняет связь `SourceOrderId`. Поддержаны `Move`/`Hold`/`Recon`, target/deadline/forbidden/outcome. Ветви/ограничения/режимы движения не используются новыми v0.4-проверками.
- **Report:** добавлены `ObservedZoneId`, `ReportedOwnStrength` (`int?`), `ReportedEnemyStrength`; `Confidence` ограничен 1..3 (сеттер и `HasRequiredFields`); `Create` принимает v0.4-поля; recon-донесение допускает `ReportedOwnStrength == null`.
- **PlayerKnowledge/KnowledgeUpdater:** append-only — каждое доставленное донесение добавляет новую версию, confidence не повышается/не понижается, версии не сливаются по факту/значению; зона различает NoData/Empty/Occupied по факту донесения (с приоритетом `ObservedZoneId`); скрытая истина не хранится.
- **GameState:** добавлен `HqZoneId`; `EndMinute`/`Seed`/`RngState` используются как v0.4-поля детерминизма; существующие списки (`EventLog`, `DeliveredReports`, `InTransitReports`, `LostReports`) остаются append-only логом.
- **Zone/CommanderTraits/Courier/IGameRules:** добавлены `CourierMinutes`; алиасы `Init`/`Pedantry`/`Honesty`; v0.4-поля курьера (`SentMinute`, `DeliverAt`, `ReturnAt`, `LossReturnAt`, `TLeg`, `TLegBack`); порог 0.30 и MVP-константы (min T_leg 5, окно recon 30, edgePenalty 0.15, honesty 0.12, offroad −5, rest +5) в `IGameRules`.
- **GameEvent:** новых record/enum/class-типов не добавлено; MVP-семантика выражается существующими событиями/`TurnResult`/AAR (изменений не потребовалось).
- **Тесты:** `DomainInvariantsTests` переписан под v0.4 (threshold, edge, PendingOrder, order/effective outcome, report confidence, append-only knowledge, append-only state, traits/zone/courier/contracts); knowledge-часть `FogOfWarTests` переписана на append-only; recon-часть оставлена для T-58. `TestContent` дополнен v0.4-полями. Проверки extended-инвариантов (branches/cargo/contact geometry/casualty) удалены.

## Изменённые файлы (ownership)
- `Kriegsspiel.Core/Domain/Unit.cs`, `Order.cs`, `EffectiveOrder.cs`, `Report.cs`, `GameState.cs`, `Zone.cs`, `CommanderTraits.cs`, `Courier.cs`, `PlayerKnowledge.cs`
- `Kriegsspiel.Core/Contracts/IGameRules.cs`, `IKnowledgeUpdater.cs` (без изменений — контракт сохранён)
- `Kriegsspiel.Core/Arbitration/KnowledgeUpdater.cs`
- `Kriegsspiel.Core.Tests/DomainInvariantsTests.cs`, `FogOfWarTests.cs`, `Fixtures/TestContent.cs`
- `docs/projectDocs/reports/handoffs/implementer-T-50-r1.md` (этот handoff)

## Проверки (команда → результат)
- `dotnet build Kriegsspiel.slnx` → **PASS** (0 Warning(s), 0 Error(s)).
- `dotnet test Kriegsspiel.Core.Tests/Kriegsspiel.Core.Tests.csproj --filter "FullyQualifiedName~DomainInvariantsTests|FullyQualifiedName~FogOfWarTests"` → **PASS** (34/34 passed, 0 failed, 0 skipped).
- `dotnet test Kriegsspiel.slnx` → **PASS** (151/151 passed, 0 failed, 0 skipped).
- Проверка типов: `git diff` по коду не содержит новых объявлений `class|record|struct|interface|enum`; новых проектов/путей `*/Mvp/*` нет.

## Утверждения T-50 (закрытие)
- `IsCombatEffective` false при `strength < 0.30*initial`, true при равенстве; morale/readiness/ammo не влияют.
- Edge-локация хранит `RemainingMinutes`/`EntryEdgeType`; `PendingOrder` единичный и перезаписывается.
- `Order`/`EffectiveOrder` поддерживают move/hold/recon + target/deadline/forbidden/outcome и связь через `SourceOrderId`.
- `Report` confidence 1..3, обязательные поля валидируются, own strength может быть null для recon.
- Knowledge хранит все версии без слияния и не меняет confidence; GameState использует существующие списки как append-only лог.
- Новых типов событий/классов нет.

## Отклонения/решения
- `Outcome` реализован строкой, как зафиксировано в `todo.md` («статус — строковый `Outcome`»), чтобы не вводить новый enum.
- Extended-члены (`LocationZoneId`, `Morale`, `Fatigue`, branches/constraints/movement mode и т.д.) сохранены только для промежуточной зелёной сборки; v0.4-проверками они не читаются. Удаление — по плану Wave 8/T-65.
- Recon-часть `FogOfWarTests` не переписывалась (вне knowledge-scope T-50; T-58).
- `GameEvent.cs` не менялся: новые типы запрещены, существующая семантика достаточна.

## TD / процесс
- Новых `TD-*` нет; задача закрывается без техдолга.
- Процессное трение: низкое; основной объём — переписать domain-контракты и knowledge-тесты. Полный `dotnet test` зелёный на момент handoff, что снимает риск переноса поломок в Wave 5.

## Для verifier
- Проверить diff `90acbe6..16d1eda` по ownership и отсутствию новых объявлений типов.
- Повторить: `dotnet build Kriegsspiel.slnx`; `dotnet test ... --filter "FullyQualifiedName~DomainInvariantsTests|FullyQualifiedName~FogOfWarTests"`; `dotnet test Kriegsspiel.slnx`.
- Merge задачи — в `wave/5-v04-foundation` (per-task), затем запуск сценария Wave 5.
