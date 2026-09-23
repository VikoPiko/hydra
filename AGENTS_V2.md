# AGENTS_V2.md — DevOps Platform Version 2 Upgrade Specification

## 1. Mission

Upgrade the existing DevOps Platform MVP into Version 2 while preserving the successful Version 1 architecture, layout, and operational-topology concept.

The project is now a solo Master's diploma project.

Do not redesign the product into a different application.

Version 2 should evolve the existing platform into a more complete developer workspace inspired by the useful parts of Azure DevOps, while keeping the project's differentiator:

A unified operational model connecting project work, distributed-system topology, deployments, runtime health, events, and dependency-impact analysis.

Version 2 must add:

- improved neutral visual design;
- a documented color palette/design system;
- real project creation and management;
- project-scoped navigation;
- task/work-item management;
- a Kanban-style board;
- Bugs, Tasks, User Stories, and optional Features/Epics;
- work-item assignment, state, priority, tags, relations, comments/history where reasonable;
- OAuth 2.0 / OpenID Connect authentication structure;
- project membership and basic roles;
- persistence of all newly created data;
- removal of assumptions that only the seeded demo project exists;
- preservation of the Commerce Demo seed project;
- improved dashboard data;
- improved monitoring/topology integration;
- architecture-drift foundations;
- environment comparison foundations;
- deployment/change correlation improvements;
- stronger filtering/search;
- better test coverage and documentation.

The platform must remain achievable by one developer.

Do not turn Version 2 into a full Azure DevOps clone.

## 2. Product Identity

The product is:

DevOps Platform for Centralized Lifecycle Management, Monitoring, and Dependency Analysis of Distributed Software Systems

The central model remains:

Planning / Work
      |
      v
Project
      |
      +----------------------+
      |                      |
      v                      v
Source / Delivery        Runtime System
      |                      |
      v                      v
Deployments --------> Operational Topology
                             |
                +------------+------------+
                |                         |
                v                         v
         Health / Events            Dependencies
                                          |
                                          v
                                   Impact Analysis

Version 2 should connect developer work with operational context without pretending to implement a complete ALM suite.

## 3. Version 2 Product Goals

A user should be able to:

1. Sign in using OpenID Connect.
2. Create a project.
3. Edit basic project information.
4. Open the seeded Commerce Demo project or a real user-created project.
5. Add project members where supported by current identity data.
6. Create environments.
7. Create system components.
8. Define dependencies.
9. Track deployment information.
10. Track runtime health.
11. View live topology.
12. Create User Stories, Tasks, Bugs, and optionally Features/Epics.
13. Manage work items through a board.
14. Assign work items.
15. Set state, priority, tags, iteration, and estimates where implemented.
16. Link work items to components and optionally deployments.
17. See recent project activity.
18. Run dependency impact analysis.
19. Compare declared and observed dependency foundations.
20. Understand what changed operationally around a deployment or health event.

## 4. Non-Goals

Do NOT implement:

- Git hosting;
- pull requests;
- complete CI engine;
- complete CD engine;
- package/artifact registry;
- full Azure Boards clone;
- advanced Scrum planning suite;
- complete Agile analytics;
- custom OAuth/OIDC authorization server;
- custom identity provider;
- custom MFA;
- custom password reset;
- enterprise directory synchronization;
- complete Kubernetes management;
- custom Prometheus replacement;
- custom log search platform;
- AI root-cause engine;
- automatic infrastructure provisioning;
- enterprise-grade billing;
- complete multi-tenancy SaaS platform.

Version 2 must deepen the project's core instead of expanding endlessly.

## 5. Repository Strategy

Keep the existing single Git monorepo.

Do not split frontend and backend into separate repositories.

Target structure remains approximately:

/
├─ AGENTS.md
├─ AGENTS_V2.md
├─ README.md
├─ compose.yaml
├─ .env.example
├─ docs/
│  ├─ vision.md
│  ├─ architecture.md
│  ├─ domain-model.md
│  ├─ authentication.md
│  ├─ work-items.md
│  ├─ design-system.md
│  ├─ api.md
│  └─ decisions/
├─ backend/
│  ├─ DevOpsPlatform.sln
│  ├─ src/
│  │  ├─ DevOpsPlatform.Api/
│  │  ├─ DevOpsPlatform.Application/
│  │  ├─ DevOpsPlatform.Domain/
│  │  ├─ DevOpsPlatform.Infrastructure/
│  │  └─ DevOpsPlatform.Persistence/
│  └─ tests/
│     ├─ DevOpsPlatform.UnitTests/
│     └─ DevOpsPlatform.IntegrationTests/
├─ frontend/
│  └─ web/
└─ scripts/

Version 2 is an incremental upgrade.

Inspect Version 1 before changing anything.

Do not recreate working code unnecessarily.

## 6. Solo-Developer Constraint

The project is now maintained by one developer.

Optimize accordingly.

Prefer:

- modular monolith;
- one PostgreSQL database;
- one frontend;
- one backend;
- one identity provider container for local development;
- simple infrastructure;
- clear vertical slices;
- high-value automation;
- straightforward deployment;
- minimal ceremony.

Avoid:

- distributed platform internals;
- multiple databases;
- Kafka/RabbitMQ inside the DevOps platform unless a real requirement emerges;
- excessive abstraction;
- speculative interfaces;
- duplicate APIs;
- architecture designed for a nonexistent 50-person team.

Every major new subsystem must justify its maintenance cost.

## 7. Version 2 Visual Direction

Keep the existing overall layout/navigation structure unless it is broken.

Do not perform a complete UX rewrite.

Improve the visual language to a neutral professional developer-tool aesthetic.

Primary theme:

White
Cool gray
Slate
Muted blue

