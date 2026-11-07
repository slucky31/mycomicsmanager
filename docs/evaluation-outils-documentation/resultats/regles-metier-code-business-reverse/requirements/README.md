# Platform Business Requirements (reverse-engineered)

This folder describes what the platform **does today**. It was extracted from the source code because no up-to-date requirements existed.
Every statement is a **draft until the product team confirms it**.


## How to read this folder

These are **working files**. They are organised by code component and carry code citations, so the extraction and review team can check every statement.
**Stakeholders should read the generated document instead:** `../deliverables/Business-Requirements-Specification.md` (built with `/re-publish`).

To understand the product from the working files, read top-down:

| Step | Read | Question it answers |
|---|---|---|
| 1 | `00-overview/system-context.md`, `component-map.md` | What is the system, what parts does it have, what does it connect to? |
| 2 | `00-overview/glossary.md` | What do the business words mean? |
| 3 | `01-domain/entities/` | What does the business manage, and how does each thing change state? |
| 4 | `02-processes/` | How does each business outcome happen end to end, and what happens when it fails? **Best starting point for business readers.** |
| 5 | `05-user-journeys/` | What do users see and do, screen by screen? |
| 6 | `03-capabilities/<component>/` | Exactly which rules each part enforces, with code evidence (for detail and verification) |
| 7 | `04-integrations/` | What is exchanged with third parties? |
| 8 | `06-open-questions.md`, `07-suspected-defects.md` | What is undecided, and what looks wrong? |
| 9 | `_review/` | What product has been asked to confirm, and what it decided |

| Reader | Read |
|---|---|
| Executive / sponsor | The deliverable: sections 2, 11, 12 |
| Product owner | The deliverable, then `_review/` packs |
| Business analyst / QA | `02-processes/`, `05-user-journeys/`, then `03-capabilities/*/rules.md` |
| Architect / engineer | `00-overview/`, `03-capabilities/` (catalogues, `_defects.md`) |

## For product reviewers

- **Start with the review packs** in `_review/`. Each one covers a single business process or area, in plain language, with columns for your decisions. You don't need to read anything else.
- **Decisions you can make on each rule:**

  | Decision | Meaning |
  |---|---|
  | **Confirmed** | Yes, this is how it should work. |
  | **Changed** | The system does this, but the rule should be … (write the correct wording). |
  | **Rejected (bug)** | The system does this, but it shouldn't. It's a defect. |
  | **Obsolete** | This is no longer used by the business. |
  | **Unsure** | Needs discussion. |

- **Confidence** tells you how sure the extraction is:
  - **High:** explicit in code.
  - **Medium:** inferred.
  - **Low / ⚠ Unsure:** a best guess. Please look at these closely.
- **Questions for you** are listed in each pack and collected in `06-open-questions.md`.
- **Things that look wrong** are in `07-suspected-defects.md`. These are *not* requirements.

## Where things are

| Folder | Contents |
|---|---|
| `component-map.md` | Which code components exist, where they are, and their ID prefixes |
| `00-overview/` | System context, glossary of business terms, coverage tracker |
| `01-domain/` | Business entities (Client, Ledger, Vault, Trade, …) and their lifecycles |
| `02-processes/` | End-to-end business processes (e.g. trade placement), the best starting point |
| `03-capabilities/` | What each component is responsible for, and its detailed rules. Includes the **web-services API catalogue** (`web-services/api-catalogue.md`) and the **nightly batch jobs** (`batch-jobs/batch-schedule.md`, one file per job in `jobs/`, output file layouts in `file-layouts/`). |
| `04-integrations/` | What we exchange with each third party |
| `05-user-journeys/` | Front-office and back-office screens, validations, permissions |
| `_review/` | Review packs for product sessions |

## For the extraction team: run order

| Step | Command | Output |
|---|---|---|
| 0 | `/re-discover` | Component map, system context, coverage tracker, glossary seed |
| 1 | `/re-domain` | Entities, lifecycles, DB-resident rules |
| 2 | `/re-service <component>` (repeat per service; resumable) | Capability and rules per service |
| 2a | `/re-verify <OUT>/03-capabilities/<component>`, **in a fresh session** | Corrections, verification log |
| 2b | `/re-api web-services` (resumable), then `/re-verify` | API catalogue, cross-cutting auth/validation, rules |
| 2c | `/re-batch batch-jobs inventory`, then `/re-batch batch-jobs` (or job ranges such as `BJ-001..BJ-020`), then `/re-verify` | Batch schedule, one file per job, file layouts |
| 3 | `/re-integration <counterparty>` | INT docs |
| 4 | `/re-ui front-end`, `/re-ui back-office` | User journeys, roles |
| 5 | `/re-process "<process>"`, including `/re-process "end-of-day batch"` | End-to-end process docs |
| 6 | `/re-review-pack <P-NNN \| BJ range \| API area>` | Pack for the product session |
| 7 | `/re-apply-review <pack>` | Decisions written back |
| any time | `/re-coverage` | Refreshed tracker, consolidated questions and defects, consistency check |
