import type { PayoffStrategy } from "../api";
import type { DebtField, DebtRow, FormProblems, FormState } from "../formState";
import { emptyDebtRow, hasProblems } from "../formState";
import { TextField } from "./TextField";

const STRATEGIES: { value: PayoffStrategy; label: string; blurb: string }[] = [
  { value: "Avalanche", label: "Avalanche", blurb: "Surplus goes to the highest interest rate — cheapest overall." },
  { value: "Snowball", label: "Snowball", blurb: "Surplus goes to the smallest balance — quickest wins." },
  { value: "Target", label: "Target", blurb: "Surplus goes to one debt you choose, whatever it costs." }
];

interface ProfileFormProps {
  form: FormState;
  onChange: (form: FormState) => void;
  problems: FormProblems;
  showProblems: boolean;
  submitting: boolean;
  onSubmit: () => void;
  error: string | null;
  followUpQuestions: string[];
}

export function ProfileForm({
  form,
  onChange,
  problems,
  showProblems,
  submitting,
  onSubmit,
  error,
  followUpQuestions
}: ProfileFormProps) {
  const set = <K extends keyof FormState>(field: K, value: FormState[K]) =>
    onChange({ ...form, [field]: value });

  const setDebt = (key: string, field: DebtField, value: string) =>
    onChange({
      ...form,
      debts: form.debts.map((debt) => (debt.key === key ? { ...debt, [field]: value } : debt))
    });

  const addDebt = () => onChange({ ...form, debts: [...form.debts, emptyDebtRow()] });

  const removeDebt = (key: string) =>
    onChange({ ...form, debts: form.debts.filter((debt) => debt.key !== key) });

  const problem = (field: "income" | "expenses") => (showProblems ? problems[field] : undefined);

  const debtProblem = (key: string, field: DebtField) =>
    showProblems ? problems.debts[key]?.[field] : undefined;

  const namedDebts = form.debts.filter((debt) => debt.name.trim() !== "");

  return (
    <section className="section appear">
      <div className="section-head">
        <div>
          <div className="step-label">Step 2</div>
          <h2>Check what we understood</h2>
        </div>
        <span className="hint">Nothing is calculated until you submit</span>
      </div>

      {followUpQuestions.length > 0 && hasProblems(problems) && (
        <div className="notice">
          <span className="notice-title">
            {followUpQuestions.length} detail{followUpQuestions.length === 1 ? "" : "s"} still missing
          </span>
          <ul className="question-list">
            {followUpQuestions.map((question) => (
              <li key={question}>{question}</li>
            ))}
          </ul>
        </div>
      )}

      <form
        className="card"
        onSubmit={(event) => {
          event.preventDefault();
          onSubmit();
        }}
      >
        <div className="grid-2">
          <TextField
            id="income"
            label="Monthly income"
            value={form.income}
            onChange={(value) => set("income", value)}
            placeholder="0.00"
            numeric
            needsInput={form.income.trim() === ""}
            error={problem("income")}
          />
          <TextField
            id="expenses"
            label="Monthly expenses"
            value={form.expenses}
            onChange={(value) => set("expenses", value)}
            placeholder="0.00"
            numeric
            needsInput={form.expenses.trim() === ""}
            error={problem("expenses")}
          />
        </div>

        <div className="field">
          <label htmlFor="strategy-Avalanche">Payoff strategy</label>
          <div className="segmented" id="strategy" role="group" aria-label="Payoff strategy">
            {STRATEGIES.map((option) => (
              <button
                key={option.value}
                id={`strategy-${option.value}`}
                type="button"
                className={form.strategy === option.value ? "is-active" : undefined}
                aria-pressed={form.strategy === option.value}
                onClick={() => set("strategy", option.value)}
              >
                {option.label}
              </button>
            ))}
          </div>
          <span className="field-error">
            {STRATEGIES.find((option) => option.value === form.strategy)?.blurb}
          </span>
        </div>

        {form.strategy === "Target" && (
          <div className="field">
            <label htmlFor="target-debt">Clear this debt first</label>
            {namedDebts.length === 0 ? (
              <span className="field-error">Name a debt below to target it.</span>
            ) : (
              <div className="segmented" id="target-debt" role="group" aria-label="Target debt">
                {namedDebts.map((debt) => (
                  <button
                    key={debt.key}
                    type="button"
                    className={form.targetDebtName === debt.name ? "is-active" : undefined}
                    aria-pressed={form.targetDebtName === debt.name}
                    onClick={() => set("targetDebtName", debt.name)}
                  >
                    {debt.name}
                  </button>
                ))}
              </div>
            )}
            {showProblems && problems.strategy && <span className="field-error">{problems.strategy}</span>}
          </div>
        )}

        <div className="section-head">
          <h2>Debts</h2>
          <button type="button" className="btn-ghost" onClick={addDebt}>
            + Add debt
          </button>
        </div>

        {form.debts.map((debt, index) => (
          <DebtFields
            key={debt.key}
            debt={debt}
            index={index}
            canRemove={form.debts.length > 1}
            onChange={setDebt}
            onRemove={removeDebt}
            problemFor={debtProblem}
          />
        ))}

        {showProblems && problems.form && <span className="field-error">{problems.form}</span>}

        {error && (
          <div className="notice is-alert">
            <span className="notice-title">The plan could not be calculated</span>
            <span className="notice-body">{error}</span>
          </div>
        )}

        <div className="actions">
          <button type="submit" className="btn-primary" disabled={submitting}>
            {submitting ? <span className="spinner" /> : "Build the plan"}
          </button>
          <span className="hint">Nothing is saved</span>
        </div>
      </form>
    </section>
  );
}

