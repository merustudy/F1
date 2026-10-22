# 인트로 대본 — A 「끊긴 문장」과 D 「고쳐 쓴 문장」 (【제안】)

> 이 문서는 【제안】이다. 사용자가 판정하기 전에는 데이터, 코드, 화면을 바꾸지 않고 유료 생성도 하지 않는다(CLAUDE.md §10).
> 2026-10-22 사용자: "일단 a와 d의 인트로 이야기를 준비해줘."
> **D의 인트로는 `25_Mercenary_Chronicle_Storyline_Proposal.md` §6의 쉬운 판 「세 푼과 백 일」이 §2를 대신한다**(사용자 "인트로도 더 쉽게").
> A의 줄기는 `23_Midnight_Storyline_Proposal.md`, D의 줄기는 `22_Story_Proposal.md` §10.3이 가진다. 둘 가운데 하나만 쓰게 된다(전제가 반대다: 22 §10.3 "위험").
> 글은 한국어가 기본이고 영어는 초안이다. 그림 소재의 영어 문장은 그림 라운드에서 다듬을 초안이다.

## 0. 공통

| 항목 | 정한 것 |
|---|---|
| 언제 | 타이틀의 [새 런]을 누른 뒤. 이어하기에서는 나오지 않는다 |
| 건너뛰기 | 아무 키나 누르면 바로 로비로 간다. 화면 오른쪽 아래에 작게 "아무 키: 건너뛰기" |
| 길이 | 본편 약 80초, 짧은 판 약 55초(장면마다 적었다). 어느 쪽을 쓸지는 MP4 목업에서 재고 정한다 |
| 형식 | **동화책(A) / 그림책과 연대기(D)**. 펼친 책이 화면을 채우고, 쪽마다 삽화 한 장과 글 두세 줄이 있다. 중간부터 책 밖의 장면 그림이 두세 장 나온다 |
| 글 | 한 화면에 세 줄까지. 줄마다 하나씩 페이드로 나타난다. 동화책의 문장은 인쇄체(해요체), 끼어드는 목소리는 손글씨 서체(반말·기록체)다 |
| 그림 | 지금 그림체(손으로 그린 잉크선, 낮은 채도)로 그린 옛 그림책 삽화. **그림 안에는 글자를 넣지 않는다**(스타일 문서의 금지 규칙). 책 제목, 이정표, 계약서의 글은 모두 화면의 글로 얹는다 |
| 움직임 | 쪽 넘김, 천천히 다가가기, 옆으로 흐르기, 재 입자, 번지는 유리 한 겹(A), 글자 위에 긋는 줄과 손글씨(D). 모두 UI 연출이고 영상은 쓰지 않는다 |
| 소리 | 대사 녹음은 없다(12 §4의 "음성"은 미결). 곡 하나(A 오르골 왈츠 / D 빗소리 위의 낮은 현)와 효과음 몇 개 |
| 만드는 순서 | 대본 승인 → 콘티 목업(정지 그림) → MP4 움직임 목업(반복·느린 재생) → 승인 → 그림 라운드(유료, 요청할 때) → 구현(화면 Architecture/12, 글 06, 그림 13, 소리 14) |

## 1. A — 「끊긴 문장」 (자정은 오지 않는다)

### 목표

| 전할 것 | 어느 장면 |
|---|---|
| 원작을 알아본다("신데렐라다") | 1~3 |
| 비틀림: 시계가 11시 59분에 멈췄고, 칠 년째다 | 4~5 (갈고리) |
| 위협과 기한: 마법이 썩고, 유리가 백 날 뒤 마을에 닿는다 | 6, 9 |
| 우리는 누구인가: 바깥에서 온 용병 | 7 |
| 왜 우리인가: 동화에 배역이 없는 사람만 규칙을 깬다. 그리고 현상금 | 8 |
| 분위기: 어둡고 약간 익살스럽다 | 7(롤로), 8 |
| **숨긴다**: 대모가 어머니라는 것(복선만: 개암나무, 흰 새, 같은 머리 색), 시계를 멈춘 사람("궁에 가 보면 알게 돼") | 2, 8 |
| **숨긴다**: 재투성이의 얼굴. 끝까지 머리칼과 뒷모습으로만 보이다가 결말에서 처음 드러난다 | 1, 3 |

### 대본

