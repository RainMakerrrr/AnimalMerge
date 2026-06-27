export const meta = {
  name: 'feature-pipeline',
  description: 'Deterministic AnimalMerge feature pipeline: planner -> implementer -> Unity console check -> game-reviewer -> fixes -> knowledge-updater. Forces the project custom subagents in exact order (no built-in Explore/Plan substitution).',
  whenToUse: 'Run when you want a guaranteed, ordered feature pipeline for AnimalMerge. Pass the task description as args.',
  phases: [
    { title: 'Plan', detail: 'planner agent builds the implementation plan (CodeGraph-first)' },
    { title: 'Implement', detail: 'implementer agent writes the code per plan' },
    { title: 'Console', detail: 'check Unity console, fix compile errors (max 2 iterations)' },
    { title: 'Review', detail: 'game-reviewer: technical + game-logic review' },
    { title: 'Fix', detail: 'implementer applies CRITICAL/WARNING findings, then re-checks console' },
    { title: 'Knowledge', detail: 'knowledge-updater records changes in Knowledge base' },
  ],
}

// args carries the task. Accept a plain string or { task, designDoc }.
const task = typeof args === 'string' ? args : (args && args.task) || ''
const designDoc = (args && args.designDoc) || 'none'

if (!task) {
  log('ERROR: no task provided. Invoke as Workflow({ name: "feature-pipeline", args: "<task text>" }).')
  return { status: 'error', error: 'no task provided' }
}

// ---------- Step 1: Planning ----------
phase('Plan')
const plan = await agent(
  `Task: ${task}\n` +
  `Design document: ${designDoc}\n\n` +
  `Study the project and create a detailed implementation plan.`,
  { agentType: 'planner', label: 'planner', phase: 'Plan' }
)
if (!plan) {
  log('Planner produced no plan — aborting.')
  return { status: 'error', error: 'planner returned nothing' }
}

// ---------- Step 2: Implementation ----------
phase('Implement')
await agent(
  `Task: ${task}\n\n` +
  `Implementation plan:\n${plan}\n\n` +
  `Implement the plan strictly following CLAUDE.md rules.`,
  { agentType: 'implementer', label: 'implementer', phase: 'Implement' }
)

// ---------- Step 3: Unity console check (max 2 iterations) ----------
phase('Console')
const CONSOLE_SCHEMA = {
  type: 'object',
  additionalProperties: false,
  properties: {
    errorsRemain: { type: 'boolean', description: 'true if compile errors still present after this attempt' },
    summary: { type: 'string' },
  },
  required: ['errorsRemain', 'summary'],
}
let consoleClean = false
for (let i = 0; i < 2; i++) {
  const check = await agent(
    `Read the Unity console via mcp__UnityMCP__read_console (errors only).\n` +
    `If there are compile errors, fix them in the context of this task: "${task}", then re-read the console to confirm.\n` +
    `Report whether compile errors REMAIN after your attempt.`,
    { agentType: 'implementer', label: `console-check-${i + 1}`, phase: 'Console', schema: CONSOLE_SCHEMA }
  )
  // Clean ONLY when the agent explicitly reported no remaining errors.
  // A null/unreadable check is NOT clean — never mark success on a failed read.
  const clean = !!check && check.errorsRemain === false
  log(`Console check ${i + 1}: ${check ? (check.errorsRemain === false ? 'clean' : 'errors remain') : 'unreadable'} — ${(check && check.summary) || ''}`)
  if (clean) { consoleClean = true; break }
}
if (!consoleClean) {
  log('Compile errors not proven cleared after 2 iterations — stopping before review.')
  return { status: 'compile-errors', plan }
}

// ---------- Step 4: Review ----------
phase('Review')
const REVIEW_SCHEMA = {
  type: 'object',
  additionalProperties: false,
  properties: {
    findings: {
      type: 'array',
      items: {
        type: 'object',
        additionalProperties: false,
        properties: {
          severity: { type: 'string', enum: ['CRITICAL', 'WARNING', 'SUGGESTION'] },
          description: { type: 'string' },
          location: { type: 'string', description: 'file:line if applicable' },
        },
        required: ['severity', 'description'],
      },
    },
  },
  required: ['findings'],
}
const review = await agent(
  `Original task: ${task}\n` +
  `Design document: ${designDoc}\n\n` +
  `Collect the changed and new files yourself:\n` +
  `- git diff --name-only HEAD\n` +
  `- git ls-files --others --exclude-standard\n` +
  `Then conduct technical and game-logic review of those files.`,
  { agentType: 'game-reviewer', label: 'game-reviewer', phase: 'Review', schema: REVIEW_SCHEMA }
)
const findings = (review && review.findings) || []
const mustFix = findings.filter(f => f.severity === 'CRITICAL' || f.severity === 'WARNING')
const suggestions = findings.filter(f => f.severity === 'SUGGESTION')

// ---------- Step 5: Apply fixes (conditional) ----------
if (mustFix.length) {
  phase('Fix')
  const fixList = mustFix
    .map(f => `[${f.severity}] ${f.description}${f.location ? ' — ' + f.location : ''}`)
    .join('\n')
  await agent(
    `Task: apply review findings.\n\n` +
    `Findings to fix:\n${fixList}\n\n` +
    `Original task for context: ${task}`,
    { agentType: 'implementer', label: 'apply-fixes', phase: 'Fix' }
  )
  // Final console check — must PROVE the console is clean before we can finish.
  const finalCheck = await agent(
    `Read the Unity console via mcp__UnityMCP__read_console (errors only). ` +
    `Fix any compile errors introduced by the recent fixes for task: "${task}", then re-read to confirm. ` +
    `Report whether compile errors REMAIN after your attempt.`,
    { agentType: 'implementer', label: 'console-check-final', phase: 'Fix', schema: CONSOLE_SCHEMA }
  )
  // Clean ONLY when the agent explicitly reported no remaining errors (mirrors the Step 3 gate).
  // Anything else — errorsRemain true, missing, or an unreadable/null check — is NOT proven clean.
  const finalClean = !!finalCheck && finalCheck.errorsRemain === false
  log(`Final console check: ${finalCheck ? (finalCheck.errorsRemain === false ? 'clean' : 'errors remain') : 'unreadable'} — ${(finalCheck && finalCheck.summary) || ''}`)
  if (!finalClean) {
    log('Compile errors not proven cleared after fixes — stopping before knowledge update.')
    return {
      status: 'compile-errors-after-fix',
      task,
      plan,
      findingsTotal: findings.length,
      criticalsAndWarnings: mustFix.length,
    }
  }
} else {
  log('No CRITICAL/WARNING findings — skipping fix step.')
}

// ---------- Step 6: Knowledge base ----------
phase('Knowledge')
await agent(
  `Completed feature: ${task}\n\n` +
  `What was done (derive the summary from this plan):\n${plan}\n\n` +
  `Key decisions: derive any architectural decisions / new patterns from the plan and the diff.\n` +
  `Changed files: run \`git diff --name-only HEAD\` and \`git ls-files --others --exclude-standard\` yourself.`,
  { agentType: 'knowledge-updater', label: 'knowledge-updater', phase: 'Knowledge' }
)

// ---------- Step 7: Report ----------
return {
  status: 'done',
  task,
  findingsTotal: findings.length,
  criticalsAndWarningsApplied: mustFix.length,
  suggestions: suggestions.map(s => s.description),
}
