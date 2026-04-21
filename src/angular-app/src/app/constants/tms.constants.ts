export const TmsName = {
  LegacyMcLeod: 'Legacy McLeod',
  TruckMate: 'TruckMate',
  BrokerAI: 'BrokerAI',
  DMS: 'DMS',
} as const;

export type TmsName = (typeof TmsName)[keyof typeof TmsName];

export const TmsCredentialKeys: Record<TmsName, string[]> = {
  [TmsName.LegacyMcLeod]: [
    'mcleod-url',
    'mcleod-auth-header',
    'company-id-header',
    'x1-url',
    'x1-auth-header',
    'wfai-url',
    'wfai-integration-base-url',
    'wfai-portal-customer-id',
    'tonuCode',
  ],
  [TmsName.TruckMate]: [
    'truckmate-url',
    'truckmate-auth-token',
    'wfai-url',
    'wfai-integration-base-url',
    'wfai-portal-customer-id',
  ],
  [TmsName.BrokerAI]: [
    'brokerai-url',
    'brokerai-username',
    'brokerai-password',
    'brokerai-divisionid',
    'wfai-url',
    'wfai-integration-base-url',
    'wfai-portal-customer-id',
  ],
  [TmsName.DMS]: [
    'freight-api-base-url',
    'wfai-integration-base-url',
    'wfai-portal-customer-id',
    'wfai-tenant-id',
    'dms-auth-url',
    'dms-add-doc-url',
    'dms-username',
    'dms-password',
    'transformer-base-url',
    'template-id',
    'template-version',
    'api-client-id',
  ],
};

export const URL_PATTERN = 'https?://.+';

export const DmsSpecialKeys = {
  TemplateId: 'template-id',
  ApiClientId: 'api-client-id',
  TemplateVersion: 'template-version',
} as const;
