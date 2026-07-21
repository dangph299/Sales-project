import { formatMoney, formatPriceRange } from './display-formatters';

describe('display formatters', () => {
  it('formats VND money consistently', () => {
    expect(formatMoney(150000)).toBe('150,000 ₫');
  });

  it('formats product price range from variants', () => {
    expect(formatPriceRange(150000, 150000)).toBe('150,000 ₫');
    expect(formatPriceRange(150000, 190000)).toBe('150,000 ₫ - 190,000 ₫');
  });

  it('does not invent a product price when range is missing', () => {
    expect(formatPriceRange(null, null)).toBe('No variant price');
  });
});
