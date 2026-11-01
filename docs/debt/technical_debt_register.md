# Technical debt register

Record actual accepted debt with evidence, a repayment trigger, and an owner.
Proposed future gameplay shortcuts are not implemented debt in this scaffold.

| Debt item | Evidence | Impact | Trigger | Owner |
| --- | --- | --- | --- | --- |
| TD-001: Editor baseline differs from the earlier 6000.3 proposal | Task 1.1 explicitly selects the local Editor; `ProjectSettings/ProjectVersion.txt` pins 6000.6.7f1. SRS Part 2 and the engineering manual still describe 6000.3. | Teammates following the older proposal could use incompatible rendering/package versions. | Before another workstation imports the project or M1 baseline sign-off: reconcile both specification documents with the approved installed-patch decision. | Lead Engineer / PM |

Editor license activation and unexecuted verification are recorded in the
verification report as blockers, not as waived acceptance criteria.
