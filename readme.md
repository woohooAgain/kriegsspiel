# Kriegsspiel

Консольная симуляция тактического командования (штабной крігсшпиль) на .NET 10. Игрок управляет подразделениями через приказы и запросы с задержками, туманом войны, донесениями и оценкой выполнения задач.

Правила игры (канон) — `docs/gameDocs/rules.md`. Рабочие документы разработки — `docs/projectDocs/`.

## Проекты решения

- `Kriegsspiel.Core` — доменная модель, арбитраж/резолверы, контракты, контент, персистентность, оценка.
- `Kriegsspiel.AI` — LLM/AI-адаптеры и наррация (опционально, с fallback).
- `Kriegsspiel.Console` — консольный shell (Spectre.Console), меню, сохранения.
- `Kriegsspiel.Core.Tests` — модульные и интеграционные тесты (xUnit).

Решение: `Kriegsspiel.slnx`.

## Требования

- .NET SDK 10.0+

## Сборка и тесты

```bash
dotnet build Kriegsspiel.slnx
dotnet test Kriegsspiel.slnx
```

## Запуск

```bash
dotnet run --project Kriegsspiel.Console/Kriegsspiel.Console.csproj
```

Параметры можно передавать через `Kriegsspiel.Console/appsettings.json` или аргументы командной строки (поддерживается `--Scenario`, `--ContentRoot`, `--Seed`, `--UseLlm` и др.):

```bash
dotnet run --project Kriegsspiel.Console/Kriegsspiel.Console.csproj -- --Scenario test_01 --UseLlm false
```

### Конфигурация

`Kriegsspiel.Console/appsettings.json`:

- `ContentRoot` — путь к каталогу контента (по умолчанию `Content`).
- `Scenario` — идентификатор сценария из `Content/scenarios/<id>.json`.
- `Seed` — seed детерминизма (опционально).
- `UseLlm` — включение LLM-режима (`false` по умолчанию, есть fallback).
- `Llm` — параметры провайдера/схемы (для будущего HTTP-клиента; при отсутствии сервера используется fallback).

Контент сценариев/таблиц: `Content/` (`friction.json`, `reports.json`, `scenarios/*.json`).

### Меню консоли

`ОБСТАНОВКА`, `КАРТА`, `ПРИКАЗ`, `ЗАПРОС`, `ЖУРНАЛ`, `ВРЕМЯ`, `СОХРАНИТЬ`, `ЗАГРУЗИТЬ`, `ВЫЙТИ`.

Сохранения записываются в каталог `saves/` (игнорируется git).

## Структура (кратко)

- `Kriegsspiel.Core/Domain` — сущности и состояние игры.
- `Kriegsspiel.Core/Arbitration` — арбитр, резолверы (передача, понимание, исполнение, бой, разведка и др.).
- `Kriegsspiel.Core/Contracts` — интерфейсы ядра.
- `Kriegsspiel.Core/Content` — загрузка/валидация контента.
- `Kriegsspiel.Core/Persistence` — DTO и сериализация состояния.
- `Kriegsspiel.Core/Reporting` — наррация и формирование донесений.
- `Kriegsspiel.Core/Assessment` — оценка/итоги.
- `Kriegsspiel.Core/Ai` — детерминированные AI-пайплайны и контракты LLM.
- `Kriegsspiel.AI` — реализации LLM-адаптеров.
- `Kriegsspiel.Console/Commands` — команды/рендеры UI.
- `Kriegsspiel.Console/Startup` — DI и игровой цикл.
- `Content/` — игровые данные и сценарии.
- `docs/` — правила и проектная документация.

Подробная карта — `docs/projectDocs/structure.md`.
