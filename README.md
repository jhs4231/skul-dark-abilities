# Skul Fan Dark Abilities — 0.2.0 개발 스냅샷

스컬의 팬 검은능력 모드입니다. **전체 구현·실전 검증이 끝난 정식 배포판이 아닙니다.**

원안: LDS_의 [일반 검은능력 구상](https://gall.dcinside.com/mgallery/board/view/?id=skul&no=58117), [추가 저주 구상](https://gall.dcinside.com/mgallery/board/view?id=skul&no=58552).
원안 그림과 게임 라이브러리는 포함하지 않았습니다. 아이콘은 코드로 생성합니다.

## 현재 작업 범위

- 일반 27개와 저주 29개, 총 56개의 정의·상점 등록과 공통 효과 코드.
- 와이번의 두 번째 슬롯을 위한 내부 정의 1개(총 내부 정의 57개).
- 상점 일반 능력 목록 페이지 전환: 화면 버튼 또는 Page Up / Page Down.
- 피해·회복·방어막·상태이상·대시·교대·소환·누적 상태 저장 처리.
- 운명의 스컬별 효과와 숙명의 스컬별 이득·대가 설계.
- 미정의 효과의 설계와 일반 능력/저주 수치는 `src/Catalog.cs`, `src/FateDesign.cs`, `src/SkullRules.cs`, `IMPLEMENTATION.md` 참고.

**정의가 등록되었다는 것은 해당 효과의 실전 검증 완료를 뜻하지 않습니다.**
현재 DLL은 최신 소스를 컴파일한 개발 스냅샷입니다. 이전 반복 빌드에서 통과한 검사와 최신 코드의 미검증 변경을 [검증 기록](VERIFICATION.md)에 구분했습니다.

## 구성

| 경로 | 내용 |
|---|---|
| `src/` | 모드 소스와 기본 비활성화된 개발 검증 코드 |
| `dist/FanDarkAbilities.dll` | 최신 소스의 빌드 결과 |
| `dist/SHA256SUMS.txt` | 빌드 DLL 해시 |
| `build.ps1` | 로컬 게임 라이브러리를 참조하는 빌드 스크립트 |
| `install.ps1` | BepInEx가 없는 새 설치용 스크립트 |
| `IMPLEMENTATION.md` | 두 원문의 판독 수치·범위 기록 |
| `VERIFICATION.md` | 현재 검증 범위와 미완료 항목 |
| `docs/VERIFICATION-0.1.1.md` | 재생 단독 시험판의 과거 검증 기록 |

게임 DLL, 역컴파일 자료, 외부 도구, 세이브, 원시 실행 로그, 개인 복원 스크립트는 Git 추적에서 제외합니다.

## 빌드

Windows .NET Framework C# 컴파일러와 로컬 Steam 게임 설치가 필요합니다.

1. BepInEx 5.4.23.2 x64의 코어 파일을 `tools/bepinex/BepInEx/core/`에 준비합니다.
2. 이 프로젝트에서 사용하는 Unity 2020.3.34용 전체 런타임 참조 파일을 `tools/unstripped/`에 준비합니다.
3. PowerShell에서 실행합니다.

```powershell
.\build.ps1 -GamePath 'C:\Program Files (x86)\Steam\steamapps\common\Skul'
```

의존 파일은 이 저장소에서 배포하지 않습니다. 검증에 사용한 게임 타이틀 버전은 `v1.09.02.3a3 / Demon King’s Castle Defense`이며 다른 버전은 미검증입니다.

## 설치

먼저 게임을 종료하고 세이브를 별도로 백업하세요. 이미 호환되는 BepInEx 및 `FanDarkAbilities_Runtime` 환경이 설치되어 있다면 DLL을 `BepInEx/plugins/FanDarkAbilities/`에 복사합니다.

`install.ps1`은 외부 의존 파일이 로컬 `tools/`에 준비되어 있고 기존 로더가 없는 **새 설치**만 대상으로 합니다. 기존 BepInEx 설치를 덮어쓰지 않습니다. 게임 실행 파일과 원래 `Managed` DLL은 교체하지 않습니다.

## 개발 기능

`Development/VerificationHotkeys`의 코드 기본값은 `false`입니다. 이를 켜면 진행 상태를 바꾸는 검증 기능이 활성화됩니다.

- F6: 재생 구매·강화·저장(재화 임시 보충 포함).
- F7: 재생 수치 검사.
- F8: 검은능력 상점 열기.
- F9: 정의·장착·단계와 선택한 피해 수치 검사.
- F10: 회복·피격·스킬/스컬 복제·변신 통합 검사.
- F11: 활성 게이트를 통한 다음 맵 이동.
- F12: 테스트 능력·카운터 저장 또는 재시작 복원 검사.

**F6·F10·F11·F12는 실제 진행을 바꿀 수 있으며, 원본 세이브 자동 복원을 보장하지 않습니다.**
일반 플레이에서는 개발 기능을 끄세요. F5는 현재 운명·숙명·무작위 효과의 설명 표시를 전환합니다.
