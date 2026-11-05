import { Component, OnInit, computed, effect, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from '../../core/services/auth.service';
import { BloodBankService } from '../../core/services/blood-bank.service';
import { DonorService } from '../../core/services/donor.service';
import { HospitalService } from '../../core/services/hospital.service';
import { NotificationService } from '../../core/services/notification.service';
import { BloodBag, BloodGroup, BloodRequest, BloodStock, ComponentCollection, ComponentDiscard, ComponentReturn, ComponentType, DonationCamp, DonationHistory, DonorDetails } from '../../core/models/blood-bank.model';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.css'
})
export class DashboardComponent implements OnInit {
  public authService = inject(AuthService);
  public notificationService = inject(NotificationService);
  private bloodBankService = inject(BloodBankService);
  private donorService = inject(DonorService);
  private hospitalService = inject(HospitalService);
  private fb = inject(FormBuilder);

  constructor() {
    effect(() => {
      const tabReq = this.notificationService.activeTabRequest();
      if (tabReq) {
        this.activeBankTab = tabReq as any;
        this.notificationService.activeTabRequest.set(null);
      }
    });
  }

  // Tabs
  public activeBankTab: 'stock' | 'camps' | 'donors' | 'requests' | 'issued' | 'summary' | 'collection' | 'discard' | 'return' = 'stock';
  public activeHospitalTab: 'new-req' | 'my-req' | 'stock-view' = 'new-req';
  public activeDonorTab: 'request' | 'my-requests' | 'camps' = 'request';

  // Modal States
  public showAddStockModal = false;
  public showCreateCampModal = false;
  public showCreateDonorModal = false;
  public showDonationModal = false;
  public donationStage: 'eligibility' | 'screening' | 'confirmation' = 'eligibility';
  public selectedDonor: DonorDetails | null = null;
  public selectedPaymentRequest: BloodRequest | null = null;
  public showPaymentModal = false;
  public showReceiptModal = signal<boolean>(false);
  public selectedReceiptRequest = signal<BloodRequest | null>(null);
  public showDonationHistoryModal = false;
  public donationHistory: DonationHistory[] = [];
  public donationHistoryDonor: DonorDetails | null = null;
  public generatedPassword: string | null = null;
  public donationActionError: string | null = null;
  public stockModalMode: 'replenish' | 'bag' = 'replenish';
  public isAddingStock = false;
  public isClearing = false;
  public addStockError: string | null = null;
  public hospitalSubmitSuccess = false;
  public donorSubmitSuccess = false;
  public paymentForm: FormGroup = this.fb.group({
    paymentMode: ['Cash', Validators.required],
    transactionId: [''],
    amountPaid: [1500, [Validators.required, Validators.min(0)]]
  });

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
    if (!bloodGroup) return this.selectedComponentBags();
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
  public completedPaymentCount = computed<number>(() => {
    return this.issuedRequests().filter(request => request.paymentStatus === 'Completed').length;
  });
  public issuedComponentTypeCount = computed<number>(() => {
    return new Set(this.issuedRequests().map(request => request.componentType)).size;
  });

  // Account Summary Signals & Calculation
  public summaryFilterMode = signal<'All' | 'Cash' | 'UPI'>('All');
  public summarySearchQuery = signal<string>('');

  public getComponentPrice(component: ComponentType): number {
    switch (component) {
      case 'Whole Blood': return 1500;
      case 'Red Blood Cells (PRBC)': return 1450;
      case 'Platelets': return 850;
      case 'Fresh Frozen Plasma (FFP)': return 600;
      case 'Cryoprecipitate': return 500;
      default: return 1000;
    }
  }

  public getRequestAmount(request: BloodRequest): number {
    return request.amountPaid ?? (this.getComponentPrice(request.componentType) * request.quantityUnits);
  }

  public totalRevenue = computed<number>(() => {
    return this.issuedRequests()
      .filter(r => r.paymentStatus === 'Completed')
      .reduce((sum, r) => sum + this.getRequestAmount(r), 0);
  });

