# Small Arena (GAME-3D-03) — SE2026-T17

Arena 3D góc nhìn thứ ba, tối đa **4 người/phòng**, dùng **Unity 6000.6.8f1**, **Mirror 96.0.1 + KCP Transport**, URP, New Input System và TextMeshPro/uGUI. Server quyết định vị trí, sát thương, HP, pickup, vòng đời và thời gian trận; client gửi ý định và hiển thị trạng thái được đồng bộ.

README được cập nhật cho **M3.2** trên nhánh `feature/tv1-m3-match-lifecycle`, baseline `5056403`, tiếp sau `d936b9a` (Fair Weapon Respawn) và `a07db32` (M3.1 + sửa teleport TH2). Thay đổi M3.2 đang ở working tree, chưa commit; không ghi là đã merge vào `main`.

## 1. Tính năng và trạng thái hiện tại

| Phần | Hành vi đã triển khai |
|---|---|
| Lobby M2 | Host/Join từ MainMenu, 4 slot đồng bộ ID/Host/Ready; chỉ Host Start khi có ít nhất 2 người và tất cả Ready. Từ chối người thứ 5 với room-full notice; tái sử dụng slot khi rời. |
| Di chuyển M1 | WASD theo trục thế giới, hướng +Z, tối đa 5 m/s; Space nhảy khoảng 1,2 m khi grounded. Server clamp input, mô phỏng CharacterController theo FixedUpdate và xử lý tách capsule giữa người chơi. |
| Melee và HP | **10 HP**, base **1 DMG**, **LMB**, cooldown **1,0 s**, cone **140°**, sphere radius 1,2 m và giới hạn reach tâm 1,8 m. Server kiểm tra hit/cooldown/range/Environment; client không tự trừ HP. Click UI không phát attack. |
| Loot mạng | **E** khi cách tâm hộp ≤1,5 m; pickup được server chiếm một lần, buff **+1 DMG → 2 DMG**. Carrier chết/disconnect thì mất buff, không drop tại chân; **sau 30 s** mới tìm điểm hồi sinh. Giữ vũ khí khi sống không chạy timer. |
| Fair Weapon Respawn | Server xét 24 candidate trên đúng sàn, tránh cover/tường/player, biên sàn 1 m và clearance 0,9 m; chọn **Maximin** khoảng cách X/Z tới người Alive gần nhất. Không có điểm an toàn thì chờ/retry; Ended hủy pending respawn. |
| Match Lifecycle M3.1 | `WaitingForPlayers → Countdown → InProgress → Ended`; binding **Connection–Session–Participant–Life**, clock `MM:SS` đồng bộ. Mặc định countdown 3 s, trận 180 s; chỉ InProgress + Alive được di chuyển/đánh/nhặt. |
| Chết và hồi sinh | Mặc định respawn sau 3 s tại marker an toàn, đầy HP/base damage và life ID mới; từ chối command của mạng sống cũ. `ArenaNetworkTransform` teleport đồng bộ physics, xóa interpolation cũ và giữ lịch sử delta compression để sửa TH2. Có cấu hình stocks hữu hạn; mặc định vô hạn. |
| Arena TV3 | `Arena_MVP`: sàn mở 16×16 m, 3–6 vật cản Cube/Sphere/Capsule sinh theo server và đồng bộ collider layout qua SyncList. Falling dưới Y=-5 gây chết trên server; spawn tránh cover và mép sàn. |
| Score và kết quả M3.2 | Server ghi Kills/Deaths/actual DamageDealt; sort Kills DESC → Deaths ASC → Damage DESC → SlotIndex ASC. Đạt 5 kills, hết giờ hoặc finite-life kết thúc sẽ chốt Rank 1/Winner. Giữ Tab xem bảng điểm, kill feed fade sau 4 s, result VICTORY / DEFEAT — Rank #X. |
| HUD và điều hướng | HUD HP/damage, overhead health, clock/phase/respawn; Escape mở menu, Resume hoặc Leave Match với teardown Host/Client. Room mode Ended đợi 10 s hoặc Host Return to Lobby: giữ connection/slot, reset Ready, bắt đầu round mới với điểm 0. |
| Hiển thị và offline | F11 toàn cục: FullScreenWindow ở độ phân giải native ↔ Windowed với **nửa chiều rộng và nửa chiều cao** màn hình. Single Player giữ WASD/Space/LMB/E, 10 HP và 1/2 DMG, Bot, Victory/Defeat và replay. Timer loot 30 s thuộc vòng chơi mạng. |

