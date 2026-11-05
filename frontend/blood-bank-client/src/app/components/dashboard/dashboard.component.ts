import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from '../../core/services/auth.service';
import { BloodBankService } from '../../core/services/blood-bank.service';
import { DonorService } from '../../core/services/donor.service';
import { HospitalService } from '../../core/services/hospital.service';
import { BloodBag, BloodGroup, BloodRequest, BloodStock, ComponentType, DonationCamp, DonorDetails } from '../../core/models/blood-bank.model';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  template: `
    <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 space-y-8">
      <!-- Top Banner -->
      <div class="bg-gradient-to-r from-slate-900 via-rose-950 to-slate-900 rounded-3xl p-6 sm:p-8 text-white shadow-xl relative overflow-hidden">
        <div class="relative z-10 flex flex-col md:flex-row md:items-center justify-between gap-6">
          <div class="space-y-2">
            <div class="inline-flex items-center space-x-2 px-3 py-1 rounded-full bg-white/10 text-rose-300 text-xs font-semibold backdrop-blur-sm border border-white/10">
              <span class="w-2 h-2 rounded-full bg-rose-400 animate-pulse"></span>
              <span>Blood Bank A Management System</span>
            </div>
            <h1 class="text-2xl sm:text-3xl font-black tracking-tight">
              @if (authService.isBloodBank()) {
                Blood Bank A Central Command
              } @else if (authService.isHospital()) {
                Hospital Blood & Components Requisition Portal
              } @else {
                Donor Blood Community & Request Portal
              }
            </h1>
            <p class="text-slate-300 text-sm max-w-xl">
              Logged in as: <span class="font-semibold text-white">{{ authService.currentUserSignal()?.fullName }}</span> 
              @if (authService.currentUserSignal()?.hospitalName) {
                ({{ authService.currentUserSignal()?.hospitalName }})
              }
            </p>
          </div>

          <div class="flex items-center space-x-3">
            <span
              [ngClass]="{
                'bg-rose-500 text-white': authService.isBloodBank(),
                'bg-emerald-500 text-white': authService.isHospital(),
                'bg-sky-500 text-white': authService.isDonor()
              }"
              class="px-3.5 py-1.5 rounded-full text-xs font-bold uppercase tracking-wider shadow-sm"
            >
              Role: {{ authService.userRoleSignal() }}
            </span>
          </div>
        </div>
      </div>

      <!-- ========================================================================= -->
      <!-- 1. BLOOD BANK A OPERATOR PORTAL (Stock, Camps, Donors, Request Forms)    -->
      <!-- ========================================================================= -->
      @if (authService.isBloodBank()) {
        <div class="space-y-6">
          <!-- Blood Bank Tab Navigation -->
          <div class="flex flex-wrap items-center gap-2 border-b border-slate-200 pb-3">
            <button
              (click)="activeBankTab = 'stock'; showIssuedComponents.set(false)"
              [ngClass]="activeBankTab === 'stock' ? 'bg-rose-600 text-white shadow-md shadow-rose-600/30' : 'bg-white text-slate-700 hover:bg-slate-100 border border-slate-200'"
              class="px-5 py-2.5 rounded-xl text-sm font-bold transition-all flex items-center space-x-2"
            >
              <svg class="w-4 h-4 fill-current" viewBox="0 0 24 24"><path d="M12 2.69l5.66 5.66a8 8 0 1 1-11.31 0z"/></svg>
              <span>Stock Details</span>
            </button>

            <button
              (click)="activeBankTab = 'camps'"
              [ngClass]="activeBankTab === 'camps' ? 'bg-rose-600 text-white shadow-md shadow-rose-600/30' : 'bg-white text-slate-700 hover:bg-slate-100 border border-slate-200'"
              class="px-5 py-2.5 rounded-xl text-sm font-bold transition-all flex items-center space-x-2"
            >
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z"/></svg>
              <span>Camp Details</span>
            </button>

            <button
              (click)="activeBankTab = 'donors'"
              [ngClass]="activeBankTab === 'donors' ? 'bg-rose-600 text-white shadow-md shadow-rose-600/30' : 'bg-white text-slate-700 hover:bg-slate-100 border border-slate-200'"
              class="px-5 py-2.5 rounded-xl text-sm font-bold transition-all flex items-center space-x-2"
            >
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197M13 7a4 4 0 11-8 0 4 4 0 018 0z"/></svg>
              <span>Donor Details</span>
            </button>

            <button
              (click)="activeBankTab = 'requests'"
              [ngClass]="activeBankTab === 'requests' ? 'bg-rose-600 text-white shadow-md shadow-rose-600/30' : 'bg-white text-slate-700 hover:bg-slate-100 border border-slate-200'"
              class="px-5 py-2.5 rounded-xl text-sm font-bold transition-all flex items-center space-x-2 relative"
            >
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2"/></svg>
              <span>Incoming Request Forms</span>
              @if (pendingRequestsCount() > 0) {
                <span class="ml-1.5 px-2 py-0.5 text-xs font-black bg-amber-400 text-amber-950 rounded-full animate-bounce">
                  {{ pendingRequestsCount() }}
                </span>
              }
            </button>

            <button
              (click)="activeBankTab = 'issued'; showIssuedComponents.set(true)"
              [ngClass]="activeBankTab === 'issued' ? 'bg-indigo-600 text-white shadow-md shadow-indigo-600/30' : 'bg-white text-slate-700 hover:bg-slate-100 border border-slate-200'"
              class="px-5 py-2.5 rounded-xl text-sm font-bold transition-all flex items-center space-x-2"
            >
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h6l4 4v12a2 2 0 01-2 2z"/></svg>
              <span>Component Issue</span>
            </button>
          </div>

          <!-- TAB 1: STOCK DETAILS -->
          @if (activeBankTab === 'stock') {
            <div class="space-y-6">
              <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
                <div>
                  <h2 class="text-xl font-black text-slate-900">Blood Bank A Stock Levels</h2>
                  <p class="text-xs text-slate-500">Select a component to view its blood groups and available units</p>
                </div>
                <div class="flex items-center space-x-2">
                  <button
                    type="button"
                    (click)="confirmClearAllData()"
                    [disabled]="isClearing"
                    class="px-3.5 py-2.5 bg-slate-100 hover:bg-red-50 text-slate-600 hover:text-red-600 font-bold rounded-xl text-xs border border-slate-200 hover:border-red-200 transition-all flex items-center space-x-1.5 disabled:opacity-50"
                  >
                    <svg class="w-4 h-4 text-slate-500 hover:text-red-500" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"/></svg>
                    <span>{{ isClearing ? 'Clearing...' : 'Clear All Data (Wipe to Empty)' }}</span>
                  </button>
                  <button
                    type="button"
                    (click)="openAddStockModal()"
                    class="px-4 py-2.5 bg-rose-600 hover:bg-rose-700 text-white font-bold rounded-xl text-xs shadow-md shadow-rose-600/20 transition-all flex items-center space-x-1.5"
                  >
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"/></svg>
                    <span>Replenish / Add Units</span>
                  </button>
                </div>
              </div>

              <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-5 gap-4">
                @for (component of componentTypes; track component) {
                  <button
                    type="button"
                    (click)="selectStockComponent(component)"
                    [ngClass]="selectedStockComponent() === component
                      ? 'border-rose-500 bg-rose-50 ring-2 ring-rose-200'
                      : 'border-slate-200 bg-white hover:border-rose-300 hover:shadow-md'"
                    class="text-left p-4 rounded-2xl border shadow-sm transition-all"
                  >
                    <div class="flex items-start justify-between gap-2">
                      <span class="w-10 h-10 rounded-xl bg-rose-100 text-rose-700 flex items-center justify-center font-black text-sm">
                        {{ getComponentShortName(component) }}
                      </span>
                      @if (selectedStockComponent() === component) {
                        <span class="text-[10px] font-black uppercase text-rose-600">Selected</span>
                      }
                    </div>
                    <h3 class="mt-3 text-sm font-extrabold text-slate-900 leading-tight">{{ component }}</h3>
                    <p class="mt-1 text-xs text-slate-500">
                      {{ getComponentUnitTotal(component) }} units
                      <span class="text-slate-400">· {{ getComponentGroupCount(component) }} blood groups</span>
                    </p>
                  </button>
                }
              </div>

              @if (!showIssuedComponents()) {
                <div>
                  <div class="mb-3">
                    <h3 class="text-base font-black text-slate-900">{{ selectedStockComponent() }} by Blood Group</h3>
                    <p class="text-xs text-slate-500">Select a blood group to view its individual blood bags</p>
                  </div>
                  <div class="grid grid-cols-2 sm:grid-cols-4 lg:grid-cols-8 gap-3">
                    @for (bloodGroup of bloodGroups; track bloodGroup) {
                      <button
                        type="button"
                        (click)="selectBloodGroup(bloodGroup)"
                        [ngClass]="selectedBloodGroup() === bloodGroup
                          ? 'border-rose-500 bg-rose-50 ring-2 ring-rose-200'
                          : 'border-slate-200 bg-white hover:border-rose-300 hover:shadow-md'"
                        class="p-3 rounded-xl border shadow-sm transition-all"
                      >
                        <span class="inline-flex items-center justify-center w-10 h-10 rounded-lg bg-rose-100 text-rose-700 font-black">
                          {{ bloodGroup }}
                        </span>
                        <span class="block mt-2 text-xs font-bold text-slate-700">
                          {{ getBloodGroupUnitTotal(bloodGroup) }} units
                          <span class="block text-[10px] text-slate-400">{{ getBloodGroupBagCount(bloodGroup) }} bags</span>
                        </span>
                      </button>
                    }
                  </div>
                </div>
              }

              <div class="flex items-center justify-between">
                <div>
                  @if (showIssuedComponents()) {
                    <h3 class="text-base font-black text-slate-900">Component Issue</h3>
                    <p class="text-xs text-slate-500">Components issued to patients</p>
                  } @else {
                    <h3 class="text-base font-black text-slate-900">{{ selectedStockComponent() }}</h3>
                    <p class="text-xs text-slate-500">Available stock by blood group</p>
                  }
                </div>
                @if (!showIssuedComponents()) {
                  <button type="button" (click)="openAddStockModal(selectedStockComponent())" class="text-xs font-bold text-rose-600 hover:text-rose-700">
                  + Add {{ selectedStockComponent() }}
                  </button>
                }
              </div>

              @if (showIssuedComponents()) {
                <div class="bg-white rounded-2xl border border-slate-200/80 shadow-sm overflow-hidden">
                  <table class="w-full text-left text-sm text-slate-600">
                    <thead class="bg-slate-50 text-[11px] uppercase tracking-wider text-slate-500 font-bold border-b border-slate-100">
                      <tr>
                        <th class="py-3.5 px-6">Hospital</th>
                        <th class="py-3.5 px-6">Patient Name</th>
                        <th class="py-3.5 px-6">Component Name</th>
                        <th class="py-3.5 px-6">Component Type</th>
                        <th class="py-3.5 px-6">Bag ID</th>
                        <th class="py-3.5 px-6">Issued On</th>
                      </tr>
                    </thead>
                    <tbody class="divide-y divide-slate-100">
                      @for (request of issuedRequests(); track request.id) {
                        <tr class="hover:bg-slate-50/80">
                          <td class="py-4 px-6 font-bold text-slate-900">{{ request.hospitalName || request.requesterName }}</td>
                          <td class="py-4 px-6 font-bold text-slate-900">{{ request.patientName }}</td>
                          <td class="py-4 px-6 font-semibold text-slate-900">{{ request.componentType }}</td>
                          <td class="py-4 px-6">
                            <span class="inline-flex items-center justify-center w-10 h-8 rounded-lg bg-rose-50 text-rose-700 font-black border border-rose-200">{{ request.bloodGroup }}</span>
                          </td>
                          <td class="py-4 px-6 text-xs font-bold text-indigo-700">{{ request.issuedBagNumbers || 'Not recorded' }}</td>
                          <td class="py-4 px-6 text-xs text-slate-400">{{ (request.processedAt || request.createdAt) | date:'medium' }}</td>
                        </tr>
                      } @empty {
                        <tr>
                          <td colspan="6" class="py-10 px-6 text-center text-sm text-slate-500">No components have been issued yet.</td>
                        </tr>
                      }
                    </tbody>
                  </table>
                </div>
              }
          <div class="bg-white rounded-2xl border border-slate-200/80 shadow-sm overflow-hidden">
            <div class="px-6 py-4 border-b border-slate-100">
              <h3 class="text-sm font-black text-slate-900">Stock Details</h3>
              <p class="text-xs text-slate-500">
                @if (selectedBloodGroup()) {
                  {{ selectedStockComponent() }} · {{ selectedBloodGroup() }} bags
                } @else {
                  Select a blood group above to view individual bags
                }
              </p>
            </div>
            <table class="w-full text-left text-sm text-slate-600">
              <thead class="bg-slate-50 text-[11px] uppercase tracking-wider text-slate-500 font-bold">
                <tr>
                  <th class="py-3 px-6">Bag Number</th>
                  <th class="py-3 px-6">Blood Group</th>
                  <th class="py-3 px-6">Units</th>
                  <th class="py-3 px-6">Date Added</th>
                  <th class="py-3 px-6">Stock Period</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-slate-100">
                @for (bag of selectedBloodGroupBags(); track bag.id) {
                  <tr>
                    <td class="py-3 px-6 font-bold text-slate-900">{{ bag.bagNumber }}</td>
                    <td class="py-3 px-6">{{ bag.bloodGroup }}</td>
                    <td class="py-3 px-6 font-bold">{{ bag.quantityUnits }}</td>
                    <td class="py-3 px-6 text-xs text-slate-400">{{ bag.addedDate | date:'mediumDate' }}</td>
                    <td class="py-3 px-6 text-xs font-bold text-slate-600">{{ getStockPeriod(bag.addedDate) }}</td>
                  </tr>
                } @empty {
                  <tr><td colspan="5" class="py-6 text-center text-xs text-slate-500">
                    @if (selectedBloodGroup()) {
                      No bags added for this component and blood group.
                    } @else {
                      Select a blood group to view bag details.
                    }
                  </td></tr>
                }
              </tbody>
            </table>
          </div>
            </div>
          }

          @if (activeBankTab === 'issued') {
            <div class="space-y-6">
              <div>
                <h2 class="text-xl font-black text-slate-900">Component Issue</h2>
                <p class="text-xs text-slate-500">Components issued to patients from fulfilled requests</p>
              </div>

              <div class="bg-white rounded-2xl border border-slate-200/80 shadow-sm overflow-hidden">
                <table class="w-full text-left text-sm text-slate-600">
                  <thead class="bg-slate-50 text-[11px] uppercase tracking-wider text-slate-500 font-bold border-b border-slate-100">
                    <tr>
                      <th class="py-3.5 px-6">Hospital</th>
                      <th class="py-3.5 px-6">Patient Name</th>
                      <th class="py-3.5 px-6">Component Name</th>
                      <th class="py-3.5 px-6">Component Type</th>
                      <th class="py-3.5 px-6">Bag ID</th>
                      <th class="py-3.5 px-6">Issued On</th>
                    </tr>
                  </thead>
                  <tbody class="divide-y divide-slate-100">
                    @for (request of issuedRequests(); track request.id) {
                      <tr class="hover:bg-slate-50/80">
                        <td class="py-4 px-6 font-bold text-slate-900">{{ request.hospitalName || request.requesterName }}</td>
                        <td class="py-4 px-6 font-bold text-slate-900">{{ request.patientName }}</td>
                        <td class="py-4 px-6 font-semibold text-slate-900">{{ request.componentType }}</td>
                        <td class="py-4 px-6">
                          <span class="inline-flex items-center justify-center w-10 h-8 rounded-lg bg-rose-50 text-rose-700 font-black border border-rose-200">{{ request.bloodGroup }}</span>
                        </td>
                        <td class="py-4 px-6 text-xs font-bold text-indigo-700">{{ request.issuedBagNumbers || 'Not recorded' }}</td>
                        <td class="py-4 px-6 text-xs text-slate-400">{{ (request.processedAt || request.createdAt) | date:'medium' }}</td>
                      </tr>
                    } @empty {
                      <tr>
                        <td colspan="6" class="py-10 px-6 text-center text-sm text-slate-500">No components have been issued yet.</td>
                      </tr>
                    }
                  </tbody>
                </table>
              </div>
            </div>
          }

          @if (activeBankTab === 'camps') {
            <div class="space-y-6">
              <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
                <div>
                  <h2 class="text-xl font-black text-slate-900">Donation Camps by Blood Bank A</h2>
                  <p class="text-xs text-slate-500">Upcoming and completed field blood collection campaigns</p>
                </div>
                <button
                  type="button"
                  (click)="showCreateCampModal = true"
                  class="px-4 py-2.5 bg-rose-600 hover:bg-rose-700 text-white font-bold rounded-xl text-xs shadow-md shadow-rose-600/20 transition-all flex items-center space-x-1.5 self-start"
                >
                  <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"/></svg>
                  <span>Organize New Camp</span>
                </button>
              </div>

              <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-5">
                @for (camp of bankCamps(); track camp.id) {
                  <div class="bg-white p-6 rounded-2xl border border-slate-200/80 shadow-sm space-y-4 hover:shadow-md transition-shadow">
                    <div class="flex items-start justify-between">
                      <span class="px-2.5 py-1 text-[11px] font-bold rounded-full"
                        [ngClass]="{
                          'bg-sky-100 text-sky-800': camp.status === 'Scheduled',
                          'bg-amber-100 text-amber-800': camp.status === 'Ongoing',
                          'bg-emerald-100 text-emerald-800': camp.status === 'Completed'
                        }"
                      >
                        {{ camp.status }}
                      </span>
                      <span class="text-xs font-bold text-slate-400">{{ camp.campDate | date:'mediumDate' }}</span>
                    </div>

                    <div>
                      <h3 class="font-extrabold text-slate-900 text-base leading-snug">{{ camp.campName }}</h3>
                      <p class="text-xs text-slate-500 mt-1 flex items-center">
                        <svg class="w-3.5 h-3.5 mr-1 text-slate-400" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z"/><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 11a3 3 0 11-6 0 3 3 0 016 0z"/></svg>
                        {{ camp.location }}
                      </p>
                    </div>

                    <div class="pt-2 border-t border-slate-100 flex items-center justify-between text-xs">
                      <span class="text-slate-500">Target Collection:</span>
                      <span class="font-black text-rose-600">{{ camp.targetUnits }} units</span>
                    </div>

                    @if (camp.notes) {
                      <p class="text-xs text-slate-400 italic bg-slate-50 p-2.5 rounded-xl border border-slate-100">
                        "{{ camp.notes }}"
                      </p>
                    }
                  </div>
                }
              </div>
            </div>
          }

          <!-- TAB 3: DONOR DETAILS -->
          @if (activeBankTab === 'donors') {
            <div class="space-y-6">
              <div>
                <h2 class="text-xl font-black text-slate-900">Registered Donors Directory</h2>
                <p class="text-xs text-slate-500">Contact information, blood groups, and donation history</p>
              </div>

              <div class="bg-white rounded-2xl border border-slate-200/80 shadow-sm overflow-hidden">
                <table class="w-full text-left text-sm text-slate-600">
                  <thead class="bg-slate-50 text-[11px] uppercase tracking-wider text-slate-500 font-bold border-b border-slate-100">
                    <tr>
                      <th class="py-3.5 px-6">Donor Name</th>
                      <th class="py-3.5 px-6">Blood Group</th>
                      <th class="py-3.5 px-6">Contact Details</th>
                      <th class="py-3.5 px-6">Donations Logged</th>
                      <th class="py-3.5 px-6">Eligibility Status</th>
                    </tr>
                  </thead>
                  <tbody class="divide-y divide-slate-100">
                    @for (donor of bankDonors(); track donor.id) {
                      <tr class="hover:bg-slate-50/80">
                        <td class="py-4 px-6 font-bold text-slate-900">{{ donor.fullName }}</td>
                        <td class="py-4 px-6">
                          <span class="px-2.5 py-1 text-xs font-black rounded-lg bg-rose-50 text-rose-700 border border-rose-200">
                            {{ donor.bloodGroup || 'Pending' }}
                          </span>
                        </td>
                        <td class="py-4 px-6 text-xs">
                          <div>{{ donor.email }}</div>
                          <div class="text-slate-400">{{ donor.phone || 'No phone recorded' }}</div>
                        </td>
                        <td class="py-4 px-6 font-bold text-slate-900">{{ donor.totalDonations }} donations</td>
                        <td class="py-4 px-6">
                          @if (donor.isEligibleToDonate) {
                            <span class="px-2.5 py-1 text-xs font-bold rounded-full bg-emerald-100 text-emerald-800 border border-emerald-200">Eligible to Donate</span>
                          } @else {
                            <span class="px-2.5 py-1 text-xs font-bold rounded-full bg-amber-100 text-amber-800 border border-amber-200">Cooldown Active</span>
                          }
                        </td>
                      </tr>
                    }
                  </tbody>
                </table>
              </div>
            </div>
          }

          <!-- TAB 4: REQUEST FORMS (HOSPITALS & DONORS) -->
          @if (activeBankTab === 'requests') {
            <div class="space-y-6">
              <div>
                <h2 class="text-xl font-black text-slate-900">Incoming Blood & Component Requests</h2>
                <p class="text-xs text-slate-500">Create and manage requisitions for Hospitals, Donors, and Blood Bank A</p>
              </div>

              <div class="bg-white rounded-2xl border border-slate-200/80 shadow-sm p-5">
                <h3 class="text-base font-black text-slate-900 mb-4">Create Request</h3>
                <form [formGroup]="bloodBankRequestForm" (ngSubmit)="submitBloodBankRequest()" class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
                  <input formControlName="hospitalName" placeholder="Hospital name" class="px-3 py-2 rounded-xl border border-slate-300 text-sm"/>
                  <input formControlName="patientName" placeholder="Patient name" class="px-3 py-2 rounded-xl border border-slate-300 text-sm"/>
                  <select formControlName="bloodGroup" class="px-3 py-2 rounded-xl border border-slate-300 text-sm">
                    @for (bg of bloodGroups; track bg) { <option [value]="bg">{{ bg }}</option> }
                  </select>
                  <select formControlName="componentType" class="px-3 py-2 rounded-xl border border-slate-300 text-sm">
                    @for (component of componentTypes; track component) { <option [value]="component">{{ component }}</option> }
                  </select>
                  <input type="number" min="1" formControlName="quantityUnits" placeholder="Units" class="px-3 py-2 rounded-xl border border-slate-300 text-sm"/>
                  <select formControlName="urgency" class="px-3 py-2 rounded-xl border border-slate-300 text-sm">
                    <option value="Normal">Normal</option><option value="Urgent">Urgent</option><option value="Critical">Critical</option>
                  </select>
                  <input formControlName="hospitalDepartment" placeholder="Department (optional)" class="px-3 py-2 rounded-xl border border-slate-300 text-sm"/>
                  <input formControlName="medicalReason" placeholder="Reason (optional)" class="px-3 py-2 rounded-xl border border-slate-300 text-sm"/>
                  <button type="submit" [disabled]="bloodBankRequestForm.invalid || isSubmittingBloodBankRequest" class="px-4 py-2 rounded-xl bg-rose-600 text-white font-bold text-sm disabled:opacity-50">
                    {{ isSubmittingBloodBankRequest ? 'Creating...' : 'Create Request' }}
                  </button>
                </form>
                @if (bloodBankRequestError) {
                  <p class="mt-3 text-xs text-red-600">{{ bloodBankRequestError }}</p>
                }
              </div>

              <div class="bg-white rounded-2xl border border-slate-200/80 shadow-sm overflow-hidden">
                <table class="w-full text-left text-sm text-slate-600">
                  <thead class="bg-slate-50 text-[11px] uppercase tracking-wider text-slate-500 font-bold border-b border-slate-100">
                    <tr>
                      <th class="py-3.5 px-6">Requester</th>
                      <th class="py-3.5 px-6">Patient & Reason</th>
                      <th class="py-3.5 px-6">Item & Units</th>
                      <th class="py-3.5 px-6">Urgency</th>
                      <th class="py-3.5 px-6">Status</th>
                      <th class="py-3.5 px-6 text-right">Actions</th>
                    </tr>
                  </thead>
                  <tbody class="divide-y divide-slate-100">
                    @for (req of bankRequests(); track req.id) {
                      <tr class="hover:bg-slate-50/80">
                        <td class="py-4 px-6">
                          <span class="text-xs font-bold px-2 py-0.5 rounded-md"
                            [ngClass]="req.requesterType === 'Hospital' ? 'bg-emerald-100 text-emerald-800' : 'bg-sky-100 text-sky-800'"
                          >
                            {{ req.requesterType }}
                          </span>
                          <div class="font-bold text-slate-900 mt-1">{{ req.hospitalName || req.requesterName }}</div>
                          <div class="text-[11px] text-slate-400">{{ req.requesterContact }}</div>
                        </td>
                        <td class="py-4 px-6">
                          <div class="font-bold text-slate-900">{{ req.patientName }}</div>
                          <div class="text-xs text-slate-500">{{ req.medicalReason || 'Emergency treatment' }}</div>
                          @if (req.hospitalDepartment) {
                            <div class="text-[11px] text-slate-400">Dept: {{ req.hospitalDepartment }}</div>
                          }
                        </td>
                        <td class="py-4 px-6">
                          <div class="flex items-center space-x-2">
                            <span class="px-2 py-0.5 font-black text-rose-700 bg-rose-50 border border-rose-200 rounded">
                              {{ req.bloodGroup }}
                            </span>
                            <span class="font-semibold text-slate-800">{{ req.componentType }}</span>
                          </div>
                          <div class="text-xs font-bold text-slate-500 mt-1">{{ req.quantityUnits }} units requested</div>
                          @if (req.issuedBagNumbers) {
                            <div class="text-xs font-bold text-indigo-700 mt-2">Bags issued: {{ req.issuedBagNumbers }}</div>
                          }
                        </td>
                        <td class="py-4 px-6">
                          <span class="px-2 py-0.5 text-xs font-bold rounded-full"
                            [ngClass]="{
                              'bg-red-100 text-red-800': req.urgency === 'Critical',
                              'bg-amber-100 text-amber-800': req.urgency === 'Urgent',
                              'bg-slate-100 text-slate-700': req.urgency === 'Normal'
                            }"
                          >
                            {{ req.urgency }}
                          </span>
                        </td>
                        <td class="py-4 px-6 font-bold">
                          <span class="px-2.5 py-1 text-xs rounded-full"
                            [ngClass]="{
                              'bg-amber-100 text-amber-800': req.status === 'Pending',
                              'bg-blue-100 text-blue-800': req.status === 'Approved',
                              'bg-emerald-100 text-emerald-800': req.status === 'Fulfilled',
                              'bg-red-100 text-red-800': req.status === 'Rejected'
                            }"
                          >
                            {{ req.status }}
                          </span>
                        </td>
                        <td class="py-4 px-6 text-right whitespace-nowrap">
                          @if (req.status === 'Pending') {
                            <div class="flex items-center justify-end space-x-1.5">
                              <button
                                (click)="processBankRequest(req.id, 'Approved')"
                                class="px-2.5 py-1.5 text-xs font-bold rounded-lg bg-blue-50 text-blue-700 hover:bg-blue-100"
                              >
                                Approve
                              </button>
                              <button
                                (click)="processBankRequest(req.id, 'Fulfilled')"
                                class="px-2.5 py-1.5 text-xs font-bold rounded-lg bg-emerald-600 text-white hover:bg-emerald-700 shadow-sm"
                              >
                                Fulfill & Deduct
                              </button>
                              <button
                                (click)="processBankRequest(req.id, 'Rejected')"
                                class="px-2.5 py-1.5 text-xs font-bold rounded-lg bg-red-50 text-red-600 hover:bg-red-100"
                              >
                                Reject
                              </button>
                            </div>
                          } @else if (req.status === 'Approved') {
                            <button
                              (click)="processBankRequest(req.id, 'Fulfilled')"
                              class="px-3 py-1.5 text-xs font-bold rounded-lg bg-emerald-600 text-white hover:bg-emerald-700 shadow-sm"
                            >
                              Dispatch & Fulfill
                            </button>
                          } @else {
                            <span class="text-xs text-slate-400 italic">Closed</span>
                          }
                        </td>
                      </tr>
                    }
                  </tbody>
                </table>
              </div>
            </div>
          }
        </div>
      }

      <!-- ========================================================================= -->
      <!-- 2. HOSPITAL PORTAL (Only Request Blood/Components & Track Requisitions)   -->
      <!-- ========================================================================= -->
      @if (authService.isHospital()) {
        <div class="space-y-6">
          <div class="flex flex-wrap items-center gap-2 border-b border-slate-200 pb-3">
            <button
              (click)="activeHospitalTab = 'new-req'"
              [ngClass]="activeHospitalTab === 'new-req' ? 'bg-emerald-600 text-white shadow-md shadow-emerald-600/30' : 'bg-white text-slate-700 hover:bg-slate-100 border border-slate-200'"
              class="px-5 py-2.5 rounded-xl text-sm font-bold transition-all flex items-center space-x-2"
            >
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"/></svg>
              <span>Submit Blood / Component Requisition</span>
            </button>

            <button
              (click)="activeHospitalTab = 'my-req'"
              [ngClass]="activeHospitalTab === 'my-req' ? 'bg-emerald-600 text-white shadow-md shadow-emerald-600/30' : 'bg-white text-slate-700 hover:bg-slate-100 border border-slate-200'"
              class="px-5 py-2.5 rounded-xl text-sm font-bold transition-all flex items-center space-x-2"
            >
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2"/></svg>
              <span>Track Hospital Requisitions ({{ hospitalRequests().length }})</span>
            </button>

            <button
              (click)="activeHospitalTab = 'stock-view'"
              [ngClass]="activeHospitalTab === 'stock-view' ? 'bg-emerald-600 text-white shadow-md shadow-emerald-600/30' : 'bg-white text-slate-700 hover:bg-slate-100 border border-slate-200'"
              class="px-5 py-2.5 rounded-xl text-sm font-bold transition-all flex items-center space-x-2"
            >
              <svg class="w-4 h-4 fill-current" viewBox="0 0 24 24"><path d="M12 2.69l5.66 5.66a8 8 0 1 1-11.31 0z"/></svg>
              <span>Blood Bank A Availability</span>
            </button>
          </div>

          <!-- TAB 1: HOSPITAL REQUISITION FORM -->
          @if (activeHospitalTab === 'new-req') {
            <div class="bg-white p-6 sm:p-8 rounded-2xl border border-slate-200 shadow-sm max-w-2xl mx-auto space-y-6">
              <div class="border-b border-slate-100 pb-4">
                <h2 class="text-xl font-black text-slate-900">Hospital Blood & Component Requisition</h2>
                <p class="text-xs text-slate-500 mt-1">Official dispatch request from your healthcare facility to Blood Bank A</p>
              </div>

              @if (hospitalSubmitSuccess) {
                <div class="p-4 rounded-xl bg-emerald-50 border border-emerald-200 text-emerald-800 text-sm flex items-center space-x-2">
                  <svg class="w-5 h-5 text-emerald-600" fill="currentColor" viewBox="0 0 20 20"><path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clip-rule="evenodd"/></svg>
                  <span>Requisition transmitted successfully to Blood Bank A!</span>
                </div>
              }

              <form [formGroup]="hospitalForm" (ngSubmit)="submitHospitalRequest()" class="space-y-4">
                <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
                  <div>
                    <label class="block text-xs font-bold uppercase tracking-wider text-slate-700 mb-1">Patient Name *</label>
                    <input type="text" formControlName="patientName" placeholder="Patient Full Name" class="w-full px-4 py-2.5 rounded-xl border border-slate-300 text-sm"/>
                  </div>
                  <div>
                    <label class="block text-xs font-bold uppercase tracking-wider text-slate-700 mb-1">Department / Ward</label>
                    <input type="text" formControlName="hospitalDepartment" placeholder="e.g. ICU / Surgery Ward" class="w-full px-4 py-2.5 rounded-xl border border-slate-300 text-sm"/>
                  </div>
                </div>

                <div class="grid grid-cols-1 sm:grid-cols-3 gap-4">
                  <div>
                    <label class="block text-xs font-bold uppercase tracking-wider text-slate-700 mb-1">Blood Group *</label>
                    <select formControlName="bloodGroup" class="w-full px-4 py-2.5 rounded-xl border border-slate-300 bg-white text-sm font-bold text-slate-800">
                      @for (bg of bloodGroups; track bg) { <option [value]="bg">{{ bg }}</option> }
                    </select>
                  </div>

                  <div>
                    <label class="block text-xs font-bold uppercase tracking-wider text-slate-700 mb-1">Component Type *</label>
                    <select formControlName="componentType" class="w-full px-4 py-2.5 rounded-xl border border-slate-300 bg-white text-sm text-slate-800">
                      @for (comp of componentTypes; track comp) { <option [value]="comp">{{ comp }}</option> }
                    </select>
                  </div>

                  <div>
                    <label class="block text-xs font-bold uppercase tracking-wider text-slate-700 mb-1">Quantity (Units) *</label>
                    <input type="number" min="1" max="50" formControlName="quantityUnits" class="w-full px-4 py-2.5 rounded-xl border border-slate-300 text-sm font-bold"/>
                  </div>
                </div>

                <div>
                  <label class="block text-xs font-bold uppercase tracking-wider text-slate-700 mb-1">Urgency Priority</label>
                  <div class="grid grid-cols-3 gap-3">
                    <button type="button" (click)="hospitalForm.patchValue({ urgency: 'Normal' })" [ngClass]="hospitalForm.get('urgency')?.value === 'Normal' ? 'bg-slate-800 text-white font-bold' : 'bg-slate-100 text-slate-700'" class="py-2 rounded-xl text-xs font-semibold">Normal (Elective)</button>
                    <button type="button" (click)="hospitalForm.patchValue({ urgency: 'Urgent' })" [ngClass]="hospitalForm.get('urgency')?.value === 'Urgent' ? 'bg-amber-500 text-white font-bold' : 'bg-slate-100 text-slate-700'" class="py-2 rounded-xl text-xs font-semibold">Urgent (Within 4h)</button>
                    <button type="button" (click)="hospitalForm.patchValue({ urgency: 'Critical' })" [ngClass]="hospitalForm.get('urgency')?.value === 'Critical' ? 'bg-red-600 text-white font-bold' : 'bg-slate-100 text-slate-700'" class="py-2 rounded-xl text-xs font-semibold">Critical (Immediate)</button>
                  </div>
                </div>

                <div>
                  <label class="block text-xs font-bold uppercase tracking-wider text-slate-700 mb-1">Clinical Indication / Diagnosis</label>
                  <textarea rows="2" formControlName="medicalReason" placeholder="e.g. Acute surgical hemorrhage, severe anemia, etc." class="w-full px-4 py-2 rounded-xl border border-slate-300 text-sm"></textarea>
                </div>

                <button type="submit" [disabled]="hospitalForm.invalid" class="w-full py-3.5 bg-emerald-600 hover:bg-emerald-700 text-white font-bold rounded-xl shadow-lg shadow-emerald-600/30 transition-all text-sm disabled:opacity-50">
                  Transmit Requisition to Blood Bank A
                </button>
              </form>
            </div>
          }

          <!-- TAB 2: MY REQUISITIONS TRACKER -->
          @if (activeHospitalTab === 'my-req') {
            <div class="bg-white rounded-2xl border border-slate-200/80 shadow-sm overflow-hidden">
              <table class="w-full text-left text-sm text-slate-600">
                <thead class="bg-slate-50 text-[11px] uppercase tracking-wider text-slate-500 font-bold border-b border-slate-100">
                  <tr>
                    <th class="py-3.5 px-6">Patient Name</th>
                    <th class="py-3.5 px-6">Item & Quantity</th>
                    <th class="py-3.5 px-6">Priority</th>
                    <th class="py-3.5 px-6">Status</th>
                    <th class="py-3.5 px-6">Requisition Date</th>
                  </tr>
                </thead>
                <tbody class="divide-y divide-slate-100">
                  @for (req of hospitalRequests(); track req.id) {
                    <tr class="hover:bg-slate-50/80">
                      <td class="py-4 px-6 font-bold text-slate-900">{{ req.patientName }}</td>
                      <td class="py-4 px-6">
                        <span class="px-2 py-0.5 rounded bg-rose-50 text-rose-700 font-black border border-rose-200 mr-1.5">{{ req.bloodGroup }}</span>
                        <span class="font-semibold text-slate-800">{{ req.componentType }} ({{ req.quantityUnits }} units)</span>
                      </td>
                      <td class="py-4 px-6 font-bold">{{ req.urgency }}</td>
                      <td class="py-4 px-6">
                        <span class="px-2.5 py-1 text-xs font-bold rounded-full"
                          [ngClass]="{
                            'bg-amber-100 text-amber-800': req.status === 'Pending',
                            'bg-blue-100 text-blue-800': req.status === 'Approved',
                            'bg-emerald-100 text-emerald-800': req.status === 'Fulfilled',
                            'bg-red-100 text-red-800': req.status === 'Rejected'
                          }"
                        >
                          {{ req.status }}
                        </span>
                      </td>
                      <td class="py-4 px-6 text-xs text-slate-400">{{ req.createdAt | date:'medium' }}</td>
                    </tr>
                  }
                </tbody>
              </table>
            </div>
          }

          <!-- TAB 3: STOCK AVAILABILITY VIEW -->
          @if (activeHospitalTab === 'stock-view') {
            <div class="bg-white rounded-2xl border border-slate-200/80 shadow-sm overflow-hidden">
              <table class="w-full text-left text-sm text-slate-600">
                <thead class="bg-slate-50 text-[11px] uppercase tracking-wider text-slate-500 font-bold border-b border-slate-100">
                  <tr>
                    <th class="py-3.5 px-6">Blood Group</th>
                    <th class="py-3.5 px-6">Component Type</th>
                    <th class="py-3.5 px-6">Available Units</th>
                    <th class="py-3.5 px-6">Depository Status</th>
                  </tr>
                </thead>
                <tbody class="divide-y divide-slate-100">
                  @for (stock of hospitalStockView(); track stock.id) {
                    <tr class="hover:bg-slate-50/80">
                      <td class="py-4 px-6 font-bold">
                        <span class="px-2.5 py-1 rounded-xl bg-rose-50 text-rose-700 font-black border border-rose-200">{{ stock.bloodGroup }}</span>
                      </td>
                      <td class="py-4 px-6 font-semibold text-slate-900">{{ stock.componentType }}</td>
                      <td class="py-4 px-6 font-extrabold text-slate-900">{{ stock.quantityUnits }} units</td>
                      <td class="py-4 px-6">
                        @if (stock.quantityUnits <= 5) {
                          <span class="px-2 py-0.5 text-xs font-bold rounded-full bg-red-100 text-red-800">Critical Shortage</span>
                        } @else {
                          <span class="px-2 py-0.5 text-xs font-bold rounded-full bg-emerald-100 text-emerald-800">Available</span>
                        }
                      </td>
                    </tr>
                  }
                </tbody>
              </table>
            </div>
          }
        </div>
      }

      <!-- ========================================================================= -->
      <!-- 3. DONOR PORTAL (Request Blood/Components, My Requests, Camps, History)  -->
      <!-- ========================================================================= -->
      @if (authService.isDonor()) {
        <div class="space-y-6">
          <div class="flex flex-wrap items-center gap-2 border-b border-slate-200 pb-3">
            <button
              (click)="activeDonorTab = 'request'"
              [ngClass]="activeDonorTab === 'request' ? 'bg-sky-600 text-white shadow-md shadow-sky-600/30' : 'bg-white text-slate-700 hover:bg-slate-100 border border-slate-200'"
              class="px-5 py-2.5 rounded-xl text-sm font-bold transition-all flex items-center space-x-2"
            >
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"/></svg>
              <span>Request Blood or Components</span>
            </button>

            <button
              (click)="activeDonorTab = 'my-requests'"
              [ngClass]="activeDonorTab === 'my-requests' ? 'bg-sky-600 text-white shadow-md shadow-sky-600/30' : 'bg-white text-slate-700 hover:bg-slate-100 border border-slate-200'"
              class="px-5 py-2.5 rounded-xl text-sm font-bold transition-all flex items-center space-x-2"
            >
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2"/></svg>
              <span>My Requests & Tracking ({{ donorRequests().length }})</span>
            </button>

            <button
              (click)="activeDonorTab = 'camps'"
              [ngClass]="activeDonorTab === 'camps' ? 'bg-sky-600 text-white shadow-md shadow-sky-600/30' : 'bg-white text-slate-700 hover:bg-slate-100 border border-slate-200'"
              class="px-5 py-2.5 rounded-xl text-sm font-bold transition-all flex items-center space-x-2"
            >
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z"/></svg>
              <span>Upcoming Camps to Donate</span>
            </button>
          </div>

          <!-- DONOR TAB 1: SUBMIT BLOOD/COMPONENT REQUEST -->
          @if (activeDonorTab === 'request') {
            <div class="bg-white p-6 sm:p-8 rounded-2xl border border-slate-200 shadow-sm max-w-2xl mx-auto space-y-6">
              <div class="border-b border-slate-100 pb-4">
                <h2 class="text-xl font-black text-slate-900">Personal Blood / Component Request</h2>
                <p class="text-xs text-slate-500 mt-1">Submit an emergency or planned blood request to Blood Bank A for yourself or a loved one</p>
              </div>

              @if (donorSubmitSuccess) {
                <div class="p-4 rounded-xl bg-sky-50 border border-sky-200 text-sky-800 text-sm flex items-center space-x-2">
                  <svg class="w-5 h-5 text-sky-600" fill="currentColor" viewBox="0 0 20 20"><path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clip-rule="evenodd"/></svg>
                  <span>Request registered! Blood Bank A operators will review and contact you.</span>
                </div>
              }

              <form [formGroup]="donorForm" (ngSubmit)="submitDonorRequest()" class="space-y-4">
                <div>
                  <label class="block text-xs font-bold uppercase tracking-wider text-slate-700 mb-1">Patient Name *</label>
                  <input type="text" formControlName="patientName" placeholder="Patient Full Name" class="w-full px-4 py-2.5 rounded-xl border border-slate-300 text-sm"/>
                </div>

                <div class="grid grid-cols-1 sm:grid-cols-3 gap-4">
                  <div>
                    <label class="block text-xs font-bold uppercase tracking-wider text-slate-700 mb-1">Blood Group *</label>
                    <select formControlName="bloodGroup" class="w-full px-4 py-2.5 rounded-xl border border-slate-300 bg-white text-sm font-bold text-slate-800">
                      @for (bg of bloodGroups; track bg) { <option [value]="bg">{{ bg }}</option> }
                    </select>
                  </div>

                  <div>
                    <label class="block text-xs font-bold uppercase tracking-wider text-slate-700 mb-1">Component Needed *</label>
                    <select formControlName="componentType" class="w-full px-4 py-2.5 rounded-xl border border-slate-300 bg-white text-sm text-slate-800">
                      @for (comp of componentTypes; track comp) { <option [value]="comp">{{ comp }}</option> }
                    </select>
                  </div>

                  <div>
                    <label class="block text-xs font-bold uppercase tracking-wider text-slate-700 mb-1">Units (Quantity) *</label>
                    <input type="number" min="1" max="10" formControlName="quantityUnits" class="w-full px-4 py-2.5 rounded-xl border border-slate-300 text-sm font-bold"/>
                  </div>
                </div>

                <div>
                  <label class="block text-xs font-bold uppercase tracking-wider text-slate-700 mb-1">Hospital / Clinic Admitted To</label>
                  <input type="text" formControlName="hospitalDepartment" placeholder="e.g. City General Hospital, Room 302" class="w-full px-4 py-2.5 rounded-xl border border-slate-300 text-sm"/>
                </div>

                <div>
                  <label class="block text-xs font-bold uppercase tracking-wider text-slate-700 mb-1">Urgency</label>
                  <select formControlName="urgency" class="w-full px-4 py-2.5 rounded-xl border border-slate-300 bg-white text-sm">
                    <option value="Normal">Normal</option>
                    <option value="Urgent">Urgent</option>
                    <option value="Critical">Critical (Immediate Emergency)</option>
                  </select>
                </div>

                <div>
                  <label class="block text-xs font-bold uppercase tracking-wider text-slate-700 mb-1">Medical Reason</label>
                  <textarea rows="2" formControlName="medicalReason" placeholder="Describe patient medical situation..." class="w-full px-4 py-2 rounded-xl border border-slate-300 text-sm"></textarea>
                </div>

                <button type="submit" [disabled]="donorForm.invalid" class="w-full py-3.5 bg-sky-600 hover:bg-sky-700 text-white font-bold rounded-xl shadow-lg shadow-sky-600/30 transition-all text-sm disabled:opacity-50">
                  Submit Request to Blood Bank A
                </button>
              </form>
            </div>
          }

          <!-- DONOR TAB 2: MY REQUESTS TRACKER -->
          @if (activeDonorTab === 'my-requests') {
            <div class="bg-white rounded-2xl border border-slate-200/80 shadow-sm overflow-hidden">
              <table class="w-full text-left text-sm text-slate-600">
                <thead class="bg-slate-50 text-[11px] uppercase tracking-wider text-slate-500 font-bold border-b border-slate-100">
                  <tr>
                    <th class="py-3.5 px-6">Patient Name</th>
                    <th class="py-3.5 px-6">Item & Quantity</th>
                    <th class="py-3.5 px-6">Urgency</th>
                    <th class="py-3.5 px-6">Status</th>
                    <th class="py-3.5 px-6">Remarks from Blood Bank A</th>
                  </tr>
                </thead>
                <tbody class="divide-y divide-slate-100">
                  @for (req of donorRequests(); track req.id) {
                    <tr class="hover:bg-slate-50/80">
                      <td class="py-4 px-6 font-bold text-slate-900">{{ req.patientName }}</td>
                      <td class="py-4 px-6">
                        <span class="px-2 py-0.5 rounded bg-rose-50 text-rose-700 font-black border border-rose-200 mr-1.5">{{ req.bloodGroup }}</span>
                        <span class="font-semibold text-slate-800">{{ req.componentType }} ({{ req.quantityUnits }} units)</span>
                      </td>
                      <td class="py-4 px-6 font-bold">{{ req.urgency }}</td>
                      <td class="py-4 px-6">
                        <span class="px-2.5 py-1 text-xs font-bold rounded-full"
                          [ngClass]="{
                            'bg-amber-100 text-amber-800': req.status === 'Pending',
                            'bg-blue-100 text-blue-800': req.status === 'Approved',
                            'bg-emerald-100 text-emerald-800': req.status === 'Fulfilled',
                            'bg-red-100 text-red-800': req.status === 'Rejected'
                          }"
                        >
                          {{ req.status }}
                        </span>
                      </td>
                      <td class="py-4 px-6 text-xs text-slate-500 italic">{{ req.adminComments || 'Processing in progress...' }}</td>
                    </tr>
                  }
                </tbody>
              </table>
            </div>
          }

          <!-- DONOR TAB 3: UPCOMING CAMPS -->
          @if (activeDonorTab === 'camps') {
            <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-5">
              @for (camp of donorCamps(); track camp.id) {
                <div class="bg-white p-6 rounded-2xl border border-slate-200/80 shadow-sm space-y-4 hover:shadow-md transition-shadow">
                  <div class="flex items-start justify-between">
                    <span class="px-2.5 py-1 text-[11px] font-bold rounded-full bg-sky-100 text-sky-800">
                      {{ camp.status }}
                    </span>
                    <span class="text-xs font-bold text-slate-400">{{ camp.campDate | date:'mediumDate' }}</span>
                  </div>

                  <div>
                    <h3 class="font-extrabold text-slate-900 text-base">{{ camp.campName }}</h3>
                    <p class="text-xs text-slate-500 mt-1">{{ camp.location }}</p>
                  </div>

                  <div class="pt-2 border-t border-slate-100 flex items-center justify-between text-xs">
                    <span class="text-slate-500">Contact Organizer:</span>
                    <span class="font-bold text-slate-800">{{ camp.contactPhone }}</span>
                  </div>

                  @if (camp.notes) {
                    <p class="text-xs text-slate-400 italic bg-slate-50 p-2.5 rounded-xl">"{{ camp.notes }}"</p>
                  }
                </div>
              }
            </div>
          }
        </div>
      }
    </div>

    <!-- ADD / REPLENISH STOCK MODAL (Blood Bank A only) -->
    @if (showAddStockModal) {
      <div class="fixed inset-0 z-50 bg-slate-900/60 backdrop-blur-sm flex items-center justify-center p-4">
        <div class="bg-white rounded-2xl max-w-md w-full p-6 shadow-2xl space-y-4 border border-slate-100">
          <div class="flex items-center justify-between border-b border-slate-100 pb-3">
            <h3 class="text-lg font-bold text-slate-900">
              {{ stockModalMode === 'replenish' ? 'Replenish Blood Bank A Units' : 'Add Blood Bag Barcode' }}
            </h3>
            <button type="button" (click)="showAddStockModal = false" class="text-slate-400 hover:text-slate-600">
              <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/></svg>
            </button>
          </div>

          <!-- Mode Toggle -->
          <div class="flex bg-slate-100 p-1 rounded-xl text-xs font-bold">
            <button
              type="button"
              (click)="stockModalMode = 'replenish'"
              [ngClass]="stockModalMode === 'replenish' ? 'bg-white text-rose-700 shadow-sm' : 'text-slate-600 hover:text-slate-900'"
              class="flex-1 py-1.5 rounded-lg transition-all text-center"
            >
              Replenish Units
            </button>
            <button
              type="button"
              (click)="stockModalMode = 'bag'"
              [ngClass]="stockModalMode === 'bag' ? 'bg-white text-rose-700 shadow-sm' : 'text-slate-600 hover:text-slate-900'"
              class="flex-1 py-1.5 rounded-lg transition-all text-center"
            >
              Add Single Bag
            </button>
          </div>

          @if (addStockError) {
            <div class="p-3.5 rounded-xl bg-red-50 border border-red-200 text-red-700 text-xs flex items-center space-x-2">
              <svg class="w-4 h-4 text-red-500 flex-shrink-0" fill="currentColor" viewBox="0 0 20 20"><path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clip-rule="evenodd"/></svg>
              <span>{{ addStockError }}</span>
            </div>
          }

          @if (stockModalMode === 'replenish') {
            <!-- REPLENISH FORM -->
            <form [formGroup]="replenishStockForm" (ngSubmit)="submitReplenishStock()" class="space-y-4">
              <div>
                <label class="block text-xs font-bold uppercase tracking-wider text-slate-700 mb-1">Blood Group</label>
                <select formControlName="bloodGroup" class="w-full px-4 py-2.5 rounded-xl border border-slate-300 text-sm font-bold text-slate-900 bg-white">
                  @for (bg of bloodGroups; track bg) { <option [value]="bg">{{ bg }}</option> }
                </select>
              </div>
              <div>
                <label class="block text-xs font-bold uppercase tracking-wider text-slate-700 mb-1">Component Type</label>
                <select formControlName="componentType" class="w-full px-4 py-2.5 rounded-xl border border-slate-300 text-sm font-medium text-slate-900 bg-white">
                  @for (comp of componentTypes; track comp) { <option [value]="comp">{{ comp }}</option> }
                </select>
              </div>
              <div>
                <label class="block text-xs font-bold uppercase tracking-wider text-slate-700 mb-1">Quantity Units</label>
                <input type="number" min="1" max="10000" formControlName="quantityUnits" placeholder="e.g. 10" class="w-full px-4 py-2.5 rounded-xl border border-slate-300 text-sm font-bold text-slate-900"/>
              </div>
              <div class="flex items-center justify-end space-x-3 pt-3 border-t border-slate-100">
                <button type="button" (click)="showAddStockModal = false" class="px-4 py-2 text-xs font-bold text-slate-600 rounded-xl hover:bg-slate-100 transition-colors">Cancel</button>
                <button type="submit" [disabled]="isAddingStock || replenishStockForm.invalid" class="px-5 py-2.5 text-xs font-bold text-white bg-rose-600 hover:bg-rose-700 rounded-xl shadow-md disabled:opacity-50 flex items-center space-x-1.5 transition-all">
                  @if (isAddingStock) {
                    <svg class="animate-spin h-3.5 w-3.5 text-white" fill="none" viewBox="0 0 24 24"><circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle><path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path></svg>
                    <span>Adding to Depository...</span>
                  } @else {
                    <span>Add to Depository</span>
                  }
                </button>
              </div>
            </form>
          } @else {
            <!-- SINGLE BAG FORM -->
            <form [formGroup]="addStockForm" (ngSubmit)="submitAddStock()" class="space-y-4">
              <div>
                <label class="block text-xs font-bold uppercase text-slate-700 mb-1">Blood Bag Number</label>
                <input type="text" formControlName="bagNumber" placeholder="e.g. BAG-2026-0001" class="w-full px-3 py-2 rounded-xl border border-slate-300 text-sm font-bold"/>
              </div>
              <div>
                <label class="block text-xs font-bold uppercase text-slate-700 mb-1">Blood Group</label>
                <select formControlName="bloodGroup" class="w-full px-3 py-2 rounded-xl border border-slate-300 text-sm font-bold">
                  @for (bg of bloodGroups; track bg) { <option [value]="bg">{{ bg }}</option> }
                </select>
              </div>
              <div>
                <label class="block text-xs font-bold uppercase text-slate-700 mb-1">Component Type</label>
                <select formControlName="componentType" class="w-full px-3 py-2 rounded-xl border border-slate-300 text-sm">
                  @for (comp of componentTypes; track comp) { <option [value]="comp">{{ comp }}</option> }
                </select>
              </div>
              <div>
                <label class="block text-xs font-bold uppercase text-slate-700 mb-1">Date Added</label>
                <input type="date" formControlName="addedDate" class="w-full px-3 py-2 rounded-xl border border-slate-300 text-sm"/>
              </div>
              <div class="flex items-center justify-end space-x-2 pt-2">
                <button type="button" (click)="showAddStockModal = false" class="px-4 py-2 text-xs font-bold text-slate-600 rounded-xl hover:bg-slate-100">Cancel</button>
                <button type="submit" [disabled]="isAddingStock || addStockForm.invalid" class="px-5 py-2.5 text-xs font-bold text-white bg-rose-600 hover:bg-rose-700 rounded-xl shadow-md disabled:opacity-50 flex items-center space-x-1.5">
                  @if (isAddingStock) {
                    <svg class="animate-spin h-3.5 w-3.5 text-white" fill="none" viewBox="0 0 24 24"><circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle><path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path></svg>
                    <span>Adding Bag...</span>
                  } @else {
                    <span>Add Blood Bag</span>
                  }
                </button>
              </div>
            </form>
          }
        </div>
      </div>
    }

    <!-- CREATE CAMP MODAL (Blood Bank A only) -->
    @if (showCreateCampModal) {
      <div class="fixed inset-0 z-50 bg-slate-900/60 backdrop-blur-sm flex items-center justify-center p-4">
        <div class="bg-white rounded-2xl max-w-md w-full p-6 shadow-2xl space-y-4 border border-slate-100">
          <h3 class="text-lg font-bold text-slate-900">Schedule Donation Camp</h3>
          <form [formGroup]="createCampForm" (ngSubmit)="submitCreateCamp()" class="space-y-4">
            <div>
              <label class="block text-xs font-bold uppercase text-slate-700 mb-1">Camp Event Name</label>
              <input type="text" formControlName="campName" placeholder="e.g. City Plaza Blood Camp" class="w-full px-3 py-2 rounded-xl border border-slate-300 text-sm"/>
            </div>
            <div>
              <label class="block text-xs font-bold uppercase text-slate-700 mb-1">Location Address</label>
              <input type="text" formControlName="location" placeholder="e.g. City Plaza, Ground Floor" class="w-full px-3 py-2 rounded-xl border border-slate-300 text-sm"/>
            </div>
            <div class="grid grid-cols-2 gap-3">
              <div>
                <label class="block text-xs font-bold uppercase text-slate-700 mb-1">Date</label>
                <input type="date" formControlName="campDate" class="w-full px-3 py-2 rounded-xl border border-slate-300 text-sm"/>
              </div>
              <div>
                <label class="block text-xs font-bold uppercase text-slate-700 mb-1">Target Units</label>
                <input type="number" min="10" formControlName="targetUnits" class="w-full px-3 py-2 rounded-xl border border-slate-300 text-sm"/>
              </div>
            </div>
            <div class="flex items-center justify-end space-x-2 pt-2">
              <button type="button" (click)="showCreateCampModal = false" class="px-4 py-2 text-xs font-bold text-slate-600 rounded-xl hover:bg-slate-100">Cancel</button>
              <button type="submit" class="px-5 py-2 text-xs font-bold text-white bg-rose-600 hover:bg-rose-700 rounded-xl shadow-md">Create Camp</button>
            </div>
          </form>
        </div>
      </div>
    }
  `
})
export class DashboardComponent implements OnInit {
  public authService = inject(AuthService);
  private bloodBankService = inject(BloodBankService);
  private donorService = inject(DonorService);
  private hospitalService = inject(HospitalService);
  private fb = inject(FormBuilder);

