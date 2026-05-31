#!/usr/bin/env python3
"""SessionStart hook: injects Knowledge/Index.md into Claude's context."""
import json
import os
from pathlib import Path

project_dir = os.environ.get("CLAUDE_PROJECT_DIR", ".")
index_path = Path(project_dir) / "Knowledge" / "Index.md"

if not index_path.exists():
    exit(0)

content = index_path.read_text(encoding="utf-8")
output = {
    "hookSpecificOutput": {
        "hookEventName": "SessionStart",
        "additionalContext": content,
    }
}
print(json.dumps(output))
