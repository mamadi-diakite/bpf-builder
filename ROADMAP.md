# Roadmap

This is the working plan for bpf-builder. Stories are grouped by epic and tagged with the milestone they belong to.

## Scope

In scope:
- Excel template and import with validation of a fiscal year's activity.
- Computing every BPF figure, with consistency checks.
- Showing the BPF in the official form layout, to copy each value into the State's online portal.

Out of scope:
- Not a filing tool. The BPF is filed on the State's online portal. The app prepares and checks the figures; the organization stays responsible for its declaration.
- Not legal or accounting advice.
- No personal data about learners or trainers. The Excel template has no name columns: learners and trainers are codes such as `Student 01` and `Trainer 01`. The app only handles the organization's aggregated activity figures.
- No real data in the repository. Samples are fictional.

## Glossary

| Term | Meaning |
|------|---------|
| BPF | Bilan Pédagogique et Financier, the yearly activity report of a French training organization |
| Form version | The layout and lines of the official form. First supported version: 10443*17 |
| Form section | A block of the form, from A (identification) to H (manager) |
| Form line | One box of the form, e.g. C-2a or F-1 total |
| Organization | The training organization using the app |
| Fiscal year | The accounting period the BPF covers |
| Session | One run of a training course |
| Delivery mode | Direct, subcontracted out (given to another organization), or subcontracted in (received from another organization) |
| Training goal | RNCP certification, RS certification, unregistered CQP, other training, skills assessment, VAE support |
| NSF code | Three-digit code of the French training specialties list |
| Learner code | Anonymous code of a learner, e.g. `Student 01` |
| Trainer code | Anonymous code of a trainer, e.g. `Trainer 01` |

## Milestones

| Milestone | Goal |
|-----------|------|
| M0 Foundation | Runnable app: accounts, organization profile, fiscal years, Excel template, import with validation report |
| M1 Calculation | Every BPF figure computed from imported data, with consistency checks |
| M2 Form view | The BPF shown in the official form layout, copy values one by one, track what was reported |
| M3 Review | Drill-down from a figure to its data, dashboard, export for the accountant, print view |
| M4 Quality | CI, tests, accessibility, docs |

---

## EPIC 1 — Project setup

| ID | Story | Milestone |
|----|-------|-----------|
| SETUP-1 | Repository with README, LICENSE (MIT), .gitignore, .editorconfig | M0 |
| SETUP-2 | .NET solution: Api, Domain, Application, Infrastructure and Tests projects | M0 |
| SETUP-3 | SQLite + EF Core, first migration, database created on startup in dev | M0 |
| SETUP-4 | Angular workspace with the main app and shared libraries | M0 |
| SETUP-5 | GitHub Actions: build and test both API and front end on each push | M4 |

## EPIC 2 — Accounts

| ID | Story | Milestone |
|----|-------|-----------|
| AUTH-1 | As a visitor, I create an account with name, email and password | M0 |
| AUTH-2 | As a user, I log in and get a token | M0 |
| AUTH-3 | As a user, I log out, and I am sent back to login when my session expires | M0 |
| AUTH-4 | As a user, I change my password with current password, new password and confirmation | M0 |
| AUTH-5 | A demo account is seeded with a documented demo password and a fictional organization | M0 |

## EPIC 3 — Organization and fiscal years

| ID | Story | Milestone |
|----|-------|-----------|
| ORG-1 | As a user, I fill my organization's identification (section A): declaration number, SIRET, NAF code, legal form, name, address, address can be published (yes/no), phone, contact email | M0 |
| ORG-2 | As a user, I fill the manager and signatory details (section H): manager name and title, signatory name and title, place, email, phone | M0 |
| ORG-3 | As a user, I create a fiscal year with start and end dates (calendar year by default) and the organization's total turnover excluding VAT | M0 |
| ORG-4 | As a user, I see my fiscal years and the import status of each one | M0 |

Acceptance notes:
- Format checks: declaration number (11 digits), SIRET (14 digits with checksum), NAF code.
- Demo values are fictional.

## EPIC 4 — Excel template

| ID | Story | Milestone |
|----|-------|-----------|
| XLS-1 | As a user, I download the Excel template, with drop-down lists for every coded column | M0 |
| XLS-2 | The template has an instructions sheet explaining each column with an example | M0 |
| XLS-3 | Reference lists (form lines, categories, goals, RNCP levels, NSF codes) are included in the template | M0 |
| XLS-4 | A filled sample file with fictional data ships with the repo | M0 |
| XLS-5 | The template carries a version number; older versions are accepted or clearly rejected | M3 |

Template sheets and columns:

