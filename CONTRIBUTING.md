# Contributing to Peerlight

Thank you for your interest in contributing to **Peerlight**! Please read through these guidelines before submitting issues or pull requests.

## 1. Governance & Project Identity

The official product name is **Peerlight**. Use this spelling and capitalization consistently across all code, documentation, assets, and UI.

## 2. Branching & Commit Conventions

### Branch Strategy
- `main`: Protected production-ready branch. All merges must pass automated checks.
- `feature/<name>`: New feature implementations (e.g. `feature/peerlight-rebrand-ui`).
- `fix/<name>`: Bug fixes and security patches.
- `spike/<name>`: Feasibility audits and exploratory prototypes.

### Commit Messages
We follow the [Conventional Commits](https://www.conventionalcommits.org/) specification:
- `feat(component): add feature description`
- `fix(component): fix bug description`
- `docs: update documentation`
- `test: add unit or integration tests`
- `refactor: clean up internal implementation`
- `chore: tooling or dependency updates`

Component scopes: `windows`, `android`, `tv`, `shared`, `protocol`, `docs`, `security`.

## 3. Issue Label Taxonomy

To maintain clarity across platforms and milestones, issues and pull requests are classified with the following standard labels:
- `platform`: OS-specific requirements or platform-level behaviors (Windows, Android, Google TV).
- `security`: Authentication, cryptographic pairing, consent boundaries, permissions, privacy.
- `performance`: Latency optimization, FPS profiling, memory budgeting, hardware encoding.
- `bug`: Defect or unintended behavior in implemented code.
- `docs`: Documentation, architecture decision records, specifications.
- `blocked`: Work items impeded by external platform limitations or missing hardware.

## 4. Definition of Done (DoD)

A feature or task checklist item in [`docs/PEERLIGHT_Master_TODO.md`](docs/PEERLIGHT_Master_TODO.md) is marked complete **only** when:
1. Working implementation code exists and is committed.
2. Acceptance criteria are satisfied.
3. Unit and automated integration tests pass.
4. Testing has been performed and documented on identified target hardware.
5. OS permissions, security boundaries, and limitations are documented.
6. Temporary media buffers are confirmed bounded and discarded without persistent disk storage.
7. No screen recording or ADB runtime dependency is introduced.
