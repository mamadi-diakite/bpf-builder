import { Routes } from '@angular/router';
import { authGuard, guestGuard } from '@bpf-builder/shared/core';
import { ChangePasswordPage } from './auth/change-password-page';
import { LoginPage } from './auth/login-page';
import { RegisterPage } from './auth/register-page';
import { FiscalYearsPage } from './fiscal-years/fiscal-years-page';
import { HomePage } from './home/home-page';
import { ImportPage } from './imports/import-page';
import { OrganizationPage } from './organization/organization-page';

export const routes: Routes = [
  { path: '', component: HomePage, title: 'BPF Builder' },
  {
    path: 'login',
    component: LoginPage,
    canActivate: [guestGuard],
    title: 'Log in · BPF Builder',
  },
  {
    path: 'register',
    component: RegisterPage,
    canActivate: [guestGuard],
    title: 'Create an account · BPF Builder',
  },
  {
    path: 'account/password',
    component: ChangePasswordPage,
    canActivate: [authGuard],
    title: 'Change password · BPF Builder',
  },
  {
    path: 'organization',
    component: OrganizationPage,
    canActivate: [authGuard],
    title: 'Organization · BPF Builder',
  },
  {
    path: 'fiscal-years',
    component: FiscalYearsPage,
    canActivate: [authGuard],
    title: 'Fiscal years · BPF Builder',
  },
  {
    path: 'fiscal-years/:id/import',
    component: ImportPage,
    canActivate: [authGuard],
    title: 'Import activity · BPF Builder',
  },
  { path: '**', redirectTo: '' },
];
