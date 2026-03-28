# Cursor Patch Instructions

Apply to TigerClaw.Cli project

## Step 1: Add Command Parser

Create file:
CliCommandParser.cs

Responsibilities:
- Parse args[]
- Map to CliCommandRequest

## Step 2: Add Command Router

Create:
CliCommandRouter.cs

Map:
run -> RuntimeFacade.RunTaskAsync
workflow -> Workflow methods
skills -> registry

## Step 3: Update Program.cs

Replace Main with:

- parse args
- route command
- print result

## Step 4: Add Compatibility Alias

If first arg == openclaw, ignore

## Step 5: Add Help System

--help support per command

## Step 6: Add JSON Output Mode

--json prints serialized result

## Step 7: Error Handling

Return exit codes

## Step 8: Test

Commands:
tigerclaw run "test"
tigerclaw workflow list
tigerclaw skills list
