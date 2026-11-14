"""Build the reviewed Vietnamese submission and render every page for QA.

Prerequisites: python -m pip install --target TestResults/BT3/pdf-deps reportlab
The reflection, its provenance, and consent are read from the assignment records.
"""
from pathlib import Path
import argparse
import csv
import html
import json
import re
import sys
from urllib.parse import urljoin

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / 'TestResults/BT3/pdf-deps'))
from reportlab.platypus import SimpleDocTemplate, Paragraph, Spacer, PageBreak, LongTable, TableStyle, Flowable
from reportlab.lib import colors
from reportlab.lib.enums import TA_LEFT
from reportlab.lib.pagesizes import A4
from reportlab.lib.styles import ParagraphStyle
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
import fitz

DOC = ROOT / 'docs/assignments/bt3-vt-066'
W = A4[0] - 84
LINK_BASE = 'https://github.com/tasdus-117/SE2026-T17/blob/codex/bt3-vt-066/docs/assignments/bt3-vt-066/'

def js(name):
    return json.loads((DOC / name).read_text(encoding='utf-8-sig'))

def fmt(text):
    text = html.escape(str(text), quote=False)
    text = re.sub(r'\[([^\]]+)\]\(([^)]+)\)', lambda m: '<link href="' + (m[2] if m[2].startswith(('https://','http://','#')) else urljoin(LINK_BASE,m[2])) + '" color="#165A99">'+m[1]+'</link>', text)
    text = re.sub(r'\*\*([^*]+)\*\*', r'<b>\1</b>', text)
    text = re.sub(r'`([^`]+)`', r'\1', text)
    return text.replace('\n', '<br/>')

pdfmetrics.registerFont(TTFont('Arial', 'C:/Windows/Fonts/arial.ttf'))
pdfmetrics.registerFont(TTFont('Arial-Bold', 'C:/Windows/Fonts/arialbd.ttf'))
pdfmetrics.registerFont(TTFont('Arial-Italic', 'C:/Windows/Fonts/ariali.ttf'))
pdfmetrics.registerFontFamily('Arial', normal='Arial', bold='Arial-Bold', italic='Arial-Italic', boldItalic='Arial-Bold')
ST = {
    'body': ParagraphStyle('body', fontName='Arial', fontSize=10.2, leading=13.2, spaceAfter=5.4, textColor=colors.HexColor('#162537'), splitLongWords=True),
    'small': ParagraphStyle('small', fontName='Arial', fontSize=8.8, leading=11.3, spaceAfter=4, splitLongWords=True),
    'title': ParagraphStyle('title', fontName='Arial-Bold', fontSize=19, leading=23, spaceAfter=10, textColor=colors.HexColor('#153D61')),
    'h1': ParagraphStyle('h1', fontName='Arial-Bold', fontSize=13.3, leading=16.6, spaceBefore=4, spaceAfter=7, textColor=colors.HexColor('#153D61')),
    'h2': ParagraphStyle('h2', fontName='Arial-Bold', fontSize=10.3, leading=13, spaceBefore=7, spaceAfter=4),
    'cell': ParagraphStyle('cell', fontName='Arial', fontSize=8.7, leading=11.1, spaceAfter=0, splitLongWords=True),
    'header': ParagraphStyle('header', fontName='Arial-Bold', fontSize=8.7, leading=11.1, textColor=colors.white, splitLongWords=True),
}

def P(text, style='body'):
    return Paragraph(fmt(text), ST[style])

def T(rows, widths=None):
    cells = [[P(c, 'header' if i == 0 else 'cell') for c in row] for i, row in enumerate(rows)]
    table = LongTable(cells, colWidths=widths or [W / len(rows[0])] * len(rows[0]), repeatRows=1, hAlign='LEFT')
    table.setStyle(TableStyle([
        ('BACKGROUND', (0, 0), (-1, 0), colors.HexColor('#153D61')),
        ('ROWBACKGROUNDS', (0, 1), (-1, -1), [colors.HexColor('#F0F4F7'), colors.white]),
        ('VALIGN', (0, 0), (-1, -1), 'TOP'),
        ('LEFTPADDING', (0, 0), (-1, -1), 5), ('RIGHTPADDING', (0, 0), (-1, -1), 5),
        ('TOPPADDING', (0, 0), (-1, -1), 4), ('BOTTOMPADDING', (0, 0), (-1, -1), 4),
        ('LINEBELOW', (0, 0), (-1, 0), 0.5, colors.HexColor('#153D61')),
        ('LINEBELOW', (0, 1), (-1, -1), 0.2, colors.HexColor('#D4DDE5'))]))
    return table

def md_table(text, header):
    lines = text[text.index(header):].splitlines()
    rows = []
    for line in lines:
        if not line.startswith('|'): break
        row = [c.strip() for c in line.strip('|').split('|')]
        if all(re.fullmatch(r':?-+:?', c.replace(' ', '')) for c in row): continue
        rows.append(row)
    return rows

