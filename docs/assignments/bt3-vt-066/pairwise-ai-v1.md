# Pairwise AI v1 — T01 data

| ID | Requester | Roster | Delivery | Requests | Boundary / actual load / success terminal |
|---|---|---|---|---:|---|
| PW01 | host | solo_ready | single | 1 | 0/0/0 |
| PW02 | host | two_partial | burst | 2 | 0/0/0 |
| PW03 | host | two_ready | after_result | 2 | 1/1/1 |
| PW04 | guest | two_partial | single | 1 | 0/0/0 |
| PW05 | guest | two_ready | burst | 2 | 0/0/0 |
| PW06 | guest | four_ready | after_result | 2 | 0/0/0 |
| PW07 | unknown | solo_ready | burst | 2 | 0/0/0 |
| PW08 | unknown | two_partial | after_result | 2 | 0/0/0 |
| PW09 | unknown | two_ready | single | 1 | 0/0/0 |
| PW10 | host | four_ready | single | 1 | 1/1/1 |
| PW11 | unknown | four_ready | burst | 2 | 0/0/0 |
| PW12 | host | solo_ready | after_result | 2 | 0/0/0 |

PASS: 32/32 feasible pairs; 12 selected rows from 33 valid full rows (36 unconstrained).
Coverage counts test data only. No Unity/production result is claimed. Pair witnesses: pairwise-ai-v1.json.
