import { Component } from '@angular/core';
import { RegistersComponent } from '../registers/registers.component';

@Component({
  selector: 'app-master-record-register',
  standalone: true,
  imports: [RegistersComponent],
  template: `<app-registers [category]="'master-record'"></app-registers>`
})
export class MasterRecordRegisterComponent {}
