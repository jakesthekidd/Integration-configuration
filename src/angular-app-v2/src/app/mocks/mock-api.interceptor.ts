import { HttpInterceptorFn, HttpResponse } from '@angular/common/http';
import { of } from 'rxjs';
import { delay } from 'rxjs/operators';
import {
  mockApplications,
  mockCapabilities,
  mockTmsSystems,
  mockPartners,
  mockCustomers,
  mockApiClients,
  mockTemplates,
  mockTemplateVersions,
  mockFieldMappings,
  mockLookupTables,
  mockTransformationLogs,
  mockTransformationLogDetails,
  mockApiClientTemplates,
  mockDeployments,
} from './mock-data';

const ok = <T>(data: T) =>
  of(new HttpResponse({ status: 200, body: { success: true, data } })).pipe(delay(120));

const okRaw = <T>(body: T) => of(new HttpResponse({ status: 200, body })).pipe(delay(120));

const notFound = () =>
  of(
    new HttpResponse({
      status: 200,
      body: { success: false, message: 'Not found (mock)' },
    }),
  ).pipe(delay(60));

export const mockApiInterceptor: HttpInterceptorFn = (req, next) => {
  const url = req.url;
  if (!url.includes('/api/v1/')) return next(req);

  const path = url.substring(url.indexOf('/api/v1/') + '/api/v1/'.length).split('?')[0];
  const segs = path.split('/').filter(Boolean);
  const method = req.method.toUpperCase();

  // Mutations: most are synthetic-success, but a few endpoints DO persist into the
  // module-level fixture arrays so the UI can show optimistic round-trips.
  if (method !== 'GET') {
    // POST /deployments — push the new deployment into the fixture so subsequent
    // GETs include it. Resets on page reload (mock state lives in memory).
    if (method === 'POST' && segs[0] === 'deployments' && segs.length === 1) {
      const body = (req.body as Partial<typeof mockDeployments[number]>) ?? {};
      const now = new Date().toISOString();
      const created = {
        id: body.id ?? 'depl-' + Math.random().toString(36).slice(2, 8),
        customerId: body.customerId ?? '',
        applicationId: body.applicationId ?? '',
        capabilityId: body.capabilityId ?? '',
        connectionId: body.connectionId ?? '',
        forkedFromTemplateId: body.forkedFromTemplateId ?? '',
        forkedFromTemplateVersion: body.forkedFromTemplateVersion ?? null,
        apiClientId: body.apiClientId,
        status: body.status ?? 'Draft',
        createdAt: body.createdAt ?? now,
        updatedAt: body.updatedAt ?? now,
        snapshotVersion: body.snapshotVersion ?? 0,
      };
      mockDeployments.push(created);
      return ok(created);
    }
    return ok({ id: 'mock-' + Math.random().toString(36).slice(2, 9), ...((req.body as object) ?? {}) });
  }

  // /applications
  if (segs[0] === 'applications') {
    if (segs.length === 1) return ok({ applications: mockApplications, totalCount: mockApplications.length });
    if (segs.length === 2) {
      const a = mockApplications.find((x) => x.id === segs[1]);
      return a ? ok(a) : notFound();
    }
    if (segs.length === 3 && segs[2] === 'capabilities') {
      const caps = mockCapabilities.filter((c) => c.applicationId === segs[1]);
      return ok({ capabilities: caps, totalCount: caps.length });
    }
  }

  // /capabilities
  if (segs[0] === 'capabilities') {
    if (segs.length === 1) {
      const appId = req.params.get('applicationId');
      const caps = appId ? mockCapabilities.filter((c) => c.applicationId === appId) : mockCapabilities;
      return ok({ capabilities: caps, totalCount: caps.length });
    }
    if (segs.length === 2) {
      const c = mockCapabilities.find((x) => x.id === segs[1]);
      return c ? ok(c) : notFound();
    }
  }

  // /apiclients
  if (segs[0] === 'apiclients') {
    if (segs.length === 1) return ok({ apiClients: mockApiClients, totalCount: mockApiClients.length });
    if (segs.length === 2) {
      const c = mockApiClients.find((x) => x.id === segs[1]);
      return c ? ok(c) : notFound();
    }
    if (segs.length === 3 && segs[2] === 'templates') {
      return ok(mockApiClientTemplates[segs[1]] ?? []);
    }
  }

  // /partners
  if (segs[0] === 'partners') {
    return ok({
      partners: mockPartners,
      totalCount: mockPartners.length,
      page: 1,
      pageSize: 1000,
    });
  }

  // /customers
  if (segs[0] === 'customers') {
    if (segs.length === 1) return ok({ customers: mockCustomers, totalCount: mockCustomers.length });
    if (segs.length === 2) {
      const c = mockCustomers.find((x) => x.customerId === segs[1]);
      return c ? ok(c) : notFound();
    }
  }

  // /tms-systems
  if (segs[0] === 'tms-systems') {
    if (segs.length === 1) return ok({ systems: mockTmsSystems, totalCount: mockTmsSystems.length });
    if (segs.length === 2) {
      const t = mockTmsSystems.find((x) => x.id === segs[1]);
      return t ? ok(t) : notFound();
    }
  }

  // /templates
  if (segs[0] === 'templates') {
    if (segs.length === 1) return ok({ templates: mockTemplates, totalCount: mockTemplates.length });
    if (segs.length === 2) {
      const t = mockTemplates.find((x) => x.id === segs[1]);
      return t ? ok(t) : notFound();
    }
    if (segs.length === 3 && segs[2] === 'versions') {
      return ok(mockTemplateVersions[segs[1]] ?? []);
    }
    if (segs.length === 4 && segs[2] === 'versions') {
      const t = mockTemplates.find((x) => x.id === segs[1]);
      return t ? ok({ ...t, version: Number(segs[3]) }) : notFound();
    }
  }

  // /field-mappings
  if (segs[0] === 'field-mappings') {
    if (segs.length === 1) {
      const tplId = req.params.get('templateId');
      const mappings = tplId ? mockFieldMappings.filter((m) => m.templateId === tplId) : mockFieldMappings;
      return ok({ mappings, totalCount: mappings.length });
    }
    if (segs.length === 2) {
      const m = mockFieldMappings.find((x) => x.id === segs[1]);
      return m ? ok(m) : notFound();
    }
  }

  // /lookup-tables
  if (segs[0] === 'lookup-tables') {
    if (segs.length === 1) {
      const tmsId = req.params.get('tmsSystemId');
      const lookupTables = tmsId ? mockLookupTables.filter((l) => l.tmsSystemId === tmsId) : mockLookupTables;
      return ok({ lookupTables, totalCount: lookupTables.length });
    }
    if (segs.length === 2) {
      const l = mockLookupTables.find((x) => x.id === segs[1]);
      return l ? ok(l) : notFound();
    }
  }

  // /deployments
  if (segs[0] === 'deployments') {
    if (segs.length === 1) {
      // Read customerId from BOTH HttpParams (set via { params }) and the URL
      // query string (when callers bake it inline). The api.service does the latter.
      const qs = url.includes('?') ? new URLSearchParams(url.split('?')[1]) : null;
      const customerId = req.params.get('customerId') ?? qs?.get('customerId') ?? null;
      const list = customerId ? mockDeployments.filter((d) => d.customerId === customerId) : mockDeployments;
      return ok({ deployments: list, totalCount: list.length });
    }
    if (segs.length === 2) {
      const d = mockDeployments.find((x) => x.id === segs[1]);
      return d ? ok(d) : notFound();
    }
  }

  // /transform-logs
  if (segs[0] === 'transform-logs') {
    if (segs.length === 1) {
      return ok({ logs: mockTransformationLogs, totalCount: mockTransformationLogs.length });
    }
    if (segs.length === 2) {
      const detail =
        mockTransformationLogDetails[segs[1]] ??
        mockTransformationLogs.find((x) => x.id === segs[1]);
      return detail ? ok(detail) : notFound();
    }
  }

  // Fallback for unrecognized GETs
  return okRaw({ success: true, data: null, message: 'Mock fallback' });
};
