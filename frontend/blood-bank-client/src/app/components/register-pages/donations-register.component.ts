import { Component } from '@angular/core';
import { RegistersComponent } from '../registers/registers.component';

@Component({
  selector: 'app-donations-register',
  standalone: true,
  imports: [RegistersComponent],
  template: `<app-registers [category]="'donations'"></app-registers>`
})
export class DonationsRegisterComponent {}
