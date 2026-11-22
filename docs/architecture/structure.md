# Cấu trúc và trách nhiệm

## Trạng thái sau di trú

Đối chiếu main `995e6a4` ngày 22/11/2026: project có offline, lobby, combat mạng và lifecycle M3. Nguồn phục hồi, GUID, namespace tương thích và bảy assembly nằm trong [báo cáo di trú](main-migration-audit.md). Các tên ví dụ trong phần bố trí bên dưới là hướng dẫn kiến trúc; xem [contract/runtime hiện hành](../protocol/tv2-004-contracts.md) để biết entry point thật. PROJECT_CONTEXT.md là context local bị ignore, không phải dependency của tài liệu bàn giao.

Scene hiện có: MainMenu (index 0), Bootstrap (index 1), Arena_MVP (index 2). GameManager/readiness ở Server/Simulation; Health/combat/pickup ở Server/Combat; local PlayerController/camera ở Client/Presentation; input ở Client/Input; HUD/menu ở Client/UI. Player/Bot prefab ở Characters, pickup ở Combat. Runtime tổng hợp cũ được thay bằng Shared.Protocol và hợp đồng Shared; không có tham chiếu Client↔Server.

[TV1.1](tv1.1-network-lifecycle.md) thêm ArenaNetworkManager/NetworkPlayer. [TV1.2](tv1.2-server-movement.md) thêm MovementInput, NetworkMovement và ServerPlayerMovement; ArenaNetworkTransform thực hiện ServerToClient replication và teleport qua NetworkTransformReliable. OfflineMatchScope tách actor/HUD offline. NetworkCombat/NetworkHealth đã chạy melee mạng; MatchSessionRuntime dùng OnlineMatch/OnlineCharacterState trong production qua ServerMatchLifecycleController. Retained-session reconnect/token chưa được triển khai/chứng nhận. Báo cáo M1 là lịch sử theo baseline riêng.

M2 TV1 bổ sung LobbyMessages trong Networking, LobbyRoom/ServerLobbyController trong Server/Sessions, view/controller UI trong Client/UI và ArenaNetworkRoot prefab cho MainMenu. [Phòng chờ cơ bản](m2-lobby-rooms.md) đã kiểm chứng Host/Join/Ready/Start và capacity bốn người; [kết quả native/standalone](../testing/m2-lobby-results.md) ghi rõ phạm vi. Bootstrap M1 trực tiếp và các tính năng offline vẫn giữ riêng.

## Gốc repository

- `Assets/`: mã và tài nguyên Unity nhập vào project.
- `Assets/_Project/`: nội dung do nhóm sở hữu; tiền tố `_` giúp dễ tìm trong Project view.
- `Assets/ThirdParty/`: tài nguyên ngoài nhóm nếu nhà cung cấp cho phép bố trí ở đây. Package và plugin phải giữ cấu trúc cài đặt chính thức; không ép mọi plugin vào thư mục này.
- `docs/`: tài liệu bàn giao và vận hành, không đóng gói vào game.
- `config/`: mẫu cấu hình chạy cho các môi trường, không chứa mật khẩu hoặc token thật.
- `tools/`: công cụ kiểm thử bên ngoài game.
- `scripts/`: tự động hóa build và chạy; khác với mã C# trong Assets.
- `.github/workflows/`: Unity CI đã có unity-ci.yml: EditMode/PlayMode, XML gate, diff check và artifact upload; theo dõi cải tiến gate tại #18.
- `Packages/`, `ProjectSettings/`: Unity tạo khi khởi tạo project thật; phải quản lý bằng Git.
- `Library/`, `Temp/`, `Builds/`, `TestResults/`, `reports/`: dữ liệu phát sinh; không commit.

## Scenes

Build settings hiện có MainMenu, Bootstrap và Arena_MVP. MainMenu chứa Host/Join lobby UI bằng prefab; không có Lobby.unity riêng. Bootstrap là đường chẩn đoán M1. Arena_MVP phục vụ trận mạng và mẫu offline qua scope riêng.

## Prefabs

- `Characters/`: Player prefab có định danh mạng và các thành phần nhân vật cần thiết.
- `Combat/`: projectile nếu dùng đạn bay, hiệu ứng và vật thể chiến đấu tái sử dụng.
- `Environment/`: vật cản và thành phần bản đồ dùng nhiều lần.
- `UI/`: dòng người chơi trong lobby, bảng điểm, thanh máu.

Không phải prefab nào cũng là network object. UI và hiệu ứng thuần hiển thị thường không cần đồng bộ thành đối tượng mạng.

## Scripts/Shared

`Domain/` chứa kiểu và quy tắc dùng chung, ví dụ `PlayerId.cs`, `MatchPhase.cs`, `WeaponRules.cs`. Ưu tiên kiểu dữ liệu C# thuần khi thực tế cho phép, tránh phụ thuộc giao diện hoặc Mirror.

`Protocol/` chứa hợp đồng ứng dụng như `PlayerInputCommand.cs`, `FireCommand.cs`, `PlayerSnapshot.cs`, mã lỗi và phiên bản giao thức. Đây là ý nghĩa dữ liệu, không phải nơi mở socket. Server luôn kiểm tra lại dữ liệu dù client dùng cùng kiểu hoặc cùng quy tắc.