Avoid:
- bright gradients;
- neon colors;
- excessive glassmorphism;
- overly saturated Azure-blue everywhere;
- decorative animations that distract from operational data.

The UI should feel closer to a serious engineering console than a marketing site.

## 8. Color Palette

Create a documented design palette and implement it through CSS variables / Tailwind tokens.

Suggested light-theme palette:

### Neutral

Background           #F8FAFC
Surface              #FFFFFF
Surface Subtle       #F1F5F9
Border               #E2E8F0
Border Strong        #CBD5E1
Text Primary         #0F172A
Text Secondary       #475569
Text Muted           #64748B
Text Disabled        #94A3B8

### Primary blue

Primary 50           #EFF6FF
Primary 100          #DBEAFE
Primary 200          #BFDBFE
Primary 300          #93C5FD
Primary 400          #60A5FA
Primary 500          #3B82F6
Primary 600          #2563EB
Primary 700          #1D4ED8
Primary 800          #1E40AF
Primary 900          #1E3A8A

Use Primary 600 or 700 for active navigation/actions.

Do not flood the entire interface with blue.

### Semantic status

Success              #16A34A
Success Background   #F0FDF4
Warning              #D97706
Warning Background   #FFFBEB
Danger               #DC2626
Danger Background    #FEF2F2
Info                  #2563EB
Info Background      #EFF6FF
Unknown               #64748B
Unknown Background    #F1F5F9

Use semantic color consistently across:
- health states;
- deployments;
- work-item states;
- alerts.

Never rely on color alone.

Also use:
- icon;
- text;
- badge;
- status label.

## 9. Typography / Density

Use the existing application font if appropriate.

Otherwise prefer a clean system/sans stack.

UI should support information density.

Guidelines:

Page title       24–30px
Section title    18–20px
Body             14px
Secondary        13px
Metadata         12px

Avoid oversized headings.

Tables and board cards should be compact but readable.

## 10. Design Tokens

Create reusable tokens for:

background
surface
surface-muted
border
border-strong
foreground
foreground-muted
primary
primary-hover
success
warning
danger
info
unknown

Also define:

radius-sm
radius-md
radius-lg
shadow-sm
shadow-md
spacing / layout constants

Do not scatter raw hex values throughout React components.

Document tokens in:

docs/design-system.md

## 11. Navigation

Keep the overall existing shell.

Move toward a project-scoped developer-platform navigation.

Example:

Logo / Product
--------------------------------
Projects

CURRENT PROJECT
Overview
Boards
Topology
Components
Deployments
Monitoring
Events
Environments
Settings
--------------------------------
User

Do not add navigation items that lead to empty placeholder pages.

A top-level project selector should remain accessible.

## 12. Real Project Management

Version 1 may rely heavily on seeded data.

Version 2 must support real user-managed projects.

Project entity should evolve to:

Project
------
Id
Name
Slug
Description
Visibility
CreatedByUserId
CreatedAt
UpdatedAt
ArchivedAt nullable

Visibility enum:

Private
Internal

Public is optional and should not be added unless required.

Rules:

- Name required.
- Slug unique.
- Slug normalized safely.
- Archived projects should not appear as active by default.
- Do not hard-delete projects with valuable history unless specifically designed.

Provide:

GET    /api/projects
GET    /api/projects/{id}
POST   /api/projects
PUT    /api/projects/{id}
POST   /api/projects/{id}/archive

Delete can be deferred.

## 13. Project Creation UX

Add:

/projects/new

Fields:

Name
Description
Slug

Optional:

Initialize with sample environment

Do not create fake components automatically unless user explicitly selects a template.

After creation redirect to:

/projects/{projectId}

Empty projects must have useful empty states.

Example:

No components yet.

Add your first system component to begin building the operational topology.

## 14. Preserve Demo Project

Keep:

Commerce Demo

as deterministic seed/demo data.

The demo must remain separate from user-created data.

Seed logic must be idempotent.

Where possible identify it by stable seed identifier/slug:

commerce-demo

## 15. User / Identity Model

Do NOT store passwords in the platform.

Identity comes from an external OIDC provider.

Create a local application user/profile record:

User
----
Id
ExternalSubject
Issuer
Email
DisplayName
AvatarUrl nullable
CreatedAt
LastLoginAt

The stable identity key is:

Issuer + ExternalSubject

Do not use email as the immutable identity key.

Email may change.

On successful authenticated access:

- resolve the external identity;
- create local User if missing;
- update relevant profile fields safely;
- update LastLoginAt.

## 16. OAuth 2.0 / OIDC Decision

Use a real external OpenID Connect provider.

Do not build an authorization server.

Default local-development provider:

Keycloak

Reasons:

- standards-based OIDC;
- self-hostable;
- Docker-friendly;
- realistic for a DevOps platform;
- replaceable later;
- avoids writing security-sensitive identity infrastructure.

The platform must not depend on Keycloak-specific APIs for ordinary login.

Authentication should depend on:

OIDC discovery
authorization endpoint
token endpoint
JWKS
issuer
audience
standard claims

This makes future providers possible:

Microsoft Entra ID
Auth0
Okta
other standards-compliant OIDC providers

## 17. Authentication Architecture

For the browser application, prefer a Backend-for-Frontend-oriented flow.

Do not store access tokens or refresh tokens in:

localStorage
sessionStorage
plain browser-readable cookies

Target conceptual flow:

Browser
   |
   | secure session cookie
   v
Next.js server / BFF
   |
   | Authorization Code + PKCE
   v
OIDC Provider (Keycloak)
   |
   | tokens kept server-side / protected
   v
Next.js server
   |
   | Bearer access token
   v
ASP.NET Core API

If the existing frontend architecture makes a full BFF impractical, use the safest practical standards-based alternative, document the tradeoff, and never expose long-lived tokens to insecure browser storage.

