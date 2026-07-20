import { expect, test, type APIRequestContext } from '@playwright/test';
import { execFile } from 'node:child_process';
import { promisify } from 'node:util';
import { blackColorId, mediumSizeId, uncategorizedCategoryId } from './reference-data';

const execFileAsync = promisify(execFile);

test('product update emits an audit event', async ({ request }) => {
  const runId = `${Date.now()}-${Math.floor(Math.random() * 10_000)}`;
  const sku = `AUDIT-UPD-${runId}`;
  const initialName = `Audit Update Product ${runId}`;
  const updatedName = `Audit Updated Product ${runId}`;
  const authHeaders = await login(request);

  const created = await request.post('/api/products/', {
    headers: authHeaders,
    data: {
      productCode: sku,
      name: initialName,
      description: null,
      categoryId: uncategorizedCategoryId,
      variants: [{ colorId: blackColorId, sizeId: mediumSizeId, price: 100_000, status: 'Published' }]
    }
  });
  expect(created.status(), await created.text()).toBe(201);
  const product = unwrap(await created.json());

  const updated = await request.put(`/api/products/${product.id}`, {
    headers: authHeaders,
    data: { name: updatedName, description: null, categoryId: uncategorizedCategoryId, status: 'Published' }
  });
  expect(updated.ok(), await updated.text()).toBeTruthy();

  const body = unwrap(await updated.json());
  expect(body.name).toBe(updatedName);
  expect(body.minPrice).toBe(100_000);

  const auditDocument = await waitForProductUpdateAudit(product.id, updatedName);
  expect(auditDocument.serviceName).toBe('Sales');
  expect(auditDocument.eventType).toBe('ProductUpdated');
  expect(auditDocument.entityType).toBe('Product');
  expect(auditDocument.entityId).toBe(product.id);
  expect(auditDocument.action).toBe('Updated');
  expect(auditDocument.schemaVersion).toBe(1);
  expect(auditDocument.changes).toContainEqual(expect.objectContaining({
    propertyPath: 'Name',
    oldValue: initialName,
    newValue: updatedName
  }));
});

async function login(request: APIRequestContext): Promise<Record<string, string>> {
  const response = await request.post('/api/auth/login', {
    data: { userName: 'admin', password: 'Admin123!' }
  });
  expect(response.ok(), await response.text()).toBeTruthy();
  const body = unwrap(await response.json());
  return { Authorization: `Bearer ${body.accessToken}` };
}

function unwrap<T>(body: T | { data: T }): T {
  return body && typeof body === 'object' && 'data' in body ? body.data : body;
}

async function waitForProductUpdateAudit(productId: string, updatedName: string): Promise<AuditDocument> {
  const deadline = Date.now() + 30_000;
  let lastDocument: AuditDocument | null = null;
  while (Date.now() < deadline) {
    lastDocument = await findProductUpdateAudit(productId, updatedName);
    if (lastDocument) {
      return lastDocument;
    }

    await new Promise(resolve => setTimeout(resolve, 1_000));
  }

  expect(lastDocument, `audit event for product ${productId}`).not.toBeNull();
  throw new Error(`Audit event for product ${productId} was not found.`);
}

async function findProductUpdateAudit(productId: string, updatedName: string): Promise<AuditDocument | null> {
  const { stdout } = await execFileAsync('dotnet', [
    'run',
    '--project',
    'AuditProbe/AuditProbe.csproj',
    '--',
    productId,
    updatedName
  ], { cwd: '../Playwright' });
  const text = stdout.trim();
  return text && text !== 'null' ? JSON.parse(text) as AuditDocument : null;
}

interface AuditDocument {
  serviceName: string;
  eventType: string;
  entityType: string;
  entityId: string;
  action: string;
  schemaVersion: number;
  changes: Array<{
    propertyPath: string;
    oldValue: unknown;
    newValue: unknown;
  }>;
}
