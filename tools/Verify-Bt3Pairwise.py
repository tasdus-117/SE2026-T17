"""Generate a deterministic constrained pairwise design and verify all feasible pairs.

No production behavior is simulated here; this checks test-data coverage only.
"""
from itertools import combinations, product
from pathlib import Path
import json

FACTORS = {
    'requester': ['host', 'guest', 'unknown'],
    'roster': ['solo_ready', 'two_partial', 'two_ready', 'four_ready'],
    'delivery': ['single', 'burst', 'after_result'],
}
NAMES = list(FACTORS)

def feasible(row):
    # A real guest must be an admitted member alongside the host.
    return not (row['requester'] == 'guest' and row['roster'] == 'solo_ready')

def pairs(row):
    return {(a, row[a], b, row[b]) for a, b in combinations(NAMES, 2)}

def main():
    out = Path(__file__).resolve().parents[1] / 'docs/assignments/bt3-vt-066'
    rows = [dict(zip(NAMES, values)) for values in product(*FACTORS.values())]
    valid = [r for r in rows if feasible(r)]
    universe = set().union(*(pairs(r) for r in valid))
    pending = set(universe)
    selected = []
    while pending:
        best = max(valid, key=lambda r: len(pairs(r) & pending))
        if not (pairs(best) & pending):
            raise AssertionError('Uncoverable pair')
        selected.append(best)
        pending -= pairs(best)
    matrix = []
    for i, row in enumerate(selected, 1):
        accepted = row['requester'] == 'host' and row['roster'] in ('two_ready', 'four_ready')
        requests = 1 if row['delivery'] == 'single' else 2
        matrix.append({'id': f'PW{i:02}', **row, 'case': 'T01', 'requests': requests,
                       'expected_accepted': accepted,
                       'expected_boundary_attempts': int(accepted),
                       'expected_loader_side_effects': int(accepted),
                       'expected_terminal_success': int(accepted)})
    actual = set().union(*(pairs(r) for r in matrix))
    assert all(feasible(r) for r in matrix)
    assert actual == universe
    witnesses = [{'pair': list(p), 'rows': [r['id'] for r in matrix if p in pairs(r)]}
                 for p in sorted(universe)]
    report = {'scope': 'Waiting session, healthy loader, all submitted requests eventually dispatched; data coverage, not executed behavior',
              'factors': FACTORS, 'full_cartesian': len(rows), 'feasible_full_rows': len(valid),
              'excluded_full_rows': [r for r in rows if not feasible(r)],
              'required_pairs': len(universe), 'covered_pairs': len(actual), 'missing_pairs': [],
              'matrix_rows': len(matrix), 'matrix': matrix, 'pair_witnesses': witnesses,
              'review_status': 'AI draft; T01 must be rerun/reverified after student case edits'}
    (out / 'pairwise-ai-v1.json').write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    lines = ['# Pairwise AI v1 — T01 data', '',
             '| ID | Requester | Roster | Delivery | Requests | Boundary / actual load / success terminal |',
             '|---|---|---|---|---:|---|']
    for r in matrix:
        k = int(r['expected_accepted'])
        lines.append(f"| {r['id']} | {r['requester']} | {r['roster']} | {r['delivery']} | {r['requests']} | {k}/{k}/{k} |")
    lines += ['', f'PASS: {len(actual)}/{len(universe)} feasible pairs; {len(matrix)} selected rows from {len(valid)} valid full rows ({len(rows)} unconstrained).',
              'Coverage counts test data only. No Unity/production result is claimed. Pair witnesses: pairwise-ai-v1.json.']
    (out / 'pairwise-ai-v1.md').write_text('\n'.join(lines) + '\n', encoding='utf-8')
    print(lines[-2])

if __name__ == '__main__':
    main()
