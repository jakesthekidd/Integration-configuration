export const TransformationStatus = {
  Success: 'Success',
  Warning: 'Warning',
  PartialSuccess: 'PartialSuccess',
  Error: 'Error',
} as const;

export type TransformationStatus = (typeof TransformationStatus)[keyof typeof TransformationStatus];

export const StatusBadgeClass: Record<TransformationStatus, string> = {
  [TransformationStatus.Success]: 'badge-success',
  [TransformationStatus.Warning]: 'badge-warning',
  [TransformationStatus.PartialSuccess]: 'badge-partial',
  [TransformationStatus.Error]: 'badge-error',
};
