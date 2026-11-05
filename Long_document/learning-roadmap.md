# Kế hoạch học cho TV2 — từ Java/OOP đến gameplay Unity/C#

2026-11-02. Kết quả học là PROPOSED/chưa thực hiện; khác deliverable sản phẩm ở [TASKS](../docs/TASKS.md). Dùng [phân công](../docs/SE2026-T17-Huong-dan-va-phan-cong-nhom.md) làm hướng cá nhân; học vừa đủ cho M1 rồi M3, không học hết mạng/UI/CI thay TV1/TV3. Phần học melee/elimination/NGO cũ bên dưới chỉ là lịch sử.

| Thứ tự / mốc | Học từ nền Java/OOP | Kết quả học quan sát được | Hỗ trợ sản phẩm, không thay acceptance |
| --- | --- | --- | --- |
| 1 / M0, làm ngay | Java class/interface/enum/collection → C# properties, struct/value vs reference, events/delegates, null và generics; compiler subset kiểm theo Editor được chọn | Giải thích data/config immutable hoặc mutable, copy struct và event subscription; pseudocode state machine có review | TV2-002 data/state/config; không cần tạo Unity project để đọc/thiết kế |
| 2 / M0–M1 | Unity component, scene/prefab, serialization; Awake/OnEnable/Start/Update/FixedUpdate/OnDisable và object lifetime | Giải thích nơi rule thuần tách MonoBehaviour, vì sao không dùng client frame time làm cooldown server; bài tập nhỏ chỉ sau project approval | TV2-002/003, TV1 motor/TV3 prefab |
| 3 / M1 | Vector/units, finite values, input magnitude, speed và eligibility; interface thay network singleton | Viết bảng zero/diagonal/invalid/state-gated inputs và expected; giải thích server mô phỏng khác client thu input | TV2-003/004 + TC-202 |
| 4 / M1, sớm | Server authority vs ownership, connection vs participant; Mirror concepts sau chọn exact version | Vẽ luồng intent → ownership TV1 → rule TV2 → sync TV1 → HUD TV3; không client damage/pose truth | TV2-004 contracts; không implement networking như bài tập bắt buộc |
| 5 / M3 | Physics.Raycast, origin/direction/range, collider/layer/trigger, obstruction, aim/camera frame | Vẽ hit/miss/wall và invalid-origin cases; sau M0 thử fixture để hiểu physics thật; API chỉ tra theo version đã pin | TV2-005/006; không dùng melee OverlapSphere làm default |
| 6 / M3 | Server clock cooldown, HP clamp, explicit life states, exactly-once death/credit, config validation | Viết trace two lethal hits + duplicate callback + new life; chứng minh không HP âm/score trùng | TV2-007/008/009; tuning vẫn GD-01–08 |
| 7 / M1–M5 | Arrange/Act/Assert, fake clock/config, boundary/table tests; Edit Mode vs physics/integration tests | Viết expected vectors ngay; sau M0 chạy suite thật, phân biệt rule pass với two-client physics pass | TV2-010/011/012; Planned không là PASS |
| 8 / M5–M6 | Đọc logs, actual RTT vs one-way delay, artifact/config provenance và support CI | Đọc một run và chỉ ra thiếu evidence, giữ defect/retest; học cách chạy suite do TV1 đưa, không nhận pipeline | TV2-013 evidence |

Những gì làm ngay không cần Unity: C# bridge đọc/giải thích, state/config schema, test vectors, contract examples và danh sách GD cần hỏi nhóm. Execution Unity/physics/multi-client chỉ sau approved project/branch. Không có course/test/human review nào được ghi hoàn thành trong task tài liệu này. Tài nguyên học cũ giữ dưới đây để tra cứu; URL/API theo phiên bản cần kiểm lại trước dùng, không là compatibility approval.

---

## LỊCH SỬ — Baseline ngày 2026-11-01, không dùng làm cổng nghiệm thu kế hoạch TV2

> Toàn bộ nội dung bên dưới được giữ để truy vết. Các nhãn CURRENT, CONFIRMED, mandatory, TODO và next action bên trong chỉ có nghĩa tại thời điểm baseline cũ. Không cộng các nghĩa vụ cũ vào kế hoạch TV2 mới.

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
