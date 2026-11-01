

<!-- Start of picture text -->
. SS<br>4 ee! 7s<br>4 ow!<br>2 S<br><!-- End of picture text -->

###### **SOFTWARE ENGINEERING 2026** 

# **Design, Code Quality & Technical Debt** 

Day 5 · 120 minutes 

Working software must remain understandable, testable, and safe to change. 

**05** 

**DAY 5** 

## **Learning outcomes** 

**Diagnose** 

**1** 

**Design** 

**2** 

**Refactor 3** 

**Manage debt** 

**4** 

Recognize coupling, low cohesion, duplication, long functions, and unclear boundaries. 

Apply separation of concerns, information hiding, and dependency direction. 

Improve structure without changing observable behavior. 

Record, prioritize, and pay technical debt using engineering evidence. 

Software Engineering 2026 

**02** 

**DAY 5** 

## **Where Day 5 fits** 



<!-- Start of picture text -->
Problem Requirements Architecture Design Implementatio<br>n<br><!-- End of picture text -->

#### **Architecture chooses the major boundaries. Design decides how code behaves inside those boundaries.** 

The decisions become visible in modules, interfaces, functions, data structures, and tests. 

Software Engineering 2026 

**03** 

**DAY 5** 

## **Maintainability as change capacity** 

**Change effort** 

**=** 

##### **Impact surface** 

How many components can this change affect? 

**×** 

##### **Uncertainty** 

How confident are we about the consequences? 

**Good design reduces the impact surface. Tests and clear contracts reduce uncertainty.** 

Software Engineering 2026 

**04** 

**DAY 5** 

## **Decomposition** 

**Purposeful modules** 

**One large module** 

UI, validation, business rules, persistence, and external calls share one change surface. 

Interface or controller **Domain logic** Business decisions 

**Infrastructure** Database and external services 

##### **A boundary earns its place when it contains a reason to change.** 

Software Engineering 2026 

**05** 

**DAY 5** 

## **Cohesion and coupling** 

**High cohesion** 

### **Low coupling** 

validate slot calculate availability **reserve appointment** cancel appointment 

**Scheduling Notification** 

**One explicit contract** 

Cohesion: related behavior stays together. Coupling: one module depends on details of another. 

Software Engineering 2026 

**06** 

**DAY 5** 

## **Information hiding** 



<!-- Start of picture text -->
PUBLIC CONTRACT<br>bookAppointment(request)<br>cancelAppointment(id, actor)<br>getAvailableSlots(date)<br>Hidden implementation<br>tables · cache · retry policy · vendor API<br><!-- End of picture text -->

#### **Callers depend on stable behavior, not internal choices.** 

Software Engineering 2026 

**07** 

**DAY 5** 

## **Separation of concerns** 



<!-- Start of picture text -->
Domain Infrastructure<br>Presentation<br>HTTP, input parsing,  Policies, invariants,  Database, queues, vendor<br>response format decisions APIs<br><!-- End of picture text -->

###### **Where should “a student may hold only one active booking per slot” live?** 

Software Engineering 2026 

**08** 

**DAY 5** 

## **SOLID as design questions** 

**S Does this unit have one coherent reason to change?** 

**O** Can new behavior arrive without editing stable branches everywhere? 

**L** Can a replacement honor the same observable contract? 

- **I** Does each caller depend only on the operations it needs? 

- **D** Do policy modules depend on abstractions rather than infrastructure details? 

Software Engineering 2026 

**09** 

**DAY 5** 

## **KISS, DRY, and YAGNI** 

**KISS DRY YAGNI** Prefer the simplest design that explains Remove duplicated knowledge. Similar Delay speculative extension points until the current requirement and remains code can represent different business evidence shows a real variation. testable. rules. 

#### **Premature abstraction can be more expensive than small, visible duplication.** 

Software Engineering 2026 

**10** 

**DAY 5** 

## **Code smells as investigation signals** 

**Long function** 

Too many levels of detail 

**Shotgun surgery** 

One change touches many files 

**Primitive obsession** 

Domain ideas appear as loose strings and numbers 

**God class** 

Too many responsibilities 

###### **Feature envy** 

Logic lives beside the wrong data 

###### **Duplicate logic** 

One rule has multiple implementations 

Software Engineering 2026 

**11** 

**DAY 5** 

## **Example: working code with hidden costs** 

###### **Mixed concerns** 

HTTP, policy, persistence, notification 

```
async function book(req, res) {
  const u = await db.users.find(req.body.userId);
  const s = await db.slots.find(req.body.slotId);
  if (!u || !s) return res.status(400).send('bad');
  if (s.booked) return res.status(409).send('busy');
  s.booked = true; s.userId = u.id;
  await db.slots.save(s);
  await mail.send(u.email, 'Booked');
  res.send({ ok: true });
}
```

###### **Weak model** 

A boolean represents a booking lifecycle 

**Race condition** 

Two requests can see booked = false 

**Poor errors** 

“bad” hides which contract failed 

Software Engineering 2026 

**12** 

**DAY 5** 

## **Refactored design** 



<!-- Start of picture text -->
async function bookAppointment(command) {<br>  return unitOfWork.run(async () => {<br>    const slot = await slots.lock(command.slotId);<br>    const result = slot.bookFor(command.studentId);<br>    await slots.save(slot);<br>    events.publish(result.events);<br>    return result.booking;<br>  });<br>}<br><!-- End of picture text -->



