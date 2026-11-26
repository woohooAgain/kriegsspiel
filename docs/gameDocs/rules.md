ШТАБНАЯ ИГРА — Ядро движка v0.4 (Спецификация правил)

Статус документа (N-09): Формальная спецификация детерминированного ядра симуляции (Core Engine v0.4, упрощённый MVP-scope). Документ является самодостаточным и вычислительно замкнутым для кодовой реализации без привязки к UI и без использования LLM.
Намеренно исключено из v0.4 (P-09, P-13): пополнение боезапаса, гибель/замена командиров, состояния паники и пленения, гражданские лица, маскировка, конвои, сложные ветвления приказов (ЕСЛИ-ТО).

1. Входы сценария и детерминизм (P-11, P-12)

Сценарий инициализирует состояние движка следующим набором данных (ScenarioConfig):
1. Граф карты \(G = (V, E)\), узел штаба игрока hqZone \(\in V\).
2. Временные рамки: длительность партии endMinute (кратна 30).
3. Пул курьеров штаба: количество курьеров K >= 1, вероятности потери депеши p_courier_loss \(\in [0, 1]\) и донесения p_report_loss \(\in [0, 1]\).
4. Стартовый список юнитов (player и enemy) с указанием начального узла zoneId, стартовой позы posture (по умолчанию defense), initialStrength, readiness, ammo и параметров commander.
5. Условия победы: целевые узлы \(V_{\text{target}} \subseteq V\), веса \(W_{v}\) и порог WinThreshold.
6. Генератор случайных чисел (P-12): движок использует единый детерминированный PRNG, инициализируемый параметром seed из ScenarioConfig. Внутри каждого 1-минутного тика юниты и депеши обрабатываются в строго отсортированном лексикографическом порядке их id, что гарантирует 100% воспроизводимость симуляции при одинаковом seed и одинаковом вводе.

2. Мир, время и управление противником (P-01, P-02)

• Граф \(G = (V, E)\): произвольный связный неориентированный граф.
	• Район (Zone): defenseBonus \(\in \{0, 1, 2\}\), observability \(\in \{1, 2, 3\}\).
	• Путь (Edge): travelTime (целое \(>0\), фактическое время перехода юнита в минутах), type \(\in \{\text{road}, \text{offroad}\}\) (offroad снижает readiness на \(-5\) при завершении ребра), courierTime (целое \(>0\), время прохода курьера в одну сторону).
• Управление противником (P-01): юниты стороны enemy подчиняются тем же правилам движения, приказов (Order), разведки и боя, что и юниты player. Приказы для enemy подаются в CommandPhase внешним контроллером сценария (скриптом или ИИ-модулем) в том же формате Order.
• Игровой цикл и порядок разрешения внутри 1-минутного тика \(t\):
	• Часы minute идут от 0 до endMinute. При minute % 30 == 0 и minute < endMinute наступает пауза CommandPhase (подача 0 или более Order / Query), после чего вызывается advanceTime(30).
	• Порядок шагов в каждом 1-минутном тике \(t\):
		1. Отправка депеш из очереди штаба (если есть свободные курьеры).
		2. Доставка депеш юнитам (deliverAt == t) и интерпретация приказов.
		3. Продвижение юнитов по рёбрам (уменьшение remainingMinutes), завершение переходов (и немедленный переход на следующее ребро маршрута в тот же тик, если путь не окончен, P-02; remainingMinutes нового ребра в этом же тике не уменьшается — уменьшение начинается со следующего тика), проверка обнаружения (Contact / ReconResult, каденция recon — §6) и проверка истечения deadline.
		4. Если t % 30 == 0: разрешение боёв в узлах с присутствием обеих сторон, затем восстановление отдыха (+5 readiness) для юнитов, которые за прошедшие 30 минут не двигались и не участвовали в бою.
		5. Доставка донесений в штаб (deliveryMinute == t), возврат курьеров в пул (returnAt == t) и проверка SilenceDetected.

3. Юнит и конечный автомат позы (N-03, N-07)

