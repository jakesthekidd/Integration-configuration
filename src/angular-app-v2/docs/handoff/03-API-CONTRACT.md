# 03 — API Contract

Every HTTP call in the app goes through `ApiService` (`src/app/services/api.service.ts`) and is intercepted by `mockApiInterceptor` (`src/app/mocks/mock-api.interceptor.ts`). To go to production, the backend must implement the endpoints in this doc with the request/response shapes shown.

The mock is **not** a spec — it's a stub. Where the prototype gets away with synthetic success or in-memory mutation, this doc calls out what real semantics the backend must provide.

---

## Base URL & envelope

```ts
// src/environments/environment.ts
apiUrl: '/api/v1'
```

Every endpoint is rooted under `/api/v1`. The standard response envelope is:

```ts
interface ApiResponse<T> {
  success: boolean;
  data?: T;
  message?: string;     // human-readable, used for error toasts
  errors?: string[];    // optional field-level errors
}
```

The mock always returns `success: true`. Production must return `success: false` with a `message` for client-visible errors.

A few legacy endpoints (e.g. `parseJson`, `transformJsonWithTemplate`) return raw `any` instead of the envelope. New endpoints should always use the envelope.

---

## Authentication

Currently: **none.** No `Authorization` header is sent. `auth.service.ts` is a stub.

Production must:
1. Inject an `Authorization: Bearer <token>` header on every request (use an Angular `HttpInterceptor` chained before the (deleted) `mockApiInterceptor`).
2. Handle `401` globally — redirect to SSO sign-in.
3. Add per-route role checks (e.g. only certain users can publish/activate).

The one place the UI already discriminates by client identity is the transform endpoint:

```
x-client-id: <apiClientId>
```

…sent on `POST /templates/:id/versions/:v/transform`. Keep that header in production.

---

## Endpoints

The table groups endpoints by resource. **All methods listed are implemented in the mock** unless flagged "stub" (synthetic success only).

### Applications

| Method | Path | Returns | Notes |
|---|---|---|---|
| GET | `/applications` | `{ applications: Application[], totalCount }` | |
| GET | `/applications/:id` | `Application` | |
| GET | `/applications/:id/capabilities` | `{ capabilities: Capability[], totalCount }` | |

### Capabilities

| Method | Path | Returns | Notes |
|---|---|---|---|
| GET | `/capabilities` | `{ capabilities, totalCount }` | Filter by `?applicationId=` |
| GET | `/capabilities/:id` | `Capability` | |

### Customers

| Method | Path | Returns | Notes |
|---|---|---|---|
| GET | `/customers` | `{ customers, totalCount }` | `?activeOnly=true\|false` |
| GET | `/customers/:id` | `Customer` | |
| POST | `/customers` | `Customer` | mock: synthetic id |
| PUT | `/customers/:id` | `Customer` | mock: synthetic success |
| DELETE | `/customers/:id` | `void` | mock: synthetic success |
| PATCH | `/customers/:id/status?enabled=…` | `Customer` | mock: synthetic success |

The **integration-enabled toggle** in the Library is currently client-side only (`customer-access.service.ts`). Production should persist it via PATCH or a dedicated endpoint, e.g. `PATCH /customers/:id?integrationEnabled=true`.

### TMS Systems (a.k.a. Connections)

| Method | Path | Returns | Notes |
|---|---|---|---|
| GET | `/tms-systems` | `{ systems: TmsSystem[], totalCount }` | `?activeOnly=true\|false` |
| GET | `/tms-systems/:id` | `TmsSystem` | |
| POST | `/tms-systems` | `TmsSystem` | mock: synthetic |
| PUT | `/tms-systems/:id` | `TmsSystem` | mock: synthetic |
| DELETE | `/tms-systems/:id` | `void` | mock: synthetic |

### Deployments

