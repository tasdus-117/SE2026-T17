# TV1.3 — kết quả native và nghiệm thu M1 (2026-11-06)

## Kết luận

**M1 đã được kiểm chứng và ký nghiệm thu trong phạm vi lifecycle, spawn, authority và movement/replication qua harness PlayMode trong engine.** Cả TV1.1, TV1.2 và bảy ca TV1.3 đã thực thi bằng Unity Test Runner và đạt. Dự án sẵn sàng bước sang M2; nhiệm vụ này không triển khai M2.

Editor thực tế: **Unity 6000.6.8f1 (237b6593792b)**, tại `C:\Program Files\Unity\Hub\Editor\6000.6.8f1\Editor\Unity.exe`. `ProjectVersion.txt` trùng phiên bản này; không sử dụng 6000.0.7f1. Chạy native trong bản sao `TestResults/TV11/ValidationProject`, giữ nguyên nguồn và cấu hình người dùng tại project gốc.

## Số liệu từ XML cuối

| Suite/phạm vi | Tổng | Đạt | Lỗi | Bỏ qua |
| --- | ---: | ---: | ---: | ---: |
| Toàn bộ PlayMode được phát hiện | 216 | 214 | 0 | 2 |
| SmallArena.PlayMode.Tests.dll | 212 | 212 | 0 | 0 |
| TV1.1 — NetworkLifecycleTests | 8 | 8 | 0 | 0 |
| TV1.2 — NetworkMovementTests | 23 | 23 | 0 | 0 |
| TV1.3 — MultiClientIntegrationTests | 7 | 7 | 0 | 0 |
| EditMode của nhóm | 34 | 34 | 0 | 0 |

Các dòng TV1 là tập con của 212 ca nhóm, không cộng trùng. **100% test PlayMode của nhóm đạt**, gồm toàn bộ ca offline cũ. Hai skip thuộc `Unity.InputSystem.IntegrationTests.dll`: `WindowsInput_MouseMovements_AreDetected` và `WindowsInput_RemoteDesktopMouseMovements_AreDetected`, cùng lý do package **“Unstable due to 1252825”**. Không tính skip thành pass hoặc tuyên bố 216/216 đạt. Root XML ghi `Skipped:Ignored` vì hai ca vendor này; assembly nhóm có trạng thái `Passed`.

Trích nội dung kết quả đã thực thi:

```text
PlayMode: total=216 passed=214 failed=0 skipped=2
SmallArena.PlayMode.Tests.dll: total=212 passed=212 failed=0 skipped=0
MultiClientIntegrationTests: total=7 passed=7 failed=0 skipped=0
EditMode: total=34 passed=34 failed=0 skipped=0
Test run completed. Exiting with code 0 (Ok). Run completed.
```

Bằng chứng: `TestResults/TV13Verified/PlayMode.xml`, `EditMode.xml`; log cuối `Logs/TV13Verified-PlayMode-hub.log` và `Logs/TV13Verified-EditMode-final.log`. Cả hai Test Runner báo thoát mã **0**; mã 198 không còn chặn hai lần chạy thành công này. Trước khi người dùng xác nhận lại license trong Hub, một số lần thử vẫn gặp mã 198; không coi lần chạy thất bại do license là kết quả test.

## Bảy ca TV1.3 đã đạt

| Ca | Kết quả |
| --- | --- |
| TwoClients_SpawnDistinctOwnedPlayers_AtRegisteredMarkers | Đạt |
| InputFromA_CannotControlB_EvenWithForgedCommandTarget | Đạt |
| ServerMovement_ReplicatesBothPlayers_ToSeparateClientProxies | Đạt |
| VirtualDisconnect_UnspawnsB_AndNativeClientContinuesMoving | Đạt |
| RepeatedDisconnect_ReleasesMarker_AndReplacementGetsFreshIdentity | Đạt |
| HackedClientVector_IsClamped_WithoutMovingOtherAvatar | Đạt |
| NativeClientDisconnect_PublishesOneDespawn_AndCleansBothSides | Đạt |

