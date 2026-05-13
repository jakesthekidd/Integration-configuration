export interface Application {
  id: string;
  name: string;
  displayName: string;
  description?: string;
  isActive: boolean;
  createdAt: string;
}

export interface ApplicationListResponse {
  applications: Application[];
  totalCount: number;
}