The preferred flow is:

Authorization Code
+
PKCE

Do not use:
- implicit flow;
- resource-owner password flow.

## 18. Local Keycloak Setup

Extend Docker Compose with Keycloak.

Suggested services:

postgres
keycloak
backend
frontend

Keycloak may use:
- its own database;
- or a separate PostgreSQL database/schema if configured cleanly.

Do not share application tables with Keycloak tables.

Create local-development realm configuration, preferably importable from a checked-in safe realm configuration without secrets.

Suggested:

Realm:
devops-platform

Client:
devops-web

Use environment variables for secrets.

Do not commit real client secrets.

Document:

Authority
ClientId
ClientSecret
Audience
Callback URL
Logout callback

## 19. Backend Authentication

ASP.NET Core API must validate bearer access tokens.

Configure:

Authority
Audience
Issuer validation
Signature validation
Lifetime validation

Use standard JWT bearer validation.

Do not manually parse JWT strings.

Protect normal platform APIs with authorization once authentication is enabled.

Allow:

/health/live

without authentication.

Decide separately whether readiness should be public.

Development-only simulation endpoints should require development mode and preferably authentication.

## 20. Frontend Authentication UX

Add:

/signin

or redirect directly to the configured OIDC provider.

Add user menu:

Display name
Email
Sign out

Unauthenticated project pages should not render protected data.

Support:

Sign in
Sign out
Session expiry
Unauthorized response handling

Do not build username/password forms.

## 21. Authorization Model

Keep Version 2 authorization simple.

Project membership:

ProjectMember
-------------
Id
ProjectId
UserId
Role
CreatedAt

Roles:

Owner
Contributor
Viewer

Suggested capabilities:

Owner:
- manage project
- manage members
- manage components
- manage environments
- manage dependencies
- manage work items
- manage deployments
- view monitoring

Contributor:
- manage components where allowed
- manage dependencies
- manage work items
- register deployments
- view monitoring

Viewer:
- read-only

Do not create dozens of fine-grained permissions.

## 22. Ownership Rules

Project creator becomes:

Owner

A project must always have at least one Owner.

For Version 2, member invitation can be simplified.

Possible implementation:

Add existing platform user by email

If the user does not yet exist locally:
- either reject with a clear message;
- or defer invitation support.

Do not implement email invitation workflows unless there is enough time.

## 23. Work-Item System

Add a project-scoped work-item model inspired by Azure Boards.

This should provide useful lifecycle management while remaining intentionally smaller.

Core entity:

WorkItem
--------
Id
ProjectId
SequenceNumber
Type
Title
Description
State
Priority
AssignedToUserId nullable
CreatedByUserId
ParentId nullable
StoryPoints nullable
OriginalEstimateHours nullable
RemainingWorkHours nullable
IterationId nullable
Area nullable
CreatedAt
UpdatedAt
ClosedAt nullable

## 24. Work-Item Types

Required:

UserStory
Task
Bug

Optional if implementation remains clean:

Feature
Epic

Do not add types merely to imitate Azure DevOps.

Suggested hierarchy:

Epic
  |
Feature
  |
User Story
  |
Task

Bug may exist independently or under a story/feature.

For MVP V2, only one ParentId is needed.

## 25. Work-Item States

Prefer a normalized small workflow.

Recommended common enum:

New
Active
Resolved
Closed
Removed

Board column configuration can map onto states later.

Do not create different incompatible workflows per type unless necessary.

## 26. Work-Item Priority

Enum:

Critical
High
Medium
Low

Persist as stable values.

Do not encode priority solely through colors.

## 27. Tags

Add:

Tag
---
Id
ProjectId
Name
ColorToken nullable

and many-to-many:

WorkItemTag
------------
WorkItemId
TagId

Tag names should be unique per project.

Use palette tokens rather than arbitrary unvalidated CSS values where practical.

## 28. Work-Item Comments

Add if achievable:

WorkItemComment
---------------
Id
WorkItemId
AuthorUserId
Body
CreatedAt
UpdatedAt nullable

Plain text or safely rendered Markdown is acceptable.

Do not support arbitrary unsanitized HTML.

Comments are useful but lower priority than working board CRUD.

## 29. Work-Item Activity History

Prefer an append-only activity record.

WorkItemActivity
----------------
Id
WorkItemId
ActorUserId nullable
Type
FieldName nullable
OldValue nullable
NewValue nullable
OccurredAt

Examples:

Created
StateChanged
Assigned
PriorityChanged
TitleChanged
CommentAdded
LinkedToComponent

Do not attempt perfect event sourcing.

This is audit/history data.

## 30. Work Items Linked to Operational Model

This is where the board should strengthen the project's unique idea.

Add optional relation:

WorkItemComponentLink
---------------------
WorkItemId
ComponentId

This allows:

Bug #42
"Payment failures after deployment"

Linked component:
Payment Service

Optional future relation:

WorkItemDeploymentLink

Implement component linking before deployment linking.

This creates useful integration between planning and runtime topology.

## 31. Iterations / Sprints

Add a lightweight iteration model.

Iteration
---------
Id
ProjectId
Name
StartDate nullable
EndDate nullable
State

States:

Planned
Current
Completed

Do not implement complex Scrum planning.

A work item may belong to one iteration.

Board can filter by:

All
Current iteration
Backlog

## 32. Work-Item APIs

Implement:

GET    /api/projects/{projectId}/work-items
GET    /api/work-items/{id}
POST   /api/projects/{projectId}/work-items
PUT    /api/work-items/{id}
PATCH  /api/work-items/{id}/state
PATCH  /api/work-items/{id}/assignment

Optional:

POST   /api/work-items/{id}/comments
POST   /api/work-items/{id}/tags/{tagId}
DELETE /api/work-items/{id}/tags/{tagId}

