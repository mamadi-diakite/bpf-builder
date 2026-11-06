import { Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { Router, RouterLink, RouterOutlet } from '@angular/router';
import { ApiHealthService, AuthService, SessionStore } from '@bpf-builder/shared/core';
import { Shell } from '@bpf-builder/shared/ui';

@Component({
  selector: 'bpf-root',
  imports: [RouterOutlet, RouterLink, Shell],
  template: `
    <bpf-shell appName="BPF Builder" [apiStatus]="apiStatus()">
      <ng-container shellNav>
        @if (user(); as user) {
          <a routerLink="/organization" class="text-emerald-600">Organization</a>
          <a routerLink="/fiscal-years" class="text-emerald-600">Fiscal years</a>
          <span class="text-slate-600" data-testid="user-name">{{ user.displayName }}</span>
          <a routerLink="/account/password" class="text-emerald-600">Change password</a>
          <button type="button" class="text-emerald-600" (click)="logOut()">Log out</button>
        } @else {
          <a routerLink="/login" class="text-emerald-600">Log in</a>
          <a routerLink="/register" class="text-emerald-600">Create an account</a>
        }
      </ng-container>
      <router-outlet />
    </bpf-shell>
  `,
})
export class App {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly user = inject(SessionStore).user;
  protected readonly apiStatus = toSignal(inject(ApiHealthService).check(), {
    initialValue: 'checking',
  });

  protected logOut(): void {
    this.auth.logout();
    void this.router.navigateByUrl('/login');
  }
}
