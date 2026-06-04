# 05 — State Management

The prototype uses **Angular signals** as the primary reactivity primitive. No NgRx. No Akita. No Signal-Store library. Just plain `signal()`, `computed()`, `effect()` from `@angular/core`, with small injectable services holding shared state.

This doc covers:
- The conventions to follow when adding to this codebase
- The "mock-backed persistence" pattern that makes the prototype feel real
- Two gotchas the prototype hit (hydration race, effect-only-when-mounted) that the developer should know before porting to a real backend

---

## Signal vocabulary used here

| Primitive | Purpose | Example |
|---|---|---|
| `signal<T>(initial)` | Mutable reactive value | `mappings = signal<MappingRow[]>([])` |
| `computed(() => …)` | Derived signal that re-evaluates on dependency change | `dirty = computed(() => deepEq(snapshot(), current()))` |
| `effect(() => …)` | Side-effect runner — fires on dependency change | mirror-write `mockVersions[id] = list` |
| `signal.set(v)` | Replace value | `selectedTab.set('mapping')` |
| `signal.update(fn)` | Functional update | `mappings.update(rows => [...rows, newRow])` |

Use signals for **component state and cross-component coordination**. Keep RxJS for HTTP.

---

## Convention: component state shape

Every editor tab follows the same pattern:

```ts
@Component({ ... })
export class ConnectionTabComponent implements OnChanges {
  @Input({ required: true }) deployment!: Deployment;
  @Output() saved = new EventEmitter<void>();

  // 1. The editable state
  credentials = signal<Record<string,string>>({});
  connectionId = signal<string | null>(null);

  // 2. The baseline ("what's saved") — used by dirty()
  private snapshot = signal<{ connectionId: string | null; credentials: Record<string,string> }>({
    connectionId: null,
    credentials: {},
  });

  // 3. Derived flags
  dirty = computed(() => !deepEq(this.snapshot(), this.current()));
  saving = signal<boolean>(false);

  // 4. ngOnChanges hydrates from props/API
  ngOnChanges(changes: SimpleChanges) {
    if (changes['deployment']) this.load();
  }

  private load() {
    // fetch from API, set credentials() + connectionId(), then snapshot.set(...) as baseline
  }

  // 5. save() round-trips through ApiService, on success resets snapshot
  save() {
    this.api.saveX(...).subscribe({
      next: () => {
        this.snapshot.set({ /* current values */ });
        this.gen.success('Saved.');
        this.saved.emit();
      },
    });
  }
}
```

Why this shape:

- **`snapshot()` is the SoT for "is the user dirty?"** — `dirty()` compares snapshot to current. No "isDirty" boolean to keep in sync.
- **`saved` event** lets the parent shell react (e.g. auto-advance to the next tab) without the child knowing about the parent's nav model.
- **`load()` runs on `ngOnChanges('deployment')`** — when the parent picks a different deployment, the tab re-hydrates.

---

## The four cross-cutting services

### `ApiService`
File: `src/app/services/api.service.ts`. All HTTP. Returns `Observable<ApiResponse<T>>`. See [03-API-CONTRACT.md](./03-API-CONTRACT.md).

