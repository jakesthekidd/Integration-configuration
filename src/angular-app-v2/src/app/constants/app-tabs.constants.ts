export const AppTab = {
  Customers: 'customers',
  Tms: 'tms',
  Templates: 'templates',
  Lookups: 'lookups',
  Test: 'test',
  Logs: 'logs',
  Integrations: 'integrations',
  Partners: 'partners',
  Applications: 'applications',
} as const;

export type AppTab = (typeof AppTab)[keyof typeof AppTab];