Server runtime tạo snapshot `GameplayConfiguration` phù hợp arena 10 HP/1 DMG. Constructor mặc định của model TV2 `rules-v0.1` vẫn là 100 HP/25 damage; không dùng các giá trị đó để mô tả tuning arena hiện hành.

M3.1, vòng loot và M3.2 có suite kiểm chứng riêng. Matchmaking, reconnect token, host migration, NAT relay, client prediction/reconciliation và chứng nhận Dedicated Server build còn ngoài phần đã bàn giao. Harness native không chứng nhận mọi tình huống hai tiến trình KCP/LAN/latency; mẫu offline chỉ là bằng chứng hành vi. Kết quả và phạm vi nghiệm thu M3.2 ở [báo cáo scoreboard/rematch](docs/testing/m3.2-scoreboard-and-win-loop.md).

## 2. Kiến trúc và thư mục

Có đúng **7 asmdef của nhóm**: năm production và hai test. `SmallArena.Client.UI` là **namespace trong SmallArena.Client**, không có asmdef riêng.

```text
Assets/_Project/
|-- Scenes/                 MainMenu.unity, Bootstrap.unity, Arena_MVP.unity
|-- Scripts/
|   |-- Shared/
|   |   |-- Domain/         SmallArena.Shared.Domain.asmdef
|   |   `-- Protocol/       SmallArena.Shared.Protocol.asmdef
|   |-- Networking/         SmallArena.Networking.asmdef; Mirror bridges/state/transport
|   |-- Server/             SmallArena.Server.asmdef
|   |   |-- Sessions/       ServerLobbyController, ServerMatchLifecycleController
|   |   |-- Simulation/     ServerPlayerMovement, ServerPlayerSeparation
|   |   |-- Combat/         ServerLootRespawnController, offline combat adapters
|   |   `-- Validation/     MovementInputValidator
|   |-- Client/             SmallArena.Client.asmdef
|   |   |-- Input/          PlayerInputHandler, NetworkCombatInput
|   |   |-- Presentation/   PlayerController, camera, health presentation
|   |   |-- UI/             namespace SmallArena.Client.UI: menu/lobby/HUD
|   |   `-- Networking/     vung mo rong client networking
|   `-- Editor/             vung cong cu Editor
|-- Prefabs/                Characters/, Combat/, Network/, Environment/, UI/
|-- Settings/               cau hinh/tai nguyen Unity cua nhom
`-- Tests/
    |-- EditMode/           SmallArena.EditMode.Tests.asmdef
    `-- PlayMode/           SmallArena.PlayMode.Tests.asmdef

Assets/Mirror/              Mirror va KCP duoc cai trong repository
Packages/                   manifest va package lock
ProjectSettings/            phien ban Unity, build scenes, input/physics/render settings
docs/                       kien truc, ket qua kiem chung, van hanh
tools/                      compiler, Domain runner, layout/XML verifiers, smoke tools
.github/workflows/          unity-ci.yml
```

Chiều tham chiếu nội bộ dưới đây là **A → B: A phụ thuộc B**; các đích rút gọn tiền tố `SmallArena.`:

```text
SmallArena.Shared.Domain      (khong tham chieu module khac hoac engine)
SmallArena.Shared.Protocol -------> Shared.Domain
SmallArena.Networking ------------> Shared.Protocol, Shared.Domain
SmallArena.Server ----------------> Networking, Shared.Protocol, Shared.Domain
SmallArena.Client ----------------> Networking, Shared.Protocol, Shared.Domain
SmallArena.EditMode.Tests --------> Shared.Domain
SmallArena.PlayMode.Tests --------> Server, Client, Networking, Shared.Protocol, Shared.Domain
```

