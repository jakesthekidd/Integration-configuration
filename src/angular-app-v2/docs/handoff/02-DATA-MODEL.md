# 02 — Data Model

This is the conceptual model the UI assumes. The TypeScript interfaces in `src/app/models/*.model.ts` are the source-of-truth shapes; this doc explains how they relate and what the lifecycle invariants are.

## Entity map

```
Customer ──────────────────────────────────┐
   │                                       │ (toggled per-app)
   ▼                                       ▼
Deployment ──── refs ───→ Application      CustomerApplicationAccess
   │                          │
   │                          ▼
   │                      Capability  (Inbound | Outbound | Bidirectional)
   │
   ├── refs ────→ Connection (a.k.a. TmsSystem)
   │                  │
   │                  └── scoped to one (Application × Capability)
   │
   ├── refs ────→ MasterTemplate (FieldMappingTemplate)
   │                  │
   │                  ├── FieldMapping[]      (template rows)
   │                  └── TemplateVersion[]   (publication history)
   │
   ├── owns ────→ DeploymentMappings[]        (forked + customized rows)
   │
   └── owns ────→ Version[]                   (Draft | Published | Activated | Archived)

LookupTable (XREF) ── keyed by ──→ tmsSystemId (or tmsName via the dialog resolution)
ApiClient ────── may be assigned to ──→ Deployment
TransformationLog ─── records ───→ executed transforms per Template
```

## Cardinalities at a glance

| Relationship | Cardinality |
|---|---|
| Customer → Deployment | 1 : N |
| Customer → CustomerApplicationAccess | 1 : N |
| Application → Capability | 1 : N |
| (Application × Capability) → Connection | 1 : N |
| Deployment → forkedFrom MasterTemplate | N : 1 (or 0:1 if from scratch) |
| Deployment → DeploymentMappings | 1 : N (per-deployment customized rows) |
| Deployment → Version | 1 : N (Draft+, exactly one Activated, history Archived) |
| MasterTemplate → FieldMapping | 1 : N (the master rows) |
| MasterTemplate → TemplateVersion | 1 : N |
| LookupTable → tmsSystem | N : 1 |
| Customer × Application × Capability → Active Deployment | **1 : 1 (invariant)** |

## Key invariant — "one Active per (customer × app × capability)"

Per `PRODUCT-GUIDING-PRINCIPLES.md §4`, **only one Deployment may have status `Active` for a given `(customerId, applicationId, capabilityId)` tuple**. When a new Version is Activated on a deployment, the previously-Active version becomes Archived (see [04-DRAFT-AND-VERSIONING.md](./04-DRAFT-AND-VERSIONING.md)). The backend MUST enforce this — the UI assumes it.

---

## Core entities

### `Customer`
File: `customer.model.ts`

The tenant. Maps 1:1 to a TMS tenant by `customerId` (external) and `tmsName` (which TMS).

Key fields:
- `customerId` — external identifier (PK)
- `tmsName` — `"mcleod-v22"`, `"mcleod-v23"`, `"sap-s4"`, `"webhook"`, etc.
- `customerName` — display
- `expressCustomerCode` — internal allowlist code shown in the Library Customers tab
- `enabled` — overall enabled flag for the customer
- `integrationEnabled` — gates whether this customer is **exposed in the Customer Setup app**. Default `false` (opt-in). Toggled from Library → Customers.
- `integrationStatusChangedAt` — audit timestamp of the last toggle
- `applications` — denormalized list of application ids the customer has access to (drives the Library "Customers" pill cells)
- `credentials` — global credentials (per-deployment credentials live separately on the connection setup)

### `Application`
File: `application.model.ts`

A category of integration work — e.g. "WorkflowAI", "MobileApp Loads", "Documents Pipeline".

Key fields: `id`, `displayName`, `description`, `isActive`.

### `Capability`
File: `capability.model.ts`

A unit of work within an application — e.g. "Import Loads", "Status Updates", "Document Ingest".

Key fields:
- `id`
- `applicationId` — parent app
- `direction` — `'Inbound' | 'Outbound' | 'Bidirectional'`
- `displayName`, `description`

### `TmsSystem` (aka **Connection**)
File: `tms-system.model.ts`

A "Connection" in the UI is a `TmsSystem` row. Scoped to **exactly one** `(applicationId, capabilityId)` pair.

