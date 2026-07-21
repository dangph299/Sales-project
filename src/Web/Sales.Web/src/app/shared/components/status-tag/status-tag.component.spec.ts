import { ComponentFixture, TestBed } from '@angular/core/testing';
import { StatusTagComponent } from './status-tag.component';

describe('StatusTagComponent', () => {
  let fixture: ComponentFixture<StatusTagComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [StatusTagComponent] }).compileComponents();
    fixture = TestBed.createComponent(StatusTagComponent);
  });

  it('renders the supplied label', () => {
    fixture.componentInstance.display = { label: 'Pending inventory', tone: 'info' };
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Pending inventory');
  });

  it('maps each tone to its NG-ZORRO tag colour', () => {
    const expected: [StatusTagComponent['display']['tone'], string][] = [
      ['success', 'success'],
      ['warning', 'warning'],
      ['danger', 'error'],
      ['info', 'processing'],
      ['neutral', 'default']
    ];

    expected.forEach(([tone, color]) => {
      fixture.componentInstance.display = { label: tone, tone };
      expect(fixture.componentInstance.color).toBe(color);
    });
  });
});
