# Small Arena — Unity/C# và kế hoạch phối hợp nhóm

Windows client, arena FFA tối đa 4 người. Theo [phân công đề xuất](docs/SE2026-T17-Huong-dan-va-phan-cong-nhom.md), hướng kế hoạch cá nhân TV2 (23001901) là Unity + Mirror, server chạy riêng, một súng raycast, HP/chết/hồi sinh/điểm số. Owner cho phép cập nhật kế hoạch cá nhân; cả nhóm/giảng viên chưa xác nhận mọi đề xuất. Unity/C# đã xác nhận, exact stack và gameplay parameters chưa duyệt.

## Trạng thái đã kiểm — 2026-11-05

- Personal integration long23001901 tại 1d7a6c9 vẫn là nền Git/tài liệu. Main remote tại 11849ad có Unity/offline combat; TV1 tại 4a7568f có authoritative movement, chưa có multi-process UDP/KCP/latency evidence do TV2 chạy. [Inventory theo SHA](docs/ARCHITECTURE.md#tv2-001--inventory-theo-ref) và [phân loại test evidence](docs/TESTING.md#tv2-001--phân-loại-bằng-chứng) ghi giới hạn cụ thể.
- TV2-001 được làm riêng trên feature/TV2-001-scope-reconciliation-and-decisions từ origin/long23001901; prerequisite docs từ 5338ba4 được chuyển chọn lọc, chưa integration. Task BLOCKED chờ human decisions/review; nhóm chưa duyệt GD-01–09.
- [SRS matrix](Long_document/srs.md#tv2-001--đối-chiếu-nguồn-ngày-2026-11-05) giữ scope conflicts; [DECISIONS agenda](docs/DECISIONS.md#tv2-001--agenda-quyết-định-ngày-2026-11-05) giữ alternatives/recommendations/approval fields; [TASKS](docs/TASKS.md) giữ dependency/gates cho đủ 13 TV2 tasks.
- TV1 sở hữu mạng/simulation/session/build-CI, TV2 rules/combat/tests, TV3 input/camera/UI/arena. Identity mapping TV3 chưa xác nhận; không gán từ PR author. Không gộp melee/elimination/pickup lịch sử vào raycast/respawn.
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

Command online chưa được TV2 chạy/kiểm chứng. Reuse project/commands trên refs TV1 chỉ sau review, approval và integration phù hợp; không tạo project thứ hai. TV2 bắt đầu ngay bằng scope/GD agenda, data/state/config schema, movement limits, CT examples và symbolic test vectors. Không chạy assignment để nhận là gameplay test; không import AD-065 hoặc tạo Unity project trong task tài liệu.
