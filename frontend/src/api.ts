export type PayoffStrategy = "Avalanche" | "Snowball" | "Target";

export interface DebtDraft {
  name: string | null;
  balance: number | null;
  annualInterestRatePercent: number | null;
  minimumPayment: number | null;
  payoffDeadline: string | null;
}

export interface ExtractionResult {
  debts: DebtDraft[];
  income: number | null;
  expenses: number | null;
}

export interface ExtractionResponse {
  extraction: ExtractionResult;
  followUpQuestions: string[];
  isComplete: boolean;
}

export interface DebtInput {
  name: string;
  balance: number;
  annualInterestRatePercent: number;
  minimumPayment: number;
  payoffDeadline: string | null;
}

export interface CreateProfileRequest {
  name: string;
  income: number;
  expenses: number;
  payoffStrategy: PayoffStrategy;
  targetDebtName: string | null;
  debts: DebtInput[];
}

export interface DebtPayment {
  name: string;
  balance: number;
  annualInterestRatePercent: number;
  minimumPayment: number;
  paymentAmount: number;
  interestThisMonth: number;
  receivesSurplus: boolean;
  deadlineMet: boolean | null;
  projectedPayoffDate: string | null;
}

export interface PaymentPlanResponse {
  name: string;
  moneyAfterExpenses: number;
  totalMinimumPayments: number;
  isAffordable: boolean;
  payments: DebtPayment[];
  extraInterestFromDeadlines: number;
}

export class ApiError extends Error {
  readonly status: number;

  constructor(status: number, message: string) {
    super(message);
    this.status = status;
  }
}

async function post<T>(path: string, body: unknown): Promise<T> {
  let response: Response;

  try {
    response = await fetch(path, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(body)
    });
  } catch {
    throw new ApiError(0, "Could not reach the server. Check your connection and try again.");
  }

  if (!response.ok) throw new ApiError(response.status, await readError(response));

  return (await response.json()) as T;
}

const MAX_ERROR_LENGTH = 220;

function clip(message: string): string {
  const collapsed = message.replace(/\s+/g, " ").trim();
  return collapsed.length > MAX_ERROR_LENGTH
    ? `${collapsed.slice(0, MAX_ERROR_LENGTH)}…`
    : collapsed;
}

async function readError(response: Response): Promise<string> {
  const raw = await response.text().catch(() => "");
  if (!raw) return `Request failed with status ${response.status}.`;

  try {
    const problem = JSON.parse(raw) as { detail?: string; title?: string };
    return clip(problem.detail || problem.title || raw);
  } catch {
    return clip(raw);
  }
}

export const extract = (text: string) =>
  post<ExtractionResponse>("/api/extract", { text });

export const createPlan = (request: CreateProfileRequest) =>
  post<PaymentPlanResponse>("/api/profiles/plan", request);
