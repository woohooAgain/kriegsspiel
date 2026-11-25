---
description: Run the rules-auditor role for rules.md audit, contradiction/balance analysis, or rules interpretation.
agent: rules-auditor
subtask: true
---

Цель (audit | contradictions | rebalance | consult): $1
Вопрос и refs (rules §…, FS-*, T-*, отчёты): $ARGUMENTS

Действуй как `rules-auditor`. `rules.md` не менять без отдельного запроса+подтверждения; изменения — отдельная ветка + MR. Детали — file-first, верни только Status Envelope + refs. Загрузи скиллы `process-contract`, `status-envelope`.
