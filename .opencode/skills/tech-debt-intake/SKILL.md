---
name: tech-debt-intake
description: Use when recording a product/code/doc finding that is outside the current Kriegsspiel task; defines TD-* deduplication, template, statuses, archiving and separation from PI-*.
---

# Tech-debt intake (TD-*)

Файлы: активные `open`/`planned` — `docs/projectDocs/tech-debt.md`; закрытые — `docs/projectDocs/tech-debt-archive.md`.

## Когда использовать
Найдена проблема/риск **вне текущей задачи/объёма**, которую нельзя или нецелесообразно править сейчас.

## Действия
1. Проверить дубликаты в активном списке и архиве.
2. Подготовить запись `TD-###` по шаблону: статус `open`, дата, контекст (`T-*`/`FS-*`/wave/triage), источник (файлы/символы/команды/refs), описание, риск, предлагаемые действия, связи (`rules.md §…`, `T-*`, `FS-*`).
3. При закрытии: `done` (или `wontfix` с причиной) + ссылки на `T-*`, MR/коммит, дата/волна. Активный список — в основном `open`/`planned`; `done`/`wontfix` — архивировать.
4. **Разграничение:** процессные фрикции/меры → `PI-*`/отчёты; продуктовые/кодовые/док-находки вне scope → `TD-*`.
5. `todo.md` не править (кроме `architect` в отдельной ветке по спецзапросу).

## Статусы
`open | planned | done | wontfix`.

## Шаблон
```md
## TD-### — Краткое название
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
