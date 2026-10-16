#!/usr/bin/env bash

# macOS ships Bash 3.2, where expanding an empty array while nounset is active
# raises an error. This script intentionally treats an empty target list as the
# normal "nothing is running" case, so do not enable `set -u` here.

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
RUN_DIR="${VENDINGFLOW_RUN_DIR:-$REPO_ROOT/.run}"
PID_DIR="$RUN_DIR/pids"

SERVICE_NAMES=(machine-service inventory-service payment-service telemetry-service api-gateway machine-simulator web)
SERVICE_PORTS=(5001 5002 5003 8084 5050 0 5173)
TARGET_PIDS=()

info() {
  printf '[vendingflow] %s\n' "$*"
}

has_pid() {
  local wanted="$1"
  local existing
  for existing in "${TARGET_PIDS[@]}"; do
    [ "$existing" = "$wanted" ] && return 0
  done
  return 1
}

process_is_in_repo() {
  local pid="$1"
  local process_cwd
  process_cwd="$(lsof -a -p "$pid" -d cwd -Fn 2>/dev/null | sed -n 's/^n//p')"
  case "$process_cwd" in
    "$REPO_ROOT"|"$REPO_ROOT"/*) return 0 ;;
    *) return 1 ;;
  esac
}

add_pid() {
  local pid="$1"
  case "$pid" in
    ''|*[!0-9]*) return ;;
  esac
  if kill -0 "$pid" 2>/dev/null && process_is_in_repo "$pid" && ! has_pid "$pid"; then
    TARGET_PIDS+=("$pid")
  fi
}

stop_process_tree() {
  local pid="$1"
  local child
  if command -v pgrep >/dev/null 2>&1; then
    for child in $(pgrep -P "$pid" 2>/dev/null || true); do
      stop_process_tree "$child"
    done
  fi
  kill -TERM "$pid" 2>/dev/null || true
}

command -v lsof >/dev/null 2>&1 || {
  info "error: Required command 'lsof' was not found."
  exit 1
}

for index in "${!SERVICE_NAMES[@]}"; do
  name="${SERVICE_NAMES[$index]}"
  port="${SERVICE_PORTS[$index]}"
  pid_file="$PID_DIR/$name.pid"

  if [ -f "$pid_file" ]; then
    add_pid "$(sed -n '1p' "$pid_file")"
  fi

  if [ "$port" -ne 0 ]; then
    while IFS= read -r pid; do
      add_pid "$pid"
    done < <(lsof -nP -t -iTCP:"$port" -sTCP:LISTEN 2>/dev/null || true)
  fi
done

# Older launcher versions did not write PID files. Include their launcher and
# simulator processes, but only when their working directory is this repository.
if command -v pgrep >/dev/null 2>&1; then
  while IFS= read -r pid; do
    add_pid "$pid"
  done < <(pgrep -f 'scripts/start-all\.sh|VendingFlow\.MachineSimulator' 2>/dev/null || true)
fi

if [ "${#TARGET_PIDS[@]}" -eq 0 ]; then
  info "no running VendingFlow services found"
  rm -f "$PID_DIR"/*.pid 2>/dev/null || true
  rmdir "$PID_DIR" 2>/dev/null || true
  exit 0
fi

info "stopping ${#TARGET_PIDS[@]} VendingFlow process trees..."
for pid in "${TARGET_PIDS[@]}"; do
  stop_process_tree "$pid"
done

for _ in {1..20}; do
  remaining=0
  for pid in "${TARGET_PIDS[@]}"; do
    if kill -0 "$pid" 2>/dev/null; then
      remaining=1
      break
    fi
  done
  [ "$remaining" -eq 0 ] && break
  sleep 0.5
done

for pid in "${TARGET_PIDS[@]}"; do
  if kill -0 "$pid" 2>/dev/null; then
    info "process $pid did not stop gracefully; forcing shutdown"
    kill -KILL "$pid" 2>/dev/null || true
  fi
done

rm -f "$PID_DIR"/*.pid 2>/dev/null || true
rmdir "$PID_DIR" 2>/dev/null || true
info "all VendingFlow services stopped"
