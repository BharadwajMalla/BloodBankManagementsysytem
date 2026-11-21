import { Component } from '@angular/core';
import { RegistersComponent } from '../registers/registers.component';

@Component({
  selector: 'app-component-collection-register',
  standalone: true,
  imports: [RegistersComponent],
  template: `<app-registers [category]="'collections'"></app-registers>`
})
export class ComponentCollectionRegisterComponent {}
