import {
  AfterContentInit,
  Component,
  ElementRef,
  OnDestroy,
  ViewChild,
  signal,
} from '@angular/core';

/** Horizontal card rail with prev/next controls (scroll-snap). */
@Component({
  selector: 'app-product-rail-carousel',
  standalone: true,
  template: `
    <div class="wrap" [class.wrap--plain]="!showNav()">
      @if (showNav()) {
        <button
          type="button"
          class="nav nav--prev"
          (click)="scrollBy(-1)"
          [disabled]="!canPrev()"
          aria-label="Anterior"
        >
          ‹
        </button>
      }
      <div class="scroller" #scroller (scroll)="sync()" role="list">
        <ng-content />
      </div>
      @if (showNav()) {
        <button
          type="button"
          class="nav nav--next"
          (click)="scrollBy(1)"
          [disabled]="!canNext()"
          aria-label="Siguiente"
        >
          ›
        </button>
      }
    </div>
  `,
  styles: [
    `
      :host {
        display: block;
      }
      .wrap {
        position: relative;
        display: grid;
        grid-template-columns: auto 1fr auto;
        align-items: stretch;
        gap: 0.35rem;
      }
      .wrap--plain {
        grid-template-columns: 1fr;
      }
      .scroller {
        display: grid;
        grid-auto-flow: column;
        grid-auto-columns: minmax(158px, 176px);
        gap: 0.65rem;
        overflow-x: auto;
        padding: 0.15rem 0.1rem 0.55rem;
        scroll-snap-type: x mandatory;
        scroll-behavior: smooth;
        -webkit-overflow-scrolling: touch;
        scrollbar-width: thin;
      }
      .scroller::-webkit-scrollbar {
        height: 6px;
      }
      .scroller::-webkit-scrollbar-thumb {
        background: #c5c9d0;
        border-radius: 999px;
      }
      .nav {
        align-self: center;
        width: 2.25rem;
        height: 2.25rem;
        border-radius: 999px;
        border: 1px solid var(--dx-line, #e2e5ea);
        background: #fff;
        color: var(--dx-ink, #1a1a1a);
        font-size: 1.35rem;
        line-height: 1;
        cursor: pointer;
        box-shadow: 0 1px 4px rgba(0, 0, 0, 0.06);
        z-index: 1;
      }
      .nav:hover:not(:disabled) {
        border-color: var(--dx-accent, #c62828);
        color: var(--dx-accent, #c62828);
      }
      .nav:disabled {
        opacity: 0.35;
        cursor: default;
      }
      @media (max-width: 640px) {
        .wrap {
          grid-template-columns: 1fr;
        }
        .nav {
          display: none;
        }
        .scroller {
          grid-auto-columns: minmax(142px, 158px);
        }
      }
    `,
  ],
})
export class ProductRailCarouselComponent implements AfterContentInit, OnDestroy {
  @ViewChild('scroller') scroller?: ElementRef<HTMLDivElement>;

  readonly canPrev = signal(false);
  readonly canNext = signal(false);
  readonly showNav = signal(false);

  private ro?: ResizeObserver;
  private mo?: MutationObserver;

  ngAfterContentInit(): void {
    queueMicrotask(() => this.bind());
  }

  ngOnDestroy(): void {
    this.ro?.disconnect();
    this.mo?.disconnect();
  }

  scrollBy(dir: -1 | 1): void {
    const el = this.scroller?.nativeElement;
    if (!el) return;
    const step = Math.max(el.clientWidth * 0.85, 200);
    el.scrollBy({ left: dir * step, behavior: 'smooth' });
  }

  sync(): void {
    const el = this.scroller?.nativeElement;
    if (!el) return;
    const max = el.scrollWidth - el.clientWidth;
    this.showNav.set(max > 8);
    this.canPrev.set(el.scrollLeft > 4);
    this.canNext.set(el.scrollLeft < max - 4);
  }

  private bind(): void {
    const el = this.scroller?.nativeElement;
    if (!el) return;
    this.sync();
    this.ro = new ResizeObserver(() => this.sync());
    this.ro.observe(el);
    this.mo = new MutationObserver(() => this.sync());
    this.mo.observe(el, { childList: true, subtree: true });
  }
}
