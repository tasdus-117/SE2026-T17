# Learning & Research Plan — Unity/C# online FFA

Phiên bản 0.3, 2026-11-01. Kế hoạch học PROPOSED, không có bài học/thí nghiệm nào được ghi hoàn thành. Unity/C# là CONFIRMED; exact Editor/package/controller/network framework chưa duyệt. [SRS](srs.md) sở hữu yêu cầu; [product roadmap](../docs/ROADMAP.md) sở hữu milestone/feature, không dùng lịch học làm delivery promise.

## Mục tiêu và giới hạn

Ba sinh viên học Unity/C#, dùng Codex/Claude và human review có ghi nhận đúng. Không giả định số giờ/tuần, không áp lịch học 7/14/30 ngày cũ vào horizon sản phẩm 60 ngày từ 2026-11-01. Học vừa đủ cho bounded feature tiếp theo; không chờ xây offline game lớn rồi mới học networking. Offline stationary bot chỉ là bài tập tùy chọn, không current MVP.

<a id="6-prioritized-7--14--and-30-day-plans"></a>

## Thứ tự học gắn dependency thực

Anchor lịch sử 7/14/30-day được giữ để link v0.2 không gãy; lịch đó đã được thay thế, không phải delivery baseline.

| Thứ tự / checkpoint | Nội dung học | Feature được hỗ trợ / bằng chứng hiểu |
| --- | --- | --- |
| 1 | C# bridge từ Java: properties/events/delegates, value/reference semantics, collections, enums, null; Unity Object lifetime/serialization/Awake/OnEnable/Update/FixedUpdate/OnDisable | F-001: giải thích prefab vs instance, serialized reference và C# subset của Editor đã chọn; không copy newer .NET syntax chưa hỗ trợ |
| 2, sớm cùng nền | Client/server/host, authority vs ownership vs transport vs session; NGO/Mirror official concepts; local input/camera binding, participant vs connection identity | TECH-001/F-003: giải thích vì sao HP server-only vẫn sai nếu tin pose/hit client; hai instance rồi bốn và khác mạng, không gọi spike là MVP |
| 3 | Vector3/XZ frames, input, gravity/grounding/jump, CharacterController vs Rigidbody tradeoffs, terrain/wall collision, camera update/fixed heading | F-002/F-004/F-007: facing theo movement và local camera; controlled boundary jump sống, ring-out khác floor contact; controller chưa duyệt |
| 4 | Network ticks/commands/snapshots, sequence/dedup, validation, interpolation; đo input response/correction trước prediction | F-005–007/F-014: room/load/start/spawn/input path có state gates; không học cả hai networking stack như implementation requirement |
| 5 | Forward melee geometry/occlusion/cooldown, collider dedup, HP clamp, knockback displacement và batch elimination | F-008–009: giải thích request/accepted attack/valid hit/effect khác nhau; zero contenders draw, không callback winner sớm |
| 6 | Trigger/contact vs authority, weapon ownership transaction, E drop, 2x measured effects | F-010: hai contacts một winner, no stack/no HP reset; 2x force không đảm bảo 2x distance |
| 7, nghiên cứu sớm từ bước 2 | Disconnect identity/grace/generation, vulnerable avatar, current snapshot restore; protocol compatibility | F-011–014: reconnect khác late join; no duplicate/reset/revive; host loss Aborted, eliminated spectator |
| 8, xuyên suốt | Edit/Play Mode/rules tests, multi-process/WAN, impairment simulator, profiling/resource samples, CI artifacts và license | F-015–019: phân biệt Planned/Executed/PASS, actual RTT vs one-way delay, protocol/cheat/load/CI evidence và human review |

<a id="72-future-multiplayer-research--after-offline-acceptance"></a>

## Research checkpoints cần ghi quyết định

Anchor “after offline acceptance” chỉ giữ tương thích liên kết lịch sử; networking hiện được nghiên cứu sớm, không cần offline acceptance trước.

