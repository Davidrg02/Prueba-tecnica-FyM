import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'fym-status-chip',
  template: `<span class="fym-status-chip" [class.active]="active()">{{ active() ? 'Activo' : 'Inactivo' }}</span>`,
  styles: [
    `
      .fym-status-chip {
        display: inline-flex;
        align-items: center;
        padding: 2px 10px;
        border-radius: 999px;
        font-size: 0.75rem;
        font-weight: 600;
        background: #fee2e2;
        color: #991b1b;
      }
      .fym-status-chip.active {
        background: #d1fae5;
        color: #065f46;
      }
    `,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StatusChipComponent {
  active = input.required<boolean>();
}
