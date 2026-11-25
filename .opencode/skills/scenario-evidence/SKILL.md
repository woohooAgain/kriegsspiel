---
name: scenario-evidence
description: Use when a role runs reproducible Kriegsspiel CLI/integration scenarios; defines deterministic setup, TTY handling, evidence refs and what counts as proof for verification.
---

# Scenario evidence (воспроизводимые прогоны)

## Правила
- Каждый прогон должен быть воспроизводим: зафиксировать seed, конфиг, сценарий, команды, окружение.
- Действовать в рамках сценария проверок волны `architect` (не расширять самовольно).
- Наблюдаемое поведение и результат `EXIT` фиксировать в артефакте (`docs/projectDocs/reports/handoffs/scenario-tester-<target>-r<N>.md` или согласованный ref).
- В ответ оркестратору — Status Envelope + refs; полные логи inline запрещены.

## TTY / Spectre.Console
- Интерактивный CLI может требовать TTY; при non-TTY использовать `script`/pseudotty.
- Если TTY недоступен — фиксировать как ограничение с доказательствами, а не как успешный прогон.
- Для UI/CLI-изменений (PI-002) обязателен минимум один воспроизводимый прогон каждого нового/изменённого интерактивного пути (конкретный пункт меню/ввод → наблюдаемый результат) отдельным evidence ref; model/probe-проверки его не заменяют.

## Минимальный набор evidence
- Команда(ы) запуска и seed/конфиг.
- Наблюдаемый вывод/состояние (ключевые строки, счётчики, `EXIT`).
- Ref на сохранённый артефакт/лог в ветке (не в `/tmp`, если нужен `verifier`).
- Ограничения окружения.

## Что считать провалом
- Расхождение с `rules.md`/`FS-*`/сценарием волны.
- Недостаток данных для проверки (тогда `blocked`/`partial`, без догадок).
