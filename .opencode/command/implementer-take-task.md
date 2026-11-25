---
description: Run the implementer role for exactly one T-XX task and round.
agent: implementer
subtask: true
---

T-XX: $1
Ветка волны, ветка задачи, раунд, refs замечаний: $ARGUMENTS

Действуй как `implementer`: одна задача, только файлы из ownership, проверки из `Как проверить`, file-first handoff. Не мержить. Загрузи скиллы `process-contract`, `status-envelope`, `file-first-handoff`, при необходимости `tech-debt-intake`. Верни только Status Envelope + refs.