- TECH-001: exact supported Editor/packages/modules, một framework, service/license/cost route; [official assessment](../docs/ARCHITECTURE.md) là điểm bắt đầu, không approval.
- F-003: kiểm connectivity khác mạng, local owner/camera, host trust; thí nghiệm hai rồi bốn instances trước khi thêm nhiều combat/content.
- O-01/O-09: controller/axes/jump/camera, speed/range/cooldown/timestep và quality budget. Ví dụ tutorial chỉ là bài học, không tự trở thành tuning baseline.
- F-007/F-015: raw correction/response dưới delay/jitter/loss; chỉ thêm prediction/reconciliation khi experiment chứng minh cần.
- O-02–08: pickup contention/drop/re-pickup/weapon out-of-arena, elimination ordering, reconnect expiry/old connection, spectator/repeat-session proposal; ghi owner decision trong SRS/DECISIONS, không nhân bản requirements.
- F-018: học Unity license activation/runner modules/artifact provenance trước khi viết CI. Không chia sẻ key hoặc giả pricing/quota.

<a id="9-learning-references"></a>

## Tài nguyên học

Nguồn cơ bản bên dưới kế thừa catalogue học v0.2, chưa reverify nội dung từng lesson trong task này. Không coi tutorial là compatibility proof cho selected patch; framework/version-sensitive sources đã kiểm nằm trong ARCHITECTURE. Chọn lesson phù hợp checkpoint, không buộc hoàn thành cả course.

| Ref | Tài nguyên | Cách dùng |
| --- | --- | --- |
| R12 | [Unity Essentials](https://learn.unity.com/pathway/unity-essentials) | Editor/scenes/components/prefabs/serialized references |
| R13 | [Junior Programmer](https://learn.unity.com/pathway/junior-programmer) | OOP/debugging/events; thực hành nhỏ có giải thích |
| R14 | [Microsoft C# tour](https://learn.microsoft.com/en-us/dotnet/csharp/tour-of-csharp/) | Cầu nối Java/C#; cross-check compiler subset theo Editor |
| R15 | [Unity Roll-a-Ball](https://learn.unity.com/course/roll-a-ball/unity-version?version=6.0) | Học collision/trigger/UI; không áp Rigidbody ball motor làm player architecture mặc định |
| R06 | [CharacterController.Move](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/CharacterController.Move.html) | Nguồn tham khảo gravity/collision, không đã chọn controller |
| R07 | [OverlapSphere](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Physics.OverlapSphere.html) | Query semantics, dedup target và wall validation vẫn phải tự thiết kế |
| R08 | [Cinemachine 3.1](https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1/manual/index.html) | Camera concept, package chưa pin/duyệt |
| R16 | [CharacterControllerSamples](https://github.com/Unity-Technologies/CharacterControllerSamples) | Nâng cao, package/ECS khác; không copy foundation |
| R22 | [Official Cinemachine introductory video](https://www.youtube.com/watch?v=x6Q5sKXjZOM) | Video cũ 2017 chỉ học concept, Inspector/API lịch sử |
| R23 | [Official Cinemachine basics video](https://www.youtube.com/watch?v=8aYvX7JyYGI) | Concept, không dùng làm compatibility evidence |

## Ghi nhận học và AI assistance

Mỗi checkpoint ghi lời giải thích của sinh viên, bài tập/commit nếu có, nguồn, điều chưa hiểu và reviewer thực. AI output cần được đọc/kiểm tra; không ghi hoàn thành course/test hoặc independent review nếu chưa xảy ra. Learning progress không thay product acceptance TC trong [TESTING](../docs/TESTING.md).

## Lịch sử tài liệu

| Version | Ngày | Thay đổi |
| --- | --- | --- |
| 0.2 | 2026-10-24 | Tách learning từ SRS; lịch offline 7/14/30 ngày, networking sau offline |
| 0.3 | 2026-11-01 | Thay thứ tự cũ bằng networking/ownership/reconnect nghiên cứu sớm; bỏ assumed hours và offline delivery schedule; giữ learning references/IDs. Nội dung v0.2 vẫn có trong Git tại 1d7a6c9 |
