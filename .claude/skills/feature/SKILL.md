---
name: feature
description: Full feature pipeline for AnimalMerge. Takes a task description and drives it to completion: planning → implementation → Unity Console check → game review → fixes → knowledge update. Use when starting work on any new feature or significant change.
---

# Feature Pipeline

Full pipeline for AnimalMerge. Takes a task and drives it to a finished implementation.

**Usage:** `/feature "brief task description [+ path to design doc]"`

---

## Execution rules (MANDATORY — read before Step 1)

This skill is an **orchestrator**. The main loop only sequences the pipeline and runs the orchestration calls listed below; all code work — planning, implementing, reviewing, and knowledge updates — is delegated to project-specific subagents. Follow these rules exactly — do not improvise or "improve" the flow:

- **Do NOT explore or read the codebase to understand it.** From the main loop, do not grep/find/Read source files, and do not launch the built-in `Explore` / `Plan` agents to study the code — all code exploration happens *inside* the subagents below, which use CodeGraph (`codegraph_explore`) instead of grep/find/read. Besides invoking the subagents via the Agent tool (which every step below does — that is the whole job of this orchestrator), the only other direct tool calls the main loop makes are the pipeline mechanics: reading the Unity console (Steps 3 and 5) and the `git` file-list commands (Step 4). These are orchestration, not code exploration.
- **Use ONLY these custom subagents, by exact `subagent_type`:** `planner`, `implementer`, `game-reviewer`, `knowledge-updater`. Never substitute the built-in `Explore`, `Plan`, `general-purpose`, or `claude` agents — they lack this project's CodeGraph and CLAUDE.md rules.
- **Run steps strictly in order**, passing each step's output to the next. Do not reorder or merge steps, and do not skip a step — except where the step itself defines a skip condition (Step 5 is skipped when findings are SUGGESTION-only; Step 3's fix branch runs only when the console has errors). One exception to waiting: Step 6 launches in the background and Step 7 does not block on it.
- If a `subagent_type` does not resolve, STOP and tell the user the agent is missing — do not fall back to a built-in agent.

> For a fully deterministic run (guaranteed agent order, no improvisation), use the Workflow version instead: `Workflow({ name: "feature-pipeline", args: "<task>" })`.

## Step 1 — Planning

Use the Agent tool with **`subagent_type: planner`** (never the built-in `Explore`/`Plan` agents). Prompt:

```
Task: <original task text from user>
Design document: <path if provided in the argument, otherwise "none">

Study the project and create a detailed implementation plan.
```

Save the resulting plan — it is needed in the next step.

---

## Step 2 — Implementation

Use the Agent tool with **`subagent_type: implementer`** (never a built-in agent). Prompt:

```
Task: <original task text>

Implementation plan:
<full plan from step 1>

Implement the plan strictly following CLAUDE.md rules.
```

---

## Step 3 — Unity Console check

Read the Unity Console via `mcp__UnityMCP__read_console`.

**If there are compile errors:**
Use the Agent tool with **`subagent_type: implementer`** (never a built-in agent). Prompt:

```
Task: fix compile errors.

Errors:
<error list from console>

Original task for context: <task text>
```

Repeat console check. Maximum 2 iterations. If errors remain after 2 iterations — report to user and stop.

---

## Step 4 — Review

Collect the full list of changed and new files using both commands:
- `git diff --name-only HEAD` — modified tracked files
- `git ls-files --others --exclude-standard` — new untracked files

Merge both lists. Use the Agent tool with **`subagent_type: game-reviewer`** (never a built-in agent). Prompt:

```
Original task: <task text>
Design document: <path if provided>

Changed files:
<merged file list>

Conduct technical and game logic review.
```

Save the findings.

---

## Step 5 — Apply fixes

If findings contain **[CRITICAL]** or **[WARNING]**:

Use the Agent tool with **`subagent_type: implementer`** (never a built-in agent). Prompt:

```
Task: apply review findings.

Findings to fix:
<CRITICAL and WARNING findings only>

Original task for context: <task text>
```

Run a final Unity Console check (1 iteration).

If findings are **[SUGGESTION]** only — skip this step.

---

## Step 6 — Update knowledge base

Use the Agent tool with **`subagent_type: knowledge-updater`** and **`run_in_background: true`**.
This step is not on the critical path — by the time it starts you already have everything Step 7
needs, so launch it and move straight to the report instead of waiting. Prompt:

```
Completed feature: <task text>

What was done:
<brief summary — main changes from plan and findings>

Key decisions (if any): <architectural decisions, new patterns>

Changed files:
<file list>
```

---

## Step 7 — Report to user

Do not wait for Step 6 to finish. Output the result in this format:

```
✅ Implemented: <one-line summary>

📋 Changed files:
- <list>

⚠️ Suggestions (not applied):
- <SUGGESTION findings if any, otherwise "none">

🧠 Knowledge base updated.
```
