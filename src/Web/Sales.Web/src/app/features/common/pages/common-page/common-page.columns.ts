import { TableColumn } from '@shared/models/table.model';
import { ColorResponse } from '../../contracts/color.response';
import { SizeLookupResponse } from '../../contracts/size-lookup.response';

export const colorListColumns: TableColumn<ColorResponse>[] = [
  { key: 'preview', header: 'Preview', type: 'custom' },
  { key: 'code', header: 'Code' },
  { key: 'name', header: 'Name' },
  { key: 'hexCode', header: 'Hex', valueFormatter: row => row.hexCode || '-' }
];

export const sizeListColumns: TableColumn<SizeLookupResponse>[] = [
  { key: 'code', header: 'Code' },
  { key: 'name', header: 'Name' }
];
