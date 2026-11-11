import { Component, OnInit, computed, effect, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from '../../core/services/auth.service';
import { BloodBankService } from '../../core/services/blood-bank.service';
import { DonorService } from '../../core/services/donor.service';
import { HospitalService } from '../../core/services/hospital.service';
import { NotificationService } from '../../core/services/notification.service';
import {
  BloodBag, BloodGroup, BloodRequest, BloodStock, ComponentCollection, ComponentDiscard,
  ComponentReturn, ComponentType, DonationCamp, DonationHistory, DonorDetails,
  TtiScreeningRecord, CreateTtiScreeningPayload,
  PatientGroupingRecord, CreatePatientGroupingPayload,
  BloodBagStockRegister, CreateBloodBagStockRegisterPayload,
  DiagnosticKitRegister, CreateDiagnosticKitRegisterPayload,
  ApheresisRecord, CreateApheresisRecordPayload,
  MasterRecordItem
} from '../../core/models/blood-bank.model';

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
  public activeBankTab: 'stock' | 'camps' | 'donors' | 'requests' | 'issued' | 'summary' | 'collection' | 'discard' | 'return' | 'tti' | 'grouping' | 'bag-stock' | 'diagnostic-kits' | 'apheresis' | 'master-record' = 'stock';
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
  public requestFilterStatus = signal<'Active' | 'Fulfilled' | 'All'>('Active');
  public filteredBankRequests = computed<BloodRequest[]>(() => {
    const filter = this.requestFilterStatus();
    if (filter === 'Active') {
      return this.bankRequests().filter(r => r.status === 'Pending' || r.status === 'Approved');
    }
    if (filter === 'Fulfilled') {
      return this.bankRequests().filter(r => r.status === 'Fulfilled');
    }
    return this.bankRequests();
  });
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
  public collectionSuccessMessage: string | null = null;

  public selectedComponentCollections = computed<ComponentCollection[]>(() => {
    const group = this.selectedBloodGroup();
    const comp = this.selectedStockComponent();
    return this.componentCollections().filter(c => {
      const matchComp = c.componentType === comp;
      const matchGroup = !group || c.bloodGroup === group;
      return matchComp && matchGroup;
    });
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

  // Edit action states
  public editingCamp: DonationCamp | null = null;
  public editingDonor: DonorDetails | null = null;
  public editingRequest: BloodRequest | null = null;
  public showEditRequestModal = false;
  public editingCollection: ComponentCollection | null = null;
  public showEditCollectionModal = false;
  public editingDiscard: ComponentDiscard | null = null;
  public showEditDiscardModal = false;
  public editingReturn: ComponentReturn | null = null;
  public showEditReturnModal = false;
  public editingStock: BloodStock | null = null;
  public showEditStockModal = false;

  public editRequestForm: FormGroup = this.fb.group({
    hospitalName: ['', [Validators.required]],
    patientName: ['', [Validators.required]],
    bloodGroup: ['O-', [Validators.required]],
    componentType: ['Whole Blood', [Validators.required]],
    quantityUnits: [1, [Validators.required, Validators.min(1)]],
    urgency: ['Normal', [Validators.required]],
    hospitalDepartment: [''],
    medicalReason: [''],
    status: ['Pending']
  });

  public editCollectionForm = this.fb.group({
    componentType: ['Whole Blood', Validators.required],
    bloodGroup: ['O-', Validators.required],
    bagType: ['Single (350ml)', Validators.required],
    volumeMl: [350, [Validators.required, Validators.min(1)]],
    bagBarcode: ['', Validators.required],
    collectionDate: ['', Validators.required],
    expiryDate: ['', Validators.required]
  });

  public editDiscardForm = this.fb.group({
    bagBarcode: ['', Validators.required],
    reason: ['', Validators.required],
    discardedBy: ['', Validators.required],
    approvalStatus: ['Pending']
  });

  public editReturnForm = this.fb.group({
    bagBarcode: ['', Validators.required],
    reason: ['', Validators.required],
    returnedBy: ['', Validators.required]
  });

  public editStockForm = this.fb.group({
    quantityUnits: [0, [Validators.required, Validators.min(0)]]
  });

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
    const availableCollections = this.componentCollections()
      .filter(c => c.componentType === this.selectedStockComponent() && c.bloodGroup === bloodGroup && c.status === 'Available')
      .length;
    const availableBags = this.bankBags().filter(bag =>
      bag.componentType === this.selectedStockComponent() && bag.bloodGroup === bloodGroup && !bag.isIssued
    ).length;
    const stockUnits = this.getBloodGroupUnitTotal(bloodGroup);
    return Math.max(availableCollections, availableBags, stockUnits);
  }

  public getBloodGroupUnitTotal(bloodGroup: BloodGroup): number {
    const stock = this.bankStocks().find(
      s => s.componentType === this.selectedStockComponent() && s.bloodGroup === bloodGroup
    );
    if (stock) {
      return stock.quantityUnits;
    }
    return this.componentCollections()
      .filter(c => c.componentType === this.selectedStockComponent() && c.bloodGroup === bloodGroup && c.status === 'Available')
      .length;
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
    const stocks = this.bankStocks().filter(s => s.componentType === component);
    if (stocks.length > 0) {
      return stocks.reduce((total, stock) => total + stock.quantityUnits, 0);
    }
    return this.componentCollections()
      .filter(c => c.componentType === component && c.status === 'Available')
      .length;
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

  public seedSampleData(): void {
    this.isClearing = true;
    this.bloodBankService.seedLiveData().subscribe({
      next: () => {
        this.isClearing = false;
        alert('Live example data has been loaded directly into the database!');
        this.loadBankData();
      },
      error: (err) => {
        this.isClearing = false;
        alert(err.error?.message || 'Failed to seed data.');
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
        this.bloodBankService.getTtiScreenings().subscribe(items => this.ttiRecords.set(items));
    this.bloodBankService.getPatientGroupings().subscribe(items => this.patientGroupingRecords.set(items));
    this.bloodBankService.getBloodBagStockRegisters().subscribe(items => this.bloodBagStockRecords.set(items));
    this.bloodBankService.getDiagnosticKitRegisters().subscribe(items => this.diagnosticKitRecords.set(items));
    this.bloodBankService.getApheresisRecords().subscribe(items => this.apheresisRecords.set(items));
    this.bloodBankService.getMasterRecords().subscribe(items => this.masterRecords.set(items));
    this.bloodBankService.getComponentReturns().subscribe(items => {
      this.componentReturns.set(items);
      this.notificationService.refresh();
    });
  }

  
  public generateCollectionBarcode(componentType: string = 'Whole Blood', bloodGroup: string = 'O-'): string {
    const prefix = componentType.includes('Whole') ? 'WB' :
                   componentType.includes('PRBC') ? 'PRBC' :
                   componentType.includes('Platelet') ? 'PLT' :
                   componentType.includes('Plasma') ? 'FFP' : 'CRYO';
    const cleanGroup = bloodGroup.replace('+', 'P').replace('-', 'N');
    const randomSuffix = Math.floor(1000 + Math.random() * 9000);
    return `BAG-2026-${prefix}-${cleanGroup}-${randomSuffix}`;
  }

  public onCollectionComponentTypeChange(): void {
    const comp = this.componentCollectionForm.get('componentType')?.value || 'Whole Blood';
    let defaultVol = 450;
    let defaultBag = 'Triple Bag (450ml)';
    let daysToAdd = 35;

    if (comp === 'Platelets') {
      defaultVol = 250;
      defaultBag = 'Single Bag (250ml)';
      daysToAdd = 5;
    } else if (comp === 'Red Blood Cells (PRBC)') {
      defaultVol = 300;
      defaultBag = 'Quadruple Bag (350ml)';
      daysToAdd = 42;
    } else if (comp === 'Fresh Frozen Plasma (FFP)') {
      defaultVol = 200;
      defaultBag = 'Double Bag (200ml)';
      daysToAdd = 365;
    } else if (comp === 'Cryoprecipitate') {
      defaultVol = 50;
      defaultBag = 'Single Cryo Bag (50ml)';
      daysToAdd = 365;
    }

    const expDate = new Date();
    expDate.setDate(expDate.getDate() + daysToAdd);

    const currentGroup = this.componentCollectionForm.get('bloodGroup')?.value || 'O-';
    this.componentCollectionForm.patchValue({
      volumeMl: defaultVol,
      bagType: defaultBag,
      expiryDate: expDate.toISOString().substring(0, 10),
      bagBarcode: this.generateCollectionBarcode(comp, currentGroup)
    });
  }

  public autoGenerateBarcode(): void {
    const comp = this.componentCollectionForm.get('componentType')?.value || 'Whole Blood';
    const group = this.componentCollectionForm.get('bloodGroup')?.value || 'O-';
    this.componentCollectionForm.patchValue({
      bagBarcode: this.generateCollectionBarcode(comp, group)
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
      next: () => {
        this.collectionSuccessMessage = `Component bag ${payload.bagBarcode} registered successfully and added to Stock Overview!`;
        setTimeout(() => this.collectionSuccessMessage = null, 6000);
        this.componentCollectionForm.reset({
          componentType: 'Whole Blood',
          bloodGroup: 'O-',
          bagType: 'Triple Bag (450ml)',
          volumeMl: 450,
          bagBarcode: this.generateCollectionBarcode('Whole Blood', 'O-'),
          collectionDate: new Date().toISOString().substring(0, 10),
          expiryDate: this.getDefaultExpiryDate()
        });
        this.loadBankData();
      },
      
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
    public deleteCamp(id: string): void {
    if (confirm('Are you sure you want to delete this donation camp?')) {
      this.bloodBankService.deleteCamp(id).subscribe({
        next: () => this.loadBankData(),
        error: err => this.componentError = err.error?.message || 'Unable to delete donation camp.'
      });
    }
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

  public openCreateCampModal(): void {
    this.editingCamp = null;
    this.createCampForm.reset({
      campName: '',
      location: '',
      campDate: new Date().toISOString().substring(0, 10),
      targetUnits: 50
    });
    this.showCreateCampModal = true;
  }

  public openEditCampModal(camp: DonationCamp): void {
    this.editingCamp = camp;
    this.createCampForm.patchValue({
      campName: camp.campName,
      location: camp.location,
      campDate: camp.campDate ? new Date(camp.campDate).toISOString().substring(0, 10) : new Date().toISOString().substring(0, 10),
      targetUnits: camp.targetUnits,
      contactPhone: camp.contactPhone || '',
      notes: camp.notes || ''
    });
    this.showCreateCampModal = true;
  }

  public openCreateDonorModal(): void {
    this.editingDonor = null;
    this.generatedPassword = null;
    this.createDonorError = null;
    this.createDonorForm.reset({ bloodGroup: 'O+' });
    this.showCreateDonorModal = true;
  }

  public openEditDonorModal(donor: DonorDetails): void {
    this.editingDonor = donor;
    this.generatedPassword = null;
    this.createDonorError = null;
    const parts = (donor.fullName || '').trim().split(' ');
    const firstName = parts[0] || '';
    const lastName = parts.slice(1).join(' ') || parts[0] || '';
    this.createDonorForm.patchValue({
      firstName,
      lastName,
      email: donor.email,
      phone: donor.phone || '',
      dateOfBirth: donor.dateOfBirth ? new Date(donor.dateOfBirth).toISOString().substring(0, 10) : '',
      sex: donor.sex || 'Male',
      address: donor.address || '',
      bloodGroup: donor.bloodGroup || 'O+'
    });
    this.showCreateDonorModal = true;
  }

  public openEditRequestModal(req: BloodRequest): void {
    this.editingRequest = req;
    this.editRequestForm.patchValue({
      hospitalName: req.hospitalName || '',
      patientName: req.patientName || '',
      bloodGroup: req.bloodGroup || 'O-',
      componentType: req.componentType || 'Whole Blood',
      quantityUnits: req.quantityUnits || 1,
      urgency: req.urgency || 'Normal',
      hospitalDepartment: req.hospitalDepartment || '',
      medicalReason: req.medicalReason || '',
      status: req.status || 'Pending'
    });
    this.showEditRequestModal = true;
  }

  public submitEditRequest(): void {
    if (!this.editingRequest || this.editRequestForm.invalid) return;
    this.bloodBankService.updateRequest(this.editingRequest.id, this.editRequestForm.value).subscribe({
      next: () => {
        this.showEditRequestModal = false;
        this.editingRequest = null;
        this.loadBankData();
      }
    });
  }

  public openEditCollectionModal(item: ComponentCollection): void {
    this.editingCollection = item;
    this.editCollectionForm.patchValue({
      componentType: item.componentType,
      bloodGroup: item.bloodGroup,
      bagType: item.bagType,
      volumeMl: item.volumeMl,
      bagBarcode: item.bagBarcode,
      collectionDate: item.collectionDate ? new Date(item.collectionDate).toISOString().substring(0, 10) : '',
      expiryDate: item.expiryDate ? new Date(item.expiryDate).toISOString().substring(0, 10) : ''
    });
    this.showEditCollectionModal = true;
  }

  public submitEditCollection(): void {
    if (!this.editingCollection || this.editCollectionForm.invalid) return;
    this.bloodBankService.updateComponentCollection(this.editingCollection.id, this.editCollectionForm.value as any).subscribe({
      next: () => {
        this.showEditCollectionModal = false;
        this.editingCollection = null;
        this.loadBankData();
      }
    });
  }

  public openEditDiscardModal(item: ComponentDiscard): void {
    this.editingDiscard = item;
    this.editDiscardForm.patchValue({
      bagBarcode: item.bagBarcode,
      reason: item.reason,
      discardedBy: item.discardedBy,
      approvalStatus: item.approvalStatus
    });
    this.showEditDiscardModal = true;
  }

  public submitEditDiscard(): void {
    if (!this.editingDiscard || this.editDiscardForm.invalid) return;
    this.bloodBankService.updateComponentDiscard(this.editingDiscard.id, this.editDiscardForm.value as any).subscribe({
      next: () => {
        this.showEditDiscardModal = false;
        this.editingDiscard = null;
        this.loadBankData();
      }
    });
  }

  public openEditReturnModal(item: ComponentReturn): void {
    this.editingReturn = item;
    this.editReturnForm.patchValue({
      bagBarcode: item.bagBarcode,
      reason: item.reason,
      returnedBy: item.returnedBy
    });
    this.showEditReturnModal = true;
  }

  public submitEditReturn(): void {
    if (!this.editingReturn || this.editReturnForm.invalid) return;
    this.bloodBankService.updateComponentReturn(this.editingReturn.id, this.editReturnForm.value as any).subscribe({
      next: () => {
        this.showEditReturnModal = false;
        this.editingReturn = null;
        this.loadBankData();
      }
    });
  }

  public openEditStockModal(stock: BloodStock): void {
    this.editingStock = stock;
    this.editStockForm.patchValue({
      quantityUnits: stock.quantityUnits
    });
    this.showEditStockModal = true;
  }

  public submitEditStock(): void {
    if (!this.editingStock || this.editStockForm.invalid) return;
    this.bloodBankService.updateStock(this.editingStock.id, { quantityUnits: Number(this.editStockForm.value.quantityUnits) }).subscribe({
      next: () => {
        this.showEditStockModal = false;
        this.editingStock = null;
        this.loadBankData();
      }
    });
  }


  // ================= Uddanam Clinical Registers State & Signals =================
  public ttiRecords = signal<TtiScreeningRecord[]>([]);
  public ttiSearchQuery = signal<string>('');
  public filteredTtiRecords = computed<TtiScreeningRecord[]>(() => {
    const q = this.ttiSearchQuery().trim().toLowerCase();
    if (!q) return this.ttiRecords();
    return this.ttiRecords().filter(r =>
      r.bagBarcode.toLowerCase().includes(q) ||
      r.bloodGroup.toLowerCase().includes(q) ||
      r.overallStatus.toLowerCase().includes(q) ||
      r.testedBy.toLowerCase().includes(q)
    );
  });

  public patientGroupingRecords = signal<PatientGroupingRecord[]>([]);
  public groupingSearchQuery = signal<string>('');
  public filteredPatientGroupings = computed<PatientGroupingRecord[]>(() => {
    const q = this.groupingSearchQuery().trim().toLowerCase();
    if (!q) return this.patientGroupingRecords();
    return this.patientGroupingRecords().filter(g =>
      g.patientName.toLowerCase().includes(q) ||
      g.bloodGroup.toLowerCase().includes(q) ||
      (g.bagNumber && g.bagNumber.toLowerCase().includes(q)) ||
      (g.hospitalName && g.hospitalName.toLowerCase().includes(q))
    );
  });

  public bloodBagStockRecords = signal<BloodBagStockRegister[]>([]);
  public bagStockSearchQuery = signal<string>('');
  public filteredBagStocks = computed<BloodBagStockRegister[]>(() => {
    const q = this.bagStockSearchQuery().trim().toLowerCase();
    if (!q) return this.bloodBagStockRecords();
    return this.bloodBagStockRecords().filter(b =>
      b.supplierName.toLowerCase().includes(q) ||
      b.batchNo.toLowerCase().includes(q) ||
      b.invoiceNo.toLowerCase().includes(q) ||
      b.codeSpecification.toLowerCase().includes(q)
    );
  });

  public diagnosticKitRecords = signal<DiagnosticKitRegister[]>([]);
  public diagnosticKitSearchQuery = signal<string>('');
  public filteredDiagnosticKits = computed<DiagnosticKitRegister[]>(() => {
    const q = this.diagnosticKitSearchQuery().trim().toLowerCase();
    if (!q) return this.diagnosticKitRecords();
    return this.diagnosticKitRecords().filter(k =>
      k.kitName.toLowerCase().includes(q) ||
      k.supplierName.toLowerCase().includes(q) ||
      k.batchNo.toLowerCase().includes(q)
    );
  });

  public apheresisRecords = signal<ApheresisRecord[]>([]);
  public apheresisSearchQuery = signal<string>('');
  public filteredApheresisRecords = computed<ApheresisRecord[]>(() => {
    const q = this.apheresisSearchQuery().trim().toLowerCase();
    if (!q) return this.apheresisRecords();
    return this.apheresisRecords().filter(a =>
      a.donorName.toLowerCase().includes(q) ||
      a.registrationNo.toLowerCase().includes(q) ||
      (a.patientName && a.patientName.toLowerCase().includes(q)) ||
      a.bloodGroup.toLowerCase().includes(q)
    );
  });

  public masterRecords = signal<MasterRecordItem[]>([]);
  public masterRecordSearchQuery = signal<string>('');
  public filteredMasterRecords = computed<MasterRecordItem[]>(() => {
    const q = this.masterRecordSearchQuery().trim().toLowerCase();
    if (!q) return this.masterRecords();
    return this.masterRecords().filter(m =>
      m.bagNumber.toLowerCase().includes(q) ||
      m.donorName.toLowerCase().includes(q) ||
      m.bloodGroup.toLowerCase().includes(q) ||
      m.componentType.toLowerCase().includes(q) ||
      m.status.toLowerCase().includes(q) ||
      m.ttiOverallStatus.toLowerCase().includes(q)
    );
  });

  // Modal & Form States
  public showTtiModal = false;
  public editingTti: TtiScreeningRecord | null = null;
  public ttiForm = this.fb.group({
    bagBarcode: ['', Validators.required],
    bloodGroup: ['O-', Validators.required],
    testingDate: [new Date().toISOString().substring(0, 10)],
    hivResult: ['Non-Reactive', Validators.required],
    hbsAgResult: ['Non-Reactive', Validators.required],
    hcvResult: ['Non-Reactive', Validators.required],
    vdrlResult: ['Non-Reactive', Validators.required],
    malariaResult: ['Negative', Validators.required],
    atypicalAntibodies: ['Negative', Validators.required],
    overallStatus: ['Passed', Validators.required],
    testedBy: ['Lab Technician', Validators.required],
    approvedBy: ['Dr. Uddanam, M.O.'],
    remarks: ['']
  });

  public showGroupingModal = false;
  public editingGrouping: PatientGroupingRecord | null = null;
  public groupingForm = this.fb.group({
    slNo: [1],
    date: [new Date().toISOString().substring(0, 10)],
    patientName: ['', Validators.required],
    ageSex: ['35 / M'],
    hospitalName: [''],
    ipNumber: [''],
    bagNumber: [''],
    bagType: ['Single (350ml)'],
    bloodGroup: ['O', Validators.required],
    rhType: ['Positive', Validators.required],
    antiA: ['-'],
    antiB: ['-'],
    antiAb: ['-'],
    antiD: ['+'],
    lectinA1: ['-'],
    lectinH: ['+'],
    aCells: ['+'],
    bCells: ['+'],
    oCells: ['-'],
    duTest: ['Negative'],
    irregularAntibodies: ['Negative'],
    autoAntibodies: ['Negative'],
    crossmatchSaline: ['Compatible'],
    crossmatchAlbumin: ['Compatible'],
    crossmatchAhg: ['Compatible'],
    technicianSignature: ['Lab Tech'],
    medicalOfficerSignature: ['M.O.']
  });

  public showBagStockModal = false;
  public editingBagStock: BloodBagStockRegister | null = null;
  public bagStockForm = this.fb.group({
    slNo: [1],
    date: [new Date().toISOString().substring(0, 10)],
    supplierName: ['', Validators.required],
    invoiceNo: ['', Validators.required],
    invoiceDate: [new Date().toISOString().substring(0, 10)],
    manufacturer: ['', Validators.required],
    batchNo: ['', Validators.required],
    dateOfSupply: [new Date().toISOString().substring(0, 10)],
    dateOfManufacture: [new Date().toISOString().substring(0, 10)],
    dateOfExpiry: [new Date().toISOString().substring(0, 10)],
    codeSpecification: ['CPDA-1 Triple (450ml)', Validators.required],
    resultCoa: ['Satisfactory', Validators.required],
    openingStock: [0, [Validators.required, Validators.min(0)]],
    quantityReceived: [0, [Validators.required, Validators.min(0)]],
    quantityUsed: [0, [Validators.required, Validators.min(0)]],
    wastage: [0, [Validators.required, Validators.min(0)]],
    balance: [0, [Validators.required, Validators.min(0)]],
    techSign: ['Store Incharge'],
    medicalOfficerSign: ['M.O.'],
    remarks: ['']
  });

  public showDiagnosticKitModal = false;
  public editingDiagnosticKit: DiagnosticKitRegister | null = null;
  public diagnosticKitForm = this.fb.group({
    slNo: [1],
    date: [new Date().toISOString().substring(0, 10)],
    supplierName: ['', Validators.required],
    kitName: ['', Validators.required],
    manufacturer: ['', Validators.required],
    invoiceNo: ['', Validators.required],
    batchNo: ['', Validators.required],
    dateOfManufacture: [new Date().toISOString().substring(0, 10)],
    dateOfExpiry: [new Date().toISOString().substring(0, 10)],
    quantityReceived: [0, [Validators.required, Validators.min(0)]],
    openingBalance: [0, [Validators.required, Validators.min(0)]],
    noOfTests: [0, [Validators.required, Validators.min(0)]],
    controlsUsed: [0, [Validators.required, Validators.min(0)]],
    wastage: [0, [Validators.required, Validators.min(0)]],
    closingBalance: [0, [Validators.required, Validators.min(0)]],
    techSign: ['Lab Tech'],
    medicalOfficerSign: ['M.O.'],
    remarks: ['']
  });

  public showApheresisModal = false;
  public editingApheresis: ApheresisRecord | null = null;
  public apheresisForm = this.fb.group({
    registrationNo: ['', Validators.required],
    date: [new Date().toISOString().substring(0, 10)],
    donorName: ['', Validators.required],
    ageSex: ['28 / M'],
    weightKg: [68, [Validators.required, Validators.min(45)]],
    bloodGroup: ['O+', Validators.required],
    phone: [''],
    email: [''],
    timeOfMeal: ['8:30 AM'],
    patientName: [''],
    patientIpNo: [''],
    patientBedNo: [''],
    patientBloodGroup: [''],
    takenAspirinLast3Days: [false],
    haemoglobinGms: [14.0, [Validators.required, Validators.min(12)]],
    plateletCountLakhs: [2.5, [Validators.required, Validators.min(1.5)]],
    wbcCount: [6500],
    bloodPressure: ['120/80'],
    pulse: [72],
    temperatureCelsius: [36.8],
    totalProteinsGms: [6.8],
    procedureStartTime: ['10:00 AM'],
    procedureEndTime: ['11:15 AM'],
    durationMinutes: [75],
    venipunctureSite: ['Right antecubital'],
    wholeBloodPumpRateMlMin: [65],
    acdRatio: ['1:9'],
    totalBloodVolumeProcessedMl: [3200],
    totalProductCollectedMl: [300],
    totalVolumeAcdUsedMl: [310],
    adverseReaction: ['None'],
    qualityControlPassed: [true],
    phlebotomistSignature: ['Phleb Tech'],
    medicalOfficerSignature: ['Dr. Uddanam M.O.'],
    remarks: ['']
  });

  // Action Methods for TTI
  public openCreateTtiModal(): void {
    this.editingTti = null;
    this.ttiForm.reset({
      bagBarcode: '',
      bloodGroup: 'O-',
      testingDate: new Date().toISOString().substring(0, 10),
      hivResult: 'Non-Reactive',
      hbsAgResult: 'Non-Reactive',
      hcvResult: 'Non-Reactive',
      vdrlResult: 'Non-Reactive',
      malariaResult: 'Negative',
      atypicalAntibodies: 'Negative',
      overallStatus: 'Passed',
      testedBy: 'Lab Technician',
      approvedBy: 'Dr. Uddanam, M.O.',
      remarks: ''
    });
    this.showTtiModal = true;
  }

  public openEditTtiModal(record: TtiScreeningRecord): void {
    this.editingTti = record;
    this.ttiForm.patchValue({
      bagBarcode: record.bagBarcode,
      bloodGroup: record.bloodGroup,
      testingDate: record.testingDate ? new Date(record.testingDate).toISOString().substring(0, 10) : '',
      hivResult: record.hivResult,
      hbsAgResult: record.hbsAgResult,
      hcvResult: record.hcvResult,
      vdrlResult: record.vdrlResult,
      malariaResult: record.malariaResult,
      atypicalAntibodies: record.atypicalAntibodies,
      overallStatus: record.overallStatus,
      testedBy: record.testedBy,
      approvedBy: record.approvedBy || '',
      remarks: record.remarks || ''
    });
    this.showTtiModal = true;
  }

  public submitTtiForm(): void {
    if (this.ttiForm.invalid) return;
    const val = this.ttiForm.value as any;
    if (this.editingTti) {
      this.bloodBankService.updateTtiScreening(this.editingTti.id, val).subscribe({
        next: () => {
          this.showTtiModal = false;
          this.editingTti = null;
          this.loadBankData();
        }
      });
    } else {
      this.bloodBankService.createTtiScreening(val).subscribe({
        next: () => {
          this.showTtiModal = false;
          this.loadBankData();
        }
      });
    }
  }

  public deleteTti(id: string): void {
    if (!confirm('Are you sure you want to delete this TTI screening record?')) return;
    this.bloodBankService.deleteTtiScreening(id).subscribe({
      next: () => this.loadBankData()
    });
  }

  // Action Methods for Patient Grouping
  public openCreateGroupingModal(): void {
    this.editingGrouping = null;
    this.groupingForm.reset({
      slNo: this.patientGroupingRecords().length + 1,
      date: new Date().toISOString().substring(0, 10),
      patientName: '',
      ageSex: '35 / M',
      hospitalName: '',
      ipNumber: '',
      bagNumber: '',
      bagType: 'Single (350ml)',
      bloodGroup: 'O',
      rhType: 'Positive',
      antiA: '-',
      antiB: '-',
      antiAb: '-',
      antiD: '+',
      lectinA1: '-',
      lectinH: '+',
      aCells: '+',
      bCells: '+',
      oCells: '-',
      duTest: 'Negative',
      irregularAntibodies: 'Negative',
      autoAntibodies: 'Negative',
      crossmatchSaline: 'Compatible',
      crossmatchAlbumin: 'Compatible',
      crossmatchAhg: 'Compatible',
      technicianSignature: 'Lab Tech',
      medicalOfficerSignature: 'M.O.'
    });
    this.showGroupingModal = true;
  }

  public openEditGroupingModal(record: PatientGroupingRecord): void {
    this.editingGrouping = record;
    this.groupingForm.patchValue({
      slNo: record.slNo,
      date: record.date ? new Date(record.date).toISOString().substring(0, 10) : '',
      patientName: record.patientName,
      ageSex: record.ageSex || '',
      hospitalName: record.hospitalName || '',
      ipNumber: record.ipNumber || '',
      bagNumber: record.bagNumber || '',
      bagType: record.bagType || 'Single (350ml)',
      bloodGroup: record.bloodGroup,
      rhType: record.rhType,
      antiA: record.antiA,
      antiB: record.antiB,
      antiAb: record.antiAb,
      antiD: record.antiD,
      lectinA1: record.lectinA1,
      lectinH: record.lectinH,
      aCells: record.aCells,
      bCells: record.bCells,
      oCells: record.oCells,
      duTest: record.duTest,
      irregularAntibodies: record.irregularAntibodies,
      autoAntibodies: record.autoAntibodies,
      crossmatchSaline: record.crossmatchSaline,
      crossmatchAlbumin: record.crossmatchAlbumin,
      crossmatchAhg: record.crossmatchAhg,
      technicianSignature: record.technicianSignature || '',
      medicalOfficerSignature: record.medicalOfficerSignature || ''
    });
    this.showGroupingModal = true;
  }

  public submitGroupingForm(): void {
    if (this.groupingForm.invalid) return;
    const val = this.groupingForm.value as any;
    if (this.editingGrouping) {
      this.bloodBankService.updatePatientGrouping(this.editingGrouping.id, val).subscribe({
        next: () => {
          this.showGroupingModal = false;
          this.editingGrouping = null;
          this.loadBankData();
        }
      });
    } else {
      this.bloodBankService.createPatientGrouping(val).subscribe({
        next: () => {
          this.showGroupingModal = false;
          this.loadBankData();
        }
      });
    }
  }

  public deleteGrouping(id: string): void {
    if (!confirm('Are you sure you want to delete this patient grouping record?')) return;
    this.bloodBankService.deletePatientGrouping(id).subscribe({
      next: () => this.loadBankData()
    });
  }

  // Action Methods for Bag Stock Register
  public openCreateBagStockModal(): void {
    this.editingBagStock = null;
    this.bagStockForm.reset({
      slNo: this.bloodBagStockRecords().length + 1,
      date: new Date().toISOString().substring(0, 10),
      supplierName: '',
      invoiceNo: '',
      invoiceDate: new Date().toISOString().substring(0, 10),
      manufacturer: 'Terumo Penpol',
      batchNo: '',
      dateOfSupply: new Date().toISOString().substring(0, 10),
      dateOfManufacture: new Date().toISOString().substring(0, 10),
      dateOfExpiry: new Date().toISOString().substring(0, 10),
      codeSpecification: 'CPDA-1 Triple (450ml)',
      resultCoa: 'Satisfactory',
      openingStock: 0,
      quantityReceived: 50,
      quantityUsed: 0,
      wastage: 0,
      balance: 50,
      techSign: 'Store Incharge',
      medicalOfficerSign: 'M.O.',
      remarks: ''
    });
    this.showBagStockModal = true;
  }

  public openEditBagStockModal(record: BloodBagStockRegister): void {
    this.editingBagStock = record;
    this.bagStockForm.patchValue({
      slNo: record.slNo,
      date: record.date ? new Date(record.date).toISOString().substring(0, 10) : '',
      supplierName: record.supplierName,
      invoiceNo: record.invoiceNo,
      invoiceDate: record.invoiceDate ? new Date(record.invoiceDate).toISOString().substring(0, 10) : '',
      manufacturer: record.manufacturer,
      batchNo: record.batchNo,
      dateOfSupply: record.dateOfSupply ? new Date(record.dateOfSupply).toISOString().substring(0, 10) : '',
      dateOfManufacture: record.dateOfManufacture ? new Date(record.dateOfManufacture).toISOString().substring(0, 10) : '',
      dateOfExpiry: record.dateOfExpiry ? new Date(record.dateOfExpiry).toISOString().substring(0, 10) : '',
      codeSpecification: record.codeSpecification,
      resultCoa: record.resultCoa,
      openingStock: record.openingStock,
      quantityReceived: record.quantityReceived,
      quantityUsed: record.quantityUsed,
      wastage: record.wastage,
      balance: record.balance,
      techSign: record.techSign || '',
      medicalOfficerSign: record.medicalOfficerSign || '',
      remarks: record.remarks || ''
    });
    this.showBagStockModal = true;
  }

  public submitBagStockForm(): void {
    if (this.bagStockForm.invalid) return;
    const val = this.bagStockForm.value as any;
    val.openingStock = Number(val.openingStock || 0);
    val.quantityReceived = Number(val.quantityReceived || 0);
    val.quantityUsed = Number(val.quantityUsed || 0);
    val.wastage = Number(val.wastage || 0);
    val.balance = val.openingStock + val.quantityReceived - val.quantityUsed - val.wastage;

    if (this.editingBagStock) {
      this.bloodBankService.updateBloodBagStockRegister(this.editingBagStock.id, val).subscribe({
        next: () => {
          this.showBagStockModal = false;
          this.editingBagStock = null;
          this.loadBankData();
        }
      });
    } else {
      this.bloodBankService.createBloodBagStockRegister(val).subscribe({
        next: () => {
          this.showBagStockModal = false;
          this.loadBankData();
        }
      });
    }
  }

  public deleteBagStock(id: string): void {
    if (!confirm('Are you sure you want to delete this bag stock register record?')) return;
    this.bloodBankService.deleteBloodBagStockRegister(id).subscribe({
      next: () => this.loadBankData()
    });
  }

  // Action Methods for Diagnostic Kits
  public openCreateDiagnosticKitModal(): void {
    this.editingDiagnosticKit = null;
    this.diagnosticKitForm.reset({
      slNo: this.diagnosticKitRecords().length + 1,
      date: new Date().toISOString().substring(0, 10),
      supplierName: '',
      kitName: 'HIV 1&2 ELISA Kit (96 Tests)',
      manufacturer: 'J. Mitra & Co.',
      invoiceNo: '',
      batchNo: '',
      dateOfManufacture: new Date().toISOString().substring(0, 10),
      dateOfExpiry: new Date().toISOString().substring(0, 10),
      quantityReceived: 5,
      openingBalance: 0,
      noOfTests: 0,
      controlsUsed: 2,
      wastage: 0,
      closingBalance: 5,
      techSign: 'Lab Tech',
      medicalOfficerSign: 'M.O.',
      remarks: ''
    });
    this.showDiagnosticKitModal = true;
  }

  public openEditDiagnosticKitModal(record: DiagnosticKitRegister): void {
    this.editingDiagnosticKit = record;
    this.diagnosticKitForm.patchValue({
      slNo: record.slNo,
      date: record.date ? new Date(record.date).toISOString().substring(0, 10) : '',
      supplierName: record.supplierName,
      kitName: record.kitName,
      manufacturer: record.manufacturer,
      invoiceNo: record.invoiceNo,
      batchNo: record.batchNo,
      dateOfManufacture: record.dateOfManufacture ? new Date(record.dateOfManufacture).toISOString().substring(0, 10) : '',
      dateOfExpiry: record.dateOfExpiry ? new Date(record.dateOfExpiry).toISOString().substring(0, 10) : '',
      quantityReceived: record.quantityReceived,
      openingBalance: record.openingBalance,
      noOfTests: record.noOfTests,
      controlsUsed: record.controlsUsed,
      wastage: record.wastage,
      closingBalance: record.closingBalance,
      techSign: record.techSign || '',
      medicalOfficerSign: record.medicalOfficerSign || '',
      remarks: record.remarks || ''
    });
    this.showDiagnosticKitModal = true;
  }

  public submitDiagnosticKitForm(): void {
    if (this.diagnosticKitForm.invalid) return;
    const val = this.diagnosticKitForm.value as any;
    val.openingBalance = Number(val.openingBalance || 0);
    val.quantityReceived = Number(val.quantityReceived || 0);
    val.noOfTests = Number(val.noOfTests || 0);
    val.controlsUsed = Number(val.controlsUsed || 0);
    val.wastage = Number(val.wastage || 0);
    val.closingBalance = Number(val.closingBalance ?? (val.openingBalance + val.quantityReceived - val.wastage));

    if (this.editingDiagnosticKit) {
      this.bloodBankService.updateDiagnosticKitRegister(this.editingDiagnosticKit.id, val).subscribe({
        next: () => {
          this.showDiagnosticKitModal = false;
          this.editingDiagnosticKit = null;
          this.loadBankData();
        }
      });
    } else {
      this.bloodBankService.createDiagnosticKitRegister(val).subscribe({
        next: () => {
          this.showDiagnosticKitModal = false;
          this.loadBankData();
        }
      });
    }
  }

  public deleteDiagnosticKit(id: string): void {
    if (!confirm('Are you sure you want to delete this diagnostic kit ledger record?')) return;
    this.bloodBankService.deleteDiagnosticKitRegister(id).subscribe({
      next: () => this.loadBankData()
    });
  }

  // Action Methods for Apheresis
  public openCreateApheresisModal(): void {
    this.editingApheresis = null;
    this.apheresisForm.reset({
      registrationNo: 'SDP-' + new Date().getFullYear() + '-' + Math.floor(100 + Math.random() * 900),
      date: new Date().toISOString().substring(0, 10),
      donorName: '',
      ageSex: '28 / M',
      weightKg: 68,
      bloodGroup: 'O+',
      phone: '',
      email: '',
      timeOfMeal: '8:30 AM',
      patientName: '',
      patientIpNo: '',
      patientBedNo: '',
      patientBloodGroup: '',
      takenAspirinLast3Days: false,
      haemoglobinGms: 14.2,
      plateletCountLakhs: 2.6,
      wbcCount: 6800,
      bloodPressure: '120/80',
      pulse: 74,
      temperatureCelsius: 36.8,
      totalProteinsGms: 6.8,
      procedureStartTime: '10:00 AM',
      procedureEndTime: '11:15 AM',
      durationMinutes: 75,
      venipunctureSite: 'Right antecubital',
      wholeBloodPumpRateMlMin: 65,
      acdRatio: '1:9',
      totalBloodVolumeProcessedMl: 3200,
      totalProductCollectedMl: 300,
      totalVolumeAcdUsedMl: 310,
      adverseReaction: 'None',
      qualityControlPassed: true,
      phlebotomistSignature: 'Phleb Tech',
      medicalOfficerSignature: 'Dr. Uddanam M.O.',
      remarks: ''
    });
    this.showApheresisModal = true;
  }

  public openEditApheresisModal(record: ApheresisRecord): void {
    this.editingApheresis = record;
    this.apheresisForm.patchValue({
      registrationNo: record.registrationNo,
      date: record.date ? new Date(record.date).toISOString().substring(0, 10) : '',
      donorName: record.donorName,
      ageSex: record.ageSex || '',
      weightKg: record.weightKg,
      bloodGroup: record.bloodGroup,
      phone: record.phone || '',
      email: record.email || '',
      timeOfMeal: record.timeOfMeal || '',
      patientName: record.patientName || '',
      patientIpNo: record.patientIpNo || '',
      patientBedNo: record.patientBedNo || '',
      patientBloodGroup: record.patientBloodGroup || '',
      takenAspirinLast3Days: record.takenAspirinLast3Days,
      haemoglobinGms: record.haemoglobinGms,
      plateletCountLakhs: record.plateletCountLakhs,
      wbcCount: record.wbcCount,
      bloodPressure: record.bloodPressure || '',
      pulse: record.pulse,
      temperatureCelsius: record.temperatureCelsius,
      totalProteinsGms: record.totalProteinsGms,
      procedureStartTime: record.procedureStartTime || '',
      procedureEndTime: record.procedureEndTime || '',
      durationMinutes: record.durationMinutes,
      venipunctureSite: record.venipunctureSite,
      wholeBloodPumpRateMlMin: record.wholeBloodPumpRateMlMin,
      acdRatio: record.acdRatio,
      totalBloodVolumeProcessedMl: record.totalBloodVolumeProcessedMl,
      totalProductCollectedMl: record.totalProductCollectedMl,
      totalVolumeAcdUsedMl: record.totalVolumeAcdUsedMl,
      adverseReaction: record.adverseReaction || 'None',
      qualityControlPassed: record.qualityControlPassed,
      phlebotomistSignature: record.phlebotomistSignature || '',
      medicalOfficerSignature: record.medicalOfficerSignature || '',
      remarks: record.remarks || ''
    });
    this.showApheresisModal = true;
  }

  public submitApheresisForm(): void {
    if (this.apheresisForm.invalid) return;
    const val = this.apheresisForm.value as any;
    val.weightKg = Number(val.weightKg || 0);
    val.haemoglobinGms = Number(val.haemoglobinGms || 0);
    val.plateletCountLakhs = Number(val.plateletCountLakhs || 0);
    val.wbcCount = Number(val.wbcCount || 0);
    val.pulse = Number(val.pulse || 0);
    val.temperatureCelsius = Number(val.temperatureCelsius || 0);
    val.totalProteinsGms = Number(val.totalProteinsGms || 0);
    val.durationMinutes = Number(val.durationMinutes || 0);
    val.wholeBloodPumpRateMlMin = Number(val.wholeBloodPumpRateMlMin || 0);
    val.totalBloodVolumeProcessedMl = Number(val.totalBloodVolumeProcessedMl || 0);
    val.totalProductCollectedMl = Number(val.totalProductCollectedMl || 0);
    val.totalVolumeAcdUsedMl = Number(val.totalVolumeAcdUsedMl || 0);

    if (this.editingApheresis) {
      this.bloodBankService.updateApheresisRecord(this.editingApheresis.id, val).subscribe({
        next: () => {
          this.showApheresisModal = false;
          this.editingApheresis = null;
          this.loadBankData();
        }
      });
    } else {
      this.bloodBankService.createApheresisRecord(val).subscribe({
        next: () => {
          this.showApheresisModal = false;
          this.loadBankData();
        }
      });
    }
  }

  public deleteApheresis(id: string): void {
    if (!confirm('Are you sure you want to delete this apheresis procedure record?')) return;
    this.bloodBankService.deleteApheresisRecord(id).subscribe({
      next: () => this.loadBankData()
    });
  }

}
