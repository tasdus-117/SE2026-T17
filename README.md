# Small Arena — Unity/C# online FFA

Game arena combat 3D cho Windows PC, bốn người FFA (1v1v1v1), một participant host authoritative server cùng local client. Một arena có terrain/walls, movement/jump, melee, HP/ring-out elimination, spectator, private code rooms, reconnect và một melee weapon auto-pickup/E-to-drop ngay trong first online MVP. 2v2 và additional weapon types là FUTURE, ngoài baseline khoảng 60 ngày từ 2026-11-01.

## Trạng thái đã kiểm chứng — 2026-11-01

- Owner `23001901`; repository `tasdus-117/SE2026-T17`. Unity/C# là CONFIRMED; Editor/packages/framework/services chưa pin/duyệt/cài trong checkout này.
- `long23001901` đã được chủ dự án duyệt, tạo và push từ setup commit `1d7a6c91fb7c2a3ef2fa233c1d5efe5cc60b462e`. Nền gồm tài liệu có sẵn, không Unity project/game source/tests/CI/build configuration.
- Tài liệu online mới thuộc `docs/23001901/online-ffa-roadmap`, **awaiting integration into long23001901**; push không đồng nghĩa merged. Xem [state](docs/PROJECT_STATE.md) cho delivery evidence mới nhất.
- Assignment AD-065 và prototype branches không phải current implementation; không import code trong task này. Các historical test reports không là gameplay/WAN evidence.

## Bản đồ tài liệu

| Đường dẫn | Trách nhiệm |
| --- | --- |
| [SRS](Long_document/srs.md) | Current requirements/rules/FR-NFR-UC-AC traceability; full historical offline v0.2 và bảng supersession |
| [Roadmap và feature catalogue](docs/ROADMAP.md) | A foundation/spike → B first complete online MVP → C verified FFA; D future 2v2/weapons; E optional; 19 bounded current features |
| [Architecture](docs/ARCHITECTURE.md) | Authority/ownership/topology, NGO vs Mirror, recommendation PROPOSED, official sources/date/cost/license limits |
| [Protocol plan](docs/PROTOCOL.md) | Logical messages, validation/order/identity/reconnect/version; chưa implementation |
| [Testing/evidence](docs/TESTING.md) | Planned TC-101–120, latency/load/cheat/CI/demo và reproducible 2x displacement measurement |
| [Learning plan](Long_document/learning-roadmap.md) | Học Unity/C# và networking sớm, không product delivery schedule |
| [Instructions](AGENTS.md) | Quy tắc Git/session/sources |
| [State](docs/PROJECT_STATE.md), [Tasks](docs/TASKS.md), [Decisions](docs/DECISIONS.md), [Work log](docs/WORK_LOG.md) | Tiến độ thật, approvals/proposals, verification và handoff |

## Setup, build và test entry points

**UNKNOWN / chưa có lệnh online được kiểm chứng.** Chưa có `Assets`, `Packages`, `ProjectSettings`, executable tests hoặc CI workflow cho game trên baseline được chọn. Không chạy lệnh assignment như thể build game. Next bounded task là TECH-001 review/approve exact matrix và service/license route; sau approved documentation integration và feature branch approval, F-001 tạo reproducible foundation và ghi setup/build commands từ kết quả thật tại đây. [TESTING](docs/TESTING.md) mô tả planned gates, không passing builds.

Không có gameplay/build/load/latency/cheat execution trong documentation task. Giữ rõ CONFIRMED/PROPOSED/UNDECIDED, CURRENT/FUTURE, TODO/DONE và committed/pushed/integrated.
