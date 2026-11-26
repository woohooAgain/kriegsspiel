---
name: code-navigation-lsp
description: Use before navigating Kriegsspiel C# code/symbols when LSP tools are available; defines LSP-first lookup order, fallback for docs/config, sufficient-result evidence and the no-guessing rule.
---

# Code navigation: LSP-first

Применяется к навигации по коду и символам (C#/проект), когда в харнессе доступны LSP-инструменты. Обратной силы нет: правило действует для новых запусков, старые артефакты не переделываются.

## Порядок
1. Для кода/символов сначала LSP: go-to-definition, find-references, document/workspace symbols, diagnostics, hover/signature.
2. Затем `read` найденных файлов по точному `file:line` из результата LSP.
3. `grep`/`glob`/`read` — fallback, если LSP не дал результата/неприменим, и по умолчанию для документации, конфигов, markdown, сценариев/данных. Не использовать их для навигации по коду, когда доступен LSP.

## Достаточный результат
- Символ подтверждён LSP: определение и/или использования с `file:line`; при правках — diagnostics по затронутым файлам.
- Перед правкой символа найти его определение и все использования (find-references); после — проверить diagnostics.
- В handoff/отчёте указывать refs: символ → `file:line`; если применимо — итог diagnostics.

## Запреты
- Не угадывать расположение символа и не «перебирать» файлы/каталоги через `glob`/`grep`/`git ls-files`, когда доступен LSP.
- Не выдавать чтение файла/каталога за LSP-подтверждение.
- Если LSP недоступен/не отвечает — зафиксировать это как ограничение и использовать fallback (`grep`/`glob`/`read`); не маскировать догадку под LSP-результат.

## Границы
- Правило не меняет ownership, ветвление, формат Status Envelope и ролевые статусы; процессные инварианты — в `process-contract`.
