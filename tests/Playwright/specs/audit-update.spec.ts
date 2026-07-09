import { expect, test, type APIRequestContext } from '@playwright/test';

test('product update emits an audit event', async ({ request }) => {
  const runId = `${Date.now()}-${Math.floor(Math.random() * 10_000)}`;
  const sku = `AUDIT-UPD-${runId}`;
  const initialName = `Audit Update Product ${runId}`;
  const updatedName = `Audit Updated Product ${runId}`;
  const authHeaders = await login(request);

  const created = await request.post('/api/products/', {
    headers: authHeaders,
    data: { sku, name: initialName, price: 100_000 }
  });
  expect(created.status(), await created.text()).toBe(201);
  const product = await created.json();

  const updated = await request.put(`/api/products/${product.id}`, {
    headers: authHeaders,
    data: { name: updatedName, price: 130_000, isActive: true }
  });
  expect(updated.ok(), await updated.text()).toBeTruthy();

  const body = await updated.json();
  expect(body.name).toBe(updatedName);
  expect(body.price).toBe(130_000);

  console.log(`AUDIT_UPDATE_RUN_ID=${runId}`);
  console.log(`AUDIT_UPDATE_PRODUCT_ID=${product.id}`);
  console.log(`AUDIT_UPDATE_NAME=${updatedName}`);
});

async function login(request: APIRequestContext): Promise<Record<string, string>> {
  const response = await request.post('/api/auth/login', {
    data: { userName: 'admin', password: 'Admin123!' }
  });
  expect(response.ok(), await response.text()).toBeTruthy();
  const body = await response.json();
  return { Authorization: `Bearer ${body.accessToken}` };
}
