---
name: tech-debt-intake
description: Use when recording a product/code/doc finding that is outside the current Kriegsspiel task; defines TD-* deduplication, template, statuses, archiving and separation from PI-*.
---

# Tech-debt intake (TD-*)

Файлы: активные `open`/`planned` — `docs/projectDocs/tech-debt.md`; закрытые — `docs/projectDocs/tech-debt-archive.md`.

## Когда использовать
Найдена проблема/риск/баг/доработка **вне текущей задачи/объёма**, которую нельзя или нецелесообразно править сейчас. Маршрутизация типов заметок — `docs/projectDocs/workflows/README.md` (дерево решений): процесс → `PI-*`, новая возможность → `BL-*`/`FS-*`, дефект/улучшение реализованного → `TD-*`.

## Действия
1. Проверить дубликаты в активном списке и архиве.
2. Подготовить запись `TD-###` по шаблону: `Тип` (`bug|improvement|debt`), `Severity` (для `bug`), статус `open`, дата, контекст (`T-*`/`FS-*`/wave/triage), источник (файлы/символы/команды/refs), описание, риск, предлагаемые действия, связи (`rules.md §…`, `T-*`, `FS-*`).
3. При закрытии: `done` (или `wontfix` с причиной) + ссылки на `T-*`, MR/коммит, дата/волна. Активный список — в основном `open`/`planned`; `done`/`wontfix` — архивировать.
4. **Разграничение:** процессные фрикции/меры → `PI-*`/отчёты; продуктовые/кодовые/док-находки вне scope (включая баги/доработки реализованного) → `TD-*`; новые идеи/фичи → `featureBacklog.md` (`BL-*`, caretaker `analyst`).
5. Триаж/`planned` (`T-*`) — `architect`; вносить `open` может любой; закрытие — закрывшая роль + подтверждение `verifier`. Отдельный `BUG-*` не вводить, пока объём мал (PI-015/PI-016).
6. `todo.md` не править (кроме `architect` в отдельной ветке по спецзапросу).

## Статусы
`open | planned | done | wontfix`.

## Типы
`bug` (обязателен `Severity: low|medium|high`) | `improvement` | `debt`.

## Шаблон
```md
## TD-### — Краткое название
- **Тип:** bug | improvement | debt
- **Severity:** low | medium | high (для bug; иначе n/a)
- **Статус:** open | planned | done | wontfix
- **Дата:** YYYY-MM-DD
- **Контекст:** T-XX / декомпозиция / прогон сценария / wave / triage / …
- **Источник:** `путь/к/файлу`, символ/секция, команда/наблюдение
- **Описание:** что обнаружено (факты)
- **Риск / почему может выстрелить:** …
- **Предлагаемые действия:** …
- **Связи:** `docs/gameDocs/rules.md §…`, `T-*`, `docs/projectDocs/…`
- **Закрытие (для done/wontfix):** T-*, MR/коммит, дата/волна, причина
```