def append_markdown(story, text):
    lines = text.splitlines(); i = 0; code = False
    while i < len(lines):
        line = lines[i]
        if line.startswith('```'):
            code = not code; i += 1; continue
        if not line.strip():
            story.append(Spacer(1, 3)); i += 1; continue
        if code:
            story.append(P(line, 'small')); i += 1; continue
        if line.startswith('|'):
            block = []
            while i < len(lines) and lines[i].startswith('|'):
                block.append(lines[i]); i += 1
            rows = md_table('\n'.join(block), block[0])
            columns = len(rows[0])
            if columns == 6: widths = [W*x for x in [.055, .12, .105, .27, .39, .06]]
            elif columns == 5: widths = [W*x for x in [.10,.20,.24,.24,.22]]
            elif columns == 4: widths = [W*x for x in [.10,.25,.27,.38]]
            elif columns == 2: widths = [W*.23,W*.77]
            else: widths = None
            if all(len(row) == columns for row in rows): story.append(T(rows, widths))
            else:
                for row in block: story.append(P(row, 'small'))
            continue
        if line.startswith('#'):
            story.append(P(line.lstrip('# ').strip(), 'h1' if line.startswith('# ') else 'h2')); i += 1; continue
        parts = [line.lstrip('> ')]
        while i + 1 < len(lines) and lines[i+1].strip() and not lines[i+1].startswith(('#','|','```','- ','> ')):
            i += 1; parts.append(lines[i])
        story.append(P(' '.join(parts), 'small')); i += 1

class MainEnd(Flowable):
    width = height = 0
    def draw(self): pass

