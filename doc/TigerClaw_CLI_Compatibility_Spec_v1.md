# TigerClaw CLI Compatibility Spec v1

## Overview
TigerClaw CLI provides OpenClaw-compatible commands while routing internally to TigerClaw Runtime.

---

## Command Groups

### 1. run
Execute natural language task

Example:
tigerclaw run "读取未读邮件"

Options:
--model
--local-only
--session

---

### 2. workflow

Commands:
- list
- show <id>
- run <id>
- validate <file>

---

### 3. skills

Commands:
- list
- show <id>
- exec <id>

---

### 4. memory

Commands:
- preferences list/set
- aliases list/set
- procedures list
- search

---

### 5. models

Commands:
- list
- default set
- route show
- test

---

### 6. configure

Commands:
- init
- set
- show

---

### 7. doctor

Check environment

---

### 8. logs

Commands:
- tasks
- task <id>
- steps <id>

---

## Command Object Model

All CLI commands normalized to:

{
  "group": "",
  "action": "",
  "target": "",
  "options": {},
  "inputs": {}
}

---

## Compatibility Rules

1. Command names match OpenClaw
2. Parameters use --key=value
3. Output supports json/text
4. Unknown command fallback to runtime

---

## Exit Codes

0 success  
1 invalid command  
2 execution error  
3 validation error  

---

## Future

- plugin CLI
- remote execution
