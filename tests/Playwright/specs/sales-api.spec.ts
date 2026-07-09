import { expect, request as playwrightRequest, test } from '@playwright/test';

const inventoryUrl = process.env.INVENTORY_API_URL ?? 'http://localhost:5001';

test.describe.serial('Sales API smoke and concurrency', () => {
  let accessToken: string;
  let productId: string;
  let customerId: string;
  let orderId: string;
  const runId = `${Date.now()}-${Math.floor(Math.random() * 10_000)}`;
  const sku = `PW-${runId}`;
  const customerName = `Playwright Customer ${runId}`;
  const phone = `849${runId.replace(/\D/g, '').slice(-8).padStart(8, '0')}`;

  test('health endpoints are ready', async ({ request }) => {
    const sales = await request.get('/health');
    expect(sales.ok(), await sales.text()).toBeTruthy();

    const inventory = await playwrightRequest.newContext({ baseURL: inventoryUrl });
    try {
      const response = await inventory.get('/health');
      expect(response.ok(), await response.text()).toBeTruthy();
    } finally {
      await inventory.dispose();
    }
  });

  test('login with seeded admin', async ({ request }) => {
    const response = await request.post('/api/auth/login', {
      data: { userName: 'admin', password: 'Admin123!' }
    });
    expect(response.ok(), await response.text()).toBeTruthy();
    const body = await response.json();
    expect(body.accessToken).toBeTruthy();
    expect(body.refreshToken).toBeTruthy();
    accessToken = body.accessToken;
  });

  test('create and search product', async ({ request }) => {
    const created = await request.post('/api/products/', {
      headers: auth(),
      data: { sku, name: `Playwright Product ${runId}`, price: 125_000 }
    });
    expect(created.status(), await created.text()).toBe(201);
    const product = await created.json();
    productId = product.id;

    const searched = await request.get('/api/products/', {
      headers: auth(),
      params: { name: 'Playwright Product', page: 1, pageSize: 20 }
    });
    expect(searched.ok(), await searched.text()).toBeTruthy();
    expect((await searched.json()).items.some((x: { id: string }) => x.id === productId)).toBeTruthy();
  });

  test('create customer and search phone by prefix and suffix', async ({ request }) => {
    const created = await request.post('/api/customers/', {
      headers: auth(),
      data: { name: customerName, phone: `+${phone}` }
    });
    expect(created.status(), await created.text()).toBe(201);
    const customer = await created.json();
    customerId = customer.id;

    for (const [phoneMatch, value] of [['prefix', phone.slice(0, 4)], ['suffix', phone.slice(-4)]]) {
      const searched = await request.get('/api/customers/', {
        headers: auth(),
        params: { phone: value, phoneMatch, page: 1, pageSize: 20 }
      });
      expect(searched.ok(), await searched.text()).toBeTruthy();
      expect((await searched.json()).items.some((x: { id: string }) => x.id === customerId)).toBeTruthy();
    }
  });

  test('single update succeeds and two updates with the same ETag produce one conflict', async ({ request }) => {
    const created = await request.post('/api/orders/', {
      headers: auth(),
      data: {
        customerId,
        lines: [{ productId, quantity: 1, discountPercent: 10 }]
      }
    });
    expect(created.status(), await created.text()).toBe(201);
    orderId = (await created.json()).id;
    const createdEtag = created.headers().etag;
    expect(createdEtag).toBeTruthy();

    const initialUpdate = await request.put(`/api/orders/${orderId}/lines`, {
      headers: { ...auth(), 'If-Match': createdEtag! },
      data: [{ productId, quantity: 2, discountPercent: 10 }]
    });
    expect(initialUpdate.status(), await initialUpdate.text()).toBe(200);
    const etag = initialUpdate.headers().etag;
    expect(etag).toBeTruthy();

    const update = (quantity: number) => request.put(`/api/orders/${orderId}/lines`, {
      headers: { ...auth(), 'If-Match': etag! },
      data: [{ productId, quantity, discountPercent: 10 }]
    });
    const responses = await Promise.all([update(3), update(4)]);
    expect(responses.map(x => x.status()).sort()).toEqual([200, 409]);

    const detail = await request.get(`/api/orders/${orderId}`, { headers: auth() });
    expect(detail.ok(), await detail.text()).toBeTruthy();
    const order = await detail.json();
    expect([3, 4]).toContain(order.totalQuantity);
    expect(order.total).toBe(order.totalQuantity * 112_500);
  });

  test('two confirms with the same ETag let exactly one order reach inventory', async ({ request }) => {
    const detail = await request.get(`/api/orders/${orderId}`, { headers: auth() });
    expect(detail.ok(), await detail.text()).toBeTruthy();
    const etag = detail.headers().etag;
    expect(etag).toBeTruthy();

    const confirm = () => request.post(`/api/orders/${orderId}/confirm`, {
      headers: { ...auth(), 'If-Match': etag! }
    });
    const responses = await Promise.all([confirm(), confirm()]);
    expect(responses.map(x => x.status()).sort()).toEqual([200, 409]);

    const confirmed = await request.get(`/api/orders/${orderId}`, { headers: auth() });
    expect(confirmed.ok(), await confirmed.text()).toBeTruthy();
    const order = await confirmed.json();
    expect(order.status).toBe('PendingInventory');
    expect(order.version).toBe(4);
  });

  function auth(): Record<string, string> {
    return { Authorization: `Bearer ${accessToken}` };
  }
});