  // Tabs
  public activeBankTab: 'stock' | 'camps' | 'donors' | 'requests' | 'issued' = 'stock';
  public activeHospitalTab: 'new-req' | 'my-req' | 'stock-view' = 'new-req';
  public activeDonorTab: 'request' | 'my-requests' | 'camps' = 'request';

  // Modal States
  public showAddStockModal = false;
  public showCreateCampModal = false;
  public stockModalMode: 'replenish' | 'bag' = 'replenish';
  public isAddingStock = false;
  public isClearing = false;
  public addStockError: string | null = null;
  public hospitalSubmitSuccess = false;
  public donorSubmitSuccess = false;

  // Domain Constants
  public bloodGroups: BloodGroup[] = ['A+', 'A-', 'B+', 'B-', 'AB+', 'AB-', 'O+', 'O-'];
  public componentTypes: ComponentType[] = [
    'Whole Blood', 
    'Red Blood Cells (PRBC)', 
    'Platelets', 
    'Fresh Frozen Plasma (FFP)', 
    'Cryoprecipitate'
  ];

  // Bank Signals
  public bankStocks = signal<BloodStock[]>([]);
  public selectedStockComponent = signal<ComponentType>('Whole Blood');
  public selectedBloodGroup = signal<BloodGroup | null>(null);
  public showIssuedComponents = signal(false);
  public selectedComponentStocks = computed<BloodStock[]>(() => {
    return this.bankStocks().filter(stock => stock.componentType === this.selectedStockComponent());
  });
  public bankBags = signal<BloodBag[]>([]);
  public selectedComponentBags = computed<BloodBag[]>(() => {
    return this.bankBags().filter(bag => bag.componentType === this.selectedStockComponent());
  });
  public selectedBloodGroupBags = computed<BloodBag[]>(() => {
    const bloodGroup = this.selectedBloodGroup();
    if (!bloodGroup) return [];
    return this.selectedComponentBags().filter(bag => bag.bloodGroup === bloodGroup);
  });
  public bankCamps = signal<DonationCamp[]>([]);
  public bankDonors = signal<DonorDetails[]>([]);
  public bankRequests = signal<BloodRequest[]>([]);
  public issuedRequests = computed<BloodRequest[]>(() => {
    return this.bankRequests().filter(request => request.status === 'Fulfilled');
  });
  public issuedUnitTotal = computed<number>(() => {
    return this.issuedRequests().reduce((total, request) => total + request.quantityUnits, 0);
  });
  public issuedComponentTypeCount = computed<number>(() => {
    return new Set(this.issuedRequests().map(request => request.componentType)).size;
  });