  public cashRevenue = computed<number>(() => {
    return this.issuedRequests()
      .filter(r => r.paymentStatus === 'Completed' && r.paymentMode === 'Cash')
      .reduce((sum, r) => sum + this.getRequestAmount(r), 0);
  });

  public cashTransactionsCount = computed<number>(() => {
    return this.issuedRequests()
      .filter(r => r.paymentStatus === 'Completed' && r.paymentMode === 'Cash').length;
  });

  public upiRevenue = computed<number>(() => {
    return this.issuedRequests()
      .filter(r => r.paymentStatus === 'Completed' && r.paymentMode === 'UPI')
      .reduce((sum, r) => sum + this.getRequestAmount(r), 0);
  });

  public upiTransactionsCount = computed<number>(() => {
    return this.issuedRequests()
      .filter(r => r.paymentStatus === 'Completed' && r.paymentMode === 'UPI').length;
  });

  public filteredSummaryRequests = computed<BloodRequest[]>(() => {
    const mode = this.summaryFilterMode();
    const query = this.summarySearchQuery().trim().toLowerCase();
    return this.issuedRequests().filter(r => {
      const matchesMode = mode === 'All' || r.paymentMode === mode;
      const matchesQuery = !query || 
        r.patientName.toLowerCase().includes(query) ||
        (r.hospitalName && r.hospitalName.toLowerCase().includes(query)) ||
        (r.requesterName && r.requesterName.toLowerCase().includes(query)) ||
        (r.transactionId && r.transactionId.toLowerCase().includes(query)) ||
        (r.issuedBagNumbers && r.issuedBagNumbers.toLowerCase().includes(query)) ||
        r.bloodGroup.toLowerCase().includes(query) ||
        r.componentType.toLowerCase().includes(query);
      return matchesMode && matchesQuery;
    });
  });

  public componentCollections = signal<ComponentCollection[]>([]);
  public componentDiscards = signal<ComponentDiscard[]>([]);
  public componentReturns = signal<ComponentReturn[]>([]);

  private getDefaultExpiryDate(): string {
    const d = new Date();
    d.setDate(d.getDate() + 35);
    return d.toISOString().substring(0, 10);
  }

  public componentCollectionForm = this.fb.group({
    componentType: ['Whole Blood', Validators.required], bloodGroup: ['O-', Validators.required],
    bagType: ['Standard CPDA-1'], volumeMl: [450, [Validators.required, Validators.min(1)]], bagBarcode: ['', Validators.required],
    collectionDate: [new Date().toISOString().substring(0, 10), Validators.required],
    expiryDate: [this.getDefaultExpiryDate(), Validators.required]
  });
  public componentDiscardForm = this.fb.group({ bagBarcode: ['', Validators.required], reason: ['', Validators.required], discardedBy: ['', Validators.required] });
  public componentReturnForm = this.fb.group({ bagBarcode: ['', Validators.required], reason: ['', Validators.required], returnedBy: ['', Validators.required] });
  public componentError: string | null = null;

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