## Scripts/Client

- `Input/`: đọc bàn phím/chuột, chuyển thành ý định. Ví dụ `PlayerInputReader.cs`.
- `Presentation/`: camera, animation, âm thanh, hiển thị nhân vật; nội suy và dự đoán nếu triển khai. Ví dụ `PlayerCamera.cs`, `RemotePlayerPresenter.cs`.
- `UI/`: menu, lobby, HUD, bảng điểm và thông báo kết nối. Ví dụ `LobbyView.cs`, `MatchHud.cs`.
- `Networking/`: vòng đời kết nối và yêu cầu reconnect phía client. Ví dụ `ClientConnectionController.cs`.

Client có thể dự đoán di chuyển để phản hồi nhanh nhưng không quyết định máu, điểm hoặc kết quả trận đấu.

## Scripts/Server

- `Simulation/`: bước mô phỏng, chuyển động hợp lệ, trạng thái trận, spawn/respawn. Ví dụ `MatchSimulation.cs`, `ServerMovementSystem.cs`.
- `Combat/`: thời gian chờ, đạn, sát thương và kiểm tra trúng. Ví dụ `ServerCombatSystem.cs`, `HitValidator.cs`.
- `Sessions/`: danh sách người chơi, ready, vào/rời phòng, giữ chỗ và phục hồi phiên. Ví dụ `LobbyService.cs`, `ReconnectService.cs`.
- `Validation/`: kiểm tra đầu vào, giới hạn tần suất và điều kiện hợp lệ. Ví dụ `InputValidator.cs`, `CommandRateLimiter.cs`.

Quy tắc cụ thể của combat nằm gần combat; chỉ tách kiểm tra dùng lại sang Validation. Không xây hai bộ quy tắc mâu thuẫn.

## Scripts/Networking

Đây là lớp nối code game với Mirror, dùng ở hai phía theo trách nhiệm.

- `Transport/`: khởi tạo/kết thúc kết nối, adapter cho callback của framework, chuyển thông điệp sang handler. Ví dụ `ArenaNetworkManager.cs`. Tên thư mục không có nghĩa phải tự viết TCP/UDP hoặc thay thế transport có sẵn của Mirror.
- `Replication/`: các NetworkBehaviour, ánh xạ trạng thái server sang dữ liệu đồng bộ và chuyển input hợp lệ đến server. Ví dụ `NetworkPlayer.cs`, `PlayerStateReplicator.cs`.

`NetworkPlayer` là cầu nối, không phải nơi chứa toàn bộ di chuyển, UI, xác thực và tính sát thương. Lớp Networking không tự quyết định một phát bắn được bao nhiêu điểm.

## Editor/Build

`Scripts/Editor/Build/` chứa mã build chỉ chạy trong Unity Editor, ví dụ `ClientBuild.cs`, `ServerBuild.cs`. Đây không phải logic runtime của server. Thư mục Editor và cấu hình assembly phù hợp giúp tách công cụ Editor khỏi player build.

## Art, Audio, Settings

`Art/` chia Models, Materials, Textures, Animations và VFX. `Audio/` lưu nhạc/âm thanh. `Settings/` lưu asset cấu hình do nhóm tạo: cấu hình vũ khí, trận đấu, hiển thị… Dữ liệu quyết định luật trận đấu được server tải và áp dụng; client sửa bản cục bộ không được làm đổi luật server.

Tránh dùng `Resources/` như kho chứa toàn bộ project. Chỉ thêm cơ chế tải tài nguyên đặc biệt khi có nhu cầu cụ thể.

## Tests và tools

`Tests/EditMode/`: kiểm tra quy tắc thuần, giới hạn input, cooldown, vòng đời phiên.

`Tests/PlayMode/`: kiểm tra tích hợp với scene, vật lý và thành phần Unity. Kiểm thử nhiều tiến trình client/server vẫn cần bộ chạy bên ngoài, không mặc định được chứng minh bằng một PlayMode test.

`tools/load/`: tạo nhiều client hoặc bot kiểm thử. Phải phân biệt tải ở mức giao thức với tải đầy đủ của nhân vật và vật lý.

`tools/latency/`: cấu hình/kịch bản áp dụng delay, jitter, loss. Chọn công cụ phù hợp hệ điều hành và transport; công cụ chỉ làm chậm HTTP không đủ đại diện cho transport game.

`tools/cheat/`: client hoặc bộ phát thông điệp sai luật dùng trong môi trường thử nghiệm do nhóm quản lý.

## Quy mô vừa đủ

Một repository, một Unity project, hai mục tiêu build client/server. Chưa cần microservices, database, tài khoản, Redis hoặc Kubernetes khi yêu cầu chưa có. Reconnect trong bản đầu có thể dùng phiên lưu trong RAM; server khởi động lại thì các phiên đó mất. Muốn khôi phục sau khi server chết là yêu cầu khác.

Các tên file ví dụ trong phần định hướng là gợi ý triển khai; không phải tính năng đã hoàn thành. Danh mục thực tế đã có nằm ở mục “Trạng thái sau di trú” đầu tài liệu và PROJECT_CONTEXT.md.