### `DraftService`
File: `src/app/services/draft.service.ts`. **The most important non-HTTP service.** Cross-tab coordination — see [01-ARCHITECTURE.md → DraftService](./01-ARCHITECTURE.md#cross-tab-state-coordination--the-draftservice) and [04-DRAFT-AND-VERSIONING.md](./04-DRAFT-AND-VERSIONING.md#draftservice--the-cross-tab-signal-bus). All state is `signal<Record<deploymentId, …>>()` so it's per-deployment.

### `CustomerAccessService`
File: `src/app/services/customer-access.service.ts`. Holds the in-memory per-customer enablement state (which apps a customer has access to, which are toggled on for the customer-setup app). Currently writes to in-memory state only — production must persist.

### `GeneralService`
File: `src/app/services/general.service.ts`. Thin wrapper around PrimeNG's `MessageService` with `success()`, `error()`, `info()`, `warn()` helpers. Use these instead of injecting `MessageService` directly so the call sites stay consistent.

---

## Mock-backed persistence pattern

The prototype's most important persistence pattern. When you want a save to **survive navigation**, you must round-trip it through the API even though the API is mocked.

### Bad — local-only save (causes bugs)

```ts
save() {
  this.snapshot.set({ /* current */ });  // ← only updates this component
  this.saved.emit();
}
```

On navigation away and back, `ngOnChanges` fires, `load()` re-reads from the API which has no record of the save, and the editor renders empty. The user thinks their work was lost.

### Good — round-trip through API

```ts
save() {
  this.api.saveDeploymentMappings(this.deployment.id, { ... }).subscribe({
    next: () => {
      this.snapshot.set({ /* current */ });
      this.saved.emit();
    },
  });
}
```

For this to work, the mock interceptor has to actually persist:

```ts
// mock-api.interceptor.ts
const mockDeploymentMappings: Record<string, FieldMapping[]> = {};

if ((method === 'PUT' || method === 'POST') && segs[0] === 'deployments' && segs[2] === 'mappings') {
  mockDeploymentMappings[deploymentId] = (body.mappings ?? []).map(m => ({ ...m }));
  return ok({ deploymentId, count: ... });
}

if (method === 'GET' && segs[0] === 'deployments' && segs[2] === 'mappings') {
  return ok({ mappings: mockDeploymentMappings[segs[1]] ?? [], totalCount: ... });
}
```

This is **the** pattern. When you add a new "save X to a deployment" feature:
1. Add a method on `ApiService` for the GET + PUT/POST
2. Add the matching routes to `mockApiInterceptor`
3. Add a `Record<string, T>` to the interceptor to back it
4. Wire the component to call the API on both load and save

In production, you delete (3) and the routes, and the `ApiService` calls hit the real server. The components don't change.

---

## Gotcha 1: hydration race

When using an `effect()` to mirror a signal back to external state, the effect runs **immediately on subscription** with the current (possibly empty) value, **before** your hydration code has loaded the seed.

This bit the version persistence:

```ts
// ❌ WRONG — initial empty fire clobbers mockVersions
constructor() {
  effect(() => {
    const list = this.versions();              // ← runs with [] on construction
    mockVersions[this.deployment.id] = list;   // ← overwrites the real seed!
  });
}

ngOnChanges() {
  this.versions.set(mockVersions[this.deployment.id] ?? []);  // too late
}
```

**Fix:** gate the effect on a `hydrated` flag:

```ts
private hydrated = signal<boolean>(false);

constructor() {
  effect(() => {
    const id = this.deployment?.id;
    if (!id) return;
    const list = this.versions();
    if (!this.hydrated()) return;  // ← gate
    mockVersions[id] = list.map(v => ({ ...v }));
  });
}

ngOnChanges() {
  this.hydrated.set(false);
  this.versions.set(seed.map(v => ({ ...v })));
  this.hydrated.set(true);  // now subsequent edits will mirror
}
```

**In production with a real backend you should NOT need a mirror effect at all** — each mutation should call its own write endpoint. The mirror is a mock-only crutch.

---

## Gotcha 2: effects only run while the component is mounted

`effect()` is destroyed when its component is destroyed. This matters for the auto-fork pattern:

The Mapping tab calls `DraftService.requestSpawnDraft(id)` to bump a counter. The Publish & Activate tab has an `effect()` on that counter that seeds a Draft row. **If the user never navigates to Publish & Activate, the effect never runs.**

This was intentionally OK because:
1. The amber draft banner is driven by `hasDraft(id)` — set directly by `DraftService.setDraft(id, true)` in `ensureDraft()`, no effect needed.
2. When the user finally visits Publish & Activate, the effect on the counter catches up and seeds the row.

But if you add a new cross-tab coordination, **don't assume the consuming effect is always running**. Either:
- Set the consumer's state directly from the producer side (like `setDraft`), or
- Have the consumer reconcile on mount (like Publish & Activate does), or
- Move the coordination into a service-level `computed()` that's always alive.

---

## Gotcha 3: don't mutate signal contents in place

Signals only fire on identity change. Mutating an array in place doesn't notify subscribers:

```ts
// ❌ WRONG — does not trigger reactivity
const rows = this.mappings();
rows.push(newRow);

// ✅ RIGHT
this.mappings.update(rows => [...rows, newRow]);

// ✅ ALSO RIGHT — explicit replace
this.mappings.set([...this.mappings(), newRow]);
```

Same for objects: spread to a new object rather than assigning a property.

---

## Gotcha 4: `computed()` is fine for snapshots; don't use it for expensive renders

Computeds are memoized but re-evaluate eagerly on any dependency change. Don't put 1000-row JSON.stringify inside a computed that fires on every keystroke.

The prototype is small enough that this isn't a problem, but worth noting before the data scale grows.

---

## Bridging signals ↔ RxJS

The app calls `subscribe()` directly inside components rather than using `toSignal()` / `toObservable()`. This is fine for now. If you want to bridge:

```ts
import { toSignal } from '@angular/core/rxjs-interop';

deployments = toSignal(this.api.getDeployments(), { initialValue: [] });
```

…but be aware of the caveats around `injectionContext` and the lack of error handling unless you wrap it.

---

## When state belongs in a service vs a component

**Component** when:
- Only used by that one component
- Doesn't survive navigation (re-derives from props on `ngOnChanges`)

**Service** when:
- Two or more components need to read it
- It must survive navigation between sibling components in the shell
- It represents a domain concept (e.g. "is there a draft for this deployment?")

The current services in this app are deliberately tiny — `DraftService` is ~70 lines. Resist the urge to centralize everything into a god-service.

---

## What needs to change for production

1. **Delete the mock interceptor.** All HTTP goes to a real backend.
2. **Delete the mirror effect in `test-publish-tab.component.ts`.** Replace with direct API calls on each mutation (publish, activate, archive, delete-draft).
3. **Add a global HTTP error interceptor.** Currently every `.subscribe()` block silently ignores errors — add a top-level handler that toasts and (where appropriate) reverts optimistic state.
4. **Add optimistic-locking headers** to PUT/POST endpoints — see [04-DRAFT-AND-VERSIONING.md → Concurrency notes](./04-DRAFT-AND-VERSIONING.md#concurrency-notes).
5. **Replace `auth.service.ts`** with real auth — token storage, refresh, route guards.
6. **Consider migrating to `httpResource()` / `rxResource()`** (Angular 20+) for resource-style data loading. The current "subscribe in a method" pattern is verbose; the new API is more declarative.
