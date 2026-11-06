import { DownloadedFile } from '@bpf-builder/shared/core';

/** Saves a file fetched with the access token, which a plain link could not send. */
export function saveFile({ fileName, content }: DownloadedFile): void {
  const url = URL.createObjectURL(content);
  const link = document.createElement('a');
  link.href = url;
  link.download = fileName;
  link.click();
  URL.revokeObjectURL(url);
}
