import { Component, OnInit, inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthModalService } from '../core/auth-modal.service';

/** Deep-link: abre el modal de login sobre el catálogo. */
@Component({
  standalone: true,
  template: `<p class="hint">Abriendo inicio de sesión…</p>`,
  styles: [
    `
      .hint {
        text-align: center;
        color: var(--dx-mute);
        padding: 2rem;
      }
    `,
  ],
})
export class LoginPageComponent implements OnInit {
  private readonly router = inject(Router);
  private readonly auth = inject(AuthModalService);

  ngOnInit(): void {
    void this.router.navigateByUrl('/catalogo').then(() => this.auth.showLogin());
  }
}