interface DebtFieldsProps {
  debt: DebtRow;
  index: number;
  canRemove: boolean;
  onChange: (key: string, field: DebtField, value: string) => void;
  onRemove: (key: string) => void;
  problemFor: (key: string, field: DebtField) => string | undefined;
}

function DebtFields({ debt, index, canRemove, onChange, onRemove, problemFor }: DebtFieldsProps) {
  return (
    <div className="debt-row">
      <div className="debt-row-head">
        <span className="index">Debt {index + 1}</span>
        {canRemove && (
          <button type="button" className="btn-ghost" onClick={() => onRemove(debt.key)}>
            Remove
          </button>
        )}
      </div>

      <div className="debt-grid">
        <div className="span-2">
          <TextField
            id={`${debt.key}-name`}
            label="Name"
            value={debt.name}
            onChange={(value) => onChange(debt.key, "name", value)}
            placeholder="Visa card"
            needsInput={debt.name.trim() === ""}
            error={problemFor(debt.key, "name")}
          />
        </div>
        <TextField
          id={`${debt.key}-balance`}
          label="Balance"
          value={debt.balance}
          onChange={(value) => onChange(debt.key, "balance", value)}
          placeholder="0.00"
          numeric
          needsInput={debt.balance.trim() === ""}
          error={problemFor(debt.key, "balance")}
        />
        <TextField
          id={`${debt.key}-rate`}
          label="Interest rate % per year"
          value={debt.annualInterestRatePercent}
          onChange={(value) => onChange(debt.key, "annualInterestRatePercent", value)}
          placeholder="0.0"
          numeric
          needsInput={debt.annualInterestRatePercent.trim() === ""}
          error={problemFor(debt.key, "annualInterestRatePercent")}
        />
        <TextField
          id={`${debt.key}-minimum`}
          label="Minimum monthly payment"
          value={debt.minimumPayment}
          onChange={(value) => onChange(debt.key, "minimumPayment", value)}
          placeholder="0.00"
          numeric
          needsInput={debt.minimumPayment.trim() === ""}
          error={problemFor(debt.key, "minimumPayment")}
        />
        <TextField
          id={`${debt.key}-deadline`}
          label="Payoff deadline"
          value={debt.payoffDeadline}
          onChange={(value) => onChange(debt.key, "payoffDeadline", value)}
          date
          optionalNote="optional"
        />
      </div>
    </div>
  );
}
