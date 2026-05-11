# RPG 핀볼 스킬 트리 다이어그램

`Skill_Tree_Formulas.md`의 연관관계를 바탕으로 시각화한 스킬 트리 구조입니다. 화살표 방향을 통해 각 스킬이 어느 티어로 연결되는지 한눈에 확인할 수 있습니다.

## 🟢 분기 1: 제어의 길 (Path of Control)

```mermaid
graph TD
    %% Tier 1
    C1[1. 플리퍼 경량화 I]
    C2[2. 플리퍼 경량화 II]
    C3[3. 탄성 강화]

    %% Tier 2
    C4[4. 패스트 드로우]
    C5[5. 롱 플리퍼]
    C6[6. 마그네틱 필드]

    %% Tier 3
    C7[7. 퀵 리커버리]
    C8[8. 에너지 실드]
    C9[9. 강화 마그네틱 필드]
    C10[10. 와이드 앵글]

    %% Tier 4
    C11[11. 트리플 드로우]
    C12[12. 세이프티 월]
    C13[13. 트램펄린 엑셀]
    C14[14. 저스트 가드]

    %% Tier 5
    C15[15. 퍼펙트 디펜스]
    C16[16. 타임 오라]
    C17[17. 리바운드 실드]
    C18[18. 매스 리콜]

    %% Tier 6
    C19{19. 타임 딜레이전}
    C20{20. 존 오브 컨트롤}

    %% Connections
    C1 --> C4
    C2 --> C5
    C3 --> C6

    C4 --> C7
    C5 --> C8
    C5 --> C10
    C6 --> C9

    C7 --> C11
    C8 --> C12
    C10 --> C14
    C12 --> C13

    C14 --> C15
    C11 --> C16
    C13 --> C17
    C9 --> C18

    C16 --> C19
    C17 --> C19
    C15 --> C20
    C18 --> C20

    %% Styling
    classDef default fill:#1a202c,stroke:#4fd1c5,stroke-width:2px,color:#fff;
    classDef ult fill:#2c1a1a,stroke:#fc8181,stroke-width:3px,color:#fff;
    class C19,C20 ult;
```

## 🔴 분기 2: 파괴의 길 (Path of Destruction)

```mermaid
graph TD
    %% Tier 1
    D1[1. 강철구 I]
    D2[2. 강철구 II]
    D3[3. 돌진력]

    %% Tier 2
    D4[4. 관성 돌파 I]
    D5[5. 관성 돌파 II]
    D6[6. 파괴 본능]

    %% Tier 3
    D7[7. 콤보 스트라이크]
    D8[8. 약점 포착]
    D9[9. 묵직한 타격]
    D10[10. 타임 브레이커]

    %% Tier 4
    D11[11. 하이퍼 콤보]
    D12[12. 소닉 붐]
    D13[13. 자이언트 볼]
    D14[14. 분노의 일격]

    %% Tier 5
    D15[15. 가속의 쾌감]
    D16[16. 아머 크래시]
    D17[17. 플리퍼 스매시]
    D18[18. 헤비 액셀러레이터]

    %% Tier 6
    D19{19. 메테오 스트라이크}
    D20{20. 제로 블레이드}

    %% Connections
    D1 --> D4
    D2 --> D5
    D3 --> D6

    D4 --> D7
    D5 --> D8
    D4 --> D9
    D6 --> D10

    D7 --> D11
    D9 --> D12
    D8 --> D13
    D10 --> D14

    D12 --> D15
    D13 --> D16
    D11 --> D17
    D14 --> D18

    D16 --> D19
    D17 --> D19
    D15 --> D20
    D18 --> D20

    %% Styling
    classDef default fill:#2c1a1a,stroke:#fc8181,stroke-width:2px,color:#fff;
    classDef ult fill:#1a202c,stroke:#4fd1c5,stroke-width:3px,color:#fff;
    class D19,D20 ult;
```

## 🔵 분기 3: 원소의 길 (Path of Elements)

```mermaid
graph TD
    %% Tier 1
    E1[1. 원소 친화 I]
    E2[2. 원소 친화 II]
    E3[3. 마나 충전]

    %% Tier 2
    E4[4. 파이어볼 I]
    E5[5. 파이어볼 II]
    E6[6. 스파크]

    %% Tier 3
    E7[7. 아이스 폼 I]
    E8[8. 아이스 폼 II]
    E9[9. 체인 라이트닝 I]
    E10[10. 플레임 트레일]

    %% Tier 4
    E11[11. 듀얼 엘리먼트]
    E12[12. 체인 라이트닝 II]
    E13[13. 멀티볼 I]
    E14[14. 서리 폭발]

    %% Tier 5
    E15[15. 멀티볼 II]
    E16[16. 속성 융합]
    E17[17. 썬더볼트]
    E18[18. 볼텍스]

    %% Tier 6
    E19{19. 원소 폭주}
    E20{20. 아마겟돈}

    %% Connections
    E1 --> E4
    E2 --> E5
    E3 --> E6

    E4 --> E7
    E5 --> E8
    E6 --> E9
    E4 --> E10

    E7 --> E11
    E10 --> E11
    E9 --> E12
    E8 --> E13
    E7 --> E14

    E13 --> E15
    E11 --> E16
    E12 --> E17
    E14 --> E18

    E16 --> E19
    E15 --> E19
    E17 --> E20
    E18 --> E20

    %% Styling
    classDef default fill:#1a1c2c,stroke:#90cdf4,stroke-width:2px,color:#fff;
    classDef ult fill:#2c1a29,stroke:#f687b3,stroke-width:3px,color:#fff;
    class E19,E20 ult;
```
