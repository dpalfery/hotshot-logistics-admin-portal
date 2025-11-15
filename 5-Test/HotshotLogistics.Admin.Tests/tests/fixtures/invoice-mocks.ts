import { Invoice, InvoiceStatus, InvoiceLineItem, PaymentTerms } from '@/admin-dashboard/types';

// Mock invoice data aligned with IInvoice domain model
export type MockInvoice = Invoice;
export type MockInvoiceLineItem = InvoiceLineItem;
export type MockPaymentTerms = PaymentTerms;

// Fixed timestamps for deterministic testing
const FIXED_DATE = '2024-01-15T10:00:00.000Z';
const FIXED_DUE_DATE = '2024-02-15T10:00:00.000Z';
const FIXED_OVERDUE_DATE = '2024-01-10T10:00:00.000Z';

// Mock payment terms
const defaultTerms: MockPaymentTerms = {
  netDays: 30,
  earlyPaymentDiscount: 0.02,
  earlyPaymentDiscountDays: 10,
  latePaymentPenalty: 0.05,
  latePaymentPenaltyDays: 5,
};

// Mock line items
const sampleLineItems: MockInvoiceLineItem[] = [
  {
    id: 1,
    description: 'Hotshot delivery service',
    quantity: 1,
    unitPrice: 1500.00,
    amount: 1500.00,
    taxApplicable: true,
    sortOrder: 1,
  },
];

// Mock invoices covering all required statuses
export const mockInvoices: MockInvoice[] = [
  {
    id: 'inv-draft-001',
    invoiceNumber: 'INV-2024-001',
    customerId: 'CUST-001',
    jobId: 'JOB-001',
    invoiceDate: FIXED_DATE,
    dueDate: FIXED_DUE_DATE,
    status: InvoiceStatus.Draft,
    lineItems: sampleLineItems,
    subTotal: 1500.00,
    taxRate: 0.08,
    taxAmount: 120.00,
    discountAmount: 0.00,
    totalAmount: 1620.00,
    paidAmount: 0.00,
    balanceDue: 1620.00,
    terms: defaultTerms,
    notes: 'Draft invoice for review',
    createdAt: FIXED_DATE,
  },
  {
    id: 'inv-sent-001',
    invoiceNumber: 'INV-2024-002',
    customerId: 'CUST-002',
    jobId: 'JOB-002',
    invoiceDate: FIXED_DATE,
    dueDate: FIXED_DUE_DATE,
    status: InvoiceStatus.Sent,
    lineItems: sampleLineItems,
    subTotal: 1500.00,
    taxRate: 0.08,
    taxAmount: 120.00,
    discountAmount: 0.00,
    totalAmount: 1620.00,
    paidAmount: 0.00,
    balanceDue: 1620.00,
    terms: defaultTerms,
    notes: 'Sent to customer',
    createdAt: FIXED_DATE,
  },
  {
    id: 'inv-viewed-001',
    invoiceNumber: 'INV-2024-006',
    customerId: 'CUST-006',
    jobId: 'JOB-006',
    invoiceDate: FIXED_DATE,
    dueDate: FIXED_DUE_DATE,
    status: InvoiceStatus.Viewed,
    lineItems: sampleLineItems,
    subTotal: 1500.00,
    taxRate: 0.08,
    taxAmount: 120.00,
    discountAmount: 0.00,
    totalAmount: 1620.00,
    paidAmount: 0.00,
    balanceDue: 1620.00,
    terms: defaultTerms,
    notes: 'Viewed by customer',
    createdAt: FIXED_DATE,
  },
  {
    id: 'inv-paid-001',
    invoiceNumber: 'INV-2024-003',
    customerId: 'CUST-003',
    jobId: 'JOB-003',
    invoiceDate: FIXED_DATE,
    dueDate: FIXED_DUE_DATE,
    status: InvoiceStatus.Paid,
    lineItems: sampleLineItems,
    subTotal: 1500.00,
    taxRate: 0.08,
    taxAmount: 120.00,
    discountAmount: 0.00,
    totalAmount: 1620.00,
    paidAmount: 1620.00,
    balanceDue: 0.00,
    terms: defaultTerms,
    notes: 'Fully paid',
    createdAt: FIXED_DATE,
    updatedAt: FIXED_DATE,
  },
  {
    id: 'inv-overdue-001',
    invoiceNumber: 'INV-2024-004',
    customerId: 'CUST-004',
    jobId: 'JOB-004',
    invoiceDate: FIXED_DATE,
    dueDate: FIXED_OVERDUE_DATE,
    status: InvoiceStatus.Overdue,
    lineItems: sampleLineItems,
    subTotal: 1500.00,
    taxRate: 0.08,
    taxAmount: 120.00,
    discountAmount: 0.00,
    totalAmount: 1620.00,
    paidAmount: 0.00,
    balanceDue: 1620.00,
    terms: defaultTerms,
    notes: 'Overdue payment',
    createdAt: FIXED_DATE,
  },
  {
    id: 'inv-cancelled-001',
    invoiceNumber: 'INV-2024-005',
    customerId: 'CUST-005',
    jobId: 'JOB-005',
    invoiceDate: FIXED_DATE,
    dueDate: FIXED_DUE_DATE,
    status: InvoiceStatus.Cancelled,
    lineItems: sampleLineItems,
    subTotal: 1500.00,
    taxRate: 0.08,
    taxAmount: 120.00,
    discountAmount: 0.00,
    totalAmount: 1620.00,
    paidAmount: 0.00,
    balanceDue: 0.00,
    terms: defaultTerms,
    notes: 'Cancelled invoice',
    createdAt: FIXED_DATE,
    updatedAt: FIXED_DATE,
  },
];

// Helper functions for filtering
export function getInvoicesByStatus(status?: string | null): MockInvoice[] {
  if (!status) return mockInvoices;
  return mockInvoices.filter(invoice => invoice.status === status);
}

export function getInvoiceById(id: string): MockInvoice | undefined {
  return mockInvoices.find(invoice => invoice.id === id);
}

// Mock response builders
export function buildInvoicesResponse(status?: string | null) {
  const items = getInvoicesByStatus(status);
  return {
    items,
    totalCount: items.length,
  };
}

export function buildInvoiceResponse(id: string) {
  const invoice = getInvoiceById(id);
  if (!invoice) {
    throw new Error(`Invoice with id ${id} not found`);
  }
  return invoice;
}