Key fields:
- `id`, `name`, `displayName`, `version`
- `applicationId`, `capabilityId` — what this connection is for
- `connectionConfig` — JSON describing required credential fields, endpoint URLs, etc.
- `sampleJsonSchema` — sample payload for the template picker / preview

### `Deployment`
File: `deployment.model.ts`

The activation bundle. One deployment = one customer's setup of one (app × capability) using one connection and one template fork.

Key fields:
- `id`, `customerId`, `applicationId`, `capabilityId`, `connectionId`
- `forkedFromTemplateId` — `""` if from scratch
- `forkedFromTemplateVersion` — `null` if from scratch
- `apiClientId` — optional API client identity
- `status` — see Deployment Status below
- `snapshotVersion` — incremented on each Publish
- `lastTestCorrelationId` — for the Activity tab

#### Deployment status enum

```ts
type DeploymentStatus = 'Draft' | 'Tested' | 'Published' | 'Active' | 'Retired';
```

| Value | Meaning |
|---|---|
| `Draft` | Brand new, never published. |
| `Tested` | A successful real-order test has run (legacy — currently de-emphasized). |
| `Published` | Has at least one Published version but is not yet activated for the (customer × app × cap) tuple. |
| `Active` | The chosen deployment for this (customer × app × cap). At most one. |
| `Retired` | Decommissioned. |

Status on the **Deployment** is the rollup of the version state machine, not the source of truth. The per-version `state` is the SoT.

### `FieldMappingTemplate` (MasterTemplate)
File: `template.model.ts`

A reusable "starter" defined in the Library. Customers' deployments fork from these.

Key fields: `id`, `name`, `description`, `version`, `status`, `latestVersionStatus`, `sourcePartnerId/Name`, `targetPartnerId/Name`, `sampleInputJson`.

`status` is one of `'Draft' | 'Published' | 'Archived'` (string-typed in the model; the picker only shows `Published` ones).

### `TemplateVersion`
File: `template.model.ts`

A publication record for a MasterTemplate. Doesn't carry its own rows in the prototype — the rows live under the master itself.

### `FieldMapping`
File: `field-mapping.model.ts`

