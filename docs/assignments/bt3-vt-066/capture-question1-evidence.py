"""Read-only baseline inspection; writes evidence only beside this script."""
from datetime import datetime, timezone
from hashlib import sha256
from pathlib import Path
import json
import subprocess
from zipfile import ZipFile
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[3]
OUT = Path(__file__).resolve().parent / 'evidence' / 'question1'
OUT.mkdir(parents=True, exist_ok=True)
BASELINE = 'd939568aae110668df5f0114541e4498593b6108'

def git(*args):
    result = subprocess.run(['git', *args], cwd=ROOT, capture_output=True)
    return {'command': ['git', *args], 'exit_code': result.returncode,
            'stdout': result.stdout.decode('utf-8-sig', errors='replace'),
            'stderr': result.stderr.decode('utf-8-sig', errors='replace')}

commands = [git(*args) for args in [
    ('status', '--short'), ('remote', '-v'), ('rev-parse', 'HEAD'),
    ('rev-parse', 'origin/main'), ('branch', '--show-current'),
    ('worktree', 'list'), ('log', '-1', '--format=fuller'),
    ('diff', '--name-only', BASELINE, '--', 'Assets/_Project/Scripts',
     'Assets/_Project/Tests', 'Assets/Mirror/Core/NetworkManager.cs',
     'tools/Run-DomainTests.ps1', 'ProjectSettings/ProjectVersion.txt')]]
files = [
    'Assets/_Project/Scripts/Client/UI/LobbyRoomController.cs',
    'Assets/_Project/Scripts/Client/UI/LobbyRoomView.cs',
    'Assets/_Project/Scripts/Networking/ArenaNetworkManager.cs',
    'Assets/_Project/Scripts/Networking/LobbyMessages.cs',
    'Assets/_Project/Scripts/Networking/MatchSessionNavigation.cs',
    'Assets/_Project/Scripts/Server/Sessions/LobbyRoom.cs',
    'Assets/_Project/Scripts/Server/Sessions/ServerLobbyController.cs',
    'Assets/Mirror/Core/NetworkManager.cs',
    'Assets/_Project/Tests/PlayMode/LobbyRoomManagementTests.cs',
    'Assets/_Project/Tests/PlayMode/LobbyGovernanceAndScreenTests.cs',
    'Assets/_Project/Tests/PlayMode/SmallArena.PlayMode.Tests.asmdef',
    'tools/Run-DomainTests.ps1', 'ProjectSettings/ProjectVersion.txt']
parts = ['Baseline: ' + BASELINE]
for file in files:
    result = git('show', BASELINE + ':' + file)
    if result['exit_code']:
        raise RuntimeError(result)
    parts.append('\nFILE ' + file + '\n' + '\n'.join(
        f'{i}: {line}' for i, line in enumerate(result['stdout'].splitlines(), 1)))
(OUT / 'baseline-numbered-source.txt').write_text('\n'.join(parts), encoding='utf-8')
hashes = {p.name: sha256(p.read_bytes()).hexdigest() for p in [
    OUT.parent.parent / 'plan-v1.md', OUT.parent.parent / 'review-01-user-verbatim.md',
    OUT.parent.parent / 'review-02-user-verbatim.md']}
(OUT / 'repository-state.json').write_text(json.dumps({
    'captured_utc': datetime.now(timezone.utc).isoformat(), 'baseline': BASELINE,
    'commands': commands, 'preserved_document_sha256': hashes,
    'live_remote_check': 'Separate approved git ls-remote invocation; see remote-main.txt.'
}, ensure_ascii=False, indent=2), encoding='utf-8')
ns = {'a': 'http://schemas.openxmlformats.org/drawingml/2006/main'}
lectures = []
for name in ['Lecture_06_AI_Assisted_Software_Development_and_Vibe_Coding_v3.pptx',
             'Lecture_07_Software_Testing_and_Quality_Engineering.pptx']:
    path = Path('C:/Users/tasdus_117/Downloads') / name
    with ZipFile(path) as z:
        slides = [n for n in z.namelist() if n.startswith('ppt/slides/slide') and n.endswith('.xml')]
        slides.sort(key=lambda n: int(n.rsplit('slide', 1)[1].split('.')[0]))
        lectures.append({'path': str(path), 'sha256': sha256(path.read_bytes()).hexdigest(),
                         'slides': [{'part': n, 'text': ' | '.join(
                             node.text or '' for node in ET.fromstring(z.read(n)).findall('.//a:t', ns))}
                                    for n in slides]})
(OUT / 'lecture-text.json').write_text(json.dumps(lectures, ensure_ascii=False, indent=2), encoding='utf-8')
print('Saved baseline source, repository state, document hashes and lecture text:', OUT)
