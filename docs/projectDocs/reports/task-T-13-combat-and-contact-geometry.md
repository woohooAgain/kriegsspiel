# Отчёт задачи T-13 — Combat and contact geometry

- **T-XX:** T-13
- **Волна / ветка волны:** Wave 1 / `wave/1-core-mechanics`
- **Ветка задачи:** `task/T-13-combat-and-contact-geometry`
- **Целевая ветка MR:** `wave/1-core-mechanics`
- **MR:** локальный merge (remote/MR отсутствует)
- **Раунды:** 1 (реализация в task-ветке) + верификация/merge в ветку волны
- **Финализировал и закоммитил отчёт (verifier, после проверок):** агент (полный цикл), 2026-11-20

## Выполнено (по пунктам «Что сделать»)
- Добавлен `ContactGeometryResolver` (определение front/flank/rear по рёбрам/вниманию/движению с fallback при неоднозначности).
- Переработан `CombatResolver` по формулам/правилам §7–§8 в рамках доменных контрактов: геометрия/позиция/terrain/коллизии, штрафы морали, потери (в т.ч. отложенные/`PendingCasualties`), расход/эффекты боезапаса, учёт `PoorControl` и состояний по доступным данным.
- Добавлены `CombatResolverTests`.

## Изменённые/созданные файлы (только ownership)
- `Kriegsspiel.Core/Arbitration/ICombatResolver.cs`
- `Kriegsspiel.Core/Arbitration/CombatResolver.cs`
- `Kriegsspiel.Core/Arbitration/ContactGeometryResolver.cs` (новый)
- `Kriegsspiel.Core.Tests/CombatResolverTests.cs` (новый)

## Проверки (из «Как проверить», выполнены implementer в ветке)
- `dotnet build Kriegsspiel.slnx` (worktree `/tmp/opencode/wave1/T-13`) → успех (0/0).
- `dotnet test Kriegsspiel.Core.Tests/Kriegsspiel.Core.Tests.csproj --filter CombatResolverTests` → Passed 7, Failed 0.

## Верификация (verifier)
- Вердикт: verified
- Замечания/раунды: критичных замечаний по фильтру/сборке нет; есть упрощения/эвристики и зависимость от интеграции T-20.
- Merge в ветку волны: выполнен (merge `task/T-13-combat-and-contact-geometry` → `wave/1-core-mechanics`).
- Post-merge в контексте ветки волны (сборка+старт): `dotnet build Kriegsspiel.slnx` → успех (0/0); `dotnet test Kriegsspiel.Core.Tests/Kriegsspiel.Core.Tests.csproj --filter CombatResolverTests` → Passed 7, Failed 0.

## Процесс (трение)
- Старт/конец или отметки раундов: реализация в task-ветке, затем верификация/merge в ветку волны.
- Неоднозначности (ссылки на карточку/`docs/gameDocs/rules.md §…`/док/файл): определение атакующего/метаданные контакта в `CombatTick`, полнота state-machine отхода/пленения/`PoorControl` и применение отложенных потерь в цикле `Arbiter` (`§2, §7, §8`, трасса B), частично вне ownership.
- Лишние поиски и причина (чего не хватило в `structure.md`/ownership/шаблоне/требованиях): согласование с доменными типами T-01 и интеграцией T-20.
- Оценка доп. затрат: средне.
- Предложения по улучшению: уточнять в карточках контракт `CombatTick`/источник геометрии и ответственность за применение `PendingCasualties`.

## Заметки / tech-debt
- Ограничения implementer: эвристики геометрии/атакующей стороны, упрощённые исходы panic/withdraw/capture и зависимость полной интеграции от `T-20/Arbiter`. Отдельный `TD-*` не оформлялся в этой сессии.
