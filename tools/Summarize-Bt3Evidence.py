from pathlib import Path
import csv
import hashlib
import json
import subprocess
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
DOC = ROOT / 'docs/assignments/bt3-vt-066'
EVIDENCE = DOC / 'evidence'

def read_json(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))

def run(label, xml_name='results.xml'):
    directory = EVIDENCE / label
    xml = ET.parse(directory / xml_name).getroot()
    manifest = read_json(directory / ('combined-manifest.json' if xml_name == 'combined.xml' else 'manifest.json'))
    return {'label': label, 'result': xml.get('result'), 'total': int(xml.get('total')),
            'passed': int(xml.get('passed')), 'failed': int(xml.get('failed')), 'exitCode': manifest['exitCode'],
            'sourceSha': manifest['sourceSha'], 'xml': str((directory / xml_name).relative_to(ROOT)).replace('\\', '/'),
            'failures': [{'id': case.get('fullname'), 'assertion': case.findtext('failure/message')}
                         for case in xml.iter('test-case') if case.get('result') == 'Failed']}

baseline = run('baseline', 'combined.xml')
runs = [run(label) for label in ['red-assertions', 'green-attempt1', 'regression', 'regression-reviewed',
                                'recovery-red', 'recovery-green', 'final-regression',
                                'mutation-final/clean', 'mutation-final/mutant', 'mutation-final/restored']]
assert baseline['total'] == baseline['passed'] == 19
assert next(r for r in runs if r['label'] == 'final-regression')['passed'] == 57
mutation = read_json(EVIDENCE / 'mutation-final/summary.json')
assert mutation['killedAtFour'] and mutation['targetRestored']
load = read_json(EVIDENCE / 'load-barrier100/summary.json')
load_manifest = read_json(EVIDENCE / 'load-barrier100/manifest.json')
for row in load['results']:
    assert row['completed'] == row['peakSubmittedInFlight'] == 100
    assert row['unexpectedErrors'] == row['timeouts'] == row['duplicateBoundary'] == row['duplicateFakeLoads'] == row['duplicateTerminal'] == 0
compiled_hashes_match = all(hashlib.sha256(Path(row['path']).read_bytes()).hexdigest().upper() == row['sha256'].upper()
                            for row in load_manifest['sourceFiles'])
assert compiled_hashes_match
cycles = list(csv.DictReader((EVIDENCE / 'flaky/real-scene-cycles.csv').open(encoding='utf-8')))
assert len(cycles) == 20 and all(row['result'] == 'Passed' for row in cycles)
guard = ROOT / 'Assets/_Project/Scripts/Networking/ArenaNetworkManager.cs'
guard_lines = [(i, line.strip()) for i, line in enumerate(guard.read_text(encoding='utf-8-sig').splitlines(), 1)
               if 'Count(connection => connection.isAuthenticated)' in line]
assert len(guard_lines) == 1 and ') > LobbyLimits.Capacity' in guard_lines[0][1]
report = {'baselineSha': 'd939568aae110668df5f0114541e4498593b6108',
          'productionSha': mutation['sourceSha'], 'baseline': baseline, 'runs': runs, 'mutation': mutation,
          'load': {'evidence': 'evidence/load-barrier100', 'sourceShaAtRun': load_manifest['sourceSha'],
                   'compiledSourceHashesMatchFinalProduction': compiled_hashes_match,
                   'rounds': len(load['results']), 'operations': sum(r['completed'] for r in load['results']),
                   'requests': sum(r['requests'] for r in load['results']),
                   'expectedFailures': sum(r['expectedFailures'] for r in load['results']),
                   'firstRound': load['results'][0],
                   'p95RangeMs': [min(r['p95Ms'] for r in load['results']), max(r['p95Ms'] for r in load['results'])],
                   'limits': load['scope'], 'dependencies': load['dependencies']},
          'flaky': {'realSceneCycles': 20, 'failures': 0, 'scope': 'timing lifecycle window and consecutive sessions/shared singleton/observer; not randomized test order or Internet'},
          'coverage': {'line': None, 'branch': None, 'reason': 'com.unity.testtools.codecoverage not declared/installed; no line/branch percentage collected',
                       'riskCoverage': 'traceability.md', 'missing': ['multi-process guest UI', 'native load hang/crash', 'Internet packet loss/reconnect', 'real arena p95']},
          'submissionGuard': {'file': str(guard.relative_to(ROOT)).replace('\\', '/'), 'baselineLine': 203,
                              'currentLine': guard_lines[0][0], 'text': guard_lines[0][1], 'operatorRestored': True}}
(DOC / 'evidence-summary.json').write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding='utf-8')
print('Verified baseline, 57 regression cases, mutation at 4, 20 scene cycles, 20x100 load and final guard.')
