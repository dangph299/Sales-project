import { expect, request as playwrightRequest, test, type APIRequestContext } from '@playwright/test';

const inventoryUrl = process.env.INVENTORY_API_URL ?? 'http://localhost:5001';

test('order confirmation is processed through Kafka inventory flow', async ({ request }) => {
  const inventory = await playwrightRequest.newContext({
    baseURL: inventoryUrl,
    extraHTTPHeaders: { Accept: 'application/json' }
  });

  try {
    const runId = `${Date.now()}-${Math.floor(Math.random() * 10_000)}`;
    const sku = `KAFKA-${runId}`;
    const authHeaders = await login(request);

    const productResponse = await request.post('/api/products/', {
      headers: authHeaders,
      data: { sku, name: `Kafka Flow Product ${runId}`, price: 100_000 }
    });
    expect(productResponse.status(), await productResponse.text()).toBe(201);
    const product = await productResponse.json();

    const stockResponse = await inventory.post(`/api/inventory/${product.id}/adjust`, {
      headers: authHeaders,
      data: { sku, quantityDelta: 10 }
    });
    expect(stockResponse.ok(), await stockResponse.text()).toBeTruthy();

    const customerResponse = await request.post('/api/customers/', {
      headers: authHeaders,
      data: { name: `Kafka Flow Customer ${runId}`, phone: `+849${runId.replace(/\D/g, '').slice(-8).padStart(8, '0')}` }
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
    const orderEtag = orderResponse.headers().etag;
    expect(orderEtag).toBeTruthy();

    const confirmResponse = await request.post(`/api/orders/${order.id}/confirm`, {
      headers: { ...authHeaders, 'If-Match': orderEtag! }
    });
    expect(confirmResponse.ok(), await confirmResponse.text()).toBeTruthy();
    expect((await confirmResponse.json()).status).toBe('PendingInventory');

    await expect
      .poll(async () => {
        const response = await request.get(`/api/orders/${order.id}`, { headers: authHeaders });
        expect(response.ok(), await response.text()).toBeTruthy();
        return (await response.json()).status;
      }, { timeout: 30_000, intervals: [500, 1_000, 2_000] })
      .toBe('Confirmed');

    const reservationResponse = await inventory.get(`/api/inventory/reservations/${order.id}`, { headers: authHeaders });
    expect(reservationResponse.ok(), await reservationResponse.text()).toBeTruthy();
  } finally {
    await inventory.dispose();
  }
});

async function login(request: APIRequestContext): Promise<Record<string, string>> {
  const response = await request.post('/api/auth/login', {
    data: { userName: 'admin', password: 'Admin123!' }
  });
  expect(response.ok(), await response.text()).toBeTruthy();
  const body = await response.json();
  return { Authorization: `Bearer ${body.accessToken}` };
}
