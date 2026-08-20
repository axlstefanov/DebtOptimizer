import type { CreateProfileRequest, DebtPayment, PaymentPlanResponse } from "../api";
import { formatAmount, formatMonth, formatRate } from "../format";

interface PlanSectionProps {
  plan: PaymentPlanResponse;
  request: CreateProfileRequest;
}

export function PlanSection({ plan, request }: PlanSectionProps) {
  const shortfall = plan.totalMinimumPayments - plan.moneyAfterExpenses;
  const surplus = plan.moneyAfterExpenses - plan.totalMinimumPayments;
  const missedDeadlines = plan.payments.filter((payment) => payment.deadlineMet === false);
  const deadlineCost = plan.extraInterestFromDeadlines;

  const deadlineFor = (name: string) =>
    request.debts.find((debt) => debt.name === name)?.payoffDeadline ?? null;

  return (
    <section className="section appear">
      <div className="section-head">
        <div>
          <div className="step-label">Step 3</div>
          <h2>{plan.name}</h2>
        </div>
        <span className="hint">This month</span>
      </div>

      <div className="summary">
        <div className="summary-cell">
          <span className="label">Money after expenses</span>
          <span className="value">{formatAmount(plan.moneyAfterExpenses)}</span>
        </div>
        <div className="summary-cell">
          <span className="label">Total minimums</span>
          <span className="value">{formatAmount(plan.totalMinimumPayments)}</span>
        </div>
      </div>

      {!plan.isAffordable && (
        <div className="notice is-alert">
          <span className="notice-title">
            You are short {formatAmount(shortfall)} against the minimum payments
          </span>
          <span className="notice-body">
            The minimums total {formatAmount(plan.totalMinimumPayments)} but only{" "}
            {formatAmount(plan.moneyAfterExpenses)} is left after expenses. Below is what the budget covers,
            highest interest rate first — the rest goes unpaid. No payoff projection is possible until income
            rises or expenses fall.
          </span>
        </div>
      )}

      {plan.isAffordable && surplus > 0 && (
        <div className="notice">
          <span className="notice-title">
            <span className="mono">{formatAmount(surplus)}</span> above the minimums
          </span>
          <span className="notice-body">
            That surplus is already folded into the amounts below, on the{" "}
            {request.payoffStrategy.toLowerCase()} order. The dot marks the debt first in line for it —
            a deadline can outrank it.
          </span>
        </div>
      )}

      {deadlineCost !== 0 && (
        <div className="notice is-accent">
          <span className="notice-title">
            Deadlines cost {formatAmount(Math.abs(deadlineCost))}
            {deadlineCost < 0 ? " less" : " extra"} in interest
          </span>
          <span className="notice-body">
            {deadlineCost > 0
              ? "Front-loading the debts with deadlines diverts money away from the highest interest rate. That is the price of honouring them — dropping a deadline would save this much."
              : "Honouring the deadlines happens to be cheaper than the unconstrained order."}
          </span>
        </div>
      )}

      {missedDeadlines.length > 0 && (
        <div className="notice is-alert">
          <span className="notice-title">
            {missedDeadlines.length === 1 ? "One deadline cannot be met" : `${missedDeadlines.length} deadlines cannot be met`}
          </span>
          <span className="notice-body">
            {missedDeadlines
              .map((payment) => {
                const projected = payment.projectedPayoffDate
                  ? formatMonth(payment.projectedPayoffDate)
                  : "never at this rate";
                return `${payment.name} clears ${projected}`;
              })
              .join(", ")}
            . Raising the payment on those debts, or moving the deadline, is the only way to close the gap.
          </span>
        </div>
      )}

      <div className="payment-list">
        {plan.payments.map((payment, index) => (
          <PaymentRow
            key={`${payment.name}-${index}`}
            payment={payment}
            affordable={plan.isAffordable}
            deadline={deadlineFor(payment.name)}
          />
        ))}
      </div>
    </section>
  );
}

interface PaymentRowProps {
  payment: DebtPayment;
  affordable: boolean;
  deadline: string | null;
}

function PaymentRow({ payment, affordable, deadline }: PaymentRowProps) {
  const unfunded = !affordable && payment.paymentAmount === 0;

  return (
    <div className="payment">
      <div className="payment-top">
        <div className="payment-identity">
          <span className="payment-name">
            {payment.receivesSurplus && <span className="priority-dot" aria-hidden="true" />}
            {payment.name}
          </span>
          <span className="payment-sub">
            {formatAmount(payment.balance)} at {formatRate(payment.annualInterestRatePercent)}
          </span>
        </div>
        <div className="payment-amount">
          <span className={unfunded ? "figure is-zero" : "figure"}>
            {formatAmount(payment.paymentAmount)}
          </span>
          <span className="caption">{unfunded ? "not covered" : "pay this month"}</span>
        </div>
      </div>

      <div className="meta-row">
        <div className="meta">
          <span className="label">Minimum</span>
          <span className="value">{formatAmount(payment.minimumPayment)}</span>
        </div>
        <div className="meta">
          <span className="label">Interest this month</span>
          <span className="value">{formatAmount(payment.interestThisMonth)}</span>
        </div>
        {payment.projectedPayoffDate && (
          <div className="meta">
            <span className="label">Projected payoff</span>
            <span className="value">{formatMonth(payment.projectedPayoffDate)}</span>
          </div>
        )}
        {deadline && (
          <div className="meta">
            <span className="label">Deadline</span>
            <span className="value">{formatMonth(deadline)}</span>
          </div>
        )}
      </div>

      {(payment.receivesSurplus || payment.deadlineMet !== null) && (
        <div className="tag-row">
          {payment.receivesSurplus && <span className="tag is-accent">First in line for surplus</span>}
          {payment.deadlineMet === true && <span className="tag">Deadline met</span>}
          {payment.deadlineMet === false && <span className="tag is-alert">Deadline missed</span>}
        </div>
      )}
    </div>
  );
}
