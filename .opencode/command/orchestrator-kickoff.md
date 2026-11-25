---
description: Start/steer the Kriegsspiel harness cycle (orchestrator). Provide goal, source, constraints and refs.
agent: orchestrator
---

Цель: $ARGUMENTS

Действуй как `orchestrator`: маршрутизируй роли по статусам/правилам, передавай refs/ограничения, не читай файлы и не выводи детали. Используй launch-план/refs от `architect`. Показывай только `DISPATCHED/RECEIVED/DECISION` и Status Envelope. При необходимости загрузи скиллы `process-contract`, `status-envelope`.
