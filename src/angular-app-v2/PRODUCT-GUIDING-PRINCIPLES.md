# Product Guiding Principles

**Project:** Transflo Integration Configurator (`src/angular-app-v2/`)
**Status:** Active prototype — v2 of the field-mapping app
**Reference:** Architecture diagrams in [FigJam — Integration Configuration](https://www.figma.com/board/JjOP6pBBp4C85LxaY7t9oB/Integration-Configuration)
**Design system:** [github.com/jakesthekidd/transflo-design-system](https://github.com/jakesthekidd/transflo-design-system) (PrimeNG 21 + TransfloTheme preset)
**Design decisions:**
- [`DESIGN-DECISIONS-2025-05-13.md`](./DESIGN-DECISIONS-2025-05-13.md) — PM + lead dev review
- [`DESIGN-DECISIONS-2025-05-19.md`](./DESIGN-DECISIONS-2025-05-19.md) — wider team review (Mohammed, Allison, Scott)
- [`DESIGN-DECISIONS-2025-05-21.md`](./DESIGN-DECISIONS-2025-05-21.md) — PM sprint planning review

This document is the **canonical reference** for what we're building, why, and the shape it has to take. It captures decisions made across the v2 design conversations so future work doesn't drift.

---

## 1. Problem statement

Transflo's customers run on a fragmented set of external systems (McLeod v22, McLeod v23, SAP S/4 HANA, NetSuite, custom webhook endpoints, SFTP file drops). Every customer needs their data mapped into Transflo's products (WorkflowAI, Mobile, LTL Nav) in a bespoke way.

The legacy product (`src/angular-app/`) collapses two very different jobs into one tabbed admin UI:

1. **Building the integration library** — defining what a "McLeod v23 Import Orders" mapping even looks like. This is engineering work, infrequent, high-risk.
2. **Onboarding a customer** — taking the existing library pieces and binding them to a specific customer with credentials, sync settings, and customizations. This is Professional Services work, daily, lower-risk per change.

Mixing both in one surface causes:
- PS team accidentally edits the master library (production-impacting changes nobody asked for)
- Engineers waste time reviewing customer-specific deviations as if they were library changes
- New customer onboarding is improvised — no guided flow, no validation gates, no clear "is this thing live yet?"
- Templates and customer customizations get tangled (no clean fork/merge model)

**v2 solves this by separating the two audiences into two distinct apps**, sharing the same chrome and underlying data model but with different navigation, primary actions, and edit rights.

---

## 2. Personas & primary use cases

### Persona A — Engineering (builds the library)
**Touchpoint:** Integration Library app
**Cadence:** Weekly–monthly
**Goals:**
- Register a new Connection (e.g., "McLeod v23") when a new system or version comes online
- Author Master Mapping Templates that PS can later fork per customer
- Maintain Lookup Tables (status codes, equipment codes) scoped to a Connection
- Manage API Clients (the `x-client-id` identities runtime callers use)
- Maintain the Partners list (populated by engineering; selected by PS during connection setup)

### Persona B — Professional Services / Customer Success (deploys)
**Touchpoint:** Customer Setup app
**Cadence:** Daily–weekly per active onboarding
**Goals:**
- See the full list of customers they're actively managing (manually added, not auto-populated)
- Add a new customer from the existing portal records when ready to configure them
- For each customer: configure per-capability deployments through the tree-view editor
- Fork a Master Mapping Template for a customer, tweak fields, test against a real order
- Publish → Activate (or rollback) a Deployment

These two personas **share the same data model** (Connections, Templates, Lookups, Customers) but operate on it differently. The library is read-only-by-default for PS; customer deployments are read-only-by-default for engineering.

### Concrete use case — ABC Carrier (from the FigJam example)

ABC Carrier is one customer with **three deployments**:

| # | Application × Capability | Connection | Partner | Mapping | Status |
|---|---|---|---|---|---|
| 1 | WorkflowAI / Import Orders | McLeod v22 | Acme Logistics | McLeod v22 → WorkflowAI (forked + customized) | Active |
| 2 | WorkflowAI / Export Documents | SAP S/4 HANA | Acme Logistics | WorkflowAI → SAP (forked + customized) | Active |
| 3 | WorkflowAI / Webhook Handler | Webhook Receiver | Globex Transport | WorkflowAI events → ABC format (forked + customized) | Draft |

Each deployment is independent: status, credentials, partner, customizations all per-row. The library entities (McLeod v22 Connection, the WorkflowAI/Import Orders MasterTemplate) are shared across many customers, but each customer's instance is a detached fork.

---

## 3. Architecture overview

```
TRANSFLO Header Toolbar (white, 50px)
  ├── Logo (left)
  └── Right: page link · 9-dot app switcher · avatar
     └── App switcher dropdown ──┐
                                 │
                                 ▼
     ┌───────────────────────┐  ┌───────────────────────┐
     │  Integration Library  │  │   Customer Setup      │
     │  (engineering)        │  │   (professional       │
     │                       │  │    services)          │
     │  /admin               │  │   /customers          │
     └───────────────────────┘  └───────────────────────┘
              │                            │
              ▼                            ▼
     Navy stage banner             Navy stage banner
     Sub-tabs in banner:           Customer list → Customer Detail
       Applications & Capabilities (tree view: apps → capabilities;
       Connections                  click any capability to edit its
       Mapping Templates            Connection, Mapping, Test & Publish,
       Lookup Tables                Activity in the right pane)
       Logs
```

Both apps live behind the same TRANSFLO chrome. The **app switcher** in the header is the only top-level nav. There is **no global left rail** — the per-customer left rail is scoped to the Customer Detail screen.

---

## 4. Domain model

| Entity | Owner | Scope | Notes |
|---|---|---|---|
| **Application** | Engineering | Top-level product | Hard-coded catalog: `WorkflowAI`, `Mobile`, `LTL Nav`. Read-only in UI. |
| **Capability** | Engineering | Owned by Application | Has direction (`Inbound` / `Outbound` / `Bidirectional`). E.g., WorkflowAI → Import Orders (Inbound), Export Documents (Outbound). |
| **Connection** | Engineering | Versioned adapter to an external system | e.g., `McLeod v22`, `McLeod v23`, `SAP S/4 HANA`, `NetSuite`, `Webhook Receiver`, `SFTP`. Defines credential schema, base URL, and auth method type. Previously called "TMS System" — renamed May 13. Full CRUD needed (currently delete-only). |
| ~~**Partner**~~ | ~~Engineering~~ | ~~External business partner~~ | ~~Removed May 19~~ — confirmed unnecessary after AJ consultation. Partner is not a data point needed at the connection level. |
| **Lookup Table** | Engineering | Scoped to a Connection | Key→value dictionaries (status codes, equipment types, doc categories). |
| **Mapping Template (Master)** | Engineering | Scoped to (Application, Capability, Connection) | Default field translations. Authored once, forked many times. |
| **API Client** | Engineering | Runtime identity | The `x-client-id` value runtime callers use. Has assigned MasterTemplate versions. |
| **Customer** | Synced from portal | Tenant | Manually activated in v2 by PS from the portal records dropdown. Not auto-populated. |
| **CustomerConnection** | PS | (Customer, Connection, Partner) | Holds per-customer credentials, tokens, URLs, and partner selection. |
| **CustomerTemplate** | PS | Forked from a MasterTemplate, attached to a Customer | Detached editable copy. Edits don't propagate to or from the master. |
| **Deployment** | PS | (Customer, Application, Capability, CustomerConnection, CustomerTemplate, Status) | The activation bundle. One Active per (Customer, App, Capability). |

### Lifecycle (Deployment status)

```
Draft  →  Tested  →  Published  →  Active  →  Retired
```

**Invariant:** Only one Deployment is `Active` per `(Customer, Application, Capability)` tuple. Activating a new one drains the prior Active to `Retired`.

**Publish vs. Activate distinction (confirmed May 13):**
- **Publish** snapshots the current Connection + Mapping into a versioned record. Does NOT make it live. Multiple published (inactive) versions can exist at once.
- **Activate** selects a published version and makes it live. Retires any prior Active.
- Published-but-inactive versions stay visible in the Mapping tab's version list so PS can reference prior configs.

---

## 5. The two apps

### App 1 — Integration Library  (`/admin`)
**Persona:** Engineering
**Purpose:** Build and maintain the reusable library that PS will deploy to customers.

Sub-tabs (in the navy stage banner):
1. **Applications & Capabilities** — read-only catalog browser
2. **Connections** — list + editor for McLeod v22, SAP S/4, etc. Table includes Application, Capability, and Total Active columns. *(formerly "TMS Systems" — renamed May 13)*
3. **Mapping Templates** — list + editor, organized as `Application × Capability × Connection`
4. **Lookup Tables** — list + editor, scoped per Connection
5. **Customers** — super admin tab for adding customers from portal picklist *(new — May 21)*
6. **Logs** — runtime transformation telemetry

> **Removed May 13:** The "Integrations" tab has been eliminated.
> **Removed May 13/19:** The "Partners" standalone management tab is removed. Partner field also removed from Connection tab (May 19).
> **Auth (May 21):** Dev side is password-protected for super admins. Standard PSG users get pass-through.

### App 2 — Customer Setup  (`/customers`)
**Persona:** Professional Services
**Purpose:** Manage every customer's deployments through a hierarchy-aware editor.

**No wizard.** The product treats each customer as a tree of deployments and lets PS edit any deployment's aspect (Connection, Mapping, Test & Publish, Activity) directly.

Surfaces:
- **Customer list** (`/customers`) — table of manually activated customers. New customers are added via "Add Customer" button + portal records dropdown, not auto-populated.
- **Customer Detail** (`/customers/:id`) — the tree view (see §6 below). This is the workhorse screen.

---

## 6. Customer Detail — the tree-view + tabs pattern

The single screen where every per-customer integration work happens.

### Layout

```
NAVY STAGE: Customers / Acme Logistics

┌────────────────────────────┬──────────────────────────────────────┐
│ LEFT RAIL (~280px, sticky) │ RIGHT PANE                           │
│                            │                                      │
│ APPLICATIONS               │ Title:   <Capability name>           │
│   ▼ WorkflowAI       (3)   │ Subtitle: <App> · <Connection> · <Status> │
│      ● Import Orders Active│ ─────────────────────────────────    │
│      ● Export Docs   Active│                                      │
│      ● Webhook       Draft │ ┌── Tabs ────────────────────────┐  │
│      + Add capability      │ │ Connection · Mapping ·          │  │
│                            │ │ Test & Publish · Activity       │  │
│   ▼ Mobile           (1)   │ └─────────────────────────────────┘  │
│      ● POD Upload  Tested  │                                      │
│      + Add capability      │   [active tab content]               │
│                            │                                      │
│   ▶ LTL Nav          (0)   │              [ Save changes ]        │
│                            │                                      │
│   + Add application        │                                      │
└────────────────────────────┴──────────────────────────────────────┘
```

### Rail
- Top section = **Applications** that have at least one deployed Capability for this customer
- Each Application is a collapsible group with its capability count
- Each Capability is a clickable node with a **status pill** (Draft / Tested / Published / Active / Retired)
- `+ Add capability` button at the leaf level of each Application
- `+ Add application` button at the root
- Single-select; the URL carries `?cap=<deploymentId>` so refresh and deep-link work

### Empty state
A new customer with zero deployments shows a centered empty pane in the right side:
> **No integrations yet**
> Add your customer's first integration to get started.
> `+ Add your first integration`

### Per-capability tabs

Each tab is **independently savable** — partial progress is fine. A deployment stays in `Draft` until the user explicitly Publishes it. PS can save the Connection today, leave, and fill the Mapping tomorrow.

| Tab | Owns | Persists to |
|---|---|---|
| **Connection** | Connection picker + customer credentials (auth method varies by type) + **Test Authentication** button | `CustomerConnection` |
| **Mapping** | Fork-master-template picker + field-mapping table + lookup table reference + versioned published-mappings list | `CustomerTemplate` |
| **Publish and Activate** *(formerly "Test & Publish" — renamed May 21)* | Publish / Activate / Retire / Rollback / Reactivate + "Send to master templates" option | Mutates `Deployment.status` and `snapshotVersion` |
| **Activity** | Read-only transformation log filtered to this `deploymentId` | — |

### Versioned Mappings List (new — May 13)
The Mapping tab shows a grid of all published (but potentially inactive) versions before the Activate action. Columns: version number, publish date, published by. This allows PS to reference prior configs and promotes a clear paper trail before going live.

### Connection tab auth methods (expanded — May 19/21)
The Connection tab credentials form expands auth options modeled after Postman: API Key, Bearer Token, Basic Auth, OAuth 2.0. The connection type definition drives which auth fields are required.

A **Test Authentication** button appears after the credentials form — validates credentials against the live system endpoint and returns an inline pass/fail. This replaces the test runner that was previously on the Test & Publish tab.

> ~~**Partner dropdown (May 13):**~~ Removed May 19 — confirmed unnecessary after AJ consultation.

### Deployment ⇄ tab semantics

- **You cannot Activate** until Connection + Mapping have at least their minimum required data
- **You can Save** any tab at any time, regardless of others' state
- **Status transitions** happen ONLY in the Test & Publish tab; the rail status pill reflects what's persisted
- Activating retires any prior Active for the same `(customer, application, capability)` tuple (the §4 invariant)

### Add Application / Add Capability flows

Both use a small `<p-dialog>`. Context is implicit from where the `+` was clicked.

**+ Add application** (rail root): Pick from apps not yet deployed for this customer → new node, select `+ Add capability` next.

**+ Add capability** (rail under an app): Pick from capabilities not yet deployed → new node, Connection tab opens, status = `Draft`.

---

## 7. Design system contract

### Chrome — non-negotiable
All chrome comes from `src/app/design-system/`:

- **HeaderToolbar** — white 50px top bar with TRANSFLO logo, page link, app switcher dropdown (9-dot grid), avatar.
- **StageLayout** — fixed-height navy banner + overlapping white card.
  - **Navy: 163px fixed.**
  - **White card overlap:** −99px when no nav, −59px when tabs/breadcrumbs are present. Sourced from Figma frame `75:1876`.
- **SideNav** — hidden (`[showSideNav]="false"`); not used in current designs.

### Type scale — 4 sizes only
| Token | Size | Use |
|---|---|---|
| `--tf-text-display` | 18px / 700 | Section title in `.tf-section-header` |
| `--tf-text-heading` | 15px / 700 | Card title, modal title |
| `--tf-text-body` | 13px / 400 (labels 600) | Default everywhere |
| `--tf-text-meta` | 11px / 600 | Tags, table headers, helper text |

### Spacing scale
`--tf-space-1..6` = `4 / 8 / 12 / 16 / 20 / 24` (px). No 10, 14, 15.

### Color
Brand blue (`--tf-blue-500: #2474BB`) is the primary. Status uses PrimeNG severity:
- Active → `success` | Inactive → `secondary` | Draft → `warn` | Failed → `danger`
- Inbound → `info` | Outbound → `warn` | Bidirectional → `contrast`

### PrimeNG-first
- `<p-button size="small">` for all buttons
- `<p-table p-datatable-sm>` for all tables
- `<p-tag>` for all status/category pills
- `<p-toast>` and `<p-confirmdialog>` mounted once in AppComponent
- `<p-dialog>` for inline pickers
- PrimeIcons (`pi pi-*`) for all icons

---

## 8. What's built in v2

| Surface | Status |
|---|---|
| HeaderToolbar with app switcher | ✓ |
| StageLayout with fixed-height navy + variable overlap | ✓ Matches Figma `75:1876` |
| AppComponent — derives apps, active tab, tab change → URL | ✓ |
| AdminShell with sub-tabs (Applications, Connections, Templates, Lookup Tables, Logs) | ✓ |
| Applications & Capabilities read-only catalog | ✓ |
| Customer list with `<p-table>`, `<p-tag>`, `<p-menu>` | ✓ |
| Customer Detail — tree-view (left rail + right pane) | ✓ |
| Connection tab (connection picker + partner dropdown + credential schema form) | ✓ |
| Mapping tab (template picker, field-mapping table, JSON test panel) | ✓ |
| Test & Publish tab (test runner, lifecycle action cards) | ✓ |
| Activity tab (transformation log table) | ✓ |
| AddDeploymentDialog (2-step for + Add application, 1-step for + Add capability) | ✓ |
| Mock API interceptor with in-memory persistence for Deployments | ✓ |
| GeneralService → PrimeNG ConfirmationService + MessageService | ✓ |
| Design tokens — `--tf-text-*`, `--tf-space-*`, `--tf-radius-*` | ✓ |
| Terminology: "Connections" throughout (TMS Systems removed) | ✓ May 13 |
| Integrations tab removed | ✓ May 13 |
| Partners standalone management removed | ✓ May 13 |
| ~~Partner dropdown on Connection tab~~ | ⚠ Needs removal — reversed May 19 (AJ confirmed unnecessary) |

## What's coming next (MVP backlog — updated May 21)

### Code cleanup (immediate)
- [ ] **Push partner field removal** — done locally, needs commit + push
- [ ] **Rename "Test and Publish" → "Publish and Activate"** — tab label, component, route param
- [ ] **Remove test runner section** from Publish and Activate tab
- [ ] **Remove Export + Webhook Handler** from capability picker (Import Orders only for MVP)
- [ ] **Remove Tonu code column** from customer list

### Connection tab
- [ ] **Test Authentication button** — inline credential validator after credentials form
- [ ] **Auth method options** — model after Postman (API Key, Bearer, Basic, OAuth 2.0)
- [ ] **Base URL field** on connection definition

### Mapping tab
- [ ] **Lookup table reference panel** — surface relevant lookup tables for the active connection
- [ ] **Versioned mappings list** — published version history grid before Activate

### Publish and Activate tab
- [ ] **"Send to master templates"** — promote successful activated config back to master template library

### Integration Library (dev side)
- [ ] **Connections table** — add Application, Capability, Total Active columns; Total Active click-through to customer list
- [ ] **Connection Create + Edit** — currently delete-only; needs full CRUD
- [ ] **Customers tab** — super admin search + picklist from portal for adding customers to the system
- [ ] **Password protection** — gate dev side behind password for super admins; pass-through for standard PSG

### Backend integrations (Mohammed's work)
- [ ] Portal GraphQL integration for customer data
- [ ] Portal authentication / SSO
- [ ] Super admin customer management

## What's out of scope until Mohammed's architecture session

- Tenant modeling in the backend
- One deployed app vs two separate surfaces
- Import Orders → Export Documents → Webhooks rollout sequence (Import Orders is MVP focus)
- Visual Connections page in Workflow AI settings (Phase 3)
- Real backend / auth / multi-tenant scoping

---

## 9. Conventions for future work

- **Stage chrome is sacred.** Do not bypass `<app-stage-layout>` for any screen.
- **Two apps, one data model.** Build in the shared mock/service layer; surface differently per shell.
- **Wizard never edits the library.** PS flows produce CustomerTemplates / CustomerConnections / Deployments — never modify library entities.
- **Forks are detached.** Editing a CustomerTemplate must not affect its MasterTemplate origin.
- **One Active per (Customer, App, Capability).** Activating retires the prior Active. Surface this prominently.
- ~~**Partners are engineering-managed.**~~ Partner field removed May 19 — not needed at connection level.
- **One connection = one capability.** A TMS may have multiple connection records — one per application/capability pair (e.g., Truckmate has separate connections for WorkflowAI/Import Orders and Mobile/Import Orders).
- **Test Authentication lives on the Connection tab.** The Publish and Activate tab is lifecycle-only — no test runner.
- **Import Orders first.** Export Documents and Webhook Handler are excluded from the customer capability picker for MVP.
- **Publish first, Activate second.** Never let PS skip directly to Active. The two-step workflow is non-negotiable.
- **PrimeNG before custom.** New surfaces import from `primeng/*`. Custom components use design tokens.
- **Sync the design system, don't fork it.** Use the `transflo-design-system-sync` skill; never rewrite ported components from scratch.

---

## 10. Open questions

| # | Question | Owner | Status |
|---|---|---|---|
| 1 | What specific fields are needed in the Test & Publish section? | Jake + Brian | Open |
| 2 | Portal filter criteria — which customers appear in the add-customer picklist? | Allison / Portal team | Open |
| 3 | Redirect vs. embedded experience — does portal launch Integration Configurator in a new tab or iframe? | Scott + Mohammed | Open |
| 4 | Authentication flow — SSO from portal session or separate login? | Mohammed + Portal team | Open |
| 5 | Base URL — per-connection-type or per-customer (can customers override)? | Mohammed + Jake | Open |
| 6 | How are tenants defined — by environment, business unit, or both? | Mohammed session | Deferred |
| 7 | One app or two deployed surfaces? | Mohammed + Scott | Deferred |

### Resolved
- ~~Wizard or no wizard?~~ → **No wizard.** Tree-view + per-capability tabs.
- ~~Templates editor pattern (PS side)~~ → Mapping tab, inline `<p-dialog>` picker.
- ~~Deployment as a first-class entity~~ → Done. Model + fixtures + interceptor routes.
- ~~Customer detail screen pattern~~ → Tree view (§6).
- ~~Multi-deployment + upsell flows~~ → `+ Add application` / `+ Add capability` modals.
- ~~"TMS Systems" terminology~~ → **Renamed to "Connections"** (May 13).
- ~~Integrations tab~~ → **Removed** (May 13).
- ~~Partners standalone management UI~~ → **Removed** (May 13/19). Partner field confirmed unnecessary by AJ.
- ~~Angular framework choice~~ → **Confirmed** (May 19).
- ~~Publish/Activate two-step workflow~~ → **Confirmed** (May 19/21). Tab renamed "Publish and Activate".
- ~~Test runner in final tab~~ → **Removed** (May 21). Auth testing moves to Connection tab; mapping tested separately.
- ~~"Test and Publish" tab name~~ → **Renamed "Publish and Activate"** (May 21).
- ~~Export + Webhook Handler in customer capability picker~~ → **Removed for MVP** (May 21). Import Orders only.
