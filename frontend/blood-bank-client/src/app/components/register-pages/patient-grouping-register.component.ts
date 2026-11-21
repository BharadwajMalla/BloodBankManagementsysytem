import { Component } from '@angular/core';
import { RegistersComponent } from '../registers/registers.component';

@Component({
  selector: 'app-patient-grouping-register',
  standalone: true,
  imports: [RegistersComponent],
  template: `<app-registers [category]="'grouping'"></app-registers>`
})
export class PatientGroupingRegisterComponent {}
