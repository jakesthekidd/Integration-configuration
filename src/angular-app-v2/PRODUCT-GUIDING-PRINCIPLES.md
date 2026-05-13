# Product Guiding Principles

**Project:** Transflo Integration Configurator (`src/angular-app-v2/`)
**Status:** Active prototype — v2 of the field-mapping app
**Reference:** Architecture diagrams in [FigJam — Integration Configuration](https://www.figma.com/board/JjOP6pBBp4C85LxaY7t9oB/Integration-Configuration)
**Design system:** [github.com/jakesthekidd/transflo-design-system](https://github.com/jakesthekidd/transflo-design-system) (PrimeNG 21 + TransfloTheme preset)

This document is the **canonical reference** for what we're building, why, and the shape it has to take. It captures decisions made across the v2 design conversations so future work doesn't drift.

---

## 1. Problem statement

Transflo's customers run on a fragmented set of TMS systems (McLeod v22, McLeod v23, SAP S/4 HANA, NetSuite, custom webhook endpoints, SFTP file drops). Every customer needs their data mapped into Transflo's products (WorkflowAI, Mobile, LTL Nav) in a bespoke way.

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
- Register a new Connection (e.g., "McLeod v23") when a new TMS or system version comes online
- Author Master Mapping Templates that PS can later fork per customer
- Maintain Lookup Tables (status codes, equipment codes) scoped to a Connection
- Manage API Clients (the `x-client-id` identities runtime callers use)

### Persona B — Professional Services / Customer Success (deploys)
**Touchpoint:** Customer Setup app
**Cadence:** Daily–weekly per active onboarding
**Goals:**
- See the full list of customers and what each has deployed
- Onboard a new customer via the 10-step wizard
- Fork a Master Mapping Template for a customer, tweak fields, test against a real order
- Publish + Activate (or rollback) a Deployment

These two personas **share the same data model** (Connections, Templates, Lookups, Customers) but operate on it differently. The library is read-only-by-default for PS; customer deployments are read-only-by-default for engineering.

### Concrete use case — ABC Carrier (from the FigJam example)

ABC Carrier is one customer with **three deployments**:

| # | Application × Capability | Connection | Credentials | Mapping | Status |
|---|---|---|---|---|---|
| 1 | WorkflowAI / Import Orders | McLeod v22 | ABC's McLeod login + API key | McLeod v22 → WorkflowAI (forked + customized) | Active |
| 2 | WorkflowAI / Export Documents | SAP S/4 HANA | ABC's SAP creds + endpoint | WorkflowAI → SAP (forked + customized) | Active |
| 3 | WorkflowAI / Webhook Handler | Webhook Receiver | ABC's webhook URL + secret | WorkflowAI events → ABC format (forked + customized) | Draft |

Each deployment is independent: status, credentials, customizations all per-row. The library entities (McLeod v22 Connection, the WorkflowAI/Import Orders MasterTemplate) are shared across many customers, but each customer's instance is a detached fork.

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
       Integrations
       Logs
```

Both apps live behind the same TRANSFLO chrome. The **app switcher** in the header is the only top-level nav. There is **no global left rail** — the per-customer left rail is scoped to the Customer Detail screen.

---

## 4. Domain model

| Entity | Owner | Scope | Notes |
|---|---|---|---|
| **Application** | Engineering | Top-level product | Hard-coded catalog: `WorkflowAI`, `Mobile`, `LTL Nav`. Read-only in UI. |
| **Capability** | Engineering | Owned by Application | Has direction (`Inbound` / `Outbound` / `Bidirectional`). E.g., WorkflowAI → Import Orders (Inbound), Export Documents (Outbound). |
| **Connection** | Engineering | Versioned adapter to an external system | e.g., `McLeod v22`, `McLeod v23`, `SAP S/4 HANA`, `NetSuite`, `Webhook Receiver`, `SFTP`. Defines credential schema + endpoint chain. |
| **Lookup Table** | Engineering | Scoped to a Connection | Key→value dictionaries (status codes, equipment types, doc categories). |
| **Mapping Template (Master)** | Engineering | Scoped to (Application, Capability, Connection) | Default field translations. Authored once, forked many times. |
| **API Client** (Integration) | Engineering | Runtime identity | The `x-client-id` value runtime callers use. Has assigned MasterTemplate versions. |
| **Customer** | Synced from legacy | Tenant | Read-only in v2's Customer Setup app — the list comes from the legacy customer registry. |
| **CustomerConnection** | PS | (Customer, Connection) | Holds per-customer credentials, tokens, URLs. |
| **CustomerTemplate** | PS | Forked from a MasterTemplate, attached to a Customer | Detached editable copy. Edits don't propagate to or from the master. |
| **Deployment** | PS | (Customer, Application, Capability, CustomerConnection, CustomerTemplate, Status) | The activation bundle. One Active per (Customer, App, Capability). |

### Lifecycle (Deployment status)

```
Draft  →  Tested  →  Published  →  Active  →  Retired
```

**Invariant:** Only one Deployment is `Active` per `(Customer, Application, Capability)` tuple. Activating a new one drains the prior Active to `Retired`.

---

## 5. The two apps

### App 1 — Integration Library  (`/admin`)
**Persona:** Engineering
**Purpose:** Build and maintain the reusable library that PS will deploy to customers.

Sub-tabs (in the navy stage banner):
1. **Applications & Capabilities** — read-only catalog browser (engineering manages the underlying fixtures in code)
2. **Connections** — list + editor for McLeod v22, SAP S/4, etc.
3. **Mapping Templates** — list + editor, organized as `Application × Capability × Connection`
4. **Lookup Tables** — list + editor, scoped per Connection
5. **Integrations** — API Client registry (`x-client-id` identities) and template assignments
6. **Logs** — runtime transformation telemetry

### App 2 — Customer Setup  (`/customers`)
**Persona:** Professional Services
**Purpose:** Manage every customer's deployments through a hierarchy-aware editor.

**No wizard.** The product treats each customer as a tree of deployments and lets PS edit any deployment's aspect (Connection, Mapping, Test & Publish, Activity) directly. This replaces the earlier linear 10-step wizard model, which created context-loss problems for multi-deployment customers and made upsell flows awkward.

Surfaces:
- **Customer list** (`/customers`) — table of all customers, click a row to drill in
- **Customer Detail** (`/customers/:id`) — the tree view (see §6 below). This is the workhorse screen.

---

## 6. Customer Detail — the tree-view + tabs pattern

The single screen where every per-customer integration work happens. Replaces the wizard entirely.

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

That button opens the same modal flow as `+ Add application`.

### Per-capability tabs

Each tab is **independently savable** — partial progress is fine. A deployment stays in `Draft` until the user explicitly Publishes it (see Test & Publish tab). PS can save the Connection today, leave, and fill the Mapping tomorrow.

| Tab | Owns | Persists to |
|---|---|---|
| **Connection** | Connection picker + customer's credentials | `CustomerConnection` |
| **Mapping** | Fork-master-template picker + field-mapping table + inline JSON test panel + lookup-table picker | `CustomerTemplate` |
| **Test & Publish** | Real-order test runner, Publish / Activate / Retire / Rollback buttons | Mutates `Deployment.status` and `snapshotVersion` |
| **Activity** | Read-only transformation log filtered to this `deploymentId` | — |

### Deployment ⇄ tab semantics

- **You cannot Activate** until Connection + Mapping have at least their minimum required data (gating lives on the Test & Publish tab)
- **You can Save** any tab at any time, regardless of others' state
- **Status transitions** (Draft → Tested → Published → Active → Retired) happen ONLY in the Test & Publish tab; the rail status pill reflects what's persisted
- Activating retires any prior Active for the same `(customer, application, capability)` tuple (the §4 invariant)

### Add Application / Add Capability flows

Both use a small `<p-dialog>` (no separate wizard). Context is implicit from where the `+` was clicked.

**+ Add application** (rail root):
1. Modal lists applications NOT yet deployed for this customer (e.g., if WorkflowAI is already present, only Mobile + LTL Nav appear)
2. Pick one → new node appears in tree, expanded, with one empty Capability slot
3. User clicks `+ Add capability` next

**+ Add capability** (rail under an app):
1. Modal lists capabilities of that application NOT yet deployed (e.g., if Import Orders is deployed, only Export Documents + Webhook Handler appear for WorkflowAI)
2. Pick one → new node appears, selected, right pane opens to the **Connection tab** with empty fields, status = `Draft`
3. User fills Connection + Mapping at their own pace, Saves each tab as they go, Publishes when ready

### Template picker (inline, lives in the Mapping tab)

Templates are **children of (Application, Capability, Connection)**, so the picker stays tight. Pressing "Fork a master template" or "Change template" inside the Mapping tab opens a `<p-dialog>` pre-filtered:

> **Templates for WorkflowAI · Import Orders · McLeod v23**
> [search]
> · McLeod v23 → EDI 204 (v3, Published) · used by 12 customers
> · McLeod v23 → Lite (v1, Published) · used by 2 customers
> · — Start from scratch —

If the user later changes the Connection on the Connection tab, the available templates list refreshes. Re-picking a different template warns before overwriting in-progress mapping edits.

### Wizard rules
- Each step's component writes to `WizardStateService` on every input change (signals-based).
- Navigation goes through the footer only. Back is always available (except step 1); Next is gated by per-step validation.
- The breadcrumb in the wizard's content card shows `All Customers / Set Up Wizard`. This is **separate** from the stage layout's breadcrumb (which we don't use in this app).
- Cancel = return to Manage Customers list; draft is discarded (confirmation TBD).

---

## 7. Design system contract

### Chrome — non-negotiable
All chrome comes from `src/app/design-system/`:

- **HeaderToolbar** — white 50px top bar with TRANSFLO logo, page link, app switcher dropdown (9-dot grid), avatar. **Local additions** (preserve on sync): `apps` Input, `(appChange)` Output for routing.
- **StageLayout** — fixed-height navy banner + overlapping white card.
  - **Navy: 163px fixed.** Does not change based on contents. Title at top (56px row), optional tabs/breadcrumbs row beneath the divider.
  - **White card overlap:** −99px when no nav (card top sits at title-row bottom + 8px), −59px when tabs/breadcrumbs are present (card top sits below the nav row + 8px). Sourced from Figma frame `75:1876` "Correct Layout".
  - **Local additions** (preserve on sync): `<ng-content>` slot, `(tabChange)` event, `[showSideNav]` / `[showActionButtons]` flags, empty-data guards on nav rows, fixed-height navy, variable card overlap.
- **SideNav** — vertical icon rail. Ported but **hidden** (`[showSideNav]="false"`); the inspiration screens don't use one.

The HeaderToolbar + StageLayout structure is **the product's frame**. Components rendered inside the stage content (shells, lists, forms) do not redefine chrome.

### Type scale — 4 sizes only
| Token | Size | Use |
|---|---|---|
| `--tf-text-display` | 18px / 700 | Section title in `.tf-section-header` (one per screen) |
| `--tf-text-heading` | 15px / 700 | Card title, modal title |
| `--tf-text-body` | 13px / 400 (labels 600) | Default everywhere |
| `--tf-text-meta` | 11px / 600 | Tags, table headers, helper text, eyebrows |

Anything else (12, 14, 16, 19, 22) is a smell.

### Spacing scale — strict
`--tf-space-1..6` = `4 / 8 / 12 / 16 / 20 / 24` (px). No 10, 14, 15.

### Color
Brand blue (`--tf-blue-500: #2474BB`) is the primary. Orange (`--tf-orange-*` and `--tf-required: #d97706`) is a single accent reserved for required-field asterisks and warnings — not a secondary brand color. Status uses PrimeNG severity:
- Active → `success` (green)
- Inactive / Paused → `secondary` (gray) — **not** `danger`
- Draft / Warning → `warn` (orange)
- Failed / Deleted → `danger` (red)
- Inbound → `info` (light blue)
- Outbound → `warn`
- Bidirectional → `contrast`

### PrimeNG-first
We use the design system's PrimeNG components for everything that has a PrimeNG equivalent:
- `<p-button size="small">` for all buttons (no Bootstrap `.btn`)
- `<p-table p-datatable-sm>` for all tables
- `<p-tag>` for all status/category pills (small + rounded, enforced globally)
- `<p-tabs>` (or PrimeNG `<p-tabview>` equivalent) for the per-capability tab strip in the right pane
- `<p-toast>` for success/error feedback (mounted once in AppComponent)
- `<p-confirmdialog>` for destructive confirmations (mounted once)
- `<p-dialog>` for inline pickers (Add Application, Add Capability, Fork Template)
- `<p-menu>` for kebab-style row actions
- PrimeIcons (`pi pi-*`) for all icons

Custom CSS is allowed when PrimeNG doesn't cover something, but must use design tokens (`var(--tf-*)`) — no hardcoded hex.

### Section header pattern — `.tf-section-header`
Every non-wizard screen's content card starts with one of these:
- White background, 12/20 padding, bottom border
- Left: `<h2>` title (18px) + optional `<p>` subtitle (13px, muted)
- Right: optional `<p-button size="small">` action

---

## 8. What's already built in v2

| Surface | Status |
|---|---|
| HeaderToolbar with app switcher (Integration Library / Customer Setup) | ✓ Wired |
| StageLayout with fixed-height navy + variable overlap | ✓ Matches Figma `75:1876` |
| AppComponent — derives apps, active tab, tab change → URL | ✓ |
| AdminShell with 6 sub-tabs in the navy banner (Integration Library) | ✓ |
| Applications & Capabilities (read-only catalog) | ✓ |
| Customer list with `<p-table>`, `<p-tag>`, kebab `<p-menu>` | ✓ |
| Customer Detail (list-of-rows version) | ✓ — being replaced by the tree view (§6) |
| Wizard (10 steps, Steps 1–10 functional) | ⚠ Being deprecated entirely — see §6 |
| Mock API interceptor with fixtures incl. Deployments | ✓ |
| GeneralService delegates to PrimeNG ConfirmationService + MessageService (SweetAlert2 removed) | ✓ |
| Design tokens — `--tf-text-*`, `--tf-space-*`, `--tf-radius-*` | ✓ |

## What's coming next (Phase 2 of the tree-view pivot)

1. **Tree-view shell** in Customer Detail — rail (apps → capabilities) + right pane with a placeholder
2. **Four per-capability tabs** — Connection, Mapping, Test & Publish, Activity. Each independently savable.
3. **Add Application / Add Capability modals** — short pickers that drop you into the empty Connection tab for the new deployment
4. **Cleanup pass** — delete the wizard shell, all step components, WizardStateService, wizard routes; rename `/wizard` → `/customers`

## What's NOT built yet and is out of scope for this pivot

- Real Connection / Template / Lookup / Integration editors in the Integration Library — those admin tabs still render legacy v1 components that need full rebuilds
- Real backend — entire data layer is still the mock interceptor
- Auth, multi-tenant scoping, audit log
- Real Deployment lifecycle wiring (POST/PATCH on deployment status; today the actions are mock optimistic updates)

---

## 9. Conventions for future work

- **Stage chrome is sacred.** Do not bypass `<app-stage-layout>` for any screen. The navy + card overlap is the product's identity.
- **Two apps, one data model.** Whenever a feature touches both personas, build it in the shared mock/service layer and surface it differently in each shell.
- **Wizard never edits the library.** A wizard can READ from MasterTemplates / Connections / Lookups, but it produces CustomerTemplates / CustomerConnections / Deployments — never modifies the library.
- **Forks are detached.** Editing a CustomerTemplate must not affect its MasterTemplate origin. Document the fork lineage in the data model but never auto-sync.
- **One Active per (Customer, App, Capability).** Activating a new Deployment retires the prior Active. Surface this prominently in the Activate step.
- **PrimeNG before custom.** New surfaces import from `primeng/*`. New custom components are last resort and must use design tokens.
- **Sync the design system, don't fork it.** The `transflo-design-system-sync` skill pulls upstream and reconciles local additions. Never rewrite ported components from scratch — preserve the `Local additions` comment block and let the skill diff.

---

## 10. Open questions / things we may still push back on

- **Naming of the two apps.** Current: "Integration Library" (engineering) + "Customer Setup" (PS). Open to a different pair if a better fit emerges.
- **Customer list ownership.** Today v2 mocks a customer list. Real customers come from the legacy system — confirm sync direction (read-only into v2, or v2 owns?) before building anything that mutates customer records.
- **Master template editor pattern.** The mapping editor for AUTHORING masters (in the Integration Library) is still TBD. The CustomerTemplate-side mapping editor lives in §6's Mapping tab. Both will share UI primitives, but master authoring may need more (versioning controls, publish/archive, "used by N customers" link).
- **Side nav re-introduction?** Inspiration screens don't show one, but as the app grows (Logs as a top-level destination? Settings? Profile?), an icon rail may be warranted. Default position: no side nav until we have ≥3 top-level destinations.

### Resolved (kept here for trail)
- ~~Wizard or no wizard?~~ → **No wizard.** Tree-view + per-capability tabs replaces the 10-step linear flow.
- ~~Templates editor pattern (PS side)~~ → Mapping tab inside each capability. Inline `<p-dialog>` picker filtered by `(Application, Capability, Connection)`.
- ~~Deployment as a first-class entity~~ → Done. Model + fixtures + interceptor routes added.
- ~~Customer detail screen pattern~~ → Tree view; details in §6.
- ~~Multi-deployment + upsell flows~~ → Solved by tree view + the `+ Add application` / `+ Add capability` modals.
- ~~Editing existing deployments~~ → Direct manipulation in the tree view's tabs; no "edit mode" toggle needed.
