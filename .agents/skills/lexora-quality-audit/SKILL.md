---
name: lexora-quality-audit
description: Runs pre-flight verification on the Lexora solution, enforcing zero build warnings, zero errors, and verifying that test suites pass.
version: 1.0.0
tags: [quality-gates, dotnet, build, test, verification]
---

# 🔍 Skill: Lexora Quality Audit

## 1. Capabilities & Pre-requisites
- **Target Solution**: `Lexora.slnx` (.NET 10 solution).
- **Execution Environment**: Windows PowerShell 5.1+ or PowerShell 7+ on Windows.
- **Constraints**: Must be executed from the repository root directory (`d:\LEARN\Lexora`).
- **Invariants Enforced**:
  - `0 Warning(s)`, `0 Error(s)`.
  - All automated tests must remain green (zero regression).

---

## 2. Step-by-Step Execution Workflow

```mermaid
flowchart TD
    Start[Trigger Audit Skill] --> Step1[Run Clean Build Check]
    Step1 --> Decision1{Build Succeeded?}
    Decision1 -- No --> Fail1[Emit Build Failure Diagnostic & Line References]
    Decision1 -- Yes --> Step2[Execute Targeted Unit Tests]
    Step2 --> Decision2{All Tests Green?}
    Decision2 -- No --> Fail2[Emit Test Regression Diagnostic]
    Decision2 -- Yes --> Success[Report: QUALITY GATES PASSED]
```

### Step 1: Silent Build Verification
Execute the quiet compilation command:
```powershell
powershell -ExecutionPolicy Bypass -File .agents/skills/lexora-quality-audit/scripts/audit_quality_gates.ps1 -Mode "BuildOnly"
```

### Step 2: Full Test Suite Verification (Optional / Full Audit)
```powershell
powershell -ExecutionPolicy Bypass -File .agents/skills/lexora-quality-audit/scripts/audit_quality_gates.ps1 -Mode "Full"
```

---

## 3. Error Handling & Recovery Protocols

| Error Condition | Root Cause | Recovery Protocol |
| :--- | :--- | :--- |
| `ERR_PROCESS_LOCKED` | `intermediatexaml\Lexora.Presentation.dll` is locked by another process | Check and terminate lingering `Microsoft.UI.Xaml.Markup.Compiler` or `Lexora.exe` processes. |
| `ERR_COMPILATION_FAIL` | Syntax error or namespace mismatch | Inspect $\pm 5$ lines surrounding the failure line; do not rewrite the entire file. |
| `ERR_TEST_REGRESSION` | One or more unit/integration tests failed | Inspect test failure output; fix the underlying business logic in Domain/Application. |

---

## 4. Verification Check
- Output JSON must contain `"status": "PASSED"`.
- Exit code must be `0`.
