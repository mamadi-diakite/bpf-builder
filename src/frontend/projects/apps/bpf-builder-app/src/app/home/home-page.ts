import { Component } from '@angular/core';

@Component({
  selector: 'bpf-home-page',
  template: `
    <section class="max-w-2xl space-y-4">
      <h1 class="text-3xl font-bold tracking-tight">Your BPF, computed from your Excel activity</h1>
      <p class="text-lg text-slate-600">
        Import the year's sessions, learners, trainers, revenue and costs from an Excel file, check
        the data, and get every figure of the BPF form ready to copy.
      </p>
    </section>
  `,
})
export class HomePage {}
