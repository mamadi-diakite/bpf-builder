import { Component, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import {
  blankToNull,
  FormSubmission,
  Organization,
  OrganizationService,
  toFormValues,
  withLoadState,
} from '@bpf-builder/shared/core';
import { FieldErrors } from '@bpf-builder/shared/ui';

interface TextField {
  readonly id: string;
  readonly label: string;
  readonly maxlength: number;
  readonly type?: 'email' | 'tel';
  readonly hint?: string;
}

const IDENTIFICATION_FIELDS = [
  'name',
  'declarationNumber',
  'siret',
  'nafCode',
  'legalForm',
  'addressLine',
  'postalCode',
  'city',
  'phone',
  'contactEmail',
] as const;

const SIGNATORY_FIELDS = [
  'managerName',
  'managerTitle',
  'signatoryName',
  'signatoryTitle',
  'signaturePlace',
  'signatoryEmail',
  'signatoryPhone',
] as const;

/** Sections A and H of the BPF, saved separately. */
@Component({
  selector: 'bpf-organization-page',
  imports: [ReactiveFormsModule, FieldErrors],
  template: `
    <section class="space-y-8">
      <div class="space-y-2">
        <h1 class="text-2xl font-bold tracking-tight">Organization</h1>
        <p class="text-slate-600">These details are copied into sections A and H of the BPF.</p>
      </div>

      @switch (status()) {
        @case ('loading') {
          <p class="text-slate-600" role="status">Loading your organization…</p>
        }
        @case ('error') {
          <p class="alert-error" role="alert">
            Your organization could not be loaded. Please try again later.
          </p>
        }
        @default {
          <form
            id="identification"
            class="panel"
            [formGroup]="identification"
            (ngSubmit)="saveIdentification()"
            novalidate
          >
            <h2 class="text-lg font-semibold">Identification (section A)</h2>
            @if (identificationSaved()) {
              <p class="alert-success" role="status">Identification saved.</p>
            }
            @if (identificationSubmission.errors().form; as message) {
              <p class="alert-error" role="alert">{{ message }}</p>
            }
            <div class="grid gap-4 sm:grid-cols-2">
              @for (field of identificationFields; track field.id) {
                <div>
                  <label class="form-label" [for]="field.id">{{ field.label }}</label>
                  <input
                    class="form-input"
                    [id]="field.id"
                    [type]="field.type ?? 'text'"
                    [attr.maxlength]="field.maxlength"
                    [formControlName]="field.id"
                  />
                  @if (field.hint) {
                    <p class="mt-1 text-xs text-slate-500">{{ field.hint }}</p>
                  }
                  <bpf-field-errors [messages]="identificationSubmission.messagesFor(field.id)" />
                </div>
              }
            </div>
            <label class="flex items-center gap-2 text-sm text-slate-700">
              <input id="publishAddress" type="checkbox" formControlName="publishAddress" />
              The address can be published in the public list of training organizations
            </label>
            <button
              class="button-primary sm:w-auto"
              type="submit"
              [disabled]="identificationSubmission.pending()"
            >
              Save identification
            </button>
          </form>

          <section class="panel" aria-labelledby="signatory-title">
            <h2 id="signatory-title" class="text-lg font-semibold">
              Manager and signatory (section H)
            </h2>
            @if (organization()) {
              <form
                id="signatory"
                class="space-y-4"
                [formGroup]="signatory"
                (ngSubmit)="saveSignatory()"
                novalidate
              >
                @if (signatorySaved()) {
                  <p class="alert-success" role="status">Manager and signatory saved.</p>
                }
                @if (signatorySubmission.errors().form; as message) {
                  <p class="alert-error" role="alert">{{ message }}</p>
                }
                <div class="grid gap-4 sm:grid-cols-2">
                  @for (field of signatoryFields; track field.id) {
                    <div>
                      <label class="form-label" [for]="field.id">{{ field.label }}</label>
                      <input
                        class="form-input"
                        [id]="field.id"
                        [type]="field.type ?? 'text'"
                        [attr.maxlength]="field.maxlength"
                        [formControlName]="field.id"
                      />
                      <bpf-field-errors [messages]="signatorySubmission.messagesFor(field.id)" />
                    </div>
                  }
                </div>
                <button
                  class="button-primary sm:w-auto"
                  type="submit"
                  [disabled]="signatorySubmission.pending()"
                >
                  Save manager and signatory
                </button>
              </form>
            } @else {
              <p class="alert-info" data-testid="signatory-hint">
                Save the identification first, then fill in this section.
              </p>
            }
          </section>
        }
      }
    </section>
  `,
})
export class OrganizationPage {
  private readonly organizations = inject(OrganizationService);
  private readonly formBuilder = inject(NonNullableFormBuilder);

  protected readonly status = signal<'loading' | 'ready' | 'error'>('loading');
  protected readonly organization = signal<Organization | null>(null);
  protected readonly identificationSaved = signal(false);
  protected readonly signatorySaved = signal(false);

  protected readonly identificationFields: readonly TextField[] = [
    { id: 'name', label: 'Name', maxlength: 200 },
    {
      id: 'declarationNumber',
      label: 'Declaration number',
      maxlength: 20,
      hint: '11 digits, given when the activity was declared.',
    },
    { id: 'siret', label: 'SIRET', maxlength: 20, hint: '14 digits.' },
    { id: 'nafCode', label: 'NAF code', maxlength: 10, hint: 'For example 85.59A.' },
    { id: 'legalForm', label: 'Legal form', maxlength: 100 },
    { id: 'addressLine', label: 'Address', maxlength: 200 },
    { id: 'postalCode', label: 'Postal code', maxlength: 10 },
    { id: 'city', label: 'City', maxlength: 100 },
    { id: 'phone', label: 'Phone', maxlength: 30, type: 'tel' },
    { id: 'contactEmail', label: 'Contact email', maxlength: 254, type: 'email' },
  ];

  protected readonly signatoryFields: readonly TextField[] = [
    { id: 'managerName', label: 'Manager name', maxlength: 100 },
    { id: 'managerTitle', label: 'Manager title', maxlength: 100 },
    { id: 'signatoryName', label: 'Signatory name', maxlength: 100 },
    { id: 'signatoryTitle', label: 'Signatory title', maxlength: 100 },
    { id: 'signaturePlace', label: 'Place of signature', maxlength: 100 },
    { id: 'signatoryEmail', label: 'Signatory email', maxlength: 254, type: 'email' },
    { id: 'signatoryPhone', label: 'Signatory phone', maxlength: 30, type: 'tel' },
  ];

  protected readonly identification = this.formBuilder.group({
    name: ['', Validators.required],
    declarationNumber: [''],
    siret: [''],
    nafCode: [''],
    legalForm: [''],
    addressLine: [''],
    postalCode: [''],
    city: [''],
    publishAddress: [false],
    phone: [''],
    contactEmail: [''],
  });
  protected readonly identificationSubmission = new FormSubmission(this.identification, {
    name: 'Enter the name of the organization.',
  });

  protected readonly signatory = this.formBuilder.group({
    managerName: [''],
    managerTitle: [''],
    signatoryName: [''],
    signatoryTitle: [''],
    signaturePlace: [''],
    signatoryEmail: [''],
    signatoryPhone: [''],
  });
  protected readonly signatorySubmission = new FormSubmission(this.signatory, {});

  constructor() {
    withLoadState(this.organizations.get()).subscribe((state) => {
      if (state.status === 'ready') {
        this.show(state.value);
      }
      this.status.set(state.status === 'missing' ? 'ready' : state.status);
    });
  }

  protected saveIdentification(): void {
    const values = this.identification.getRawValue();
    this.identificationSaved.set(false);
    this.identificationSubmission.submit(
      () => this.organizations.saveIdentification({ ...blankToNull(values), name: values.name }),
      (organization) => {
        this.show(organization);
        this.identificationSaved.set(true);
      },
    );
  }

  protected saveSignatory(): void {
    this.signatorySaved.set(false);
    this.signatorySubmission.submit(
      () => this.organizations.saveSignatory(blankToNull(this.signatory.getRawValue())),
      (organization) => {
        this.show(organization);
        this.signatorySaved.set(true);
      },
    );
  }

  private show(organization: Organization): void {
    this.organization.set(organization);
    this.identification.setValue({
      ...toFormValues(organization, IDENTIFICATION_FIELDS),
      publishAddress: organization.publishAddress,
    });
    this.signatory.setValue(toFormValues(organization, SIGNATORY_FIELDS));
  }
}
