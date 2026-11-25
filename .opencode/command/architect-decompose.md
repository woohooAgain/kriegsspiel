---
description: Run the architect role (decomposition, todo.md plan, wave scenario/launch plan, triage).
agent: architect
subtask: true
---

Режим (decompose | plan-fix | triage | create-wave-branch): $1
Цель/контекст и refs (FS-*, база, ограничения): $ARGUMENTS

Действуй как `architect`. Загрузи нужные скиллы (`process-contract`, `status-envelope`, `task-decomposition`, `gate-checklist`, при необходимости `tech-debt-intake`). `todo.md` правится только в отдельной ветке по спецзапросу. Верни только Status Envelope + refs.
