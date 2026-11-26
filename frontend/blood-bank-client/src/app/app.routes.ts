import { Routes } from '@angular/router';
import { LoginComponent } from './components/login/login.component';
import { RegisterComponent } from './components/register/register.component';
import { DashboardComponent } from './components/dashboard/dashboard.component';
import { MasterDataComponent } from './components/master-data/master-data.component';
import { RegistersComponent } from './components/registers/registers.component';
import { TtiScreeningRegisterComponent } from './components/register-pages/tti-screening-register.component';
import { PatientGroupingRegisterComponent } from './components/register-pages/patient-grouping-register.component';
import { BloodBagStockRegisterComponent } from './components/register-pages/blood-bag-stock-register.component';
import { BagInventoryRegisterComponent } from './components/register-pages/bag-inventory-register.component';
import { DiagnosticKitsRegisterComponent } from './components/register-pages/diagnostic-kits-register.component';
import { ApheresisRegisterComponent } from './components/register-pages/apheresis-register.component';
import { MasterRecordRegisterComponent } from './components/register-pages/master-record-register.component';
import { RequisitionsRegisterComponent } from './components/register-pages/requisitions-register.component';
import { IssueRecordsRegisterComponent } from './components/register-pages/issue-records-register.component';
import { ComponentCollectionRegisterComponent } from './components/register-pages/component-collection-register.component';
import { ComponentDiscardRegisterComponent } from './components/register-pages/component-discard-register.component';
import { ComponentReturnsRegisterComponent } from './components/register-pages/component-returns-register.component';
import { DonorsRegisterComponent } from './components/register-pages/donors-register.component';
import { DonationsRegisterComponent } from './components/register-pages/donations-register.component';
import { authGuard } from './core/guards/auth.guard';
import { bloodBankStaffGuard } from './core/guards/blood-bank-staff.guard';
export const routes: Routes = [
  {
    path: '',
    redirectTo: 'dashboard',
    pathMatch: 'full'
  },
  {
    path: 'login',
    component: LoginComponent,
    title: 'Sign In | BloodBank SaaS'
  },
  {
    path: 'register',
    component: RegisterComponent,
    title: 'Register | BloodBank SaaS'
  },
  {
    path: 'dashboard',
    component: DashboardComponent,
    canActivate: [authGuard],
    title: 'Live Inventory Dashboard | BloodBank SaaS'
  },
  {
    path: 'master-data',
    component: MasterDataComponent,
    canActivate: [authGuard, bloodBankStaffGuard],
    title: 'Master Data | Uddanam Blood Centre'
  },
  {
    path: 'registers',
    component: RegistersComponent,
    canActivate: [authGuard, bloodBankStaffGuard],
    pathMatch: 'full',
    title: 'Paper Registers | Uddanam Blood Centre'
  },
  {
    path: 'registers/tti',
    component: TtiScreeningRegisterComponent,
    canActivate: [authGuard, bloodBankStaffGuard],
    title: 'TTI Screening Register | Uddanam Blood Centre'
  },
  {
    path: 'registers/grouping',
    component: PatientGroupingRegisterComponent,
    canActivate: [authGuard, bloodBankStaffGuard],
    title: 'Patient Grouping Register | Uddanam Blood Centre'
  },
  {
    path: 'registers/bag-stock',
    component: BloodBagStockRegisterComponent,
    canActivate: [authGuard, bloodBankStaffGuard],
    title: 'Blood-Bag Stock Register | Uddanam Blood Centre'
  },
  {
    path: 'registers/bags',
    component: BagInventoryRegisterComponent,
    canActivate: [authGuard, bloodBankStaffGuard],
    title: 'Bag Inventory | Uddanam Blood Centre'
  },
  {
    path: 'registers/diagnostic-kits',
    component: DiagnosticKitsRegisterComponent,
    canActivate: [authGuard, bloodBankStaffGuard],
    title: 'Diagnostic Kits Register | Uddanam Blood Centre'
  },
  {
    path: 'registers/apheresis',
    component: ApheresisRegisterComponent,
    canActivate: [authGuard, bloodBankStaffGuard],
    title: 'Apheresis Register | Uddanam Blood Centre'
  },
  {
    path: 'registers/master-record',
    component: MasterRecordRegisterComponent,
    canActivate: [authGuard, bloodBankStaffGuard],
    title: 'Master Record | Uddanam Blood Centre'
  },
  {
    path: 'registers/requisitions',
    component: RequisitionsRegisterComponent,
    canActivate: [authGuard, bloodBankStaffGuard],
    title: 'Requisitions Register | Uddanam Blood Centre'
  },
  {
    path: 'registers/issues',
    component: IssueRecordsRegisterComponent,
    canActivate: [authGuard, bloodBankStaffGuard],
    title: 'Issue Records | Uddanam Blood Centre'
  },
  {
    path: 'registers/collections',
    component: ComponentCollectionRegisterComponent,
    canActivate: [authGuard, bloodBankStaffGuard],
    title: 'Component Collection Register | Uddanam Blood Centre'
  },
  {
    path: 'registers/discards',
    component: ComponentDiscardRegisterComponent,
    canActivate: [authGuard, bloodBankStaffGuard],
    title: 'Component Discard Register | Uddanam Blood Centre'
  },
  {
    path: 'registers/returns',
    component: ComponentReturnsRegisterComponent,
    canActivate: [authGuard, bloodBankStaffGuard],
    title: 'Component Returns Register | Uddanam Blood Centre'
  },
  {
    path: 'registers/donors',
    component: DonorsRegisterComponent,
    canActivate: [authGuard, bloodBankStaffGuard],
    title: 'Donors Register | Uddanam Blood Centre'
  },
  {
    path: 'registers/donations',
    component: DonationsRegisterComponent,
    canActivate: [authGuard, bloodBankStaffGuard],
    title: 'Donations Register | Uddanam Blood Centre'
  },
  {
    path: '**',
    redirectTo: 'dashboard'
  }
];