• side \(\in \{\text{player}, \text{enemy}\}\).
• location: { type: "node", zoneId: v } либо { type: "edge", edgeId: e, fromZone: u, toZone: v, remainingMinutes: m, entryEdgeType: e.type }.
• strength: текущая сила (\(\ge 0\); старт initialStrength \(>0\)). При strength == 0 юнит уничтожается и удаляется из симуляции.
• isCombatEffective: strength >= 0.30 * initialStrength.
• readiness \(\in [0, 100]\); ammo (целое \(\ge 0\)).
• commander: init, pedantry, honesty \(\in \{0, 1, 2, 3\}\).
• effectiveOrder: текущий исполняемый приказ или null.
• pendingOrder: единичный буфер приказа, полученного на ребре. Если во время движения приходит новый приказ, он перезаписывает текущий pendingOrder (N-03).
• Поза (posture \(\in \{\text{march}, \text{attack}, \text{defense}\}\)) (N-07):
	• march: юнит находится на ребре (location.type == "edge").
	• attack: юнит вошёл в узел \(v\), где присутствуют юниты противника с strength > 0 (включая одновременный вход обеих сторон в один тик), либо находится в узле \(v\) в состоянии боя при выполнении intent == move в узел \(v\).
	• defense: юнит находится в узле \(v\) без движения и не атакует (в том числе после успешного занятия targetZone при intent == move, если противника в узле нет — в этом случае effectiveOrder.intent автоматически переходит в hold с целью \(v\), N-07).

4. Штаб, курьеры и запросы (N-02, N-04)

• Пул курьеров штаба: обслуживает только исходящие от штаба депеши (Order и Query). Обратные донесения (Report) отправляются собственными вестовыми юнитов и пул курьеров штаба не занимают (N-04).
	• Время плеча (\(T_{\text{leg}}\)): кратчайший путь по Дейкстре (по сумме courierTime) от hqZone до узла юнита (если юнит на ребре \((u, v)\) — до узла \(v\)). Минимум \(T_{\text{leg}} = 5\) минут.
	• Обратное плечо (\(T_{\text{leg\_back}}\)): кратчайший путь по Дейкстре (по сумме courierTime) от текущего узла юнита до hqZone (если юнит на ребре \((u, v)\) — от узла \(v\)). Для одного и того же узла в неориентированном графе \(T_{\text{leg\_back}} = T_{\text{leg}}\); к \(T_{\text{leg\_back}}\) применяется тот же минимум 5 минут. Значение фиксируется в момент события (eventMinute / deliverAt) по текущему узлу юнита и далее для этого события не пересчитывается.
• Очередь и отправка: депеши фиксируют createdMinute = minute. При наличии свободного курьера депеша отправляется в тик \(t\) (sentMinute = t), пересчитывается \(T_{\text{leg}}\), задаются deliverAt = t + T_leg и returnAt = t + 2 * T_leg. Приоритет: Query выше Order, далее FIFO по createdMinute.
• Потеря курьера: в момент sentMinute проверяется rand() < p_courier_loss. При срабатывании депеша не доставляется (deliverAt = null), в EventLog пишется CourierLost, а курьер возвращается в пул в sentMinute + 2 * T_leg + 60.
• Запросы (Query) и тишина (N-02):
	• В минуту deliverAt живой юнит формирует QueryReply с eventMinute = deliverAt. Ответ доставляется в штаб в deliveryMinute = deliverAt + T_leg_back (если не сработала потеря донесения rand() < p_report_loss).
	• В момент deliverAt фиксируется ожидаемое время ответа expectedReplyAt = deliverAt + T_leg_back + 15. Если ответ не получен до expectedReplyAt (или если курьер с запросом погиб — до sentMinute + 2 * T_leg + 30), фиксируется событие SilenceDetected(unitId, minute).

5. Приказ, поиск пути и интерпретация (N-05, N-06)

