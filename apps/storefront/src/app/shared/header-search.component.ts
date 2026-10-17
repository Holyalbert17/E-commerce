import { CurrencyPipe } from '@angular/common';
import {
  Component,
  ElementRef,
  EventEmitter,
  HostListener,
  Input,
  OnChanges,
  OnDestroy,
  OnInit,
  Output,
  SimpleChanges,
  inject,
  signal,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink, NavigationEnd } from '@angular/router';
import { Subject, Subscription, debounceTime, distinctUntilChanged, filter } from 'rxjs';
import { FastSearchService } from '../core/fast-search.service';
import { ApiService, ProductListItem } from '../core/api.service';
import { getTenantSlug } from '../core/tenant.interceptor';
import { DxIconComponent } from './dx-icon.component';

@Component({
  selector: 'app-header-search',
  standalone: true,
  imports: [FormsModule, RouterLink, CurrencyPipe, DxIconComponent],
  template: `
    <form class="search" role="search" (ngSubmit)="submit()" [class.search--catalog]="variant === 'catalog'">
      <label class="sr-only" for="q-{{ variant }}">Buscar productos</label>
      <div class="search-wrap">
        <div class="search-field">
          <input
            id="q-{{ variant }}"
            class="dx-input"
            type="search"
            name="q"
            [(ngModel)]="q"
            (ngModelChange)="onQuery($event)"
            (focus)="onFocus()"
            [placeholder]="placeholder"
            autocomplete="off"
            aria-autocomplete="list"
            [attr.aria-expanded]="open()"
            [attr.aria-controls]="'search-suggest-' + variant"
          />
          @if (q.trim()) {
            <button type="button" class="clear" (click)="clear()" aria-label="Limpiar búsqueda">
              <dx-icon name="close" [size]="16" />
            </button>
          }
          <span class="sep" aria-hidden="true"></span>
          <button type="submit" class="glass" aria-label="Buscar">
            <dx-icon name="search" [size]="18" />
          </button>
        </div>

        @if (open() && q.trim().length >= 2) {
          <div class="suggest" [id]="'search-suggest-' + variant" role="listbox">
            @if (loading()) {
              <p class="suggest-msg">Buscando…</p>
            } @else if (results().length === 0) {
              <p class="suggest-msg">Sin coincidencias rápidas para “{{ q.trim() }}”</p>
            } @else {
              @for (p of results(); track p.id) {
                <a
                  class="hit"
                  role="option"
                  [routerLink]="['/producto', p.slugSeo || p.id]"
                  (click)="close()"
                >
                  <span
                    class="thumb"
                    [style.backgroundImage]="p.imagenPrincipalUrl ? 'url(' + media(p.imagenPrincipalUrl) + ')' : 'none'"
                  ></span>
                  <span class="body">
                    <span class="title" [innerHTML]="highlight(p.nombre)"></span>
                    @if (p.nombreCorto && p.nombreCorto !== p.nombre) {
                      <span class="spec">{{ p.nombreCorto }}</span>
                    } @else if (p.sku) {
                      <span class="spec">{{ p.sku }}</span>
                    }
                    @if (p.precioDesde != null) {
                      <span class="prices">
                        <strong class="now">{{ p.precioDesde | currency: 'PYG':'symbol-narrow':'1.0-0' }}</strong>
                      </span>
                    }
                  </span>
                </a>
              }
            }
            @if (!loading()) {
              <button type="button" class="see-all" (click)="submit()">
                @if (results().length) {
                  Ver todos los resultados
                  @if (source()) {
                    <small class="src"> · {{ source() }}</small>
                  }
                } @else {
                  Buscar “{{ q.trim() }}” en el catálogo
                }
              </button>
            }
          </div>
        } @else if (open() && history().length) {
          <div class="suggest" [id]="'search-suggest-' + variant" role="listbox">
            <div class="hist-head">
              <span>Búsquedas recientes</span>
              <button type="button" class="hist-clear" (click)="clearHistory($event)">Borrar</button>
            </div>
            @for (h of history(); track h) {
              <button type="button" class="hist-hit" role="option" (click)="pickHistory(h)">
                <dx-icon name="search" [size]="14" />
                <span>{{ h }}</span>
              </button>
            }
          </div>
        }
      </div>
    </form>
  `,
  styles: [
    `
      :host {
        display: block;
        min-width: 0;
        width: 100%;
      }
      .search {
        display: block;
        width: 100%;
        min-width: 0;
      }
      .search-wrap {
        position: relative;
        width: 100%;
      }
      .search-field {
        display: flex;
        align-items: center;
        background: #fff;
        border: 1px solid #d8d8d8;
        border-radius: 999px;
        min-height: 48px;
        padding: 0 0.35rem 0 1rem;
        box-shadow: 0 1px 2px rgba(0, 0, 0, 0.04);
      }
      .search--catalog .search-field {
        min-height: 44px;
        border-radius: 12px;
        border-color: var(--dx-line, #e8e8e8);
      }
      .search-field:focus-within {
        border-color: #bbb;
        box-shadow: 0 0 0 3px rgba(0, 0, 0, 0.06);
      }
      .search-field .dx-input {
        flex: 1;
        min-width: 0;
        border: 0 !important;
        box-shadow: none !important;
        background: transparent !important;
        padding: 0.65rem 0.5rem 0.65rem 0 !important;
        min-height: 44px;
        border-radius: 0 !important;
        font-size: 0.98rem;
      }
      .clear,
      .glass {
        flex-shrink: 0;
        width: 40px;
        height: 40px;
        border: 0;
        background: transparent;
        color: var(--dx-mute, #8a8a8a);
        border-radius: 999px;
        cursor: pointer;
        display: grid;
        place-items: center;
      }
      .clear:hover,
      .glass:hover {
        color: var(--dx-red, #c62828);
        background: #f5f5f5;
      }
      .sep {
        width: 1px;
        height: 22px;
        background: #e5e5e5;
        flex-shrink: 0;
      }
      .suggest {
        position: absolute;
        left: 0;
        right: 0;
        top: calc(100% + 0.4rem);
        z-index: 80;
        background: #fff;
        border: 1px solid var(--dx-line, #e8e8e8);
        border-radius: 14px;
        box-shadow: 0 16px 40px rgba(15, 23, 42, 0.14);
        max-height: min(70vh, 420px);
        overflow: auto;
        display: grid;
      }
      .suggest-msg {
        margin: 0;
        padding: 1rem;
        color: var(--dx-mute, #8a8a8a);
        font-size: 0.92rem;
      }
      .hit {
        display: grid;
        grid-template-columns: 52px 1fr;
        gap: 0.75rem;
        align-items: center;
        padding: 0.65rem 0.85rem;
        text-decoration: none;
        color: var(--dx-ink, #141414);
        border-bottom: 1px solid #f3f3f3;
      }
      .hit:hover,
      .hit:focus-visible {
        background: #f3f4f6;
        outline: none;
        color: var(--dx-ink, #141414);
      }
      .thumb {
        width: 52px;
        height: 52px;
        border-radius: 10px;
        background: #f3f3f3 center / cover no-repeat;
        border: 1px solid #eee;
      }
      .body {
        display: grid;
        gap: 0.15rem;
        min-width: 0;
      }
      .title {
        color: var(--dx-ink, #141414);
        font-weight: 650;
        font-size: 0.95rem;
        line-height: 1.25;
      }
      .hit:hover .title,
      .hit:focus-visible .title {
        color: var(--dx-ink, #141414);
      }
      .title u {
        color: inherit;
        text-decoration: underline;
        text-decoration-color: var(--dx-red, #c62828);
        text-underline-offset: 2px;
      }
      .spec {
        color: var(--dx-mute, #8a8a8a);
        font-size: 0.8rem;
        overflow: hidden;
        text-overflow: ellipsis;
        white-space: nowrap;
      }
      .hit:hover .spec,
      .hit:focus-visible .spec {
        color: var(--dx-mute, #8a8a8a);
      }
      .prices .now {
        color: var(--dx-price, #c62828);
        font-size: 0.92rem;
      }
      .hit:hover .prices .now,
      .hit:focus-visible .prices .now {
        color: var(--dx-price, #c62828);
      }
      .see-all {
        display: block;
        width: 100%;
        border: 0;
        background: #fafafa;
        padding: 0.85rem 1rem;
        font: inherit;
        font-weight: 700;
        color: var(--dx-red, #c62828);
        cursor: pointer;
        text-align: center;
      }
      .hist-head {
        display: flex;
        justify-content: space-between;
        align-items: center;
        padding: 0.65rem 0.85rem 0.35rem;
        font-size: 0.75rem;
        font-weight: 700;
        color: var(--dx-mute, #8a8a8a);
        text-transform: uppercase;
        letter-spacing: 0.03em;
      }
      .hist-clear {
        border: 0;
        background: none;
        color: var(--dx-red, #c62828);
        font: inherit;
        font-size: 0.75rem;
        font-weight: 650;
        cursor: pointer;
        text-transform: none;
        letter-spacing: 0;
      }
      .hist-hit {
        display: flex;
        align-items: center;
        gap: 0.55rem;
        width: 100%;
        border: 0;
        border-bottom: 1px solid #f3f3f3;
        background: #fff;
        padding: 0.65rem 0.85rem;
        text-align: left;
        font: inherit;
        color: inherit;
        cursor: pointer;
      }
      .hist-hit:hover {
        background: #fafafa;
      }
      .hist-hit dx-icon {
        color: var(--dx-mute, #8a8a8a);
      }
      .src {
        font-weight: 500;
        color: #999;
        text-transform: lowercase;
      }
      .sr-only {
        position: absolute;
        width: 1px;
        height: 1px;
        padding: 0;
        margin: -1px;
        overflow: hidden;
        clip: rect(0, 0, 0, 0);
        border: 0;
      }
    `,
  ],
})
export class HeaderSearchComponent implements OnInit, OnDestroy, OnChanges {
  private readonly api = inject(ApiService);
  private readonly fast = inject(FastSearchService);
  private readonly router = inject(Router);
  private readonly host = inject(ElementRef<HTMLElement>);
  private readonly query$ = new Subject<string>();
  private sub!: Subscription;
  private navSub!: Subscription;
  private seq = 0;

