import type { CreateProfileRequest, ExtractionResult, PayoffStrategy } from "./api";
import { fromNumber, toDecimal } from "./format";

export interface DebtRow {
  key: string;
  name: string;
  balance: string;
  annualInterestRatePercent: string;
  minimumPayment: string;
  payoffDeadline: string;
}

export interface FormState {
  name: string;
  income: string;
  expenses: string;
  strategy: PayoffStrategy;
  targetDebtName: string;
  debts: DebtRow[];
}

export type DebtField = Exclude<keyof DebtRow, "key">;

export interface FormProblems {
  income?: string;
  expenses?: string;
  strategy?: string;
  form?: string;
  debts: Record<string, Partial<Record<DebtField, string>>>;
}

let keySeed = 0;
const nextKey = () => `debt-${++keySeed}`;

export function emptyDebtRow(): DebtRow {
  return {
    key: nextKey(),
    name: "",
    balance: "",
    annualInterestRatePercent: "",
    minimumPayment: "",
    payoffDeadline: ""
  };
}

export function formFromExtraction(extraction: ExtractionResult): FormState {
  const debts = extraction.debts.map((draft) => ({
    key: nextKey(),
    name: draft.name ?? "",
    balance: fromNumber(draft.balance),
    annualInterestRatePercent: fromNumber(draft.annualInterestRatePercent),
    minimumPayment: fromNumber(draft.minimumPayment),
    payoffDeadline: draft.payoffDeadline ?? ""
  }));

  return {
    name: "My plan",
    income: fromNumber(extraction.income),
    expenses: fromNumber(extraction.expenses),
    strategy: "Avalanche",
    targetDebtName: "",
    debts: debts.length > 0 ? debts : [emptyDebtRow()]
  };
}

function amountProblem(raw: string, allowNegative = false): string | undefined {
  if (raw.trim() === "") return "Required.";

  const value = toDecimal(raw);
  if (value === null) return "Enter a number.";
  if (!allowNegative && value < 0) return "Cannot be negative.";

  return undefined;
}

export function findProblems(form: FormState): FormProblems {
  const problems: FormProblems = { debts: {} };

  problems.income = amountProblem(form.income);
  problems.expenses = amountProblem(form.expenses);

  for (const debt of form.debts) {
    const row: Partial<Record<DebtField, string>> = {};

    if (debt.name.trim() === "") row.name = "Required.";
    row.balance = amountProblem(debt.balance);
    row.annualInterestRatePercent = amountProblem(debt.annualInterestRatePercent);
    row.minimumPayment = amountProblem(debt.minimumPayment);

    for (const field of Object.keys(row) as DebtField[]) {
      if (row[field] === undefined) delete row[field];
    }

    if (Object.keys(row).length > 0) problems.debts[debt.key] = row;
  }

  if (form.debts.length === 0) problems.form = "Add at least one debt.";

  if (
    form.strategy === "Target" &&
    !form.debts.some((debt) => debt.name.trim() !== "" && debt.name === form.targetDebtName)
  ) {
    problems.strategy = "Pick the debt you want to clear first.";
  }

  if (problems.income === undefined) delete problems.income;
  if (problems.expenses === undefined) delete problems.expenses;

  return problems;
}

export const hasProblems = (problems: FormProblems) =>
  Boolean(problems.income || problems.expenses || problems.strategy || problems.form) ||
  Object.keys(problems.debts).length > 0;

export function toRequest(form: FormState): CreateProfileRequest {
  return {
    name: form.name.trim() === "" ? "My plan" : form.name.trim(),
    income: toDecimal(form.income) ?? 0,
    expenses: toDecimal(form.expenses) ?? 0,
    payoffStrategy: form.strategy,
    targetDebtName: form.strategy === "Target" ? form.targetDebtName : null,
    debts: form.debts.map((debt) => ({
      name: debt.name.trim(),
      balance: toDecimal(debt.balance) ?? 0,
      annualInterestRatePercent: toDecimal(debt.annualInterestRatePercent) ?? 0,
      minimumPayment: toDecimal(debt.minimumPayment) ?? 0,
      payoffDeadline: debt.payoffDeadline.trim() === "" ? null : debt.payoffDeadline
    }))
  };
}
