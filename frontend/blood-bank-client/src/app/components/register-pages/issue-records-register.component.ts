import { Component } from '@angular/core';
import { RegistersComponent } from '../registers/registers.component';

@Component({
  selector: 'app-issue-records-register',
  standalone: true,
  imports: [RegistersComponent],
  template: `<app-registers [category]="'issues'"></app-registers>`
})
export class IssueRecordsRegisterComponent {}
