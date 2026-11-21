import { Component } from '@angular/core';
import { RegistersComponent } from '../registers/registers.component';

@Component({
  selector: 'app-donors-register',
  standalone: true,
  imports: [RegistersComponent],
  template: `<app-registers [category]="'donors'"></app-registers>`
})
export class DonorsRegisterComponent {}