Support useful query parameters:

?type=Bug
?state=Active
?assignedTo=me
?iteration=current
?search=payment

Do not create dozens of specialized endpoints when query filtering is sufficient.

## 33. Board Page

Add:

/projects/[projectId]/boards

Primary Kanban columns:

New
Active
Resolved
Closed

Cards should show:

ID
Type
Title
Assignee
Priority
Tags
Story points / estimate when relevant
Linked component indicator

Drag-and-drop state changes are desirable.

If drag-and-drop introduces instability, provide explicit state actions first.

Persist every state change.

Do not make the board purely client-side.

## 34. Backlog / List View

Add either within Boards or as a sub-view:

Board | Backlog

Backlog should support:
- search;
- type filter;
- state filter;
- priority;
- assignment;
- iteration.

A compact table is sufficient.

## 35. Work-Item Detail View

Clicking a card opens a detail drawer or page.

Display:

Type
Title
Description
State
Priority
Assignee
Creator
Iteration
Story points
Estimates
Tags
Parent
Linked components
Comments
Activity/history
Created/updated dates

Keep interaction consistent with topology component side panels.

## 36. Project Overview V2

Enhance:

/projects/[projectId]

Suggested overview:

Project name
Current environment

Summary cards:
- Open work items
- Active bugs
- Components
- Unhealthy/degraded components
- Recent deployments

Panels:
- Board snapshot
- Runtime health snapshot
- Recent activity
- Recent deployments

This page should connect development activity with operational status.

## 37. Unified Activity Feed

Version 2 should introduce a project activity feed.

Possible sources:

Work item created
Work item closed
Deployment registered
Health changed
Component created
Dependency created
Dependency observed
Project updated

Do not force every subsystem into one giant event-sourced architecture.

A query service may merge:
- OperationalEvent;
- WorkItemActivity;
- relevant project events.

Return a normalized activity DTO for UI.

## 38. Operational Timeline Improvements

Keep the operational event timeline.

Enhance correlation around deployments.

For component details, show something like:

14:20 Deployment v2.4.7 completed
14:24 Error rate threshold exceeded
14:27 Health changed to Degraded
14:35 Bug #42 created and linked

Do not state:

deployment caused failure

unless causation is explicitly known.

Use:

occurred after
correlated in time

## 39. Topology V2

Preserve the topology screen.

Improve:

- search;
- component-type filters;
- health filters;
- dependency-source filters;
- project-created data support;
- better empty states;
- better detail panel;
- improved layout;
- impact-analysis presentation;
- linked active work items;
- related recent deployments;
- consistent palette.

Node details should include:

Name
Type
Health
Environment
Current version
Owner
Repository
Metrics
Dependencies
Dependents
Active linked work items
Recent deployments
Recent events

## 40. Dependency Source Support

Keep:

Declared
Discovered
Observed

Version 2 should add foundations for comparing them.

Where enough data exists, classify relationships for display as:

Declared only
Observed only
Declared + observed
Discovered

Do not invent runtime observations.

Observed edges must originate from actual ingestion/data.

## 41. Architecture Drift Foundation

Add a backend query/service capable of comparing:

declared dependencies
vs
observed dependencies

Possible result:

{
  "declaredOnly": [],
  "observedOnly": [],
  "matching": []
}

This may be a secondary Version 2 feature.

UI can show:

Potential architecture drift

Use careful wording.

Do not call every mismatch an error.

## 42. Environment Comparison Foundation

Support comparing two environments for:

component existence
deployed version
health state
declared dependencies

Endpoint suggestion:

GET /api/projects/{projectId}/environment-comparison
    ?left={environmentId}
    &right={environmentId}

Return structured differences.

UI may be a simple table.

Do not build a complex visual diff engine.

## 43. Deployment V2

Keep the Version 1 deployment entity.

Improve UI and APIs to support real project data.

Add optional fields if useful:

SourceBranch nullable
TriggeredByUserId nullable
ExternalBuildId nullable
Notes nullable

Do not add a complete pipeline engine.

Deployment history should connect to:
- component;
- environment;
- commit SHA;
- work items when linked;
- operational events.

## 44. Monitoring V2

Do not replace the existing monitoring architecture.

Improve:
- real project support;
- filtering;
- component detail;
- health history if available;
- clearer semantic status styles;
- runtime update reliability;
- SSE reconnection UX.

Prometheus/OpenTelemetry remain integration goals, not reasons to overbuild.

If V1 still uses simulated RuntimeState updates, preserve this for deterministic demo behavior.

## 45. Search

Add project-scoped search where practical.

Minimum search targets:

work items
components

Optional:

deployments
events

Do not build Elasticsearch for Version 2.

PostgreSQL ILIKE or equivalent is sufficient at diploma scale.

## 46. Empty States

All newly user-created projects begin mostly empty.

Every page must handle this intentionally.

Examples:

Board:
No work items yet.
Create your first User Story, Task, or Bug.

Topology:
No system components have been registered.
Add components to start building the topology.

Deployments:
No deployments recorded.

Avoid blank pages and null-reference failures.

## 47. Database V2 Entities

Existing:

Project
Environment
SystemComponent
Dependency
Deployment
RuntimeState
OperationalEvent

Add:

User
ProjectMember
WorkItem
Tag
WorkItemTag
WorkItemComment
WorkItemActivity
Iteration
WorkItemComponentLink

Optional later:

WorkItemDeploymentLink

Update migrations incrementally.

Do not squash Version 1 migrations unless there is a strong project reason.

## 48. Important Database Constraints

Add:

