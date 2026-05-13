/**
 * Per-Connection credential schema.
 *
 * Each Connection defines the shape of credentials a customer must supply
 * for the runtime to authenticate against the external system. The wizard's
 * step 5 ("Add Customer's Credentials") renders a form keyed off the picked
 * Connection's id. Output is stored on the wizard draft as
 * `credentials: { [field.key]: string }`.
 *
 * Future: this should move into the Connection entity itself once the backend
 * is real. Today it's a static lookup keyed by the seeded mock Connection ids.
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

export const CONNECTION_CREDENTIAL_SCHEMAS: Record<string, CredentialField[]> = {
  'conn-mcleod-v22': [
    { key: 'mcleod-url', label: 'McLeod base URL', type: 'url', required: true, placeholder: 'https://customer.mcleodsoftware.com' },
    { key: 'mcleod-auth-header', label: 'Authorization header', type: 'password', required: true, hint: 'The value sent as the Authorization header.' },
    { key: 'company-id', label: 'Company ID', type: 'text', required: true },
  ],
  'conn-mcleod-v23': [
    { key: 'mcleod-url', label: 'McLeod base URL', type: 'url', required: true, placeholder: 'https://customer.mcleodsoftware.com' },
    { key: 'mcleod-api-key', label: 'API key', type: 'password', required: true },
    { key: 'company-id', label: 'Company ID', type: 'text', required: true },
  ],
  'conn-sap-s4': [
    { key: 'sap-base-url', label: 'SAP S/4 endpoint', type: 'url', required: true, placeholder: 'https://customer.sap.example.com/sap/opu/odata/...' },
    { key: 'sap-client', label: 'Client', type: 'text', required: true, placeholder: '100' },
    { key: 'sap-username', label: 'Username', type: 'text', required: true },
    { key: 'sap-password', label: 'Password', type: 'password', required: true },
  ],
  'conn-netsuite': [
    { key: 'ns-account-id', label: 'NetSuite account ID', type: 'text', required: true },
    { key: 'ns-consumer-key', label: 'Consumer key', type: 'password', required: true },
    { key: 'ns-consumer-secret', label: 'Consumer secret', type: 'password', required: true },
    { key: 'ns-token-id', label: 'Token ID', type: 'password', required: true },
    { key: 'ns-token-secret', label: 'Token secret', type: 'password', required: true },
  ],
  'conn-webhook': [
    { key: 'webhook-url', label: 'Inbound webhook URL', type: 'url', required: true, placeholder: 'https://hooks.customer.example/transflo' },
    { key: 'webhook-secret', label: 'Shared secret', type: 'password', required: true, hint: 'Signed-payload secret used to verify inbound webhooks.' },
  ],
  'conn-sftp': [
    { key: 'sftp-host', label: 'SFTP host', type: 'text', required: true, placeholder: 'sftp.customer.example.com' },
    { key: 'sftp-port', label: 'Port', type: 'number', required: true, placeholder: '22' },
    { key: 'sftp-username', label: 'Username', type: 'text', required: true },
    { key: 'sftp-password', label: 'Password', type: 'password', required: false, hint: 'Required if SSH key is not provided.' },
    { key: 'sftp-private-key', label: 'SSH private key', type: 'password', required: false, hint: 'PEM-encoded. Required if no password.' },
  ],
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
