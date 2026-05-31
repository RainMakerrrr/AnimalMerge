# update-knowledge

Run this skill when a task is complete. Updates the project knowledge base in `Knowledge/Index.md`.

## Steps

1. **Gather what changed**
   Run: `git log --oneline -10`
   Run: `git diff --name-only HEAD~1 HEAD 2>/dev/null || git status --short`
   Combine with what was discussed in this session.

2. **Update `Knowledge/Index.md`**
   - Add a dated bullet to `## Последние изменения` describing what was done (one line, concise)
   - Update `## Текущее состояние` to reflect the new state of the project
   - If a significant architectural decision was made → create `Knowledge/Decisions/YYYY-MM-DD-short-name.md`

3. **Write a session log (optional, if significant work was done)**
   Create `Knowledge/Sessions/YYYY-MM-DD.md` with:
   ```
   # Session — YYYY-MM-DD

   ## Done
   - bullet list of what was implemented/changed

   ## Decisions
   - any decisions made and why

   ## Next
   - what's left or what comes next
   ```

4. **Confirm**
   Output a one-line summary: "Knowledge updated: [what was added]"

## Rules
- Keep entries in `## Последние изменения` to one line each
- Date format: `[YYYY-MM-DD]`
- Don't duplicate — check existing entries before adding
- Update `## Текущее состояние` fully (replace, don't append)
