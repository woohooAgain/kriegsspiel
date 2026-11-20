# Отчёт задачи T-03 — Content profile and validation

- **T-XX:** `T-03`
- **Волна:** Wave 0
- **Ветка задачи:** `task/T-03-content-profile-and-validation`
- **Целевая ветка MR:** `master`
- **MR:** не создан — в репозитории не настроен remote; интеграция выполнена через `wave0-integration`
- **Раунды implementer:** 1 (`fde7313` — схема/валидация/контент), 2 (`89f0558` — ControlStateWeights и тест), 3 (`ff8090a` — совместимость с Arbitration/ContentLoader), 4 (`7e72d01`, `1aae871` — исправления `ContentLoaderTests`)

## Выполнено (по пунктам «Что сделать»)
- `FrictionTable` и модели контента расширены под параметры §12: курьеры, `P0/Pmin/Pmax`, веса A, режимы передвижения, бой/мораль/пленение/гражданские/снабжение/разведка, модификаторы и профили сложности.
- `Scenario`/`Objective`/`ScenarioBundle`: seed, порог победы, типы рёбер, объекты зон, цели с контрольными минутами/весами и состояниями контроля.
- `ContentLoader`: контролируемая `ContentValidationException` — probabilities ∈ [0,1], веса целей = 1, `couriers >= 1`, уникальные id, симметричные/валидные рёбра, enum'ы, времена > 0, неотрицательные коэффициенты. По §11 веса состояний контроля `1/0.5/0` не требуют суммы 1 (исправлено в раунде 2).
- `ReportTemplates` и `reports.json`: placeholder'ы времени события/доставки/источника/уверенности/типа/зоны/юнита/чисел, ключ `Impossible`; JSON-контент и `TestContent` обновлены.
- `ContentLoaderTests`: позитивные кейсы и негативные на каждую ключевую валидацию, включая регрессионный тест весов 1.5.

## Изменённые/созданные файлы (только ownership)
- `Kriegsspiel.Core/Content/{ContentLoader,Scenario,ScenarioBundle,Objective,FrictionTable,ReportTemplates}.cs`
- `Content/friction.json`, `Content/reports.json`, `Content/scenarios/{crossing_01,test_01}.json`
- `Kriegsspiel.Core.Tests/Fixtures/TestContent.cs`
- Новый: `Kriegsspiel.Core.Tests/ContentLoaderTests.cs`

## Проверки (из «Как проверить»)
- `python3 -m json.tool` — все 4 JSON-файла валидны.
- `dotnet build Kriegsspiel.slnx` — **Build succeeded**, `0 Warning(s)`, `0 Error(s)`.
- `dotnet test Kriegsspiel.Core.Tests/Kriegsspiel.Core.Tests.csproj --no-build` — **Passed 46, Failed 0, Skipped 0** (в т.ч. `ContentLoaderTests`).

## Верификация (verifier)
- Вердикт: `changes requested` (раунд 1) → `verified` (раунд 2) → `verified` (финальный раунд после runtime-фиксов).
- Замечания/раунды: default `ControlStateWeights` = 1.5 отвергался проверкой суммы = 1; исправлено (`89f0558`). После установки .NET исправлены совместимость и десериализация/тесты контента (`ff8090a`, `7e72d01`, `1aae871`).
- Merge: `wave0-integration` (`6879aab`, `23cc87d`), затем в `master`; финальные фиксы тоже слиты в `master`, 2026-11-20. Post-merge сборка, тесты и запуск консоли успешны.

## Заметки / tech-debt
- `TD-005` (GameStateFactory не пробрасывает `Scenario.Seed`/`EndMinute`).