| # | 시간 | 화면 | 글 (한국어) | 글 (영어 초안) | 소리·움직임 |
|---|---|---|---|---|---|
| 0 | 0:00–0:05 | 검은 화면. 촛불 하나가 켜지며 낡은 가죽 동화책이 드러난다. 표지의 금박 제목은 긁혀 읽을 수 없다 | — | — | 오르골 태엽을 감는 소리, 이어 왈츠의 첫 소절. 책이 스스로 펼쳐진다 |
| 1 | 0:05–0:13 | 1쪽: 부엌 화덕 옆, 재 속에 웅크려 잠든 소녀. 얼굴은 머리칼에 가려 있다 | 옛날 옛적에, 재 속에서 잠드는 아이가 있었어요. / 사람들은 그 아이를 재투성이라고 불렀지요. | Once upon a time, there was a girl who slept in the ashes. / Everyone called her Cinder. | 천천히 다가가기 |
| 2 | 0:13–0:23 | 2쪽: 묘지의 개암나무, 가지에 앉은 흰 새. 나무 아래 지팡이를 든 대모. 얼굴은 두건의 그림자 속이고, 두건 밖으로 흘러나온 머리칼이 소녀와 같은 색이다. 곁에서 호박이 마차로 부풀어 오른다 | 무도회 날 밤, 개암나무 아래에 대모가 나타났어요. / 대모가 지팡이를 흔들자 호박은 마차가, 생쥐는 말이 되었지요. / "자정이 되면 모두 본디로 돌아간단다." | On the night of the ball, a godmother appeared beneath the hazel tree. / With a wave of her wand, the pumpkin became a coach, and the mice became horses. / "At midnight," she said, "everything returns to what it was." | 쪽 넘김. 지팡이 끝의 반짝임 몇 점 |
| 3 | 0:23–0:31 | 3쪽: 샹들리에 아래, 왕자와 춤추는 소녀의 뒷모습 | 아이는 왕자와 춤을 추었어요. / 시간 가는 줄도 모르고. | She danced with the prince, / and forgot all about the time. | 왈츠가 이어진다 |
| 4 | 0:31–0:39 | 4쪽: 달빛 아래 궁의 계단. 계단 중간에 떨어진 유리 구두 한 짝. 계단 위로 시계탑의 문자판이 보이고, 바늘은 11시 59분 | 그리고 시계가 열두 번— | And then the clock struck twel— | **오르골이 느려지다 한 음에서 멈춘다.** 1초의 정적 |
| 5 | 0:39–0:48 | 같은 쪽이 잿빛으로 바랜다. 위에서 재가 내려앉고, 그림의 계단에서부터 유리가 번져 인쇄된 문장을 덮는다. 여백에 거칠게 기운 손글씨 | …치지 않았어. / 칠 년째. | ...It didn't. / Not for seven years. | 유리에 금 가는 소리, 바람 |
| 6 | 0:48–0:57 | 5쪽: 언덕 위의 궁과 그 아래 잿빛골(로비 그림과 같은 구도). 궁에서부터 언덕을 타고 유리가 흘러내린다. 손글씨 | 끝나지 않는 마법은 썩어. / 저 유리는 백 날이면 이 마을에 닿아. | Magic that never ends begins to rot. / In a hundred days, that glass will reach this town. | 책이 재를 뿜으며 덮인다 |
| 7 | 0:57–1:07 | 장면(책 밖): 밤의 국경. 썩어 가는 호박 마차를 귀와 꼬리가 쥐인 마른 말 여섯이 끈다. 마부석에는 쥐 수염을 단 사내 롤로. 마차 창에 용병 다섯의 실루엣(도끼, 장검, 지팡이). 길가에 빈 이정표(글은 화면 글로 "여기서부터 밤") | (롤로) 국경을 넘으면 밤이오. 늘 밤이지. / …수염은 신경 쓰지 마시오. | (Rollo) Past the border, it's night. Always night. / ...Pay no mind to the whiskers. | 옆으로 흐르기. 덜컹이는 바퀴, 낮은 현 |
| 8 | 1:07–1:20 | 장면: 재가 눈처럼 내리는 잿빛골의 성문. 등불 아래 두건 쓴 대모. 지친 눈만 보인다 | (대모) 동화에 든 자는 동화의 규칙을 못 깨. 나도, 왕도, 이 마을 사람도. / 그래서 너희를 불렀어. 동화에 배역이 없는 사람들. / 현상금은 궁의 보물 창고 전부야. 시계를 치게 해. / (돌아서며) 누가 멈췄냐고? …궁에 가 보면 알게 돼. | (Godmother) No one inside a fairy tale can break its rules. Not me, not the king, not a soul in this town. / So I sent for you. People with no part in any tale. / The bounty is the palace treasury. All of it. Make the clock strike. / Who stopped it? ...You'll see, at the palace. | 세 줄씩 두 화면. 마지막 줄에서 대모가 등불을 들고 돌아선다 |
| 9 | 1:20– | 로비(잿빛골). 마을 가장자리에 유리가 번져 있다(남은 날에 따라 자란다) | 안내 줄: 첫 원정 — 성문으로 | First expedition — to the gate | 로비 곡으로 이어진다 |

