import { Component, computed, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import type { GifteePersonalInfoItem } from '../../../app.models';
import { CommonModalTemplate } from '../../../shared/components/modal/common-modal-template/common-modal-template';
import { ButtonText, ModalTitle } from '../../../app.enum';

@Component({
  selector: 'app-delete-participant-modal',
  standalone: true,
  imports: [CommonModule, CommonModalTemplate],
  templateUrl: './delete-participant-modal.html',
  styleUrls: ['./delete-participant-modal.scss'],
})
export class DeleteParticipantModal {
  // вход: массив персональной информации (signal)
  readonly personalInfo = input.required<GifteePersonalInfoItem[]>();

  // выходы: события кнопок
  readonly deleteButtonAction = output<void>();
  readonly cancelButtonAction = output<void>();

  public readonly title = ModalTitle.RemoveParticipant;
  public readonly buttonDeleteText = ButtonText.Delete;
  public readonly buttonCancelText = ButtonText.Cancel;

  public readonly fullName = computed(() => {
    const firstName =
      this.personalInfo().find((i) => i.term === 'First name')?.value || '';
    const lastName =
      this.personalInfo().find((i) => i.term === 'Last name')?.value || '';
    return `${firstName} ${lastName}`.trim();
  });

  public readonly subtitle = computed(
    () =>
      `Are you sure you want to remove ${this.fullName() || 'this participant'}? All of their data will be permanently deleted. This action cannot be undone.`
  );

  public onDeleteButtonClick(): void {
    this.deleteButtonAction.emit();
  }

  public onCancelButtonClick(): void {
    this.cancelButtonAction.emit();
  }
}
