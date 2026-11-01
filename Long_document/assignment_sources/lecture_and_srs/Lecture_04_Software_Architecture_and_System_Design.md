###### **SOFTWARE ENGINEERING 2026** 

# **Software Architecture and System Design** 

Lecture 04 · 120 minutes 

**Requirements Architecture** 

Vietnam National University 

## **One requirement, many possible systems** 

#### **University clinic appointment system** 

###### **Single web app** 

**Mobile + API** 

**Modular monolith** 

**Microservices** 

**Architecture explains why one structure fits the context better than another** 

undefined 

Software Engineering 2026 

## **Learning outcomes** 

- Translate requirements and quality attributes into architectural decisions 

- Compare common architectural styles and identify their trade-offs 

- Define module, API, data and deployment boundaries 

- Record decisions with lightweight Architecture Decision Records 

- Review AI-generated designs for drift, coupling and hidden risk 

undefined 

Software Engineering 2026 

## **Class plan** 

**1 Architecture thinking** 

**2 Quality attributes and constraints 3 Styles and boundaries 4 AI-assisted design review** 

**Group design studio** 

**5** 

**20 min** 

**20 min** 

**35 min** 

**20 min** 

**25 min** 

undefined 

Software Engineering 2026 

## **Requirements shape architecture** 

**Stakehold ers** 

**Requireme nts** 

**Quality attributes** 

**Decisions** 

**System structure** 

**Architecture starts before implementation, but evidence from implementation changes it** 

undefined 

Software Engineering 2026 

## **Software architecture** 

**The system's major elements, their relationships, and the decisions that are costly to change** 

###### **Elements** 

modules, services, databases 

**Relationships** 

calls, events, data flows 

**Decisions** boundaries, ownership, deployment 

undefined 

Software Engineering 2026 

## **Architectural decisions** 

###### **Hard to change later** 

- Where business rules live 

- Who owns each data set 

- How components communicate 

- How the system scales and fails 

###### **Easy to change locally** 

- Button color 

- Small validation message 

- A private helper function 

- A local refactoring with tests 

undefined 

Software Engineering 2026 

## **Quality attributes** 

**Availability** 

###### **Security** 

###### **Testability** 

Can users book when a component fails? 

Who may see or change medical data? 

Can a team verify behavior in isolation? 

**Performance** 

**Modifiability** 

**Operability** 

How fast under expected load? 

How costly is a new role or workflow? 

Can the team detect and recover from failure? 

undefined 

Software Engineering 2026 

## **Quality attribute scenario** 



<!-- Start of picture text -->
Source<br><!-- End of picture text -->

Two students 

**Artifact** 

Booking service + database 



<!-- Start of picture text -->
Stimulus<br><!-- End of picture text -->

Book the same slot 

**Response** One succeeds, one receives conflict 



<!-- Start of picture text -->
Environment<br><!-- End of picture text -->



<!-- Start of picture text -->
Normal operation<br><!-- End of picture text -->

###### **Measure** 

No duplicate slot; response < 2 s 

undefined 

Software Engineering 2026 

## **Constraints narrow the design space** 

- Team of 3–5 students with one semester of development time 

- Existing university identity provider 

- Low operating budget and limited DevOps experience 

- Personal data requires strict access control 

- GitHub collaboration and observable contribution history 

**The best architecture fits the constraints** 

undefined 

Software Engineering 2026 

## **Three views of one system** 



<!-- Start of picture text -->
Module view<br><!-- End of picture text -->

**How code is organized** 

packages, layers, ownership 



<!-- Start of picture text -->
Runtime view<br><!-- End of picture text -->

**How components interact** requests, events, failure 

**Deployment view** 

**Where software runs** processes, containers, networks 

**A single box-and-arrow diagram rarely answers every question** 

undefined 

Software Engineering 2026 

## **Cohesion and coupling** 

###### **High cohesion** 

- A module has one clear responsibility 

- Related rules change together 

- The module exposes a small interface 

- Tests focus on meaningful behavior 

###### **Low coupling** 

- Modules know little about each other 

- Dependencies point in one direction 

- Changes stay inside a boundary 

- Teams can work with fewer conflicts 

undefined 

Software Engineering 2026 

## **Boundary test** 

### **Where should BookingPolicy live?** 

**UI component** 

**API controller** 

**Booking domain module** 

**Database trigger** 

**Choose the boundary that owns the rule, not the screen that currently uses it** 

undefined 

Software Engineering 2026 

## **Layered architecture** 



<!-- Start of picture text -->
Presentation<br>Application<br>Domain<br>Infrastructure<br><!-- End of picture text -->

**Strength: understandable dependency direction** 

**Risk: business rules leak across layers** 

undefined 

Software Engineering 2026 

## **MVC and client–server** 

**Client View + interaction** 



**Controller request coordination** 



**Model domain + persistence** 

**MVC organizes application responsibilities. Client–server describes a runtime relationship.** 

undefined 

Software Engineering 2026 

## **Monolith, modular monolith and microservices** 