  /** `header` = barra global; `catalog` = listado (mismo motor, submit local). */
  @Input() variant: 'header' | 'catalog' = 'header';
  @Input() placeholder = '¿Qué estás buscando?';
  /** Texto sincronizado desde la URL / padre. */
  @Input() query = '';
  /** Si false, no navega a /buscar: emite `committed` (catálogo). */
  @Input() navigateOnSubmit = true;
  @Output() readonly committed = new EventEmitter<string>();

  q = '';
  readonly open = signal(false);
  readonly loading = signal(false);
  readonly results = signal<ProductListItem[]>([]);
  readonly source = signal<string | null>(null);
  readonly history = signal<string[]>([]);

  private readonly historyKey = 'ecom_search_history';

  constructor() {
    this.sub = this.query$.pipe(debounceTime(120), distinctUntilChanged()).subscribe((raw) => void this.runSuggest(raw));
    this.navSub = this.router.events
      .pipe(filter((e): e is NavigationEnd => e instanceof NavigationEnd))
      .subscribe((e) => {
        this.close();
        if (this.variant === 'header' && e.urlAfterRedirects.startsWith('/buscar')) {
          const tree = this.router.parseUrl(e.urlAfterRedirects);
          this.q = (tree.queryParams['q'] as string) || '';
        }
      });
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['query'] && this.query !== this.q) {
      this.q = this.query ?? '';
    }
  }

  ngOnInit(): void {
    if (this.query) this.q = this.query;
    this.history.set(this.readHistory());
    this.fast.setTenant(getTenantSlug());
    void this.fast.prefetch('B2C');
  }

  ngOnDestroy(): void {
    this.sub.unsubscribe();
    this.navSub.unsubscribe();
  }

  @HostListener('document:click', ['$event'])
  onDocClick(ev: MouseEvent): void {
    if (!this.host.nativeElement.contains(ev.target as Node)) this.close();
  }

  onFocus(): void {
    void this.fast.prefetch('B2C');
    this.history.set(this.readHistory());
    if (this.q.trim().length >= 2) this.open.set(true);
    else if (this.history().length) this.open.set(true);
  }

  onQuery(value: string): void {
    const t = value.trim();
    if (t.length >= 2) {
      this.open.set(true);
      this.query$.next(value);
    } else {
      this.results.set([]);
      this.open.set(this.history().length > 0);
      this.query$.next(value);
    }
  }

  clear(): void {
    this.q = '';
    this.results.set([]);
    this.source.set(null);
    this.open.set(this.history().length > 0);
    this.query$.next('');
    if (!this.navigateOnSubmit) this.committed.emit('');
  }

  close(): void {
    this.open.set(false);
  }

  pickHistory(term: string): void {
    this.q = term;
    this.submit();
  }

  clearHistory(ev: Event): void {
    ev.preventDefault();
    ev.stopPropagation();
    try {
      localStorage.removeItem(this.historyKey);
    } catch {
      /* ignore */
    }
    this.history.set([]);
    if (this.q.trim().length < 2) this.open.set(false);
  }

  submit(): void {
    const q = this.q.trim();
    if (q) this.pushHistory(q);
    this.close();
    this.committed.emit(q);
    if (!this.navigateOnSubmit) {
      queueMicrotask(() =>
        document.getElementById('productos')?.scrollIntoView({ behavior: 'smooth', block: 'start' }),
      );
      return;
    }
    void this.router.navigate(['/buscar'], { queryParams: q ? { q } : {} }).then(() => {
      document.getElementById('productos')?.scrollIntoView({ behavior: 'smooth', block: 'start' });
    });
  }

  private readHistory(): string[] {
    try {
      const raw = JSON.parse(localStorage.getItem(this.historyKey) || '[]');
      return Array.isArray(raw)
        ? raw.map((x) => String(x).trim()).filter(Boolean).slice(0, 8)
        : [];
    } catch {
      return [];
    }
  }

  private pushHistory(term: string): void {
    const t = term.trim();
    if (t.length < 2) return;
    const next = [t, ...this.readHistory().filter((x) => x.toLowerCase() !== t.toLowerCase())].slice(0, 8);
    try {
      localStorage.setItem(this.historyKey, JSON.stringify(next));
    } catch {
      /* ignore */
    }
    this.history.set(next);
  }

  media(url: string | null | undefined): string {
    return this.api.resolveMediaUrl(url) || '';
  }

  highlight(nombre: string): string {
    const q = this.q.trim();
    const safe = this.escape(nombre);
    if (!q || q.length < 2) return safe;
    const escQ = this.escape(q).replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
    try {
      return safe.replace(new RegExp(`(${escQ})`, 'ig'), '<u>$1</u>');
    } catch {
      return safe;
    }
  }

  private async runSuggest(raw: string): Promise<void> {
    const q = raw.trim();
    if (q.length < 2) {
      this.results.set([]);
      this.loading.set(false);
      this.source.set(null);
      return;
    }
    const my = ++this.seq;
    this.loading.set(true);
    const res = await this.fast.suggest(q, 6, 'B2C');
    if (my !== this.seq) return;
    this.loading.set(false);
    this.results.set(res.items);
    this.source.set(res.source);
  }

  private escape(s: string): string {
    return s.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
  }
}
