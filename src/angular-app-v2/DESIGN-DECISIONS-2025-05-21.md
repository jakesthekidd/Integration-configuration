# Design Decisions — May 21, 2025

**Meeting:** Integrations Platform Design Review
**Attendees:** Jake (Dev), Brian (PM)
**Context:** Follow-up review after May 19 wider team session. Stories already documented — this meeting produces targeted updates to scope and UX before next development sprint.

---

## Confirmed Decisions

### 1. Rename "Test and Publish" → "Publish and Activate"

**Decision:** The fourth capability tab is renamed from "Test and Publish" to **"Publish and Activate"**.

**Rationale:** Testing is handled separately — auth is validated via the Test Authentication button on the Connection tab; mapping is validated independently. The final tab's job is purely lifecycle management (publish a snapshot, activate it). The word "Test" in the tab name implies redundant work.

**Code action:** Rename tab label, component selector/file name, and any route references from `test-publish` → `publish-activate`.

---

### 2. Remove Test Runner from Publish and Activate Tab

**Decision:** Remove the "Run a real-order test" section from the (formerly Test & Publish) tab entirely.

**Rationale:** Redundant. Auth testing moves to the Connection tab (Test Authentication button). Mapping validation is its own concern. The Publish and Activate tab should only contain lifecycle actions: Publish, Activate, Retire, Rollback, Reactivate.

---

### 3. Add "Test Authentication" Button to Connection Tab

**Decision:** Add a **Test Authentication** button on the Connection tab, appearing after the credentials form.

**Behavior:** Validates the entered credentials against the live system endpoint. Distinct from the mapping test. Returns a pass/fail result inline.

**Code action:** Add button + mock result signal to `connection-tab.component.ts`.

---

### 4. Add "Send to Master Templates" in Publish Section

**Decision:** Add a **"Send to master templates"** option in the Publish and Activate tab.

**When available:** After a configuration has been successfully activated and is working well, PSG can promote their customer-specific fork back up to a master template for reuse.

**Rationale:** Creates a feedback loop — successful real-world configs become the starting point for future customers, reducing setup time over time.

---

### 5. Connections Table — Three New Columns

**Decision:** Add three columns to the Connections table in the Integration Library (dev side):

| Column | Detail |
|---|---|
| **Application** | Which application this connection serves (e.g., WorkflowAI, Mobile) |
| **Capability** | Which specific capability (e.g., Import Orders, Export Documents) |
| **Total Active** | Count of customers currently active on this connection. Clicking the count opens a customer list for that connection. |

**Rationale:** Before modifying a connection, devs need to know the blast radius — Total Active shows how many live customers would be affected.

**Code action:** Update `tms-systems.component.html` table + mock data to include application/capability/active count.

---

### 6. Capability Definition Clarified — One Connection = One Capability

**Decision (clarification):** Each connection ties to **one specific capability**, not multiple. A single TMS may have multiple connections — one per capability.

**Example:** Truckmate has two separate connections:
- `Truckmate → WorkflowAI / Import Orders`
- `Truckmate → Mobile / Import Orders`

These are distinct connection records with distinct credential schemas, master templates, and customer deployments. This is a data model clarification for Mohammed.

---

### 7. Customer Tab on Dev Side (Integration Library)

**Decision:** Add a **Customers** tab to the Integration Library (admin/dev side) with:
- Search bar
- Picklist pulling from all portal customers
- Ability for super admins to add customers to the system

**Rationale:** Super admins need a dedicated place to manage which portal customers are visible in the Customer Setup app. Standard PSG users get pass-through auth; super admins need this additional control surface.

---

### 8. Authentication — Password Protection for Dev Side (Option 1)

**Decision:** Implement **password protection** for the Integration Library (dev/admin side) as the MVP auth approach.

**Option chosen:** Option 1 — simpler password gate on the dev side. Option 2 (new user groups requiring other team involvement) is deferred.

**Standard users:** Pass-through authentication — no additional friction.
**Super admins / devs:** Password-protected access to the Integration Library.

---

### 9. Remove Export and Webhook Handler from Customer Capability Picker

**Decision:** Remove Export Documents and Webhook Handler from the capability picker in the Customer Setup flow.

**Rationale:** Avoids confusion in MVP. Import Orders is the sole capability for the initial rollout. Other capabilities can be re-introduced once Import Orders is stable.

**Code action:** Filter `AddDeploymentDialog` or mock capabilities data to only show Import Orders capabilities for now.

---

### 10. Remove Tonu Code Column from Customer List

**Decision:** Remove the Tonu code column from the customer list table.

**Rationale:** Location for codes/enums management is TBD — needs a separate working session. Don't display it until there's a proper home for it.

**Open question:** Where do connection-specific codes and enums live for socket access? (Lookup Tables? Separate codes section?) — requires dedicated working session.

---

### 11. Lookup Table Visibility in Mapping Tab

**Decision:** Lookup table visibility should be surfaced within the Mapping section. Currently missing.

**Detail:** When PSG is configuring a field mapping, they need to see and reference the lookup tables associated with the active connection. This is not yet wired up.

**Code action:** Add lookup table reference panel or picker to `mapping-tab.component.ts`.

---

## Things to Remove (Code Actions)

| Item | File | Status |
|---|---|---|
| Partner field | `connection-tab.component.ts` | ✓ Done locally — needs push |
| Export + Webhook Handler capabilities | `add-deployment-dialog.component.ts` / mock data | Pending |
| Test runner section | `test-publish-tab.component.ts` | Pending (tab rename first) |
| Tonu code column | `customers.component.html` | Pending |

## Things to Rename (Code Actions)

| Old | New | Files |
|---|---|---|
| "Test and Publish" tab label | "Publish and Activate" | `customer-detail.component.ts`, `test-publish-tab.component.ts` |
| `test-publish` route param | `publish-activate` | `customer-detail.component.ts` |

## Things to Add (Code Actions)

| Item | File | Priority |
|---|---|---|
| Test Authentication button + inline result | `connection-tab.component.ts` | High |
| "Send to master templates" action | `test-publish-tab.component.ts` (rename first) | Medium |
| Application, Capability, Total Active columns | `tms-systems.component.html` | Medium |
| Click-through on Total Active → customer list | `tms-systems.component.ts` | Medium |
| Customers tab in Integration Library | `admin-shell.component.ts` + new component | Medium |
| Lookup table reference in Mapping tab | `mapping-tab.component.ts` | Medium |

---

## Open Questions

| # | Question | Owner | Status |
|---|---|---|---|
| 1 | Where do connection-specific codes and enums live? (Lookup Tables vs. separate section) | Working session TBD | Open |
| 2 | Capability one-to-many clarification — confirm with Mohammed before backend modeling | Mohammed + Jake | Open |
| 3 | "Send to master templates" — what data gets promoted, and does it create a new version or replace? | Brian + Jake | Open |
| 4 | Password protection implementation — shared password or per-user? | Brian | Open |

---

*Document created: May 21, 2025 — Jake Cummings*
*Next checkpoint: Dev sprint kickoff with Mohammed (TBD)*
