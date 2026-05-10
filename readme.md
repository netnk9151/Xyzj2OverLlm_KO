# 하일참강호 2 한국어 패치 (패치 방법 안내 예정)



※ 본 패치는 https://github.com/joshfreitas1984/Xyzj2OverLlm 를 기반으로 만들어졌음을 알립니다. 도움 주신 Lash께 감사를 표합니다.



설치 방법:

[한패 파일](https://github.com/netnk9151/Xyzj2OverLlm_KO/releases/tag/Korean)을 받아 하일참강호가 설치된 곳에 압축풀기.

예) Steam\steamapps\common\下一站江湖Ⅱ\下一站江湖Ⅱ

실행 안내:

1. 제어판-> 시계 및 국가 -> 관리자 옵션 -> 시스템 로캘 변경 -> Beta: 세계 언어 지원을 위해 Unicode UTF-8 사용 체크 후 재부팅, 이후 게임 실행.




# 제보

[네이버 소요객잔][(https://cafe.naver.com/beemu)]


  
### 작업 방법 (추가 예정)






### 커스텀 텍스트 리사이저 (제미나이 번역)

Keypad_-(숫자패드 -)를 누르면 현재 커서 아래에 있는 요소에 대한 리사이저(resizer)가 BepInEx/resizers/zzAddedResizers.yaml 파일에 추가됩니다. 이후 해당 리사이저의 속성을 원하는 대로 수정한 뒤 다시 불러올 수 있습니다.
화면이 이상하게 보인다면 Keypad_+(숫자패드 +)를 눌러 리사이저를 다시 불러오세요.
Keypad_*(숫자패드 *)를 사용하면 화면상의 모든 텍스트 항목을 추가할 수 있습니다. 다만, 아주 많은 양이 한꺼번에 추가될 수 있으니 주의하세요!
경로(path) 내에 *를 사용하여 와일드카드를 표시할 수 있습니다 (즉, *가 있는 위치에서 0개 이상의 문자와 일치함). 이를 통해 하나의 리사이저 설정으로 많은 항목을 한꺼번에 처리할 수 있습니다.
패치에 zzAddedResizers.yaml 파일이 포함되어 있다는 점을 유의해 주세요. 따라서 설정을 계속 유지하고 싶다면 작업이 끝난 후 다른 yaml 파일로 옮기시기 바랍니다. 유용하다고 생각되는 리사이저 설정이 있다면 언제든 제출해 주세요!
설정 가능한 항목은 다음과 같습니다: (항목을 포함하지 않으면 컨트롤의 기본값이 유지됩니다.)

```yaml
- path: "GameStart/GameUIRoot/*/FormRoot" # GameStart/GameUIRoot으로 시작하고 내부에 FormRoot가 있는 모든 항목을 가져옵니다.
  sampleText: "Commission"    # 해당 경로가 무엇을 위한 것이었는지 알 수 있도록 추출된 텍스트입니다.
  idealFontSize: 30           # 원하는 글꼴 크기
  allowWordWrap: false        # 컴포넌트의 자동 줄바꿈 허용 여부
  allowAutoSizing: false      # 개발자가 설정한 너비에 따라 글꼴 크기가 자동으로 변하도록 허용
  allowLeftTrimText: true     # 텍스트의 왼쪽 잘림(left trim) 허용 여부
  adjustX: 0                  # 좌우 위치 조절을 위한 양수 또는 음수 값
  adjustY: 0                  # 상하 위치 조절을 위한 양수 또는 음수 값
  adjustWidth: 0              # 컨트롤의 허용 너비 조절을 위한 양수 또는 음수 값
  adjustHeight: 0             # 컨트롤의 허용 높이 조절을 위한 양수 또는 음수 값
  minFontSize: 0              # 자동 크기 조절(autosizing) 시 최소 글꼴 크기
  maxFontSize: 0              # 자동 크기 조절(autosizing) 시 최대 글꼴 크기
  lineSpacing: 0.0            # 텍스트 줄 간격
  characterSpacing: 0.0       # 텍스트 자간
  wordSpacing: 0.0            # 단어 간격
  fontPercentage: 0.70        # 사용할 글꼴 크기 비율 (0보다 크게 설정하면 위에서 설정한 max/min 글꼴 크기를 대체함)
  alignment: Center           # TextMeshProGUI의 화면 정렬 방식
  overflow: Overflow          # TextMeshProGUI의 넘침(overflow) 모드
```
