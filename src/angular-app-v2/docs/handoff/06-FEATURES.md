# 06 — Features Catalog

One-pager per user-facing feature. Each section calls out:
- **What** the feature does
- **Where** in the code it lives
- **Behavior notes** (edge cases, design rationale)
- **What's faked** that the production version must replace

---

## Customer enablement (Library → Customers tab)

**What:** Internal admins control which customers appear in the Customer Setup app. New customers default to "not exposed."

**Where:**
- UI: `src/app/shells/admin-shell.component.ts` (the Customers tab)
- Model field: `Customer.integrationEnabled` (default `false`), `Customer.integrationStatusChangedAt`
- Currently persisted: in-memory via `customer-access.service.ts`

**Behavior:**
- Customer rows show a toggle. Flipping it ON adds the customer to `/customers` listing.
- Audit timestamp updates on each flip.
- Truck Mate is seeded with `integrationEnabled: false` so the demo flow exercises this toggle.

**Production gap:**
- Persist via `PATCH /customers/:id` (see [03-API-CONTRACT.md](./03-API-CONTRACT.md)).
- Add an audit log entry per flip (who, when).
- Add role gating — only admins should see this toggle.

---

## Walkthrough flow (chevrons + auto-advance)

**What:** First-time setup on a customer feels like a guided walkthrough without being a separate stepper component. The four tabs (Connection › Mapping › Publish & Activate · Activity) are visually separated into the workflow trio (chevrons between) and the side-utility (Activity, offset right).

**Where:**
- `src/app/shells/customer-detail.component.ts` — tab strip rendering, `selectTab()`, `onConnectionSaved()`, `onMappingSaved()`
- The chevron uses `<span class="tab-chevron"><i class="pi pi-angle-right"></i></span>`

**Behavior:**
- Saving on the Connection tab auto-advances to Mapping with a success toast.
- Saving on the Mapping tab auto-advances to Publish & Activate.
- Activity does **not** advance — it's a parallel utility, not part of the linear flow.
- The user can still freely click between tabs at any time. Auto-advance is a convenience, not a constraint.

**Design rationale:** an explicit `<p-stepper>` would have duplicated the tabs and added a parallel "where am I" indicator. The user's feedback was "make it as minimal as possible." Tabs + chevrons + auto-advance is the minimal version.

**Production gap:**
- None functionally. May want to add a "skip walkthrough" toggle for power users who don't want auto-advance.

---

## Connection tab: credentials + Test Authentication

**What:** Per-deployment, the user picks one of the available connections for the (app × cap) and fills in credentials. The "Test Authentication" button verifies connectivity.

**Where:** `src/app/capability/connection-tab.component.ts`

