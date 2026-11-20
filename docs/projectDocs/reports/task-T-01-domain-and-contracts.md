# Отчёт задачи T-01 — Domain and contracts

- **T-XX:** `T-01`
- **Волна:** Wave 0
- **Ветка задачи:** `task/T-01-domain-and-contracts`
- **Целевая ветка MR:** `master`
- **MR:** не создан — в репозитории не настроен remote; интеграция выполнена через `wave0-integration`
- **Раунды implementer:** 1 (`cecd2da` — домен и контракты), 2 (`2e9b274` — удалён дубль значения `ReportKind`), 3 (`db7a076` — снят `required` с коллекций `GameState`), 4 (`b226f81` — `Zone.Cover: double`, исправления `PlayerKnowledge`)

## Выполнено (по пунктам «Что сделать»)
- Введена `Faction` и использована в доменных моделях; `CommanderTraits` расширен до 5 параметров 0–3.
- `Unit`: training/experience/маскировка/внимание/движение/охрана/плен/NoPrisonersPolicy/ссылка на эффективный приказ; `IsCombatEffective` по формуле §7/§11 с fallback при нулевом снимке.
- `Zone`: `EdgeType`, `ZoneObject`, `Cargo`, `ControllingFaction`, `Cover`; выделены `ZoneObject`, `MovableObject`, `CivilianGroup`, `EffectiveOrder`, `Courier`, `Cargo`, `ContactGeometry`, `PendingCasualty`.
- `Order`: 7 слотов, `OrderVersion`, ветви ≤3, `MovementMode`, `OrderConstraint`, цели и дедлайны; namespace `Kriegsspiel.Core.Orders` сохранён.
- `Report`: 4 обязательных поля, алиасы старых свойств, `FactKey`/`Facts`, `ReportKind.Impossible`; `PlayerKnowledge`: no-data vs empty, два timestamp, уверенность 0–3, обе версии при противоречии.
- `GameEvent`: добавлены query/edge/camouflage/guard/pending/report/order-события; `GameState`: `InTransitReports`, пул курьеров, `Seed`/RNG state, `PendingCasualties`.
- Контракты `IReportFactory`, `IKnowledgeUpdater`, `IRandomSource`, `IGameRules`; добавлен `DomainInvariantsTests`.

## Изменённые/созданные файлы (только ownership)
- `Kriegsspiel.Core/Domain/{CommanderTraits,Unit,Zone,Order,Report,PlayerKnowledge,GameEvent,GameState}.cs`
- Новые: `Kriegsspiel.Core/Domain/{ZoneObject,MovableObject,CivilianGroup,EffectiveOrder,Courier,Cargo,ContactGeometry,PendingCasualty}.cs`
- Новые: `Kriegsspiel.Core/Contracts/{IReportFactory,IKnowledgeUpdater,IRandomSource,IGameRules}.cs`
- Новый: `Kriegsspiel.Core.Tests/DomainInvariantsTests.cs`

## Проверки (из «Как проверить»)
- `dotnet build Kriegsspiel.slnx` — **Build succeeded**, `0 Warning(s)`, `0 Error(s)`.
- `dotnet test Kriegsspiel.Core.Tests/Kriegsspiel.Core.Tests.csproj --no-build` — **Passed 46, Failed 0, Skipped 0** (в т.ч. `DomainInvariantsTests` — 23 теста).
- Статически: уникальность значений enum, баланс скобок, using/namespace, отсутствие комментариев, `git diff --check` — без замечаний.

## Верификация (verifier)
- Вердикт: `changes requested` (раунд 1) → `verified` (раунд 2) → `verified` (финальный раунд после runtime-фиксов).
- Замечания/раунды: дубль `QueryAnswer = 6` / `QueryResponse = 6`; закрыто удалением `QueryResponse` (`2e9b274`). После установки .NET найдены и исправлены CS9035 в `GameState`, неверный тип `Zone.Cover` и логика `PlayerKnowledge` (`db7a076`, `b226f81`).
- Merge: выполнен в `wave0-integration` (`2305abe`, `594150a`), затем в `master`; финальные фиксы тоже слиты в `master`, 2026-11-20. Post-merge сборка, тесты и запуск консоли успешны.

## Заметки / tech-debt
- `TD-001` (AmbiguityScorer), `TD-002` (CombatResolver/ENEMY_), `TD-003` (Impossible ⇒ ReportBy), `TD-004` (deliveryMinute = ∞).
