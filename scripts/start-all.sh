#!/usr/bin/env bash

set -u

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
RUN_DIR="${VENDINGFLOW_RUN_DIR:-$REPO_ROOT/.run}"
LOG_DIR="$RUN_DIR/logs"
PID_DIR="$RUN_DIR/pids"

SERVICE_NAMES=()
SERVICE_PIDS=()
TAIL_PID=""
SHUTTING_DOWN=0

info() {
  printf '[vendingflow] %s\n' "$*"
}

fail() {
  printf '[vendingflow] error: %s\n' "$*" >&2
  exit 1
}

require_command() {
  command -v "$1" >/dev/null 2>&1 || fail "Required command '$1' was not found."
}

port_is_open() {
  nc -z localhost "$1" >/dev/null 2>&1
}

check_application_port() {
  local port="$1"
  local service="$2"

  if port_is_open "$port"; then
    fail "Port $port is already in use ($service cannot start)."
  fi
}

check_dependency_port() {
  local port="$1"
  local dependency="$2"

  if ! port_is_open "$port"; then
    info "warning: $dependency is not reachable on localhost:$port"
  fi
}

start_service() {
  local name="$1"
  local working_directory="$2"
  shift 2

  local log_file="$LOG_DIR/$name.log"
  : > "$log_file"

  (
    cd "$working_directory" || exit 1
    exec "$@"
  ) >> "$log_file" 2>&1 &

  local pid=$!
  SERVICE_NAMES+=("$name")
  SERVICE_PIDS+=("$pid")
  printf '%s\n' "$pid" > "$PID_DIR/$name.pid"
  info "started $name (pid $pid, log $log_file)"
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

cleanup() {
  local exit_code=$?
  local pid

  if [ "$SHUTTING_DOWN" -eq 1 ]; then
    return
  fi
  SHUTTING_DOWN=1

  if [ -n "$TAIL_PID" ]; then
    kill "$TAIL_PID" 2>/dev/null || true
  fi

  if [ "${#SERVICE_PIDS[@]}" -gt 0 ]; then
    info "stopping services..."
    for pid in "${SERVICE_PIDS[@]}"; do
      stop_process_tree "$pid"
    done
    wait "${SERVICE_PIDS[@]}" 2>/dev/null || true
    for name in "${SERVICE_NAMES[@]}"; do
      rm -f "$PID_DIR/$name.pid"
    done
  fi

  exit "$exit_code"
}

on_signal() {
  trap - INT TERM
  exit 130
}

trap cleanup EXIT
trap on_signal INT TERM

require_command dotnet
require_command mvn
require_command npm
require_command nc

if [ ! -d "$REPO_ROOT/VendingFlow.Web/node_modules" ]; then
  fail "Web dependencies are missing. Run 'cd VendingFlow.Web && npm install' first."
fi

check_application_port 5050 "API Gateway"
check_application_port 5001 "Machine Service"
check_application_port 5002 "Inventory Service"
check_application_port 5003 "Payment Service"
check_application_port 5173 "Web Dashboard"
check_application_port 8084 "Telemetry Service"

check_dependency_port 5432 "PostgreSQL"
check_dependency_port 5672 "RabbitMQ"
check_dependency_port 6379 "Redis"

mkdir -p "$LOG_DIR" "$PID_DIR"

start_service "machine-service" "$REPO_ROOT" \
  env ASPNETCORE_ENVIRONMENT=Development \
  dotnet run --project VendingFlow.MachineService/src/Services/MachineService.API/MachineService.API.csproj \
  --urls http://localhost:5001

start_service "inventory-service" "$REPO_ROOT" \
  env ASPNETCORE_ENVIRONMENT=Development \
  dotnet run --project VendingFlow.InventoryService/src/Services/InventoryService.API/InventoryService.API.csproj \
  --urls http://localhost:5002

start_service "payment-service" "$REPO_ROOT" \
  env ASPNETCORE_ENVIRONMENT=Development \
  dotnet run --project VendingFlow.PaymentService/src/Services/PaymentService.API/PaymentService.API.csproj \
  --urls http://localhost:5003

start_service "telemetry-service" "$REPO_ROOT/VendingFlow.TelemetryService" \
  mvn spring-boot:run

start_service "api-gateway" "$REPO_ROOT" \
  env ASPNETCORE_ENVIRONMENT=Development \
  dotnet run --project VendingFlow.ApiGateway/VendingFlow.ApiGateway.csproj \
  --urls http://localhost:5050

start_service "machine-simulator" "$REPO_ROOT" \
  env DOTNET_ENVIRONMENT=Development \
  dotnet run --project VendingFlow.MachineSimulator/VendingFlow.MachineSimulator.csproj

start_service "web" "$REPO_ROOT/VendingFlow.Web" npm run dev

info "all services launched; press Ctrl+C to stop"
info "dashboard: http://localhost:5173 | gateway: http://localhost:5050"

tail -n 0 -F "$LOG_DIR"/*.log &
TAIL_PID=$!

while :; do
  for index in "${!SERVICE_PIDS[@]}"; do
    if ! kill -0 "${SERVICE_PIDS[$index]}" 2>/dev/null; then
      wait "${SERVICE_PIDS[$index]}" 2>/dev/null
      service_exit=$?
      info "${SERVICE_NAMES[$index]} exited with status $service_exit"
      info "last log lines:"
      tail -n 30 "$LOG_DIR/${SERVICE_NAMES[$index]}.log" >&2
      exit "$service_exit"
    fi
  done
  sleep 1
done