| Sheet | Columns |
|-------|---------|
| Sessions | Session code, Title, Start date, End date, Planned hours per learner, Delivery mode, Distance learning (none / partial / full), Training goal, RNCP level (only for RNCP goal), NSF code |
| Learners | Learner code, Session code, Category (private-sector employee / apprentice / job seeker / individual at own expense / other), Hours attended |
| Trainers | Trainer code, Session code, Type (internal / external subcontractor), Hours delivered |
| Revenue | Form line (1, 2a to 2h, 3 to 11), Amount excl. VAT, Date, Session code (optional), Label |
| Costs | Category (trainer salaries / bought training services and fees / other training costs), Amount excl. VAT, Label |

## EPIC 5 — Import

| ID | Story | Milestone |
|----|-------|-----------|
| IMP-1 | As a user, I upload a filled template for a fiscal year | M0 |
| IMP-2 | I get a validation report with sheet, row and column for each problem: missing value, unknown code, unknown session, date outside the fiscal year, RNCP level missing or not expected, learner hours above planned hours | M0 |
| IMP-3 | Nothing is saved while there are blocking errors. Warnings do not block | M0 |
| IMP-4 | Importing again replaces the year's data, after confirmation | M1 |
| IMP-5 | As a user, I see the import history of a fiscal year | M3 |

## EPIC 6 — BPF calculation

| ID | Story | Milestone |
|----|-------|-----------|
| BPF-1 | Section B: distance learning is "yes" when at least one session is partly or fully remote | M1 |
| BPF-2 | Section C: revenue per line (1, 2a to 2h, 3 to 11), line 2 subtotal, grand total, and share of total turnover in % | M1 |
| BPF-3 | Section D: total training costs, of which trainer salaries, of which bought training services and fees | M1 |
| BPF-4 | Section E: number of internal and external trainers (distinct codes) and their hours | M1 |
| BPF-5 | Section F-1: learners and hours by category, from direct and subcontracted-out sessions | M1 |
| BPF-6 | Section F-2: learners and hours of subcontracted-out sessions | M1 |
| BPF-7 | Section F-3: learners and hours by training goal, with the RNCP split by level | M1 |
| BPF-8 | Section F-4: five main specialties by NSF code, other specialties grouped, total | M1 |
| BPF-9 | Section G: learners and hours of subcontracted-in sessions, kept out of section F | M1 |
| BPF-10 | Consistency checks: F-1 total = F-3 total = F-4 total; F-2 ≤ F-1; revenue on line 10 only with subcontracted-in sessions; learner hours ≤ planned hours | M1 |

Acceptance notes:
- Each rule is a small, tested function, tied to a form version.

Rules to confirm with the official notice:
- A learner in several sessions: counted once, or once per session.
- A session spanning two fiscal years: how its hours are split.
- Edge cases of revenue allocation (personal training account, apprenticeship).

## EPIC 7 — Form view

| ID | Story | Milestone |
|----|-------|-----------|
| FORM-1 | As a user, I see the computed BPF for a fiscal year in the layout of the official form: sections A to H, same titles, line numbers and boxes | M2 |
| FORM-2 | As a user, I copy any value with one click | M2 |
| FORM-3 | As a user, I tick each box as "reported" while I type it into the State's portal; my progress is saved | M2 |
| FORM-4 | Failed consistency checks are shown next to the boxes concerned | M2 |
| FORM-5 | Layout, labels and line numbers are defined per form version | M2 |
| FORM-6 | Every page states that this is a preparation aid and that the BPF is filed on the State's portal | M2 |
| FORM-7 | Print-friendly view of the form | M3 |

Acceptance notes:
- Form labels stay in French, as on the official form, so users find the same boxes on the portal. The rest of the UI is in English.

## EPIC 8 — Review and export

| ID | Story | Milestone |
|----|-------|-----------|
| REV-1 | As a user, I click a figure and see the sessions, learners or revenue lines behind it | M3 |
| REV-2 | As a user, I see a yearly dashboard: sessions, learners, hours, revenue by funder | M3 |
| REV-3 | As a user, I export the computed figures to Excel for my accountant | M3 |

## EPIC 9 — Quality and docs

| ID | Story | Milestone |
|----|-------|-----------|
| QA-1 | Reference tests: the sample file always gives the same expected BPF figures | M4 |
| QA-2 | API integration tests for import and calculation | M4 |
| QA-3 | Unit tests for key Angular components and services | M4 |
| QA-4 | End-to-end test: download the template, import the sample, check the form view totals (Playwright) | M4 |
| QA-5 | Docs: what the BPF is, how to fill the template, limits of the tool | M4 |

---

## Later ideas

- Download the filled form as a PDF
- Enter or fix data directly in the app, without Excel
- CSV import
- Compare two years side by side
- Several organizations per account
- French UI
