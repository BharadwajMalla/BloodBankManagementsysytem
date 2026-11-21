import { Component } from '@angular/core';
import { RegistersComponent } from '../registers/registers.component';

@Component({
  selector: 'app-requisitions-register',
  standalone: true,
  imports: [RegistersComponent],
  template: `<app-registers [category]="'requisitions'"></app-registers>`
})
export class RequisitionsRegisterComponent {}
