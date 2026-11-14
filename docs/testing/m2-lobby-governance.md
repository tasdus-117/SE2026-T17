# M2 — quản trị phòng, slot và F11

## Quy tắc và ranh giới

- `LobbyRoom` ở Server/Sessions cấp slot 0–3 nhỏ nhất còn trống. Disconnect trả
  đúng slot; thành viên còn lại giữ vị trí và người thay thế luôn Not Ready.
- Server chỉ chấp nhận Start từ host khi phòng Waiting, có ít nhất hai người
  và tất cả Ready. Snapshot gửi `CanStart` và các slot; view không tự quyết định
  luật. Remote gửi Start hoặc host gọi khi thiếu điều kiện đều không đổi pha.
- `ArenaNetworkManager.maxConnections` vẫn là 4. Mirror cho một connection từ
  chối tạm thời đi qua transport để nhận `Room is full (4/4)`; connection này
  không được vào roster, không authenticated và không được spawn avatar.
  Client xác nhận đã nhận rejection rồi server disconnect; timeout năm giây
  giải phóng peer không phản hồi. Không sửa mã Mirror hoặc tăng sức chứa phòng.
- Client giữ lý do qua disconnect, hiện modal `Cannot join: Room is full (4/4)`
  và trở lại connection panel. OK đóng modal; thử Host/Join mới xóa lỗi cũ.
- View/controller nằm Client.UI; message/state bridge nằm Networking; luật
  roster/Ready/Start nằm Server. Giữ bảy asmdef một chiều và offline gameplay.
- `ScreenDisplayController` được cài trước scene đầu, tồn tại qua scene bằng
  DontDestroyOnLoad. F11 chỉ đổi yêu cầu fullscreen, không sửa cursor, input
  gameplay, Time.timeScale hoặc trạng thái phòng. Giữ phím không toggle lặp.

Khi dựng lại UI, chép các builder trong tools vào Assets/Editor của bản sao
kiểm chứng, gồm `Build-LobbyGovernanceUI.cs`. Builder lobby cũ gọi bước nâng cấp
slot/modal mới sau khi lưu; có thể chạy riêng
`SmallArena.Tools.LobbyGovernanceSceneBuilder.Build` để nâng cấp bổ sung.
Không dùng bản client cũ với schema LobbySnapshot/LobbyMember mới.

## Kiểm tra tương tác

1. Mở hai bản MainMenu, chọn Multiplayer; bản A Host, bản B Join địa chỉ A.
   Cả hai thấy hai slot có người, badge Host và Not Ready; hai slot trống
   màu xám Waiting... . Tên/ID dùng ID connection thật, không phải số slot.
2. Host một mình bật Ready: Start vẫn khóa. Hai người cùng Ready: chỉ host
   có Start được bật. B đổi về Not Ready: Start khóa ngay khi snapshot tới.
3. Cho B rời phòng; slot B trở thành trống mà slot khác không dịch chuyển.
   B hoặc người mới join lại: dùng slot trống và phải Ready lại.
4. Cho đủ bốn người; bản thứ năm Join: thấy modal chính xác
   `Cannot join: Room is full (4/4)`, không chuyển arena hoặc spawn avatar.
   Bốn người cũ vẫn đổi Ready và hoạt động bình thường. Đóng modal, giải phóng
   một slot rồi thử Join lại thành công.
5. Thử F11 ở MainMenu, Bootstrap và Arena_MVP: window/fullscreen luân phiên,
   giữ phím không lặp; chuột vẫn đúng trạng thái menu hoặc gameplay trước đó.
   Kiểm tra hình ảnh fullscreen cần cửa sổ tương tác, batchmode không chứng minh
   màn hình vật lý thay đổi chế độ.
6. Hai người Ready và host Start; xác nhận movement/combat/loot E, HP 10/10,
   damage 1→2, Escape/Leave và Single Player vẫn hoạt động.

## Bằng chứng tự động

Unity 6000.6.8f1, native runner trong bản sao có cache, không dừng Editor của người dùng.

| Kiểm chứng | Kết quả |
| --- | --- |
| PlayMode | 271 ca: **269 đạt, 0 lỗi, 2 skip package có sẵn** |
| LobbyGovernanceAndScreenTests | **7/7 đạt** |
| EditMode | **34/34 đạt**, 0 lỗi/bỏ qua |
| Assembly nhóm | **7/7**, 0 cảnh báo/lỗi với Roslyn strict; native Mirror weaving đạt |
| Layout/GUID | **4.289 assertion/18 YAML**, **1.476 cặp metadata**, graph không vòng |

Đủ baseline 264 PlayMode trước nhiệm vụ, gồm 13 loot/collision, 13 combat/HUD
và bảy TV1.3. Hai skip là package Input System như baseline; cảnh báo analyzer
vendor/Mirror vẫn còn, không khẳng định toàn bộ Unity/vendor zero-warning.
Bằng chứng: TestResults/LobbyGovernance/PlayMode-final.xml, EditMode.xml,
summary.json, tested-source-hashes.json; Logs/LobbyGovernance-PlayMode-final.log,
Logs/LobbyGovernance-EditMode.log. Lần focused đầu 6/7 phát hiện test F11 xử lý
input sai thời điểm; giữ kết quả chẩn đoán riêng, không cộng vào tổng ca đạt.
Màn hình/cursor vật lý và KCP nhiều cửa sổ cần làm checklist thủ công ở trên.
Harness dùng một NetworkClient native và connection ảo trong engine; snapshot
cho peer ảo được deserialize và đưa vào view riêng. Đây không phải hai engine
hoặc kiểm chứng KCP nhiều tiến trình.
