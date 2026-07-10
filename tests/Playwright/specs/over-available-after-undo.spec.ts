import { expect, request as playwrightRequest, test, type APIRequestContext } from '@playwright/test';

const inventoryUrl = process.env.INVENTORY_API_URL ?? 'http://localhost:5001';

test('confirm after undo with quantity above available does not get stuck', async ({ request }) => {
  test.setTimeout(150_000);

  const inventory = await playwrightRequest.newContext({
    baseURL: inventoryUrl,
    extraHTTPHeaders: { Accept: 'application/json' }
  });

  try {
    const runId = `${Date.now()}-${Math.floor(Math.random() * 10_000)}`;
    const sku = `OVER-AVAILABLE-${runId}`;
    const authHeaders = await login(request);

    const productResponse = await request.post('/api/products/', {
      headers: authHeaders,
      data: { sku, name: `Over Available Product ${runId}`, price: 100_000 }
    });
    expect(productResponse.status(), await productResponse.text()).toBe(201);
    const product = await productResponse.json();

    const stockResponse = await inventory.post(`/api/inventory/${product.id}/adjust`, {
      headers: authHeaders,
      data: { sku, quantityDelta: 2 }
    });
    expect(stockResponse.ok(), await stockResponse.text()).toBeTruthy();

    const customerResponse = await request.post('/api/customers/', {
      headers: authHeaders,
      data: { name: `Over Available Customer ${runId}`, phone: `+849${runId.replace(/\D/g, '').slice(-8).padStart(8, '0')}` }
    });
    expect(customerResponse.status(), await customerResponse.text()).toBe(201);
    const customer = await customerResponse.json();

    const orderResponse = await request.post('/api/orders/', {
      headers: authHeaders,
      data: {
        customerId: customer.id,
        lines: [{ productId: product.id, quantity: 2, discountPercent: 0 }]
      }
    });
    expect(orderResponse.status(), await orderResponse.text()).toBe(201);
    const order = await orderResponse.json();
    console.log(`order=${order.id}`);

    await confirmAndWaitForStatus(request, authHeaders, order.id, orderResponse.headers().etag, 'Confirmed');
    console.log('first confirm -> Confirmed');

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
    console.log('undo confirm -> Draft / reservation Released');

    const draft = await getOrder(request, authHeaders, order.id);
    const updateResponse = await request.put(`/api/orders/${order.id}/lines`, {
      headers: { ...authHeaders, 'If-Match': draft.etag! },
      data: [{ productId: product.id, quantity: 3, discountPercent: 0 }]
    });
    expect(updateResponse.status(), await updateResponse.text()).toBe(200);
    const updated = await updateResponse.json();
    expect(updated.totalQuantity).toBe(3);
    console.log('updated quantity -> 3 while available stock is 2');

    const reconfirmResponse = await request.post(`/api/orders/${order.id}/confirm`, {
      headers: { ...authHeaders, 'If-Match': updateResponse.headers().etag! }
    });
    expect(reconfirmResponse.status(), await reconfirmResponse.text()).toBe(200);
    expect((await reconfirmResponse.json()).status).toBe('PendingInventory');

    await expect
      .poll(async () => {
        const current = await getOrder(request, authHeaders, order.id);
        console.log(`status=${current.body.status}`);
        return current.body.status;
      }, { timeout: 120_000, intervals: [500, 1_000, 2_000, 5_000] })
      .toBe('InventoryRejected');
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
