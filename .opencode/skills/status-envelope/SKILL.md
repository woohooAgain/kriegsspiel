---
name: status-envelope
description: Use when reporting to the orchestrator or writing handoffs in a Kriegsspiel role; defines the compact Status Envelope format, fields, and per-role status vocabularies.
---

# Status Envelope

Каждый ответ роли оркестратору — краткий блок (ориентир ≤10 строк), без деталей реализации/логов. Подробности — в артефактах по refs.

## Формат
```
ROLE=<role> | TARGET=<T-XX|Wave N|FS-*|PI-*|gate|...> | STATUS=<...> | ROUND=<n|n/a>
ARTIFACTS=<paths/worktree/branch/commit/MR refs>
NEXT=<следующий агент/действие или none>
BLOCKERS=<none|кратко>
SUMMARY=<1-3 короткие строки фактов/вердикта>
```

## Правила
- `STATUS` — только из словаря роли (ниже); не вводить новые значения молча.
- `ARTIFACTS` — refs (путь/worktree/ветка/MR/коммит), не содержимое. Для коммитящих запусков обязательны `worktree=<path>` и `branch=<own-branch>@<commit>` (`process-contract` §5.1).
- На доработках — дельта относительно прошлого раунда.
- Полные логи/диффы/копипасты/длинные списки замечаний запрещены.

## Словари статусов по ролям
- **orchestrator:** `dispatched | received | decision | blocked`.
- **analyst:** `ready_for_decomposition | draft | blocked | clarified`.
- **architect:** `planned | gate_ready | wave_branch_created | triage_done | blocked`.
- **implementer:** `ready_for_verifier | blocked | in_progress`.
- **verifier:** `gate_passed | gate_failed | verified | changes_requested | blocked | ready_to_merge | wave_passed | wave_failed`.
- **scenario-tester:** `passed | failed | blocked | partial`.
- **process-master:** `retro_done | actions_proposed | blocked`.
- **rules-auditor:** `audited | contradictions_found | rebalance_proposed | consulted | change_ready_for_mr | blocked`.
- **docs-maintainer:** `updated | no_change | blocked`.
- **repo-navigator:** `found | not_found | blocked`.

## Примеры
```
ROLE=implementer | TARGET=T-24 | STATUS=ready_for_verifier | ROUND=2
ARTIFACTS=worktree=/tmp/opencode/ks-implementer-T-24-r2; branch=task/T-24-scalable-knowledge-map@9f6c82a; docs/projectDocs/reports/handoffs/implementer-T-24-r2.md
NEXT=verifier | BLOCKERS=none
SUMMARY=Fixed menu filter reset; build/test green.
```
```
ROLE=verifier | TARGET=gate | STATUS=gate_failed | ROUND=1
ARTIFACTS=docs/projectDocs/todo.md#wave-5; refs gaps: base sync, ownership
NEXT=architect | BLOCKERS=plan base stale vs target base
SUMMARY=Gate failed before wave branch creation.
```