**Behavior:**
- The connection dropdown is filtered to `TmsSystem` rows scoped to this deployment's `(applicationId, capabilityId)`.
- Credential fields are driven by the connection's `connectionConfig` JSON (declared types: `text`, `password`, etc.).
- "Test Authentication" is **mocked**: random 80% pass, 20% fail, with a 1.2-second delay.
- The result renders as an inline banner (`.test-result--pass` green / `.test-result--fail` red). Pass and fail use the same copy structure for scannability — see [08-DESIGN-SYSTEM.md → Auth Result Banner](./08-DESIGN-SYSTEM.md#auth-result-banner-pattern).
- Snapshot view disables every input (`*ngIf="!viewingVersion()"`).
- Auto-fork: editing a credential when no Draft exists calls `ensureDraft()` — flips amber banner + requests spawn.

**Production gap:**
- `POST /deployments/:id/test-auth` — real connectivity check using the supplied credentials. Should hit the actual TMS endpoint with a no-op call (e.g. `GET /ping`) and return success/failure + error detail.
- Save endpoint: `PUT /deployments/:id/connection` with `{ connectionId, credentials }`. The current `save()` is a `setTimeout` stub.
- Credentials should be **encrypted at rest** server-side. The UI just sends them in the clear over HTTPS.

---

## Mapping tab: template fork + per-deployment field mapping

**What:** Pick a MasterTemplate to fork from (or start from scratch), customize the resulting field-mapping rows for this customer.

**Where:** `src/app/capability/mapping-tab.component.ts`

**Behavior:**
- Header shows "Forked from *MasterTemplate* · vN" or "Built from scratch."
- "Change template" / "Fork a master" button (primary outline) opens a picker dialog.
- The picker is filtered to **Published** templates relevant to this (app × cap × connection). Filtering by connection is best-effort in the prototype (the model doesn't carry the (app, cap, connection) tuple on templates yet — see [02-DATA-MODEL.md](./02-DATA-MODEL.md)).
- Forking replaces the table rows with the master's rows. The deployment is stamped with `forkedFromTemplateId + forkedFromTemplateVersion`.
- Editing any row → `ensureDraft()` → amber banner + draft spawn request.
- "Reference tables" button (secondary outline) opens the XREF dialog — see below.
- "Test with sample JSON" panel is local-preview-only — confirms parse + row count, does not actually evaluate the transformations.
- Save round-trips through `PUT /deployments/:id/mappings`. State survives navigation (see [05-STATE-MANAGEMENT.md → Mock-backed persistence](./05-STATE-MANAGEMENT.md#mock-backed-persistence-pattern)).

**Production gap:**
- Real JSON-path evaluator on the server for the "Test with sample JSON" panel. Endpoint: probably `POST /deployments/:id/preview-transform` with a sample input.
- Each row's `transformationConfig` UI is a single text input today. Production should branch by `transformationType` (Lookup needs a table picker, DateFormat needs a format picker, etc.).

---

## Reference Tables dialog

**What:** Read-only "cheat sheet" view of cross-reference lookup tables relevant to this deployment. Helps analysts authoring mappings see legal source codes and platform values.

**Where:** `src/app/capability/reference-tables-dialog.component.ts`. Triggered from the Mapping tab header.

**Behavior:**
- Left rail: list of tables filtered to `(connectionId OR customer.tmsName)`. The dual-key resolution accommodates legacy seeds (keyed by connectionId) and newer seeds (keyed by `tmsName` like `mcleod-v22`). See [02-DATA-MODEL.md → Production schema notes](./02-DATA-MODEL.md#production-schema-notes) for the cleanup recommendation.
- Right pane:
  - Table description + tags (field name · default value · case sensitivity)
  - Search box (filters both source and target columns)
  - Source → Target table with a per-row "copy to clipboard" button
- Banner at top: "These tables are read-only here. Open the Integration Library to edit."
- "Edit in Library →" link routes to `/library/lookup-tables`.

**Production gap:**
- Tighten the schema: split `LookupTable.tmsSystemId` into a proper FK to `tms_systems` once data is consistent.
- The mappings JSON column should be a child table: `lookup_table_entries(table_id, source_code, target_value)`.

---

## Publish & Activate tab

**What:** Per-deployment version history with state transitions (Draft → Published → Activated → Archived).

**Where:** `src/app/capability/test-publish-tab.component.ts`

**Behavior:**
- Table rows for every Version, ordered newest first.
- Per-row actions depend on state:
  - **Draft:** Publish, Discard, View field mappings (the editor at this snapshot)
  - **Published:** Activate, View field mappings
  - **Activated:** View field mappings only (current row)
  - **Archived:** View field mappings only
- Activating a Published version transactionally archives the previously-Active one.
- Auto-fork effect: when `DraftService.spawnRequest(id)` ticks, seed a new Draft row forked from the current Active (or from scratch if none exists).
- Mirror effect: keeps `mockVersions[id]` in sync with the local signal so navigation doesn't lose state. **Delete this when wiring real backend.**

**Production gap:**
- All state transitions need real endpoints — see [03-API-CONTRACT.md](./03-API-CONTRACT.md) and [04-DRAFT-AND-VERSIONING.md → Backend contract](./04-DRAFT-AND-VERSIONING.md#backend-contract).
- Add a confirmation modal for Activate (it's destructive to the previously-Active version).
- Optimistic locking — two users activating simultaneously must not corrupt history.

---

## Snapshot view (read-only historical viewing)

**What:** Click "View field mappings" on a Published/Activated/Archived row in Publish & Activate — Connection and Mapping tabs switch to read-only mode showing that snapshot's data.

**Where:**
- Triggers: `test-publish-tab.component.ts` → `DraftService.setViewVersion(id, { id, label })`
- Banner + read-only mode: `connection-tab.component.ts`, `mapping-tab.component.ts`
- Suppression of amber draft banner: `customer-detail.component.ts`'s `viewingSnapshotForSelected()` computed

**Behavior:**
- Indigo banner: "Viewing field mappings for vN — read-only snapshot."
- "Return to current" button clears the view-version signal.
- All form controls bound `[disabled]="!!viewingVersion()"`.
- Save buttons hidden via `*ngIf="!viewingVersion()"`.
- The amber "unsaved draft" banner is **suppressed** in snapshot view to avoid visual conflict.

**Production gap:**
- The prototype doesn't actually load the historical version's data — it just disables inputs and renders the current state. In production, the snapshot view needs to fetch `GET /deployments/:id/versions/:n/payload` and render *that* data, not the live editor's data.

---

## Auth Result Banner (success / fail pattern)

**What:** Inline pass/fail banner under the Connection tab's Test Authentication button. Documented in the design system for reuse.

**Where:**
- Implementation: `src/app/capability/connection-tab.component.ts` (`.test-result`, `.test-result--pass`, `.test-result--fail`)
- Storybook: `src/transflo-design-system/src/stories/overlay/auth-result-banner.stories.ts` (`Patterns/Auth Result Banner`)

**Behavior:**
- Both states use the same copy structure for scannability:
  - ✅ "Authentication successful — connected to {system}."
  - ❌ "Authentication failed — could not connect to {system}. Verify credentials and try again."
- Dismissable via the X icon.
- Lives above the action bar, anchored.

See [08-DESIGN-SYSTEM.md → Auth Result Banner](./08-DESIGN-SYSTEM.md#auth-result-banner-pattern).

---

## Activity tab

**What:** Recent transformation runs for this deployment, with drill-in to input/output payload.

**Where:** `src/app/capability/activity-tab.component.ts`

**Behavior:**
- Reads from `GET /transform-logs?templateId=…` (currently the mock returns all logs regardless of template).
- Click a row → modal with `inputData`, `outputData`, `errors`.
- Status pills: Success / Warning / PartialSuccess / Error.

**Production gap:**
- The endpoint should filter by deployment, not template — a customer cares about *their* runs, not all runs of the upstream master template.
- Add pagination (the mock has 6 logs; production will have thousands).
- Add a date-range picker UI (the API already supports `from`/`to`).

---

## Integration Library (admin shell)

**What:** Catalog of reusable building blocks. Tabs:
- **Applications & Capabilities** — read-only listing
- **Master Templates** — list + drill into versions + field-mappings editor
- **Lookup Tables** — full CRUD for cross-reference tables
- **API Clients** — assign templates to client identities
- **Customers** — the gateway toggle (integration-enabled flag)
- **Partners** — read-only listing

**Where:** `src/app/shells/admin-shell.component.ts`

**Behavior:**
- Tab is driven by `?tab=` query param so deep-links work.
- Edits in the Library are **global** — changing a template doesn't propagate to existing deployments that forked from it.

**Production gap:**
- Add role gating — only certain users should see the Library at all.
- "Template depends on lookup table X" reverse-lookup before deleting a lookup table.

---

## Truck Mate end-to-end demo

The canonical smoke test for the prototype. Run through this on a fresh page load:

1. **Open `/admin`** → Customers tab → find "Truck Mate Logistics" → flip Enable toggle.
2. **Navigate to `/customers`** → "Truck Mate Logistics" now appears.
3. **Click the row** → customer detail screen.
4. **In the deployments rail**, click "Add deployment" → pick "WorkflowAI" → "Import Loads" → save.
5. **Connection tab** opens → pick the McLeod v22 connection → fill credentials → click "Test Authentication" (may be pass or fail — try again on fail).
6. **Save changes** → auto-advance to Mapping.
7. **Click "Fork a master"** → pick a McLeod-style template → table populates.
8. **Edit one row** → notice amber "unsaved draft" banner appears.
9. **Click "Reference tables"** → see the 3 seeded McLeod v22 XREFs (status, equipment, stop types).
10. **Save changes** → auto-advance to Publish & Activate.
11. **Publish v1 → Activate v1** → row turns green.
12. **Navigate back to Mapping** → see saved rows + no draft banner (we're viewing the Active).
13. **Edit one row** → amber draft banner reappears + new v2 Draft auto-forks on Publish & Activate.
14. **Click "View field mappings" on v1 (Activated) from Publish & Activate** → C/M tabs go indigo, inputs disabled.
15. **Click "Return to current"** → editor re-enabled, working on v2 Draft.

If any step fails, that's a regression — fix before continuing handoff.
