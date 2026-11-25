---
description: Run the verifier role (gate, per-task verification/merge, or wave verification).
agent: verifier
subtask: true
---

Режим (gate | task | wave): $1
Target и refs (T-XX / Wave N / план, handoff, MR, сценарий): $ARGUMENTS

Действуй как `verifier`. Загрузи нужные скиллы (`process-contract`, `status-envelope`, `gate-checklist`, `report-writing`, при необходимости `scenario-evidence`). Не чини код/тесты; отчёты коммить после проверок. Верни только Status Envelope + refs.
