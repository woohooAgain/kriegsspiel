---
name: feature-spec
description: Use when the analyst writes or updates a Kriegsspiel FS-* feature spec in docs/gameDocs/featureSpecs; defines statuses, template, readiness criteria and scope boundaries.
---

# Feature spec (FS-*)

Каталог: `docs/gameDocs/featureSpecs/`. `FS-*` — вход для `architect`; не заменяет `todo.md` и не является каноном правил.

## Статусы
`draft | in_review | ready_for_decomposition | blocked`.

Готовность к декомпозиции: цель и поведение понятны, scope/non-scope определены, неоднозначности разрешены или эскалированы, есть проверяемые критерии, `Открытые вопросы: Нет`.

## Именование
`FS-<id>-<kebab-slug>.md` (id стабилен).

## Шаблон
```md
# FS-<id> — <название>

- **Статус:** draft | in_review | ready_for_decomposition | blocked
- **Источник:** `docs/gameDocs/featureBacklog.md` #… / запрос …
- **Связанные правила:** `docs/gameDocs/rules.md §…` (или «нет/не применимо»)
- **Владелец спеки:** analyst

## Цель и ценность
## Бизнес-поведение и сценарии
## Scope / Non-scope
- **In scope:** …
- **Out of scope:** …
## Разрешённые неоднозначности и решения
- (вопрос → решение → основание/ссылка; если требует правки `rules.md` — эскалация, статус blocked)
## Критерии готовности к декомпозиции / приёмки (бизнес)
- [ ] …
## Открытые вопросы
- Нет (обязательно для `ready_for_decomposition`) / список с владельцем и блокером
## Примечания для architect
- Ограничения, зависимости, риски, предложения по нарезке (не `T-*`).
```

## Границы и эскалация
- Не менять `docs/gameDocs/rules.md`/`AGENTS.md`/`todo.md`; при необходимости изменения канона — предложение + отдельный запрос с подтверждением, статус `blocked`.
- Неразрешимые вопросы/противоречия — эскалация к `rules-auditor` через `orchestrator`; решения фиксировать в `FS-*`/артефакте.
