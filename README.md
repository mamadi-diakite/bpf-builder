# bpf-builder

A web app to help French training organizations prepare their yearly BPF ("Bilan Pédagogique et Financier", form cerfa 10443).

The user fills an Excel template with the year's activity (sessions, learners, trainers, revenue, costs) and imports it. The app checks the data, computes every BPF figure for the fiscal year and shows it in the layout of the official form, so each value can be copied into the State's online portal.

## Who it is for

Small training organizations that track their activity in Excel and want to avoid calculation and allocation errors when preparing their BPF.

## What it is not

- Not a filing tool. The BPF is filed on the State's online portal; the app only prepares and checks the figures.
- Not legal or accounting advice.
- No personal data: learners and trainers are anonymous codes (`Student 01`, `Trainer 01`). The app only handles aggregated activity figures.

## Stack

- Front end: Angular workspace (app + libraries)
- Back end: ASP.NET Core Web API (C#)
- Database: SQLite with EF Core

## Status

Early development (milestone M0). What works today:

- Create an account, log in, change your password. Sessions expire after 60 minutes.
- Fill in the organization's details: identification (section A of the BPF) and manager and signatory (section H).
- Create fiscal years (the last calendar year by default) with the organization's total turnover, and see which ones have imported activity.
- Download the Excel activity template, or a sample filled with fictional data, and import a filled template into a fiscal year. The validation report lists each problem with its sheet, row and column. Nothing is saved while the file has errors; warnings do not block.

Computing the BPF figures and showing them in the layout of the form are planned (see [ROADMAP.md](ROADMAP.md)).

## Getting started

### Prerequisites

- [.NET SDK 10.0.403](https://dotnet.microsoft.com/download) or a later 10.0 feature band
- [Node.js 24](https://nodejs.org/) with npm 11

### Run the API

```bash
cd src/backend
dotnet run --project src/BpfBuilder.Api --launch-profile http
```

The API listens on http://localhost:5083. In Development it creates the SQLite database in `src/backend/src/BpfBuilder.Api/App_Data/`, applies the migrations, and adds the demo data below when the database has no users.

The access tokens are signed with a key that is never stored in the repository. In Development, a random key is generated at startup, so you need to log in again after each restart. In any other environment, set a base64 key of at least 32 bytes:

```bash
export Jwt__SigningKey="$(openssl rand -base64 32)"
```

### Run the web app

```bash
cd src/frontend
npm install
npm start
```

Open http://localhost:4200. Calls to `/api` are sent to the API on port 5083.

### Demo account

The demo account is fictional and only exists in Development.

| Name | Email | Password |
|------|-------|----------|
| Camille Durand | camille.durand@example.com | `TestDemo123!` |

It owns a fictional training organization, Horizon Skills Training, with sections A and H filled in, and the last two calendar years as fiscal years, without imported activity.

To try the import, open the fiscal years page, choose the 2025 fiscal year, download the sample and import it as it is: the sample describes activity in 2025. Importing it into another year shows the validation report with its errors.

To start again from an empty demo, stop the API and delete `src/backend/src/BpfBuilder.Api/App_Data/`.

### The activity template

The workbook has an Instructions sheet, one sheet each for sessions, learners, trainers, revenue and costs, and a Reference lists sheet with the allowed codes. Learners and trainers are anonymous codes, never names.

### Run the tests

Back end (with coverage, migrations and generated code excluded):

```bash
cd src/backend
dotnet test --settings coverage.runsettings --collect "XPlat Code Coverage"
```

Front end (Vitest, the run fails below 100% coverage):

```bash
cd src/frontend
npm run test:ci
```

## Project layout

```
src/backend/    ASP.NET Core solution: Domain, Application.Contracts, Application, Infrastructure, Api, and their test projects
src/frontend/   Angular workspace: projects/apps/bpf-builder-app and the shared libraries in projects/libs/shared
```

## License

[MIT](LICENSE)
