# Publish & Activate — Redesign Plan

Source: PM working session 5/28/26 + Jake's clarifications.
Replaces the current chevron/stacked-containers UX with a simpler **draft banner + versions table** model.

---

## Model

Four statuses, one of them lives outside the table:

| Status        | Lives in       | Singleton? | Actions                                  |
| ------------- | -------------- | ---------- | ---------------------------------------- |
| **Draft**     | Banner above table | Yes    | Publish · Discard                         |
| **Published** | Versions table | No         | Activate · Archive · Edit notes           |
| **Active**    | Versions table | Yes        | Edit notes (must activate something else to demote) |
| **Archived**  | Versions table | No         | Reactivate · Edit notes · Delete          |

### State transitions

- Editing any version's snapshot (Active or Archived) → spawns/updates the singleton Draft.
- Publish draft → new row enters table as **Published**, draft clears.
- Activate a Published or Archived row → that row becomes **Active**, previous Active **auto-archives** with an undo toast (5s window).
- Archive a Published or Active row → status flips to Archived (Active requires another version to be activated first).
- Reactivate an Archived → same as Activate.

### Confirmed decisions

1. Draft is created when editing a non-Active version too (e.g. forking off an Archived row to try a rollback variant).
2. Activating auto-archives the previous Active. Undo toast restores both rows.

---

## UI

### Publish & Activate tab

```
[Draft banner — conditional]
  ⚠  Unsaved draft — based on v3 (Active)
  Notes: [textarea]
  [Discard]                                    [Publish]

[Versions p-table]
  # │ Status     │ Published   │ Activated    │ Notes │ ⋯
  4 │ ● Active   │ 6/01/26     │ 6/01/26      │ ...   │ ⋯
  3 │ Published  │ 5/12/26     │ —            │ ...   │ ⋯
  2 │ Archived   │ 4/01/26     │ 4/01–6/01    │ ...   │ ⋯
  1 │ Archived   │ 3/01/26     │ 3/01–4/01    │ ...   │ ⋯
```

Tab renamed from **"Test & Publish"** → **"Publish & Activate"**.

### Cross-tab draft pill

On Connection, Mapping, Activity tabs — top-right header pill:

- No draft: `● Active — v3` (subtle blue)
- Draft exists: `⚠ Unsaved draft — based on v3` (amber, clickable, navigates to Publish & Activate)

### Read-only version viewing

- Tabs accept a `?viewVersion=<id>` query param.
- When set, Connection/Mapping/Activity tabs render the snapshot of that version, **read-only**.
- Banner across page: `Viewing v2 (Archived) — read-only.  [Return to current]`
- Entered from the versions table row action **"View field mappings"**.

---

## Build order

| #   | Step                                                                       | Status      |
| --- | -------------------------------------------------------------------------- | ----------- |
| 1   | Rewrite `test-publish-tab.component.ts`: draft banner + p-table            | **Done**    |
| 2   | Tighten version logic: at most one Draft per deployment                    | **Done** (enforced in `editAsDraft`) |
| 3   | Rename tab label everywhere (`Status` → `Publish & Activate`)              | **Done**    |
| 4   | Cross-tab draft signaling: `DraftService` + amber dot on tab + warning banner on Connection/Mapping/Activity | **Done** |
| 5   | View-archived-mappings: `DraftService.setViewVersion` + read-only banner on Mapping tab | **Done** (banner only; snapshot data binding deferred — current mappings still shown) |
| 6   | Auto-archive previous Active with undo banner                              | **Done** (5s inline undo) |
| 7   | Remove "Status" terminology from labels/banners                            | **Done**    |
| 8   | Delete `lifecycle-chevron.component.ts` (no longer used)                   | **Done**    |
| 9   | Tighten this plan doc / mark steps complete                                | **Done**    |

## Known gaps (deferred)

- **Step 5 is half-done:** The "View field mappings" action navigates to the Mapping tab and shows a read-only banner, but the displayed mappings are still the current ones, not a true snapshot of the chosen version. Wiring real per-version snapshots requires the mock data to carry per-version mapping arrays, which is a small-but-real refactor of `mockVersions` + `MappingTabComponent.ngOnChanges`.
- **DraftService reactivity edge case:** The amber dot on the tab strip relies on `DraftService.hasDraft()` reading a signal during change detection. If the customer-detail's computed isn't triggered by the signal read (it should be, but worth verifying in dev), wrap as `hasDraft$()` instead.
- **Connection tab does not yet auto-spawn a draft** when credentials are mutated. Currently only the Publish & Activate tab can mint drafts (via "Edit as new draft" or seeded mock data). Closing this loop requires Connection & Mapping tabs to dispatch "spawn draft" on first mutation — out of scope for this redesign session.

---

## Out of scope for now

- Diffing two versions side-by-side (could come later as a separate view).
- Per-row permission gating.
- Backend persistence — still mock-only.
