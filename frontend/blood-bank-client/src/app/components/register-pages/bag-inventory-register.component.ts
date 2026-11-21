import { Component } from '@angular/core';
import { RegistersComponent } from '../registers/registers.component';

@Component({
  selector: 'app-bag-inventory-register',
  standalone: true,
  imports: [RegistersComponent],
  template: `<app-registers [category]="'bags'"></app-registers>`
})
export class BagInventoryRegisterComponent {}