• Структура приказа (Order): unitId, intent \(\in \{\text{move}, \text{hold}, \text{recon}\}\), targetZone (\(V \cup \{\text{null}\}\)), deadline (минута или null), forbiddenZone (\(V \cup \{\text{null}\}\)).
• Семантика intent (I-04, G3):
	• move: идти в targetZone (при targetZone == null — см. Шаг 2).
	• hold: при targetZone != null — идти в targetZone и по прибытии удерживать её (posture defense, без дальнейшего движения); при targetZone == null — удерживать текущий узел (posture defense, без движения).
	• recon: вести наблюдение targetZone из смежного узла (§6, каденция recon).
• Поиск пути Дейкстрой (N-05):
	• Если forbiddenZone != null и forbiddenZone != startZone, узел forbiddenZone исключается из графа перед поиском.
	• Если forbiddenZone == startZone, юнит может выйти по любому исходящему ребру \((startZone, w)\), но узел startZone исключается из дальнейшего пути (возврат запрещён).
	• Вес ребра для выбора маршрута: \(w(e) = e.\text{travelTime}\) при deadline != null, и \(w(e) = e.\text{travelTime} \times (e.\text{type} == \text{offroad} ? 0.8 : 1.0)\) при deadline == null. Фактическое время перехода всегда равно \(e.\text{travelTime}\).
• Неоднозначность \(A\) и вероятность искажения \(P\):
	• \(A = 0.0\); если targetZone == null и intent != hold, \(A += 0.70\); если deadline == null и intent == move, \(A += 0.15\).
	• \(P = \text{clamp}\big(0.03 + A \cdot (1 + 0.15 \cdot \text{pedantry} - 0.10 \cdot \text{init}),\; 0.03,\; 0.90\big)\).
• Интерпретация в минуту deliverAt (или по завершении ребра из pendingOrder):
	• Шаг 1 (Конфликт с forbiddenZone): если targetZone != null и (targetZone == forbiddenZone или путь до targetZone с учётом фильтрации не существует):
		• при pedantry >= init: effectiveOrder = null, юнит остаётся на месте (defense), шлёт ImpossibleOrder и завершает обработку (Шаг 2 не выполняется);
		• при init > pedantry: forbiddenZone сбрасывается в null, и обработка переходит к Шагу 2.
	• Шаг 2 (Искажение при \(A > 0\)): бросок rand() < P. Если сработал:
		• при pedantry >= 2: effectiveOrder = null, юнит остаётся на месте (defense) и шлёт ClarificationRequest;
		• при pedantry < 2: если targetZone == null, выбирается случайный смежный узел (для recon — узел через ребро от смежного); если targetZone != null, intent подменяется (move \(\leftrightarrow \) hold).
• Просрочка deadline: если t > effectiveOrder.deadline и цель ещё не достигнута, при pedantry >= 2 и init <= 1 юнит прерывает приказ (effectiveOrder = null, defense) и шлёт ImpossibleOrder; иначе продолжает выполнение.

6. Наблюдение и структура донесений (I-27, N-01, N-04, N-08)

• Отправка донесений (N-04): при срабатывании триггера (Arrival, Contact, ReconResult, ImpossibleOrder, ClarificationRequest, QueryReply) юнит отправляет донесение собственным вестовым. Проверяется rand() < p_report_loss: при срабатывании донесение теряется в пути (ReportLost в EventLog); иначе доставляется в штаб в deliveryMinute = eventMinute + T_leg_back.
• Формула обнаружения (p_detect) (N-08):
\(p_{\text{detect}}=\text{clamp}\big(0.25+0.15\cdot v.\text{observability}-\text{edgePenalty},\;0.10,\;0.95\big)\)
где edgePenalty = 0.15, если наблюдатель вошёл в \(v\) или ведёт recon через ребро типа offroad; для периодической проверки в узле (когда юнит уже стоит в \(v\)) edgePenalty = 0.0 (N-08).
• Каденция разведки (P-07, G2): при effectiveOrder.intent == recon проверка p_detect выполняется в каждый 1-минутный тик, начиная с deliverAt, пока юнит стоит в узле, из которого targetZone достижим по одному ребру (наблюдение через это ребро; юнит в наблюдаемый узел не входит и в бою в нём не участвует, §7). Наблюдаемый узел — targetZone; при targetZone == null проверка p_detect не выполняется до смены приказа. Момент генерации ReconResult: первая успешная проверка немедленно формирует один ReconResult с confidence 3 и прекращает наблюдение по этому приказу; если за 30-минутное окно наблюдения (считая от deliverAt) успеха не было, по истечении окна формируется один ReconResult с confidence 1. Смена приказа (новый deliverAt или применение pendingOrder по завершении ребра) прерывает окно и сбрасывает наблюдение.
• Поля Report, confidence и искажения (I-27, N-01):
	• confidence:
		• 3 — ReconResult при успешном p_detect, а также Arrival, ImpossibleOrder, ClarificationRequest;
		• 2 — Contact (включая автоматический Contact при начале боя в узле, N-01) и QueryReply по текущему узлу юнита;
		• 1 — ReconResult при провале p_detect либо QueryReply о соседнем узле.
	• reportedOwnStrength: null для ReconResult (I-27); для всех прочих типов — реальный strength юнита, умноженный при honesty < 3 на \(1 + (3 - \text{honesty}) \times 0.12\).
	• reportedEnemyStrength: 0, если противник не обнаружен (или при провале recon); иначе сумма strength противника в observedZoneId, умноженная при honesty < 3 на \(1 - (3 - \text{honesty}) \times 0.12\).
