# Design — Status & Versioning Redesign

> **Source of truth** for the Status tab + version-container redesign. Implementers should read this end-to-end before touching code. Changes to this spec require a corresponding update here before code lands.

**Reference Figma:** [Integration Configurator — node 115-2980](https://www.figma.com/design/8xqjykavNytpX8ctrnaJuD/Integration-Configurator?node-id=115-2980)

**Last updated:** 2026-05-26
**Status:** Approved for implementation

---

## 1. Context

**User**: Internal Transflo team configuring integrations on behalf of a customer. **Not** the customer themselves. Copy/density should reflect a technical internal audience — short labels, precise verbs, no consumer-y hand-holding.

**Today's problem** (post-iteration of the previous Status tab redesign):

- Status, versioning, and archived configurations don't visually relate to each other.
- Hard to see lineage between versions.
- Status banner + lifecycle controls + promote actions all live on the same flat surface — no sense that "this deployment has had a history."

**Goal**: Make the chevron status bar the primary interactive control, and make version history feel cohesive — stacked, related, scannable, with clear lineage.

---

## 2. Model

### 2.1 Version states

A version (the unit of one snapshot of Connection + Mapping for one Customer × App × Capability deployment) is in exactly one of these states at any time:

| State        | Persistent?    | Notes |
|--------------|----------------|-------|
| **Draft**    | Yes            | Editable. Multiple drafts can coexist. |
| **Published**| Yes (briefly)  | Snapshot is locked in; not live. User typically activates from here, but the version can sit here. |
| **Activated**| Yes (singular) | Live. **Exactly one** Activated version per (Customer × App × Capability) at any time. |
| **Archived** | Yes            | Historical. Was Activated, now retired. Can be rolled back to. |

> **Retire is dropped.** Per alignment Q2, "Archive" subsumes the prior "Retire" concept. There is no separate Retire action.

### 2.2 Invariants

1. **One Activated per (Customer, App, Capability)**. Activating v2 must auto-archive the previously-Activated v1.
2. **Drafts cannot be Activated directly**. They must first move Draft → Published → Activated. The chevron enforces this.
3. **Multiple Drafts allowed**. A deployment can have N drafts pending review/iteration.
4. **No auto-fork**. Editing an Activated version does *not* implicitly create a new Draft — the user must explicitly fork one via the "+ New draft from this" button.
5. **Versions are append-only**. They stack indefinitely; no version is ever physically deleted *except* unpublished Drafts (see Manage menu — Delete).

### 2.3 What each version stores

Each version is a snapshot of the (Connection + Mapping) configuration at the time it was Published. Drafts hold the user's in-progress edits (mutable). Published/Activated/Archived versions are immutable snapshots.

---

## 3. Lifecycle / Chevron Behavior

### 3.1 Visual model

The chevron is **the primary lifecycle control** on each version's container. It visually shows the three forward stages:

```
[ Draft ]─────[ Published ]─────[ Activated ]
```

The current state is highlighted; downstream stages are clickable; upstream stages are inert (you can't un-publish via the chevron — that's a Manage menu action).

### 3.2 Click semantics

- **Click is always forward**. The chevron only advances state.
- **Each click triggers a confirmation dialog** before commit. No silent transitions.
- **Confirmation copy is contextual and warns about side effects**. Example for Draft → Published:
  > "Publish v2? This locks the current Connection + Mapping into a versioned snapshot. The version isn't live yet — activate it next when you're ready."
- And for Published → Activated:
  > "Activating v2 will archive v1 and apply the new mapping to **{customerName}**'s integration. Continue?"

### 3.3 Chevron position-by-state

| Version state | Chevron position             | Clickable stages |
|---------------|------------------------------|------------------|
| Draft         | Pos 1/3 filled               | Published (advance) |
| Published     | Pos 2/3 filled               | Activated (advance) |
| Activated     | Pos 3/3 filled, **inert**    | None — chevron is purely a status indicator |
| Archived      | Hidden                       | N/A — Archived containers don't show a chevron |

> Per alignment Q3, **clicking Published from Draft lands in the Published state** (not jumps to Activated). Two clicks total to go Draft → Activated.

### 3.4 Hover & affordance

- Each clickable chevron stage has a clear hover state (outlined, brand-blue glow, or tinted bg) so it's obviously interactive.
- Inert stages (e.g. Activated when version is Activated) have no hover effect and a `not-allowed` cursor on hover (signaling "this is not a button right now").

---

## 4. Version Container

Each version has its own **container** — a self-contained card showing its chevron, metadata, and Manage menu.

### 4.1 Container header (always visible)

Every container shows:

- **Version number** (e.g. "v2")
- **Created timestamp** (e.g. "Created May 24, 2026 · 3:42 PM")
- **Author** (e.g. "Jake Cummings")
- **State chip** (Draft / Published / Activated / Archived — colored to match)
- **Manage menu** (⋯ overflow, see §6)

These let the user tell multiple Drafts apart at a glance.

### 4.2 Expansion behavior

| State      | Default expansion | Body content when expanded |
|------------|-------------------|----------------------------|
| Draft      | **Expanded**      | Chevron + summary of changes (diff vs Activated, optional) |
| Published  | **Expanded**      | Chevron + summary |
| Activated  | **Expanded**      | Chevron (inert) + summary + **`+ New draft from this`** button |
| Archived   | **Collapsed**     | Click header to expand; body shows the historic chevron (all 3 filled, dimmed) + summary |

### 4.3 Stack order (top to bottom)

```
┌─ Drafts (newest first) ──────────────────────┐
│  v4 · Draft         (you can have many)       │
│  v3 · Draft                                    │
├─ Activated ──────────────────────────────────┤
│  v2 · Activated  ← current live version       │
├─ Archived (newest first, all collapsed) ─────┤
│  v1 · Archived                                 │
│  v0 · Archived (further back, also collapsed) │
└──────────────────────────────────────────────┘
```

Published versions, when they exist, sit between Drafts and the Activated row.

### 4.4 Fork affordance

The Activated version's container shows a primary button:

> **`+ New draft from this`**

Clicking it forks a new Draft seeded with the Activated version's Connection + Mapping configuration. The new Draft appears at the top of the stack.

**Why a visible button (not a banner on edit)**:
- Discoverable upfront — user sees the path to make changes immediately.
- Maps cleanly to the "manual fork" mental model.
- Doesn't require change-detection plumbing in Connection/Mapping tabs.
- Keeps each container's primary action obvious.

---

## 5. Manage Menu (per version)

Each version's `⋯` menu offers these items, gated by version state:

| Item            | Available on                  | Behavior |
|-----------------|-------------------------------|----------|
| **Archive**     | Activated                     | Manually archive the active version. The deployment goes "offline" until another version is activated. Confirms with warning. |
| **Rollback**    | Archived only                 | Reactivate this archived version. The currently-Activated version (if any) is auto-archived. Per Q3 — "Reactivate the archived version" — no fork-choice prompt, just reactivate directly. |
| **Duplicate**   | All states                    | Create a new Draft seeded with this version's config. |
| **Delete**      | Draft only                    | Permanently remove this draft. No effect on other versions. Confirms destructively. |

> Items not relevant to the current state are hidden, not greyed out. Per Q2, **Retire is dropped** — Archive subsumes it.

---

## 6. Interaction Walkthroughs

### 6.1 Happy path — Publishing a new version

1. User clicks **`+ New draft from this`** on the Activated v1 container.
2. A new Draft container (v2) appears at the top of the stack. User edits Connection/Mapping in their respective tabs.
3. User returns to the Status tab, sees v2 Draft container expanded.
4. User clicks the Published chevron on v2's container.
5. Confirm dialog: "Publish v2? This locks the snapshot. Activate it next when ready." → Confirm.
6. v2's chevron advances to 2/3 (Published). Manage menu now includes "Archive" (in case they want to bail). Activated chevron is now the next clickable stage.
7. User clicks the Activated chevron on v2's container.
8. Confirm dialog: "Activating v2 will archive v1 and apply the new mapping to {customer}'s integration. Continue?" → Confirm.
9. v2's chevron locks at 3/3 (Activated). **v1's container drops to the Archived section, collapsed.** v2's container shows the `+ New draft from this` button.

### 6.2 Rollback

1. User on Activated v2. Notices a regression.
2. Expands the Archived section, finds v1.
3. Opens v1's Manage menu → clicks **Rollback**.
4. Confirm dialog: "Reactivate v1 and archive v2? This applies v1's mapping to {customer}'s integration immediately. Continue?" → Confirm.
5. v1 returns to the Activated row. v2 drops to Archived.

### 6.3 Multiple drafts

1. User forks v3 from the Activated v2 → edits.
2. While iterating, user forks v4 from v2 as well (alternate approach) → edits.
3. Status tab shows both v3 and v4 as expanded Draft containers, stacked above the Activated v2. Container headers (timestamp, author) let the user tell them apart.
4. User can publish either; whichever is activated archives v2.
5. The other Draft stays a Draft; user can delete it via Manage → Delete, or keep iterating.

---

## 7. Open / Deferred

These are intentionally **not** part of this PR but flagged so we don't lose them:

1. **Diff between versions**. Showing "what changed from v1 → v2" inline in the version container body. Bigger feature — defer.
2. **Auto-fork on edit**. If usability testing shows people forget to fork before editing the Mapping tab, layer in option C (banner on edit). Not in this PR.
3. **Backend persistence**. Right now everything is mock state in the interceptor — restructure mock-data + persist on hard refresh comes after the UI is approved.
4. **Activity tab integration**. Each lifecycle event should write a row to the Activity tab. Wire up after the UI lands.
5. **Inline confirmation for low-risk actions**. Some confirms (e.g. Duplicate, Delete a Draft) could be inline mini-confirms instead of full SweetAlert modals. Polish pass.

---

## 8. Out of Scope

- The Connection tab.
- The Mapping tab.
- The Activity tab (other than the deferred integration noted above).
- The deployment-level rail/tree on the left side of customer-detail.
- Anything outside `src/app/capability/test-publish-tab.component.ts` and `src/app/shells/customer-detail.component.ts`.

---

## 9. Implementation Notes (for the developer)

- The component file is `src/app/capability/test-publish-tab.component.ts`. It currently models a single deployment as a single version; that needs to evolve into a **list of versions** with the deployment as their parent.
- The `Deployment` model in `src/app/models/deployment.model.ts` currently encodes status on the deployment itself. We'll need either a `versions: Version[]` field added, or a separate `mockVersions` table keyed by deploymentId. Recommend the latter for cleaner separation.
- The chevron should be its own small reusable component (e.g. `lifecycle-chevron.component.ts`) so it can render in both expanded and collapsed (Archived) containers.
- The "Status" tab keeps its URL key `publish-activate` for deep-link compatibility — only the label changes (it's already "Status" from the prior redesign).
- Customer name is needed for confirmation copy ("apply the new mapping to {customerName}'s integration"). Pull from the parent customer-detail's `customer()` signal via an `@Input`.

---

*This doc supersedes any prior spec for the Status tab. If the brief shifts, update this file in the same PR.*
