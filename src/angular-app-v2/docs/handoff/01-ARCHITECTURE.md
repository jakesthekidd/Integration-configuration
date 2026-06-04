# 01 — Architecture

## Two apps, one codebase

The router exposes two distinct user-facing apps:

```
/customers              → WizardShellComponent       (Customer list)
/customers/:id          → CustomerDetailComponent    (Per-customer setup; the workhorse)
/admin                  → AdminShellComponent        (Integration Library)
```

The user lands at `/customers` by default. `/wizard/*` URLs are legacy redirects kept alive during the pivot and will be deleted in a future cleanup.

## High-level component tree

```
AppComponent
├── /customers
│   └── WizardShellComponent
│       └── customer table → row click → /customers/:id
│
├── /customers/:id
│   └── CustomerDetailComponent
│       ├── customer header (name, TMS, "enable customer" toggle)
│       ├── applications & deployments rail (left)
│       └── deployment detail (right)
│           ├── tab strip: Connection › Mapping › Publish & Activate · Activity
│           └── active tab content:
│               ├── ConnectionTabComponent
│               ├── MappingTabComponent
│               │   └── ReferenceTablesDialogComponent  (modal)
│               ├── TestPublishTabComponent              (Publish & Activate)
│               └── ActivityTabComponent
│
└── /admin
    └── AdminShellComponent
        └── tabs: Applications · Capabilities · MasterTemplates · LookupTables · APIClients
```

`CustomerDetailComponent` is the heaviest screen and the most important to understand. It coordinates four child tabs and the cross-tab `DraftService` signaling that makes auto-fork + snapshot viewing work.

## Layer responsibilities

| Layer | Examples | Responsibility |
|---|---|---|
| **Shells** | `customer-detail.component.ts` | Owns the URL, fetches top-level data (customer, deployments), routes user clicks to the right child tab, displays cross-tab banners. |
| **Tab components** | `connection-tab`, `mapping-tab`, `test-publish-tab`, `activity-tab` | Self-contained editors for one tab. Receive a `deployment` input. Save via `ApiService`. Emit `(saved)` events for the shell to react. |
| **Dialogs** | `add-deployment-dialog`, `reference-tables-dialog` | Modal experiences invoked from a parent. Two-way bound `visible`. |
| **Services** | `api.service`, `draft.service`, `customer-access.service`, `general.service`, `auth.service` | Cross-cutting state, HTTP, toasts, auth, per-deployment coordination signals. |
| **Mocks** | `mock-api.interceptor.ts`, `mock-data.ts` | In-memory fake backend. Will be deleted in production. See [03-API-CONTRACT.md](./03-API-CONTRACT.md). |
| **Models** | `models/*.model.ts` | Pure TypeScript types — no logic. Source of truth for entity shape. |

## Routing & navigation

Routes are defined in `src/app/app.routes.ts`. Inside `CustomerDetailComponent`, the **active tab is a signal**, not a URL segment:

```ts
activeTab = signal<'connection' | 'mapping' | 'publish-activate' | 'activity'>('connection');
```

This was deliberate — the tabs are local state of the customer-detail page, not shareable URLs. If linkable tab URLs become a requirement, this is a 1-hour refactor (add a `:tab` route param, sync the signal both ways).

