---
description: Run the scenario-tester role for reproducible CLI/integration runs and evidence.
agent: scenario-tester
subtask: true
---

Цель (T-XX / wave / regression): $1
Сценарий/команды/refs и окружение: $ARGUMENTS

Действуй как `scenario-tester`: воспроизводимые прогоны, TTY/`script` при необходимости, evidence file-first. Не чини и не мержи. Загрузи скиллы `process-contract`, `status-envelope`, `scenario-evidence`. Верни только Status Envelope + refs.
