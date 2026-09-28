import { HttpErrorResponse } from '@angular/common/http';
export function apiError(error: unknown): string {
  if (!(error instanceof HttpErrorResponse)) return 'Có lỗi xảy ra. Vui lòng thử lại.';
  if (error.status === 0) return 'Không kết nối được máy chủ.';
  if (error.status === 403) return 'Bạn không có quyền thực hiện thao tác này.';
  if (error.status === 429) return 'Bạn thử quá nhiều lần. Vui lòng đợi một phút.';
  if (error.status === 404) return 'Không tìm thấy dữ liệu.';
  const body = error.error;
  if (typeof body?.message === 'string') return body.message;
  if (body?.errors) return Object.values(body.errors).flat().join(' ');
  return 'Không thể xử lý yêu cầu. Vui lòng thử lại.';
}

