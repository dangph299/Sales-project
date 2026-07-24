import { OrderCustomerRequest } from '../../api/requests/order-customer.request';
import { OrderLineRequest } from '../../api/requests/order-line.request';
import { OrderResponse } from '../../api/responses/order.response';
import { OrderCustomerFormErrors } from '../../components/order-customer-form/order-customer-form.component';
import { CartLine } from '../../models/cart-line.model';

export function emptyOrderCustomer(): OrderCustomerRequest {
  return { name: '', phone: '', email: '', address: '' };
}

export function toOrderLineRequests(lines: readonly CartLine[]): OrderLineRequest[] {
  return lines.map(line => ({
    productVariantId: line.variant.id,
    quantity: line.quantity,
    discountPercent: line.discountPercent
  }));
}

export function hasCustomerChanged(customer: OrderCustomerRequest, order: OrderResponse): boolean {
  return customer.name !== order.customerName
    || customer.phone !== order.customerPhone
    || (customer.email || '') !== (order.customerEmail || '')
    || (customer.address || '') !== (order.customerAddress || '');
}

export function haveLinesChanged(lines: readonly CartLine[], order: OrderResponse): boolean {
  const requests = toOrderLineRequests(lines);
  if (requests.length !== order.lines.length) {
    return true;
  }

  return requests.some(line => {
    const existing = order.lines.find(x => x.productVariantId === line.productVariantId);
    return !existing || existing.quantity !== line.quantity || existing.discountPercent !== line.discountPercent;
  });
}

export function toCartLines(order: OrderResponse): CartLine[] {
  return order.lines.map(line => ({
    product: {
      id: line.productId ?? line.productVariantId,
      sku: line.sku,
      productCode: line.productCode,
      name: line.productName,
      status: 'Published',
      variants: []
    },
    variant: {
      id: line.productVariantId,
      sku: line.sku,
      color: line.colorCode || line.colorName
        ? { id: line.colorCode ?? line.productVariantId, code: line.colorCode ?? '', name: line.colorName ?? line.colorCode ?? '' }
        : null,
      size: line.sizeCode
        ? { id: line.sizeCode, code: line.sizeCode, name: line.sizeCode }
        : null,
      price: line.unitPrice,
      status: 'Published'
    },
    quantity: line.quantity,
    discountPercent: line.discountPercent
  }));
}

export interface OrderFormValidationResult {
  customerErrors: OrderCustomerFormErrors;
  formErrors: { lines?: string };
  isValid: boolean;
}

/**
 * Explains every problem rather than just disabling Save, so the user can see
 * what to fix instead of guessing why the button does nothing.
 */
export function validateOrderForm(
  customer: OrderCustomerRequest,
  lines: readonly CartLine[]
): OrderFormValidationResult {
  const customerErrors: OrderCustomerFormErrors = {};
  if (!customer.name.trim()) {
    customerErrors.name = 'Customer name is required.';
  }

  const customerPhoneDigits = customer.phone.replace(/\D/g, '');
  if (!customer.phone.trim()) {
    customerErrors.phone = 'Phone is required.';
  } else if (customerPhoneDigits.length < 9 || customerPhoneDigits.length > 15) {
    customerErrors.phone = 'Phone must contain 9 to 15 digits.';
  }

  const formErrors: { lines?: string } = {};
  if (lines.length === 0) {
    formErrors.lines = 'Add at least one product.';
  }

  return {
    customerErrors,
    formErrors,
    isValid: !customerErrors.name && !customerErrors.phone && !formErrors.lines
  };
}