The walkthrough flow (auto-advance after save) calls `selectTab(...)` from `onConnectionSaved()` and `onMappingSaved()` handlers — see [06-FEATURES.md → Walkthrough flow](./06-FEATURES.md#walkthrough-flow).

## Cross-tab state coordination — the `DraftService`

The single most important architectural pattern in this app. The tabs need to react to each other:

- **Mapping tab edit** → cross-tab amber dot should appear on "Publish & Activate" tab even before the user navigates there.
- **Publish & Activate tab** → opening an archived version flips the Connection + Mapping tabs into read-only snapshot mode.
- **Connection tab edit** when no draft exists → auto-fork a Draft so the user can save changes.

These would be tangled if tabs talked to each other directly. Instead they all read/write a small set of signals on the **`DraftService`** (`src/app/services/draft.service.ts`):

| Signal | Set by | Read by |
|---|---|---|
| `hasDraft(deploymentId)` | Tab components (`ensureDraft()` on first edit) and Publish & Activate (on draft creation/promotion) | All tabs (banner visibility) and shell (amber dot on tab label) |
| `viewVersion(deploymentId)` | Publish & Activate (when user clicks "View field mappings" on a past version) | All tabs (to switch to read-only snapshot mode) |
| `spawnRequest(deploymentId)` | Tab components (counter incremented on first edit) | Publish & Activate (effect seeds a Draft row when counter ticks) |

This is the prototype's stand-in for what would be a small bus / state-machine in production. See [05-STATE-MANAGEMENT.md](./05-STATE-MANAGEMENT.md) for the gotchas (hydration race, effect-only running when tab is mounted, etc.).

## Data flow on every tab

The same pattern repeats across all four tabs:

```
ngOnChanges('deployment')
  → load() — fetches initial state via ApiService
  → hydrates local signal(s) (mappings, credentials, versions, etc.)
  → sets a `snapshot` signal as the baseline for `dirty()` computed
  
user interaction
  → updateRow() / onCredChange() / addRow()
  → ensureDraft() — flips DraftService.hasDraft + requests spawn
  → updates local signal
  → dirty() = true

save()
  → ApiService.saveX(...) — round-trips through mock interceptor
  → on success: snapshot.set(latest) + emit (saved)
  → dirty() back to false
```

Both **read** (`load()`) and **write** (`save()`) go through `ApiService`. This is critical — it's what makes the prototype feel like a real app when navigating between tabs. See [05-STATE-MANAGEMENT.md → Mock-backed persistence pattern](./05-STATE-MANAGEMENT.md#mock-backed-persistence-pattern).

## File-by-file map

### `src/app/`

| File | What it does |
|---|---|
| `app.component.ts` | Root component — just a `<router-outlet>` + global `<p-toast>` host. |
| `app.config.ts` | Bootstrap config — registers the `mockApiInterceptor`, `provideAnimationsAsync()`, `provideHttpClient(withInterceptors(...))`. |
| `app.routes.ts` | Two top-level apps + legacy redirects. |

### `src/app/shells/`

| File | What it does |
|---|---|
| `wizard-shell.component.ts` | Customer list. Routes to `/customers/:id` on row click. Has "create customer" button (dialog flow). |
| `customer-detail.component.ts` | The workhorse. Customer header + deployments rail + tab strip + tab content. ~600+ lines. Coordinates `DraftService`, walkthrough auto-advance, snapshot banner suppression. |
| `admin-shell.component.ts` | Integration Library tabs (Apps, Capabilities, Templates, Lookups, API Clients). Read-mostly except for lookup-tables editor. |

### `src/app/capability/`

| File | What it does |
|---|---|
| `connection-tab.component.ts` | Per-deployment connection picker + credentials form + Test Authentication. Inline pass/fail banner. |
| `mapping-tab.component.ts` | Per-deployment field-mapping editor. Template picker dialog. Sample JSON test panel (local-preview-only). Reference Tables button. |
| `reference-tables-dialog.component.ts` | Read-only cross-reference (XREF) viewer for the mapping tab. See [06-FEATURES.md](./06-FEATURES.md#reference-tables-dialog). |
| `test-publish-tab.component.ts` | Publish & Activate tab. Version history table. Activate/archive workflow. Auto-fork effect. |
| `activity-tab.component.ts` | Recent transformation-log activity for this deployment. Read-only. |
| `add-deployment-dialog.component.ts` | Dialog launched from the deployments rail to enable a new (application × capability) for a customer. |
| `autofocus.directive.ts` | Small directive to autofocus an input. |

### `src/app/services/`

| File | What it does |
|---|---|
| `api.service.ts` | All HTTP. Wraps `HttpClient`. ~380 lines. Returns `Observable<ApiResponse<T>>`. Will hit a real backend in production. |
| `draft.service.ts` | Cross-tab draft signaling. ~70 lines. The most important non-HTTP service. |
| `customer-access.service.ts` | Customer enablement + per-application-capability toggle persistence. |
| `auth.service.ts` | Stub — currently returns a hardcoded user. Will be wired to real SSO. |
| `general.service.ts` | Toast helpers (`success()`, `error()`, etc.) that wrap PrimeNG's `MessageService`. |

### `src/app/mocks/`

| File | What it does |
|---|---|
| `mock-api.interceptor.ts` | `HttpInterceptorFn` that intercepts `/api/v1/*` and routes to in-memory data. ~260 lines. |
| `mock-data.ts` | Every fixture (customers, applications, capabilities, templates, mappings, versions, deployments). ~1100 lines. |

### `src/app/wizard/` (legacy)

Older wizard-style screens kept around during the pivot. Not part of the current user flow but still routable. Slated for deletion. Do **not** add new features here.

## Build & deployment

- `npm run build` — production build → `dist/field-mapping-app-v2/`
- `npm run vercel-build` — same, used by the Vercel preview deployment
- `vercel.json` at the project root configures the SPA fallback (`/* → /index.html`)
- The app currently deploys to a Vercel preview environment for stakeholder review. Production hosting is TBD.

## What's *not* in the prototype

Things a real developer will need to design and add:

- **Authentication & authorization.** `auth.service.ts` returns a hardcoded user. There's no token, no SSO, no route guards.
- **Real backend.** Every HTTP call is intercepted. See [03-API-CONTRACT.md](./03-API-CONTRACT.md) for the contract the real backend must honor.
- **Error handling.** The mock interceptor always returns success. Real network errors will hit empty `.subscribe()` blocks. Add a global error handler and reactive error UI.
- **Tests.** There are no meaningful tests. Add unit tests for services first, then component tests for the four tabs.
- **i18n.** All copy is hardcoded English.
- **Telemetry / analytics.** No instrumentation.
- **Feature flags / staged rollout.** No flag plumbing.
