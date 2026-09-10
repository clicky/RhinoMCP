import type { Attachment } from './events.js';

/** Kept in step with AttachmentPicker.MaxBytes on the host side. */
export const MAX_ATTACHMENT_BYTES = 10 * 1024 * 1024;

export function isAttachable(file: File): boolean {
  return file.size <= MAX_ATTACHMENT_BYTES;
}

export async function readAttachments(files: readonly File[]): Promise<Attachment[]> {
  const read = files.map(
    (file, index) =>
      new Promise<Attachment>((resolve, reject) => {
        const reader = new FileReader();
        reader.onerror = () => reject(reader.error);
        reader.onload = () =>
          resolve({
            id: `local-${Date.now()}-${index}`,
            kind: file.type.startsWith('image/') ? 'image' : 'text',
            name: file.name,
            mediaType: file.type || 'text/plain',
            bytes: file.size,
            dataUrl: String(reader.result),
          });
        reader.readAsDataURL(file);
      }),
  );
  return Promise.all(read);
}