  // Hospital Signals
  public hospitalRequests = signal<BloodRequest[]>([]);
  public hospitalStockView = signal<BloodStock[]>([]);

  // Donor Signals
  public donorRequests = signal<BloodRequest[]>([]);
  public donorCamps = signal<DonationCamp[]>([]);

  public pendingRequestsCount = computed<number>(() => {
    return this.bankRequests().filter(r => r.status === 'Pending').length;
  });

  // Reactive Forms
  public hospitalForm: FormGroup = this.fb.group({
    patientName: ['', [Validators.required, Validators.minLength(2)]],
    hospitalDepartment: [''],
    bloodGroup: ['O-', [Validators.required]],
    componentType: ['Red Blood Cells (PRBC)', [Validators.required]],
    quantityUnits: [2, [Validators.required, Validators.min(1)]],
    urgency: ['Critical', [Validators.required]],
    medicalReason: ['']
  });

  public donorForm: FormGroup = this.fb.group({
    patientName: ['', [Validators.required, Validators.minLength(2)]],
    hospitalDepartment: [''],
    bloodGroup: ['O+', [Validators.required]],
    componentType: ['Whole Blood', [Validators.required]],
    quantityUnits: [1, [Validators.required, Validators.min(1)]],
    urgency: ['Urgent', [Validators.required]],
    medicalReason: ['']
  });

