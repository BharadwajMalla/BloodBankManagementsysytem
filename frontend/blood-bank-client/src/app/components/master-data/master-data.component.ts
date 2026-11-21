import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import {
  MasterDataCommand,
  MasterDataEntry,
  MasterDataType
} from '../../core/models/blood-bank.model';
import { AuthService } from '../../core/services/auth.service';
import { MasterDataService } from '../../core/services/master-data.service';

interface MasterDataField {
  key: keyof MasterDataCommand;
  label: string;
  type?: 'text' | 'email' | 'tel' | 'number' | 'password' | 'select' | 'textarea';
  required?: boolean;
  min?: number;
  options?: string[];
}

const masterDataFields: Record<MasterDataType, MasterDataField[]> = {
  hospitals: [
    { key: 'name', label: 'Hospital name', required: true },
    { key: 'registrationNumber', label: 'Registration number', required: true },
    { key: 'contactPerson', label: 'Contact person' },
    { key: 'email', label: 'Login email', type: 'email', required: true },
    { key: 'phone', label: 'Phone', type: 'tel' },
    { key: 'address', label: 'Address', type: 'textarea' },
    { key: 'password', label: 'Initial login password', type: 'password', required: true }
  ],
  doctors: [
    { key: 'code', label: 'Doctor code', required: true },
    { key: 'name', label: 'Doctor name', required: true },
    { key: 'registrationNumber', label: 'Medical registration number' },
    { key: 'specialty', label: 'Specialty' },
    { key: 'hospitalName', label: 'Hospital' },
    { key: 'email', label: 'Email', type: 'email' },
    { key: 'phone', label: 'Phone', type: 'tel' }
  ],
  staff: [
    { key: 'code', label: 'Employee number', required: true },
    { key: 'name', label: 'Full name', required: true },
    { key: 'email', label: 'Login email', type: 'email', required: true },
    { key: 'phone', label: 'Phone', type: 'tel' },
    {
      key: 'role',
      label: 'Login role',
      type: 'select',
      required: true,
      options: ['Admin', 'BloodBank', 'TechnicalSupervisor', 'Technician']
    },
    { key: 'department', label: 'Department', required: true },
    { key: 'password', label: 'Initial login password', type: 'password', required: true }
  ],
  products: [
    { key: 'code', label: 'Product code', required: true },
    { key: 'name', label: 'Product name', required: true },
    { key: 'category', label: 'Category' },
    { key: 'shelfLifeDays', label: 'Shelf life (days)', type: 'number', required: true, min: 1 },
    { key: 'description', label: 'Description', type: 'textarea' }
  ],
  'bag-types': [
    { key: 'code', label: 'Bag type code', required: true },
    { key: 'name', label: 'Bag type name', required: true },
    { key: 'volumeMl', label: 'Volume (mL)', type: 'number', required: true, min: 1 },
    { key: 'anticoagulant', label: 'Anticoagulant' },
    { key: 'description', label: 'Description', type: 'textarea' }
  ],
  suppliers: [
    { key: 'code', label: 'Supplier code', required: true },
    { key: 'name', label: 'Supplier name', required: true },
    { key: 'contactPerson', label: 'Contact person' },
    { key: 'phone', label: 'Phone', type: 'tel' },
    { key: 'email', label: 'Email', type: 'email' },
    { key: 'address', label: 'Address', type: 'textarea' }
  ]
};

@Component({
  selector: 'app-master-data',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './master-data.component.html'
})
export class MasterDataComponent implements OnInit {
  public readonly authService = inject(AuthService);
  private readonly masterDataService = inject(MasterDataService);

  public readonly categories: { type: MasterDataType; label: string }[] = [
    { type: 'hospitals', label: 'Hospitals' },
    { type: 'doctors', label: 'Doctors' },
    { type: 'staff', label: 'Staff & roles' },
    { type: 'products', label: 'Products' },
    { type: 'bag-types', label: 'Bag types' },
    { type: 'suppliers', label: 'Suppliers' }
  ];

  public selectedType: MasterDataType = 'hospitals';
  public records: MasterDataEntry[] = [];
  public search = '';
  public isLoading = false;
  public isSaving = false;
  public showForm = false;
  public editingId: string | null = null;
  public errorMessage = '';
  public successMessage = '';
  public form: Record<string, string | number | boolean | undefined> = {};

  public get fields(): MasterDataField[] {
    return masterDataFields[this.selectedType].filter(field =>
      field.key !== 'password' || !this.editingId
    );
  }

  public get selectedCategoryLabel(): string {
    return this.categories.find(item => item.type === this.selectedType)?.label ?? 'record';
  }

