# Design Decisions — May 13, 2025

**Meeting:** Field Mapper Productization Review
**Attendees:** Jake (Dev), Brian (PM/Lead Dev)
**Context:** Pre-architecture review checkpoint. Brian flagged that a working session with Scott and Allison is scheduled for next week to review best practices — decisions made here are directionally correct but implementation details may shift after that session.

---

## Status of Current Build

The current `angular-app-v2` prototype was reviewed and received positive feedback on:

- Overall design pattern and visual hierarchy (aligns with company standards)
- Customer-level credential setup approach (logical, understandable)
- Two-surface separation (Customer Setup vs. Integration Library / Developer surface)
- Template forking concept for mapping customization

---

## Confirmed Decisions

### 1. Two-Surface Architecture — Keep and Formalize

**Decision:** Maintain the hard split between two distinct UX surfaces:

| Surface | Persona | Scope |
|---|---|---|
| **Customer Setup** | PSG / Professional Services | Configure integrations per customer |
| **Developer Tools** | Engineering | Create connections, templates, lookup tables |

**Rationale:** The relationship between the two wasn't immediately obvious to reviewers — this needs to be surfaced more clearly in the UI (header context, labeling, etc.).

---

### 2. Terminology Change: "TMS Systems" → "Connections"

**Decision:** Remove "TMS Systems" language everywhere. Rename throughout:

| Old | New |
|---|---|
| TMS Systems | Connections |
| Create new system | Create new connection |
| TMS System picker | Connection picker |

**Scope:** Applies to nav labels, form labels, API display names, and any table column headers. Backend model names are out of scope for now.

---

### 3. Remove "Integrations" Tab

**Decision:** The standalone "Integrations" tab/section is eliminated. Everything consolidates under "Connections."

**Rationale:** The distinction between Integrations and Connections created confusion. The Connections surface already captures the necessary relationship between customers, apps, and data routing.

---

### 4. Remove Standalone "Partners" Management UI

**Decision:** Partners are no longer managed as a top-level entity. Instead, a **partner dropdown** is added at the connection setup level.

- The dropdown is populated and maintained by developers (Developer Tools surface)
- PSG users select a partner when configuring a connection — they do not create or manage partners directly

---

### 5. Publish → Activate Workflow (Two-Step, Versioned)

**Decision:** Formalize the two-step lifecycle for mappings:

```
Draft → Published → Active
```

**Key rules:**
- **Publish** creates a versioned snapshot of the current mapping. It does NOT make it live.
- **Activate** makes a specific published version live and retires any prior active version for the same (Customer × App × Capability) tuple.
- Published (but inactive) versions remain visible in a **versioned mapping list** so PSG users can reference prior configurations.
- This prevents developers from being interrupted for minor mapping tweaks — PSG can publish + activate independently.

**New UI requirement:** A published mappings list/grid must be visible in the Mapping tab before the Activate action, showing version number, publish date, and who published it.

---

### 6. Tenant Picker — New Addition

**Decision:** Add tenant-level selection to support customers with multiple environments or business units (e.g., a carrier that also has a broker division, or prod vs. sandbox environments).

**Placement:** Tenant picker sits at the **application level** within the customer detail tree view — selecting an app asks "which tenant/environment?" before loading capabilities.

**Open question:** How tenants are defined and managed (by devs? auto-discovered from portal?) — to be resolved after architecture review.

---

### 7. Customer List — Manual Activation, Not Auto-Population

**Decision:** Replace auto-populating all portal customers with an **"Add Customer" button + dropdown** that pulls from existing portal records.

**Rationale:** Auto-populating all portal customers creates noise (POC/fake records, inactive accounts). PSG explicitly activates a customer in the tool when they're ready to configure them.

**Open question:** Filtering criteria for the portal records dropdown (active only? by account type?).

---

## Things Explicitly Kept As-Is

- Visual hierarchy and layout structure
- Stage layout (navy header, white card overlap)
- Customer-level credential setup pattern
- Connection credential schema approach (schema-driven form per connection type)
- Tree-view left rail (App → Capability) + right pane (tabs)
- The four-tab structure per capability (Connection, Mapping, Test & Publish, Activity)

---

## Deferred to Architecture Review (Next Week)

The following should **not** be built out until after Scott and Allison's session:

- How tenants are modeled in the backend and how they relate to customers
- Whether the Customer Setup and Developer Tools surfaces are deployed as one app or two separate apps
- Integration mechanism with existing portal customer records (API? shared DB? event sync?)
- Visual Connections page in Workflow AI settings (noted as Phase 3, not yet scoped)

---

## Open Questions

| # | Question | Owner | Status |
|---|---|---|---|
| 1 | What specific information is needed in the Test & Publish section? | Jake + Brian | Open |
| 2 | How is the partner dropdown populated — dev-only CRUD or synced from somewhere? | Brian | Open |
| 3 | Should customers require manual activation or is there a smarter filter on portal records? | PM | Open |
| 4 | How are tenants defined — by environment (prod/sandbox), by business unit, or both? | Architecture Review | Deferred |
| 5 | One app or two deployed surfaces post-architecture review? | Scott + Allison | Deferred |

---

## Immediate Next Steps (Pre-Architecture Review)

- [ ] Rename "TMS Systems" → "Connections" throughout the UI
- [ ] Remove "Integrations" tab from Developer Tools nav
- [ ] Replace standalone Partners management with a Partners dropdown on the connection form
- [ ] Add versioned mappings list/grid to the Mapping tab (published versions, not yet active)
- [ ] Add tenant picker at the application level in the customer detail tree view
- [ ] Replace the auto-populated customer list with "Add Customer" button + portal dropdown
- [ ] Update `PRODUCT-GUIDING-PRINCIPLES.md` to reflect these decisions post-architecture review

---

*Document created: May 13, 2025 — Jake Cummings*
*Next review checkpoint: Post-architecture working session (week of May 19)*