  public addStockForm: FormGroup = this.fb.group({
    bagNumber: ['', [Validators.required, Validators.maxLength(50)]],
    bloodGroup: ['O-', [Validators.required]],
    componentType: ['Whole Blood', [Validators.required]],
    addedDate: [new Date().toISOString().substring(0, 10), [Validators.required]]
  });

  public replenishStockForm: FormGroup = this.fb.group({
    bloodGroup: ['O-', [Validators.required]],
    componentType: ['Whole Blood', [Validators.required]],
    quantityUnits: [10, [Validators.required, Validators.min(1), Validators.max(10000)]]
  });

  public createCampForm: FormGroup = this.fb.group({
    campName: ['', [Validators.required]],
    location: ['', [Validators.required]],
    campDate: [new Date().toISOString().substring(0, 10), [Validators.required]],
    targetUnits: [50, [Validators.required, Validators.min(5)]]
  });

  public bloodBankRequestForm: FormGroup = this.fb.group({
    hospitalName: ['', [Validators.required, Validators.minLength(2)]],
    patientName: ['', [Validators.required, Validators.minLength(2)]],
    bloodGroup: ['O-', [Validators.required]],
    componentType: ['Whole Blood', [Validators.required]],
    quantityUnits: [1, [Validators.required, Validators.min(1)]],
    urgency: ['Normal', [Validators.required]],
    hospitalDepartment: [''],
    medicalReason: ['']
  });
  public isSubmittingBloodBankRequest = false;
  public bloodBankRequestError: string | null = null;