  public createDonorForm: FormGroup = this.fb.group({
    firstName: ['', [Validators.required, Validators.minLength(2)]],
    lastName: ['', [Validators.required, Validators.minLength(2)]],
    email: ['', [Validators.required, Validators.email]],
    phone: ['', [Validators.required]],
    dateOfBirth: ['', [Validators.required]],
    sex: ['', [Validators.required]],
    address: ['', [Validators.required]],
    bloodGroup: ['O+', [Validators.required]],
  });
  public donationForm: FormGroup = this.fb.group({
    donorConfirmed: [false],
    weightKg: [null],
    pulsePerMinute: [null],
    hemoglobin: [null],
    hematocrit: [null],
    plateletCount: [null],
    bloodPressure: [''],
    temperatureCelsius: [null],
    screeningStaffName: [''],
    screeningStaffSignature: [''],
    screeningDate: [new Date().toISOString().substring(0, 10)],
    screeningTime: [''],
    medicalExamAccepted: [false],
    deferralReason: [''],
    medicalOfficerName: [''],
    medicalOfficerSignature: [''],
    medicalOfficerDate: [new Date().toISOString().substring(0, 10)],
    donorNumber: [''],
    bagType: ['Whole Blood'],
    segmentNumber: [''],
    donationStartTime: [''],
    donationEndTime: [''],
    venipunctureSite: ['Right arm'],
    phlebotomistName: [''],
    phlebotomistSignature: [''],
    visualInspectionPassed: [false],
    feelsHealthyToday: [false, [Validators.requiredTrue]],
    donatedRecently: [false],
    hadDonationDiscomfort: [false],
    ateRecently: [false, [Validators.requiredTrue]],
    sleptWell: [false, [Validators.requiredTrue]],
    hasInfectionRisk: [false],
    hasColdFeverOrSinusitis: [false],
    tookAntibioticsRecently: [false],
    tookAlcoholRecently: [false],
    smokesFrequently: [false],
    tookMedicationRecently: [false],
    hadRecentVaccination: [false],
    hasRecentIllness: [false],
    hasRecentSurgeryOrTattoo: [false],
    hadRecentMalariaOrEndemicExposure: [false],
    hasChronicDisease: [false],
    isPregnantOrBreastfeeding: [false],
    hadRecentAbortion: [false],
    hasUnsafeSexualExposure: [false],
    hasHivOrHepatitisRisk: [false],
    hasPermanentDisqualification: [false],
    unitsDonated: [1, [Validators.required, Validators.min(1), Validators.max(10)]],
    campName: ['']
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
  public isCreatingDonor = false;
  public createDonorError: string | null = null;

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
    this.bloodBankService.getComponentCollections().subscribe(items => this.componentCollections.set(items));
    this.bloodBankService.getComponentDiscards().subscribe(items => this.componentDiscards.set(items));
    this.bloodBankService.getComponentReturns().subscribe(items => {
      this.componentReturns.set(items);
      this.notificationService.refresh();
    });
  }

  public createComponentCollection(): void {
    if (this.componentCollectionForm.invalid) return;
    this.componentError = null;
    const value = this.componentCollectionForm.getRawValue();
    const payload = {
      componentType: value.componentType ?? 'Whole Blood',
      bloodGroup: value.bloodGroup ?? 'O-',
      bagType: value.bagType ?? '',
      volumeMl: Number(value.volumeMl ?? 0),
      bagBarcode: value.bagBarcode ?? '',
      collectionDate: value.collectionDate ?? '',
      expiryDate: value.expiryDate ?? ''
    };
    this.bloodBankService.createComponentCollection(payload).subscribe({
      next: () => { this.componentCollectionForm.reset({ componentType: 'Whole Blood', bloodGroup: 'O-', volumeMl: 450, collectionDate: new Date().toISOString().substring(0, 10), expiryDate: this.getDefaultExpiryDate() }); this.loadBankData(); },
      error: err => this.componentError = err.error?.message || 'Unable to save component collection.'
    });
  }
  public createComponentDiscard(): void {
    if (this.componentDiscardForm.invalid) return;
    const value = this.componentDiscardForm.getRawValue();
    this.bloodBankService.createComponentDiscard({ bagBarcode: value.bagBarcode ?? '', reason: value.reason ?? '', discardedBy: value.discardedBy ?? '' }).subscribe({
      next: () => { this.componentDiscardForm.reset(); this.loadBankData(); },
      error: err => this.componentError = err.error?.message || 'Unable to submit discard request.'
    });
  }
  public createComponentReturn(): void {
    if (this.componentReturnForm.invalid) return;
    const value = this.componentReturnForm.getRawValue();
    this.bloodBankService.createComponentReturn({ bagBarcode: value.bagBarcode ?? '', reason: value.reason ?? '', returnedBy: value.returnedBy ?? '' }).subscribe({
      next: () => { this.componentReturnForm.reset(); this.loadBankData(); },
      error: err => this.componentError = err.error?.message || 'Unable to record component return.'
    });
  }
  public reviewComponentDiscard(id: string, approve: boolean): void {
    this.bloodBankService.reviewComponentDiscard(id, approve).subscribe({ next: () => this.loadBankData(), error: err => this.componentError = err.error?.message || 'Unable to review discard request.' });
  }
  public deleteComponentCollection(id: string): void {
    if (confirm('Delete this available component?')) this.bloodBankService.deleteComponentCollection(id).subscribe({ next: () => this.loadBankData(), error: err => this.componentError = err.error?.message || 'Unable to delete collection.' });
  }
  public deleteComponentDiscard(id: string): void {
    if (confirm('Delete this discard record?')) this.bloodBankService.deleteComponentDiscard(id).subscribe({ next: () => this.loadBankData(), error: err => this.componentError = err.error?.message || 'Unable to delete discard record.' });
  }
  public deleteComponentReturn(id: string): void {
    if (confirm('Delete this return record?')) this.bloodBankService.deleteComponentReturn(id).subscribe({ next: () => this.loadBankData(), error: err => this.componentError = err.error?.message || 'Unable to delete return record.' });
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
    if (status === 'Fulfilled') {
      const request = this.bankRequests().find(item => item.id === id);
      if (request) {
        this.selectedPaymentRequest = request;
        const autoAmount = this.getRequestAmount(request);
        this.paymentForm.reset({ paymentMode: 'Cash', transactionId: '', amountPaid: autoAmount });
        this.showPaymentModal = true;
      }
      return;
    }
    this.bloodBankService.processRequest(id, status).subscribe({
      next: () => this.loadBankData(),
      error: (err) => alert(err.error?.message || 'Failed to process request.')
    });
  }

  public submitPaymentAndFulfill(): void {
    if (!this.selectedPaymentRequest) return;
    const paymentMode = this.paymentForm.get('paymentMode')?.value as 'Cash' | 'UPI';
    const transactionId = String(this.paymentForm.get('transactionId')?.value || '').trim();
    const amountPaid = Number(this.paymentForm.get('amountPaid')?.value || this.getRequestAmount(this.selectedPaymentRequest));

    if (paymentMode === 'UPI' && !transactionId) {
      this.paymentForm.get('transactionId')?.setErrors({ required: true });
      return;
    }

    const fulfilledRequest = { ...this.selectedPaymentRequest, paymentMode, transactionId, amountPaid, paymentStatus: 'Completed' as const };

    this.bloodBankService.processRequest(this.selectedPaymentRequest.id, 'Fulfilled', undefined, paymentMode, transactionId || undefined, amountPaid).subscribe({
      next: (updated) => {
        this.showPaymentModal = false;
        this.selectedPaymentRequest = null;
        this.loadBankData();
        // Immediately present the official invoice/receipt
        this.openReceiptModal(updated || fulfilledRequest);
      },
      error: (err) => alert(err.error?.message || 'Payment or component issue could not be completed.')
    });
  }

  public openReceiptModal(request: BloodRequest): void {
    this.selectedReceiptRequest.set(request);
    this.showReceiptModal.set(true);
  }

  public closeReceiptModal(): void {
    this.showReceiptModal.set(false);
    this.selectedReceiptRequest.set(null);
  }

  public printReceipt(): void {
    window.print();
  }

  public openDonationHistory(donor: DonorDetails): void {
    this.donationHistoryDonor = donor;
    this.donationHistory = [];
    this.showDonationHistoryModal = true;
    this.bloodBankService.getDonationHistory(donor.id).subscribe({
      next: history => this.donationHistory = history,
      error: () => alert('Donation history could not be loaded.')
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

  public submitCreateDonor(): void {
    if (this.createDonorForm.invalid) return;

    this.isCreatingDonor = true;
    this.createDonorError = null;
    this.bloodBankService.createDonor(this.createDonorForm.value).subscribe({
      next: (result) => {
        this.isCreatingDonor = false;
        this.showCreateDonorModal = false;
        this.generatedPassword = result.generatedPassword;
        this.createDonorForm.reset({ bloodGroup: 'O+' });
        this.loadBankData();
      },
      error: (err) => {
        this.isCreatingDonor = false;
        this.createDonorError = err.error?.message || 'Failed to create donor details.';
      }
    });
  }

  public openDonationForm(donor: DonorDetails): void {
    if (!donor.isEligibleToDonate) return;
    this.selectedDonor = donor;
    this.donationStage = 'eligibility';
    this.donationActionError = null;
    this.donationForm.reset({
      feelsHealthyToday: false,
      donatedRecently: false,
      hadDonationDiscomfort: false,
      ateRecently: false,
      sleptWell: false,
      hasInfectionRisk: false,
      hasColdFeverOrSinusitis: false,
      tookAntibioticsRecently: false,
      tookAlcoholRecently: false,
      smokesFrequently: false,
      tookMedicationRecently: false,
      hadRecentVaccination: false,
      hasRecentIllness: false,
      hasRecentSurgeryOrTattoo: false,
      hadRecentMalariaOrEndemicExposure: false,
      hasChronicDisease: false,
      isPregnantOrBreastfeeding: false,
      hadRecentAbortion: false,
      hasUnsafeSexualExposure: false,
      hasHivOrHepatitisRisk: false,
      hasPermanentDisqualification: false,
      donorConfirmed: false,
      weightKg: null,
      pulsePerMinute: null,
      hemoglobin: null,
      hematocrit: null,
      plateletCount: null,
      bloodPressure: '',
      temperatureCelsius: null,
      screeningStaffName: '',
      screeningStaffSignature: '',
      screeningDate: new Date().toISOString().substring(0, 10),
      screeningTime: '',
      medicalExamAccepted: false,
      deferralReason: '',
      medicalOfficerName: '',
      medicalOfficerSignature: '',
      medicalOfficerDate: new Date().toISOString().substring(0, 10),
      donorNumber: '',
      bagType: 'Whole Blood',
      segmentNumber: '',
      donationStartTime: '',
      donationEndTime: '',
      venipunctureSite: 'Right arm',
      phlebotomistName: '',
      phlebotomistSignature: '',
      visualInspectionPassed: false,
      unitsDonated: 1,
      campName: ''
    });
    this.showDonationModal = true;
  }

  public completeDonation(): void {
    if (!this.selectedDonor || this.donationForm.invalid) return;
    this.bloodBankService.completeDonation(this.selectedDonor.id, this.donationForm.value).subscribe({
      next: (result) => {
        if (!result.isEligible) {
          this.donationActionError = result.message || 'Donation could not be completed.';
          return;
        }

        this.donationActionError = null;
        this.showDonationModal = false;
        this.selectedDonor = null;
        this.loadBankData();
      },
      error: (err) => {
        this.donationActionError = err.error?.message || 'Donation could not be completed.';
      }
    });
  }

  public proceedToScreening(): void {
    if (this.donationForm.invalid) return;
    this.donationStage = 'screening';
  }

  public proceedToConfirmation(): void {
    if (!this.donationForm.get('medicalExamAccepted')?.value) {
      this.donationActionError = 'Blood-bank staff must mark the medical examination as accepted before proceeding.';
      return;
    }
    this.donationActionError = null;
    this.donationStage = 'confirmation';
  }

  public confirmAndRecordDonation(): void {
    if (!this.donationForm.get('donorConfirmed')?.value) {
      this.donationActionError = 'Please record the donor confirmation before completing the donation.';
      return;
    }
    if (!this.donationForm.get('medicalExamAccepted')?.value) {
      this.donationActionError = 'Medical examination acceptance is required before completing the donation.';
      return;
    }
    this.completeDonation();
  }

  public printDonationForm(): void {
    window.print();
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
