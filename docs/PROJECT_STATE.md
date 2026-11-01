# Project State — handoff nhánh tài liệu

Last updated: 2026-11-01 (Asia/Bangkok). Project: SE2026-T17 / Small Arena, owner 23001901.

## Branch/base/evidence

- Current branch: `docs/23001901/online-ffa-roadmap`, tạo với owner approval từ `long23001901` tại `1d7a6c91fb7c2a3ef2fa233c1d5efe5cc60b462e`.
- Integration baseline: owner đã duyệt commit setup này cho Unity/C# mới; `long23001901` đã tạo/push với upstream `origin/long23001901`, live remote tip kiểm bằng ls-remote đúng SHA. Đây là nền tài liệu, không runnable game.
- Verified online content commit: `178ee30ecaaecccb44743e18d8189ef08ca8a06f` — `docs(roadmap): DOC-001 define online FFA documentation baseline`. `git push -u origin docs/23001901/online-ffa-roadmap` đã thành công, upstream established; content checkpoint local/upstream cùng SHA. Handoff record này được ghi sau content push; commit chứa record tra bằng `git log` và trạng thái push hiện tại phải verify ở RESUME. PR NONE created. **Awaiting integration into long23001901**; không merge/cherry-pick/PR trong task.
- Setup memory trước đây ghi uncommitted/not pushed/Unity proposed/integration absent đã stale: live Git chứng minh setup docs/reference files đã tracked/pushed, owner xác nhận Unity/C# online và đã phê duyệt foundation. Các historical WORK_LOG/DECISIONS không bị viết lại.

## Current goal và kết quả

- BASE-001 DONE: approved integration creation/push, không import assignment/Godot/Go. Docs kế thừa memory theo ancestry, không cần chuyển commit.
- DOC-001 DONE cho documentation: Vietnamese SRS v0.3 giữ full v0.2 với retired IDs, 19-feature roadmap A/B/C + D future/E optional, architecture NGO/Mirror official comparison, protocol và TC-101–120 Planned; README/learning/instructions/memory đã reconcile, kiểm và commit/push trong nhánh này. Không đồng nghĩa gameplay DONE hoặc integrated.
- Product CONFIRMED: Windows Unity/C#, host+3 remote FFA, private code rooms, one arena, movement/jump/facing/fixed-heading camera, melee HP+knockback, weapon auto pickup/E drop/2x, HP/ring-out unified elimination, spectator/results, reconnect/forfeit/host abort và protocol/load/latency/cheat/CI evidence bắt buộc trong first MVP. 2v2/additional weapons FUTURE.
- Chưa có Unity Assets/Packages/ProjectSettings, game implementation, online test harness, CI workflow hoặc verified build command trên baseline này. Historical assignment Node/SQLite evidence không chứng minh gameplay/3D/WAN/reconnect.

## Kiểm chứng và giới hạn

Git status/branch/ops/refs/history/ancestry, repository remote identity, clean assignment worktree và prompt hash đã kiểm. Markdown checks PASS: 52 local links/11 anchors; 14 FR/6 NFR/5 UC, 20 AC/20 TC unique và traceability; 19-feature dependency DAG; historical SRS exact normalized preservation; WORK_LOG append-only; UTF-8/fences/common secret patterns và semantic MVP review. `git diff --check` không whitespace error. Chi tiết/fixed check failures trong WORK_LOG. Gameplay/build/load/latency/cheat tests NOT RUN; mọi online TC vẫn Planned. Official tech/licensing/pricing sources đã đọc ngày 2026-11-01, không installed inventory hoặc approval.

## Uncommitted work và preservation

12 task-related Markdown paths đã commit ở 178ee30: README.md, AGENTS.md, Long_document/{srs,learning-roadmap}.md, docs/{ROADMAP,ARCHITECTURE,PROTOCOL,TESTING,PROJECT_STATE,TASKS,DECISIONS,WORK_LOG}.md. Explicit staged-path/content review và staged whitespace check PASS; no source/config/build output/secret-pattern matches. Checkpoint sau content push: tracked tree/index clean, docs branch merge-base 1d7a6c9 và ahead integration một commit; record hiện tại chỉ cập nhật PROJECT_STATE/TASKS/WORK_LOG cho actual delivery. Preexisting `docs/prompts/create_roadmap_prompt.md` giữ nguyên/untracked, SHA256 `13A8FA34DAA474C0F8CFCB058AA9649721BFA08C9281E9005B46472C1EDF3DC8`. `.ad065-worktree/` và assignment ref tại f4835ce untouched; main b5054a6 và setup 1d7a6c9 không đổi. Không stage prompt hoặc generated/source/config files.

## Blockers và exact next action

TECH-001 TODO: owner/team review recommendation NGO + Unity Transport + MPS/Relay và candidate Unity 6.3 LTS, chọn đúng một framework, pin compatible exact versions, license/runner/modules/service budget/identity plan; O-01–09 tuning/drop/reconnect/ordering/spectator details còn PROPOSED/UNDECIDED. Không còn blocker cho documentation độc lập.

Exact next action: RESUME verify live branch/status/ops/refs và ahead/behind với long23001901; owner review DOC-001 và TECH-001 matrix/license/budget/identity choices. Integration vào long23001901 cần authorization riêng (chưa có). Sau successful authorized integration mới reconcile integration memory, xin feature branch từ latest approved integration để làm GAME-001/F-001. Acceptance F-001 là FR-101/AC-101/TC-101: clean reproducible pins, client/headless artifacts/smoke và README commands từ thực tế. F-003 early two→four/different-network spike tiếp ngay, không làm offline game lớn trước networking. Không tiếp tục implementation trong task tài liệu.