  ngOnInit(): void {
    this.refreshData();
  }

  public refreshData(): void {
    if (this.authService.isBloodBank()) {
      this.loadBankData();
    } else if (this.authService.isHospital()) {
      this.loadHospitalData();
    } else if (this.authService.isDonor()) {
      this.loadDonorData();
    }
  }

  public selectStockComponent(component: ComponentType): void {
    this.showIssuedComponents.set(false);
    this.selectedStockComponent.set(component);
    this.selectedBloodGroup.set(null);
  }

  public selectBloodGroup(bloodGroup: BloodGroup): void {
    this.selectedBloodGroup.set(bloodGroup);
  }

  public getBloodGroupBagCount(bloodGroup: BloodGroup): number {
    return this.bankBags().filter(bag =>
      bag.componentType === this.selectedStockComponent() && bag.bloodGroup === bloodGroup
    ).length;
  }

  public getBloodGroupUnitTotal(bloodGroup: BloodGroup): number {
    return this.bankStocks()
      .filter(stock =>
        stock.componentType === this.selectedStockComponent() && stock.bloodGroup === bloodGroup
      )
      .reduce((total, stock) => total + stock.quantityUnits, 0);
  }

  public getStockPeriod(addedDate: string): string {
    const added = new Date(addedDate);
    const today = new Date();
    added.setHours(0, 0, 0, 0);
    today.setHours(0, 0, 0, 0);
    const days = Math.max(0, Math.floor((today.getTime() - added.getTime()) / 86400000));
    return `${days} ${days === 1 ? 'day' : 'days'}`;
  }

