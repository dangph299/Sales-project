import { Component } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { DropdownComponent } from './dropdown.component';
import { SelectOption } from '../../models/select-option.model';

@Component({
  standalone: true,
  imports: [FormsModule, DropdownComponent],
  template: `
    <app-dropdown
      label="Status"
      name="status"
      [(ngModel)]="value"
      [options]="options"
      [clearable]="true"
      [disabled]="disabled">
    </app-dropdown>
  `
})
class HostComponent {
  value = 'Draft';
  disabled = false;
  options: SelectOption<string>[] = [
    { value: 'Draft', label: 'Draft' },
    { value: 'Published', label: 'Published' }
  ];
}

describe('DropdownComponent', () => {
  let fixture: ComponentFixture<HostComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HostComponent, NoopAnimationsModule]
    }).compileComponents();

    fixture = TestBed.createComponent(HostComponent);
    fixture.detectChanges();
  });

  it('renders label and stores selected value through ControlValueAccessor', () => {
    const text = fixture.nativeElement.textContent as string;
    const dropdown = fixture.debugElement.children[0].componentInstance as DropdownComponent<string>;

    expect(text).toContain('Status');
    expect(dropdown.value).toBe('Draft');
  });

  it('writes selected values back to the host model', () => {
    const dropdown = fixture.debugElement.children[0].componentInstance as DropdownComponent<string>;

    dropdown.changeValue('Published');
    fixture.detectChanges();

    expect(fixture.componentInstance.value).toBe('Published');
  });

  it('does not change value while disabled', () => {
    fixture.componentInstance.disabled = true;
    fixture.detectChanges();
    const dropdown = fixture.debugElement.children[0].componentInstance as DropdownComponent<string>;

    dropdown.setDisabledState(true);
    dropdown.changeValue('Published');

    expect(fixture.componentInstance.value).toBe('Draft');
  });
});
