import { Component } from '@angular/core';
import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { AutocompleteComponent } from './autocomplete.component';

interface Option {
  id: string;
  label: string;
}

@Component({
  standalone: true,
  imports: [FormsModule, AutocompleteComponent],
  template: `
    <app-autocomplete
      label="Customer"
      name="customer"
      [(ngModel)]="value"
      [searchFn]="search"
      [displayWith]="display"
      [minSearchLength]="2"
      [debounceMs]="100"
      (selected)="selected = $event">
    </app-autocomplete>
  `
})
class HostComponent {
  value: Option | string | null = null;
  selected: Option | null = null;
  terms: string[] = [];

  search = (term: string): Promise<Option[]> => {
    this.terms.push(term);
    return Promise.resolve([{ id: term, label: `Option ${term}` }]);
  };

  display = (value: Option | string | null): string => {
    if (!value) {
      return '';
    }

    return typeof value === 'string' ? value : value.label;
  };
}

describe('AutocompleteComponent', () => {
  let fixture: ComponentFixture<HostComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HostComponent, NoopAnimationsModule]
    }).compileComponents();

    fixture = TestBed.createComponent(HostComponent);
    fixture.detectChanges();
  });

  it('debounces search and respects minimum length', fakeAsync(() => {
    const autocomplete = fixture.debugElement.children[0].componentInstance as AutocompleteComponent<Option>;

    autocomplete.changeText('a');
    tick(100);
    autocomplete.changeText('ab');
    tick(100);

    expect(fixture.componentInstance.terms).toEqual(['ab']);
  }));

  it('selects and clears options through ControlValueAccessor', () => {
    const autocomplete = fixture.debugElement.children[0].componentInstance as AutocompleteComponent<Option>;
    const option = { id: '1', label: 'Option 1' };

    autocomplete.selectOption(option, { isUserInput: true } as never);
    fixture.detectChanges();

    expect(fixture.componentInstance.value).toEqual(option);
    expect(fixture.componentInstance.selected).toEqual(option);

    autocomplete.clear();
    fixture.detectChanges();

    expect(fixture.componentInstance.value).toBeNull();
  });
});
