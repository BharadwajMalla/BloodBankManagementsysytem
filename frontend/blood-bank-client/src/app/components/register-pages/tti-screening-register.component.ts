import { Component } from '@angular/core';
import { RegistersComponent } from '../registers/registers.component';

@Component({
  selector: 'app-tti-screening-register',
  standalone: true,
  imports: [RegistersComponent],
  template: `<app-registers [category]="'tti'"></app-registers>`
})
export class TtiScreeningRegisterComponent {}
