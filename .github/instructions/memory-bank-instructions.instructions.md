# Memory Bank Instructions

## Overview
Memory Bank system for maintaining project knowledge across sessions. All agents must read ALL memory bank files at start of every task.

## Memory Bank Structure

### Core Files (Required)
1. `brief.md` - Foundation document, manually maintained, defines core requirements
2. `product.md` - Why project exists, problems solved, user experience goals
3. `context.md` - Current work focus, recent changes, next steps (factual only)
4. `architecture.md` - System architecture, technical decisions, design patterns
5. `tech.md` - Technologies, development setup, dependencies, tool usage

### Optional Files
- `tasks.md` - Documentation of repetitive tasks and workflows

## Core Workflows

### Memory Bank Update
Triggered by: new patterns discovered, significant changes, explicit user request "update memory bank".

Process:
1. Review ALL project files
2. Document current state and insights
3. Focus on context.md for current state tracking

### Add Task
For repetitive tasks following similar patterns.

Process:
1. Create/update `tasks.md`
2. Document: task name, files modified, step-by-step workflow, considerations
3. Include discovered context

### Regular Task Execution
- Read ALL memory bank files at task start (mandatory)
- Include `[Memory Bank: Active]` or `[Memory Bank: Missing]` in response
- Update context.md after significant task completion
- Suggest memory bank updates for major changes

## Context Window Management
When context fills up:
1. Suggest updating memory bank to preserve state
2. Recommend fresh conversation
3. Load memory bank files automatically in new session

## Technical Implementation
- Files stored as markdown in `.kilocode/rules/memory-bank/`
- Kilo Code Custom Rules feature
- Both user and agents can access

## Important Notes
- Memory resets completely between sessions
- Memory Bank is ONLY link to previous work
- Must be maintained with precision and clarity
- Prioritize brief.md for inconsistencies

## When to Apply
Apply when managing tasks, creating status files, or creating development tasks for other specialized modes. Do not apply if executing mode with detailed instructions.