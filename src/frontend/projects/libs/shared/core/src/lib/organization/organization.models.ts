/** Section A of the BPF: who the training organization is. */
export interface OrganizationIdentification {
  readonly name: string;
  readonly declarationNumber: string | null;
  readonly siret: string | null;
  readonly nafCode: string | null;
  readonly legalForm: string | null;
  readonly addressLine: string | null;
  readonly postalCode: string | null;
  readonly city: string | null;
  readonly publishAddress: boolean;
  readonly phone: string | null;
  readonly contactEmail: string | null;
}

/** Section H of the BPF: the manager and the person who signs. */
export interface OrganizationSignatory {
  readonly managerName: string | null;
  readonly managerTitle: string | null;
  readonly signatoryName: string | null;
  readonly signatoryTitle: string | null;
  readonly signaturePlace: string | null;
  readonly signatoryEmail: string | null;
  readonly signatoryPhone: string | null;
}

export interface Organization extends OrganizationIdentification, OrganizationSignatory {
  readonly id: string;
  readonly updatedAtUtc: string;
}

/** Form values with blank texts sent as `null`, so optional fields are cleared on the server. */
export function blankToNull<T extends object>(
  values: T,
): { [K in keyof T]: T[K] extends string ? string | null : T[K] } {
  return Object.fromEntries(
    Object.entries(values).map(([key, value]) => [
      key,
      typeof value === 'string' && value.trim() === '' ? null : value,
    ]),
  ) as { [K in keyof T]: T[K] extends string ? string | null : T[K] };
}

/** Values for a form whose controls are named after the given fields, with `null` shown as empty. */
export function toFormValues<T extends object, K extends keyof T>(
  source: T,
  fields: readonly K[],
): Record<K, NonNullable<T[K]> | ''> {
  return Object.fromEntries(fields.map((field) => [field, source[field] ?? ''])) as Record<
    K,
    NonNullable<T[K]> | ''
  >;
}