- **Shared.Domain và Shared.Protocol thuần C#**, `noEngineReferences: true`; Domain không tham chiếu UnityEngine/Mirror. TV2 models và runtime/loot selector có thể kiểm thử không cần engine.
- Networking và Server dùng `Mirror`/`Mirror.Components`; Client dùng Mirror, Input System, TMP/uGUI. Test assemblies dùng NUnit/Unity Test Runner và package test tương ứng, production không tham chiếu test.
- **Không có tham chiếu vòng hoặc Server ↔ Client.** `NetworkPlayer` là bridge Command/lifecycle mỏng; input/camera/HUD nằm ở Client, movement/separation và điều phối loot ở Server. `NetworkCombat`/`NetworkHealth` nằm trong Networking nhưng xử lý hit/HP chỉ trên server, kết hợp hợp đồng Domain và replication.
- Pure client, kể cả owner, không chạy CharacterController physics hoặc ghi official pose; server mô phỏng rồi replicate ServerToClient. UI chỉ quan sát và gọi điều hướng, không chuyển phase hay xác nhận hit.

## 3. Ba thành viên tích hợp công việc thế nào?

Đây là phân công vai trò TV1–TV3; không suy diễn danh tính cá nhân hoặc độc quyền tác giả từ tên nhánh/commit.

