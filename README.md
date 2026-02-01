# Async API Integration Practice

Practicing async/await, Polly v8 resilience, and parallel orchestration across two fault-simulating APIs (CRM and ERP).

## Problems

| # | Problem | Focus |
|---|---------|-------|
| 1 | Fault-simulating mock APIs | Two minimal APIs with random errors, latency, and rate limiting |
| 2 | Composable resilience pipelines | Polly v8 retry, circuit breaker, rate-limit, and fast-fail strategies |
| 3 | Order validation workflow | `Task.WhenAll` for parallel credit checks across both services |
| 4 | Order completion workflow | Chained async updates to balance, loyalty, and order status |
| 5 | Customer loyalty reconciliation | Batch sync comparing calculated vs. stored loyalty points |

## Key Takeaways

Different operations deserve different resilience strategies: loyalty updates use a fast-fail pipeline because they are non-critical, while credit checks use exponential backoff with jitter to avoid a thundering-herd on the CRM. `Task.WhenAll` over async lambdas is the main concurrency lever, but each result still needs individual failure tracking so one bad order does not cancel the entire batch.