**짧은 판(약 55초)**: 3을 뺀다. 7의 롤로 대사를 8의 앞에 붙여 한 장면으로 합친다. 8은 "그래서 너희를 불렀어…", "현상금은…", "누가 멈췄냐고?…" 세 줄만 쓴다.

### 그림 목록 (8장, 약 $0.25)

| # | 소재 | 영어 소재 초안 |
|---|---|---|
| A0 | 촛불 아래 펼쳐지기 전의 낡은 가죽 동화책. 표지의 금박 장식은 긁혀 있다 | an old worn leather storybook lying closed under a single candle, its gilded cover ornament scratched away |
| A1 | 부엌 화덕 옆 재 속에 웅크려 잠든 소녀, 얼굴은 머리칼에 가려짐 | a girl curled up asleep in the ashes beside a kitchen hearth, her face hidden by her hair |
| A2 | 묘지의 개암나무와 흰 새, 두건의 대모(얼굴은 그림자), 마차로 부푸는 호박 | a tall hazel tree in a graveyard with a white bird on a branch; beneath it a hooded godmother with a wand, face in shadow, a few strands of hair the same colour as the girl's; a pumpkin swelling into a coach |
| A3 | 샹들리에 아래 왕자와 춤추는 소녀의 뒷모습 | a prince and a girl dancing under a chandelier, seen from behind |
| A4 | 달빛의 궁 계단, 떨어진 유리 구두 한 짝, 계단 위 시계탑 문자판(11시 59분) | moonlit palace stairs with a single glass slipper lying on a step; above, a clock tower face with the hands at one minute to twelve |
| A5 | 언덕 위의 궁과 그 아래 잿빛골, 언덕을 타고 흘러내리는 유리 | a palace on a hill above an ash-grey town at night, glass creeping down the hillside from the palace |
| A6 | 밤의 국경, 쥐 귀와 꼬리의 마른 말 여섯이 끄는 썩은 호박 마차, 쥐 수염 마부, 창 안 용병 다섯의 실루엣 | at a night border, a rotting pumpkin coach pulled by six gaunt horses with mouse ears and tails, a coachman with rat whiskers, five mercenary silhouettes in the windows (axe, longsword, staff) |
| A7 | 재가 내리는 성문, 등불 아래 두건의 대모 | a town gate in falling ash like snow, a hooded old godmother holding a lantern, only her tired eyes visible |

책의 틀, 재 입자, 번지는 유리는 UI의 조각으로 만든다(유리는 한 장의 무늬를 날에 따라 넓게 드러낸다).

## 2. D — 「고쳐 쓴 문장」 (옛날 옛적의 용병들)

### 목표

| 전할 것 | 어느 장면 |
|---|---|
| 원작들을 알아본다(다섯 동화) | 0~1 |
| 비틀림: "동화는 거짓말이다. 진짜는 우리가 했다" | 1~2 (갈고리) |
| 우리는 누구인가: 세 푼 받고 해산된 용병 | 3 |
| 일과 기한: 영주의 백 일 계약, 조항 다섯, 혼례일 | 4~5 |
| 왜 이 이야기를 남기나: 동화가 이름을 지웠다 | 6~7 |
| 분위기: 건조한 기록체의 비꼼, 전쟁 뒤의 진창 | 2~4 |
| **숨긴다**: 조항 다섯이 모두 영주의 죄라는 것, 영주가 푸른 수염이라는 것(복선만: 쌓인 책의 맨 아래 「푸른 수염」, 푸른 깃발) | 0, 4 |

### 대본

