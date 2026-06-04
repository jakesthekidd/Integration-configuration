import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { TmsSystem, CreateTmsSystemRequest, ApiResponse, TmsSystemListResponse } from '../models/tms-system.model';
import {
  FieldMappingTemplate,
  CreateTemplateRequest,
  UpdateTemplateRequest,
  TemplateListResponse,
} from '../models/template.model';
import {
  FieldMapping,
  CreateFieldMappingRequest,
  UpdateFieldMappingRequest,
  FieldMappingListResponse,
} from '../models/field-mapping.model';
import {
  LookupTable,
  CreateLookupTableRequest,
  UpdateLookupTableRequest,
  LookupTableListResponse,
} from '../models/lookup-table.model';
import { Customer, CustomerRequest, CustomerListResponse } from '../models/customer.model';
import { TransformationLogDetail, TransformationLogListResponse } from '../models/transformation-log.model';
import { CreatePartnerRequest, Partner, PartnerListResponse } from '../models/partner.model';
import { environment } from '../../environments/environment';
import {
  ApiClient,
  CreateApiClientRequest,
  UpdateApiClientRequest,
  ApiClientListResponse,
} from '../models/api-client.model';
import { TemplateVersionResponse } from '../models/template.model';
import { ApplicationListResponse } from '../models/application.model';
import { CapabilityListResponse } from '../models/capability.model';
import { Deployment, DeploymentListResponse } from '../models/deployment.model';

@Injectable({
  providedIn: 'root',
})
export class ApiService {
  private apiUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  // Applications
  getApplications(): Observable<ApiResponse<ApplicationListResponse>> {
    return this.http.get<ApiResponse<ApplicationListResponse>>(`${this.apiUrl}/applications`);
  }

  getCapabilitiesForApplication(appId: string): Observable<ApiResponse<CapabilityListResponse>> {
    return this.http.get<ApiResponse<CapabilityListResponse>>(`${this.apiUrl}/applications/${appId}/capabilities`);
  }

  // Deployments
  getDeployments(customerId?: string): Observable<ApiResponse<DeploymentListResponse>> {
    const url = customerId
      ? `${this.apiUrl}/deployments?customerId=${customerId}`
      : `${this.apiUrl}/deployments`;
    return this.http.get<ApiResponse<DeploymentListResponse>>(url);
  }

  getDeploymentById(id: string): Observable<ApiResponse<Deployment>> {
    return this.http.get<ApiResponse<Deployment>>(`${this.apiUrl}/deployments/${id}`);
  }

  createDeployment(d: Partial<Deployment>): Observable<ApiResponse<Deployment>> {
    return this.http.post<ApiResponse<Deployment>>(`${this.apiUrl}/deployments`, d);
  }

  // Capabilities
  getCapabilities(applicationId?: string): Observable<ApiResponse<CapabilityListResponse>> {
    const url = applicationId
      ? `${this.apiUrl}/capabilities?applicationId=${applicationId}`
      : `${this.apiUrl}/capabilities`;
    return this.http.get<ApiResponse<CapabilityListResponse>>(url);
  }

  // API Clients
  getApiClients(): Observable<ApiResponse<ApiClientListResponse>> {
    return this.http.get<ApiResponse<ApiClientListResponse>>(`${this.apiUrl}/apiclients`);
  }

  getApiClientById(id: string): Observable<ApiResponse<ApiClient>> {
    return this.http.get<ApiResponse<ApiClient>>(`${this.apiUrl}/apiclients/${id}`);
  }

  createApiClient(request: CreateApiClientRequest): Observable<ApiResponse<ApiClient>> {
    return this.http.post<ApiResponse<ApiClient>>(`${this.apiUrl}/apiclients`, request);
  }

  updateApiClient(id: string, request: UpdateApiClientRequest): Observable<ApiResponse<ApiClient>> {
    return this.http.put<ApiResponse<ApiClient>>(`${this.apiUrl}/apiclients/${id}`, request);
  }

