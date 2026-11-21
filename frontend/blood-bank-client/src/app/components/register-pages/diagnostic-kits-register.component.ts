import { Component } from '@angular/core';
import { RegistersComponent } from '../registers/registers.component';

@Component({
  selector: 'app-diagnostic-kits-register',
  standalone: true,
  imports: [RegistersComponent],
  template: `<app-registers [category]="'diagnostic-kits'"></app-registers>`
})
export class DiagnosticKitsRegisterComponent {}
