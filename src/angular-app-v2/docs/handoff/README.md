# Developer Handoff — Integration Configurator

> **Audience:** the engineer(s) who will turn this prototype into a production application.
> **State:** UX/UI prototype on Angular 21 + PrimeNG 21 with an in-memory mock backend. All persistence is faked via an `HttpInterceptor`. Auth is stubbed. No real APIs are wired.

---

## What this app is

The **Integration Configurator** is the operator tool that lets Transflo staff (and eventually customers) configure how an external system (TMS, ERP, webhook) integrates with the Transflo platform. The user picks a **customer**, enables an **application capability** (e.g. WorkflowAI Import Loads), sets up the **connection**, **maps** the field translations, and **publishes & activates** the integration.

Two top-level apps share the codebase:

| App | Route | Purpose |
|---|---|---|
| **Customer Setup** | `/customers`, `/customers/:id` | Per-customer setup walkthrough (Connection → Mapping → Publish & Activate → Activity). The primary user flow. |
| **Integration Library** | `/admin` | Catalog of reusable building blocks — Applications, Capabilities, MasterTemplates, Lookup Tables, API Clients. Edits here are global, not per-customer. |

---

## Read these in order

1. **[01-ARCHITECTURE.md](./01-ARCHITECTURE.md)** — How the app is structured. Shells, routes, tab components, services, where things live.
2. **[02-DATA-MODEL.md](./02-DATA-MODEL.md)** — Every entity, the relationships between them, and the status/state enums that drive UI behavior.
3. **[03-API-CONTRACT.md](./03-API-CONTRACT.md)** — Every endpoint the mock interceptor implements, the request/response shape, and what the production backend needs to honor.
4. **[04-DRAFT-AND-VERSIONING.md](./04-DRAFT-AND-VERSIONING.md)** — The most subtle behavior in the app: the Draft → Published → Activated → Archived lifecycle, auto-forking on edit, and read-only snapshot viewing.
5. **[05-STATE-MANAGEMENT.md](./05-STATE-MANAGEMENT.md)** — Signal-based reactivity patterns, `DraftService` coordination, hydration-race gotchas, the "mock-backed persistence" pattern.
6. **[06-FEATURES.md](./06-FEATURES.md)** — One-pager per feature: walkthrough chevrons, reference-tables dialog, auth-result banner, template picker, customer enablement flow, and more.
7. **[07-FROM-PROTOTYPE-TO-PRODUCTION.md](./07-FROM-PROTOTYPE-TO-PRODUCTION.md)** — Concrete migration checklist. What's real, what's faked, what to wire up first.
8. **[08-DESIGN-SYSTEM.md](./08-DESIGN-SYSTEM.md)** — Styling tokens, PrimeNG conventions, the sibling `transflo-design-system` Storybook, where to find reusable banner/dialog patterns.

---

## Quick start

```bash
cd src/angular-app-v2
npm install
npm start         # dev server on http://localhost:4400
npm run build     # production build into dist/field-mapping-app-v2
```

There is **no backend** to run. The `mockApiInterceptor` intercepts every `/api/v1/*` request and returns canned data from `src/app/mocks/mock-data.ts`. State is in memory and resets on full page reload.

---

## Repo orientation

```
src/angular-app-v2/
├── src/app/
│   ├── app.config.ts            ← interceptor registration, providers
│   ├── app.routes.ts            ← two top-level apps + legacy redirects
│   ├── shells/                  ← screen-level containers
│   │   ├── admin-shell.component.ts          (Library)
│   │   ├── wizard-shell.component.ts         (Customer list)
│   │   └── customer-detail.component.ts      (Customer detail; the workhorse)
│   ├── capability/              ← the four tabs inside customer-detail
│   │   ├── connection-tab.component.ts
│   │   ├── mapping-tab.component.ts
│   │   ├── test-publish-tab.component.ts     (Publish & Activate)
│   │   ├── activity-tab.component.ts
│   │   └── reference-tables-dialog.component.ts
│   ├── services/                ← cross-cutting state + API
│   │   ├── api.service.ts
│   │   ├── draft.service.ts                  (cross-tab draft signaling)
│   │   ├── customer-access.service.ts
│   │   ├── auth.service.ts                   (stub)
│   │   └── general.service.ts                (toasts)
│   ├── models/                  ← TypeScript domain types
│   ├── mocks/                   ← in-memory data + interceptor
│   │   ├── mock-data.ts
│   │   └── mock-api.interceptor.ts
│   └── design-system/           ← in-app token CSS
└── docs/handoff/                ← you are here
```

The sibling **`src/transflo-design-system/`** is a separate Angular + Storybook project that hosts the visual design system. It's referenced for cross-team alignment but the app does NOT import from it at runtime — styles are duplicated where needed (see [08-DESIGN-SYSTEM.md](./08-DESIGN-SYSTEM.md)).

---

## Pre-existing design docs

Older context that's still useful — read these too:

- `../../PRODUCT-GUIDING-PRINCIPLES.md` — product invariants (e.g. "one Active deployment per (customer × app × capability)")
- `../../DESIGN-STATUS-VERSIONING.md` — original version-state design rationale
- `../../PUBLISH-ACTIVATE-PLAN.md` — Publish & Activate tab design plan
- `../../DESIGN-DECISIONS-2025-05-*.md` — dated design-decision logs

These are the source of truth for *why* things are shaped the way they are. The handoff docs in this folder are the source of truth for *what* and *how*.

---

## Conventions in this codebase

- **Angular 21 standalone components.** No `NgModule`s. Imports are declared on each `@Component`.
- **Signals over RxJS for component state.** RxJS is still used for HTTP. The two are bridged manually with `subscribe(...)` in services.
- **PrimeNG 21** for almost every UI primitive. `p-table`, `p-button`, `p-select`, `p-dialog`, `p-toast`, `p-message`, `p-tag`, `p-tooltip`, `p-checkbox`, `p-password`, `p-textarea`.
- **No state library.** No NgRx, Akita, Signal-Store. Cross-component coordination goes through small `@Injectable({ providedIn: 'root' })` services that expose `signal()`s (see `DraftService`).
- **Mock-first.** Every "save" round-trips through the `ApiService` → `mockApiInterceptor`. See [05-STATE-MANAGEMENT.md](./05-STATE-MANAGEMENT.md) for the persistence pattern.
- **No tests yet.** A `dummy.spec.ts` exists to keep the test runner alive but there are no meaningful unit or integration tests. Adding them is a Phase-1 priority.

---

## Where to start as the implementing developer

1. Run the app locally and click through the Truck Mate flow described in [06-FEATURES.md](./06-FEATURES.md#truck-mate-end-to-end-demo). This is the canonical smoke test.
2. Read [01-ARCHITECTURE.md](./01-ARCHITECTURE.md) end to end.
3. Read [03-API-CONTRACT.md](./03-API-CONTRACT.md) to scope the backend work.
4. Read [04-DRAFT-AND-VERSIONING.md](./04-DRAFT-AND-VERSIONING.md). This is the **highest-risk** behavior to preserve when wiring the real backend.
5. Use [07-FROM-PROTOTYPE-TO-PRODUCTION.md](./07-FROM-PROTOTYPE-TO-PRODUCTION.md) as a checklist for the migration.
