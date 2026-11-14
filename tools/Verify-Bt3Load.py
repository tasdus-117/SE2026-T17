"""Independent raw CSV verification; never runs or simulates the workload."""
from collections import defaultdict
import csv
import json
import math
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'docs/assignments/bt3-vt-066/evidence/load-barrier100'
rows = list(csv.DictReader((OUT / 'raw.csv').open(encoding='utf-8')))
summary = json.loads((OUT / 'summary.json').read_text(encoding='utf-8'))
by_round = defaultdict(list)
assert len({r['identity'] for r in rows}) == len(rows) == 2000
for row in rows:
    by_round[int(row['round'])].append(row)
    failure = row['expected_failure'].lower() == 'true'
    assert int(row['requests']) == 3 and int(row['boundary_attempts']) == 1
    assert int(row['fake_loader_side_effects']) == (0 if failure else 1)
    assert int(row['success_transitions']) == (0 if failure else 1)
    assert int(row['failure_transitions']) == int(row['failure_callbacks']) == (1 if failure else 0)
    assert row['terminal'] == ('Failed' if failure else 'Playing') and row['final_room'] == 'Offline'
    assert float(row['latency_ms']) >= 0
checks = []
for expected in summary['results']:
    sample = by_round[expected['round']]
    assert len(sample) == 100
    values = sorted(float(row['latency_ms']) for row in sample)
    rank = math.ceil(.95 * len(values))
    p95 = values[rank - 1]
    assert abs(p95 - expected['p95Ms']) < 0.00001
    checks.append({'round':expected['round'], 'samples':100, 'nearestRank':rank, 'p95MsFromRaw':p95, 'matchesSummary':True})
result = {'method':'Independent CSV parse and nearest-rank calculation; rounded raw values compared at 1e-5 ms tolerance',
          'operations':len(rows), 'distinctIdentities':2000, 'requests':sum(int(r['requests']) for r in rows),
          'expectedFailures':sum(r['expected_failure'].lower()=='true' for r in rows),
          'duplicateBoundaryOrFakeLoadOrTerminal':0, 'rounds':checks}
(OUT / 'independent-verification.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
print('PASS: independent raw CSV check, 2000 identities, 6000 requests, all 20 nearest-rank p95 values match.')
