import { DOCUMENT } from '@angular/common';
import { AfterViewInit, Directive, ElementRef, Input, NgZone, OnChanges, OnDestroy, SimpleChanges, inject } from '@angular/core';

const textLikeSelector = [
  'input:not([type])',
  'input[type="text"]',
  'input[type="search"]',
  'input[type="tel"]',
  'input[type="email"]',
  'input[type="url"]',
  'input[type="password"]',
  'input[type="number"]',
  'textarea',
  '[data-focus-required="true"] input',
  '[data-focus-required="true"] textarea'
].join(',');

@Directive({
  selector: '[appFocusFirstRequired]',
  standalone: true
})
export class FocusFirstRequiredDirective implements AfterViewInit, OnChanges, OnDestroy {
  @Input('appFocusFirstRequired') enabled: boolean | string | null = true;
  @Input() focusFirstRequiredTrigger: unknown;

  private readonly host = inject(ElementRef) as ElementRef<HTMLElement>;
  private readonly document = inject(DOCUMENT);
  private readonly zone = inject(NgZone);

  private animationFrame = 0;
  private hasFocusedForCurrentTrigger = false;
  private viewInitialized = false;

  ngAfterViewInit(): void {
    this.viewInitialized = true;
    this.scheduleFocus();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['enabled'] || changes['focusFirstRequiredTrigger']) {
      this.hasFocusedForCurrentTrigger = false;
      this.scheduleFocus();
    }
  }

  ngOnDestroy(): void {
    this.cancelScheduledFocus();
  }

  private scheduleFocus(attempt = 0): void {
    if (!this.viewInitialized || !this.shouldFocus()) {
      return;
    }

    this.cancelScheduledFocus();
    this.zone.runOutsideAngular(() => {
      queueMicrotask(() => {
        this.animationFrame = requestAnimationFrame(() => {
          this.animationFrame = requestAnimationFrame(() => this.focusFirstCandidate(attempt));
        });
      });
    });
  }

  private focusFirstCandidate(attempt: number): void {
    if (!this.shouldFocus() || this.userAlreadyFocusedInside()) {
      return;
    }

    const candidate = this.findFirstCandidate();
    if (!candidate) {
      if (attempt < 4) {
        this.scheduleFocus(attempt + 1);
      }

      return;
    }

    candidate.focus({ preventScroll: true });
    this.hasFocusedForCurrentTrigger = true;
    this.verifyFocusWasKept(candidate, attempt);
  }

  private shouldFocus(): boolean {
    return this.isEnabled()
      && !this.hasFocusedForCurrentTrigger
      && (this.focusFirstRequiredTrigger === undefined || this.focusFirstRequiredTrigger === '' || Boolean(this.focusFirstRequiredTrigger));
  }

  private verifyFocusWasKept(candidate: HTMLElement, attempt: number): void {
    if (attempt >= 4) {
      return;
    }

    this.animationFrame = requestAnimationFrame(() => {
      if (!this.isEnabled()
        || !(this.focusFirstRequiredTrigger === undefined || this.focusFirstRequiredTrigger === '' || Boolean(this.focusFirstRequiredTrigger))
        || this.document.activeElement === candidate
        || this.userAlreadyFocusedInside()) {
        return;
      }

      this.hasFocusedForCurrentTrigger = false;
      this.scheduleFocus(attempt + 1);
    });
  }

  private isEnabled(): boolean {
    return this.enabled !== false && this.enabled !== null && this.enabled !== 'false';
  }

  private findFirstCandidate(): HTMLElement | null {
    return Array.from(this.host.nativeElement.querySelectorAll<HTMLElement>(textLikeSelector))
      .find(element => this.isRequiredCandidate(element) && this.canFocus(element)) ?? null;
  }

  private isRequiredCandidate(element: HTMLElement): boolean {
    if (element.closest('[data-focus-required="true"]')) {
      return true;
    }

    if (element.hasAttribute('required') || element.getAttribute('aria-required') === 'true') {
      return true;
    }

    const formItem = element.closest('.ant-form-item, nz-form-item');
    return Boolean(formItem?.querySelector('.ant-form-item-required, [nzrequired], [nzRequired]'));
  }

  private canFocus(element: HTMLElement): boolean {
    const input = element as HTMLInputElement | HTMLTextAreaElement;
    const style = this.document.defaultView?.getComputedStyle(element);
    return input.type !== 'hidden'
      && !input.disabled
      && !input.readOnly
      && element.getAttribute('aria-disabled') !== 'true'
      && !element.closest('[hidden], [aria-hidden="true"], fieldset[disabled]')
      && element.tabIndex !== -1
      && element.getClientRects().length > 0
      && style?.display !== 'none'
      && style?.visibility !== 'hidden';
  }

  private userAlreadyFocusedInside(): boolean {
    const activeElement = this.document.activeElement as HTMLElement | null;
    if (!activeElement || activeElement === this.document.body) {
      return false;
    }

    return this.host.nativeElement.contains(activeElement) && this.isInteractive(activeElement);
  }

  private isInteractive(element: HTMLElement): boolean {
    return element.matches('input, textarea, select, button, [tabindex]:not([tabindex="-1"]), [contenteditable="true"]');
  }

  private cancelScheduledFocus(): void {
    if (this.animationFrame) {
      cancelAnimationFrame(this.animationFrame);
      this.animationFrame = 0;
    }
  }
}
