export interface TokenResponse {
  accessToken: string;
  expiresIn: number;
  refreshToken: string;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  total: number;
}

export interface ProductDto {
  id: string;
  sku: string;
  name: string;
  price: number;
  isActive: boolean;
  version: number;
  updatedAt: string;
  isDelete: boolean;
  deleteByUser?: string | null;
  deletedAt?: string | null;
}

export interface CustomerDto {
  id: string;
  name: string;
  phone: string;
  version: number;
  updatedAt: string;
  isDelete: boolean;
  deleteByUser?: string | null;
  deletedAt?: string | null;
}

export enum PhoneMatch {
  Prefix = 1,
  Suffix = 2
}

export const PhoneMatchApiValue: Record<PhoneMatch, 'prefix' | 'suffix'> = {
  [PhoneMatch.Prefix]: 'prefix',
  [PhoneMatch.Suffix]: 'suffix'
};

export interface OrderLineInput {
  productId: string;
  quantity: number;
  discountPercent: number;
}

export interface OrderLineDto extends OrderLineInput {
  sku: string;
  productName: string;
  unitPrice: number;
  lineTotal: number;
}

export interface OrderDto {
  id: string;
  customerId: string;
  customerName: string;
  customerPhone: string;
  createdAt: string;
  status: string;
  totalQuantity: number;
  total: number;
  version: number;
  updatedAt: string;
  rejectionReason?: string | null;
  lines: OrderLineDto[];
}

export interface InventoryDto {
  productId: string;
  sku: string;
  available: number;
  reserved: number;
  version: number;
}

export interface ApiResult<T> {
  body: T;
  etag?: string | null;
  status: number;
}