  public selectIssuedComponents(): void {
    this.showIssuedComponents.set(true);
  }

  public getComponentShortName(component: ComponentType): string {
    if (component === 'Red Blood Cells (PRBC)') return 'RBC';
    if (component === 'Fresh Frozen Plasma (FFP)') return 'FFP';
    if (component === 'Whole Blood') return 'WB';
    if (component === 'Platelets') return 'PLT';
    return 'CRYO';
  }

  public getComponentUnitTotal(component: ComponentType): number {
    return this.bankStocks()
      .filter(stock => stock.componentType === component)
      .reduce((total, stock) => total + stock.quantityUnits, 0);
  }

  public getComponentGroupCount(component: ComponentType): number {
    return this.bankStocks().filter(stock => stock.componentType === component).length;
  }

  public openAddStockModal(component: ComponentType = this.selectedStockComponent()): void {
    this.showIssuedComponents.set(false);
    this.selectedStockComponent.set(component);
    this.stockModalMode = 'replenish';
    this.replenishStockForm.patchValue({ componentType: component, bloodGroup: 'O-', quantityUnits: 10 });
    this.addStockForm.patchValue({
      componentType: component,
      bloodGroup: 'O-',
      bagNumber: `BAG-${new Date().toISOString().substring(0, 10).replace(/-/g, '')}-${Math.floor(1000 + Math.random() * 9000)}`
    });
    this.addStockError = null;
    this.showAddStockModal = true;
  }

