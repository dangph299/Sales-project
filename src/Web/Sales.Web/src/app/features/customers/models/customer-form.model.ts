export interface CustomerFormModel {
  name: string;
  phone: string;
  email: string;
  address: string;
}

export function emptyCustomerForm(): CustomerFormModel {
  return {
    name: 'Jane Smith',
    phone: `090${Math.floor(1000000 + Math.random() * 8999999)}`,
    email: '',
    address: ''
  };
}
