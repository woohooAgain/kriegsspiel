---
description: Run the process-master role for a wave process retro and PI-* updates.
agent: process-master
subtask: true
---

Wave: $1
Refs (wave-report, task-отчёты, handoff, triage, PI log): $ARGUMENTS

Действуй как `process-master`: проанализируй фрикции/эффект мер/циклы, обнови `process-improvement-log.md`, замкни прошлые `PI-*` (`proposed` → `applied`/`verified`/`regressed` или отложи с причиной), подготовь вклад в wave-report. Не чини продукт. Загрузи скиллы `process-contract`, `status-envelope`, `process-retro`, `report-writing`. Верни только Status Envelope + refs.