Fixture dùng ArenaNetworkManager, floor/bốn marker và Player prefab thật. Một native NetworkClient kết nối bằng transport hàng đợi bộ nhớ, dùng proxy tách khỏi server object; connection B ảo gửi Ready/AddPlayer/Command đã serialize vào callback server thật. Native client dùng handler spawn/EntityState/destroy và NetworkTransformReliable chuẩn. Không tự gán ownership bằng reflection hoặc gọi trực tiếp simulation receiver.

Đã xác nhận spawn riêng, input owner cô lập, Command giả target bị từ chối, clamp 5 m/s theo tick server, nhận pose trung gian/hội tụ, destroy proxy và dọn owned/connection, event local despawn một lần, tái sử dụng marker và client còn lại tiếp tục di chuyển.

## Sửa lỗi phát hiện trong lần chạy thật

Lần native đầu (`PlayMode-initial.xml`, `Logs/TV13Verified-PlayMode-initial.log`) có 207 đạt / 7 lỗi / 2 bỏ qua. Hai lời gọi `LogAssert.NoUnexpectedReceived()` trong fixture TV1.3 coi log thành công của Mirror như “Server started listening” và log disconnect là lỗi. Đã bỏ yêu cầu toàn bộ luồng log phải rỗng, giữ nguyên assertion gameplay, authority, replication và teardown. Test Runner vẫn tự đánh trượt Error/Exception/Assert bất ngờ; cảnh báo Command giả authority vẫn có `LogAssert.Expect` cụ thể. Không bật `ignoreFailingMessages` hoặc che lỗi console. Toàn bộ suite đã chạy lại đạt sau sửa, không chỉ chạy riêng ca lỗi.

## Biên dịch và tính toàn vẹn

- Native Unity compilation và Mirror weaving hoàn tất; log cuối PlayMode/EditMode không có lỗi C#, lỗi Weaver hoặc thiếu tham chiếu/script. Còn cảnh báo analyzer trong package/vendor Mirror; không sửa vendor để che cảnh báo.
- Roslyn **7/7 asmdef nhóm đạt**, cảnh báo mã nhóm là lỗi; bằng chứng `TestResults/TV13Verified/Compilation/results.json`.
- Scaffold xác nhận **1.442 cặp metadata/GUID**, bảy assembly không vòng và MainMenu 0 / Bootstrap 1 / Arena_MVP 2. Công cụ giờ kiểm tra phiên bản khai báo của project khớp Editor được chọn, thay vì ghim 6000.6.7f1.
- Saved layout đạt **2.790 assertion / 12 YAML**; nguồn/test/scene/prefab và metadata được đối chiếu hash với bản sao thực thi. Snapshot ở `TestResults/TV13Verified/tested-source-hashes.json`; scaffold/layout ở `scaffold.txt`, `layout.txt` cùng thư mục.
- Không sửa hoặc hoàn nguyên package, scene, prefab, Input Actions và cấu hình người dùng. Không stage, commit hoặc push. Context/AGENTS, log và XML giữ cục bộ theo Git ignore.

## Chạy lại và giới hạn nghiệm thu

Xem [hướng dẫn Test Runner](m1-multi-client-testing.md). Có thể chạy cùng native CLI sau khi license Editor trong Hub hoạt động:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.6.8f1\Editor\Unity.exe' -batchmode -nographics -projectPath '<project-kiem-chung>' -runTests -testPlatform PlayMode -testResults '<PlayMode.xml>' -logFile '<PlayMode.log>' | Out-Null
& 'C:\Program Files\Unity\Hub\Editor\6000.6.8f1\Editor\Unity.exe' -batchmode -nographics -projectPath '<project-kiem-chung>' -runTests -testPlatform EditMode -testFilter SmallArena.Tests.EditMode -testResults '<EditMode.xml>' -logFile '<EditMode.log>' | Out-Null
```

Nghiệm thu theo harness trong engine đã thống nhất: một NetworkClient thật và một connection/observer ảo. Không chứng nhận hai OS client, KCP timeout, mạng latency/load, UI/combat mạng, reconnect token, prediction/reconciliation hoặc Dedicated Server subtarget. M2 còn cần lobby/ready/tối đa bốn người/từ chối người thứ năm và cleanup khi tải scene.