  public submitReplenishStock(): void {
    if (this.replenishStockForm.invalid) return;

    this.isAddingStock = true;
    this.addStockError = null;

    const val = this.replenishStockForm.value;
    const payload = {
      bloodGroup: val.bloodGroup,
      componentType: val.componentType,
      quantityUnits: Number(val.quantityUnits)
    };

    this.bloodBankService.replenishStock(payload).subscribe({
      next: () => {
        this.isAddingStock = false;
        this.showAddStockModal = false;
        this.loadBankData();
      },
      error: (err) => {
        this.isAddingStock = false;
        this.addStockError = err.error?.message || (err.status === 401 ? 'Session expired. Please log in again.' : 'Failed to replenish stock.');
        console.error('Replenish stock failed:', err);
      }
    });
  }

  public confirmClearAllData(): void {
    if (!confirm('Are you sure you want to clear all data? This will empty all stock levels, blood bags, camps, requests, and donation records so you can test adding everything from scratch.')) {
      return;
    }

    this.isClearing = true;
    this.bloodBankService.clearAllData().subscribe({
      next: () => {
        this.isClearing = false;
        alert('All operational data has been cleared. Depository is now completely empty!');
        this.loadBankData();
      },
      error: (err) => {
        this.isClearing = false;
        alert(err.error?.message || 'Failed to clear data.');
      }
    });
  }

