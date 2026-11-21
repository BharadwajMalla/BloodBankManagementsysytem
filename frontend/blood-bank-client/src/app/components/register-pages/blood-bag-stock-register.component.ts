import { Component } from '@angular/core';
import { RegistersComponent } from '../registers/registers.component';

@Component({
  selector: 'app-blood-bag-stock-register',
  standalone: true,
  imports: [RegistersComponent],
  template: `<app-registers [category]="'bag-stock'"></app-registers>`
})
export class BloodBagStockRegisterComponent {}
