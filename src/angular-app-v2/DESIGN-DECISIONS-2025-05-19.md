# Design Decisions — May 19, 2025

**Meeting:** Platform Integrations Architecture — Wider Team Review
**Attendees:** Jake (Dev), Mohammed (Lead Dev), Allison (BA), Scott (PM), additional stakeholders
**Context:** Broader concept review with the full team including the developer who will own backend implementation. First exposure for most attendees. Goal was to validate the POC direction before converting to a formal MVP backlog.

---

## Summary Verdict

The application successfully demonstrates the core workflow from customer selection through connection activation. Strong foundation with working Angular code and clear UX design. The concept is viable — the team agreed to move forward toward MVP. Missing pieces are backend integrations with the portal, expanded auth/connection management, and CRUD completeness.

**Year-end target:** 30 integrations and 50 customers live. The self-service approach is the key unlock — it allows developers to focus on building new connection types rather than reconfiguring existing ones for every customer.

---

## Confirmed Decisions

### 1. Remove Partner Field from Connection Tab

**Decision:** The Partner field is **removed** from the Connection tab configuration.

**Confirmed by:** AJ consultation post-May 13 meeting.

**Impact:** Reverses the May 13 decision to add a Partner dropdown. The field added on May 13 should be pulled from `connection-tab.component.ts`. Partner is not a needed data point at the connection level.

**Code action needed:** Remove `partnerId` signal, `partnerOptions` computed, partner dropdown from template, partner from dirty check and snapshot in `connection-tab.component.ts`.

---

### 2. Capability Rollout — Import Orders First

**Decision:** Initial MVP focuses exclusively on the **Import Orders** capability before expanding to Export Documents and Webhooks.

**Rationale:** Reduces scope for the first production deployment. Gets 30 integrations live faster by going deep on one capability rather than wide across three.

**Impact:** Mock data and demo flows should prioritize Import Orders. Other capabilities remain in the data model but are not the primary test path.

---

### 3. Angular Framework — Confirmed

**Decision:** Stick with Angular + Jake's existing codebase. No framework pivot.

**Rationale:** Working code already exists. Mohammed confirmed this is acceptable given the existing team skill set.

---

### 4. Publish → Activate Workflow — Confirmed

**Decision:** Two-step Publish/Activate workflow is confirmed by the wider team.

**Note:** The distinction wasn't immediately clear to all attendees — the UX needs to make it more explicit:
- **Publish** = save a version (can have multiple)
- **Activate** = make one version live

This distinction needs stronger visual treatment in the Test & Publish tab UI.

---

### 5. Customer List — Portal Picklist, Filtered

**Decision:** Customer list pulled from existing portal via GraphQL. Filtered to show only customers who need integration work — not all portal customers.

**Clarification from meeting:** TFX Admin is a legacy system that syncs with portal. Customer source of truth is the portal.

**Open question:** What filter criteria define "customers needing integration work"? (Portal team / Allison to clarify.)

---

## Things to Add (Backlog)

### Must-Have for MVP

| Item | Detail |
|---|---|
| **CRUD for Connections** | Currently dev section only has Delete. Need full Create + Edit + Delete. |
| **Base URL field on Connections** | Each connection type needs a configurable base URL (e.g., McLeod API endpoint, SAP host). |
| **Expanded auth method options** | Model after Postman: API Key, Bearer Token, Basic Auth, OAuth 2.0, etc. Connection type drives which auth fields appear. |
| **Test Connection button** | Dedicated button to validate credentials against the live system — separate from the mapping test. Validates auth + reachability. |
| **Super admin customer management** | Page for adding customers to the system from portal picklist. PSG cannot self-serve until a customer is added by super admin. |
| **Portal GraphQL integration** | Customer data queried from portal's GraphQL API. Replace mock customer list with real data. |
| **Portal authentication integration** | SSO/auth flow connecting Integration Configurator to existing portal session. |

### Stories Allison Will Create
1. Portal authentication integration
2. Super admin page for adding customers
3. Super admin CRUD functions in the dev section (Connections)
4. Test connection button functionality

---

## Things to Remove / Clean Up

| Item | Detail |
|---|---|
| **Partner field** | Remove from Connection tab (see Decision #1 above). |
| **POC placeholder content** | Any fake/placeholder text or data that doesn't reflect real functionality should be removed before MVP handoff. |

---

## Open Questions

| # | Question | Owner | Status |
|---|---|---|---|
| 1 | Portal filter criteria — which customers appear in the add-customer picklist? | Allison / Portal team | Open |
| 2 | Redirect vs. embedded experience — does portal launch Integration Configurator in a new tab or iframe? | Scott + Mohammed | Open |
| 3 | Authentication flow — will it be SSO from portal session or separate login? | Mohammed + Portal team | Open |
| 4 | Base URL — is it per-connection-type or per-customer-connection (can customers override it)? | Mohammed + Jake | Open |
| 5 | Jake's cloud credit constraints — does this affect demo environment or just development pacing? | Jake | Open |

---

## Architecture Notes for Mohammed

- The "Integration Configurations" feature will appear as an additional tile in the existing Transflo portal
- Two-tier architecture: **Dev Admin** (connections, templates, lookup tables) + **PSG Config** (customer deployments)
- One connection template → many customers. Developer builds `McLeod v23` once; PSG configures credentials per customer without dev involvement.
- Each customer gets their own isolated: credential set, forked mapping template, deployment lifecycle

### Concrete example from meeting:

**Chema Brokerage switching McLeod v22 → v23:**
1. PSG selects Chema from filtered portal picklist
2. Chooses WorkflowAI application
3. Selects "Import Orders" capability
4. Picks McLeod v23 connection (dev already built this)
5. Enters Chema's credentials for v23
6. Runs test → publishes → activates
7. Done — no developer involvement needed

---

## Next Steps

- [ ] Jake + [presenter] to review demo and make small edits
- [ ] Allison to write 4 stories (listed above)
- [ ] Schedule working session with Mohammed + Scott to break down dev stories
- [ ] Convert POC → MVP with detailed backlog
- [ ] Remove Partner field from Connection tab code
- [ ] Strengthen Publish vs. Activate visual distinction in UI

---

*Document created: May 19, 2025 — Jake Cummings*
*Next checkpoint: Working session with Mohammed + Scott (TBD)*
