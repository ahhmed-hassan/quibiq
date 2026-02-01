# Coding Practice

LINQ and C# data-processing practice covering grouping, joins, validation, and parsing.

## Problems

| # | Problem | Focus |
|---|---------|-------|
| 1 | Sales Analysis | `GroupBy`, `Sum`, `Take` for aggregation and top-N queries |
| 2 | Data Reconciliation | `GroupJoin`, `ExceptBy` for joins and set operations |
| 3 | Order Transformation | `ErrorOr<T>` and enum parsing for a validation pipeline |
| 4 | CSV Invoice Parser | `Split`, `TryParse`, and line-level error reporting |

## Key Takeaways

`GroupJoin` avoids multiple passes over the data but requires careful use of `SelectMany` to flatten results, so readability is a real tradeoff. Problems 3 and 4 replace thrown exceptions with the `ErrorOr<T>` pattern, keeping validation composable and testable without try/catch overhead. The set progresses from pure LINQ aggregation toward real-world concerns like system-to-system reconciliation and raw-text parsing.
