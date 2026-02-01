# LINQ Practice

Non-trivial LINQ exercises covering joins, grouping, data transformation, query optimization, and dynamic filtering.

## Problems

| # | Problem | Focus |
|---|---------|-------|
| 1 | Customer Spending Analysis | `GroupJoin`, `GroupBy`, `Sum` for segment-level aggregation |
| 2 | SAP-to-Salesforce Transformation | `Select` with nullable parsing, `OfType<T>` to discard failed rows |
| 3 | Query Optimization | `GroupBy` + `Join` to reduce O(n*m) to O(n+m), plus `GroupJoin` left join |
| 4 | Dynamic Appointment Search | Custom `WhereIf<T>` extension over `IQueryable` for conditional filters |

## Key Takeaways

Pre-grouping orders into a dictionary-like structure before joining is the key move that turns a nested-loop scan into a single-pass lookup (Problem 3). Returning null from a `Select` and then filtering with `OfType<>` is a concise way to skip records that fail parsing without throwing exceptions (Problem 2). The `WhereIf` extension keeps dynamic queries fluent by hiding the repetitive null-checks behind a single generic method (Problem 4).
