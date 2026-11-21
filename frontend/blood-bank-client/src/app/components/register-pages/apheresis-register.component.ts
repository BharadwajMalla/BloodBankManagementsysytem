import { Component } from '@angular/core';
import { RegistersComponent } from '../registers/registers.component';

@Component({
  selector: 'app-apheresis-register',
  standalone: true,
  imports: [RegistersComponent],
  template: `<app-registers [category]="'apheresis'"></app-registers>`
})
export class ApheresisRegisterComponent {}
