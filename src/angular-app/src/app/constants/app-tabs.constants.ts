export const AppTab = {
  Customers: 'customers',
  Tms: 'tms',
  Templates: 'templates',
  Lookups: 'lookups',
  Test: 'test',
  Logs: 'logs',
  Integrations: 'integrations',
  Patners: 'patners',
} as const;

export type AppTab = (typeof AppTab)[keyof typeof AppTab];
