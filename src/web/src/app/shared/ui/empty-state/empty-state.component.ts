import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'fym-empty-state',
  imports: [MatIconModule],
  template: `
    <div class="fym-empty-state">
      <mat-icon>{{ icon() }}</mat-icon>
      <p class="title">{{ title() }}</p>
      @if (description()) {
        <p class="description">{{ description() }}</p>
      }
      <ng-content />
    </div>
  `,
  styles: [
    `
      .fym-empty-state {
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        gap: 8px;
        padding: 48px 24px;
        text-align: center;
        color: var(--fym-text-muted);
      }
      mat-icon {
        font-size: 40px;
        width: 40px;
        height: 40px;
        color: var(--fym-primary-300);
      }
      .title {
        margin: 0;
        font-weight: 600;
        color: var(--fym-text);
      }
      .description {
        margin: 0;
        font-size: 0.875rem;
        max-width: 360px;
      }
    `,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EmptyStateComponent {
  icon = input('inbox');
  title = input.required<string>();
  description = input<string>();
}
