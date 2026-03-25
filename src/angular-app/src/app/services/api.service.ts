import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { TmsSystem, CreateTmsSystemRequest, ApiResponse, TmsSystemListResponse } from '../models/tms-system.model';
import { FieldMappingTemplate, CreateTemplateRequest, UpdateTemplateRequest, TemplateListResponse } from '../models/template.model';
import { FieldMapping, CreateFieldMappingRequest, UpdateFieldMappingRequest, FieldMappingListResponse } from '../models/field-mapping.model';
import { LookupTable, CreateLookupTableRequest, UpdateLookupTableRequest, LookupTableListResponse } from '../models/lookup-table.model';
import { Customer, CustomerRequest, CustomerListResponse } from '../models/customer.model';
import { TransformationLogSummary, TransformationLogDetail, TransformationLogListResponse } from '../models/transformation-log.model';
import { environment } from '../../environments/environment';
import { TransformRequest } from '../models/transformation-test.model';

@Injectable({
  providedIn: 'root'
})
export class ApiService {
  private apiUrl = environment.apiUrl;

  constructor(private http: HttpClient) { }

  // Customers
  getCustomers(activeOnly?: boolean): Observable<ApiResponse<CustomerListResponse>> {
    const url = activeOnly !== undefined
      ? `${this.apiUrl}/customers?activeOnly=${activeOnly}`
      : `${this.apiUrl}/customers`;
    return this.http.get<ApiResponse<CustomerListResponse>>(url);
  }

  getCustomerById(id: string): Observable<ApiResponse<Customer>> {
    return this.http.get<ApiResponse<Customer>>(`${this.apiUrl}/customers/${id}`);
  }

  createCustomer(request: CustomerRequest): Observable<ApiResponse<Customer>> {
    return this.http.post<ApiResponse<Customer>>(
      `${this.apiUrl}/customers`,
      request
    );
  }

  updateCustomer(id: string, request: CustomerRequest): Observable<ApiResponse<Customer>> {
    return this.http.put<ApiResponse<Customer>>(
      `${this.apiUrl}/customers/${id}`,
      request
    );
  }

  deleteCustomer(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/customers/${id}`);
  }

  setCustomerStatus(id: string, enabled: boolean): Observable<ApiResponse<Customer>> {
    return this.http.patch<ApiResponse<Customer>>(
      `${this.apiUrl}/customers/${id}/status?enabled=${enabled}`,
      {}
    );
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

  // Templates
  getTemplates(tmsSystemId?: string): Observable<ApiResponse<TemplateListResponse>> {
    const url = tmsSystemId
      ? `${this.apiUrl}/templates?tmsSystemId=${tmsSystemId}`
      : `${this.apiUrl}/templates`;
    return this.http.get<ApiResponse<TemplateListResponse>>(url);
  }

  getTemplateById(templateId: string, version?: number): Observable<ApiResponse<FieldMappingTemplate>> {
    const url = version
      ? `${this.apiUrl}/templates/${templateId}/versions/${version}`
      : `${this.apiUrl}/templates/${templateId}`;
    return this.http.get<ApiResponse<FieldMappingTemplate>>(url);
  }

  getTemplateVersions(templateId: string): Observable<ApiResponse<any[]>> {
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

  createTemplateVersion(templateId: string): Observable<ApiResponse<any>> {
    return this.http.post<ApiResponse<any>>(`${this.apiUrl}/templates/${templateId}/versions`, {});
  }

  publishTemplateVersion(templateId: string, version: number): Observable<ApiResponse<any>> {
    return this.http.post<ApiResponse<any>>(`${this.apiUrl}/templates/${templateId}/versions/${version}/publish`, {});
  }

  duplicateTemplate(templateId: string): Observable<ApiResponse<FieldMappingTemplate>> {
    return this.http.post<ApiResponse<FieldMappingTemplate>>(
      `${this.apiUrl}/templates/${templateId}/duplicate`, {}
    );
  }

  reactivateTemplate(templateId: string): Observable<void> {
    return this.http.post<void>(
      `${this.apiUrl}/templates/${templateId}/reactivate`, {}
    );
  }

  parseJson(jsonString: string, includeSampleValues: boolean = false): Observable<ApiResponse<any>> {
    return this.http.post<ApiResponse<any>>(`${this.apiUrl}/json/parse`, { jsonString, includeSampleValues });
  }

  // Field Mappings
  getFieldMappings(templateId?: string): Observable<ApiResponse<FieldMappingListResponse>> {
    const url = templateId
      ? `${this.apiUrl}/field-mappings?templateId=${templateId}`
      : `${this.apiUrl}/field-mappings`;
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
  getTransformationLogs(templateId?: string, status?: string, limit: number = 100): Observable<ApiResponse<TransformationLogListResponse>> {
    const params: string[] = [];
    if (templateId) params.push(`templateId=${encodeURIComponent(templateId)}`);
    if (status) params.push(`status=${encodeURIComponent(status)}`);
    params.push(`limit=${limit}`);
    return this.http.get<ApiResponse<TransformationLogListResponse>>(
      `${this.apiUrl}/transform-logs?${params.join('&')}`
    );
  }

  getTransformationLogById(id: string): Observable<ApiResponse<TransformationLogDetail>> {
    return this.http.get<ApiResponse<TransformationLogDetail>>(`${this.apiUrl}/transform-logs/${id}`);
  }

  transformJsonWithTemplate(request: TransformRequest): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/transform`, request);
  }
}