class Document(SimpleDocTemplate):
    def afterFlowable(self, flowable):
        if isinstance(flowable, MainEnd): self.main_page_count = self.page

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--output', default=str(ROOT / 'output/pdf/BT3_23001947_VT-066.pdf'))
    args = parser.parse_args()
    approval = js('submission-approval.json')
    assert approval['commitmentConfirmed'] is True
    reflection = (DOC / 'reflection-user.txt').read_text(encoding='utf-8').strip()
    reflection_words = len(reflection.split())
    assert 250 <= reflection_words <= 350
    verification = js('github-verified.json') if (DOC / 'github-verified.json').exists() else None
    verified = verification is not None and verification.get('verified') is True
    branch = verification['url'] if verified else approval['plannedBranchUrl']
    report = js('evidence-summary.json')
    q1 = (DOC / 'question-1.md').read_text(encoding='utf-8')
    ac = md_table(q1, '| AC | Điều kiện đo được |')
    problem = q1.split('**Problem statement.**',1)[1].split('\n\n',1)[0].strip()
    invariant = q1.split('**Invariant retry.**',1)[1].split('**Non-functional constraints.**',1)[0].strip()
    nfc = q1.split('**Non-functional constraints.**',1)[1].split('## 2.',1)[0].strip()
    production = report['productionSha']; baseline = report['baselineSha']
    body = []
    def add(text, style='body'): body.append(P(text, style))
    def next_page(): body.append(PageBreak())

    # Main 1: identity and the unchanged accepted specification.
    add('BÀI TẬP CÁ NHÂN 3 · VT-066', 'title')
    add('Nguyễn Chí Hoàng Tú · MSSV 23001947 · Nhóm trưởng SE2026-T17\nGAME-3D-03 — Small Arena: multiplayer 3D authoritative server')
    add('[Repository chính thức](https://github.com/tasdus-117/SE2026-T17)\nBaseline: '+baseline, 'small')
    add('[Branch codex/bt3-vt-066]('+branch+')'+ (' — đã xác minh trên GitHub.' if verified else ' — dự kiến; hiện chỉ local, chưa push/chưa xác minh GitHub.'), 'small')
    add(approval['commitment'], 'small')
    add('Câu 1. Specification đã chốt', 'h1')
    add(problem)
    body.append(T(ac, [W*.18, W*.82]))
    add('Invariant và contract retry', 'h2')
    add(invariant, 'small')
    next_page()

    # Main 2: reviewed plan. Full accepted source is reproduced unchanged in the AI log appendix.
    add('Câu 1. Constraints, baseline và plan sau review', 'h1')
    for paragraph in nfc.splitlines():
        if paragraph.strip(): add(paragraph, 'small')
    add('Baseline và bảo toàn checkout', 'h2')
    add('HEAD, origin/main và main trên GitHub cùng '+baseline+'. Chọn baseline vì có đầy đủ lobby slice/regression và remote khớp. Tạo worktree sạch .worktrees/bt3-vt-066, nhánh codex/bt3-vt-066. Bốn Unity settings local của checkout chính giữ nguyên hash; không commit settings do import. Evidence: repository-state.json, remote-main.txt, preservation-final.json.', 'small')
    add('Call path và evidence baseline', 'h2')
    add('StartButton → LobbyRoomController.StartMatch → ArenaNetworkManager.RequestRoomStart → LobbyStartRequest → ServerLobbyController → LobbyRoom.TryStart → Loading → LoadRoomArena → Mirror ServerChangeScene → LoadSceneAsync → OnServerSceneChanged → Playing. UI hoàn tất theo OnClientSceneChanged của chính client.', 'small')
    add('Baseline refs: LobbyRoomController.cs:27,79; ArenaNetworkManager.cs:93–103,169–177,228–232; ServerLobbyController.cs:65–83; LobbyRoom.cs:14,35–41; Mirror NetworkManager.cs:828,861–889,1045,1076; MainMenu.unity:2974,4708,4825,4838. File đầy đủ có đánh số dòng ở evidence/question1/baseline-numbered-source.txt.', 'small')
    body.append(T([
        ['Review','Đề xuất trước / evidence','Quyết định sinh viên và plan sau'],
        ['R1 — sửa','Overlay qua scene nhưng mốc/vòng đời chưa chặt; Mirror :828, :861, :1045/:1076; UI thuộc MainMenu.','Một Start, bật khi client scene-start, còn sau RoomPanel destruction, dọn theo chính client ready/failure/disconnect; guest không theo host; dùng mốc, chưa phần trăm.'],
        ['R2 — sửa','Loading trước load tại ServerLobbyController:67–69; chưa tách guest leave và failure/UI sau teardown.','Boundary failure terminal ≤1, retry không tải lại; teardown an toàn, UI lỗi đọc được; guest leave chỉ local guest. Kiểm state + B/L/terminal.'],
        ['R3 — giữ','Request rỗng LobbyMessages:24; guard LobbyRoom:14,35–41.','Giữ room/session guard, không thêm operation ID; chứng minh phiên mới Start; snapshot resend được phép.']], [W*.14,W*.37,W*.49]))
    add('Plan, files và verification', 'h2')
    add('Spec → plan → generate → verify (Lecture 06 slides 9/14/17/21). File dự kiến: LobbyRoom, ServerLobbyController, ArenaNetworkManager, LobbyMessages nếu cần, overlay Client mới, LobbyRoomController/View, navigation nếu cần, PlayMode tests, pairwise/load tools và evidence. Giữ authority/host/readiness/capacity/spawn; không sửa vendor/scenes/packages.', 'small')
    add('Baseline → AI cases/user sửa ≥2 → feasible pairwise → behavioral RED → patch/diff user review → cùng filter GREEN → regression → mutation/coverage/flaky/load → traceability/reflection/PDF. Lecture 07 slides 7/16/18/21/23. Run-DomainTests.ps1:2 dùng 6000.6.7f1 và :32–40 chỉ compile 3 EditMode files, không discover lobby. Dùng Unity 6000.6.8f1 PlayMode; kiểm license/import/XML đúng suite.', 'small')
    add('Giới hạn: boundary startup exception/preflight; không hứa phát hiện native hang/crash sau startup. Mirror pause messages nên client có thể chỉ quan sát disconnect. Bản Câu 1 đã chốt, plan gốc/sau review, prompt hiện có và review nguyên văn được lưu toàn văn trong phụ lục AI Usage Log; prompt lịch sử tạo plan-v1 chưa có trong bản ghi.', 'small')
    next_page()

    rows = [
        ['T01','AC1/2','Unit + Int.','12 PW rows; room Waiting, healthy loader.','Accept iff host + 2/4 all Ready; rejected B=0. Fake load counts chỉ ở Unit.','P0'],
        ['T02','AC2','Int. Mirror','2 Start sát nhau, cùng connection/session.','Production B=1, native arena sceneLoaded L=1, S=1; thêm retry sau Playing.','P0'],
        ['T03','AC2','Unit op.','Retry khi Loading và sau terminal.','B=1, fake L=1, terminal=1; ghi riêng do Mirror pause dispatch.','P0'],
        ['T04','AC4','Unit/Int.','Complete lặp và scene không liên quan.','Chỉ Loading→Playing một lần; không mở spawn sớm.','P0'],
        ['T05','AC2/5','Int. boundary','Scene không tồn tại; retry, callback muộn.','B=1, native L=0, F=1; Failed rồi Offline; retry/late Complete rejected, gate false.','P0'],
        ['T06','AC1','Int. Mirror','Guest đã Ready gửi Start.','Negative: Waiting, B=L=S=F=0; host vẫn Start được.','P0'],
        ['T07','AC1','Unit/Int.','Solo Ready hoặc 2 người chưa all Ready.','Negative: Waiting, B=L=S=F=0; không sửa roster/readiness.','P0'],
        ['T08','AC3','Host E2E','Start thật; sceneLoaded checkpoint.','Overlay còn sau RoomPanel bị hủy/trước local-ready; dọn sau ready; native load một lần.','P1'],
        ['T09','AC3','Client local','Guest-only hook; server callback trước ready.','Overlay còn sau RoomPanel destruction/server complete; stop cleans. Không phải guest process E2E.','P1'],
        ['T10','AC5','Int. UI','Failure/disconnect và teardown manager.','Overlay dọn; text lỗi active/readable trên MainMenu phục hồi.','P1'],
        ['T11','AC5','Int. server','Host +2 virtual guest; 1 guest rời.','Server/guest khác tiếp tục, không Failed do leave; UI process guest còn thiếu evidence.','P0'],
        ['T12','AC2/5','Unit/Int.','Teardown A → B; startup failure tại menu.','Stale completion lúc Waiting ignored; manager được tái tạo, phiên mới đủ Ready Start được.','P0'],
        ['T13','Capacity','Int. callback','Authenticated total 3/4/5 gồm incoming.','3/4 admitted, 5 rejected; mutant >= fail tại 4. Không thay retry test.','P1'],
        ['T14','AC5','Int. bridge','Inactive server, unavailable/wrong scene.','Tín hiệu lỗi thật trước Mirror; valid wrong MainMenu cũng bị reject, không tạo load.','P0'],
        ['T15','AC4','Int. Mirror','AddPlayer trước Playing / sau Failed.','Gate chặn; Playing hợp lệ mới spawn, không nhân đôi. Xem scope từng assertion.','P0'],
        ['T16','AC3/NFC','Int. lặp','20 session chuyển scene liên tiếp.','Đúng một observer, không stale overlay/singleton, lifecycle window và cleanup pass.','P1'],
    ]
    head = ['ID','AC/risk','Level','Precondition/input','Expected result','Ưu tiên']
    widths = [W*x for x in [.055,.095,.115,.255,.42,.06]]
    add('Câu 2. Chiến lược test theo risk', 'h1')
    add('P0 ưu tiên retry gây trùng, terminal failure và quyền/spawn. Unit cho room/operation state; Integration/API qua Mirror LobbyStartRequest, LobbyReadyRequest, LobbySnapshot, callback và load boundary; host E2E dùng UI + scene thật. Guest local/virtual không được gọi là guest E2E qua scene.', 'small')
    add('Không cần REST/database/payment/email/queue tests vì slice không có dependency này; không dùng 100 người trong phòng 4 người. Unit lobby vẫn chạy trong Unity assembly; Domain runner không thay được PlayMode.', 'small')
    add('Oracle: B=boundary attempts, L=loader side effects mỗi process, S/F=terminal success/failure. Reject 0/0/0; healthy B=L=S=1; startup failure trước native load B=1,L=S=0,F=1. Snapshot resend không phải terminal event trùng.', 'small')
    body.append(T([head]+rows[:8], widths))
    add('Review AI cases: bản đầu 18 IDs. Sinh viên trực tiếp xóa T17 (trùng T03, bool-only) và T18 (fixed 2s + RoomPanel sai lifecycle). Còn 16 IDs, ≥2 negative T06/T07, failure T05 kiểm state và side effects. Before/after/nguyên văn ở phụ lục AI Usage Log.', 'small')
    next_page()
    add('Câu 2. Bảng sau review và pairwise', 'h1')
    body.append(T([head]+rows[8:], widths))
    add('Factors, constraints và chứng minh pairwise', 'h2')
    add('Requester H/G/U = host/guest/unknown ID tại rule boundary. Roster 1R/2P/2R/4R = solo Ready / two partial / two Ready / four Ready. Delivery 1/B/A = single / 2 sát nhau / lặp sau kết quả đầu (rejected hoặc terminal). Tất cả bắt đầu Waiting, healthy loader.', 'small')
    matrix = js('pairwise-ai-v1.json')
    mp = [['PW','Role','Roster','Delivery','Requests','B/L/S expected']]
    ro = {'solo_ready':'1R','two_partial':'2P','two_ready':'2R','four_ready':'4R'}
    for row in matrix['matrix']:
        k = int(row['expected_accepted'])
        mp.append([row['id'], {'host':'H','guest':'G','unknown':'U'}[row['requester']], ro[row['roster']], {'single':'1','burst':'B','after_result':'A'}[row['delivery']],row['requests'],f'{k}/{k}/{k}'])
    body.append(T(mp,[W*x for x in [.12,.12,.18,.18,.16,.24]]))
    add('Loại guest×solo_ready ở cả 3 delivery vì guest admitted phải tồn tại cạnh host. Unknown là input rule không thuộc roster; không dựng state network bất khả thi. 36 full combinations →33 feasible; 11 role×roster +9 role×delivery +12 roster×delivery =32 feasible pairs. Script Verify-Bt3Pairwise.py xác minh 12 rows phủ 32/32, lưu witness từng cặp; sau user xóa T17/T18 vẫn 32/32.', 'small')
    add('Matrix chỉ coverage dữ liệu, không phải test pass. T01 là 1 case tham số hóa 12 rows. Loading retry/failure/late callback/guest leave/biên4 được giữ bằng T02–T05,T09–T14, không loại vì matrix healthy chưa có.', 'small')
    next_page()

    add('Câu 3. Test-first implementation và review', 'h1')
    body.append(T([
        ['Run','Kết quả thực tế','Evidence'],
        ['Baseline','19/19 pass, exit 0; exact suites 12+7. License warning ban đầu rồi updated thành công.','baseline/combined.xml + log/manifest'],
        ['Compile lần đầu','CS0029 Action → UnityAction, không có XML. Không tính RED.','red/unity.log'],
        ['Behavior RED','20 discovered; 19 failed, 1 passed; exit 2. T08/T09 fail đúng overlay behavior.','red-assertions/results.xml'],
        ['GREEN cùng filter','20/20 pass, exit 0. Không lấy thiếu test/compile error làm pass.','green-attempt1/results.xml'],
        ['Recovery RED→GREEN','No manager sau MainMenu failure → reload menu → phiên mới Start được. Cùng test, 1 fail→1 pass.','recovery-red / recovery-green'],
        ['Final regression','57/57 pass, exit 0; gồm feature, lobby 2 suites và teardown; 20 cycles trong T16.','final-regression/results.xml']], [W*.19,W*.51,W*.30]))
    add('Assertion RED tiêu biểu', 'h2')
    add('T08: “Overlay must remain visible after RoomPanel destruction, before Mirror local-ready callback.” Expected True, actual False. Recovery: “Failure teardown in MainMenu must recreate a usable manager for a new session.” Expected True, actual False.', 'small')
    add('Patch và review', 'h2')
    add('LobbyStartOperation giữ cùng production rule cho coordinator/test/harness, chỉ được gọi trên server thread. LobbyRoom guard success/failure. Networking preflight active server, transition đang chạy, target Arena_MVP và scene có trong build. Client overlay quan sát lifecycle, sống trên root phiên và tạo canvas chỉ trong thao tác. Pending notice sống qua manager teardown; recovery reload MainMenu để tạo manager mới.', 'small')
    add('User duyệt v2 và recovery bổ sung; diff/hash được lưu. Giữ request Start rỗng, authority/host/readiness/capacity/spawn; không sửa vendor Mirror. Append Failed ở cuối enum, không thêm operation ID. Hai failure signals thật được test: scene không khả dụng và inactive/wrong-target guard; injected exception chỉ bổ sung test handling.', 'small')
    body.append(T([
        ['Mốc','Full commit SHA'],
        ['Test-first','267bbca668c50975556780662787ad7760c4d177'],
        ['Sửa compile, trước patch','c9a27babe977155c9c7b4e95c8d7c4dd2423325a'],
        ['Feature v2 được duyệt','c4719b4e998f83c1c6eb784ea5b5c239202573d9'],
        ['Production + recovery',production]], [W*.30,W*.70]))
    add('Lệnh: powershell -NoProfile -ExecutionPolicy Bypass -File tools/Run-Bt3Tests.ps1 -Label <mới> -Filter <suite/test>. Filter RED/GREEN: LobbyStartOperationTests;LobbyStartProgressTests (namespace SmallArena.Tests.PlayMode). Từng manifest lưu argv/SHA/exit/test IDs; source-hashes/source.diff ở các run cuối ghi cả patch chưa commit. Log runtime nằm trong evidence/<run>/.', 'small')
    next_page()

    add('Câu 4. Mutation, coverage, flaky và tải', 'h1')
    add('Mutation capacity: baseline ArenaNetworkManager.cs:203; sau patch dòng '+str(report['submissionGuard']['currentLine'])+'. Mirror OnServerAuthenticated đặt incoming.isAuthenticated=true trước callback, count gồm incoming. 3/4 admitted, 5 rejected. Mutation >→>= chỉ làm sai đúng biên4.', 'small')
    body.append(T([
        ['Bản / cùng T13 filter','Kết quả','Điều xác minh'],
        ['Sạch','3/3 pass, exit 0','Counts 3,4,5'],
        ['Mutant >=','2 pass, 1 fail, exit 2','T13(4): Expected admitted=1, actual=0'],
        ['Khôi phục >','3/3 pass, exit 0','targetRestored=true; nhánh nộp không chứa mutant']], [W*.35,W*.25,W*.40]))
    add('Mutant commit: '+report['mutation']['mutantSha']+'. Nhánh mutant tạm được giữ riêng để audit; không merge. Lệnh tools/Run-Bt3Mutation.ps1; raw diff/XML/assertion ở evidence/mutation-final/. Mutation kiểm capacity, T02/T05 mới chứng minh retry/failure.', 'small')
    add('Coverage và flaky', 'h2')
    add('Chưa thu line/branch percentage: project chưa cài com.unity.testtools.codecoverage. Risk coverage đối chiếu AC1–AC5, oracle B/L/terminal và khả năng fail thật; không suy quality từ số %. Timing: checkpoint sceneLoaded trước ready thay fixed sleep. Shared state: 20 consecutive real scene sessions, đúng 1 observer/no stale UI; 20/20 pass. Đây là 20 cycles trong một Editor run, chưa randomized order hay Internet.', 'small')
    add('100 tác vụ đồng thời — phạm vi harness', 'h2')
    lr = report['load']
    body.append(T([
        ['Thông số','Kết quả thực tế'],
        ['Môi trường','.NET 8.0.21; Windows 10.0.26200; 24 logical processors'],
        ['Workload','20 rounds ×100 independent room sessions; barrier + dispatch barrier, peak submitted in-flight=100 mỗi round'],
        ['Requests / outcomes','6000 requests; mỗi op 3 request cùng identity. 1800 success +200 expected startup failures; 2000/2000 completed'],
        ['Unexpected / duplicate','0 unexpected error, 0 timeout, 0 duplicate B/fake L/terminal'],
        ['Latency/p95','Submit → caller observes terminal; gồm dispatch wait. Nearest-rank mẫu95/100; per-round p95 26.0288–32.1951 ms; max 0.03220 s ≤3.0 s'],
        ['Production logic thật','LobbyRoom.cs + LobbyStartOperation.cs + LobbyMessages.cs; source hashes khớp production cuối'],
        ['Dependency giả','Mirror.NetworkMessage marker, fake load counter + delay12–24 ms; 10% failure trước side effect. Không Unity/network/native arena IO.']], [W*.28,W*.72]))
    add('Kết luận chỉ về core logic với dependency giả; chưa chứng minh p95 multiplayer/arena thật hoặc thread safety cho cùng một room. Lệnh tools/Run-Bt3Load.ps1 -Rounds 20 -Label <mới>. Raw CSV/summary/manifest: evidence/load-barrier100/. Run load thăm dò trước đó có peak<100, không dùng làm workload chính.', 'small')
    next_page()

    add('Câu 5. Engineering evidence và ownership', 'h1')
    body.append(T([
        ['AC / risk','Test/file → kết quả','Evidence'],
        ['AC1 authority','T01/T06/T07, OperationTests + lobby suites → pass','pairwise 32/32; baseline/final XML'],
        ['AC2 retry/session','T02/T03/T12, Boundary/Operation/Progress tests → B1, L1, S1; recovery pass','final-regression; recovery RED/GREEN'],
        ['AC3 overlay','T08/T09/T16, ProgressTests → host native pass; guest local only','RED/GREEN; 20 cycle CSV'],
        ['AC4 completion/spawn','T04/T15, Operation + baseline spawn gate → pass','final-regression; no late failure→Playing'],
        ['AC5 failure/isolation','T05/T10/T11/T14, Boundary/Progress → B1,L0,F1; readable menu','real missing-scene + final XML']], [W*.20,W*.49,W*.31]))
    add('Tất cả test file thuộc Assets/_Project/Tests/PlayMode; production commit '+production+'. Full Test IDs/file/commit/results và giới hạn từng assertion: traceability.md. README có prerequisites, lệnh test/mutation/load và cách xác minh XML đúng suites.', 'small')
    add('AI Usage Log và review trực tiếp', 'h2')
    add('Giữ plan trước/sau, R1–R3 nguyên văn, Câu 1 chốt; prompt gốc tạo plan-v1 thiếu trong bản ghi, không dựng lại. AI sinh18 cases; sinh viên trực tiếp xóa T17/T18, before/after giữ nguyên, bảng cuối16 IDs và pairwise32/32. User duyệt diff v2 rồi recovery riêng. Công cụ: Codex, Git/Python, Unity PlayMode, .NET harness, ReportLab/PyMuPDF cho PDF.', 'small')
    add('Ít nhất hai điểm AI cần kiểm chứng', 'h2')
    add('1. Label trong RoomPanel + fixed2s trông hợp lý nhưng sai vòng đời; sinh viên xóa T18. T08 native từng RED rồi GREEN quan sát đúng khoảng sau RoomPanel destruction.\n2. TryStart=false không chứng minh side effect chỉ một lần; sinh viên xóa T17. T02/T05 đo B/native L/terminal; mutation chỉ kiểm capacity.\n3. Constant fakeLoads=0 assertion bị bỏ; native SceneManager observer thay thế.\n4. UI lỗi đọc được chưa đủ recovery: test mới RED vì mất manager, sửa navigation rồi GREEN phiên mới.', 'small')
    add('Điều chưa chứng minh và test tiếp theo', 'h2')
    add('Guest process riêng qua scene, native hang/crash sau startup, Internet/reconnect, real arena p95 và randomized suite order còn thiếu evidence. Không dùng virtual guest hoặc fake-delay load để kết luận các phần đó. Reflection ở trang tiếp theo ưu tiên host/guest hai process và làm guest tải chậm hơn host.', 'small')
    add('[README tái lập]('+branch.replace('/tree/','/blob/')+'/docs/assignments/bt3-vt-066/README.md) · [Traceability]('+branch.replace('/tree/','/blob/')+'/docs/assignments/bt3-vt-066/traceability.md)', 'small')
    next_page()
    add('Câu 5. Reflection', 'h1')
    add(f'{reflection_words} từ theo khoảng trắng. Codex hỗ trợ viết lại ở ngôi thứ nhất dựa trên bằng chứng thực thi. Chi tiết trong AI Usage Log.', 'small')
    for paragraph in reflection.split('\n\n'): add(paragraph)
    add('Cam kết đã được sinh viên xác nhận', 'h2')
    add(approval['commitment'])
    add('Tình trạng bàn giao', 'h2')
    add(('Branch GitHub đã xác minh: '+branch+'; remote SHA: '+verification['sha']+'. Không merge; sinh viên tự nộp PDF và link vào Google Classroom.' if verified else 'Bản PDF để review: nhánh codex/bt3-vt-066 hiện chỉ local. Sinh viên đã xác nhận cam kết nhưng trả lời “Chưa cho phép push”. Link branch trên trang đầu là URL dự kiến, chưa được công bố/xác minh; sẽ cập nhật sau khi được phép push. Không merge.'), 'small')
    body.append(MainEnd())
    next_page()

    # Appendices: preserved AI inputs/outputs/review and execution evidence.
    add('PHỤ LỤC · AI USAGE LOG VÀ EVIDENCE', 'title')
    add('Phần phụ lục không tính vào 8 trang nội dung chính. Các bản nguồn dưới đây giữ nội dung lịch sử; trạng thái “chưa chạy” trong plan cũ mô tả thời điểm lập plan, không phủ nhận evidence thực thi sau đó.', 'small')
    for title, filename in [
        ('A1. AI Usage Log','AI-USAGE-LOG.md'),
        ('A2. Câu 1 đã chốt — bản nguồn đầy đủ, không sửa AC/invariant/R1–R3','question-1.md'),
        ('A3. Output plan gốc trước review','plan-v1.md'),
        ('A4. Output plan sau review','plan-reviewed.md'),
        ('A5. Review plan nguyên văn trước và lúc tiếp tục','review-01-user-verbatim.md'),
        ('A6. R1–R3 nguyên văn lúc tiếp tục','review-02-user-verbatim.md'),
        ('A7. Test cases AI-before (18 IDs), strategy và candidate review','test-cases-ai-v1.md'),
        ('A8. Sinh viên trực tiếp xóa T17/T18 — nguyên văn và before/after','test-review-user-verbatim.md'),
        ('A9. Bảng test sau chỉnh sửa (16 IDs)','test-cases-reviewed.md'),
        ('A10. Traceability đầy đủ','traceability.md')]:
        add(title,'h1'); append_markdown(body,(DOC/filename).read_text(encoding='utf-8')); body.append(Spacer(1,10))
    add('B. Log/XML thực thi, SHA và assertions', 'h1')
    for run in [report['baseline']]+report['runs']:
        add(run['label']+f" — {run['passed']}/{run['total']} pass, exit {run['exitCode']}", 'h2')
        add('Source SHA ghi trong manifest: '+run['sourceSha']+'; [XML thực tế]('+branch.replace('/tree/','/blob/')+'/'+run['xml']+').', 'small')
        failures = run['failures']
        selected = [f for f in failures if 'T08_' in f['id'] or 'T09_' in f['id'] or 'RecreatesMenu' in f['id'] or 'Boundary(4)' in f['id']]
        if not selected and failures: selected = failures[:1]
        for failure in selected:
            add(failure['id']+'\n'+failure['assertion'], 'small')
    add('B1. Pairwise witness và kiểm sau review', 'h2')
    add(json.dumps(js('pairwise-review-verification.json'),ensure_ascii=False,indent=2), 'small')
    add('Universe: requester×roster11, requester×delivery9, roster×delivery12. Từng pair có danh sách PW witness trong pairwise-ai-v1.json; verifier kiểm trên toàn bộ33 feasible full rows, không chỉ đếm rows đã chọn.', 'small')
    add('B2. 20 vòng tải 100 tác vụ — summary và raw data', 'h2')
    load_rows = [['Round','Completed','Requests','Peak in-flight','p95 ms','Unexpected/timeout/duplicates']]
    for r in js('evidence/load-barrier100/summary.json')['results']:
        load_rows.append([r['round'],r['completed'],r['requests'],r['peakSubmittedInFlight'],f"{r['p95Ms']:.4f}",'0 / 0 / 0'])
    body.append(T(load_rows,[W*x for x in [.10,.14,.14,.17,.15,.30]]))
    add('[Raw CSV 2000 samples]('+branch.replace('/tree/','/blob/')+'/docs/assignments/bt3-vt-066/evidence/load-barrier100/raw.csv). Identity session riêng cho mỗi op, requests retry giữ identity; summary dependencies/scope và source hash trong cùng thư mục.', 'small')
    add('B3. Flaky checks: 20 real scene cycles', 'h2')
    cycles = list(csv.DictReader((DOC/'evidence/flaky/real-scene-cycles.csv').open(encoding='utf-8')))
    body.append(T([['Round','Seconds','Result']]+[[r['round'],r['seconds'],r['result']] for r in cycles],[W*.2,W*.4,W*.4]))
    add('B4. Reproduction và dữ liệu nguồn', 'h2')
    append_markdown(body,(DOC/'README.md').read_text(encoding='utf-8'))

    output = Path(args.output); output.parent.mkdir(parents=True,exist_ok=True)
    def page(canvas, doc):
        canvas.setFont('Arial',7.3); canvas.setFillColor(colors.HexColor('#53687C'))
        canvas.drawString(42,A4[1]-25,'23001947 · VT-066 · SE2026-T17')
        canvas.drawRightString(A4[0]-42,A4[1]-25,'Nội dung chính' if doc.page<=8 else 'Phụ lục AI Usage Log / evidence')
        canvas.drawString(42,23, 'Đã xác minh branch · không merge' if verified else 'Bản review · branch local, chưa push')
        canvas.drawRightString(A4[0]-42,23,str(doc.page))
    document = Document(str(output),pagesize=A4,rightMargin=42,leftMargin=42,topMargin=43,bottomMargin=40,
                        title='BT3_23001947_VT-066',author='Nguyễn Chí Hoàng Tú',pageCompression=1)
    document.build(body,onFirstPage=page,onLaterPages=page)
    assert document.main_page_count == 8, f'Main content overflowed: {document.main_page_count} pages'
    pdf = fitz.open(output)
    qa = ROOT/'TestResults/BT3/pdf-qa'; qa.mkdir(parents=True,exist_ok=True)
    for i,p in enumerate(pdf): p.get_pixmap(matrix=fitz.Matrix(1.25,1.25),alpha=False).save(qa/f'page-{i+1:02}.png')
    texts = [p.get_text() for p in pdf]
    assert all(name in texts[0] for name in ['Nguyễn Chí Hoàng Tú','23001947','VT-066',baseline])
    assert all(term in '\n'.join(texts) for term in ['AC1','AC2','AC3','AC4','AC5',str(reflection_words),'32.1951'])
    assert not any('\ufffd' in t for t in texts)
    links = [l.get('uri') for p in pdf for l in p.get_links() if l.get('uri')]
    report_qa = {'output':str(output),'mainPages':document.main_page_count,'totalPages':len(pdf),'appendixStartsAt':9,
                 'reflectionWords':reflection_words,'githubVerified':verified,'branchUrl':branch,
                 'fontNames':sorted({f[3] for p in pdf for f in p.get_fonts()}),'linkCount':len(links),
                 'renderFolder':str(qa),'unicodeReplacementCharacters':False,'visualInspection':'Pending model inspection of rendered PNGs'}
    (DOC/'pdf-qa.json').write_text(json.dumps(report_qa,ensure_ascii=False,indent=2),encoding='utf-8')
    # Contact sheets make every rendered page available for visual layout inspection.
    from PIL import Image,ImageOps,ImageDraw
    for group in range((len(pdf)+5)//6):
        sheet = Image.new('RGB',(930,940),'#cbd3dd'); draw=ImageDraw.Draw(sheet)
        for j in range(6):
            n=group*6+j
            if n>=len(pdf): break
            thumb=Image.open(qa/f'page-{n+1:02}.png'); thumb.thumbnail((300,425))
            x=(j%3)*310+(310-thumb.width)//2; y=(j//3)*470+22
            sheet.paste(thumb,(x,y)); draw.text((x,y-18),str(n+1),fill='black')
        sheet.save(qa/f'contact-{group+1:02}.png')
    print(json.dumps(report_qa,ensure_ascii=False,indent=2))

if __name__=='__main__': main()
