import { Organization } from '@bpf-builder/shared/core';
import { AppHarness } from '../../testing/app-harness';

const organization: Organization = {
  id: 'org-1',
  name: 'Horizon Skills Training',
  declarationNumber: '11756543210',
  siret: '73282932000074',
  nafCode: '85.59A',
  legalForm: 'SAS',
  addressLine: '12 rue des Lilas',
  postalCode: '75011',
  city: 'Paris',
  publishAddress: true,
  phone: '01 23 45 67 89',
  contactEmail: 'contact@example.com',
  managerName: null,
  managerTitle: null,
  signatoryName: null,
  signatoryTitle: null,
  signaturePlace: null,
  signatoryEmail: null,
  signatoryPhone: null,
  updatedAtUtc: '2026-11-06T09:00:00Z',
};

const signed: Organization = {
  ...organization,
  managerName: 'Camille Durand',
  managerTitle: 'Director',
  signatoryName: 'Camille Durand',
  signatoryTitle: 'Director',
  signaturePlace: 'Paris',
  signatoryEmail: 'camille.durand@example.com',
  signatoryPhone: '06 12 34 56 78',
};

describe('Organization page', () => {
  let app: AppHarness;

  beforeEach(() => (app = new AppHarness()));

  afterEach(() => app.http.verify());

  function value(id: string): string {
    return app.element.querySelector<HTMLInputElement>(`#${id}`)!.value;
  }

  function publishAddress(): HTMLInputElement {
    return app.element.querySelector<HTMLInputElement>('#publishAddress')!;
  }

  async function openPage(): Promise<void> {
    app.signIn();
    await app.open('/organization');
    expect(app.text('[role="status"]')).toBe('Loading your organization…');
  }

  it('is only for signed-in users', async () => {
    await app.open('/organization');

    expect(app.router.url).toBe('/login?returnUrl=%2Forganization');
  });

  it('starts with empty sections before the identification is saved', async () => {
    await openPage();
    app.http.expectOne('/api/organization').flush(null, { status: 404, statusText: 'Not Found' });
    await app.settle();

    expect(value('name')).toBe('');
    expect(publishAddress().checked).toBe(false);
    expect(app.texts('#identification p.text-xs')).toEqual([
      '11 digits, given when the activity was declared.',
      '14 digits.',
      'For example 85.59A.',
    ]);
    expect(app.text('[data-testid="signatory-hint"]')).toBe(
      'Save the identification first, then fill in this section.',
    );
    expect(app.element.querySelector('#signatory')).toBeNull();
  });

  it('requires a name, then creates the organization with blank fields sent as null', async () => {
    await openPage();
    app.http.expectOne('/api/organization').flush(null, { status: 404, statusText: 'Not Found' });
    await app.settle();

    await app.submit('#identification');
    expect(app.text('#identification bpf-field-errors')).toBe(
      'Enter the name of the organization.',
    );

    await app.type('name', 'Horizon Skills Training');
    await app.type('siret', '732 829 320 00074');
    await app.click('#publishAddress');
    await app.submit('#identification');

    const request = app.http.expectOne('/api/organization/identification');
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({
      name: 'Horizon Skills Training',
      declarationNumber: null,
      siret: '732 829 320 00074',
      nafCode: null,
      legalForm: null,
      addressLine: null,
      postalCode: null,
      city: null,
      publishAddress: true,
      phone: null,
      contactEmail: null,
    });
    request.flush(organization);
    await app.settle();

    expect(app.text('#identification [role="status"]')).toBe('Identification saved.');
    expect(value('city')).toBe('Paris');
    expect(value('managerName')).toBe('');
    expect(app.element.querySelector('[data-testid="signatory-hint"]')).toBeNull();
  });

  it('shows the server messages of each field', async () => {
    await openPage();
    app.http.expectOne('/api/organization').flush(organization);
    await app.settle();

    await app.submit('#identification');
    app.http
      .expectOne('/api/organization/identification')
      .flush(
        { errors: { Siret: ['Enter a valid 14-digit SIRET.'] } },
        { status: 400, statusText: 'Bad Request' },
      );
    await app.settle();

    expect(app.texts('#identification bpf-field-errors p')).toEqual([
      'Enter a valid 14-digit SIRET.',
    ]);
    expect(app.element.querySelector('#identification [role="status"]')).toBeNull();
  });

  it('shows a saved organization and saves the manager and signatory', async () => {
    await openPage();
    app.http.expectOne('/api/organization').flush(organization);
    await app.settle();

    expect(value('name')).toBe('Horizon Skills Training');
    expect(value('nafCode')).toBe('85.59A');
    expect(publishAddress().checked).toBe(true);
    expect(app.element.querySelector<HTMLInputElement>('#contactEmail')!.type).toBe('email');

    await app.type('managerName', 'Camille Durand');
    await app.type('signatoryEmail', 'camille.durand@example.com');
    await app.submit('#signatory');

    const request = app.http.expectOne('/api/organization/signatory');
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({
      managerName: 'Camille Durand',
      managerTitle: null,
      signatoryName: null,
      signatoryTitle: null,
      signaturePlace: null,
      signatoryEmail: 'camille.durand@example.com',
      signatoryPhone: null,
    });
    request.flush(signed);
    await app.settle();

    expect(app.text('#signatory [role="status"]')).toBe('Manager and signatory saved.');
    expect(value('signaturePlace')).toBe('Paris');
  });

  it('explains when the signatory cannot be saved', async () => {
    await openPage();
    app.http.expectOne('/api/organization').flush(signed);
    await app.settle();

    await app.submit('#signatory');
    app.http
      .expectOne('/api/organization/signatory')
      .flush(
        { title: "Save the organization's identification first." },
        { status: 404, statusText: 'Not Found' },
      );
    await app.settle();

    expect(app.text('#signatory [role="alert"]')).toBe(
      "Save the organization's identification first.",
    );

    await app.submit('#identification');
    app.http
      .expectOne('/api/organization/identification')
      .flush(null, { status: 500, statusText: 'Error' });
    await app.settle();

    expect(app.text('#identification [role="alert"]')).toBe(
      'Something went wrong. Please try again.',
    );
  });

  it('explains when the organization cannot be loaded', async () => {
    await openPage();
    app.http.expectOne('/api/organization').flush(null, { status: 500, statusText: 'Error' });
    await app.settle();

    expect(app.text('[role="alert"]')).toBe(
      'Your organization could not be loaded. Please try again later.',
    );
    expect(app.element.querySelector('form')).toBeNull();
  });
});
