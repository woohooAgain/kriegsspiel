# Отчёт задачи T-24 — Scalable knowledge-map console view (overview/detail, topology, units)

- **T-XX:** T-24 (FS-005, Wave 4)
- **Волна / ветка волны:** Wave 4 / `wave/4-large-maps-console-display` @ `1156fba`
- **Ветка задачи / MR:** `task/T-24-scalable-knowledge-map` @ `6084efb`; MR не создан (нет remote/gh), передача по ref ветки и commit
- **Раунды:** handoff `docs/projectDocs/reports/handoffs/implementer-T-24-r1.md` (r1, в коммите задачи)
- **Финализировал и закоммитил отчёт (verifier, после проверок):** verifier, r1, после per-task проверок; статус `changes_requested` (merge не выполнялся)

## Выполнено (по пунктам «Что сделать»)
- Реализовано масштабируемое топологическое представление: компактный обзор (сводка/страницы по 12 зон/фильтры/поиск), деталь зоны (знание §6, обе версии, соседи с типом ребра и временем), сводка своих подразделений по зонам; источники данных разделены.
- Запрет утечки соблюдён: `PresentUnitIds` в файлах владельца не используется, противник выводится только из `PlayerKnowledge`.
- Обнаружен функциональный дефект в навигации: пункт фильтра «Синие по донесениям» не применяет фильтр по фракции Blue (см. «Верификация»). Остальные заявленные режимы и фильтры подтверждены.

## Изменённые/созданные файлы (только ownership)
- `Kriegsspiel.Console/Commands/MapCommand.cs` (M)
- `Kriegsspiel.Console/Commands/MapRenderer.cs` (M)
- `docs/projectDocs/reports/handoffs/implementer-T-24-r1.md` (A)
- Проверка `git diff --name-status 1156fba..6084efb`: других файлов нет; `todo.md`, `rules.md`, `AGENTS.md`, файлы T-25 не тронуты; `git diff --check` чист.

## Проверки (evidence refs)
- `dotnet build Kriegsspiel.slnx` в worktree задачи: PASS (0 warning, 0 error).
- `dotnet test Kriegsspiel.slnx --no-build` в worktree задачи: PASS, 145/145.
- `git grep -n "PresentUnitIds" 6084efb -- <файлы владельца>`: совпадений нет (exit 1).
- Throwaway probe implementer (22 проверки, `/tmp/opencode/t24-probe/Program.cs`) повторён verifier: `PROBE_OK` (знание §6/обе версии, топология, фильтры на уровне `MapFilter`, масштаб 100 зон, отсутствие скрытого имени противника).
- CLI `test_01` → `КАРТА` (лог implementer `/tmp/opencode/t24-cli-test01-final.log`) и `river_line_10x10` (`/tmp/opencode/t24-cli-river-r2.log`): заявленные сценарии обзора/детали/поиска/сводки воспроизведены в handoff.
- Дополнительная CLI-проверка verifier выбора «Синие по донесениям»: `/tmp/opencode/t24-verifier-blue-filter.log` → FAIL (фильтр не применяется; см. ниже).

## Верификация (verifier)
- Вердикт: **changes requested** (r1), ROUND=1.
- Замечания (дельта от r1 отсутствует, это первый раунд):
  1. `Kriegsspiel.Console/Commands/MapCommand.cs:155` предлагает пункт `Синие по донесениям`, но в switch на строке 162 литерал `Синые по донесениям` (опечатка: `ы` вместо `и`). Пункт попадает в ветку `_` и сбрасывает фильтр к `MapFilter.Default` вместо `Faction = Faction.Blue`. Воспроизведение: `test_01` → `КАРТА` → `ФИЛЬТР` → `Синие по донесениям`: ожидается `Фильтр: сторона=синие` и 0 зон (синего знания по донесениям в сценарии нет); фактически `Фильтр: все зоны`, 2 зоны (`/tmp/opencode/t24-verifier-blue-filter.log`). Противоречит п. 2 «Что сделать» и п. 1 «Готово, если» карточки (сужение по принадлежности/кем по донесениям), а также утверждению handoff о работоспособности фильтров по фракции.
  - Ожидаемая дельта implementer: исправить литерал в `PromptFilter` на точное совпадение с пунктом меню, повторить CLI-проверку синего фильтра и приложить новый handoff `implementer-T-24-r2.md` с результатом; остальные файлы не трогать.
- Merge в ветку волны: **не выполнен** (по процессу до `verified`).
- Post-merge в контексте ветки волны: n/a (merge не производился).

## Процесс (трение)
- Неоднозначности: нет (карточка `docs/projectDocs/todo.md:648-674`, спека `FS-005` дают однозначные критерии).
- Лишние поиски и причина: низкие; дополнительный CLI-прогон потребовался для проверки заявленной implementer работоспособности фильтра по фракции — probe проверял `MapFilter` напрямую и не покрывал путь `PromptFilter`.
- Оценка доп. затрат: низко (build/test/grep + один CLI-прогон; дефект найден статически и подтверждён CLI).
- Предложения по улучшению: в handoff для UI-фич различать проверки модели (`MapFilter`) и проверки пути ввода/меню (`PromptFilter`).

## Заметки / tech-debt
- TD-* новых нет; продуктовый код и тесты verifier не изменялись.