**Monolith Modular monolith Microservices Deployment** one unit one unit many units **Boundary strength** often weak explicit modules network + data ownership **Operational cost** low low to medium high **Team fit** small small to medium multiple independent teams **Course project** possible recommended default rarely justified 

undefined 

Software Engineering 2026 

## **Event-driven architecture** 

**Notification** 

**Booking service** 



**AppointmentBooked** 



**Analytics** 

**Benefit: producers stay independent of consumers** 

**Cost: ordering, duplication and debugging become harder** 

undefined 

Software Engineering 2026 

## **API boundaries** 

###### **Good contract** 

- Expresses domain actions 

- Validates inputs at the boundary 

- Uses stable identifiers 

- Returns explicit error semantics 

###### **Fragile contract** 

- Mirrors database tables 

- Leaks internal implementation details 

- Uses ambiguous status codes 

- Changes without version discipline 

undefined 

Software Engineering 2026 

## **Data ownership** 

**Identity Scheduling Booking** User profile Doctor slots Appointments 

**Notification** 

Delivery log 

**Shared database tables create hidden coupling. Ownership makes change impact visible.** 

undefined 

Software Engineering 2026 

## **Concurrency changes the design** 

**Student A Book 09:00** 



**Booking transaction** 



**Unique slot constraint** 

**Student B Book 09:00** 



**A screen-level check cannot guarantee correctness under concurrent requests** 

undefined 

Software Engineering 2026 

## **Architecture Decision Record** 

**Context** 

Team size, quality attributes, constraints 

**Decision** 

Use a modular monolith with PostgreSQL 

**Alternatives** 

Single unstructured app; microservices 

**Consequences** 

Simple deployment; module rules require discipline 

One decision per record. Keep it short enough that the team will maintain it. 

undefined 

Software Engineering 2026 

## **Trade-offs, not perfect solutions** 

##### **A choice improves one quality while adding cost elsewhere** 

**Add cache** faster reads stale data, invalidation **Split service** team autonomy network and operations **Use events** loose runtime coupling eventual consistency **Add abstraction** easier substitution more code and concepts 

undefined 

Software Engineering 2026 

## **Evolutionary architecture** 

**MVP** 

one deployable unit 

**Growth** 

explicit modules 

**Pressure** 

measure bottlenecks 

**Extraction** split only when evidence supports it 

**Preserve options. Delay expensive distribution until the system needs it.** 

undefined 

Software Engineering 2026 

## **Architecture drift in AI-generated code** 

###### **Typical drift** 

###### **Review signals** 

- Business rules duplicated in UI and API 

   - Dependency direction remains stable 

- New libraries solve the same problem twice 

- Data access bypasses module boundaries 

   - One owner for each rule and data set 

   - Tests protect boundaries 

   - ADRs explain exceptions 

- Prompted fixes add hidden shortcuts 

undefined 

Software Engineering 2026 

## **AI-assisted architecture workflow** 

**Specify context** 

**Generate options** 

**Challenge assumptions** 

**Choose + record** 

**Verify in code** 

**Useful prompts request alternatives and consequences, not a single confident answer** 

undefined 

Software Engineering 2026 

## **Architecture fitness checks** 

**Dependency test API contract test** 

Domain cannot import infrastructure 

Breaking response changes fail CI 

**Database rule** 

A slot remains unique under concurrency 

**Operational check** 

Health endpoint and structured logs exist 

undefined 

Software Engineering 2026 

## **Case study: university clinic** 

- Students book, cancel and reschedule appointments 

- Doctors publish available slots 

- Only authorized staff access patient notes 

- No slot may be booked twice 

- Email delivery may fail without losing the appointment 

- The first release serves one campus and a small team 

- **Identify 3 architecture drivers** 

undefined 

Software Engineering 2026 

## **Group design studio** 

**20 minutes · groups of 3–5** 

**1** Choose architecture style 

**2** Draw module + runtime views 

**3** Assign data ownership 

**4 Write one ADR** 

**5** Explain one trade-off 

undefined 

Software Engineering 2026 

## **Sample architecture** 

**Domain modules** 

**PostgreSQL** 

**Web client** 

**API + application** 

**Outbox worker** 

**Email provider** 

**Booking and scheduling remain modules in one deployable application. The outbox protects notification delivery.** 

undefined 

Software Engineering 2026 

## **Architecture review questions** 

- Which requirement drove each major component? 

- Where does each business rule live? 

- Who owns each data set? 

- What fails when email, database or identity is unavailable? 

- How will the team test concurrency and access control? 

- What change would force the next architectural step? 

undefined 

Software Engineering 2026 

## **Apply this to your team project** 

**1 Architecture context** 

users, constraints, quality attributes 

**2 Two views** 

module view + runtime view 

**3 One ADR** 

decision, alternatives, consequences 

**4 Evidence** 

repository structure and tests match the design 

undefined 

Software Engineering 2026 

## **Key ideas** 

- **01** Architecture connects requirements to system structure 

- **02** Quality attributes make trade-offs explicit 

- **03** Boundaries protect change and team ownership 

- **04** ADRs preserve decision context 

- **05 AI accelerates options, while engineers verify the system** 

undefined 

Software Engineering 2026 