| # | 시간 | 화면 | 글 (한국어) | 글 (영어 초안) | 소리·움직임 |
|---|---|---|---|---|---|
| 0 | 0:00–0:06 | 촛불 아래 쌓인 그림책 다섯 권. 책등의 제목은 화면 글로 얹는다. 위에서부터 헨젤과 그레텔, 백설공주, 춤추는 열두 공주, 잠자는 숲속의 공주, 푸른 수염 | — | — | 빗소리. 천천히 다가가기 |
| 1a | 0:06–0:10 | 「백설공주」의 한 쪽이 펼쳐진다. 작은 삽화(숲으로 달아나는 소녀). 인쇄체 문장 | 사냥꾼은 차마 공주를 죽이지 못하고 숲으로 보내 주었어요. | The huntsman could not bring himself to kill her, and let her flee into the forest. | 깃펜을 쥔 손이 들어와 "사냥꾼"에 줄을 긋고, 위에 손글씨로 "용병" |
| 1b | 0:10–0:13 | 「잠자는 숲속의 공주」의 한 쪽(가시덤불의 성) | 왕자가 가시덤불을 헤치고 들어가 공주를 깨웠어요. | The prince cut through the briars and woke the princess. | "왕자"에 줄, 위에 "용병". 앞 장보다 빠르게 |
| 1c | 0:13–0:16 | 「푸른 수염」의 한 쪽(성문으로 달려오는 말 탄 사람 둘) | 그때 아내의 오빠 둘이 말을 달려왔어요. | Just then, her two brothers came galloping up. | "아내의 오빠 둘"에 줄, 위에 "용병들". 펜 끝이 종이를 긁는다 |
| 2 | 0:16–0:26 | 손이 그림책을 덮고, 그 위에 낡은 가죽 장부를 펼친다. 첫 장에 손글씨 | 동화는 거짓말이다. / 진짜는 우리가 했다. / — 용병단 연대기. 기록자 야코프, 전에 수도사였던 자. | Fairy tales are lies. / We did the real thing. / — Chronicle of the Company. Kept by Jakob, formerly a monk. | 펜 소리. 빗소리가 멀어진다 |
| 3 | 0:26–0:38 | 장면: 비 오는 전쟁터의 해산. 진창에 줄 선 용병들. 회계관이 한 손바닥에 동전 세 닢을 떨어뜨린다 | (야코프) 전쟁은 끝났다. 끝났다는 건, 우리에게 줄 돈이 없다는 뜻이다. / 열두 해를 싸운 값은 세 푼이었다. | (Jakob) The war was over. Which meant there was no more money for us. / Twelve years of fighting, paid in three pennies. | 동전 셋이 떨어지는 소리 |
| 4 | 0:38–0:50 | 장면: 진흙투성이 장터. 모병꾼 티모가 북을 친다. 뒤에 푸른 바탕의 깃발. 벽에 붙은 계약서 | (티모) 영주님께서 칼잡이를 구하신다! 백 일 계약, 선금은 은화로! / (야코프) 영주는 전쟁 영웅이었다. 우리 같은 용병으로 시작해 땅을 얻은 사람. 그래서 믿었다. | (Timo) His lordship is hiring swords! A hundred days' contract, advance paid in silver! / (Jakob) The lord was a war hero. He started as one of us, a sellsword, and won himself land. So we trusted him. | 모병 북 |
| 5 | 0:50–1:02 | 양피지 계약서(UI 위에 글). 조항 다섯과 서명란. 서명란에는 X 표 넷, 서툰 이름 하나, "야코프(대필)" | 하나, 숲의 마녀를 처치할 것. / 둘, 광산을 비울 것. / 셋, 춤추는 마을을 잠재울 것. / 넷, 잠든 성의 문을 열 것. / 다섯, 영주의 혼례일까지 위의 일을 마칠 것. / (야코프) 조항은 다섯이었다. 우리는 그 다섯이 하나의 이야기라는 걸 몰랐다. | First, slay the witch of the woods. / Second, clear the mine. / Third, quiet the dancing town. / Fourth, open the gates of the sleeping castle. / Fifth, finish all of the above by his lordship's wedding day. / (Jakob) Five clauses. We didn't know they were all one story. | 조항이 한 줄씩 적혀 나간다. 마지막 줄에서 잠깐 멈춤 |
| 6 | 1:02–1:14 | 장면: 해 질 녘, 전쟁에 망가진 마을(로비 그림과 같은 구도). 언덕 위에 푸른 깃발의 성 | (야코프) 백 년 뒤, 사람들은 우리가 한 일을 동화로 전했다. / 우리 이름은 한 줄도 남지 않았다. / 그래서 여기 적는다. | (Jakob) A hundred years later, people told what we did as fairy tales. / Not one of our names survived. / So I am writing them here. | 낮은 현 |
| 7 | 1:14– | 연대기의 다음 장. 이번 런의 시작 로스터 이름이 한 줄씩 적힌다(지금은 로언, 아스트리드, 엘라, 미라, 카이. 데이터에서 읽는다). 이어서 로비 | 안내 줄: 첫 조항 — 굶주린 숲 | First clause — the hungry woods | 펜 소리. 로비 곡으로 이어진다 |

