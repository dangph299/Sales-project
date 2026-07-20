import { expect, request as playwrightRequest, test, type APIRequestContext } from '@playwright/test';
import { blackColorId, mediumSizeId, uncategorizedCategoryId } from './reference-data';

const inventoryUrl = process.env.INVENTORY_API_URL ?? 'http://localhost:5001';

test('order can be confirmed again after undo confirm', async ({ request }) => {
  const inventory = await playwrightRequest.newContext({
    baseURL: inventoryUrl,
    extraHTTPHeaders: { Accept: 'application/json' }
  });

  try {
    const runId = `${Date.now()}-${Math.floor(Math.random() * 10_000)}`;
    const sku = `RECONFIRM-${runId}`;
    const authHeaders = await login(request);

    const productResponse = await request.post('/api/products/', {
      headers: authHeaders,
      data: {
        productCode: sku,
        name: `Reconfirm Product ${runId}`,
        description: null,
        categoryId: uncategorizedCategoryId,
        variants: [{ colorId: blackColorId, sizeId: mediumSizeId, price: 100_000, status: 'Published' }]
      }
    });
    expect(productResponse.status(), await productResponse.text()).toBe(201);
    const product = await productResponse.json();
    const productVariant = product.variants[0];

    const stockResponse = await inventory.post(`/api/inventory/${productVariant.id}/adjust`, {
      headers: authHeaders,
      data: { sku: productVariant.sku, quantityDelta: 10 }
    });
    expect(stockResponse.ok(), await stockResponse.text()).toBeTruthy();

    const customerResponse = await request.post('/api/customers/', {
      headers: authHeaders,
      data: { name: `Reconfirm Customer ${runId}`, phone: `+849${runId.replace(/\D/g, '').slice(-8).padStart(8, '0')}` }
    });
    expect(customerResponse.status(), await customerResponse.text()).toBe(201);
    const customer = await customerResponse.json();

    const orderResponse = await request.post('/api/orders/', {
      headers: authHeaders,
      data: {
        customerId: customer.id,
        lines: [{ productVariantId: productVariant.id, quantity: 2, discountPercent: 0 }]
      }
    });
    expect(orderResponse.status(), await orderResponse.text()).toBe(201);
    const order = await orderResponse.json();

    await confirmAndWaitForStatus(request, authHeaders, order.id, orderResponse.headers().etag, 'Confirmed');

    const confirmed = await getOrder(request, authHeaders, order.id);
    const undoResponse = await request.post(`/api/orders/${order.id}/undo-confirm`, {
      headers: { ...authHeaders, 'If-Match': confirmed.etag! }
    });
    expect(undoResponse.status(), await undoResponse.text()).toBe(200);
    expect((await undoResponse.json()).status).toBe('Draft');

    await expect
      .poll(async () => {
        const response = await inventory.get(`/api/inventory/reservations/${order.id}`, { headers: authHeaders });
        expect(response.ok(), await response.text()).toBeTruthy();
        return (await response.json()).status;
      }, { timeout: 30_000, intervals: [500, 1_000, 2_000] })
      .toBe('Released');

    const draft = await getOrder(request, authHeaders, order.id);
    await confirmAndWaitForStatus(request, authHeaders, order.id, draft.etag, 'Confirmed');
  } finally {
    await inventory.dispose();
  }
});

async function confirmAndWaitForStatus(
  request: APIRequestContext,
  authHeaders: Record<string, string>,
  orderId: string,
  etag: string | undefined,
  expectedStatus: string)
{
  expect(etag).toBeTruthy();
  const confirmResponse = await request.post(`/api/orders/${orderId}/confirm`, {
    headers: { ...authHeaders, 'If-Match': etag! }
  });
  expect(confirmResponse.status(), await confirmResponse.text()).toBe(200);
  expect((await confirmResponse.json()).status).toBe('PendingInventory');

  await expect
    .poll(async () => {
      const order = await getOrder(request, authHeaders, orderId);
      return order.body.status;
    }, { timeout: 30_000, intervals: [500, 1_000, 2_000] })
    .toBe(expectedStatus);
}

async function getOrder(request: APIRequestContext, authHeaders: Record<string, string>, orderId: string) {
  const response = await request.get(`/api/orders/${orderId}`, { headers: authHeaders });
  expect(response.ok(), await response.text()).toBeTruthy();
  return { body: await response.json(), etag: response.headers().etag };
}

async function login(request: APIRequestContext): Promise<Record<string, string>> {
  const response = await request.post('/api/auth/login', {
    data: { userName: 'admin', password: 'Admin123!' }
  });
  expect(response.ok(), await response.text()).toBeTruthy();
  const body = await response.json();
  return { Authorization: `Bearer ${body.accessToken}` };
}
