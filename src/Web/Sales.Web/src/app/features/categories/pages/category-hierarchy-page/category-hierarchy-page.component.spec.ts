import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { ApiClientError } from '../../../../core/api/api-client-result';
import { CategoryApiService } from '../../api/category-api.service';
import { CategoryResponse } from '../../api/responses/category.response';
import { CategoryHierarchyPageComponent } from './category-hierarchy-page.component';

const hierarchy: CategoryResponse[] = [
  category('blank', 'BLANK', 'Blank', null, 1),
  category('shirt', 'SHIRT', 'Shirt', 'blank', 1),
  category('tshirt', 'TSHIRT', 'TShirt', 'shirt', 1)
];

describe('CategoryHierarchyPageComponent expansion', () => {
  let fixture: ComponentFixture<CategoryHierarchyPageComponent>;
  let component: CategoryHierarchyPageComponent;
  let categoryApi: jasmine.SpyObj<CategoryApiService>;

  beforeEach(async () => {
    categoryApi = jasmine.createSpyObj<CategoryApiService>('CategoryApiService', ['list', 'create']);
    categoryApi.list.and.resolveTo(hierarchy);

    await TestBed.configureTestingModule({
      imports: [CategoryHierarchyPageComponent],
      providers: [
        provideNoopAnimations(),
        { provide: CategoryApiService, useValue: categoryApi }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(CategoryHierarchyPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  });

  it('clicking a collapsed parent shows its direct children', () => {
    expect(rowNames()).toEqual(['Blank']);

    clickToggle('Expand Blank');
    expect(rowNames()).toEqual(['Blank', 'Shirt']);
  });

  it('clicking an expanded parent hides all descendants', () => {
    clickToggle('Expand Blank');
    clickToggle('Expand Shirt');
    expect(rowNames()).toEqual(['Blank', 'Shirt', 'TShirt']);

    clickToggle('Collapse Blank');
    expect(rowNames()).toEqual(['Blank']);
  });

  it('expanding a child shows the next hierarchy level', () => {
    clickToggle('Expand Blank');
    clickToggle('Expand Shirt');

    expect(rowNames()).toEqual(['Blank', 'Shirt', 'TShirt']);
  });

  it('leaf rows do not render an active expand control', () => {
    clickToggle('Expand Blank');
    clickToggle('Expand Shirt');

    expect(toggle('Expand TShirt')).toBeNull();
    expect(toggle('Collapse TShirt')).toBeNull();
  });

  it('Expand All shows all hierarchy levels', () => {
    component.expandAll();
    fixture.detectChanges();

    expect(rowNames()).toEqual(['Blank', 'Shirt', 'TShirt']);
  });

  it('Collapse All hides every descendant', () => {
    component.expandAll();
    fixture.detectChanges();
    component.collapseAll();
    fixture.detectChanges();

    expect(rowNames()).toEqual(['Blank']);
  });

  it('expansion uses category IDs, not row indexes or object references', () => {
    component.toggleCategory('blank');
    fixture.detectChanges();
    component.categories.set([...hierarchy].reverse());
    fixture.detectChanges();

    expect(rowNames()).toEqual(['Blank', 'Shirt']);
  });

  it('filtering does not make expand controls stop working after filters are cleared', () => {
    component.searchText.set('TShirt');
    fixture.detectChanges();
    expect(rowNames()).toEqual(['Blank', 'Shirt', 'TShirt']);

    component.searchText.set('');
    fixture.detectChanges();
    expect(rowNames()).toEqual(['Blank']);

    clickToggle('Expand Blank');
    expect(rowNames()).toEqual(['Blank', 'Shirt']);
  });

  it('keeps the create modal open when the API returns a duplicate unique conflict', async () => {
    categoryApi.create.and.rejectWith(new ApiClientError({
      isSuccess: false,
      status: 409,
      statusText: 'Conflict',
      data: null,
      message: 'A resource with the same unique value already exists.',
      errorCode: 'unique_violation',
      correlationId: 'corr-1',
      traceId: 'trace-1',
      errors: [],
      validationErrors: []
    }));

    component.openCreateCategory();
    component.categoryForm = {
      name: 'Blank',
      description: '',
      parentCategoryId: '',
      sortOrder: 1,
      status: 'Draft'
    };

    await component.saveCategory();
    fixture.detectChanges();

    expect(component.categoryModalOpen()).toBeTrue();
    expect(component.errorMessage()).toContain('same unique value');
  });

  function clickToggle(label: string): void {
    const button = toggle(label);
    expect(button).withContext(label).not.toBeNull();
    button!.click();
    fixture.detectChanges();
  }

  function toggle(label: string): HTMLButtonElement | null {
    return fixture.nativeElement.querySelector(`button[aria-label="${label}"]`);
  }

  function rowNames(): string[] {
    return Array.from(fixture.nativeElement.querySelectorAll('.category-name'))
      .map(element => (element as HTMLElement).textContent?.trim() ?? '');
  }
});

function category(
  id: string,
  categoryCode: string,
  name: string,
  parentCategoryId: string | null,
  sortOrder: number): CategoryResponse {
  return {
    id,
    categoryCode,
    name,
    parentCategoryId,
    sortOrder,
    description: null,
    status: 'Draft'
  };
}