One row in a template (or in a deployment's customized rows). The atomic unit of "this source path translates to this target path".

Key fields:
- `id`, `templateId`
- `sourcePath`, `targetPath`
- `transformationType` — see enum below
- `isRequired`, `defaultValue`
- `transformationConfig` — JSON for the picked transformation type
- `validationRules` — optional JSON

#### Transformation types

```ts
const TransformationTypes = [
  'Direct',
  'Concat',
  'Lookup',
  'Conditional',
  'ArrayMap',
  'ArrayFlatten',
  'DateFormat',
  'Math',
  'Substring',
  'Constant',
  'Template',
  'PrefixMap',
  'ConditionalDateFormat',
];
```

The prototype renders these in a `p-select` dropdown but **does not implement** the transformation logic itself — actual evaluation must happen server-side. The "Test with sample JSON" panel on the Mapping tab is a stub that confirms parse + count only.

### `DeploymentMappings` (interceptor-side)

There's no model file — these are stored in the mock interceptor as `Record<deploymentId, FieldMapping[]>`. Conceptually, this is a separate table in production:

```
deployment_mappings
├── deployment_id   (FK → deployments)
├── source_path
├── target_path
├── transformation_type
├── is_required
└── default_value
```

Forked from a master template but **divergent** — edits don't propagate back.

### `Version`
File: `version.model.ts`

The snapshot of (Connection + Mapping) on a Deployment. **Most important model in the system.** See [04-DRAFT-AND-VERSIONING.md](./04-DRAFT-AND-VERSIONING.md) for the full lifecycle.

Key fields:
- `id`, `deploymentId`, `versionNumber` (1, 2, 3, …)
- `state` — `'Draft' | 'Published' | 'Activated' | 'Archived'`
- `createdAt`, `createdBy`
- `publishedAt`, `activatedAt`, `archivedAt` — set on the respective transitions
- `notes` — optional one-line change summary
- `basedOnVersionNumber` — for Drafts, which version they were forked from

### `LookupTable` (cross-reference table)
File: `lookup-table.model.ts`

A key→value translation table used by `Lookup`-type field mappings (and surfaced read-only by the Reference Tables dialog).

Key fields:
- `id`, `tmsSystemId` (overloaded — see [06-FEATURES.md → Reference tables](./06-FEATURES.md#reference-tables-dialog))
- `fieldName` — e.g. `"loadStatus"`
- `mappings` — **JSON string** of `{ sourceCode: platformValue }`
- `defaultValue` — fallback when source not in table
- `isCaseSensitive`
- `name`, `description`

**Heads up:** `mappings` is stored as a JSON-encoded string, not a real `Record`. Parse before reading.

### `ApiClient`
File: `api-client.model.ts`

A machine identity that can be assigned to a deployment for inbound webhooks / API auth. Mostly read-only in the UI today.

### `Partner`
File: `partner.model.ts`

Source/target organization context on a MasterTemplate (e.g. "C.H. Robinson" as the source partner). Display-only in the prototype.

### `TransformationLog` / `TransformationLogDetail`
File: `transformation-log.model.ts`

A row in the Activity tab. The detail has `inputData` / `outputData` strings for the drill-in view.

---

## Lifecycle state machines

### Customer enablement

```
created (integrationEnabled = false)
   │
   │ Library admin toggles "Enable in Customer Setup"
   ▼
integrationEnabled = true, integrationStatusChangedAt = now()
   │
   │ Customer now appears in the Customer Setup app
   ▼
[user opens /customers/:id, configures deployments]
```

### Deployment / Version lifecycle (summary)

Full detail in [04-DRAFT-AND-VERSIONING.md](./04-DRAFT-AND-VERSIONING.md). The short version:

```
                ┌────────────┐
                │   Draft    │  ←—— auto-forked from Active (or seeded fresh)
                └─────┬──────┘
                      │ user clicks "Publish"
                      ▼
                ┌────────────┐
                │ Published  │  ←—— ready to test in pre-prod
                └─────┬──────┘
                      │ user clicks "Activate"
                      ▼
                ┌────────────┐
                │ Activated  │  ←—— the live one. At most 1 per (cust × app × cap)
                └─────┬──────┘
                      │ another version gets Activated
                      ▼
                ┌────────────┐
                │  Archived  │  ←—— immutable history
                └────────────┘
```

---

## Reference: model file → entities

| File | Exports |
|---|---|
| `application.model.ts` | `Application`, `ApplicationListResponse` |
| `capability.model.ts` | `Capability`, `CapabilityDirection`, `CapabilityListResponse` |
| `customer.model.ts` | `Customer`, `Credential`, `CustomerRequest`, `CustomerListResponse` |
| `tms-system.model.ts` | `TmsSystem`, `CreateTmsSystemRequest` |
| `deployment.model.ts` | `Deployment`, `DeploymentStatus`, `DeploymentListResponse` |
| `template.model.ts` | `FieldMappingTemplate`, `CreateTemplateRequest`, `UpdateTemplateRequest`, `TemplateListResponse`, `TemplateVersionResponse` |
| `field-mapping.model.ts` | `FieldMapping`, `Create/UpdateFieldMappingRequest`, `FieldMappingListResponse`, `TransformationTypes` |
| `lookup-table.model.ts` | `LookupTable`, `Create/UpdateLookupTableRequest`, `LookupTableListResponse` |
| `version.model.ts` | `Version`, `VersionState` |
| `api-client.model.ts` | `ApiClient`, `Create/UpdateApiClientRequest`, `ApiClientListResponse`, `ApiClientTemplateAssignmentRequest` |
| `partner.model.ts` | `Partner`, `CreatePartnerRequest`, `PartnerListResponse` |
| `transformation-log.model.ts` | `TransformationLogSummary`, `TransformationLogDetail` (read-only) |
| `transformation-test.model.ts` | Test-execution shapes (for Activity drill-in) |

---

## Production schema notes

A few shape decisions in the prototype that the production schema will likely want to change:

1. **`LookupTable.tmsSystemId` is overloaded.** In existing seeds it holds a connection id; in newer Truck Mate seeds it holds a `tmsName`. Production should split these into either two FK columns or a single FK to `tms_systems`.
2. **`LookupTable.mappings` is a JSON string.** A proper relational design has a child `lookup_table_entries(table_id, source_code, target_value)` table.
3. **`DeploymentMappings` has no model file** — only an in-interceptor `Record`. Promote to a proper table on the backend.
4. **`Customer.applications` is denormalized** for UI display speed. The truth lives in `CustomerApplicationAccess`.
5. **Dates are mixed `Date` and `string`.** The HTTP layer always returns strings (ISO); some models still type these as `Date`. Standardize on ISO strings everywhere and have a tiny helper convert at the UI edge if needed.
