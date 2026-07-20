export function formatMoney(amount: number | null | undefined): string {
  if (amount === null || amount === undefined) {
    return 'Chua co gia';
  }

  return `${new Intl.NumberFormat('vi-VN').format(amount)} ₫`;
}

export function formatPriceRange(minPrice: number | null | undefined, maxPrice: number | null | undefined): string {
  if (minPrice === null || minPrice === undefined || maxPrice === null || maxPrice === undefined) {
    return 'Chua co gia variant';
  }

  if (minPrice === maxPrice) {
    return formatMoney(minPrice);
  }

  return `${formatMoney(minPrice)} - ${formatMoney(maxPrice)}`;
}

export function formatDateTime(text: string | null | undefined): string {
  if (!text) {
    return '-';
  }

  const date = new Date(text);
  if (Number.isNaN(date.getTime())) {
    return '-';
  }

  return new Intl.DateTimeFormat('vi-VN', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  }).format(date);
}

export function compactText(text: string | null | undefined, maxLength = 48): string {
  if (!text) {
    return '-';
  }

  if (text.length <= maxLength) {
    return text;
  }

  return `${text.slice(0, maxLength - 1)}...`;
}