  public ngOnInit(): void {
    this.loadRecords();
  }

  public selectType(type: MasterDataType): void {
    this.selectedType = type;
    this.search = '';
    this.showForm = false;
    this.editingId = null;
    this.loadRecords();
  }

  public loadRecords(): void {
    this.isLoading = true;
    this.errorMessage = '';
    this.masterDataService.get(this.selectedType, this.search).subscribe({
      next: records => {
        this.records = records;
        this.isLoading = false;
      },
      error: error => {
        this.errorMessage = this.getErrorMessage(error);
        this.isLoading = false;
      }
    });
  }

  public beginCreate(): void {
    this.editingId = null;
    this.form = { isActive: true };
    this.errorMessage = '';
    this.successMessage = '';
    this.showForm = true;
  }

  public beginEdit(record: MasterDataEntry): void {
    this.editingId = record.id;
    this.form = {
      code: record.code,
      name: record.name,
      registrationNumber: record.registrationNumber,
      contactPerson: record.contactPerson,
      phone: record.phone,
      email: record.email,
      address: record.address,
      specialty: record.specialty,
      hospitalName: record.hospitalName,
      category: record.category,
      department: record.department,
      role: record.role,
      shelfLifeDays: record.shelfLifeDays,
      volumeMl: record.volumeMl,
      anticoagulant: record.anticoagulant,
      description: record.description,
      isActive: record.isActive
    };
    this.errorMessage = '';
    this.successMessage = '';
    this.showForm = true;
  }

  public save(): void {
    if (this.isSaving)
      return;
    this.isSaving = true;
    this.errorMessage = '';
    const command = this.toCommand();
    const request = this.editingId
      ? this.masterDataService.update(this.selectedType, this.editingId, command)
      : this.masterDataService.create(this.selectedType, command);

    request.subscribe({
      next: () => {
        this.isSaving = false;
        this.showForm = false;
        this.successMessage = 'Master-data record saved.';
        this.loadRecords();
      },
      error: error => {
        this.isSaving = false;
        this.errorMessage = this.getErrorMessage(error);
      }
    });
  }

  public setActive(record: MasterDataEntry): void {
    this.errorMessage = '';
    this.masterDataService.setActive(this.selectedType, record.id, !record.isActive).subscribe({
      next: () => {
        this.successMessage = record.isActive ? 'Record deactivated.' : 'Record activated.';
        this.loadRecords();
      },
      error: error => this.errorMessage = this.getErrorMessage(error)
    });
  }

  public displayDetails(record: MasterDataEntry): string {
    switch (this.selectedType) {
      case 'hospitals':
        return record.registrationNumber ?? 'No registration number';
      case 'doctors':
        return [record.specialty, record.hospitalName].filter(Boolean).join(' · ') || '—';
      case 'staff':
        return [record.role, record.department].filter(Boolean).join(' · ') || '—';
      case 'products':
        return `${record.category || 'Product'} · ${record.shelfLifeDays ?? '—'} days`;
      case 'bag-types':
        return `${record.volumeMl ?? '—'} mL · ${record.anticoagulant || 'No anticoagulant listed'}`;
      case 'suppliers':
        return record.contactPerson || record.address || '—';
    }
  }

  private toCommand(): MasterDataCommand {
    return {
      code: this.value('code') ?? '',
      name: this.value('name') ?? '',
      registrationNumber: this.value('registrationNumber'),
      contactPerson: this.value('contactPerson'),
      phone: this.value('phone'),
      email: this.value('email'),
      address: this.value('address'),
      specialty: this.value('specialty'),
      hospitalName: this.value('hospitalName'),
      category: this.value('category'),
      department: this.value('department'),
      role: this.value('role'),
      shelfLifeDays: this.numberValue('shelfLifeDays'),
      volumeMl: this.numberValue('volumeMl'),
      anticoagulant: this.value('anticoagulant'),
      description: this.value('description'),
      password: this.value('password'),
      isActive: this.form['isActive'] !== false
    };
  }

  private value(key: string): string | undefined {
    const value = this.form[key];
    return typeof value === 'string' && value.trim() ? value.trim() : undefined;
  }

  private numberValue(key: string): number | undefined {
    const value = this.form[key];
    return typeof value === 'number' && Number.isFinite(value) ? value : undefined;
  }

  private getErrorMessage(error: unknown): string {
    if (error instanceof HttpErrorResponse) {
      return error.error?.detail ?? error.error?.message ?? error.message;
    }
    return 'An unexpected error occurred while saving master data.';
  }
}
