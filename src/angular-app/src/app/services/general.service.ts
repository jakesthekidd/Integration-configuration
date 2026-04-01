import { Injectable } from '@angular/core';
import Swal, { SweetAlertIcon } from 'sweetalert2';

@Injectable({
  providedIn: 'root',
})
export class GeneralService {
  confirm(options: {
    title: string;
    text?: string;
    confirmText?: string;
    cancelText?: string;
    icon?: SweetAlertIcon;
    confirmColor?: string;
  }) {
    return Swal.fire({
      title: options.title,
      text: options.text ?? '',
      icon: options.icon ?? 'question',
      showCancelButton: true,

      confirmButtonText: options.confirmText ?? 'Yes',
      cancelButtonText: options.cancelText ?? 'Cancel',

      confirmButtonColor: options.confirmColor ?? '#3085d6',
      cancelButtonColor: '#6c757d',

      reverseButtons: false,
    });
  }

  simpleConfirm(message: string) {
    return Swal.fire({
      title: message,
      icon: 'question',
      showCancelButton: true,
      confirmButtonText: 'Yes',
      cancelButtonText: 'Cancel',
    });
  }

  success(message: string) {
    return Swal.fire({
      icon: 'success',
      title: 'Success',
      text: message,
      timer: 2000,
      showConfirmButton: false,
    });
  }

  error(message: string) {
    return Swal.fire({
      icon: 'error',
      title: 'Error',
      text: message,
    });
  }
}
