export type ECustomerStatus = 'Normal' | 'Suspended' | 'Blocked';
export type EProductStatus = 'Draft' | 'Published' | 'Discontinued';
export type EProductVariantStatus = 'Draft' | 'Published' | 'Discontinued';
export type ECategoryStatus = 'Draft' | 'Published' | 'Archived';
export type EOrderStatus = 'Draft' | 'PendingInventory' | 'Confirmed' | 'Cancelled' | 'InventoryRejected';

export interface TokenResponse {
  accessToken: string;
  expiresIn: number;
  refreshToken: string;
}

export interface ApiResponse<T> {
  success: boolean;
  succeeded?: boolean;
  message?: string | null;
  correlationId?: string | null;
  data?: T | null;
}

export interface ApiErrorResponse {
  status: number;
  errorCode: string;
  message?: string | null;
  traceId?: string | null;
  correlationId?: string | null;
  errors?: ApiError[] | null;
  validationErrors?: ValidationError[] | null;
}

export interface ApiError {
  code: string;
  message: string;
}

export interface ValidationError {
  field: string;
  message: string;
  code?: string | null;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  total: number;
}

export interface PagedResponse<T> {
  items: T[];
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
  pageNumber: number;
  pageSize: number;
}

export interface ApiResult<T> {
  body: T;
  etag?: string | null;
  status: number;
  message?: string | null;
  correlationId?: string | null;
}

export enum PhoneMatch {
  Prefix = 1,
  Suffix = 2
}

export const PhoneMatchApiValue: Record<PhoneMatch, 'prefix' | 'suffix'> = {
  [PhoneMatch.Prefix]: 'prefix',
  [PhoneMatch.Suffix]: 'suffix'
};

export interface CustomerDto {
  id: string;
  customerCode?: string | null;
  name: string;
  phone: string;
  email?: string | null;
  address?: string | null;
  status?: ECustomerStatus | string | null;
  version: number;
  updatedAt: string;
  createdAt?: string | null;
  isDelete: boolean;
  deleteByUser?: string | null;
  deletedAt?: string | null;
}

export interface CreateCustomerRequestDto {
  name: string;
  phone: string;
  email?: string | null;
  address?: string | null;
}

export interface UpdateCustomerRequestDto {
  name: string;
  phone: string;
  email?: string | null;
  address?: string | null;
}

export interface UpdateCustomerStatusRequestDto {
  status: ECustomerStatus;
}

export interface CategoryDto {
  id: string;
  categoryCode: string;
  name: string;
  description?: string | null;
  parentCategoryId?: string | null;
  sortOrder: number;
  status: ECategoryStatus | string;
}

export interface CreateCategoryRequestDto {
  categoryCode: string;
  name: string;
  description?: string | null;
  parentCategoryId?: string | null;
  sortOrder: number;
}

export interface UpdateCategoryRequestDto {
  name: string;
  description?: string | null;
  parentCategoryId?: string | null;
  sortOrder: number;
  status: ECategoryStatus;
}

export interface ProductCategoryDto {
  id: string;
  categoryCode: string;
  name: string;
}

export interface ProductColorDto {
  id: string;
  code: string;
  name: string;
  hexCode?: string | null;
}

export interface ProductSizeDto {
  id: string;
  code: string;
  name: string;
}

export interface ProductVariantDto {
  id: string;
  sku: string;
  color?: ProductColorDto | null;
  size?: ProductSizeDto | null;
  price: number;
  status: EProductVariantStatus | string;
}

export interface ProductDto {
  id: string;
  sku: string;
  productCode?: string | null;
  name: string;
  description?: string | null;
  categoryId?: string | null;
  category?: ProductCategoryDto | null;
  status?: EProductStatus | string | null;
  minPrice?: number | null;
  maxPrice?: number | null;
  variants?: ProductVariantDto[] | null;
  isActive: boolean;
  version: number;
  updatedAt: string;
  isDelete: boolean;
  deleteByUser?: string | null;
  deletedAt?: string | null;
}

export interface CreateProductVariantRequestDto {
  colorId: string;
  sizeId: string;
  price: number;
  status: EProductVariantStatus;
}

export interface CreateProductRequestDto {
  productCode: string;
  name: string;
  description?: string | null;
  categoryId: string;
  variants?: CreateProductVariantRequestDto[];
}

export interface UpdateProductRequestDto {
  name: string;
  description?: string | null;
  categoryId: string;
  status: EProductStatus;
}

export interface UpdateProductVariantRequestDto extends CreateProductVariantRequestDto {
}

export interface OrderLineInput {
  productVariantId: string;
  quantity: number;
  discountPercent: number;
}

export interface OrderLineDto extends OrderLineInput {
  productId?: string | null;
  productCode?: string | null;
  sku: string;
  productName: string;
  colorCode?: string | null;
  colorName?: string | null;
  sizeCode?: string | null;
  unitPrice: number;
  lineTotal: number;
}

export interface OrderDto {
  id: string;
  customerId: string;
  customerName: string;
  customerPhone: string;
  createdAt: string;
  status: EOrderStatus | string;
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

export interface ReservationDto {
  orderId?: string | null;
  status?: string | null;
  lines?: ReservationLineDto[] | null;
  expiresAt?: string | null;
  createdAt?: string | null;
}

export interface ReservationLineDto {
  productId?: string | null;
  productVariantId?: string | null;
  sku?: string | null;
  quantity?: number | null;
}
