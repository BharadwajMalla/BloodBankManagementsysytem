import { Component } from '@angular/core';
import { RegistersComponent } from '../registers/registers.component';

@Component({
  selector: 'app-component-discard-register',
  standalone: true,
  imports: [RegistersComponent],
  template: `<app-registers [category]="'discards'"></app-registers>`
})
export class ComponentDiscardRegisterComponent {}