| Vai trò | Trách nhiệm và điểm tích hợp hiện hữu |
|---|---|
| **TV1 — Server/Networking/Integration** | KCP connection lifecycle/spawn/authority; server movement, validation và separation; lobby 4 slot, Ready/Start, pause/teardown; tích hợp melee geometry, E pickup và respawn Maximin 30 s. `ServerMatchLifecycleController` nối session/participant/life với model TV2; `NetworkMatchController` đồng bộ clock/score/feed/winner, `ArenaNetworkTransform` sửa teleport; điều phối Ended → Lobby và rematch. Phối hợp UI/input/arena với TV3 và giữ luật/config theo TV2. |
| **TV2 — Domain/Rules** | Model thuần C# `GameplayConfiguration`, `OnlineCharacterState`, `OnlineMatch` và định nghĩa `rules-v0.1` (PR #9). Server runtime giữ snapshot config, dùng deadline/life/cooldown/HP contracts; TV2 tiếp tục sở hữu quy tắc combat/validation để tránh hai bộ luật mâu thuẫn. |
| **TV3 — Client/UI/Arena** | Arena layout, `ArenaObstacles`, cover/collider và layout/collision verification (PR #7/#11); loading/recovery lobby → arena (PR #12). Giữ biên presentation/input/UI/scene/prefab, phối hợp TV1 để collider descriptions và vùng spawn được server kiểm chứng và client dựng nhất quán. |

Lịch sử tích hợp tiêu biểu: PR #10 cho M2 lobby; PR #8 cho Unity CI; PR #9 cho model TV2; PR #11/#12 cho arena/loading TV3; `a07db32` cho M3.1/TH2; `d936b9a` cho Fair Weapon Respawn. Xem `git log --oneline -n 20` và `git branch -a` để đối chiếu checkout đang dùng.

### Git, PR và cổng chất lượng

1. Tạo nhánh theo vai trò/chức năng, ví dụ `feature/tv1-...`, `feature/tv2-...`, `feature/tv3-...` → PR được review → `main`. Lịch sử cũng có `feature/TV2-...` và `codex/tv3-...`; chọn mẫu nhất quán cho công việc mới, không đổi tên các nhánh đang tồn tại chỉ vì tài liệu.
2. PR mô tả vấn đề, hành vi cuối, module/asset ảnh hưởng và bằng chứng test. Thay đổi luật/config cần review TV2; thay đổi giao thức/lifecycle cần TV1; scene/UI/collider cần TV3 phối hợp. Đổi hoặc di chuyển asset phải giữ `.meta`/GUID.
3. Trước merge: `git diff --check`, graph/GUID/build-order hợp lệ, **7/7 assembly không lỗi/cảnh báo compiler mã nhóm**, native EditMode và PlayMode không lỗi. Ghi rõ skip vendor và phạm vi smoke nhiều tiến trình; Roslyn không thay native physics/weaving.
4. [Unity CI](.github/workflows/unity-ci.yml) chạy trên PR vào `main` và push `main`: job Git diff trên Ubuntu 24.04; job Unity trên Windows Server 2022 dùng GameCI `testMode: all`, kiểm hai XML bằng `Assert-UnityTestResults.ps1`, lưu artifact `unity-test-results` 7 ngày. Phiên bản Editor lấy từ `ProjectVersion.txt`.
5. CI cần repository secrets `UNITY_LICENSE`, `UNITY_EMAIL`, `UNITY_PASSWORD`; xem [hướng dẫn CI](.github/workflows/README.md). PR từ fork bỏ qua job Unity do không có secrets. Workflow hiện không gọi strict Roslyn compiler và không tự build/phát hành server/client; thực hiện compiler gate cục bộ bên dưới. Quy tắc bảo vệ nhánh và kết quả CI trên GitHub cần kiểm tra trong Settings/Checks, không được chứng minh chỉ bằng YAML.

Commit mã, asset + `.meta`, `Packages/`, `ProjectSettings/` và tài liệu. Không commit `Library/`, `Temp/`, build binaries, `TestResults/`, log hoặc secrets. `AGENTS.md`/`PROJECT_CONTEXT.md` là ngữ cảnh cục bộ bị ignore, không phải tài liệu yêu cầu người clone repository phải có. Giữ quy ước PascalCase; tránh tập trung mọi trách nhiệm vào `Managers`/`Utils` hoặc một NetworkPlayer monolith.

## 4. Quickstart

### Chuẩn bị

- Unity Hub với **Unity 6000.6.8f1** đã kích hoạt license; Windows Build Support cho bài thử Editor + Windows Standalone. Mở đúng gốc có `Assets/`, `Packages/`, `ProjectSettings/`, chờ import và Mirror weaving hoàn tất.
- Mirror/KCP đã ở `Assets/Mirror`; package manifest có URP/Input System/TMP/uGUI. Giữ manifest/lock và GUID, không cài thêm một bản Mirror trùng.
- PowerShell cho công cụ kiểm chứng. Domain runner cần .NET SDK/Roslyn: script hiện dùng **SDK 8.0.318, runtime/reference 8.0.21 đi kèm Editor**, qua `-EditorRoot`; không có dự án `dotnet test` riêng để gọi thay script.
- Scene build order đã đăng ký: **MainMenu 0 → Bootstrap 1 → Arena_MVP 2**. MainMenu là điểm vào thường lệ; Bootstrap trực tiếp là đường debug networking cũ.

### Single Player

1. Mở `Assets/_Project/Scenes/MainMenu.unity`, nhấn Play → **Single Player**.
2. WASD di chuyển, Space nhảy grounded, LMB đấm, E nhặt hộp vàng. Camera giữ hướng +Z; pickup buff damage 1 → 2, Bot bắt đầu 10 HP.
3. Bot chết → Victory; rơi void → Defeat. Dùng Play Again hoặc Main Menu; Escape mở menu/Resume/Leave Match. Tutorial hiện vẫn Coming Soon.

### Hai client: Editor + Standalone

1. Build Windows Standalone bằng Unity Editor với scene list trên; build lại sau thay đổi protocol, prefab hoặc teleport. Server/Editor/client phải cùng phiên bản mã.
2. Editor mở MainMenu → Play → Multiplayer → **Host Room**. Chạy executable → Multiplayer → nhập **localhost** hoặc **127.0.0.1** → **Join Room**. Phòng mặc định dùng KCP **UDP 7777**; qua LAN dùng IP máy Host và cho phép cổng đó qua firewall.
3. Cả hai Ready; Host Start chỉ mở khi đủ ít nhất 2 người và tất cả Ready. Đợi loading và countdown 3 s, rồi kiểm tra movement/HP/clock trên cả hai. Phòng tối đa 4 người; người thứ 5 nhận room-full notice.
4. Trong standalone, F11 chuyển fullscreen/native ↔ cửa sổ rộng/cao bằng nửa màn hình để đặt cạnh Editor. Kiểm tra độ phân giải cửa sổ ở standalone; Game view trong Editor không phải cửa sổ player độc lập.
5. Đứng trong reach, phía +Z của người đánh; LMB mỗi 1 s, HP giảm 1 hoặc 2 khi có buff. E nhặt loot. Carrier chết: buff mất, avatar hồi sinh an toàn sau 3 s; hộp mới chỉ có sau **30 s từ lúc carrier chết/disconnect**, ở điểm Maximin an toàn. Khi sống giữ buff hơn 30 s không sinh thêm hộp.
6. Giữ Tab xem Kills/Deaths/Damage và trạng thái; nhả để ẩn. Lethal hit hiện kill feed hai bên. Đạt 5 kills hoặc hết giờ: input khóa, hiện VICTORY / DEFEAT và bảng xếp hạng theo bốn khóa.
7. Đợi 10 s hoặc Host bấm Return to Lobby: cả hai trở về phòng cũ, giữ slot nhưng Not Ready. Ready/Start lần nữa, điểm/feed phải reset. Escape → Leave Match vẫn dọn phiên; Host rời dừng server và client quay về menu.

Chi tiết: [M1 nhiều client](docs/testing/m1-multi-client-testing.md), [lobby governance](docs/testing/m2-lobby-governance.md), [M3.1/TH2](docs/testing/m3.1-match-lifecycle.md), [Fair Weapon Respawn](docs/testing/weapon-respawn-fair-spawn.md), [M3.2 score và rematch](docs/testing/m3.2-scoreboard-and-win-loop.md).

## 5. Kiểm thử và biên dịch

Kết quả M3.2 ngày 21/11/2026, có XML/log và hash 252 tệp nguồn/asset/metadata khớp bản sao kiểm chứng:

| Bộ kiểm chứng | Kết quả gần nhất |
|---|---|
| EditMode của nhóm | **236/236 đạt**, 0 lỗi, 0 skip |
| PlayMode của nhóm | **344/344 đạt**, 0 lỗi, 0 skip |
| PlayMode gồm package | **348 tổng: 346 đạt, 0 lỗi, 2 skip vendor** |
| Strict compilation | **7/7 asmdef**, 0 lỗi/cảnh báo compiler mã nhóm; native import/Mirror weaving thành công |
| Kiểm chứng loot mới | **25 Domain + 6 PlayMode**, đã nằm trong tổng; có ca đợi thật 30 s và 20 chu kỳ respawn |
| Kiểm chứng M3.2 mới | **24 Domain + 7 PlayMode**, đã nằm trong tổng; thêm 7 ca cấu hình hợp lệ/không hợp lệ. Có scoring/replication, Tab/feed/result, nonzero-score reset và round hai giữ nguyên connection. |

Hai skip thuộc package Input System, mouse Windows/RDP, lý do `Unstable due to 1252825`; không tính thành pass. Native M3.1/loot dùng Host hoặc client engine thật cùng connection/observer guest ảo. Kết quả hai cửa sổ/KCP và các giới hạn được ghi riêng trong từng báo cáo, không suy từ số test native.

### Unity Test Runner và CLI

Trong Editor: mở **Test Runner**, chọn EditMode → Run All và PlayMode → Run All. Trên Windows, chạy block sau từ gốc repository **đã đóng Editor**, hoặc từ một bản sao riêng đã đồng bộ sạch; không chạy hai Editor trên cùng project. Các lệnh thực thi tuần tự và kiểm exit code:

```powershell
$editorRoot = 'C:\Program Files\Unity\Hub\Editor\6000.6.8f1\Editor'
$projectPath = (Get-Location).Path
$resultRoot = Join-Path $projectPath 'TestResults/Local'
$logRoot = Join-Path $projectPath 'Logs'
New-Item -ItemType Directory -Force -Path $resultRoot, $logRoot | Out-Null

foreach ($platform in @('EditMode', 'PlayMode')) {
    $xml = Join-Path $resultRoot ($platform.ToLowerInvariant() + '-results.xml')
    $log = Join-Path $logRoot ('Local-' + $platform + '.log')
    $arguments = @('-batchmode', '-nographics',
        '-projectPath', ('"{0}"' -f $projectPath), '-runTests', '-testPlatform', $platform,
        '-testResults', ('"{0}"' -f $xml), '-logFile', ('"{0}"' -f $log))
    $process = Start-Process -FilePath (Join-Path $editorRoot 'Unity.exe') `
        -ArgumentList $arguments -WindowStyle Hidden -PassThru
    $process.WaitForExit()
    if ($process.ExitCode -ne 0) { throw "$platform failed: exit $($process.ExitCode). See $log" }
}
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Assert-UnityTestResults.ps1 -ResultsRoot $resultRoot
```

Không thêm `-quit` vào `-runTests`. Nếu license trả exit 198, kích hoạt lại trong Unity Hub trước khi chạy lại. Lọc riêng bằng `-testFilter SmallArena.Tests.EditMode.FairLootSpawnTests` hoặc `SmallArena.Tests.PlayMode.WeaponRespawnAndObstaclesTests`; suite đầy đủ mới bao phủ các regression M1/M2/M3.1.

### Công cụ cục bộ trước PR

```powershell
$editorRoot = 'C:\Program Files\Unity\Hub\Editor\6000.6.8f1\Editor'
git diff --check
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Verify-Scaffold.ps1 -EditorRoot $editorRoot
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Compile-UnityAssemblies.ps1 -EditorRoot $editorRoot
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Run-DomainTests.ps1 -EditorRoot $editorRoot
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Verify-ArenaLayout.ps1 -EditorRoot $editorRoot
```

Roslyn compiler coi cảnh báo mã nhóm là lỗi; Domain runner chạy 236 ca thuần C# bằng SDK của Editor. Layout verifier kiểm YAML/scene/prefab và có bước compile; các công cụ này bổ sung, không thay native PlayMode. Báo cáo thô M3.2 ở `TestResults/M32/`, log ở `Logs/` được ignore; người clone mới xem kết quả bàn giao trong tài liệu và tự chạy lại, không cần tải cache của người khác.

## 6. Tài liệu liên quan

| Chủ đề | Nguồn |
|---|---|
| Kiến trúc và authority | [Cấu trúc](docs/architecture/structure.md), [Authority](docs/architecture/authority.md), [Protocol](docs/protocol/protocol.md) |
| Kết nối và movement | [TV1.1](docs/architecture/tv1.1-network-lifecycle.md), [TV1.2](docs/architecture/tv1.2-server-movement.md), [M1 verification](docs/testing/tv1.3-results.md) |
| Lobby, combat, navigation | [M2 lobby](docs/architecture/m2-lobby-rooms.md), [Combat/HUD](docs/architecture/network-combat-and-hud.md), [Melee/F11](docs/testing/melee-reliability-and-display.md), [Pause/teardown](docs/testing/in-game-menu-and-teardown.md) |
| Arena và loading | [TV3.1 arena](docs/testing/tv3-1-open-arena.md), [Lobby → Arena](docs/testing/lobby-arena-loading.md) |
| Runtime và kết quả theo mốc | [M3.1 + TH2](docs/testing/m3.1-match-lifecycle.md), [Fair Weapon Respawn: 205/337](docs/testing/weapon-respawn-fair-spawn.md), [M3.2 score/rematch: 236/344](docs/testing/m3.2-scoreboard-and-win-loop.md) |
| Vận hành và CI | [Runbook](docs/operations/runbook.md), [Test plan](docs/testing/test-plan.md), [CI setup](.github/workflows/README.md) |

Một số tài liệu kiến trúc/kiểm chứng cũ chứa scaffold, ví dụ dự kiến hoặc trạng thái theo từng mốc. Đọc ngày/phạm vi của báo cáo; dùng README này cùng báo cáo M3.1/loot/M3.2 để xác định tính năng hiện hành, không coi mọi ví dụ trong lịch sử là tính năng đã triển khai.
