# TimeTheft

Unity 6 (`6000.0.68f1`) 기반의 독립 2D 개발 저장소다. 개발 기준은 [TimeTheft/TimeTheft](https://github.com/TimeTheft/TimeTheft)의 `main`, 로컬 경로는 `C:\unity\TimeTheft`다. 2026-11-20의 2D Worktree 파일을 새 Git 이력으로 가져왔으며, 기존 Match·AI·3D 기반 코드와 에셋을 유지한다.

2D는 [TimeTheft2DStageStudy](Assets/Scenes/_Development/TimeTheft2DStageStudy.unity)와 A/B 비교 씬의 시안 단계다. 초기 커밋은 2D 개발 시작 기준점이며 완성된 2D 게임을 의미하지 않는다. 기존 타이틀·게임 빌드 목록은 유지한다.

- 보존된 3D 빌드 진입점은 [TimeTheftTitle](Assets/Scenes/TimeTheftTitle.unity) → [TimeTheftGame](Assets/Scenes/TimeTheftGame.unity)이다.
- Study·Validation 씬은 [Assets/Scenes/_Development](Assets/Scenes/_Development)에 있다.
- 개발 지침은 [AGENTS.md](AGENTS.md), [Unity 하네스](.docs/harness/UNITY.md), [Git 하네스](.docs/harness/GITHUB.md)를 참고한다.
- [HANDOFF](.docs/project/HANDOFF.md)에서 현재 구현과 검증 이력을, [기획서 안내](.docs/project/GAME_DESIGN.md)에서 Notion 원문을 찾을 수 있다.

```text
Assets/
  Animations/  Art/  Fonts/  Input/  Prefabs/
  Scenes/      Scripts/      Settings/  Tests/  TextMesh Pro/
Packages/     ProjectSettings/     Tools/
.docs/        .agents/            .vscode/
```

게임잼 제출 빌드와 제작 원본·과거 실험 자료는 비공개 [TimeTheft-Preservation](https://github.com/TimeTheft/TimeTheft-Preservation)에 보존되어 있다. 기존 3D는 [TimeTheft-3D](https://github.com/TimeTheft/TimeTheft-3D)에 보존하며, 구 Worktree와 로컬 복구 자료는 `C:\unity\TimeTheft-Backups`에 보관한다.
