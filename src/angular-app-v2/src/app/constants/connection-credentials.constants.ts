/**
 * Per-Connection credential schema.
 *
 * Each Connection defines the shape of credentials a customer must supply
 * for the runtime to authenticate against the external system. The Connection
 * tab renders a form keyed off the picked Connection's id.
 *
 * Future: this should live on the Connection entity itself once the backend
 * is real. Today it's a static lookup keyed by the seeded mock Connection ids.
 * Multiple connection IDs can share the same field set (e.g. McLeod v23 is
 * the same auth surface regardless of which capability it's wired to).
 */

export type CredentialFieldType = 'text' | 'password' | 'url' | 'number';

export interface CredentialField {
  key: string;
  label: string;
  type: CredentialFieldType;
  required: boolean;
  /** Short helper text shown under the input. */
  hint?: string;
  /** Optional placeholder for the input. */
  placeholder?: string;
}

// ── Per-system field sets ─────────────────────────────────────────────────
const MCLEOD_V22_FIELDS: CredentialField[] = [
  { key: 'mcleod-url', label: 'McLeod base URL', type: 'url', required: true, placeholder: 'https://customer.mcleodsoftware.com' },
  { key: 'mcleod-auth-header', label: 'Authorization header', type: 'password', required: true, hint: 'The value sent as the Authorization header.' },
  { key: 'company-id', label: 'Company ID', type: 'text', required: true },
];

const MCLEOD_V23_FIELDS: CredentialField[] = [
  { key: 'mcleod-url', label: 'McLeod base URL', type: 'url', required: true, placeholder: 'https://customer.mcleodsoftware.com' },
  { key: 'mcleod-api-key', label: 'API key', type: 'password', required: true },
  { key: 'company-id', label: 'Company ID', type: 'text', required: true },
];

const SAP_FIELDS: CredentialField[] = [
  { key: 'sap-base-url', label: 'SAP S/4 endpoint', type: 'url', required: true, placeholder: 'https://customer.sap.example.com/sap/opu/odata/...' },
  { key: 'sap-client', label: 'Client', type: 'text', required: true, placeholder: '100' },
  { key: 'sap-username', label: 'Username', type: 'text', required: true },
  { key: 'sap-password', label: 'Password', type: 'password', required: true },
];

const NETSUITE_FIELDS: CredentialField[] = [
  { key: 'ns-account-id', label: 'NetSuite account ID', type: 'text', required: true },
  { key: 'ns-consumer-key', label: 'Consumer key', type: 'password', required: true },
  { key: 'ns-consumer-secret', label: 'Consumer secret', type: 'password', required: true },
  { key: 'ns-token-id', label: 'Token ID', type: 'password', required: true },
  { key: 'ns-token-secret', label: 'Token secret', type: 'password', required: true },
];

const WEBHOOK_FIELDS: CredentialField[] = [
  { key: 'webhook-url', label: 'Inbound webhook URL', type: 'url', required: true, placeholder: 'https://hooks.customer.example/transflo' },
  { key: 'webhook-secret', label: 'Shared secret', type: 'password', required: true, hint: 'Signed-payload secret used to verify inbound webhooks.' },
];

const SFTP_FIELDS: CredentialField[] = [
  { key: 'sftp-host', label: 'SFTP host', type: 'text', required: true, placeholder: 'sftp.customer.example.com' },
  { key: 'sftp-port', label: 'Port', type: 'number', required: true, placeholder: '22' },
  { key: 'sftp-username', label: 'Username', type: 'text', required: true },
  { key: 'sftp-password', label: 'Password', type: 'password', required: false, hint: 'Required if SSH key is not provided.' },
  { key: 'sftp-private-key', label: 'SSH private key', type: 'password', required: false, hint: 'PEM-encoded. Required if no password.' },
];

// ── Connection ID → field set lookup ──────────────────────────────────────
export const CONNECTION_CREDENTIAL_SCHEMAS: Record<string, CredentialField[]> = {
  // Mobile · Import Loads — both McLeod versions
  'conn-mobile-loads-mcleod-v22': MCLEOD_V22_FIELDS,
  'conn-mobile-loads-mcleod-v23': MCLEOD_V23_FIELDS,
  // Mobile · Export Scans
  'conn-mobile-scans-sftp': SFTP_FIELDS,
  // WorkflowAI · Import Documents
  'conn-wfai-import-docs-mcleod': MCLEOD_V22_FIELDS,
  'conn-wfai-import-docs-sap': SAP_FIELDS,
  // WorkflowAI · Export Documents
  'conn-wfai-export-docs-sap': SAP_FIELDS,
  // WorkflowAI · Webhooks
  'conn-wfai-webhooks-receiver': WEBHOOK_FIELDS,
  // User Workflow · Import Loads
  'conn-uw-loads-netsuite': NETSUITE_FIELDS,
  // LTL Navigator · Import Orders
  'conn-ltl-orders-mcleod': MCLEOD_V23_FIELDS,
};

/** Returns true when every required field in the schema has a non-blank value. */
export function credentialsAreValid(
  connectionId: string,
  credentials: Record<string, string>,
): boolean {
  const schema = CONNECTION_CREDENTIAL_SCHEMAS[connectionId];
  if (!schema) return false;
  return schema.every((f) => !f.required || (credentials[f.key]?.trim().length ?? 0) > 0);
}
