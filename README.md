# Small Arena — Unity/C# và kế hoạch phối hợp nhóm

Windows client, arena FFA tối đa 4 người. Theo [phân công đề xuất](docs/SE2026-T17-Huong-dan-va-phan-cong-nhom.md), hướng kế hoạch cá nhân TV2 (23001901) là Unity + Mirror, server chạy riêng, một súng raycast, HP/chết/hồi sinh/điểm số. Owner cho phép cập nhật kế hoạch cá nhân; cả nhóm/giảng viên chưa xác nhận mọi đề xuất. Unity/C# đã xác nhận, exact stack và gameplay parameters chưa duyệt.

## Trạng thái đã kiểm — 2026-11-02

- Nhánh docs/23001901/online-ffa-roadmap tại 9d29f97, cùng cached upstream; base long23001901 = 1d7a6c9, ahead 2 / behind 0. Tài liệu chưa integrated; phiên này không commit/push/PR/merge.
- Checkout chỉ có tài liệu/assignment artifacts; không Assets, Packages, ProjectSettings hay game source/test harness/CI. Đường dẫn Unity trong phân công là đề xuất. Claim về main/PR #2 của phân công không chứng minh checkout này; không thay nguồn bằng main.
- TV1 sở hữu project/network/simulation/session/build-CI; TV2 rules/domain/combat/tests; TV3 input/camera/UI/arena/prefab. TV2 góp M1 ngay bằng speed/eligibility/input limits; chủ trì combat M3.
- Baseline host+3/melee/ring-out/spectator/pickup-E-drop trước đây được giữ lịch sử trong SRS và docs; không gộp với raycast/respawn thành mandatory scope mới. Xem bảng xung đột và GD-01–09 trong SRS.

## Bản đồ nguồn

| Tài liệu | Sở hữu |
| --- | --- |
| [SRS](Long_document/srs.md) | Yêu cầu đề xuất mới/ID mapping, scope conflicts và quyết định mở; full v0.3 online/v0.2 offline |
| [TASKS](docs/TASKS.md) | TV2-001–013, deliverable/acceptance/verification/actual status; task cũ giữ lịch sử |
| [ROADMAP](docs/ROADMAP.md) | M0–M6, trình tự/phối hợp cả nhóm |
| [ARCHITECTURE](docs/ARCHITECTURE.md), [PROTOCOL](docs/PROTOCOL.md) | Boundaries, proposed paths và logical contracts; chưa Mirror API |
| [TESTING](docs/TESTING.md) | TC-201–209 Planned, rule/integration/cheat/evidence; không passing gameplay tests |
| [Learning](Long_document/learning-roadmap.md) | Học cho TV2 từ Java/OOP; không là product deliverables |
| [AGENTS](AGENTS.md) | Git/session safeguards |
| [State](docs/PROJECT_STATE.md), [Decisions](docs/DECISIONS.md), [Work log](docs/WORK_LOG.md) | Handoff, approvals/unknowns, kết quả thực |

## Setup/build/test

UNKNOWN: chưa có command online được kiểm chứng. TV1 khởi tạo sau TECH-001 và các approval cần thiết. TV2 bắt đầu ngay bằng scope/GD agenda, data/state/config schema, movement limits, CT examples và symbolic test vectors. Không chạy assignment để nhận là gameplay test; không import AD-065 hoặc tạo Unity project trong task tài liệu.
