import { useEffect, useMemo, useRef, useState } from "react";
import type { CreateProfileRequest, PaymentPlanResponse } from "./api";
import { ApiError, createPlan, extract } from "./api";
import type { FormState } from "./formState";
import { findProblems, formFromExtraction, hasProblems, toRequest } from "./formState";
import { PlanSection } from "./components/PlanSection";
import { ProfileForm } from "./components/ProfileForm";

const PLACEHOLDER = `I take home about 3,200 a month and my living costs are around 1,900.

I owe 4,800 on a Visa card at 22.9% with a 120 minimum, 11,000 on a car loan at 6.4% paying 260 a month, and 2,300 to my brother at no interest. I promised him it would be gone by June 2027.`;

interface PlanResult {
  plan: PaymentPlanResponse;
  request: CreateProfileRequest;
}

export default function App() {
  const [text, setText] = useState("");
  const [analysedText, setAnalysedText] = useState<string | null>(null);
  const [extracting, setExtracting] = useState(false);
  const [extractError, setExtractError] = useState<string | null>(null);

  const [followUpQuestions, setFollowUpQuestions] = useState<string[]>([]);
  const [form, setForm] = useState<FormState | null>(null);
  const [showProblems, setShowProblems] = useState(false);

  const [planning, setPlanning] = useState(false);
  const [planError, setPlanError] = useState<string | null>(null);
  const [result, setResult] = useState<PlanResult | null>(null);

  const planRef = useRef<HTMLDivElement>(null);
  const problems = useMemo(() => (form ? findProblems(form) : null), [form]);

  useEffect(() => {
    if (result) planRef.current?.scrollIntoView({ behavior: "smooth", block: "start" });
  }, [result]);

  const analysed = analysedText !== null;
  const textUnchanged = analysedText === text;

  async function handleExtract() {
    if (text.trim() === "" || extracting) return;

    setExtracting(true);
    setExtractError(null);

    try {
      const response = await extract(text);
      setForm(formFromExtraction(response.extraction));
      setFollowUpQuestions(response.followUpQuestions);
      setAnalysedText(text);
      setShowProblems(false);
      setPlanError(null);
      setResult(null);
    } catch (error) {
      setExtractError(describeExtractFailure(error));
    } finally {
      setExtracting(false);
    }
  }

  async function handleSubmit() {
    if (!form || !problems || planning) return;

    if (hasProblems(problems)) {
      setShowProblems(true);
      setPlanError(null);
      return;
    }

    const request = toRequest(form);
    setPlanning(true);
    setPlanError(null);

    try {
      setResult({ plan: await createPlan(request), request });
    } catch (error) {
      setPlanError(error instanceof ApiError ? error.message : "Something went wrong. Try again.");
      setResult(null);
    } finally {
      setPlanning(false);
    }
  }

  return (
    <main className="page">
      <header className="masthead">
        <h1>Debt Optimizer</h1>
        <p>
          Describe your debts in plain language. We turn that into a form you can correct, then work out what
          to pay this month.
        </p>
      </header>

      <section className="section">
        <div className="section-head">
          <div>
            <div className="step-label">Step 1</div>
            <h2>Describe your situation</h2>
          </div>
          <span className="hint">Income, expenses, every debt</span>
        </div>

        <div className="card">
          <div className="field">
            <label htmlFor="situation">In your own words</label>
            <textarea
              id="situation"
              value={text}
              placeholder={PLACEHOLDER}
              onChange={(event) => setText(event.target.value)}
              spellCheck={false}
            />
          </div>

          {extractError && (
            <div className="notice is-alert">
              <span className="notice-title">We could not read that</span>
              <span className="notice-body">{extractError}</span>
            </div>
          )}

          <div className="actions">
            <button
              type="button"
              className={analysed ? "btn-secondary" : "btn-primary"}
              disabled={extracting || text.trim() === "" || (analysed && textUnchanged)}
              onClick={handleExtract}
            >
              {extracting ? <span className="spinner" /> : analysed ? "Analyse the new text" : "Analyse"}
            </button>
            <span className="hint">
              {analysed && textUnchanged
                ? "Correct the details in the form below. No need to rewrite this."
                : "Rough numbers are fine, you can fix them next."}
            </span>
          </div>
        </div>
      </section>

      {form && problems && (
        <ProfileForm
          form={form}
          onChange={setForm}
          problems={problems}
          showProblems={showProblems}
          submitting={planning}
          onSubmit={handleSubmit}
          error={planError}
          followUpQuestions={followUpQuestions}
        />
      )}

      <div ref={planRef}>
        {result && <PlanSection plan={result.plan} request={result.request} />}
      </div>
    </main>
  );
}

function describeExtractFailure(error: unknown): string {
  if (!(error instanceof ApiError)) return "Something went wrong. Try again.";

  if (error.status === 502) {
    return `The extraction service did not answer usefully. Try again, or describe the numbers more plainly. One debt per sentence. (${error.message})`;
  }

  return error.message;
}
