# DebtOptimizer

Tell it what you owe in your own words. It tells you where your money goes this month.

**[debtoptimizer.onrender.com](https://debtoptimizer.onrender.com)**

Free tier, so the first load takes about 30 seconds to wake up.

![Describe your situation](docs/input.png)

---

## The problem

Most people pay the minimum because that is what the bill says. If the minimum is below the monthly interest, the balance grows while you pay. Pay 50 a month on 3,000 at 20% and ten years later you still owe 3,000, having paid 6,000.

Which debt to attack is also not obvious. A euro against a 20% card saves 20 cents a year, forever. The same euro against a 5% loan saves 5 cents. The balance never enters that calculation. A 500 card at 20% beats a 50,000 loan at 5% for the next euro.

That is the **avalanche method**. It is provably optimal for minimising interest, and it is what this app does by default.

![The plan](docs/plan.png)

---

## What it does

| | |
|---|---|
| **Reads plain text** | *"about 3k on my card and a car loan, 10 grand at 5 percent"* becomes structured data. Anything you did not say comes back empty and gets asked. Never guessed. |
| **Handles deadlines** | *"car gone by March"* reserves what that costs per month, then avalanches the rest. If March is impossible it gives you the earliest date that is not. |
| **Prices your choices** | Honouring a deadline costs interest. It tells you how much, so you can decide. |
| **Three strategies** | Avalanche (cheapest), snowball (smallest first), target (one specific debt). Set it yourself, or let the AI read it from how you describe things. |
| **Says no honestly** | If your minimums exceed your income, it says so and shows what the budget covers. |

---

## Decisions

**No solver.** Deadlines look like constrained optimisation. They are not. Each one has a fixed monthly requirement, so you subtract them all and the remainder has one obvious best use. Reserve, then sort. Knowing when not to reach for OR-Tools was the point.

**No CQRS.** Two entities, no read/write asymmetry, no scale pressure. It would have been decoration.

**The AI never does maths.** It turns sentences into JSON and classifies intent. Every number the user sees comes from C#. Models are unreliable at arithmetic and reliable at form filling, so it only does the second. A failed call returns 502 rather than a plausible looking empty result.

**Nullable extraction DTOs.** `decimal?` instead of `decimal`, because 0% loans are real. Without that, "the user said zero" and "the user said nothing" are the same value, and the whole gap detection feature falls apart.

**One interface.** `IDebtExtractor` exists because swapping Gemini for Azure OpenAI is a real possibility. `PaymentPlanService` has none, because nothing will ever implement it twice.

**Pure calculation core.** `PaymentPlanService` has no DbContext, no HTTP, no clock. It survived a full swap from SQL Server to PostgreSQL without a single test changing.

---

## Stack

ASP.NET Core 10, PostgreSQL, EF Core, Gemini, React with TypeScript, Docker, GitHub Actions, Render.

Three stage Dockerfile: Node builds the frontend, the .NET SDK publishes the API, the runtime image gets both. One deployment, no CORS. Every push to master runs 28 tests, and the deploy only fires if they pass.

---

## Run it

Needs `DB_PASSWORD` and `GEMINI_API_KEY` in a `.env` file at the repo root.

```bash
docker compose up -d --build   # localhost:5154
dotnet test tests/DebtOptimizer.Tests
```

---

## API

```
POST /api/extract              text to structured data, plus questions for the gaps
POST /api/infer-strategy       text to strategy, with a reason
POST /api/profiles/plan        request to plan, nothing stored
POST /api/profiles             save
GET  /api/profiles/{id}        read
POST /api/profiles/{id}/plan   plan from a saved profile
```

---

## Not done

**No integration tests.** All 28 cover the calculation in isolation. Nothing crosses the HTTP or database boundary, which is how an enum serialisation bug reached production once.

**No staging environment.** A green build deploys straight to production. Tests gate it, but nobody looks at it running before users do.

**Free tier database.** It expires. Fine for a demo, not for anything real.

**Savings.** Debts first. Once savings compete for the same euro it gets harder. An emergency buffer prevents future 20% debt, which can beat paying down 5% debt today.
