import { CustomerStatus } from '../../constants/customer-status';

export interface CustomerResponse {
  id: string;
  customerCode?: string | null;
  name: string;
  phone: string;
  email?: string | null;
  address?: string | null;
  status?: CustomerStatus | string | null;
  version: number;
  updatedAt: string;
  createdAt?: string | null;
  isDelete: boolean;
  deleteByUser?: string | null;
  deletedAt?: string | null;
}
