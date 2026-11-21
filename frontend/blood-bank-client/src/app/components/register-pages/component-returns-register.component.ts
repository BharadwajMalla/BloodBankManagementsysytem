import { Component } from '@angular/core';
import { RegistersComponent } from '../registers/registers.component';

@Component({
  selector: 'app-component-returns-register',
  standalone: true,
  imports: [RegistersComponent],
  template: `<app-registers [category]="'returns'"></app-registers>`
})
export class ComponentReturnsRegisterComponent {}
