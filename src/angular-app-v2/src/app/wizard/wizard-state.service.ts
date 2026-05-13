import { Injectable, signal } from '@angular/core';

/**
 * The in-progress wizard draft. Each step writes to this; the shell reads from it
 * to decide validity and surface a Review screen later.
 *
 * The architecture (per PRODUCT-GUIDING-PRINCIPLES.md §6) says the wizard
 * BINDS library pieces to an EXISTING customer — it does not create customers.
 * The customer list comes from the legacy system.
 */

/** One mapping row in the forked CustomerTemplate the user customizes in step 7. */
export interface FieldMappingDraft {
  id: string;
  sourcePath: string;
  targetPath: string;
  transformationType: string;
  isRequired: boolean;
  defaultValue: string;
}

export interface WizardDraft {
  // Step 1: pick an existing customer
  customerId: string;

  // Step 2: pick an Application (WorkflowAI / Mobile / LTL Nav)
  applicationId: string;

  // Step 3: pick a Capability under that Application
  capabilityId: string;

  // Step 4: pick a Connection (McLeod v23, SAP S/4, etc.)
  connectionId: string;

  // Step 5: per-customer credentials, keyed by Connection's credential schema
  credentials: { [key: string]: string };

  // Step 6: pick a MasterTemplate to fork (or sentinel TEMPLATE_FROM_SCRATCH)
  templateId: string;
  templateVersion: number | null;

  // Step 7: customizations applied to the forked CustomerTemplate
  fieldMappings: FieldMappingDraft[];
  /** Tracks the source template the mappings were forked from. Empty means scratch. */
  forkedFromTemplateId: string;

  // Step 8 / runtime: API client identity (defaulted; assignable later)
  apiClientId: string;

  // Step 9: real-order test result
  testPassed: boolean;
  testCorrelationId: string;
}

const emptyDraft = (): WizardDraft => ({
  customerId: '',
  applicationId: '',
  capabilityId: '',
  connectionId: '',
  credentials: {},
  templateId: '',
  templateVersion: null,
  fieldMappings: [],
  forkedFromTemplateId: '',
  apiClientId: '',
  testPassed: false,
  testCorrelationId: '',
});

@Injectable({ providedIn: 'root' })
export class WizardStateService {
  readonly draft = signal<WizardDraft>(emptyDraft());

  patch(update: Partial<WizardDraft>) {
    this.draft.update((d) => ({ ...d, ...update }));
  }

  reset() {
    this.draft.set(emptyDraft());
  }
}
