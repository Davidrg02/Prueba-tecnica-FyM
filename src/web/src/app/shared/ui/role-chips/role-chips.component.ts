import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RoleSummary } from '../../../core/models/user.models';

@Component({
  selector: 'fym-role-chips',
  template: `
    <div class="fym-role-chips">
      @for (role of roles(); track role.id) {
        <span class="chip" [class.system]="role.level >= 100">{{ role.name }}</span>
      }
    </div>
  `,
  styles: [
    `
      .fym-role-chips {
        display: flex;
        gap: 4px;
        flex-wrap: wrap;
      }
      .chip {
        padding: 2px 10px;
        border-radius: 999px;
        font-size: 0.75rem;
        font-weight: 600;
        background: var(--fym-primary-050);
        color: var(--fym-primary-700);
      }
      .chip.system {
        background: #fef3c7;
        color: #92400e;
      }
    `,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RoleChipsComponent {
  roles = input.required<RoleSummary[]>();
}
