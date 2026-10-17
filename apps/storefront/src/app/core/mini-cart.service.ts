import { Injectable, signal } from '@angular/core';
import { Cart, CartLine } from './api.service';

@Injectable({ providedIn: 'root' })
export class MiniCartService {
  readonly open = signal(false);
  readonly cart = signal<Cart | null>(null);
  readonly lastAddedId = signal<number | null>(null);

  show(cart: Cart, lastAddedArticuloId?: number | null): void {
    this.cart.set(cart);
    this.lastAddedId.set(lastAddedArticuloId ?? null);
    this.open.set(true);
  }

  close(): void {
    this.open.set(false);
  }

  highlightLine(lines: CartLine[]): CartLine | null {
    const id = this.lastAddedId();
    if (id == null) return lines[0] ?? null;
    return lines.find((l) => l.idArticulo === id) ?? lines[0] ?? null;
  }
}
