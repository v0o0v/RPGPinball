# PR Review Style Guide

모든 리뷰 코멘트는 한국어로 작성한다.

다음 기준에 해당하는 경우에만 코멘트한다.
- 버그 가능성이 높음
- 성능 저하 가능성이 명확함
- 구조적으로 유지보수성이 크게 떨어짐
- Unity 관점에서 잘못된 사용 패턴임

다음은 코멘트하지 않는다.
- 취향 차이 수준의 네이밍
- 사소한 포맷팅
- 이미 팀 컨벤션으로 허용된 표현
- 의미 없는 칭찬이나 장문 설명

리뷰 스타일:
- 한 코멘트는 2~4문장 이내
- 가능한 경우 문제점 1개 + 수정 방향 1개만 제시
- 중복 지적 금지
- 정말 중요하지 않으면 코멘트하지 말 것

Unity/C# 우선 리뷰 항목:
1. 문법/안정성
- null 가능성
- 이벤트 등록/해제 누락
- async/Coroutine 오용
- 컬렉션 수정 중 foreach 사용 등 위험 패턴

2. 성능
- Update/LateUpdate/FixedUpdate 내부의 불필요한 할당
- GetComponent, Find 계열 반복 호출
- LINQ/boxing/GC alloc 유발 코드
- 문자열 결합/로그 남발
- 비싼 연산의 프레임 반복 수행

3. 구조
- MonoBehaviour가 너무 많은 책임을 가짐
- 매직 넘버 남발
- 직렬화 필드와 런타임 상태 혼재
- 테스트/재사용 어려운 강결합 구조

4. Unity 특화
- Awake/OnEnable/Start 실행 순서 의존성
- ScriptableObject, Addressables, pooling 필요성이 명확한 경우
- Destroy/Instantiate 남용
- 리소스 해제 누락 가능성

리뷰 우선순위:
- correctness > performance > structure > style
