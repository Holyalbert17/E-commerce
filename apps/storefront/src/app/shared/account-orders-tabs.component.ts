import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { DxIconComponent } from './dx-icon.component';

@Component({
  selector: 'app-account-orders-tabs',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, DxIconComponent],
  template: `
    <nav class="acct-tabs" aria-label="Pedidos y devoluciones">
      <a routerLink="/mis-pedidos" routerLinkActive="on" [routerLinkActiveOptions]="{ exact: true }">
        <dx-icon name="package" [size]="16" /> Pedidos
      </a>
      <a routerLink="/devoluciones" routerLinkActive="on">
        <dx-icon name="truck" [size]="16" /> Devoluciones
      </a>
    </nav>
  `,
  styles: [
    `
      .acct-tabs {
        display: flex;
        flex-wrap: wrap;
        gap: 0.2rem;
        margin: 0 0 1.15rem;
        padding: 0.22rem;
        background: #fff;
        border: 1px solid var(--dx-line);
        border-radius: 12px;
        width: fit-content;
        max-width: 100%;
      }
      a {
        display: inline-flex;
        align-items: center;
        gap: 0.4rem;
        padding: 0.5rem 1.05rem;
        border-radius: 9px;
        text-decoration: none;
        color: var(--dx-ink) !important;
        font-weight: 700;
        font-size: 0.92rem;
      }
      a:hover {
        background: #f7f7f8;
      }
      a.on {
        background: var(--dx-red-soft);
        color: var(--dx-red) !important;
      }
    `,
  ],
})
export class AccountOrdersTabsComponent {}
