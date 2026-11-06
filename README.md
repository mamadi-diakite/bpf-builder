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

Early development. Setup instructions will be added as the code lands.

## License

[MIT](LICENSE)