unique User on (Issuer, ExternalSubject)
unique Project.Slug
unique ProjectMember on (ProjectId, UserId)
unique Tag on (ProjectId, Name)
index WorkItem on (ProjectId, State)
index WorkItem on (ProjectId, Type)
index WorkItem on AssignedToUserId
index WorkItem on IterationId
index WorkItemActivity on (WorkItemId, OccurredAt)

Keep existing topology/runtime indexes.

## 49. Authorization Enforcement

Do not secure only the UI.

Backend must enforce project access.

Introduce a reusable project-access authorization mechanism.

Examples:

CanViewProject
CanContributeToProject
CanManageProject

Do not scatter raw role checks across every controller.

Keep the implementation simple and testable.

## 50. API Error Behavior

Retain standardized Problem Details.

Expected:

400 validation
401 unauthenticated
403 authenticated but not permitted
404 missing or inaccessible resource when appropriate
409 conflict
500 unexpected

Do not leak identity-provider details in errors.

## 51. Demo Data V2

Commerce Demo should include work-management data.

Seed example:

User Story:

US #101
As a customer, I want to place an order so that I can purchase products.
State: Active
Priority: High

Tasks:

TASK #102
Implement order validation
Active

TASK #103
Integrate Payment Service
Closed

Bug:

BUG #104
Payment failures after deployment
Active
Critical
Linked component: Payment Service

Feature optional:

FEATURE #100
Checkout improvements

Add tags:

backend
frontend
production
payments

Add sample activity.

## 52. Version 2 Demo Scenario

The diploma demo should support:

1. Sign in.
2. Open project list.
3. Show Commerce Demo.
4. Create a new project.
5. Open Commerce Demo.
6. Open Boards.
7. Create a Bug.
8. Assign it.
9. Move it from New -> Active.
10. Link it to Payment Service.
11. Open Topology.
12. See Payment Service.
13. Trigger simulated degradation.
14. Node changes live.
15. Operational event appears.
16. Open impact analysis.
17. Show potentially affected upstream components.
18. Open Payment Service details.
19. Show linked Bug.
20. Show deployment and health timeline.
21. Recover service.
22. Close/resolve Bug.

This demonstrates the core thesis:

development work
+
system architecture
+
runtime state
+
operational events

in one project context.

## 53. Frontend Structure

Keep existing frontend organization where reasonable.

Recommended conceptual areas:

src/
├─ app/
├─ components/
│  ├─ shell/
│  ├─ projects/
│  ├─ boards/
│  ├─ topology/
│  ├─ monitoring/
│  ├─ deployments/
│  └─ ui/
├─ features/
│  ├─ auth/
│  ├─ projects/
│  ├─ work-items/
│  ├─ topology/
│  ├─ deployments/
│  └─ runtime/
├─ lib/
│  ├─ api/
│  ├─ auth/
│  └─ utils/
└─ styles/

Do not reorganize working Version 1 code solely to match this exact structure.

## 54. Backend Structure

Keep Clean Architecture-inspired modular monolith.

Organize use cases by feature rather than giant generic service classes.

Conceptually:

Application/
├─ Projects/
├─ WorkItems/
├─ Components/
├─ Topology/
├─ Deployments/
├─ Runtime/
├─ Events/
└─ Identity/

Do not introduce a mediator library solely to achieve folder aesthetics.

## 55. Authentication Abstractions

Application code may use:

ICurrentUser

Example properties:

UserId
ExternalSubject
Email
DisplayName
IsAuthenticated

Infrastructure/API resolves these from authenticated claims.

Domain must not reference:
- Keycloak;
- JWT libraries;
- ASP.NET HttpContext.

## 56. Keycloak Is Replaceable

Do not write business logic like:

if provider == "keycloak"

for normal authentication behavior.

Provider-specific setup should be confined to:
- local Docker/dev configuration;
- auth configuration;
- documentation.

The rest of the app should depend on OIDC claims/identity abstractions.

## 57. Security Rules

Version 2 must enforce:

- no secrets committed;
- no JWT tokens logged;
- no access/refresh tokens in localStorage;
- HTTPS assumptions documented for non-local deployment;
- SameSite/Secure/HttpOnly session cookies where relevant;
- state/nonce validation handled by the OIDC library;
- PKCE enabled;
- issuer/audience validation;
- CORS restricted to configured origins;
- project authorization at backend level;
- safe Markdown rendering if comments/descriptions support Markdown.

Do not implement crypto manually.

## 58. Testing V2

Expand testing.

Unit:
- work-item state logic;
- work-item validation;
- project slug rules;
- project role checks;
- impact analysis;
- architecture-drift comparison.

Integration:
- create project;
- list only accessible projects;
- create work item;
- move work item state;
- assign work item;
- link work item to component;
- board query;
- authorization failures.

Authentication tests may use:
- mocked/test authentication scheme for API integration tests.

Do not require a live Keycloak instance for every unit/integration test.

Add a small real-provider smoke path where practical.

## 59. Board Acceptance Tests

At minimum verify:

Given a project member
When they create a Bug
Then it appears in New.

When state changes to Active
Then it appears in Active.

When assigned
Then assignee is persisted.

When linked to Payment Service
Then component detail includes the active work item.

When a Viewer attempts modification
Then API rejects the operation.

## 60. Project Acceptance Tests

Verify:

authenticated user can create project
creator becomes Owner
project persists
new project has no demo topology unless explicitly seeded
owner can add environment
owner/contributor can create component
viewer cannot modify project

## 61. Design-System Page

Optional but useful for development:

/dev/design-system

Development only.

Display:

colors
badges
buttons
inputs
cards
statuses
work-item types
health states
typography

Do not expose this route in production navigation.

## 62. Work-Item Visual Language

Use restrained type indicators.

Example:

User Story  blue icon/badge
Task        slate/blue-gray
Bug         red semantic accent
Feature     indigo or muted primary
Epic        violet only if implemented