  private loadBankData(): void {
    this.bloodBankService.getStock().subscribe({
      next: (stocks) => this.bankStocks.set(stocks),
      error: (err) => console.error('Failed to load stocks:', err)
    });
    this.bloodBankService.getBloodBags().subscribe({
      next: (bags) => this.bankBags.set(bags),
      error: (err) => console.error('Failed to load blood bags:', err)
    });
    this.bloodBankService.getCamps().subscribe({
      next: (camps) => this.bankCamps.set(camps),
      error: (err) => console.error('Failed to load camps:', err)
    });
    this.bloodBankService.getDonors().subscribe({
      next: (donors) => this.bankDonors.set(donors),
      error: (err) => console.error('Failed to load donors:', err)
    });
    this.bloodBankService.getRequests().subscribe({
      next: (reqs) => this.bankRequests.set(reqs),
      error: (err) => console.error('Failed to load requests:', err)
    });
  }

  private loadHospitalData(): void {
    this.hospitalService.getMyRequests().subscribe(reqs => this.hospitalRequests.set(reqs));
    this.hospitalService.getAvailableStockSummary().subscribe(stocks => this.hospitalStockView.set(stocks));
  }

  private loadDonorData(): void {
    this.donorService.getMyRequests().subscribe(reqs => this.donorRequests.set(reqs));
    this.donorService.getUpcomingCamps().subscribe(camps => this.donorCamps.set(camps));
  }

  public processBankRequest(id: string, status: 'Approved' | 'Fulfilled' | 'Rejected'): void {
    this.bloodBankService.processRequest(id, status).subscribe({
      next: () => this.loadBankData(),
      error: (err) => alert(err.error?.message || 'Failed to process request.')
    });
  }

  public submitAddStock(): void {
    if (this.addStockForm.invalid) return;

    this.isAddingStock = true;
    this.addStockError = null;

    const formVal = this.addStockForm.value;
    const payload = {
      bagNumber: formVal.bagNumber.trim(),
      bloodGroup: formVal.bloodGroup,
      componentType: formVal.componentType,
      addedDate: formVal.addedDate
    };

    this.bloodBankService.addStock(payload).subscribe({
      next: () => {
        this.isAddingStock = false;
        this.showAddStockModal = false;
        this.loadBankData();
      },
      error: (err) => {
        this.isAddingStock = false;
        this.addStockError = err.error?.message || (err.status === 401 ? 'Session expired. Please log in again.' : 'Failed to save stock. Please ensure the backend is running.');
        console.error('Add stock failed:', err);
      }
    });
  }

  public submitCreateCamp(): void {
    if (this.createCampForm.invalid) return;
    this.bloodBankService.createCamp(this.createCampForm.value).subscribe({
      next: () => {
        this.showCreateCampModal = false;
        this.loadBankData();
      }
    });
  }

  public submitBloodBankRequest(): void {
    if (this.bloodBankRequestForm.invalid) return;

    this.isSubmittingBloodBankRequest = true;
    this.bloodBankRequestError = null;
    this.bloodBankService.createRequest(this.bloodBankRequestForm.value).subscribe({
      next: () => {
        this.isSubmittingBloodBankRequest = false;
        this.bloodBankRequestForm.reset({
          bloodGroup: 'O-',
          componentType: 'Whole Blood',
          quantityUnits: 1,
          urgency: 'Normal'
        });
        this.loadBankData();
      },
      error: (err) => {
        this.isSubmittingBloodBankRequest = false;
        this.bloodBankRequestError = err.error?.message || 'Failed to create request.';
      }
    });
  }

  public submitHospitalRequest(): void {
    if (this.hospitalForm.invalid) return;
    this.hospitalService.submitRequisition(this.hospitalForm.value).subscribe({
      next: () => {
        this.hospitalSubmitSuccess = true;
        this.hospitalForm.reset({ bloodGroup: 'O-', componentType: 'Red Blood Cells (PRBC)', quantityUnits: 2, urgency: 'Critical' });
        this.loadHospitalData();
        setTimeout(() => this.hospitalSubmitSuccess = false, 5000);
      }
    });
  }

  public submitDonorRequest(): void {
    if (this.donorForm.invalid) return;
    this.donorService.submitRequest(this.donorForm.value).subscribe({
      next: () => {
        this.donorSubmitSuccess = true;
        this.donorForm.reset({ bloodGroup: 'O+', componentType: 'Whole Blood', quantityUnits: 1, urgency: 'Urgent' });
        this.loadDonorData();
        setTimeout(() => this.donorSubmitSuccess = false, 5000);
      }
    });
  }
}