  deleteApiClient(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/apiclients/${id}`);
  }

  getAssignedTemplates(id: string): Observable<ApiResponse<TemplateVersionResponse[]>> {
    return this.http.get<ApiResponse<TemplateVersionResponse[]>>(`${this.apiUrl}/apiclients/${id}/templates`);
  }

  assignTemplate(id: string, templateVersionId: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/apiclients/${id}/templates`, { templateVersionId });
  }

  removeTemplate(id: string, templateVersionId: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/apiclients/${id}/templates/${templateVersionId}`);
  }

  // Partners
  getPartners(): Observable<ApiResponse<PartnerListResponse>> {
    return this.http.get<ApiResponse<PartnerListResponse>>(`${this.apiUrl}/partners`);
  }

  createPartner(request: CreatePartnerRequest): Observable<ApiResponse<Partner>> {
    return this.http.post<ApiResponse<Partner>>(`${this.apiUrl}/partners`, request);
  }

  deletePartner(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/partners/${id}`);
  }

  // Customers
  getCustomers(activeOnly?: boolean): Observable<ApiResponse<CustomerListResponse>> {
    const url =
      activeOnly !== undefined ? `${this.apiUrl}/customers?activeOnly=${activeOnly}` : `${this.apiUrl}/customers`;
    return this.http.get<ApiResponse<CustomerListResponse>>(url);
  }

  getCustomerById(id: string): Observable<ApiResponse<Customer>> {
    return this.http.get<ApiResponse<Customer>>(`${this.apiUrl}/customers/${id}`);
  }

  createCustomer(request: CustomerRequest): Observable<ApiResponse<Customer>> {
    return this.http.post<ApiResponse<Customer>>(`${this.apiUrl}/customers`, request);
  }

  updateCustomer(id: string, request: CustomerRequest): Observable<ApiResponse<Customer>> {
    return this.http.put<ApiResponse<Customer>>(`${this.apiUrl}/customers/${id}`, request);
  }

  deleteCustomer(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/customers/${id}`);
  }

  setCustomerStatus(id: string, enabled: boolean): Observable<ApiResponse<Customer>> {
    return this.http.patch<ApiResponse<Customer>>(`${this.apiUrl}/customers/${id}/status?enabled=${enabled}`, {});
  }

  // TMS Systems
  getTmsSystems(activeOnly: boolean = false): Observable<ApiResponse<TmsSystemListResponse>> {
    return this.http.get<ApiResponse<TmsSystemListResponse>>(`${this.apiUrl}/tms-systems?activeOnly=${activeOnly}`);
  }

  getTmsSystemById(id: string): Observable<ApiResponse<TmsSystem>> {
    return this.http.get<ApiResponse<TmsSystem>>(`${this.apiUrl}/tms-systems/${id}`);
  }

  createTmsSystem(request: CreateTmsSystemRequest): Observable<ApiResponse<TmsSystem>> {
    return this.http.post<ApiResponse<TmsSystem>>(`${this.apiUrl}/tms-systems`, request);
  }

  updateTmsSystem(id: string, request: Partial<TmsSystem>): Observable<ApiResponse<TmsSystem>> {
    return this.http.put<ApiResponse<TmsSystem>>(`${this.apiUrl}/tms-systems/${id}`, request);
  }

  deleteTmsSystem(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/tms-systems/${id}`);
  }

  // Partners
  getPagedPartners(): Observable<ApiResponse<PartnerListResponse>> {
    return this.http.get<ApiResponse<PartnerListResponse>>(`${this.apiUrl}/partners?page=1&pageSize=1000`);
  }

  // Templates
  getTemplates(
    tmsSystemId?: string,
    page?: number,
    pageSize?: number,
    status?: string,
  ): Observable<ApiResponse<TemplateListResponse>> {
    const params: string[] = [];
    if (tmsSystemId) params.push(`tmsSystemId=${tmsSystemId}`);
    if (page !== undefined) params.push(`page=${page}`);
    if (pageSize !== undefined) params.push(`pageSize=${pageSize}`);
    if (status) params.push(`status=${encodeURIComponent(status)}`);
    const url = params.length ? `${this.apiUrl}/templates?${params.join('&')}` : `${this.apiUrl}/templates`;
    return this.http.get<ApiResponse<TemplateListResponse>>(url);
  }

  getTemplateById(templateId: string, version?: number): Observable<ApiResponse<FieldMappingTemplate>> {
    const url = version
      ? `${this.apiUrl}/templates/${templateId}/versions/${version}`
      : `${this.apiUrl}/templates/${templateId}`;
    return this.http.get<ApiResponse<FieldMappingTemplate>>(url);
  }

  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  getTemplateVersions(templateId: string): Observable<ApiResponse<any[]>> {
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    return this.http.get<ApiResponse<any[]>>(`${this.apiUrl}/templates/${templateId}/versions`);
  }

  createTemplate(request: CreateTemplateRequest): Observable<ApiResponse<FieldMappingTemplate>> {
    return this.http.post<ApiResponse<FieldMappingTemplate>>(`${this.apiUrl}/templates`, request);
  }

  updateTemplate(templateId: string, request: UpdateTemplateRequest): Observable<ApiResponse<FieldMappingTemplate>> {
    return this.http.put<ApiResponse<FieldMappingTemplate>>(`${this.apiUrl}/templates/${templateId}`, request);
  }

  deleteTemplate(templateId: string, version?: number): Observable<void> {
    const url = version
      ? `${this.apiUrl}/templates/${templateId}?version=${version}`
      : `${this.apiUrl}/templates/${templateId}`;
    return this.http.delete<void>(url);
  }

  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  createTemplateVersion(templateId: string, baseVersion?: number): Observable<ApiResponse<any>> {
    const body = baseVersion !== undefined ? { baseVersion } : {};
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    return this.http.post<ApiResponse<any>>(`${this.apiUrl}/templates/${templateId}/versions`, body);
  }

  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  publishTemplateVersion(templateId: string, version: number): Observable<ApiResponse<any>> {
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    return this.http.post<ApiResponse<any>>(`${this.apiUrl}/templates/${templateId}/versions/${version}/publish`, {});
  }

  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  deleteTemplateVersion(templateId: string, version: number): Observable<ApiResponse<any>> {
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    return this.http.delete<ApiResponse<any>>(`${this.apiUrl}/templates/${templateId}/versions/${version}`);
  }

  duplicateTemplate(
    templateId: string,
    options?: { includeAllVersions: boolean },
  ): Observable<ApiResponse<FieldMappingTemplate>> {
    return this.http.post<ApiResponse<FieldMappingTemplate>>(
      `${this.apiUrl}/templates/${templateId}/duplicate`,
      options ?? {},
    );
  }

  reactivateTemplate(templateId: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/templates/${templateId}/reactivate`, {});
  }

  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  parseJson(jsonString: string, includeSampleValues: boolean = false): Observable<ApiResponse<any>> {
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    return this.http.post<ApiResponse<any>>(`${this.apiUrl}/json/parse`, { jsonString, includeSampleValues });
  }

  /** Per-deployment saved mappings (mock-backed). */
  getDeploymentMappings(deploymentId: string): Observable<
    ApiResponse<{ mappings: FieldMapping[]; totalCount: number }>
  > {
    return this.http.get<ApiResponse<{ mappings: FieldMapping[]; totalCount: number }>>(
      `${this.apiUrl}/deployments/${deploymentId}/mappings`,
    );
  }

  /** Persist a deployment's mappings + the template ref it was forked from. */
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  saveDeploymentMappings(
    deploymentId: string,
    body: {
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      mappings: any[];
      forkedFromTemplateId?: string;
      forkedFromTemplateVersion?: number | null;
    },
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
  ): Observable<ApiResponse<any>> {
    return this.http.put<ApiResponse<unknown>>(
      `${this.apiUrl}/deployments/${deploymentId}/mappings`,
      body,
    );
  }

  // Field Mappings
  getFieldMappings(templateId?: string, templateVersionId?: string): Observable<ApiResponse<FieldMappingListResponse>> {
    let url = templateId ? `${this.apiUrl}/field-mappings?templateId=${templateId}` : `${this.apiUrl}/field-mappings`;

    if (templateVersionId) {
      url += (url.includes('?') ? '&' : '?') + `templateVersionId=${templateVersionId}`;
    }
    return this.http.get<ApiResponse<FieldMappingListResponse>>(url);
  }

  getFieldMappingById(id: string): Observable<ApiResponse<FieldMapping>> {
    return this.http.get<ApiResponse<FieldMapping>>(`${this.apiUrl}/field-mappings/${id}`);
  }

  createFieldMapping(request: CreateFieldMappingRequest): Observable<ApiResponse<FieldMapping>> {
    return this.http.post<ApiResponse<FieldMapping>>(`${this.apiUrl}/field-mappings`, request);
  }

  updateFieldMapping(id: string, request: UpdateFieldMappingRequest): Observable<ApiResponse<FieldMapping>> {
    return this.http.put<ApiResponse<FieldMapping>>(`${this.apiUrl}/field-mappings/${id}`, request);
  }

  deleteFieldMapping(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/field-mappings/${id}`);
  }

  deleteFieldMappingsByTemplate(templateId: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/field-mappings/template/${templateId}`);
  }

  // Lookup Tables
  getLookupTables(tmsSystemId?: string): Observable<ApiResponse<LookupTableListResponse>> {
    const url = tmsSystemId
      ? `${this.apiUrl}/lookup-tables?tmsSystemId=${tmsSystemId}`
      : `${this.apiUrl}/lookup-tables`;
    return this.http.get<ApiResponse<LookupTableListResponse>>(url);
  }

  getLookupTableById(id: string): Observable<ApiResponse<LookupTable>> {
    return this.http.get<ApiResponse<LookupTable>>(`${this.apiUrl}/lookup-tables/${id}`);
  }

  createLookupTable(request: CreateLookupTableRequest): Observable<ApiResponse<LookupTable>> {
    return this.http.post<ApiResponse<LookupTable>>(`${this.apiUrl}/lookup-tables`, request);
  }

  updateLookupTable(id: string, request: UpdateLookupTableRequest): Observable<ApiResponse<LookupTable>> {
    return this.http.put<ApiResponse<LookupTable>>(`${this.apiUrl}/lookup-tables/${id}`, request);
  }

  deleteLookupTable(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/lookup-tables/${id}`);
  }

  // Transformation Logs
  getTransformationLogs(
    templateId?: string,
    status?: string,
    limit: number = 100,
    from?: string,
    to?: string,
  ): Observable<ApiResponse<TransformationLogListResponse>> {
    const params: string[] = [];
    if (templateId) params.push(`templateId=${encodeURIComponent(templateId)}`);
    if (status) params.push(`status=${encodeURIComponent(status)}`);
    params.push(`limit=${limit}`);
    if (from) params.push(`from=${encodeURIComponent(from)}`);
    if (to) params.push(`to=${encodeURIComponent(to)}`);
    return this.http.get<ApiResponse<TransformationLogListResponse>>(
      `${this.apiUrl}/transform-logs?${params.join('&')}`,
    );
  }

  getTransformationLogById(id: string): Observable<ApiResponse<TransformationLogDetail>> {
    return this.http.get<ApiResponse<TransformationLogDetail>>(`${this.apiUrl}/transform-logs/${id}`);
  }

  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  transformJsonWithTemplate(
    templateId: string,
    version: number,
    sourceDocument: unknown,
    clientId: string,
  ): Observable<any> {
    const headers = new HttpHeaders({ 'x-client-id': clientId });
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    return this.http.post<any>(
      `${this.apiUrl}/templates/${templateId}/versions/${version}/transform`,
      { sourceDocument },
      { headers },
    );
  }
}