• Вне scope v0.4 (P-05, G4): слияние и разрешение противоречивых версий донесений (fact key) и отдельный «отчёт о потерях» намеренно исключены; штаб сохраняет все полученные Report в EventLog без слияния версий и без приоритизации.

7. Бой, победа и разбор (N-06)

• Бой (t % 30 == 0): разрешается в каждом узле \(v\), где присутствуют юниты обеих сторон с strength > 0 (юниты, ведущие recon из соседнего узла, в бою в \(v\) не участвуют).
	• \(\text{Power}_i = \text{strength}_i \times \frac{\text{readiness}_i}{100} \times \text{PostureMult}_i \times \text{TerrainMult}_i \times \text{AmmoMult}_i\) (PostureMult: march = 0.5, attack = 1.0, defense = 1.3; TerrainMult: 1 + 0.1 * v.defenseBonus только при posture == defense, иначе 1.0; AmmoMult: 1.0 при ammo > 0, 0.5 при ammo == 0).
	• Если \(\text{Power}_{\text{player}} + \text{Power}_{\text{enemy}} > 0\), для каждого юнита \(i\) стороны self одновременно списывается:
\(\text{Loss}_i = 0.25 \times \frac{\text{Power}_{\text{enemy}}}{\text{Power}_{\text{player}} + \text{Power}_{\text{enemy}}} \times \text{strength}_i\), ammo = max(0, ammo - 1), readiness = max(0, readiness - 10). При strength == 0 юнит удаляется.
	• Отступление: выживший юнит с strength < 0.30 * initialStrength отступает в смежный с \(v\) узел \(w\) без противника (с минимальным travelTime, effectiveOrder = null); если свободных смежных узлов нет — уничтожается (strength = 0).
• Победа: в минуту endMinute по целевым узлам \(V_{\text{target}}\) считается \(\text{Score} = \sum W_v \times \text{StateScore}(v)\) (состояние оценивается в интересах стороны player: Achieved = 1.0, если в \(v\) есть юниты player с isCombatEffective == true и нет таковых у enemy; Contested = 0.5, если в \(v\) есть юниты с isCombatEffective == true у обеих сторон; Failed = 0.0, если юнитов player с isCombatEffective == true в \(v\) нет). Победа при \(\text{Score} \ge \text{WinThreshold}\).
• Разбор (EventLog и метрики) (N-06):
	• Для каждого приказа фиксируются createdMinute, sentMinute (null, если не отправлен до endMinute), deliverAt (null, если потерян курьер или не отправлен до endMinute), исходный Order, effectiveOrder и статус (Executed, Distorted, Failed/Rejected).
	• Метрики: средняя задержка доставки avg(deliverAt - createdMinute) по доставленным приказам; доля приказов с A > 0; доля приказов со статусом Distorted или Failed/Rejected.