7의 이름은 결말의 명부("여백의 이름들", 22 §10.3)와 짝을 이룬다. 처음에 적은 이름 가운데 몇이 끝까지 남는지가 그 런의 이야기다.

**짧은 판(약 55초)**: 1a~1c를 1c 하나로 줄인다. 4의 야코프 줄을 빼고, 5와 6을 한 화면으로 합친다(조항은 둘째 줄까지만 보이고 넘어간다).

### 그림 목록 (9장, 약 $0.27)

| # | 소재 | 영어 소재 초안 |
|---|---|---|
| D0 | 촛불 아래 쌓인 그림책 다섯 권(책등은 비워 둔다) | a stack of five old picture books under a candle, their spines blank |
| D1a | 숲으로 달아나는 소녀(작은 삽화) | a small storybook vignette: a girl fleeing into a dark forest |
| D1b | 가시덤불에 덮인 성(작은 삽화) | a small storybook vignette: a castle smothered in briars |
| D1c | 성문으로 말을 달려오는 두 사람(작은 삽화) | a small storybook vignette: two riders galloping toward a castle gate |
| D2 | 낡은 가죽 장부의 펼친 쪽(빈 쪽, 글은 화면에서) | an open worn leather ledger with blank yellowed pages, a quill beside it |
| D3 | 비 오는 전쟁터, 진창에 줄 선 용병들, 손바닥에 떨어지는 동전 셋 | a rainy battlefield after the war, mercenaries queuing in the mud, a paymaster dropping three coins into one open palm |
| D4 | 진흙투성이 장터, 북 치는 모병꾼, 푸른 깃발, 벽의 빈 벽보 | a muddy market square, a recruiter beating a drum, a blue banner behind him, a blank notice nailed to a wall |
| D5 | 양피지 계약서의 바탕(빈 종이, 밀랍 봉인) | a blank parchment contract with a blue wax seal |
| D6 | 해 질 녘 전쟁에 망가진 마을, 언덕 위 푸른 깃발의 성 | a war-ravaged town at dusk, a castle with blue banners on the hill above |

글자는 모두 화면의 글로 얹는다. 줄 긋기와 손글씨는 UI 연출이다(손글씨 서체 하나가 필요하다: Architecture/06의 글꼴).

## 3. 둘의 비교

| | A 「끊긴 문장」 | D 「고쳐 쓴 문장」 |
|---|---|---|
| 갈고리 | 모두가 아는 문장이 "열두 번—"에서 끊기고 오르골이 멈춘다 | 모두가 아는 문장의 "사냥꾼·왕자·오빠들"이 차례로 지워지고 "용병"이 된다 |
| 끼어드는 목소리 | 대모의 손글씨(반말) | 야코프의 연대기(기록체) |
| 플레이어가 받는 질문 | 누가 시계를 멈췄나 | 조항 다섯은 어떻게 하나의 이야기인가 |
| 런과의 연결 | 로비 가장자리의 유리가 남은 날만큼 자란다 | 연대기에 이번 런의 이름이 적힌다 |
| 그림 / 비용 | 8장, 약 $0.25 | 9장, 약 $0.27 |
| 새로 필요한 것 | 오르골 왈츠 1곡, 유리 번짐 조각 | 손글씨 서체, 빗소리·북·펜 효과음 |

## 4. 판정할 것 (권장안은 굵게)

1. 대본: **A·D 모두 이 대본을 출발값으로** / 고칠 장면을 지정.
2. 길이: **본편(약 80초)과 짧은 판(약 55초)을 MP4 목업으로 재고 고른다** / 지금 정한다.
3. 건너뛰기와 보이는 때: **새 런마다, 아무 키로 건너뜀** / 처음 한 번만(설정 저장에 칸 하나 — Save 확인 필요).
4. 영어 글: **이 초안을 출발값으로, 구현 단계에서 다듬는다** / 지금 다듬는다.
5. 다음 걸음: **줄기를 고른 뒤(A 또는 D), 그 인트로의 콘티 목업(정지 그림)부터** / 둘 다 목업.
