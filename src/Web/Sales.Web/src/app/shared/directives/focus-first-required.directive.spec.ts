import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { ComponentFixture, TestBed, fakeAsync, flushMicrotasks, tick } from '@angular/core/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { AutocompleteComponent } from '../components/autocomplete/autocomplete.component';
import { FocusFirstRequiredDirective } from './focus-first-required.directive';

function flushAutoFocus(): void {
  flushMicrotasks();
  tick(50);
}

@Component({
  standalone: true,
  imports: [FocusFirstRequiredDirective],
  template: `
    <div [appFocusFirstRequired]="enabled" [focusFirstRequiredTrigger]="trigger">
      <input name="optionalName">
      <input name="hiddenName" required hidden>
      <input name="disabledName" required disabled>
      <input name="readonlyName" required readonly>
      <input name="targetName" required>
      <textarea name="laterNotes" required></textarea>
    </div>
  `
})
class FocusHostComponent {
  enabled = true;
  trigger = 1;
}

@Component({
  standalone: true,
  imports: [FocusFirstRequiredDirective],
  template: `
    <div [appFocusFirstRequired]="enabled" [focusFirstRequiredTrigger]="trigger">
      <textarea name="notes" required></textarea>
    </div>
  `
})
class TextareaHostComponent {
  enabled = true;
  trigger = 1;
}

@Component({
  standalone: true,
  imports: [CommonModule, FocusFirstRequiredDirective],
  template: `
    <div [appFocusFirstRequired]="true" [focusFirstRequiredTrigger]="trigger">
      <input *ngIf="showInput" name="asyncName" required>
    </div>
  `
})
class AsyncHostComponent {
  trigger = 1;
  showInput = false;
}

@Component({
  standalone: true,
  imports: [AutocompleteComponent, FocusFirstRequiredDirective],
  template: `
    <div [appFocusFirstRequired]="true" [focusFirstRequiredTrigger]="trigger">
      <app-autocomplete
        name="customerPhone"
        presentation="control"
        [required]="true"
        [options]="[]"
        [displayWith]="display">
      </app-autocomplete>
    </div>
  `
})
class AutocompleteHostComponent {
  trigger = 1;
  display = (value: string | null): string => value ?? '';
}

describe('FocusFirstRequiredDirective', () => {
  afterEach(() => {
    (document.activeElement as HTMLElement | null)?.blur();
  });

  it('focuses the first visible enabled editable required text input', fakeAsync(() => {
    const fixture = createFixture(FocusHostComponent);

    flushAutoFocus();

    expect((document.activeElement as HTMLInputElement).name).toBe('targetName');
  }));

  it('focuses a required textarea when it is the first eligible control', fakeAsync(() => {
    const fixture = createFixture(TextareaHostComponent);

    flushAutoFocus();

    expect((document.activeElement as HTMLTextAreaElement).name).toBe('notes');
    fixture.destroy();
  }));

  it('does not focus when disabled for edit mode', fakeAsync(() => {
    const fixture = createFixture(FocusHostComponent);
    fixture.componentInstance.enabled = false;
    fixture.detectChanges();

    flushAutoFocus();

    expect((document.activeElement as HTMLInputElement).name).not.toBe('targetName');
  }));

  it('does not steal focus when the user already focused inside the form', fakeAsync(() => {
    const fixture = createFixture(FocusHostComponent);
    const laterInput = fixture.nativeElement.querySelector('[name="laterNotes"]') as HTMLTextAreaElement;

    laterInput.focus();
    flushAutoFocus();

    expect(document.activeElement).toBe(laterInput);
  }));

  it('waits for create form content rendered after the trigger changes', fakeAsync(() => {
    const fixture = createFixture(AsyncHostComponent);

    fixture.componentInstance.showInput = true;
    fixture.detectChanges();
    flushAutoFocus();

    expect((document.activeElement as HTMLInputElement).name).toBe('asyncName');
  }));

  it('focuses the real input inside the shared autocomplete', fakeAsync(() => {
    const fixture = createFixture(AutocompleteHostComponent);

    flushAutoFocus();

    expect((document.activeElement as HTMLInputElement).name).toBe('customerPhone');
  }));
});

function createFixture<T>(component: new () => T): ComponentFixture<T> {
  TestBed.configureTestingModule({
    imports: [component, NoopAnimationsModule]
  });

  const fixture = TestBed.createComponent(component);
  fixture.detectChanges();
  return fixture;
}
