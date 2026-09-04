import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'fym-not-found',
  imports: [RouterLink, MatButtonModule, MatIconModule],
  template: `
    <div class="fym-error-page">
      <mat-icon>search_off</mat-icon>
      <h1>404 · Página no encontrada</h1>
      <p>La página que busca no existe o fue movida.</p>
      <a mat-flat-button color="primary" routerLink="/">Volver al panel</a>
    </div>
  `,
  styles: [
    `
      .fym-error-page {
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        gap: 12px;
        min-height: 70vh;
        text-align: center;
        padding: 24px;
      }
      mat-icon {
        font-size: 56px;
        width: 56px;
        height: 56px;
        color: var(--fym-primary-300);
      }
      h1 {
        margin: 0;
        font-size: 1.5rem;
      }
      p {
        color: var(--fym-text-muted);
        max-width: 360px;
      }
    `,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NotFoundComponent {}