Do not make every card highly saturated.

Priority can use:
- Critical: danger;
- High: warning/danger-muted;
- Medium: neutral;
- Low: muted.

## 63. Board Interaction

Preferred Version 2 UX:

+-------------------------------------------------------------+
| Boards | Backlog       Search       Iteration       + New   |
+-------------------------------------------------------------+
| NEW          ACTIVE          RESOLVED          CLOSED       |
|                                                             |
| [US #101]    [BUG #104]     [TASK #109]       [TASK #103] |
| ...          ...            ...               ...          |
+-------------------------------------------------------------+

Support:
- card click;
- quick state update;
- create button;
- filter;
- drag/drop if stable.

Do not build swimlanes/custom board editors yet.

## 64. Project Settings

Add:

/projects/[projectId]/settings

Support:

Name
Description
Slug where safe
Members
Archive project

Do not add dozens of configuration sections.

## 65. RUP / Master's Documentation Updates

Update documentation to reflect solo development.

Vision should explain:
- reduced team;
- unchanged project goals;
- Version 2 scope;
- prioritized value.

Update:

docs/vision.md
docs/architecture.md
docs/domain-model.md

Add:

docs/work-items.md
docs/authentication.md
docs/design-system.md

Update iteration planning.

## 66. Version 2 RUP Priorities

Treat Version 2 as a new iteration within Construction/Elaboration refinement.

Resolve highest technical risks early:

OIDC integration
project authorization
work-item persistence
board state management
project-created topology

Do not spend the first iteration only polishing CSS.

## 67. Suggested Solo Delivery Order

Implement in vertical slices.

V2.1 — Visual Foundation
- introduce palette/tokens;
- update shell/sidebar;
- preserve page layout;
- update status colors;
- clean up typography/spacing.

V2.2 — Real Projects
- project CRUD;
- project creation;
- empty states;
- project ownership;
- demo project coexistence.

V2.3 — Authentication
- Keycloak local service;
- OIDC flow;
- local User;
- CurrentUser;
- authenticated API;
- project membership;
- authorization.

V2.4 — Work Items
- WorkItem entity;
- migration;
- CRUD;
- types/states/priorities;
- board list APIs.

V2.5 — Boards
- board UI;
- create/edit drawer;
- assignment;
- state changes;
- tags;
- backlog/list filters.

V2.6 — Operational Integration
- link work item to component;
- show work item on component detail;
- unified activity;
- deployment/runtime correlations.

V2.7 — Topology Improvements
- filters;
- search;
- dependency source presentation;
- drift comparison foundation;
- environment comparison foundation.

V2.8 — Hardening
- tests;
- authorization checks;
- accessibility;
- responsive behavior;
- documentation;
- demo flow.

## 68. Version 2 Definition of Done

Version 2 is complete when:

1. application uses the neutral white/slate/blue palette;
2. color palette is documented;
3. user can sign in through configured OIDC provider;
4. browser does not store access/refresh tokens in localStorage;
5. API validates authenticated requests;
6. authenticated user can create a real project;
7. project creator becomes Owner;
8. project data persists;
9. Commerce Demo still exists;
10. real projects can create environments/components/dependencies;
11. board supports User Stories, Tasks, and Bugs;
12. board data persists;
13. work items can move between states;
14. work items can be assigned;
15. work items can have priority;
16. tags work if implemented in the selected slice;
17. work item can link to component;
18. linked active work items appear from component context;
19. topology works for user-created projects;
20. live runtime updates still work;
21. impact analysis still works;
22. deployment history still works;
23. project overview shows useful work + runtime data;
24. authorization prevents unauthorized modification;
25. backend tests pass;
26. frontend lint/build/type checks pass;
27. migrations apply from a clean database;
28. Docker Compose can bring up required local dependencies;
29. documentation matches implementation;
30. demo workflow is reproducible.

## 69. Migration Strategy

Before implementing:

1. inspect current Version 1 migrations;
2. do not delete them;
3. add additive Version 2 migrations;
4. preserve seeded demo identifiers;
5. verify migration from an existing V1 database;
6. verify migration from empty database;
7. ensure seed process remains idempotent.

Do not use EnsureCreated when migrations are the intended database strategy.

## 70. Existing Data Compatibility

Where V1 Project rows lack creator/member data:

Migration/seed strategy should assign ownership safely.

For local demo/development, an initial admin/demo user may become owner of Commerce Demo after first authenticated login or through controlled seed configuration.

Document the chosen approach.

Do not fabricate production identities.

## 71. Observability of the Platform

Add basic structured logging if not already present.

Useful log contexts:

ProjectId
ComponentId
WorkItemId
EnvironmentId
DeploymentId
UserId

Do not log:
- tokens;
- secrets;
- full authorization headers.

## 72. API Versioning

Do not introduce complicated API versioning unless existing contracts already require it.

Version 2 of the product does not automatically mean:

/api/v2/*

Preserve current APIs where compatible.

Evolve contracts deliberately.

## 73. Performance Targets

Reasonable diploma targets:

project overview:
< 2 sec typical local/dev dataset

board:
< 2 sec for <= 500 work items

topology:
< 3 sec for <= 200 nodes

normal API queries:
< 1 sec typical local/dev dataset

live health update visible:
within ~5 sec under normal demo conditions

These are engineering targets, not guaranteed production SLAs.

## 74. Accessibility

At minimum:

- keyboard-focusable controls;
- meaningful button labels;
- semantic status text;
- sufficient contrast;
- color not sole state indicator;
- board cards usable without relying exclusively on drag/drop;
- form errors associated with fields.

## 75. Responsive Behavior

Primary target:

desktop developer workstation

Support reasonable tablet/smaller desktop layouts.

Do not spend significant time optimizing a complex topology graph for phones.

Mobile can be limited but must not catastrophically break.

## 76. ADRs

Add ADRs:

0006-use-external-oidc-provider.md
0007-use-keycloak-for-local-development.md
0008-use-bff-oriented-authentication.md
0009-add-project-work-items.md
0010-link-work-items-to-components.md
0011-preserve-modular-monolith-for-v2.md

Each ADR:

Context
Decision
Alternatives considered
Consequences

Keep concise.

## 77. Authentication ADR Guidance

ADR should explicitly explain:

Why not custom IdP:
- security-sensitive;
- large scope;
- not core research contribution;
- standards already exist;
- solo developer constraint.

Why Keycloak:
- OIDC standards;
- self-hosting;
- Docker support;
- provider replaceability;
- local deterministic demo.

Why BFF-oriented flow:
- avoid browser token storage;
- centralized authentication handling;
- secure cookies;
- clean API boundary.

## 78. Future Authentication Providers

Configuration should allow:

OIDC__Authority
OIDC__ClientId
OIDC__ClientSecret
OIDC__Audience

or equivalent.

Do not use hard-coded realm URLs throughout the codebase.

## 79. Future Azure DevOps / Git Integration

Version 2 Board is internal.

Do not implement Azure DevOps synchronization yet.

However, use stable WorkItem IDs and clear boundaries so future integrations can map external work items when needed later.

Do not add speculative columns unless required.

## 80. Future Telemetry Provider

Preserve existing abstraction direction for:

ITelemetryProvider

but do not force all current runtime-state operations through a needlessly complex plugin framework.

The current direct API ingestion remains valid.

## 81. Data Ownership Principle

A project owns:

Environments
Components
Dependencies
Deployments
OperationalEvents
Iterations
WorkItems
Tags

Project membership controls access.

A user may belong to multiple projects.

All project-scoped queries must prevent cross-project data leakage.

## 82. Delete Behavior

Prefer archive/soft-state transitions for valuable lifecycle records.

Examples:

Project -> Archived
WorkItem -> Removed/Closed

For low-level mutable configuration entities, carefully defined deletes are acceptable.

Do not cascade-delete entire operational history accidentally.

## 83. Work-Item Numbering

Provide human-friendly project-scoped or global numeric display IDs.

Example:

BUG #104
TASK #105

Database primary key may remain Guid.

Possible field:

SequenceNumber long

Use a safe database-backed mechanism.

Do not derive numbers from collection counts.

If project-scoped sequencing adds unnecessary complexity, use a globally increasing number.

## 84. Work-Item Description

Description may support Markdown.

If implemented:

- render safely;
- sanitize;
- do not allow arbitrary HTML execution.

Plain text is acceptable for initial Version 2.

## 85. Board State Updates

State changes must:

1. validate permission;
2. validate target state;
3. update WorkItem;
4. set ClosedAt when appropriate;
5. append WorkItemActivity;
6. save atomically.

Use a database transaction when multiple writes must remain consistent.

## 86. User Story / Bug / Task Creation

Create form should ask only useful fields initially:

Type
Title
Description
Priority
Assignee
Iteration
Tags
Linked component

Do not force every possible field.

Advanced fields can appear in details.

## 87. Current User Convenience

Support:

Assigned to me
Created by me

through query filtering.

Do not hard-code the solo developer assumption into domain logic.

The architecture should still support multiple users.

## 88. Activity Page

Optional route:

/projects/[projectId]/activity

If not added, show activity on Overview.

Normalize activity cards:

time
actor
event type
target
summary

Examples:

Viktor moved BUG #104 to Active
Payment Service became Degraded
Deployment v1.4.0 completed

## 89. Component Detail V2

Enhance component view/drawer with sections:

Overview
Dependencies
Deployments
Runtime
Work Items
Events

Avoid separate routes for every tiny subsection unless existing routing favors it.

## 90. Topology Impact Analysis V2

Preserve reverse traversal.

Improve response metadata:

{
  "sourceComponent": {},
  "directDependents": [],
  "indirectDependents": [],
  "affectedPaths": [],
  "summary": {
    "directCount": 1,
    "indirectCount": 2
  }
}

Do not assign arbitrary risk scores.

Use:

potentially affected

## 91. Architecture Drift Endpoint

Suggested:

GET /api/projects/{projectId}/environments/{environmentId}/architecture-drift

Return:

matching
declaredOnly
observedOnly

Do not implement auto-remediation.

## 92. Environment Comparison Endpoint

Suggested:

GET /api/projects/{projectId}/environment-comparison
?leftEnvironmentId=...
&rightEnvironmentId=...

Compare:

components
versions
health
dependencies

Return structured differences.

UI can be secondary.

## 93. Version 2 Dashboard Metrics

Project dashboard can show:

Open Work Items
Active Bugs
Components
Healthy / Degraded / Unhealthy
Deployments in last 7 days
Recent Activity

Do not add vanity metrics without user value.

## 94. Suggested Seed Board

Commerce Demo:

Feature #100
Checkout Reliability

User Story #101
As a customer I want to place an order

Task #102
Implement order validation

Task #103
Integrate payment service

Bug #104
Payment failures after deployment

Relations:

Feature #100
  -> User Story #101
       -> Task #102
       -> Task #103

Link:

Bug #104 -> Payment Service

## 95. Suggested Version 2 Pages

Required:

/projects
/projects/new
/projects/[projectId]
/projects/[projectId]/boards
/projects/[projectId]/topology
/projects/[projectId]/components
/projects/[projectId]/deployments
/projects/[projectId]/events
/projects/[projectId]/environments
/projects/[projectId]/settings

Optional:

/projects/[projectId]/activity
/projects/[projectId]/monitoring

Do not create redundant pages if topology already satisfies monitoring needs.

## 96. Code Quality

Continue Version 1 rules.

C#:

nullable enabled
CancellationToken propagated
ProblemDetails
async all the way
no .Result/.Wait()
clear DTOs
domain independent of infrastructure

TypeScript:

strict
no arbitrary any
typed API layer
shared reusable UI
no business logic buried in JSX

## 97. Dependency Policy

Before adding a package:

1. confirm it solves a real problem;
2. prefer actively maintained libraries;
3. avoid packages for trivial helpers;
4. pin sensible versions through normal package management;
5. do not add overlapping state-management libraries.

For board drag-and-drop, use a maintained library only if current project dependencies do not already provide a good solution.

## 98. README V2

Update README with:

Version 2 features
architecture
authentication setup
Keycloak local setup
project creation
board usage
topology demo
database migration
tests
Docker Compose
demo credentials if development realm intentionally provides them

Never include real secrets.

## 99. docs/authentication.md

Explain:

OIDC vs OAuth 2.0 roles
chosen provider
Authorization Code + PKCE
BFF-oriented flow
browser session
backend JWT validation
logout
local User provisioning
project authorization
provider replacement

Include Mermaid sequence diagram.

## 100. docs/work-items.md

Document:

work-item types
states
priority
hierarchy
assignment
tags
iterations
component linking
board behavior
non-goals

## 101. docs/design-system.md

Document:

palette
semantic colors
typography
spacing
status representation
board-card style
topology-node style

This document should match implemented CSS tokens.

## 102. Sign-Out

Support coordinated logout where provider supports it.

At minimum:

clear local application session
redirect through provider logout/end-session flow
return to application

Do not merely hide frontend UI while server session remains active.

## 103. Token Refresh

If refresh tokens are used:

- keep them server-side/protected;
- rotate/respect provider behavior;
- do not expose them to browser JavaScript.

If the selected framework abstracts refresh/session renewal safely, use it.

Document actual behavior.

## 104. Failure States

Handle:

OIDC provider unavailable
session expired
invalid token
user authenticated but no project permission
backend unavailable
SSE disconnected
empty board
empty topology
migration failure

UI should fail gracefully.

## 105. Development Mode

Development setup should be easy.

One command should ideally bring up:

PostgreSQL
Keycloak
Backend
Frontend

via:

docker compose up --build

If frontend/backend developers prefer local processes, document that path too.

## 106. Keycloak Bootstrap

Provide a deterministic development realm import if practical.

The realm may include:

devops-platform realm
devops-web client
one development user
standard scopes

Development credentials may be documented only if clearly local/demo credentials.

Do not reuse them outside development.

## 107. Solo Project Workflow

Because one person now owns the project:

- keep issues/work items small;
- implement vertical slices;
- require tests for risky logic;
- avoid weeks of infrastructure work;
- preserve a working main;
- use feature branches when useful;
- continuously update documentation.

The platform's own Board may eventually be used to manage development of the platform itself.

This is a useful demonstration, but not a requirement for bootstrap.

## 108. Suggested Internal Iterations

Iteration 1
Visual refresh + real projects.

Iteration 2
OIDC authentication + project membership.

Iteration 3
Work-item backend + migrations.

Iteration 4
Board frontend.

Iteration 5
Work-item/component integration + activity.

Iteration 6
Topology/runtime improvements.

Iteration 7
Drift/environment comparison foundations.

Iteration 8
Testing/documentation/demo hardening.

Keep the application runnable after every iteration.

## 109. Version 2 Quality Gate

Before finishing:

Backend:
dotnet restore
dotnet build
dotnet test

Frontend:
npm install
npm run lint
npm run build

or equivalent package-manager commands.

Database:
- migrations apply successfully from empty database;
- migrations apply successfully from Version 1 state where reproducible.

Docker:
docker compose config
docker compose up --build

Authentication:
- sign in works;
- protected API works;
- unauthenticated API fails correctly;
- logout works;
- project authorization works.

Board:
- create;
- read;
- update;
- state change;
- assignment;
- persistence.

Topology:
- demo project still renders;
- real project renders;
- live state still updates;
- impact analysis still works.

Do not claim a check passed unless it was actually executed.

## 110. Bootstrap Agent Execution Instructions

When given this file:

1. Read the existing repository first.
2. Read the original AGENTS.md.
3. Treat this file as Version 2 requirements that override conflicting Version 1 product requirements.
4. Preserve working Version 1 behavior unless explicitly replaced.
5. Inspect existing migrations before changing entities.
6. Inspect frontend design before changing colors/layout.
7. Implement in vertical slices.
8. Keep the app buildable.
9. Commit coherent changes if permitted.
10. Do not rewrite functioning modules without a concrete benefit.
11. Do not silently remove features.
12. Update documentation to match reality.
13. Record significant architectural decisions in ADRs.
14. Prefer complete smaller features over many partial placeholders.

## 111. First Version 2 Deliverable

The first V2 vertical slice should be:

Visual tokens
   ->
Real project creation
   ->
Authenticated user
   ->
Project ownership
   ->
Create work item
   ->
Board display

Then:

Work item
   ->
Linked component
   ->
Topology detail
   ->
Operational event context

This validates the Version 2 thesis:

A developer should be able to move naturally between planning work and the live operational system that work affects.

## 112. Final Product Principle

Do not optimize the product for the number of features.

Optimize for the coherence of this workflow:

Plan work
   |
   v
Implement / deploy
   |
   v
Observe runtime system
   |
   v
Understand dependencies
   |
   v
Identify impact
   |
   v
Connect incident/change back to work

That connection is what differentiates this Master's project from:

another Kanban board
another monitoring dashboard
another service graph
another CRUD project manager

Version 2 should make those capabilities feel like one system, not four unrelated modules.
