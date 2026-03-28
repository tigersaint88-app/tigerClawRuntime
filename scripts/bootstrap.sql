-- TigerClaw Runtime V1 - Database bootstrap
-- SQLite schema

CREATE TABLE IF NOT EXISTS preferences (
    key TEXT NOT NULL,
    value TEXT NOT NULL,
    user_id TEXT NOT NULL DEFAULT '',
    updated_at_utc TEXT NOT NULL,
    PRIMARY KEY (key, user_id)
);

CREATE TABLE IF NOT EXISTS aliases (
    alias TEXT NOT NULL,
    resolved_value TEXT NOT NULL,
    user_id TEXT NOT NULL DEFAULT '',
    updated_at_utc TEXT NOT NULL,
    PRIMARY KEY (alias, user_id)
);

CREATE TABLE IF NOT EXISTS procedures (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    task_type TEXT NOT NULL,
    steps_summary TEXT NOT NULL,
    user_id TEXT,
    created_at_utc TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS tasks (
    id TEXT PRIMARY KEY,
    workflow_id TEXT NOT NULL,
    user_id TEXT NOT NULL,
    input_text TEXT,
    status TEXT NOT NULL,
    created_at_utc TEXT NOT NULL,
    completed_at_utc TEXT
);

CREATE TABLE IF NOT EXISTS task_steps (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    task_id TEXT NOT NULL,
    step_id TEXT NOT NULL,
    status TEXT NOT NULL,
    message TEXT,
    output TEXT,
    artifact_path TEXT,
    completed_at_utc TEXT,
    retry_count INTEGER,
    FOREIGN KEY (task_id) REFERENCES tasks(id)
);

CREATE TABLE IF NOT EXISTS audit_logs (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    task_id TEXT,
    step_id TEXT,
    event_type TEXT NOT NULL,
    message TEXT,
    payload TEXT,
    created_at_utc TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_tasks_workflow_id ON tasks(workflow_id);
CREATE INDEX IF NOT EXISTS idx_tasks_user_id ON tasks(user_id);
CREATE INDEX IF NOT EXISTS idx_task_steps_task_id ON task_steps(task_id);
CREATE INDEX IF NOT EXISTS idx_audit_logs_task_id ON audit_logs(task_id);
