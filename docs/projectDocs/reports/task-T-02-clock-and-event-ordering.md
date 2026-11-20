# Отчёт задачи T-02 — Clock and event ordering

- **T-XX:** `T-02`
- **Волна:** Wave 0
- **Ветка задачи:** `task/T-02-clock-and-event-ordering`
- **Целевая ветка MR:** `master`
- **MR:** не создан — в репозитории не настроен remote; интеграция выполнена через `wave0-integration`
- **Раунды implementer:** 1 (`894054a`)

## Выполнено (по пунктам «Что сделать»)
- Ключ планирования заменён на `ScheduledAt(Minute, Phase, Sequence)` с `IComparable`; `EventPhase` зафиксирован в порядке `Delivery=0 … Scheduled=6`.
- `Schedule` в прошлое бросает `ArgumentOutOfRangeException`; обработанная фаза текущей минуты — `InvalidOperationException`; silent-clamp удалён. Sequence монотонный, детерминированный, с уникальным tiebreaker в `PriorityQueue`.
- `AdvanceTo(target >= Now)` возвращает события строго по `(minute, phase, sequence)` и не теряет события `Now`; `target < Now` отклоняется сразу.
- `PendingCount`, `HasPendingEvents`, `PendingKeys` добавлены; 2-аргументный `Schedule(GameEvent, int)` сохранён как compatibility-overload с маппингом событий в фазы.
- `ClockTests`: 12 тестов — порядок минут/фаз/sequence, резервирование sequence, запрет прошлого, `AdvanceTo(Now)`, `PendingCount`, маппинг overload.

## Изменённые/созданные файлы (только ownership)
- `Kriegsspiel.Core/Domain/Clock.cs`
- `Kriegsspiel.Core.Tests/ClockTests.cs`

## Проверки (из «Как проверить»)
- `dotnet build Kriegsspiel.slnx` — **Build succeeded**, `0 Warning(s)`, `0 Error(s)`.
- `dotnet test Kriegsspiel.Core.Tests/Kriegsspiel.Core.Tests.csproj --no-build` — **Passed 46, Failed 0, Skipped 0** (в т.ч. `ClockTests`).
- Статически: баланс скобок, `git diff --check`, ownership — без замечаний; вердикт verifier подтвердил осмысленность assertion'ов.

## Верификация (verifier)
- Вердикт: `verified` (обеих итераций замечаний по T-02 не было); финальный verifier волны подтвердил без регрессий.
- Merge: `wave0-integration` (`49d234b`), затем fast-forward в `master` (`23cc87d`), 2026-11-20. Post-merge сборка, тесты и smoke-запуск консоли успешны.

## Заметки / tech-debt
- Нет собственных находок; связанные записи волны — в `wave-0-report.md`.
