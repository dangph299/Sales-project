import { ProductSizeDto } from '../models';

export const seededSizes: (ProductSizeDto & { sortOrder: number })[] = [
  { id: '20000000-0000-0000-0000-000000000001', code: 'XXS', name: 'Extra Extra Small', sortOrder: 10 },
  { id: '20000000-0000-0000-0000-000000000002', code: 'XS', name: 'Extra Small', sortOrder: 20 },
  { id: '20000000-0000-0000-0000-000000000003', code: 'S', name: 'Small', sortOrder: 30 },
  { id: '20000000-0000-0000-0000-000000000004', code: 'M', name: 'Medium', sortOrder: 40 },
  { id: '20000000-0000-0000-0000-000000000005', code: 'L', name: 'Large', sortOrder: 50 },
  { id: '20000000-0000-0000-0000-000000000006', code: 'XL', name: 'Extra Large', sortOrder: 60 },
  { id: '20000000-0000-0000-0000-000000000007', code: 'XXL', name: 'Extra Extra Large', sortOrder: 70 },
  { id: '20000000-0000-0000-0000-000000000008', code: 'XXXL', name: 'Extra Extra Extra Large', sortOrder: 80 }
];
