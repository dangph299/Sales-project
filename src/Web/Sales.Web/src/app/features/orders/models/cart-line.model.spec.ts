import {
  CartLine,
  cartGrandTotal,
  cartLineTotal,
  cartSubtotal,
  normalizeDiscountPercent,
  normalizeQuantity
} from './cart-line.model';

function line(price: number, quantity: number, discountPercent: number): CartLine {
  return {
    product: { id: 'p', sku: 'P', name: 'Product' },
    variant: { id: 'v', sku: 'V', price, status: 'Published' },
    quantity,
    discountPercent
  };
}

describe('cart line maths', () => {
  it('clamps quantity to at least one', () => {
    expect(normalizeQuantity(0)).toBe(1);
    expect(normalizeQuantity(-5)).toBe(1);
    expect(normalizeQuantity(Number.NaN)).toBe(1);
    expect(normalizeQuantity(4)).toBe(4);
  });

  it('clamps discount percent to the 0-100 range', () => {
    expect(normalizeDiscountPercent(-10)).toBe(0);
    expect(normalizeDiscountPercent(150)).toBe(100);
    expect(normalizeDiscountPercent(Number.NaN)).toBe(0);
    expect(normalizeDiscountPercent(25)).toBe(25);
  });

  it('applies the discount to the line total and rounds', () => {
    expect(cartLineTotal(line(100_000, 2, 10))).toBe(180_000);
    expect(cartLineTotal(line(99_999, 1, 33))).toBe(Math.round(99_999 * 0.67));
  });

  it('subtotal ignores discounts while grand total applies them', () => {
    const lines = [line(100_000, 2, 50), line(50_000, 1, 0)];

    expect(cartSubtotal(lines)).toBe(250_000);
    expect(cartGrandTotal(lines)).toBe(150_000);
  });
});
