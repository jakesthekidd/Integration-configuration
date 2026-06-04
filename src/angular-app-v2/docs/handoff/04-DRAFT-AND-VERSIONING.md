# 04 — Drafts & Versioning

This is the **most important** document for the implementing developer. The Draft → Published → Activated → Archived flow is the central abstraction of the app, and getting it wrong breaks the user's mental model of "what's actually live for this customer."

Read `../../DESIGN-STATUS-VERSIONING.md` and `../../PUBLISH-ACTIVATE-PLAN.md` first if you haven't — they're the original product design rationale. This doc is the *implementation* counterpart.

---

## The version is the unit, not the deployment

A `Deployment` is "this customer's setup of this (app × capability)." A `Version` is "what that setup *was* at a point in time" — a snapshot of (Connection + Mapping).

Every change a user makes happens on a **Draft** version. Only by clicking **Publish** and then **Activate** does the change become live. The previously-Active version becomes **Archived** automatically when a new one is Activated.

```
A deployment looks like:

  Deployment "WorkflowAI Import Loads for Truck Mate"
    ├── v1   Archived    (was Active, replaced 2 days ago)
    ├── v2   Archived    (was Active, replaced 8 hours ago)
    ├── v3   Activated   ← LIVE
    └── v4   Draft       ← user is editing this
```

Invariant: **at most one `Activated` version per deployment.** (And per the broader product rule, at most one `Activated` version across all deployments of the same `(customer × app × capability)`.)

---

## The four states

```ts
type VersionState = 'Draft' | 'Published' | 'Activated' | 'Archived';
```

| State | Mutable? | Visible where | What it means |
|---|---|---|---|
| `Draft` | yes | Connection/Mapping tabs (editable), Publish & Activate (row) | Working copy. The user can edit freely. Multiple drafts are allowed but the UI nudges users toward one. |
| `Published` | no | Publish & Activate (row), and snapshot-view in C/M | Frozen and ready for real-data testing. Not live yet. |
| `Activated` | no | Connection/Mapping tabs (read-only when not viewing a snapshot), Publish & Activate (row) | The version actually serving traffic. |
| `Archived` | no | Publish & Activate (row), snapshot-view in C/M | Historical record. Was Activated at some point, then replaced. |

---

## State transitions

```
            ┌─────────┐
            │  None   │ (brand new deployment, no versions yet)
            └────┬────┘
                 │ user edits Connection/Mapping → ensureDraft() → spawn v1 Draft
                 ▼
            ┌─────────┐
   ┌────────│  Draft  │────────┐
   │        └────┬────┘        │
   │             │ "Publish"   │ user discards
   │             ▼             │
   │        ┌──────────┐       │
   │        │Published │       │
   │        └────┬─────┘       │
   │             │ "Activate"  │
   │             ▼             │
   │     ┌────────────┐        │
   │     │ Activated  │        │
   │     └────┬───────┘        │
   │          │ another version  │
   │          │ gets Activated   │
   │          ▼                  │
   │     ┌──────────┐             │
   │     │ Archived │             │
   │     └──────────┘             │
   │                              │
   └──── auto-fork on edit ◀──────┘
              of Active
```

### Auto-forking

The crucial UX shortcut. When a user starts editing the Connection or Mapping tab and **no Draft exists**:

1. `ensureDraft()` is called on the first edit.
2. It flips `DraftService.setDraft(deploymentId, true)` immediately so the cross-tab amber banner + dot appear.
3. It calls `DraftService.requestSpawnDraft(deploymentId)` — increments a counter.
4. The Publish & Activate tab's `effect()` on that counter (when mounted) seeds a new Draft row in the version list, forked from the current Active.

