import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'fym-page-header',
  template: `
    <header class="fym-page-header">
      <div>
        <h1>{{ title() }}</h1>
        @if (subtitle()) {
          <p class="subtitle">{{ subtitle() }}</p>
        }
      </div>
      <div class="actions">
        <ng-content select="[actions]" />
      </div>
    </header>
  `,
  styles: [
    `
      .fym-page-header {
        display: flex;
        align-items: flex-start;
        justify-content: space-between;
        gap: 16px;
        flex-wrap: wrap;
      }
      h1 {
        margin: 0;
        font-size: 1.5rem;
        font-weight: 600;
        color: var(--fym-text);
      }
      .subtitle {
        margin: 4px 0 0;
        color: var(--fym-text-muted);
        font-size: 0.9rem;
      }
      .actions {
        display: flex;
        gap: 8px;
        align-items: center;
      }
    `,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PageHeaderComponent {
  title = input.required<string>();
  subtitle = input<string>();
}
