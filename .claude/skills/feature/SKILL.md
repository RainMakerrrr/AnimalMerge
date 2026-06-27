---
name: feature
description: Full feature pipeline for AnimalMerge. Takes a task description and drives it to completion: planning → implementation → Unity Console check → game review → fixes → knowledge update. Use when starting work on any new feature or significant change.
---

# Feature Pipeline

Full pipeline for AnimalMerge. Takes a task and drives it to a finished implementation.

**Usage:** `/feature "brief task description [+ path to design doc]"`

---

## Step 1 — Planning

Spawn agent `planner` with the following prompt:

```
Task: <original task text from user>
Design document: <path if provided in the argument, otherwise "none">

Study the project and create a detailed implementation plan.
```

Save the resulting plan — it is needed in the next step.

---

## Step 2 — Implementation

Spawn agent `implementer` with the following prompt:

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
Spawn agent `implementer` with the following prompt:

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

Merge both lists and spawn agent `game-reviewer` with the following prompt:

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

Spawn agent `implementer` with the following prompt:

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

Spawn agent `knowledge-updater` with the following prompt:

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

Output the result in this format:

```
✅ Implemented: <one-line summary>

📋 Changed files:
- <list>

⚠️ Suggestions (not applied):
- <SUGGESTION findings if any, otherwise "none">

🧠 Knowledge base updated.
```
