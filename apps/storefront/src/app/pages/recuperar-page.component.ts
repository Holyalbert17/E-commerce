import { Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthModalService } from '../core/auth-modal.service';

/** Deep-link: abre el modal de reset de contraseña sobre el catálogo. */
@Component({
  standalone: true,
  template: `<p class="hint">Abriendo recuperación de contraseña…</p>`,
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
export class RecuperarPageComponent implements OnInit {
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly auth = inject(AuthModalService);

  ngOnInit(): void {
    const q = this.route.snapshot.queryParamMap;
    const c = q.get('c') || '';
    const k = q.get('k') || '';
    const tenant = q.get('tenant') || undefined;
    void this.router.navigateByUrl('/catalogo').then(() => this.auth.showReset(c, k, tenant));
  }
}