| Method | Path | Returns | Notes |
|---|---|---|---|
| GET | `/deployments` | `{ deployments, totalCount }` | `?customerId=` filter; supported both via `HttpParams` and inline query string |
| GET | `/deployments/:id` | `Deployment` | |
| POST | `/deployments` | `Deployment` | mock **does** push to fixture; survives navigation, not reload |
| GET | `/deployments/:id/mappings` | `{ mappings: FieldMapping[], totalCount }` | per-deployment customized rows; mock-backed (see [05-STATE-MANAGEMENT.md](./05-STATE-MANAGEMENT.md#mock-backed-persistence-pattern)) |
| PUT/POST | `/deployments/:id/mappings` | `{ deploymentId, count }` | persists mappings + stamps `forkedFromTemplateId/Version` |

**Production must add:**
- `PATCH /deployments/:id` — update status, apiClientId, connection
- `DELETE /deployments/:id` — soft-delete / retire
- `GET /deployments/:id/versions` — version history (currently the version data is generated client-side; see [04-DRAFT-AND-VERSIONING.md](./04-DRAFT-AND-VERSIONING.md))
- `POST /deployments/:id/versions` — fork a new Draft
- `POST /deployments/:id/versions/:n/publish`
- `POST /deployments/:id/versions/:n/activate`
- `POST /deployments/:id/versions/:n/archive`

The prototype does all version-state transitions in `test-publish-tab.component.ts` against an in-memory `mockVersions: Record<deploymentId, Version[]>`. **This is the biggest API surface the prototype doesn't reach** — design these endpoints carefully (see [04-DRAFT-AND-VERSIONING.md](./04-DRAFT-AND-VERSIONING.md#backend-contract)).

### MasterTemplates

| Method | Path | Returns | Notes |
|---|---|---|---|
| GET | `/templates` | `{ templates, totalCount }` | filters: `?tmsSystemId=`, `?page=`, `?pageSize=`, `?status=` |
| GET | `/templates/:id` | `FieldMappingTemplate` | |
| GET | `/templates/:id/versions` | `TemplateVersion[]` | |
| GET | `/templates/:id/versions/:v` | `FieldMappingTemplate` | template at a specific version |
| POST | `/templates` | `FieldMappingTemplate` | stub |
| PUT | `/templates/:id` | `FieldMappingTemplate` | stub |
| DELETE | `/templates/:id` | `void` | `?version=` to delete one version |
| POST | `/templates/:id/versions` | `any` | `{ baseVersion }` body; stub |
| POST | `/templates/:id/versions/:v/publish` | `any` | stub |
| DELETE | `/templates/:id/versions/:v` | `any` | stub |
| POST | `/templates/:id/duplicate` | `FieldMappingTemplate` | `{ includeAllVersions }`; stub |
| POST | `/templates/:id/reactivate` | `void` | stub |

### Field Mappings (template-level rows)

| Method | Path | Returns | Notes |
|---|---|---|---|
| GET | `/field-mappings` | `{ mappings, totalCount }` | filter `?templateId=` (+ optional `&templateVersionId=`) |
| GET | `/field-mappings/:id` | `FieldMapping` | |
| POST | `/field-mappings` | `FieldMapping` | stub |
| PUT | `/field-mappings/:id` | `FieldMapping` | stub |
| DELETE | `/field-mappings/:id` | `void` | stub |
| DELETE | `/field-mappings/template/:templateId` | `void` | stub |

Note the distinction:
- **`/field-mappings`** — rows belonging to a **MasterTemplate** (Library-owned, shared)
- **`/deployments/:id/mappings`** — rows belonging to a **Deployment** (per-customer, customized fork)

### Lookup Tables (cross-reference tables)

| Method | Path | Returns | Notes |
|---|---|---|---|
| GET | `/lookup-tables` | `{ lookupTables, totalCount }` | `?tmsSystemId=` filter |
| GET | `/lookup-tables/:id` | `LookupTable` | |
| POST | `/lookup-tables` | `LookupTable` | stub |
| PUT | `/lookup-tables/:id` | `LookupTable` | stub |
| DELETE | `/lookup-tables/:id` | `void` | stub |

### API Clients

| Method | Path | Returns | Notes |
|---|---|---|---|
| GET | `/apiclients` | `{ apiClients, totalCount }` | |
| GET | `/apiclients/:id` | `ApiClient` | |
| GET | `/apiclients/:id/templates` | `TemplateVersionResponse[]` | assigned templates |
| POST | `/apiclients` | `ApiClient` | stub |
| PUT | `/apiclients/:id` | `ApiClient` | stub |
| DELETE | `/apiclients/:id` | `void` | stub |
| POST | `/apiclients/:id/templates` | `void` | `{ templateVersionId }`; stub |
| DELETE | `/apiclients/:id/templates/:templateVersionId` | `void` | stub |

### Partners

| Method | Path | Returns | Notes |
|---|---|---|---|
| GET | `/partners` | `{ partners, totalCount, page, pageSize }` | always returns full list in mock |
| POST | `/partners` | `Partner` | stub |
| DELETE | `/partners/:id` | `void` | stub |

### Transformation Logs

| Method | Path | Returns | Notes |
|---|---|---|---|
| GET | `/transform-logs` | `{ logs, totalCount }` | filters: `?templateId=`, `?status=`, `?limit=`, `?from=`, `?to=` |
| GET | `/transform-logs/:id` | `TransformationLogDetail` | with `inputData`, `outputData`, `errors` |

### Transformation execution (legacy)

| Method | Path | Returns | Notes |
|---|---|---|---|
| POST | `/templates/:id/versions/:v/transform` | `any` | header `x-client-id`; the real transform engine endpoint |
| POST | `/json/parse` | `any` | `{ jsonString, includeSampleValues }`; helper for the Library JSON tool |

---

## Endpoints the prototype calls but doesn't have

The Mapping & Publish tabs operate on **client-only state** for these — production needs real endpoints:

1. **Version state machine** — the entire `Version[]` per deployment is generated and mutated client-side. See [04-DRAFT-AND-VERSIONING.md](./04-DRAFT-AND-VERSIONING.md#backend-contract).
2. **Connection persistence** — the Connection tab's `save()` is currently a `setTimeout` stub. There's no `PUT /deployments/:id/connection` or equivalent. Production needs an endpoint to persist `connectionId + credentials` per deployment.
3. **Test Authentication** — the "Test Authentication" button on the Connection tab is a random pass/fail timer. Production needs `POST /deployments/:id/test-auth` that actually hits the connected system with the supplied credentials.
4. **Customer integration-enabled toggle** — see Customers section above.
5. **Real transformation preview** — the Mapping tab's "Test with sample JSON" panel only parses and counts. Wire to `POST /templates/:id/versions/:v/transform` or a deployment-scoped equivalent.

---

## Mock interceptor cheat sheet

What the mock actually does:

- `GET *` → reads from `mock-data.ts` arrays. Returns `ok({ ... })` with a 120ms delay.
- `POST/PUT/PATCH/DELETE *` → mostly returns synthetic success. Exceptions:
  - `POST /deployments` — pushes to `mockDeployments` array
  - `PUT/POST /deployments/:id/mappings` — writes to `mockDeploymentMappings` record and stamps the deployment's `forkedFromTemplateId/Version + updatedAt`

State **survives navigation** within the SPA and **resets on full page reload** (mock data lives in module-level arrays).

The Version[] state lives separately in `test-publish-tab.component.ts` and a `mockVersions` record there — *not* in the interceptor. This is a wart of the prototype that the production migration cleans up by introducing real version endpoints.

---

## Response shape conventions

- **Lists** wrap in an object: `{ items: T[], totalCount: number }`. Use a consistent key name per resource (`customers`, `templates`, `applications`, etc.). The prototype isn't consistent here — settle on one (recommend `items`) when designing the real API.
- **IDs** are strings, not UUIDs in the mock (`cust-001`, `tmpl-001`, `depl-…`). Production should use real UUIDs.
- **Dates** are ISO 8601 strings on the wire. Some models still type them as `Date` — see [02-DATA-MODEL.md](./02-DATA-MODEL.md#production-schema-notes).
- **Errors** should populate `{ success: false, message, errors }` instead of throwing 4xx/5xx in many cases — the prototype expects to read `response.body.success`. (You can also use HTTP status codes; the global error handler that production needs should handle both.)

---

## Order of work for backend wiring

Suggested priority for the API team:

1. **Auth** — token middleware, role checks
2. **Customers + integration-enabled toggle** — gates the whole Customer Setup app
3. **Deployments + per-deployment mappings** — the daily workhorse
4. **Versions + state machine** — the highest-risk feature ([04-DRAFT-AND-VERSIONING.md](./04-DRAFT-AND-VERSIONING.md))
5. **Connection persistence + Test Authentication** — completes the Connection tab story
6. **Templates + field-mappings + lookup-tables** — Library editing (less urgent if Library is admin-only)
7. **Transformation logs + execute endpoint** — Activity tab + real-data testing
8. **API Clients** — assignment workflow
