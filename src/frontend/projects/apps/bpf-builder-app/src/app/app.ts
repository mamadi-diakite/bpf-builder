import { Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { RouterOutlet } from '@angular/router';
import { ApiHealthService } from '@bpf-builder/shared/core';
import { Shell } from '@bpf-builder/shared/ui';

@Component({
  selector: 'bpf-root',
  imports: [RouterOutlet, Shell],
  template: `
    <bpf-shell appName="BPF Builder" [apiStatus]="apiStatus()">
      <router-outlet />
    </bpf-shell>
  `,
})
export class App {
  protected readonly apiStatus = toSignal(inject(ApiHealthService).check(), {
    initialValue: 'checking',
  });
}