The implementation detail "when mounted" matters — see [the hydration gotcha below](#hydration-race--why-its-needed).

### Brand-new deployments

If a deployment has **no Active version yet** (just created), the auto-fork effect would historically bail because there was no `baseVersion` to fork from. This was fixed:

```ts
// test-publish-tab.component.ts — auto-fork effect
const nextNumber = baseVersion
  ? Math.max(0, ...this.versions().map((x) => x.versionNumber)) + 1
  : 1;
// ...
notes: baseVersion
  ? `Auto-forked from v${baseVersion.versionNumber} (${baseVersion.state}) on first edit`
  : 'New draft — first configuration for this capability',
basedOnVersionNumber: baseVersion?.versionNumber,
```

When seeding from scratch, `basedOnVersionNumber` is undefined and the notes copy reflects that this is the first config.

### Promotion / "Publish"

When user clicks **Publish** on a Draft row:
- Draft → Published
- `publishedAt = now`
- Connection + Mapping become read-only-from-this-version forward; further edits auto-fork another Draft

### Activation

When user clicks **Activate** on a Published row:
- Published → Activated
- `activatedAt = now`
- Any previously-Activated version for **this deployment** is moved to Archived (`archivedAt = now`)
- The deployment's `status` rolls up to `Active`

### Discard / delete Draft

A Draft can be deleted from the Publish & Activate row. No state transition — the row is removed.

---

## Viewing a snapshot (read-only mode)

The Publish & Activate tab lets users click "View field mappings" on any Published / Activated / Archived row. This:

1. Sets `DraftService.setViewVersion(deploymentId, { id, label })`.
2. The Connection and Mapping tabs read `viewVersion()`, render an indigo "Viewing snapshot" banner, and **disable all inputs**.
3. The amber draft banner is **suppressed** while in snapshot view (because the user is not editing the current draft — they're looking at history).
4. The user clicks "Return to current" to clear `viewVersion` and go back to whatever the current state was.

```ts
// customer-detail.component.ts
viewingSnapshotForSelected = computed(() => {
  const id = this.selectedDeployment()?.id;
  return !!id && !!this.draftService.viewVersion(id);
});

// in the template:
@if (hasDraftForSelected() && activeTab() !== 'publish-activate' && !viewingSnapshotForSelected()) {
  <amber draft banner>
}
```

This three-way state — **edit mode** vs **read-only-current** vs **read-only-historical** — is the asymmetric banner logic table the prototype settled on. The matrix is:

| Selected version state | Banner |
|---|---|
| Draft (no snapshot view) | **Amber: unsaved draft** |
| Activated (no snapshot view, no draft on this deployment) | none |
| Activated **with** a Draft also existing (no snapshot view) | Amber draft banner (the draft is the dominant signal) |
| Any state, **snapshot view is set** | **Indigo: viewing snapshot — read-only** + suppress amber |

---

## `DraftService` — the cross-tab signal bus

`src/app/services/draft.service.ts`. Three signal maps keyed by `deploymentId`:

| Signal | Purpose |
|---|---|
| `draftsByDeployment` | True if a Draft exists. Drives banners + tab dots. |
| `viewVersionByDeployment` | If set, the currently-being-viewed historical snapshot. Drives read-only mode in C/M tabs. |
| `spawnRequestByDeployment` | Monotonic counter. Incremented by C/M tabs on first edit. Consumed by Publish & Activate tab's effect to seed a new Draft row. |

### Why a counter and not a direct call

The Connection/Mapping tabs can't directly mutate the version list — that lives inside `test-publish-tab.component.ts`. The counter pattern lets them signal "please make a draft" without coupling, and the Publish & Activate tab seeds the draft only when **it's mounted and effects run**.

There's a subtlety here: if the user is on the Mapping tab and never visits Publish & Activate, the counter increments but nothing seeds the draft row. That's fine — `setDraft(true)` is called directly by `ensureDraft()` so the banner still appears, and when they finally open Publish & Activate the effect catches up.

---

## Hydration race — why it's needed

**The biggest production-readiness bug to know about.** Two real bugs the prototype hit:

### Bug 1: mappings appear empty after navigation

`MappingTabComponent.save()` was originally a fake — it set a local snapshot signal but didn't round-trip through the API. On navigation away and back, the next `load()` got nothing from `mockDeploymentMappings` (still empty) and showed an empty mapping table.

**Fix:** `save()` now POSTs to `PUT /deployments/:id/mappings` which writes to the mock interceptor's `mockDeploymentMappings` store. Next `load()` reads it back. The pattern:

```ts
save() {
  this.api.saveDeploymentMappings(this.deployment.id, {
    mappings: this.mappings(),
    forkedFromTemplateId: ...,
    forkedFromTemplateVersion: ...,
  }).subscribe({
    next: () => {
      this.snapshot.set({ /* baseline */ });
      this.gen.success('Mapping saved.');
      this.saved.emit();
    },
  });
}
```

### Bug 2: activated versions disappear after navigation

`TestPublishTabComponent.versions` is a local signal initialized from `mockVersions[deploymentId]` on mount. When the user activated a version, mutated the local signal, then navigated away, the next mount re-read `mockVersions` — which was never written back — and got the stale seed.

**Fix:** an `effect()` mirrors the signal back to `mockVersions`, gated by a `hydrated` flag to avoid clobbering on initial empty fire:

```ts
private hydrated = signal<boolean>(false);

constructor() {
  effect(() => {
    const id = this.deployment?.id;
    if (!id) return;
    const list = this.versions();
    if (!this.hydrated()) return;  // ← prevents initial empty-fire clobber
    mockVersions[id] = list.map((v) => ({ ...v }));
  });
}

ngOnChanges() {
  this.hydrated.set(false);
  this.versions.set(seed.map((v) => ({ ...v })));
  this.hydrated.set(true);
}
```

### The lesson for production

In production with a real backend, neither of these hacks is needed — `save()` POSTs to the server, the next `load()` reads from the server, end of story. **Both the mirror-effect and the `hydrated` gate go away.** They're only here because of the in-memory mock.

When wiring the real backend, look at every place a tab component holds local state that isn't write-through to the API and confirm the read-after-navigate works. If it doesn't, the answer is **never** "add a mirror effect" — it's "make the save endpoint persistent."

---

## Backend contract

What production needs the API to expose for this feature to work properly:

```
GET   /deployments/:id/versions
        → Version[]  (ordered by versionNumber DESC)

POST  /deployments/:id/versions
        body: { basedOnVersionNumber?: number, notes?: string }
        → Version  (state = 'Draft', versionNumber = max+1)

PATCH /deployments/:id/versions/:n
        body: { notes? }
        → Version
        (only mutating Drafts; Published/Activated/Archived are immutable)

POST  /deployments/:id/versions/:n/publish
        → Version  (state = 'Published', publishedAt = now)
        only valid from 'Draft'

POST  /deployments/:id/versions/:n/activate
        → { activated: Version, archivedPrevious?: Version }
        only valid from 'Published'
        backend MUST move previously-Activated version (if any) to Archived in same txn

DELETE /deployments/:id/versions/:n
        → void
        only valid for 'Draft' state
```

### Concurrency notes

- The "one Active per (customer × app × cap)" invariant should be enforced at the **DB** level with a partial unique index, not just at the service layer.
- Activation should be transactional — either both the new version flips to Activated AND the old one to Archived, or neither.
- The client currently has no optimistic-locking signal. Production should add `If-Match` or a version-row `etag` to prevent stale-write races. Two users publishing the same Draft simultaneously is currently undefined behavior.

### Cross-deployment activation

When a deployment becomes `Active`, any **other** deployments on the same `(customer × app × cap)` tuple that were `Active` must transition to `Retired`. The UI assumes this happens automatically. The prototype doesn't model this — production must.

---

## Where this lives in code

| File | What it owns |
|---|---|
| `src/app/services/draft.service.ts` | The cross-tab signal bus |
| `src/app/capability/test-publish-tab.component.ts` | The version list, auto-fork effect, state transitions, mirror effect |
| `src/app/capability/connection-tab.component.ts` | `ensureDraft()` on first edit, snapshot-view banner |
| `src/app/capability/mapping-tab.component.ts` | `ensureDraft()` on first edit, snapshot-view banner |
| `src/app/shells/customer-detail.component.ts` | Banner coordination, snapshot view detection, tab dot |

---

## What to validate when porting to production

A checklist for the developer wiring the real backend:

- [ ] Edit a brand-new deployment → see a `v1 Draft` appear on Publish & Activate
- [ ] Edit an already-Active deployment → see auto-fork create a new `vN+1 Draft` while `vN Activated` remains
- [ ] Publish a Draft → status flips, edits to C/M afterward auto-fork another Draft
- [ ] Activate a Published → previously-Active becomes Archived, banner clears
- [ ] Click "View field mappings" on an Archived row → C/M tabs go indigo, inputs disabled
- [ ] "Return to current" → indigo banner clears, current state restored
- [ ] Multi-tab/multi-user: two browsers editing the same Draft → no silent data loss (needs optimistic locking)
- [ ] Reload mid-edit → unsaved Draft survives (currently the mock loses everything on reload; production must persist on every save)