<!-- Start of picture text -->
Domain object<br>Owns booking invariants<br>Transaction boundary<br>Protects concurrent updates<br>Domain event<br>Notification becomes a subscriber<br><!-- End of picture text -->

**More structure is justified only when it makes a real change safer.** 

Software Engineering 2026 

**13** 

**DAY 5** 

## **Refactoring loop** 



<!-- Start of picture text -->
1 2 3 4<br>Characterize Change Verify Commit<br>Capture current behavior  Make one small structural  Run tests and inspect the  Preserve a reversible<br>with tests move diff checkpoint<br><!-- End of picture text -->

#### **Refactoring changes structure while preserving observable behavior.** 

Software Engineering 2026 

**14** 

**DAY 5** 

## **Tests create design freedom** 

**Without tests With behavior tests Tests still need judgment** Every structural change The team can separate carries unknown regression structural edits from A green suite proves only risk. feature changes and the behavior the tests Teams avoid cleanup until detect regressions actually cover. change becomes urgent. quickly. 

Software Engineering 2026 

**15** 

**DAY 5** 

## **Technical debt** 

### **A design shortcut that makes future change more expensive.** 



<!-- Start of picture text -->
Principal<br><!-- End of picture text -->

###### **Interest** 

The effort required to remove or redesign the shortcut. 

Extra cost paid whenever the affected code changes. 

**Debt becomes dangerous when the team cannot see it, measure it, or choose when to repay it.** 

Software Engineering 2026 

**16** 

**DAY 5** 

## **Four technical debt patterns** 

###### **Deliberate and prudent** 

Ship a limited solution, record the trade-off, schedule repayment 

###### **Accidental and prudent** 

The team learns a better design after implementation 

###### **Deliberate and reckless** 

Skip design and tests without understanding the consequence 

###### **Accidental and reckless** 

The team lacks the knowledge to recognize the problem 

Software Engineering 2026 

**17** 

**DAY 5** 

## **Technical debt register** 

|**Debt item**|**Evidence**|**Impact**|**Trigger**|**Owner**|
|---|---|---|---|---|
|**Booking rule duplicated**|3 files contain different checks|Change may create<br>inconsistent behavior|Next booking feature|Minh|
|**No migration rollback**|Deployment rehearsal failed|Recovery time is unknown|Before public demo|Lan|
|**Agent-generated dependency**|Package has no maintainer<br>review|Security and upgrade risk|Before release|An|



###### **A debt item needs evidence and a repayment trigger, not a vague “clean up later.”** 

Software Engineering 2026 

**18** 

**DAY 5** 

## **Debt prioritization** 



<!-- Start of picture text -->
Frequent change Pay first<br>Lower impact<br>CHANGE FREQUENCY<br>Monitor Protect and plan<br><!-- End of picture text -->



<!-- Start of picture text -->
FAILURE IMPACT<br><!-- End of picture text -->

###### **Debt near important, frequently changed code accumulates the most interest.** 

Software Engineering 2026 

**19** 

**DAY 5** 

## **AI-generated code and design drift** 



<!-- Start of picture text -->
Prompt Patch Next prompt Repository<br>Local request Plausible code Another local request Inconsistent system<br><!-- End of picture text -->

**Typical drift: duplicated helpers · inconsistent errors · bypassed boundaries · unused dependencies** 

###### **Repository context helps. Engineering review still decides what belongs.** 

Software Engineering 2026 

**20** 

**DAY 5** 

## **Review checklist for agent-generated changes** 

**11** 

**Boundary** 

**22** 

###### **Contract** 

Does the patch place behavior in the correct module? 

Does it preserve API, data, and error semantics? 

**Duplication 33** 

**44** 

**Failure** 

Did the agent create another source of truth? 

What happens on timeout, concurrency, or partial success? 

**Dependency 55** 

**Evidence 66** 

Who maintains the new package and why is it needed? 

Which tests prove the requirement and the failure cases? 

Software Engineering 2026 

**21** 

**DAY 5** 

## **Team exercise: design review** 

**20 minutes** 

**Inspect · 5 min** 

Choose one recently changed feature. Mark responsibilities, dependencies, and duplicated rules. 

**Explain · 5 min** 

Draw the current dependency direction. Identify one smell and its change risk. 

**Refactor plan · 7 min** 

Propose the smallest safe structural change. Name the tests needed before editing. 

**Share · 3 min** 

State the debt item, evidence, owner, and repayment trigger. 

Software Engineering 2026 

**22** 

**DAY 5** 

## **Design review output** 

###### **Problem evidence** 

**22** 

**Change risk 22** 

**Refactor scope 22** 

**22 Verification** 

The smell appears in real repository code, with file or symbol references. 

The team connects the structure to a likely defect or costly change. 

The proposal is small, reversible, and behavior preserving. 

Tests protect the behavior that the refactor could break. 

**8 points · fast formative feedback** 

Software Engineering 2026 

**23** 

**DAY 5** 

## **Key ideas** 

**Design** Organize code around responsibilities and stable contracts. **Quality** Make code understandable, testable, and safe to change. **Refactoring** Improve structure through small behavior-preserving steps. **Technical debt** Use evidence, ownership, and repayment triggers. **AI assistance** Treat generated code as a proposal that requires system-level review. 

**Next: AI-assisted Software Development & Vibe Coding** 

Software Engineering 2026 

**24** 

